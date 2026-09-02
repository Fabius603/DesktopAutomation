namespace TaskAutomation.Jobs.ControlFlow;

public enum ControlFlowBlockKind
{
    Conditional
}

public enum ControlFlowMarkerRole
{
    Start,
    Section,
    End
}

/// <summary>
/// Describes the structural role of a persisted job step without coupling the model to a frontend.
/// </summary>
public interface IControlFlowMarker
{
    ControlFlowBlockKind BlockKind { get; }
    ControlFlowMarkerRole MarkerRole { get; }
}

public static class ControlFlowDiagnosticCodes
{
    public const string OrphanSection = "control_flow.orphan_section";
    public const string SectionAfterElse = "control_flow.section_after_else";
    public const string DuplicateElse = "control_flow.duplicate_else";
    public const string OrphanEnd = "control_flow.orphan_end";
    public const string MissingEnd = "control_flow.missing_end";
}

public sealed record ControlFlowDiagnostic(
    string Code,
    int StepIndex,
    JobStep Step);

public sealed record ControlFlowSection(
    int MarkerIndex,
    ControlFlowMarkerRole Role,
    JobStep Marker);

public sealed record ControlFlowBlock(
    ControlFlowBlockKind Kind,
    int StartIndex,
    int? EndIndex,
    int Depth,
    int? ParentStartIndex,
    IReadOnlyList<ControlFlowSection> Sections)
{
    public int LastIndex(int stepCount) => EndIndex ?? Math.Max(StartIndex, stepCount - 1);

    public bool Contains(int index, int stepCount)
        => index >= StartIndex && index <= LastIndex(stepCount);

    public int? FindSection(ControlFlowMarkerRole role)
        => Sections.FirstOrDefault(section => section.Role == role)?.MarkerIndex;

    public bool IsSectionEmpty(int markerIndex, int stepCount)
    {
        if (!Sections.Any(section => section.MarkerIndex == markerIndex)) return false;

        var nextBoundary = Sections
            .Where(section => section.MarkerIndex > markerIndex)
            .Select(section => section.MarkerIndex)
            .DefaultIfEmpty(EndIndex ?? Math.Max(markerIndex + 1, stepCount))
            .Min();
        return nextBoundary == markerIndex + 1;
    }
}

/// <summary>
/// Immutable structural projection shared by authoring, validation, and execution layers.
/// </summary>
public sealed class ControlFlowStructure
{
    private readonly IReadOnlyList<JobStep> _steps;

    internal ControlFlowStructure(
        IReadOnlyList<JobStep> steps,
        IReadOnlyList<ControlFlowBlock> blocks,
        IReadOnlyList<ControlFlowDiagnostic> diagnostics)
    {
        _steps = steps;
        Blocks = blocks;
        Diagnostics = diagnostics;
    }

    public IReadOnlyList<ControlFlowBlock> Blocks { get; }
    public IReadOnlyList<ControlFlowDiagnostic> Diagnostics { get; }
    public bool IsValid => Diagnostics.Count == 0;

    public ControlFlowBlock? GetBlockStartingAt(int index)
        => Blocks.FirstOrDefault(block => block.StartIndex == index);

    public ControlFlowBlock? GetBlockEndingAt(int index)
        => Blocks.FirstOrDefault(block => block.EndIndex == index);

    public ControlFlowBlock? GetOwningBlock(int index, ControlFlowBlockKind? kind = null)
    {
        return Blocks
            .Where(block => (kind is null || block.Kind == kind) && block.Contains(index, _steps.Count))
            .OrderByDescending(block => block.Depth)
            .ThenByDescending(block => block.StartIndex)
            .FirstOrDefault();
    }

    public int GetOpenBlockCountAt(int insertionIndex)
    {
        var clamped = Math.Clamp(insertionIndex, 0, _steps.Count);
        return Blocks.Count(block =>
            block.StartIndex < clamped
            && (block.EndIndex is null || block.EndIndex >= clamped));
    }
}
