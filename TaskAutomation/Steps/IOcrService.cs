using System.Drawing;
using TaskAutomation.Contracts.Geometry;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps;

public sealed record OcrRecognitionOptions(string Languages, OcrPageLayout PageLayout);

public sealed record OcrRecognitionWord(string Text, double Confidence, PixelRegion BoundingBox);

public sealed record OcrRecognitionLine(string Text, double Confidence, PixelRegion BoundingBox);

public sealed record OcrRecognition(
    string Text,
    double Confidence,
    IReadOnlyList<OcrRecognitionLine> Lines,
    IReadOnlyList<OcrRecognitionWord> Words);

/// <summary>Host-agnostic OCR service. Coordinates are always local to the supplied image.</summary>
public interface IOcrService : IDisposable
{
    Task<OcrRecognition> RecognizeAsync(
        Bitmap image,
        OcrRecognitionOptions options,
        CancellationToken cancellationToken);
}
