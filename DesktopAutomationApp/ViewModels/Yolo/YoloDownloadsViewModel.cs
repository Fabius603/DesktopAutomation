using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using ImageDetection.YOLO;
using ImageDetection.Model;

namespace DesktopAutomationApp.ViewModels
{
    public sealed class YoloDownloadsViewModel : ViewModelBase
    {
        private readonly IYOLOModelDownloader _downloader;
        private readonly ILogger<YoloDownloadsViewModel> _logger;
        private CancellationTokenSource? _cancellationTokenSource;

        public ObservableCollection<YoloModelEntry> Models { get; } = new();

        public ICommand RefreshCommand { get; }
        public ICommand DownloadCommand { get; }
        public ICommand UninstallCommand { get; }
        public ICommand OpenFolderCommand { get; }
        public ICommand AddLocalModelCommand { get; }

        public YoloDownloadsViewModel(IYOLOModelDownloader downloader, ILogger<YoloDownloadsViewModel> logger)
        {
            _downloader = downloader;
            _logger = logger;

            RefreshCommand = new RelayCommand(async () => await RefreshModelsAsync());
            DownloadCommand = new RelayCommand<YoloModelEntry>(async model => await DownloadModelAsync(model), CanDownload);
            UninstallCommand = new RelayCommand<YoloModelEntry>(async model => await UninstallModelAsync(model), CanUninstall);
            OpenFolderCommand = new RelayCommand(OpenFolder);
            AddLocalModelCommand = new RelayCommand(async () => await AddLocalModelAsync());

            _downloader.DownloadProgressChanged += OnDownloadProgressChanged;

            _ = Task.Run(async () => await RefreshModelsAsync());
        }

        private void OpenFolder()
        {
            Directory.CreateDirectory(_downloader.ModelFolderPath);
            System.Diagnostics.Process.Start("explorer.exe", _downloader.ModelFolderPath);
        }

