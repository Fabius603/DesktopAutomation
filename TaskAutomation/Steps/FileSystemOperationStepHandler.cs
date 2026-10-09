using System.IO;
using Microsoft.Extensions.Logging;
using TaskAutomation.Jobs;
using TaskAutomation.Logging;

namespace TaskAutomation.Steps;

public sealed class FileSystemOperationStepHandler
    : JobStepHandler<FileSystemOperationStep, FileSystemOperationResult>
{
    private readonly Func<string, string, bool> _sameVolume;
    private readonly Action<string>? _beforePublish;
    private readonly Action<string>? _deleteSource;

    public FileSystemOperationStepHandler() : this(null, null, null) { }
    internal FileSystemOperationStepHandler(Func<string, string, bool>? sameVolume,
        Action<string>? beforePublish, Action<string>? deleteSource)
    {
        _sameVolume = sameVolume ?? ((source, target) => string.Equals(
            Path.GetPathRoot(source), Path.GetPathRoot(target), StringComparison.OrdinalIgnoreCase));
        _beforePublish = beforePublish;
        _deleteSource = deleteSource;
    }

    protected override Task<FileSystemOperationResult> ExecuteCoreAsync(
        FileSystemOperationStep step,
        IStepPipelineContext context,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => ExecuteCore(step, context, cancellationToken), cancellationToken);
    }

    private FileSystemOperationResult ExecuteCore(
        FileSystemOperationStep step,
        IStepPipelineContext context,
        CancellationToken cancellationToken)
    {
        var settings = step.Settings;
        var source = ResolvePath(settings.SourceMode, settings.SourcePath, settings.SourceResult, context);
        var target = settings.Operation is FileSystemOperation.Copy or FileSystemOperation.Move
            ? ResolvePath(settings.TargetMode, settings.TargetPath, settings.TargetResult, context)
            : string.Empty;

        if (context.ExecutionLogSession is { } session)
            context.ExecutionLogService.Record(session, new LogEvent
            {
                Code = LogCodes.StepPaths,
                Context = LogAmbient.Current,
                Category = LogArea.FileAccess,
                Phase = StepLogScope.CurrentPhase,
                Iteration = StepLogScope.CurrentIteration,
                Paths = new[] { new LogPath("Source", source), new LogPath("Target", target) }
                    .Where(path => StepLogPaths.Normalize(path.Value) is not null)
                    .Select(path => path with { Value = StepLogPaths.Normalize(path.Value)! }).ToArray()
            });

        EnsureSourceExists(source);
        var summary = settings.Operation switch
        {
            FileSystemOperation.Copy => Copy(source, target, settings, cancellationToken),
            FileSystemOperation.Move => Move(source, target, settings, cancellationToken),
            FileSystemOperation.Rename => Rename(source, settings.NewName, settings, cancellationToken),
            FileSystemOperation.Delete => Delete(source, settings.Filter, settings, cancellationToken),
            _ => throw new InvalidOperationException($"Unbekannte Dateisystemaktion: {settings.Operation}")
        };

        context.Logger.LogInformation(
            "FileSystemOperationStepHandler: {Operation} von {Source} nach {Target}, {Count} Einträge.",
            settings.Operation, source, summary.TargetPath, summary.Paths.Count);

        return new FileSystemOperationResult
        {
            WasExecuted = true,
            Operation = settings.Operation,
            SourcePath = source,
            TargetPath = summary.TargetPath,
            ItemType = summary.ItemType,
            AffectedCount = summary.Paths.Count,
            AffectedFileCount = summary.FileCount,
            AffectedDirectoryCount = summary.DirectoryCount,
            AffectedBytes = summary.Bytes,
            AffectedPaths = summary.Paths,
            CompletedAtUtc = DateTime.UtcNow
        };
    }

    private static string ResolvePath(
        FileSystemPathSource mode,
        string explicitPath,
        ResultBinding result,
        IStepPipelineContext context)
    {
        var raw = explicitPath;
        if (mode == FileSystemPathSource.TaskResult)
        {
            var resolved = ResultBindingResolver.Resolve<string>(context.Results, result);
            if (!resolved.IsSuccess || string.IsNullOrWhiteSpace(resolved.FirstOrDefault))
                throw new InvalidOperationException(resolved.Error ?? "Das ausgewählte Step-Ergebnis enthält keinen Pfad.");
            raw = resolved.FirstOrDefault!;
        }

        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException("Es wurde kein Pfad angegeben.");
        return Path.GetFullPath(Environment.ExpandEnvironmentVariables(raw.Trim()));
    }

    private OperationSummary Copy(
        string source, string target, FileSystemOperationSettings settings, CancellationToken cancellationToken)
    {
        target = ResolveTargetPath(source, target);
        ValidateDistinctPaths(source, target);
        EnsureTargetMissing(target);
        EnsureParent(target, settings.CreateParentDirectories);
        var staging = target + ".copy-" + Guid.NewGuid().ToString("N");
        try
        {
            if (File.Exists(source))
            {
                ExecuteWithRetry(() => File.Copy(source, staging, false), settings, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                _beforePublish?.Invoke(target);
                File.Move(staging, target, overwrite: false);
                return OperationSummary.ForFile(target, new FileInfo(target).Length);
            }

            EnsureNotInsideSource(source, target);
            var summary = new MutableSummary();
            CopyDirectory(source, staging, settings, summary, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            _beforePublish?.Invoke(target);
            Directory.Move(staging, target);
            var copied = summary.ToResult(target, FileSystemItemType.Directory);
            return copied with { Paths = copied.Paths.Select(path => ReplaceRoot(path, staging, target)).ToArray() };
        }
        catch
        {
            TryDeleteCreatedTarget(staging);
            throw;
        }
    }

    private OperationSummary Move(
        string source, string target, FileSystemOperationSettings settings, CancellationToken cancellationToken)
    {
        target = ResolveTargetPath(source, target);
        ValidateDistinctPaths(source, target);
        EnsureTargetMissing(target);
        EnsureParent(target, settings.CreateParentDirectories);
        if (Directory.Exists(source)) EnsureNotInsideSource(source, target);

        var sameRoot = _sameVolume(source, target);
        if (sameRoot)
        {
            var before = Inspect(source);
            ExecuteWithRetry(
                () =>
                {
                    if (File.Exists(source)) File.Move(source, target);
                    else Directory.Move(source, target);
                },
                settings, cancellationToken);
            return before with
            {
                TargetPath = target,
                Paths = before.Paths.Select(path => ReplaceRoot(path, source, target)).ToArray()
            };
        }

        var copied = Copy(source, target, settings, cancellationToken);
        // Source deletion may fail after deleting some entries. The complete copy is then
        // the only recoverable version and must be retained.
        if (_deleteSource is null) DeleteExact(source, settings, cancellationToken);
        else _deleteSource(source);
        return copied;
    }

    private static OperationSummary Rename(
        string source, string newName, FileSystemOperationSettings settings, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(newName)
            || newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || !string.Equals(Path.GetFileName(newName), newName, StringComparison.Ordinal))
            throw new InvalidOperationException("Der neue Name ist ungültig.");

        var parent = Path.GetDirectoryName(source)
            ?? throw new InvalidOperationException("Der Quellpfad besitzt kein übergeordnetes Verzeichnis.");
        var target = Path.Combine(parent, newName);
        ValidateDistinctPaths(source, target);
        EnsureTargetMissing(target);
        var before = Inspect(source);
        ExecuteWithRetry(
            () =>
            {
                if (File.Exists(source)) File.Move(source, target);
                else Directory.Move(source, target);
            },
            settings, cancellationToken);
        return before with
        {
            TargetPath = target,
            Paths = before.Paths.Select(path => ReplaceRoot(path, source, target)).ToArray()
        };
    }

    private static OperationSummary Delete(
        string source, string filter, FileSystemOperationSettings settings, CancellationToken cancellationToken)
    {
        if (File.Exists(source) || string.IsNullOrWhiteSpace(filter))
        {
            ProtectRoot(source);
            var before = Inspect(source);
            DeleteExact(source, settings, cancellationToken);
            return before with { TargetPath = string.Empty };
        }

        ProtectRoot(source);
        var matches = ParseFilters(filter)
            .SelectMany(pattern => Directory.EnumerateFileSystemEntries(source, pattern, SearchOption.TopDirectoryOnly))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var combined = new MutableSummary();
        foreach (var path in matches)
        {
            cancellationToken.ThrowIfCancellationRequested();
            combined.Add(Inspect(path));
            DeleteExact(path, settings, cancellationToken);
        }
        return combined.ToResult(string.Empty, FileSystemItemType.Multiple);
    }

    private static void CopyDirectory(
        string source, string target, FileSystemOperationSettings settings,
        MutableSummary summary, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if ((File.GetAttributes(source) & FileAttributes.ReparsePoint) != 0)
            throw new NotSupportedException($"Verknüpfte Ordner werden nicht unterstützt: {source}");

        Directory.CreateDirectory(target);
        summary.AddDirectory(target);
        foreach (var file in Directory.EnumerateFiles(source))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var destination = Path.Combine(target, Path.GetFileName(file));
            ExecuteWithRetry(() => File.Copy(file, destination, false), settings, cancellationToken);
            summary.AddFile(destination, new FileInfo(destination).Length);
        }
        foreach (var directory in Directory.EnumerateDirectories(source))
            CopyDirectory(directory, Path.Combine(target, Path.GetFileName(directory)), settings, summary, cancellationToken);
    }

    private static OperationSummary Inspect(string path)
    {
        if (File.Exists(path))
            return OperationSummary.ForFile(path, new FileInfo(path).Length);
        var files = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).ToArray();
        var directories = new[] { path }
            .Concat(Directory.EnumerateDirectories(path, "*", SearchOption.AllDirectories)).ToArray();
        return new OperationSummary(path, FileSystemItemType.Directory,
            files.Length, directories.Length, files.Sum(file => new FileInfo(file).Length),
            directories.Concat(files).ToArray());
    }

    private static void DeleteExact(
        string path, FileSystemOperationSettings settings, CancellationToken cancellationToken)
    {
        ExecuteWithRetry(
            () =>
            {
                if (File.Exists(path)) File.Delete(path);
                else Directory.Delete(path, true);
            },
            settings, cancellationToken);
    }

    private static void ExecuteWithRetry(
        Action action, FileSystemOperationSettings settings, CancellationToken cancellationToken)
    {
        var retries = settings.RetryLockedFiles ? settings.RetryCount : 0;
        for (var attempt = 0; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try { action(); return; }
            catch (IOException ex) when (IsSharingViolation(ex) && attempt < retries)
            {
                if (settings.RetryDelayMs > 0)
                    Task.Delay(settings.RetryDelayMs, cancellationToken).GetAwaiter().GetResult();
            }
        }
    }

    private static bool IsSharingViolation(IOException exception) =>
        (exception.HResult & 0xFFFF) is 32 or 33;

    private static void EnsureSourceExists(string source)
    {
        if (!File.Exists(source) && !Directory.Exists(source))
            throw new FileNotFoundException($"Die Quelle wurde nicht gefunden: {source}", source);
    }

    private static void EnsureTargetMissing(string target)
    {
        if (File.Exists(target) || Directory.Exists(target))
            throw new IOException($"Am Zielpfad ist bereits eine Datei oder ein Ordner vorhanden: {target}");
    }

    private static string ResolveTargetPath(string source, string target)
    {
        if (!Directory.Exists(target))
            return target;

        var sourceName = Path.GetFileName(Path.TrimEndingDirectorySeparator(source));
        if (string.IsNullOrWhiteSpace(sourceName))
            throw new InvalidOperationException("Für die Quelle konnte kein Name ermittelt werden.");
        return Path.Combine(target, sourceName);
    }

    private static void EnsureParent(string target, bool create)
    {
        var parent = Path.GetDirectoryName(target);
        if (string.IsNullOrWhiteSpace(parent)) return;
        if (Directory.Exists(parent)) return;
        if (!create) throw new DirectoryNotFoundException($"Der übergeordnete Zielordner wurde nicht gefunden: {parent}");
        Directory.CreateDirectory(parent);
    }

    private static void ValidateDistinctPaths(string source, string target)
    {
        if (string.Equals(
                Path.TrimEndingDirectorySeparator(source),
                Path.TrimEndingDirectorySeparator(target),
                StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Quelle und Ziel dürfen nicht identisch sein.");
    }

    private static void EnsureNotInsideSource(string source, string target)
    {
        var sourcePrefix = Path.TrimEndingDirectorySeparator(source) + Path.DirectorySeparatorChar;
        if (target.StartsWith(sourcePrefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Ein Ordner darf nicht in einen eigenen Unterordner kopiert oder verschoben werden.");
    }

    private static void ProtectRoot(string path)
    {
        if (string.Equals(
                Path.TrimEndingDirectorySeparator(path),
                Path.TrimEndingDirectorySeparator(Path.GetPathRoot(path) ?? string.Empty),
                StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Ein Laufwerksstamm darf nicht gelöscht oder geleert werden.");
    }

    private static string[] ParseFilters(string filter)
    {
        var filters = filter.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (filters.Length == 0 || filters.Any(value =>
                value.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]) >= 0))
            throw new InvalidOperationException("Der Löschfilter ist ungültig.");
        return filters;
    }

    private static string ReplaceRoot(string path, string source, string target) =>
        string.Equals(path, source, StringComparison.OrdinalIgnoreCase)
            ? target
            : target + path[source.Length..];

    private static void TryDeleteCreatedTarget(string target)
    {
        try
        {
            if (File.Exists(target)) File.Delete(target);
            else if (Directory.Exists(target)) Directory.Delete(target, true);
        }
        catch { }
    }

    protected override FileSystemOperationResult CreateDefault() => FileSystemOperationResult.Default;

    private sealed record OperationSummary(
        string TargetPath, FileSystemItemType ItemType, int FileCount,
        int DirectoryCount, long Bytes, IReadOnlyList<string> Paths)
    {
        public static OperationSummary ForFile(string path, long bytes) =>
            new(path, FileSystemItemType.File, 1, 0, bytes, [path]);
    }

    private sealed class MutableSummary
    {
        private readonly List<string> _paths = [];
        private int _files;
        private int _directories;
        private long _bytes;
        public void AddFile(string path, long bytes) { _paths.Add(path); _files++; _bytes += bytes; }
        public void AddDirectory(string path) { _paths.Add(path); _directories++; }
        public void Add(OperationSummary summary)
        {
            _paths.AddRange(summary.Paths);
            _files += summary.FileCount;
            _directories += summary.DirectoryCount;
            _bytes += summary.Bytes;
        }
        public OperationSummary ToResult(string target, FileSystemItemType type) =>
            new(target, type, _files, _directories, _bytes, _paths.ToArray());
    }
}
