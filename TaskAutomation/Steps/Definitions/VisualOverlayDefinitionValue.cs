using System.Text.Json;
using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

internal static class VisualOverlayDefinitionValue
{
    public static JsonNode? Write(VisualOverlaySettings? settings, ResultBinding? legacyDetections = null)
    {
        var normalized = Clone(settings ?? new VisualOverlaySettings());
        if (normalized.DetectionResults.Count == 0 && legacyDetections?.IsConfigured == true)
            normalized.DetectionResults.Add(Clone(legacyDetections));
        return JsonSerializer.SerializeToNode(normalized);
    }

    public static VisualOverlaySettings Read(StepDraft draft, string fieldId)
    {
        try
        {
            return draft.Values.GetValueOrDefault(fieldId)?.Deserialize<VisualOverlaySettings>()
                ?? new VisualOverlaySettings();
        }
        catch (JsonException) { return new VisualOverlaySettings(); }
        catch (InvalidOperationException) { return new VisualOverlaySettings(); }
    }

    public static bool IsValid(VisualOverlaySettings overlay, bool requireContent) =>
        (!requireContent || overlay.DetectionResults.Count > 0 || overlay.TextResults.Count > 0)
        && overlay.DetectionResults is not null
        && overlay.TextResults is not null
        && overlay.DetectionResults.All(binding => binding?.IsConfigured == true)
        && overlay.TextResults.All(entry =>
            entry is not null
            && entry.Id != Guid.Empty
            && entry.Result.IsConfigured
            && float.IsFinite(entry.FontSize) && entry.FontSize > 0
            && float.IsFinite(entry.Opacity) && entry.Opacity is >= 0 and <= 1
            && entry.DesktopIndex >= 0
            && entry.DurationMs >= 0);

    private static VisualOverlaySettings Clone(VisualOverlaySettings settings) => new()
    {
        DetectionResults = (settings.DetectionResults ?? []).Where(binding => binding is not null).Select(Clone).ToList(),
        TextResults = (settings.TextResults ?? []).Where(entry => entry is not null).Select(entry => new TextResultOverlaySettings
        {
            Id = entry.Id,
            Result = Clone(entry.Result),
            FontSize = entry.FontSize,
            FontColor = entry.FontColor,
            Opacity = entry.Opacity,
            DesktopIndex = entry.DesktopIndex,
            OffsetX = entry.OffsetX,
            OffsetY = entry.OffsetY,
            DurationMs = entry.DurationMs,
            ClearOnJobEnd = entry.ClearOnJobEnd
        }).ToList()
    };

    private static ResultBinding Clone(ResultBinding binding) => new()
    {
        ProviderId = binding.ProviderId,
        SourceId = binding.SourceId,
        LegacySourceStepId = binding.LegacySourceStepId,
        LegacyPropertyId = binding.LegacyPropertyId,
        LegacyPropertyPath = binding.LegacyPropertyPath
    };
}
