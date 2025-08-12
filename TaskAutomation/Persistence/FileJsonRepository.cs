using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TaskAutomation.Persistence
{
    /// <summary>
    /// Liest alle *.json aus einem Ordner (je Datei: Objekt ODER Liste von Objekten).
    /// Speichert atomar in EINE konsolidierte Datei (z. B. "hotkeys.json") im selben Ordner.
    /// </summary>
    public sealed class FileJsonRepository<T> : IJsonRepository<T>, IDisposable
    {
        private readonly FolderJsonRepositoryOptions _opt;
        private readonly Func<T, string> _keySelector;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly FileSystemWatcher? _watcher;

        public FileJsonRepository(FolderJsonRepositoryOptions options, Func<T, string> keySelector, bool enableWatcher = false)
        {
            _opt = options ?? throw new ArgumentNullException(nameof(options));
            _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));

            Directory.CreateDirectory(_opt.FolderPath);

            if (enableWatcher)
            {
                _watcher = new FileSystemWatcher(_opt.FolderPath, _opt.SearchPattern)
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

        private static IEnumerable<T> DeserializeManyOrSingle(string json, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.ValueKind switch
            {
                JsonValueKind.Array => JsonSerializer.Deserialize<List<T>>(json, options) ?? Enumerable.Empty<T>(),
                JsonValueKind.Object => JsonSerializer.Deserialize<T>(json, options) is { } one ? new[] { one } : Enumerable.Empty<T>(),
                _ => Enumerable.Empty<T>()
            };
        }

        public async Task<IReadOnlyList<T>> LoadAllAsync()
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                var list = new List<T>();
                foreach (var file in Directory.EnumerateFiles(_opt.FolderPath, _opt.SearchPattern, SearchOption.TopDirectoryOnly))
                {
                    // Die konsolidierte Datei gehört auch dazu – ist okay (wir mergen einfach).
                    var fileItems = await ReadFileFlexibleAsync(file).ConfigureAwait(false);
                    if (fileItems != null && fileItems.Count > 0)
                        list.AddRange(fileItems);
                }

                // Duplikate (gleicher Key) zuletzt gewinnt: nach Key gruppieren
                var merged = list
                    .GroupBy(_keySelector, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.Last())
                    .ToList();

                return merged;
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task SaveAllAsync(IEnumerable<T> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            var target = Path.Combine(_opt.FolderPath, _opt.SaveFileName);
            var temp = target + ".tmp";

            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                // JSON schreiben
                await using (var fs = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await JsonSerializer.SerializeAsync(fs, items, _opt.JsonOptions).ConfigureAwait(false);
                    await fs.FlushAsync().ConfigureAwait(false);
                }

                // Backup der Ziel-Datei (optional)
                if (_opt.CreateBackup && File.Exists(target))
                {
                    // 1) Backup-Ordner anlegen (Unterordner "Backup" neben der Zieldatei)
                    var targetDir = Path.GetDirectoryName(target)!;
                    var backupDir = Path.Combine(targetDir, "Backup");
                    Directory.CreateDirectory(backupDir);

                    // 2) Backup-Dateiname mit Zeitstempel (YYYYMMDDHHMMSSfff empfohlen)
                    var baseName = Path.GetFileName(target);
                    var timestamp = DateTime.UtcNow.ToString(_opt.BackupSuffixFormat);
                    var bak = Path.Combine(backupDir, $"{baseName}.{timestamp}.bak");

                    // 3) Kollisionen sicher vermeiden (falls zwei Backups im selben Takt)
                    int counter = 1;
                    while (File.Exists(bak))
                    {
                        bak = Path.Combine(backupDir, $"{baseName}.{timestamp}_{counter++}.bak");
                    }

                    File.Copy(target, bak, overwrite: false);

                    // 4) Retention: maximal 5 Backups pro Basisdatei behalten
                    const int MAX_BACKUPS = 5;

                    // Alle Backups für diese Basisdatei einsammeln
                    var allBackups = Directory.GetFiles(backupDir, $"{baseName}.*.bak");

                    // Sortierung: neueste zuerst.
                    // Hinweis: Wenn _opt.BackupSuffixFormat = "yyyyMMddHHmmssfff" ist,
                    // ist die Dateiname-Sortierung bereits chronologisch korrekt.
                    var ordered = allBackups
                        .OrderByDescending(f => f, StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    if (ordered.Count > MAX_BACKUPS)
                    {
                        foreach (var old in ordered.Skip(MAX_BACKUPS))
                        {
                            try { File.Delete(old); } catch { /* absichtlich ignoriert */ }
                        }
                    }
                }

                // Atomar ersetzen
                File.Move(temp, target, overwrite: true);
            }
            finally
            {
                // Temp aufräumen, falls Move fehlgeschlagen
                if (File.Exists(temp))
                {
                    try { File.Delete(temp); } catch { /* ignore */ }
                }
                _gate.Release();
            }
        }

        public async Task<T?> LoadAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return default;
            var all = await LoadAllAsync().ConfigureAwait(false);
            return all.FirstOrDefault(x => string.Equals(_keySelector(x), key, StringComparison.OrdinalIgnoreCase));
        }

        public async Task SaveAsync(T item)
        {
            ArgumentNullException.ThrowIfNull(item);
            var key = _keySelector(item);
            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("KeySelector liefert einen leeren Schlüssel.");

            var all = (await LoadAllAsync().ConfigureAwait(false)).ToList();
            var idx = all.FindIndex(x => string.Equals(_keySelector(x), key, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0) all[idx] = item; else all.Add(item);
            await SaveAllAsync(all).ConfigureAwait(false);
        }

        public async Task DeleteAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            var all = (await LoadAllAsync().ConfigureAwait(false)).ToList();
            all.RemoveAll(x => string.Equals(_keySelector(x), key, StringComparison.OrdinalIgnoreCase));
            await SaveAllAsync(all).ConfigureAwait(false);
        }

        /// <summary>
        /// Liest eine Datei, die entweder eine Liste (JSON-Array) oder ein einzelnes Objekt enthält.
        /// </summary>
        private async Task<List<T>> ReadFileFlexibleAsync(string file)
        {
            try
            {
                await using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var doc = await JsonDocument.ParseAsync(fs).ConfigureAwait(false);

                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    // Liste
                    await using var fs2 = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    var items = await JsonSerializer.DeserializeAsync<List<T>>(fs2, _opt.JsonOptions).ConfigureAwait(false);
                    return items ?? new List<T>();
                }
                else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    // Einzel-Objekt
                    await using var fs2 = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    var obj = await JsonSerializer.DeserializeAsync<T>(fs2, _opt.JsonOptions).ConfigureAwait(false);
                    return obj != null ? new List<T> { obj } : new List<T>();
                }
                else
                {
                    // ignorieren (z. B. leere Datei)
                    return new List<T>();
                }
            }
            catch
            {
                // Datei überspringen, wenn inkonsistent – Logging optional an der Stelle injizieren
                return new List<T>();
            }
        }
    }
}
