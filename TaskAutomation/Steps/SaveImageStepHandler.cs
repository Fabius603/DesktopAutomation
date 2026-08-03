using System.Drawing.Imaging;
using System.IO;
using ImageCapture.Video;
using Microsoft.Extensions.Logging;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps;

public sealed class SaveImageStepHandler : JobStepHandler<SaveImageStep, SaveImageResult>
{
    protected override Task<SaveImageResult> ExecuteCoreAsync(
        SaveImageStep step,
        IStepPipelineContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var imageInput = ResultBindingResolver.ResolveCapture(context.Results, step.Settings.ImageSource);
        var image = imageInput.Image;
        if (image is null)
            throw new InvalidOperationException(
                imageInput.Resolution.Error ?? "Die ausgewählte Bildquelle enthält kein Bild.");

        if (string.IsNullOrWhiteSpace(step.Settings.SavePath))
            throw new InvalidOperationException("Es wurde kein Speicherordner angegeben.");
        if (string.IsNullOrWhiteSpace(step.Settings.FileName)
            || step.Settings.FileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || !string.Equals(
                Path.GetFileName(step.Settings.FileName),
                step.Settings.FileName,
                StringComparison.Ordinal))
            throw new InvalidOperationException("Der Dateiname ist ungültig.");

        var directory = Path.GetFullPath(
            Environment.ExpandEnvironmentVariables(step.Settings.SavePath.Trim()));
        var fileName = step.Settings.FileName.Trim();
        var (format, formatName) = ResolveFormat(fileName);
        Directory.CreateDirectory(directory);

        var basePath = Path.Combine(directory, fileName);
        var targetPath = VideoHelper.GetUniqueFilePath(basePath);
        var temporaryPath = Path.Combine(directory, $".{fileName}.{Guid.NewGuid():N}.tmp");
        var overlay = VisualOverlayResolver.Resolve(
            context.Results, step.Settings.Overlay, null, context.Logger);
        using var renderedImage = overlay.HasContent
            ? VisualOverlayRenderer.Draw(image, imageInput.Capture.Offset, overlay)
            : null;
        var imageToSave = renderedImage ?? image;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            using (var stream = new FileStream(
                       temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                imageToSave.Save(stream, format);
                stream.Flush(true);
            }

            cancellationToken.ThrowIfCancellationRequested();
            while (true)
            {
                try
                {
                    File.Move(temporaryPath, targetPath, false);
                    break;
                }
                catch (IOException) when (File.Exists(targetPath))
                {
                    targetPath = VideoHelper.GetUniqueFilePath(basePath);
                }
            }
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }

        var fileSize = new FileInfo(targetPath).Length;
        context.Logger.LogInformation(
            "SaveImageStepHandler: Bild als {Format} mit {Width}x{Height} unter {Path} gespeichert.",
            formatName, imageToSave.Width, imageToSave.Height, targetPath);

        return Task.FromResult(new SaveImageResult
        {
            WasExecuted = true,
            FilePath = targetPath,
            FileName = Path.GetFileName(targetPath),
            Format = formatName,
            Width = imageToSave.Width,
            Height = imageToSave.Height,
            FileSizeBytes = fileSize,
            SavedAtUtc = DateTime.UtcNow
        });
    }

    private static (ImageFormat Format, string Name) ResolveFormat(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".png" => (ImageFormat.Png, "PNG"),
            ".jpg" or ".jpeg" => (ImageFormat.Jpeg, "JPEG"),
            ".bmp" => (ImageFormat.Bmp, "BMP"),
            ".gif" => (ImageFormat.Gif, "GIF"),
            ".tif" or ".tiff" => (ImageFormat.Tiff, "TIFF"),
            _ => throw new InvalidOperationException(
                "Das Bildformat wird nicht unterstützt. Erlaubt sind PNG, JPEG, BMP, GIF und TIFF.")
        };

    protected override SaveImageResult CreateDefault() => SaveImageResult.Default;
}
