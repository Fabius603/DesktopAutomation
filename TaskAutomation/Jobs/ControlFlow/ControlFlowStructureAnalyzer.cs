namespace TaskAutomation.Jobs.ControlFlow;

/// <summary>
/// Builds the single shared interpretation of control-flow markers in a flat step list.
/// </summary>
public static class ControlFlowStructureAnalyzer
{
    private sealed class Frame(
        ControlFlowBlockKind kind,
        int startIndex,
        int depth,
        int? parentStartIndex)
    {
        public ControlFlowBlockKind Kind { get; } = kind;
        public int StartIndex { get; } = startIndex;
        public int Depth { get; } = depth;
        public int? ParentStartIndex { get; } = parentStartIndex;
        public bool SeenElse { get; set; }
        public List<ControlFlowSection> Sections { get; } = [];
    }

    public static ControlFlowStructure Analyze(IReadOnlyList<JobStep> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);

        var diagnostics = new List<ControlFlowDiagnostic>();
        var completedBlocks = new List<ControlFlowBlock>();
        var openBlocks = new Stack<Frame>();

        for (var index = 0; index < steps.Count; index++)
        {
            var step = steps[index];
            if (step is not IControlFlowMarker marker) continue;

            switch (marker.MarkerRole)
            {
                case ControlFlowMarkerRole.Start:
                    var frame = new Frame(
                        marker.BlockKind,
                        index,
                        openBlocks.Count,
                        openBlocks.TryPeek(out var parent) ? parent.StartIndex : null);
                    frame.Sections.Add(new(index, ControlFlowMarkerRole.Start, step));
                    openBlocks.Push(frame);
                    break;

                case ControlFlowMarkerRole.Section:
                    if (!openBlocks.TryPeek(out var sectionBlock)
                        || sectionBlock.Kind != marker.BlockKind)
                    {
                        diagnostics.Add(new(ControlFlowDiagnosticCodes.OrphanSection, index, step));
                        break;
                    }

                    if (step is ElseIfStep && sectionBlock.SeenElse)
                    {
                        diagnostics.Add(new(ControlFlowDiagnosticCodes.SectionAfterElse, index, step));
                    }
                    else if (step is ElseStep)
                    {
                        if (sectionBlock.SeenElse)
                        {
                            diagnostics.Add(new(ControlFlowDiagnosticCodes.DuplicateElse, index, step));
                        }
                        else
                        {
                            sectionBlock.SeenElse = true;
                        }
                    }

                    sectionBlock.Sections.Add(new(index, ControlFlowMarkerRole.Section, step));
                    break;

                case ControlFlowMarkerRole.End:
                    if (!openBlocks.TryPeek(out var completed)
                        || completed.Kind != marker.BlockKind)
                    {
                        diagnostics.Add(new(ControlFlowDiagnosticCodes.OrphanEnd, index, step));
                        break;
                    }

                    openBlocks.Pop();
                    completedBlocks.Add(new(
                        completed.Kind,
                        completed.StartIndex,
                        index,
                        completed.Depth,
                        completed.ParentStartIndex,
                        completed.Sections.ToArray()));
                    break;
            }
        }

        foreach (var open in openBlocks)
        {
            diagnostics.Add(new(
                ControlFlowDiagnosticCodes.MissingEnd,
                open.StartIndex,
                steps[open.StartIndex]));
            completedBlocks.Add(new(
                open.Kind,
                open.StartIndex,
                null,
                open.Depth,
                open.ParentStartIndex,
                open.Sections.ToArray()));
        }

        return new ControlFlowStructure(
            steps,
            completedBlocks.OrderBy(block => block.StartIndex).ToArray(),
            diagnostics.ToArray());
    }
}
