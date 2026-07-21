using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TaskAutomation.Scripts
{
    public sealed class ScriptExecutor : IScriptExecutor
    {
        private readonly ILogger<ScriptExecutor> _logger;

        public ScriptExecutor(ILogger<ScriptExecutor> logger)
        {
            _logger = logger;
        }

        public async Task ExecuteScriptFile(
            string scriptPath,
            string arguments,
            CancellationToken ct = default,
            Action<string, bool>? outputCallback = null)
        {
            if (string.IsNullOrWhiteSpace(scriptPath))
                throw new ArgumentException("Script path is null/empty.", nameof(scriptPath));

            scriptPath = Path.GetFullPath(scriptPath);

            if (!File.Exists(scriptPath))
                throw new FileNotFoundException("Script file not found.", scriptPath);

            var (fileName, commandArguments, workingDir) = ResolveCommand(scriptPath, arguments);

            _logger.LogInformation("Starte Skript {Script} mit Interpreter {Exe} {Args} (WorkingDir={Dir})",
                scriptPath, fileName, commandArguments, workingDir);

            using var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = commandArguments,
                    WorkingDirectory = workingDir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                },
                EnableRaisingEvents = true
            };

            proc.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    _logger.LogDebug("[{Script}] STDOUT: {Line}", Path.GetFileName(scriptPath), e.Data);
                    ForwardOutput(outputCallback, e.Data, isError: false, scriptPath);
                }
            };
            proc.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    _logger.LogWarning("[{Script}] STDERR: {Line}", Path.GetFileName(scriptPath), e.Data);
                    ForwardOutput(outputCallback, e.Data, isError: true, scriptPath);
                }
            };

            if (!proc.Start())
                throw new InvalidOperationException($"Failed to start process for script '{scriptPath}'.");

            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            using var reg = ct.Register(() =>
            {
                try
                {
                    if (!proc.HasExited)
                    {
                        _logger.LogWarning("Cancellation angefordert – beende Prozess {Pid} ({Script})", proc.Id, scriptPath);
                        KillProcessTree(proc);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler beim Abbrechen des Prozesses {Pid} ({Script})", proc.Id, scriptPath);
                }
            });

            var sw = Stopwatch.StartNew();

            try
            {
                await proc.WaitForExitAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Ausführung von {Script} abgebrochen.", scriptPath);
            }

            sw.Stop();

            if (proc.ExitCode == 0)
            {
                _logger.LogInformation("Skript {Script} erfolgreich beendet (Dauer {Duration} ms).",
                    scriptPath, sw.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogError("Skript {Script} beendet mit ExitCode {ExitCode} (Dauer {Duration} ms).",
                    scriptPath, proc.ExitCode, sw.ElapsedMilliseconds);
                throw new InvalidOperationException($"Script '{scriptPath}' exited with code {proc.ExitCode}.");
            }
        }

        private void ForwardOutput(
            Action<string, bool>? outputCallback,
            string line,
            bool isError,
            string scriptPath)
        {
            if (outputCallback == null) return;
            try
            {
                outputCallback(line, isError);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Skriptausgabe von {Script} konnte nicht an das Job-Log weitergeleitet werden.",
                    scriptPath);
            }
        }

        // Rest wie gehabt …
        private static (string fileName, string arguments, string workingDir) ResolveCommand(
            string scriptPath, string? scriptArguments)
        {
            var ext = Path.GetExtension(scriptPath).ToLowerInvariant();
            var dir = Path.GetDirectoryName(scriptPath) ?? Environment.CurrentDirectory;
            string? shebang = TryReadShebang(scriptPath);

            switch (ext)
            {
                case ".ps1":
                    var pwsh = FindOnPath("pwsh.exe");
                    var ps = pwsh ?? FindOnPath("powershell.exe") ?? "powershell.exe";
                    var args = $"-NoLogo -NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"";
                    return (ps, AppendArguments(args, scriptArguments), dir);

                case ".bat":
                case ".cmd":
                    return ("cmd.exe", AppendArguments($"/c \"{scriptPath}\"", scriptArguments), dir);

                case ".sh":
                    var bash = FindOnPath("bash.exe") ?? "bash";
                    return (bash, AppendArguments($"\"{scriptPath}\"", scriptArguments), dir);

                case ".py":
                    var python = FindOnPath("python.exe") ?? FindOnPath("py.exe") ?? "python";
                    return (python, AppendArguments($"\"{scriptPath}\"", scriptArguments), dir);

                case ".js":
                    var node = FindOnPath("node.exe") ?? "node";
                    return (node, AppendArguments($"\"{scriptPath}\"", scriptArguments), dir);

                case ".vbs":
                case ".wsf":
                    var cscript = FindOnPath("cscript.exe") ?? "cscript.exe";
                    return (cscript, AppendArguments($"//nologo \"{scriptPath}\"", scriptArguments), dir);

                case ".exe":
                    return (scriptPath, scriptArguments?.Trim() ?? string.Empty, dir);

                default:
                    if (!string.IsNullOrEmpty(shebang))
                    {
                        var (exe, shebangArgs) = ParseShebang(shebang);
                        var exePath = FindOnPath(exe) ?? exe;
                        var args2 = string.IsNullOrEmpty(shebangArgs)
                            ? $"\"{scriptPath}\""
                            : $"{shebangArgs} \"{scriptPath}\"";
                        return (exePath, AppendArguments(args2, scriptArguments), dir);
                    }
                    return (scriptPath, scriptArguments?.Trim() ?? string.Empty, dir);
            }
        }

        private static string AppendArguments(string commandArguments, string? scriptArguments)
            => string.IsNullOrWhiteSpace(scriptArguments)
                ? commandArguments
                : $"{commandArguments} {scriptArguments.Trim()}";

        private static string? TryReadShebang(string scriptPath)
        {
            try
            {
                using var sr = new StreamReader(scriptPath);
                var first = sr.ReadLine();
                if (first != null && first.StartsWith("#!"))
                    return first.Substring(2).Trim();
            }
            catch { }
            return null;
        }

        private static (string exe, string args) ParseShebang(string shebang)
        {
            var parts = shebang.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return (shebang, "");
            var exe = parts[0];
            var args = string.Join(" ", parts.Skip(1));
            return (exe, args);
        }

        private static string? FindOnPath(string fileName)
        {
            try
            {
                if (Path.IsPathRooted(fileName) && File.Exists(fileName))
                    return fileName;

                var envPath = Environment.GetEnvironmentVariable("PATH") ?? "";
                foreach (var p in envPath.Split(Path.PathSeparator))
                {
                    if (string.IsNullOrWhiteSpace(p)) continue;
                    var candidate = Path.Combine(p.Trim(), fileName);
                    if (File.Exists(candidate))
                        return candidate;
                }
            }
            catch { }
            return null;
        }

        private static void KillProcessTree(Process proc)
        {
            try
            {
                if (proc.HasExited) return;
                proc.Kill(entireProcessTree: true);
            }
            catch { }
        }
    }
}
