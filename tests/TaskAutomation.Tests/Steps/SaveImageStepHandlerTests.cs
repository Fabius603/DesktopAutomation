using System.Drawing;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Steps;

public sealed class SaveImageStepHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_SavesImageAndStoresCompleteResult()
    {
        using var temp = new TempDirectory();
        using var bitmap = new Bitmap(7, 5);
        bitmap.SetPixel(2, 3, Color.CornflowerBlue);
        var context = Context(bitmap);
        var step = Step(temp.Path, "capture.png");

        var result = Assert.IsType<SaveImageResult>(
            await new SaveImageStepHandler().ExecuteAsync(step, context, default));

        var expectedPath = Path.Combine(temp.Path, "capture.png");
        Assert.True(File.Exists(expectedPath));
        using var saved = new Bitmap(expectedPath);
        Assert.Equal(bitmap.Width, saved.Width);
        Assert.Equal(bitmap.Height, saved.Height);
        Assert.Equal(bitmap.GetPixel(2, 3).ToArgb(), saved.GetPixel(2, 3).ToArgb());
        Assert.Equal(expectedPath, result.FilePath);
        Assert.Equal("capture.png", result.FileName);
        Assert.Equal("PNG", result.Format);
        Assert.Equal(7, result.Width);
        Assert.Equal(5, result.Height);
        Assert.True(result.FileSizeBytes > 0);
        Assert.Same(result, context.Results.GetRaw(step.Id));
    }

    [Fact]
    public async Task ExecuteAsync_CreatesDirectoryAndReplacesExistingFile()
    {
        using var temp = new TempDirectory();
        var directory = Path.Combine(temp.Path, "nested");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "image.jpg"), "old");
        using var bitmap = new Bitmap(3, 2);
        var context = Context(bitmap);

        var result = Assert.IsType<SaveImageResult>(
            await new SaveImageStepHandler().ExecuteAsync(
                Step(directory, "image.jpg"), context, default));

        Assert.Equal("JPEG", result.Format);
        using var saved = new Bitmap(result.FilePath);
        Assert.Equal(3, saved.Width);
        Assert.Equal(2, saved.Height);
    }

    [Fact]
    public async Task ExecuteAsync_MissingImageOrUnsupportedFormatFailsWithoutStoredResult()
    {
        using var temp = new TempDirectory();
        var missingContext = new PipelineContextStub();
        var missing = Step(temp.Path, "image.png");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => new SaveImageStepHandler().ExecuteAsync(missing, missingContext, default));
        Assert.Null(missingContext.Results.GetRaw(missing.Id));

        using var bitmap = new Bitmap(2, 2);
        var invalidContext = Context(bitmap);
        var invalid = Step(temp.Path, "image.webp");
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => new SaveImageStepHandler().ExecuteAsync(invalid, invalidContext, default));
        Assert.Null(invalidContext.Results.GetRaw(invalid.Id));
    }

    private static PipelineContextStub Context(Bitmap bitmap)
    {
        var context = new PipelineContextStub();
        context.Results.Set<DesktopDuplicationStep>(new DesktopDuplicationResult
        {
            WasExecuted = true,
            Image = bitmap,
            Bounds = new Rectangle(0, 0, bitmap.Width, bitmap.Height)
        }, "capture");
        return context;
    }

    private static SaveImageStep Step(string path, string fileName) => new()
    {
        Id = Guid.NewGuid().ToString(),
        Settings = new()
        {
            SavePath = path,
            FileName = fileName,
            ImageSource = new()
            {
                SourceStepId = "capture",
                PropertyId = "image",
                PropertyPath = "Image"
            }
        }
    };

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), "DesktopAutomation.Tests", Guid.NewGuid().ToString("N"));

        public TempDirectory() => Directory.CreateDirectory(Path);

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, true);
        }
    }
}
