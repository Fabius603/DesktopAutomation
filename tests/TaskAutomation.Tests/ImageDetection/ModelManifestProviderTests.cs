using System.Text.Json;
using ImageDetection.YOLO;

namespace TaskAutomation.Tests.ImageDetection;

public sealed class ModelManifestProviderTests : IDisposable
{
    private readonly string _modelFolder = Path.Combine(
        Path.GetTempPath(),
        "DesktopAutomation.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void LoadManifest_AddsNewDefaultsWithoutReplacingExistingEntries()
    {
        Directory.CreateDirectory(_modelFolder);
        File.WriteAllText(
            Path.Combine(_modelFolder, "yolo-models.json"),
            """
            {
              "custom": {
                "url": "https://example.test/custom.onnx",
                "displayName": "Custom",
                "description": "Custom model"
              }
            }
            """);

        var manifest = ModelManifestProvider.LoadManifest(_modelFolder);

        Assert.Equal("https://example.test/custom.onnx", manifest.Models["custom"].Url);
        Assert.Contains("yolo11n", manifest.Models);
        Assert.Contains("screenparser", manifest.Models);
        Assert.Equal(0.1f, manifest.Models["screenparser"].RecommendedConfidenceThreshold);

        using var persisted = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(_modelFolder, "yolo-models.json")));
        Assert.True(persisted.RootElement.TryGetProperty("custom", out _));
        Assert.True(persisted.RootElement.TryGetProperty("yolo11n", out _));
        Assert.True(persisted.RootElement.TryGetProperty("screenparser", out _));
    }

    [Fact]
    public void LoadManifest_AddsRecommendedConfidenceToExistingDefaultModel()
    {
        Directory.CreateDirectory(_modelFolder);
        File.WriteAllText(
            Path.Combine(_modelFolder, "yolo-models.json"),
            """
            {
              "screenparser": {
                "url": "https://example.test/screenparser.onnx"
              }
            }
            """);

        var manifest = ModelManifestProvider.LoadManifest(_modelFolder);

        Assert.Equal(0.1f, manifest.Models["screenparser"].RecommendedConfidenceThreshold);
        Assert.Contains(
            "\"recommendedConfidenceThreshold\": 0.1",
            File.ReadAllText(Path.Combine(_modelFolder, "yolo-models.json")));
    }

    [Fact]
    public void LoadManifest_UpgradesKnownLegacyYoloDownloadWithoutReplacingCustomModels()
    {
        Directory.CreateDirectory(_modelFolder);
        File.WriteAllText(
            Path.Combine(_modelFolder, "yolo-models.json"),
            """
            {
              "yolo11n": {
                "url": "https://github.com/ultralytics/assets/releases/download/v8.4.0/yolo11n.onnx",
                "displayName": "My custom display name",
                "description": "My custom description"
              }
            }
            """);

        var manifest = ModelManifestProvider.LoadManifest(_modelFolder);
        var yolo = manifest.Models["yolo11n"];

        Assert.StartsWith(
            "https://raw.githubusercontent.com/Fabius603/DesktopAutomation/",
            yolo.Url);
        Assert.Equal(
            "634279b40c07c6391472c51ad45b81ebc48706a9a1fe72dd3396322acd0c053b",
            yolo.Sha256);
        Assert.Equal(10930182, yolo.Size);
        Assert.Equal("My custom display name", yolo.DisplayName);
        Assert.Equal("My custom description", yolo.Description);
    }

    public void Dispose()
    {
        if (Directory.Exists(_modelFolder))
            Directory.Delete(_modelFolder, recursive: true);
    }
}
