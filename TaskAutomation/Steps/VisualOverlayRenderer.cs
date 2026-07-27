using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using Microsoft.Extensions.Logging;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps;

public sealed record ResolvedTextOverlay(
    Guid Id,
    string Text,
    float FontSize,
    Color Color,
    int DesktopIndex,
    int OffsetX,
    int OffsetY,
    int DurationMs,
    bool ClearOnJobEnd);

public sealed record ResolvedVisualOverlay(
    IReadOnlyList<IReadOnlyList<DetectionItem>> DetectionGroups,
    IReadOnlyList<ResolvedTextOverlay> Texts)
{
    public bool HasContent => DetectionGroups.Any(group => group.Count > 0)
                              || Texts.Any(text => !string.IsNullOrEmpty(text.Text));
}

public static class VisualOverlayResolver
{
    public static ResolvedVisualOverlay Resolve(
        IJobResultStore results,
        VisualOverlaySettings? settings,
        ResultBinding? legacyDetections,
        ILogger logger)
    {
        settings ??= new VisualOverlaySettings();
        var detectionBindings = settings.DetectionResults.Count > 0
            ? settings.DetectionResults
            : legacyDetections?.IsConfigured == true ? [legacyDetections] : [];
        var detectionGroups = new List<IReadOnlyList<DetectionItem>>();
        foreach (var binding in detectionBindings.Where(binding => binding.IsConfigured))
        {
            var resolved = ResultBindingResolver.ResolveDetections(results, binding);
            if (resolved.IsSuccess)
                detectionGroups.Add(resolved.Values);
            else
                logger.LogWarning(
                    "Overlay: Erkennungsergebnis {SourceStepId}/{PropertyPath} ist nicht verfügbar und wird übersprungen.",
                    binding.SourceStepId, binding.PropertyPath);
        }

        var texts = new List<ResolvedTextOverlay>();
        foreach (var entry in settings.TextResults.Where(entry => entry.Result.IsConfigured))
        {
            var resolved = ResultBindingResolver.Resolve<object>(results, entry.Result);
            if (!resolved.IsSuccess || resolved.Values.Count == 0)
            {
                logger.LogWarning(
                    "Overlay: Textergebnis {SourceStepId}/{PropertyPath} ist nicht verfügbar und wird übersprungen.",
                    entry.Result.SourceStepId, entry.Result.PropertyPath);
                continue;
            }

            var text = string.Join(Environment.NewLine, resolved.Values.Select(ResultDisplayFormatter.Format));
            if (string.IsNullOrEmpty(text)) continue;
            texts.Add(new ResolvedTextOverlay(
                entry.Id == Guid.Empty ? Guid.NewGuid() : entry.Id,
                text,
                entry.FontSize,
                ParseColor(entry.FontColor, entry.Opacity),
                entry.DesktopIndex,
                entry.OffsetX,
                entry.OffsetY,
                entry.DurationMs,
                entry.ClearOnJobEnd));
        }

        return new ResolvedVisualOverlay(detectionGroups, texts);
    }

    private static Color ParseColor(string value, float opacity)
    {
        var color = ColorTranslator.FromHtml(string.IsNullOrWhiteSpace(value) ? "#FFFFFF" : value);
        var alpha = (byte)Math.Clamp(Math.Round(opacity * 255), 0, 255);
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }
}

public static class ResultDisplayFormatter
{
    public static string Format(object? value) => value switch
    {
        null => string.Empty,
        bool boolean => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "de"
            ? boolean ? "Ja" : "Nein"
            : boolean ? "Yes" : "No",
        DateTime dateTime => dateTime.ToString(CultureInfo.CurrentCulture),
        DateTimeOffset dateTimeOffset => dateTimeOffset.ToString(CultureInfo.CurrentCulture),
        Point point => $"X={point.X}, Y={point.Y}",
        Rectangle rectangle =>
            $"X={rectangle.X}, Y={rectangle.Y}, Breite={rectangle.Width}, Höhe={rectangle.Height}",
        DetectionItem detection => detection.BoundingBox is { } bounds
            ? $"{detection.Confidence:P1} ({bounds.X}, {bounds.Y}, {bounds.Width}, {bounds.Height})"
            : $"{detection.Confidence:P1} (X={detection.Center.X}, Y={detection.Center.Y})",
        IFormattable formattable => formattable.ToString(null, CultureInfo.CurrentCulture) ?? string.Empty,
        _ => value.ToString() ?? string.Empty
    };
}

public static class VisualOverlayRenderer
{
    public static Bitmap Draw(Bitmap source, Point captureOffset, ResolvedVisualOverlay overlay)
    {
        var result = (Bitmap)source.Clone();
        using var graphics = Graphics.FromImage(result);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

        for (var groupIndex = 0; groupIndex < overlay.DetectionGroups.Count; groupIndex++)
        {
            for (var itemIndex = 0; itemIndex < overlay.DetectionGroups[groupIndex].Count; itemIndex++)
            {
                var detection = overlay.DetectionGroups[groupIndex][itemIndex];
                var color = itemIndex == 0 ? Color.OrangeRed : Color.LimeGreen;
                using var pen = new Pen(color, 2);
                using var brush = new SolidBrush(color);
                if (detection.BoundingBox is { } bounds)
                {
                    bounds.Offset(-captureOffset.X, -captureOffset.Y);
                    graphics.DrawRectangle(pen, bounds);
                }
                var center = new Point(
                    detection.Center.X - captureOffset.X,
                    detection.Center.Y - captureOffset.Y);
                graphics.FillEllipse(brush, center.X - 4, center.Y - 4, 8, 8);
            }
        }

        foreach (var text in overlay.Texts)
        {
            using var font = new Font(FontFamily.GenericSansSerif, text.FontSize, FontStyle.Regular, GraphicsUnit.Point);
            using var brush = new SolidBrush(text.Color);
            graphics.DrawString(text.Text, font, brush, text.OffsetX, text.OffsetY);
        }

        return result;
    }
}
