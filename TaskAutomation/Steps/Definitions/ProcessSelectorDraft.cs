using System.Text.Json;
using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

internal static class ProcessSelectorDraft
{
    public static JsonNode? Create(ProcessTargetSettings target) =>
        JsonSerializer.SerializeToNode(new StepProcessSelectorValue(
            JsonSerializer.SerializeToNode(target.ProcessSource),
            target.ProcessName,
            target.ExecutablePath,
            target.WindowTitleContains));

    public static bool TryRead(
        StepDraft draft,
        string fieldId,
        out StepProcessSelectorValue selector,
        out ResultBinding binding)
    {
        selector = new StepProcessSelectorValue(null, string.Empty, string.Empty, string.Empty);
        binding = new ResultBinding();
        if (!draft.Values.TryGetValue(fieldId, out var value) || value is null)
            return false;
        try
        {
            selector = value.Deserialize<StepProcessSelectorValue>() ?? selector;
            binding = selector.ProcessSource?.Deserialize<ResultBinding>() ?? new ResultBinding();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static bool IsConfigured(StepDraft draft, string fieldId) =>
        TryRead(draft, fieldId, out var selector, out var binding)
        && (binding.IsConfigured
            || !string.IsNullOrWhiteSpace(selector.ProcessName)
            || !string.IsNullOrWhiteSpace(selector.ExecutablePath));

    public static void Apply(StepDraft draft, string fieldId, ProcessTargetSettings target)
    {
        if (!TryRead(draft, fieldId, out var selector, out var binding))
            throw new InvalidOperationException("The process selector draft is invalid.");
        target.ProcessSource = binding;
        target.ProcessName = binding.IsConfigured ? string.Empty : selector.ProcessName;
        target.ExecutablePath = binding.IsConfigured ? string.Empty : selector.ExecutablePath;
        target.WindowTitleContains = binding.IsConfigured ? string.Empty : selector.WindowTitleContains;
    }
}
