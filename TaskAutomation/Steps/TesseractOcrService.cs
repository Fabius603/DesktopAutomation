using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tesseract;
using TaskAutomation.Contracts.Geometry;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps;

/// <summary>Serializes access to one reusable native Tesseract engine per language combination.</summary>
public sealed class TesseractOcrService(ILogger<TesseractOcrService> logger) : IOcrService
{
    private readonly ConcurrentDictionary<string, EngineEntry> _engines =
        new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    public async Task<OcrRecognition> RecognizeAsync(
        Bitmap image,
        OcrRecognitionOptions options,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(image);
        cancellationToken.ThrowIfCancellationRequested();

        var languages = NormalizeLanguages(options.Languages);
        var entry = _engines.GetOrAdd(languages, CreateEngine);
        await entry.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var imageStream = new MemoryStream();
            image.Save(imageStream, System.Drawing.Imaging.ImageFormat.Png);
            using var pix = Pix.LoadFromMemory(imageStream.ToArray());
            using var page = entry.Engine.Process(pix, ToPageSegMode(options.PageLayout));
            var text = page.GetText().Trim();
            var lines = ReadLines(page);
            var words = ReadWords(page);
            return new OcrRecognition(text, page.GetMeanConfidence() / 100d, lines, words);
        }
        finally
        {
            entry.Gate.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var entry in _engines.Values)
        {
            entry.Engine.Dispose();
            entry.Gate.Dispose();
        }
        _engines.Clear();
    }

    private EngineEntry CreateEngine(string languages)
    {
        var dataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");
        if (!Directory.Exists(dataPath))
            throw new DirectoryNotFoundException($"Tesseract language data was not found at '{dataPath}'.");

        logger.LogInformation("Initialisiere Tesseract OCR fuer {Languages}.", languages);
        return new EngineEntry(
            new TesseractEngine(dataPath, languages, Tesseract.EngineMode.LstmOnly),
            new SemaphoreSlim(1, 1));
    }

    private static IReadOnlyList<OcrRecognitionLine> ReadLines(Page page) =>
        ReadElements(page, PageIteratorLevel.TextLine)
            .Select(element => new OcrRecognitionLine(element.Text, element.Confidence, element.BoundingBox))
            .ToArray();

    private static IReadOnlyList<OcrRecognitionWord> ReadWords(Page page) =>
        ReadElements(page, PageIteratorLevel.Word)
            .Select(element => new OcrRecognitionWord(element.Text, element.Confidence, element.BoundingBox))
            .ToArray();

    private static IReadOnlyList<OcrElement> ReadElements(Page page, PageIteratorLevel level)
    {
        using var iterator = page.GetIterator();
        if (iterator is null) return [];

        var elements = new List<OcrElement>();
        iterator.Begin();
        do
        {
            var text = iterator.GetText(level)?.Trim();
            if (string.IsNullOrWhiteSpace(text)
                || !iterator.TryGetBoundingBox(level, out var bounds))
                continue;

            elements.Add(new OcrElement(
                text,
                iterator.GetConfidence(level) / 100d,
                new PixelRegion(bounds.X1, bounds.Y1, bounds.Width, bounds.Height)));
        }
        while (iterator.Next(level));

        return elements;
    }

    private static string NormalizeLanguages(string languages)
    {
        var normalized = string.Join('+', languages.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase));
        return string.IsNullOrEmpty(normalized) ? "deu+eng" : normalized;
    }

    private static PageSegMode ToPageSegMode(OcrPageLayout layout) => layout switch
    {
        OcrPageLayout.TextBlock => PageSegMode.SingleBlock,
        OcrPageLayout.TextLine => PageSegMode.SingleLine,
        OcrPageLayout.SingleWord => PageSegMode.SingleWord,
        OcrPageLayout.SparseText => PageSegMode.SparseText,
        _ => PageSegMode.Auto
    };

    private sealed record EngineEntry(TesseractEngine Engine, SemaphoreSlim Gate);
    private sealed record OcrElement(string Text, double Confidence, PixelRegion BoundingBox);
}
