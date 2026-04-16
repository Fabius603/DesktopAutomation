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

        private string FilePathForKey(string key) =>
            Path.Combine(_opt.DirectoryPath, NamePolicy.Sanitize(key) + ".json");

        public async Task<IReadOnlyList<T>> LoadAllAsync()
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                var files = Directory.GetFiles(_opt.DirectoryPath, "*.json");
                var result = new List<T>(files.Length);
                foreach (var file in files)
                {
                    var item = await ReadSingleFileAsync(file).ConfigureAwait(false);
                    if (item != null) result.Add(item);
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
                var newKeys = new HashSet<string>(itemList.Select(x => NamePolicy.Sanitize(_keySelector(x))), StringComparer.OrdinalIgnoreCase);

                // Dateien löschen, die nicht mehr vorhanden sind
                foreach (var existing in Directory.GetFiles(_opt.DirectoryPath, "*.json"))
                {
                    var baseName = Path.GetFileNameWithoutExtension(existing);
                    if (!newKeys.Contains(baseName))
                        try { File.Delete(existing); } catch { }
                }

                // Jedes Item in eigene Datei schreiben
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
                var file = FilePathForKey(key);
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
                if (File.Exists(file)) File.Delete(file);
            }
            finally { _gate.Release(); }
        }

        private async Task WriteItemAsync(T item)
        {
            var key = _keySelector(item);
            var file = FilePathForKey(key);
            var temp = file + ".tmp";

            await using (var fs = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(fs, item, _opt.JsonOptions).ConfigureAwait(false);
                await fs.FlushAsync().ConfigureAwait(false);
            }

            File.Move(temp, file, overwrite: true);
        }

        private async Task<T?> ReadSingleFileAsync(string file)
        {
            if (!File.Exists(file)) return default;
            try
            {
                await using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                return await JsonSerializer.DeserializeAsync<T>(fs, _opt.JsonOptions).ConfigureAwait(false);
            }
            catch
            {
                return default;
            }
        }
    }
}
