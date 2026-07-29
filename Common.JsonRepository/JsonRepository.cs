using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Common.JsonRepository
{
    /// <summary>
    /// Speichert jedes Objekt in einer eigenen JSON-Datei ({key}.json) im konfigurierten Verzeichnis.
    /// </summary>
    public sealed class JsonRepository<T> : IJsonRepository<T>, IDisposable
    {
        private readonly JsonRepositoryOptions _opt;
        private readonly Func<T, string> _keySelector;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly FileSystemWatcher? _watcher;
        private readonly List<JsonRepositoryLoadError> _loadErrors = [];

        public JsonRepository(JsonRepositoryOptions options, Func<T, string> keySelector, bool enableWatcher = false)
        {
            _opt = options ?? throw new ArgumentNullException(nameof(options));
            _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));

            Directory.CreateDirectory(_opt.DirectoryPath);

            if (enableWatcher)
            {
                _watcher = new FileSystemWatcher(_opt.DirectoryPath, "*.json")
                {
                    IncludeSubdirectories = false,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
                };
                _watcher.EnableRaisingEvents = true;
            }
        }

        public void Dispose()
        {
            _watcher?.Dispose();
            _gate.Dispose();
        }

        public string DirectoryPath => _opt.DirectoryPath;
        public IReadOnlyList<JsonRepositoryLoadError> LoadErrors => _loadErrors.ToArray();

        private string FilePathForKey(string key) =>
            JsonRepositoryPath.ForKey(_opt.DirectoryPath, key);

        public async Task<IReadOnlyList<T>> LoadAllAsync()
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                _loadErrors.Clear();
                var files = Directory.GetFiles(_opt.DirectoryPath, "*.json");
                var result = new List<T>(files.Length);
                foreach (var file in files)
                {
                    var item = await ReadSingleFileAsync(file).ConfigureAwait(false);
                    if (item == null) continue;
                    result.Add(item);
                    TryMigrateLegacyFile(file, item);
                }
                return result;
            }
            finally { _gate.Release(); }
        }

        public async Task SaveAllAsync(IEnumerable<T> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                var itemList = items.ToList();

                // Dateien löschen, die nicht mehr vorhanden sind
                foreach (var item in itemList)
                    await WriteItemAsync(item).ConfigureAwait(false);
            }
            finally { _gate.Release(); }
        }

        public async Task<T?> LoadAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return default;

            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                var file = ResolveExistingFilePath(key);
                return await ReadSingleFileAsync(file).ConfigureAwait(false);
            }
            finally { _gate.Release(); }
        }

        public async Task SaveAsync(T item)
        {
            ArgumentNullException.ThrowIfNull(item);
            var key = _keySelector(item);
            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("KeySelector liefert einen leeren Schlüssel.");

            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                await WriteItemAsync(item).ConfigureAwait(false);
            }
            finally { _gate.Release(); }
        }

        public async Task DeleteAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return;

            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                var file = FilePathForKey(key);
                var legacyFile = JsonRepositoryPath.LegacyForKey(_opt.DirectoryPath, key);
                if (File.Exists(file)) File.Delete(file);
                if (!string.Equals(file, legacyFile, StringComparison.OrdinalIgnoreCase) && File.Exists(legacyFile))
                    File.Delete(legacyFile);
            }
            finally { _gate.Release(); }
        }

        private async Task WriteItemAsync(T item)
        {
            var key = _keySelector(item);
            var file = FilePathForKey(key);
            var temp = file + ".tmp";
            var backup = file + ".bak";

            await using (var fs = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(fs, item, _opt.JsonOptions).ConfigureAwait(false);
                await fs.FlushAsync().ConfigureAwait(false);
            }

            if (File.Exists(file))
                File.Replace(temp, file, backup, ignoreMetadataErrors: true);
            else
                File.Move(temp, file);

            var legacyFile = JsonRepositoryPath.LegacyForKey(_opt.DirectoryPath, key);
            if (!string.Equals(file, legacyFile, StringComparison.OrdinalIgnoreCase) && File.Exists(legacyFile))
                File.Delete(legacyFile);
        }

        private string ResolveExistingFilePath(string key)
        {
            var canonical = FilePathForKey(key);
            if (File.Exists(canonical)) return canonical;
            var legacy = JsonRepositoryPath.LegacyForKey(_opt.DirectoryPath, key);
            return File.Exists(legacy) ? legacy : canonical;
        }

        private void TryMigrateLegacyFile(string sourceFile, T item)
        {
            var targetFile = FilePathForKey(_keySelector(item));
            if (string.Equals(sourceFile, targetFile, StringComparison.OrdinalIgnoreCase) || File.Exists(targetFile))
                return;

            try
            {
                File.Move(sourceFile, targetFile);
            }
            catch (IOException)
            {
                // Best effort: Die Quelldatei bleibt vollständig erhalten.
            }
            catch (UnauthorizedAccessException)
            {
                // Best effort: Die Quelldatei bleibt vollständig erhalten.
            }
        }

        private async Task<T?> ReadSingleFileAsync(string file)
        {
            if (!File.Exists(file)) return default;
            try
            {
                await using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                return await JsonSerializer.DeserializeAsync<T>(fs, _opt.JsonOptions).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _loadErrors.Add(new JsonRepositoryLoadError(file, exception.Message));
                return default;
            }
        }
    }
}
