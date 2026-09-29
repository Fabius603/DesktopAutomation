using ImageDetection.YOLO;
using Microsoft.Extensions.Logging.Abstractions;

namespace TaskAutomation.Tests.ImageDetection;

public sealed class YoloModelDownloaderTests : IDisposable
{
    private readonly string _modelFolder = Path.Combine(
        Path.GetTempPath(),
        "DesktopAutomation.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task DownloadModelAsync_ReturnsLocallyImportedModelWithoutCatalogEntry()
    {
        Directory.CreateDirectory(_modelFolder);
        var modelPath = Path.Combine(_modelFolder, "custom-local-model.onnx");
        await File.WriteAllBytesAsync(modelPath, [1, 2, 3, 4]);
        await File.WriteAllLinesAsync(
            Path.Combine(_modelFolder, "custom-local-model.labels.txt"),
            ["Button", "Checkbox"]);

        var downloader = new YOLOModelDownloader(
            NullLogger<YOLOModelDownloader>.Instance,
            modelFolderPath: _modelFolder);

        var model = await downloader.DownloadModelAsync("custom-local-model");

        Assert.Equal("custom-local-model", model.Id);
        Assert.Equal(modelPath, model.OnnxPath);
        Assert.Equal(4, model.OnnxSizeBytes);
    }

    [Fact]
    public async Task DownloadModelAsync_RejectsUnknownModelThatIsNotInstalled()
    {
        var downloader = new YOLOModelDownloader(
            NullLogger<YOLOModelDownloader>.Instance,
            modelFolderPath: _modelFolder);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            downloader.DownloadModelAsync("missing"));

        Assert.Contains("nicht lokal installiert", exception.Message);
    }

    public void Dispose()
    {
        if (Directory.Exists(_modelFolder))
            Directory.Delete(_modelFolder, recursive: true);
    }
}