        private async Task RefreshModelsAsync()
        {
            try
            {
                var folder = _downloader.ModelFolderPath;
                Directory.CreateDirectory(folder);

                var manifest = ModelManifestProvider.LoadManifest(folder);
                var existingModels = Models.ToList();

                var onDisk = Directory.GetFiles(folder, "*.onnx")
                    .Select(f => Path.GetFileNameWithoutExtension(f)!)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                Models.Clear();

                // 1) Manifest-Modelle
                foreach (var kvp in manifest.Models)
                {
                    var modelKey = kvp.Key;
                    var manifestEntry = kvp.Value;
                    var onnxPath = Path.Combine(folder, modelKey + ".onnx");
                    var isInstalled = File.Exists(onnxPath);

                    var fileSize = isInstalled ? TryGetFileSize(onnxPath) : 0L;

                    var existing = existingModels.FirstOrDefault(e => e.ModelKey == modelKey);
                    if (existing != null)
                    {
                        existing.IsInstalled = isInstalled;
                        existing.FileSizeBytes = fileSize;
                        existing.IsLocal = false;
                        Models.Add(existing);
                    }
                    else
                    {
                        Models.Add(new YoloModelEntry
                        {
                            ModelKey = modelKey,
                            DisplayName = manifestEntry.DisplayName ?? modelKey,
                            Description = manifestEntry.Description ?? "",
                            IsInstalled = isInstalled,
                            FileSizeBytes = fileSize,
                            IsLocal = false
                        });
                    }
                }

                // 2) Lokal abgelegte Modelle die NICHT im Manifest stehen
                foreach (var key in onDisk.Where(k => !manifest.Models.ContainsKey(k)).OrderBy(k => k))
                {
                    var onnxPath = Path.Combine(folder, key + ".onnx");
                    var existing = existingModels.FirstOrDefault(e => e.ModelKey == key);
                    if (existing != null)
                    {
                        existing.FileSizeBytes = TryGetFileSize(onnxPath);
                        Models.Add(existing);
                    }
                    else
                    {
                        Models.Add(new YoloModelEntry
                        {
                            ModelKey = key,
                            DisplayName = key,
                            Description = "",
                            IsInstalled = true,
                            FileSizeBytes = TryGetFileSize(onnxPath),
                            IsLocal = true
                        });
                    }
                }

                _logger.LogInformation("Modelliste aktualisiert: {count} Einträge.", Models.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Aktualisieren der Modelliste.");
            }
        }

        private async Task DownloadModelAsync(YoloModelEntry? model)
        {
            if (model == null || model.IsInstalled || model.IsDownloading || model.IsLocal)
                return;

            try
            {
                model.IsDownloading = true;
                model.DownloadProgress = 0;

                _cancellationTokenSource = new CancellationTokenSource();
                var ct = _cancellationTokenSource.Token;

                _logger.LogInformation("Download gestartet für Modell: {modelKey}", model.ModelKey);

                await _downloader.DownloadModelAsync(model.ModelKey, ct);

                model.IsDownloading = false;
                model.DownloadProgress = 100;
                await RefreshModelsAsync();

                _logger.LogInformation("Download abgeschlossen für Modell: {modelKey}", model.ModelKey);
            }
            catch (OperationCanceledException)
            {
                model.IsDownloading = false;
                model.DownloadProgress = 0;
                _logger.LogInformation("Download abgebrochen für Modell: {modelKey}", model.ModelKey);
            }
            catch (Exception ex)
            {
                model.IsDownloading = false;
                model.DownloadProgress = 0;
                _logger.LogError(ex, "Fehler beim Download von Modell: {modelKey}", model.ModelKey);
            }
        }

        private async Task UninstallModelAsync(YoloModelEntry? model)
        {
            if (model == null || !model.IsInstalled)
                return;

            try
            {
                _logger.LogInformation("Deinstallation gestartet für Modell: {modelKey}", model.ModelKey);

                var success = await _downloader.UninstallModelAsync(model.ModelKey);

                if (success)
                {
                    await RefreshModelsAsync();
                    _logger.LogInformation("Deinstallation abgeschlossen für Modell: {modelKey}", model.ModelKey);
                }
                else
                {
                    _logger.LogWarning("Deinstallation fehlgeschlagen für Modell: {modelKey}", model.ModelKey);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler bei der Deinstallation von Modell: {modelKey}", model.ModelKey);
            }
        }

        private void OnDownloadProgressChanged(object? sender, ModelDownloadProgressEventArgs e)
        {
            var model = Models.FirstOrDefault(m => m.ModelKey == e.ModelName);
            if (model != null)
            {
                model.DownloadProgress = e.ProgressPercent;
                model.IsDownloading = e.Status == ModelDownloadStatus.DownloadingOnnx;
            }
        }

        private bool CanDownload(YoloModelEntry? model)
            => model != null && !model.IsInstalled && !model.IsDownloading && !model.IsLocal;

        private bool CanUninstall(YoloModelEntry? model)
            => model != null && model.IsInstalled && !model.IsDownloading;

        private async Task AddLocalModelAsync()
        {
            var dialog = new OpenFileDialog
            {
                Title = "ONNX-Modelldatei auswählen",
                Filter = "ONNX-Modelle (*.onnx)|*.onnx",
                Multiselect = false
            };

            if (dialog.ShowDialog() != true)
                return;

            var sourcePath = dialog.FileName;
            var modelKey = Path.GetFileNameWithoutExtension(sourcePath);
            var destPath = Path.Combine(_downloader.ModelFolderPath, Path.GetFileName(sourcePath));

            try
            {
                Directory.CreateDirectory(_downloader.ModelFolderPath);
                File.Copy(sourcePath, destPath, overwrite: true);
                await _downloader.EnsureLabelsAsync(modelKey, destPath);
                await RefreshModelsAsync();
                _logger.LogInformation("Lokales Modell hinzugefügt: {modelKey}", modelKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Hinzufügen des lokalen Modells: {modelKey}", modelKey);
            }
        }

        private static long TryGetFileSize(string path)        {
            try { return new FileInfo(path).Length; }
            catch { return 0L; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _downloader.DownloadProgressChanged -= OnDownloadProgressChanged;
            }
            base.Dispose(disposing);
        }
    }

    public class YoloModelEntry : INotifyPropertyChanged
    {
        private bool _isInstalled;
        private int _downloadProgress;
        private bool _isDownloading;
        private long _fileSizeBytes;
        private bool _isLocal;

        public string ModelKey { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public bool IsInstalled
        {
            get => _isInstalled;
            set { _isInstalled = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusText)); }
        }

        public int DownloadProgress
        {
            get => _downloadProgress;
            set { _downloadProgress = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusText)); }
        }

        public bool IsDownloading
        {
            get => _isDownloading;
            set { _isDownloading = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusText)); }
        }

        public long FileSizeBytes
        {
            get => _fileSizeBytes;
            set { _fileSizeBytes = value; OnPropertyChanged(); OnPropertyChanged(nameof(FileSizeDisplay)); }
        }

        public bool IsLocal
        {
            get => _isLocal;
            set { _isLocal = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusText)); }
        }

        public string StatusText
        {
            get
            {
                if (IsDownloading) return $"Downloading... {DownloadProgress}%";
                if (IsLocal) return "Lokal";
                if (IsInstalled) return "Installiert";
                return "Nicht installiert";
            }
        }

        public string FileSizeDisplay
        {
            get
            {
                if (FileSizeBytes <= 0) return "";
                if (FileSizeBytes < 1024) return $"{FileSizeBytes} B";
                if (FileSizeBytes < 1024 * 1024) return $"{FileSizeBytes / 1024.0:F1} KB";
                if (FileSizeBytes < 1024L * 1024 * 1024) return $"{FileSizeBytes / (1024.0 * 1024.0):F1} MB";
                return $"{FileSizeBytes / (1024.0 * 1024.0 * 1024.0):F1} GB";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
