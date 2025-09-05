using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
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

        public YoloDownloadsViewModel(IYOLOModelDownloader downloader, ILogger<YoloDownloadsViewModel> logger)
        {
            _downloader = downloader;
            _logger = logger;

            RefreshCommand = new RelayCommand(async () => await RefreshModelsAsync());
            DownloadCommand = new RelayCommand<YoloModelEntry>(async model => await DownloadModelAsync(model), CanDownload);
            UninstallCommand = new RelayCommand<YoloModelEntry>(async model => await UninstallModelAsync(model), CanUninstall);

            // Event für Progress-Updates
            _downloader.DownloadProgressChanged += OnDownloadProgressChanged;

            // Initial laden
            _ = Task.Run(async () => await RefreshModelsAsync());
        }

        private async Task RefreshModelsAsync()
        {
            try
            {
                var manifest = ModelManifestProvider.LoadManifest();
                var existingModels = Models.ToList();
                
                Models.Clear();

                foreach (var kvp in manifest.Models)
                {
                    var modelKey = kvp.Key;
                    var manifestEntry = kvp.Value;
                    var onnxPath = Path.Combine(_downloader.ModelFolderPath, modelKey + ".onnx");
                    var isInstalled = File.Exists(onnxPath);
                    
                    var fileSize = 0L;
                    if (isInstalled)
                    {
                        try
                        {
                            fileSize = new FileInfo(onnxPath).Length;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Fehler beim Ermitteln der Dateigröße für {modelKey}", modelKey);
                        }
                    }

                    // Bestehenden Eintrag wiederverwenden (um Progress zu behalten)
                    var existingEntry = existingModels.FirstOrDefault(e => e.ModelKey == modelKey);
                    if (existingEntry != null)
                    {
                        existingEntry.IsInstalled = isInstalled;
                        existingEntry.FileSizeBytes = fileSize;
                        Models.Add(existingEntry);
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
                            DownloadProgress = 0,
                            IsDownloading = false
                        });
                    }
                }

                _logger.LogInformation("Modelliste aktualisiert: {count} Modelle gefunden.", Models.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Aktualisieren der Modelliste.");
            }
        }

        private async Task DownloadModelAsync(YoloModelEntry? model)
        {
            if (model == null || model.IsInstalled || model.IsDownloading)
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
                await RefreshModelsAsync(); // Status aktualisieren

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
                    await RefreshModelsAsync(); // Status aktualisieren
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
                
                if (e.Status == ModelDownloadStatus.Completed)
                {
                    model.IsDownloading = false;
                    // RefreshModelsAsync wird bereits in DownloadModelAsync aufgerufen
                }
            }
        }

        private bool CanDownload(YoloModelEntry? model)
        {
            return model != null && !model.IsInstalled && !model.IsDownloading;
        }

        private bool CanUninstall(YoloModelEntry? model)
        {
            return model != null && model.IsInstalled && !model.IsDownloading;
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
            set { _downloadProgress = value; OnPropertyChanged(); }
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

        public string StatusText
        {
            get
            {
                if (IsDownloading)
                    return $"Downloading... {DownloadProgress}%";
                if (IsInstalled)
                    return "Installed";
                return "Not installed";
            }
        }

        public string FileSizeDisplay
        {
            get
            {
                if (FileSizeBytes == 0)
                    return "";
                
                if (FileSizeBytes < 1024)
                    return $"{FileSizeBytes} B";
                if (FileSizeBytes < 1024 * 1024)
                    return $"{FileSizeBytes / 1024.0:F1} KB";
                if (FileSizeBytes < 1024 * 1024 * 1024)
                    return $"{FileSizeBytes / (1024.0 * 1024.0):F1} MB";
                
                return $"{FileSizeBytes / (1024.0 * 1024.0 * 1024.0):F1} GB";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
