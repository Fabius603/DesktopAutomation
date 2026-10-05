namespace TaskAutomation.Jobs.ControlFlow;

public sealed record ControlFlowInsertionTarget(int Index, int Depth);

/// <summary>Structural editing units for the persisted, flat step sequence.</summary>
public static class ControlFlowEditRules
{
    /// <summary>Add after the focused row, or append when there is no focused row.</summary>
    public static int ResolveAddInsertionIndex(IReadOnlyList<JobStep> steps, int focusedIndex = -1)
        => focusedIndex >= 0 && focusedIndex < steps.Count ? focusedIndex + 1 : steps.Count;

    /// <summary>Alternative branches belong to the nearest complete conditional, before Else or its end.</summary>
    public static int? ResolveBranchInsertionIndex(IReadOnlyList<JobStep> steps, int anchorIndex, bool elseIf)
    {
        if (anchorIndex < 0 || anchorIndex >= steps.Count) return null;
        var structure = ControlFlowStructureAnalyzer.Analyze(steps);
        var block = structure.GetOwningBlock(anchorIndex, ControlFlowBlockKind.Conditional);
        if (block?.EndIndex is not int end) return null;
        if (structure.Diagnostics.Any(diagnostic => diagnostic.StepIndex >= block.StartIndex
            && diagnostic.StepIndex <= end)) return null;
        var existingElse = block.Sections.FirstOrDefault(section => section.Marker is ElseStep);
        return elseIf ? existingElse?.MarkerIndex ?? end : existingElse is null ? end : null;
    }

    /// <summary>Resolve a visible row edge to a persisted insertion slot. Outdenting crosses only closing markers.</summary>
    public static ControlFlowInsertionTarget ResolveInsertionTarget(IReadOnlyList<JobStep> steps,
        int hoveredIndex, bool insertAfter, int requestedDepth, bool afterWholeBlock = false)
    {
        var structure = ControlFlowStructureAnalyzer.Analyze(steps);
        var index = Math.Clamp(hoveredIndex + (insertAfter ? 1 : 0), 0, steps.Count);
        if (afterWholeBlock && structure.GetBlockStartingAt(hoveredIndex)?.EndIndex is int end)
            index = end + 1;
        else if (afterWholeBlock && structure.GetOwningBlock(hoveredIndex) is { } branchBlock
                 && branchBlock.SectionEndExclusive(hoveredIndex, steps.Count) is int boundary)
            index = boundary;
        var target = new ControlFlowInsertionTarget(index, structure.GetOpenBlockCountAt(index));
        while (target.Depth > Math.Max(0, requestedDepth) && index < steps.Count
               && structure.GetBlockEndingAt(index) is not null)
        {
            index++;
            target = new ControlFlowInsertionTarget(index, structure.GetOpenBlockCountAt(index));
        }
        return target;
    }

    public static bool TryMove(IReadOnlyList<JobStep> source, IReadOnlyList<JobStep> target,
        IEnumerable<int> selection, int targetIndex, out List<JobStep> remaining, out List<JobStep> reordered)
    {
        var indices = ExpandSelection(source, selection);
        remaining = source.ToList();
        reordered = target.ToList();
        if (indices.Count == 0 || targetIndex < 0 || targetIndex > target.Count) return false;
        var same = ReferenceEquals(source, target);
        if (same && indices.Contains(targetIndex)) return false;
        var moving = indices.Select(index => source[index]).ToArray();
        var selected = indices.ToHashSet();
        remaining = source.Where((_, index) => !selected.Contains(index)).ToList();
        reordered = same ? remaining : target.ToList();
        var insert = targetIndex - (same ? indices.Count(index => index < targetIndex) : 0);
        reordered.InsertRange(insert, moving);
        return (same || ControlFlowStructureAnalyzer.Analyze(remaining).IsValid)
            && ControlFlowStructureAnalyzer.Analyze(reordered).IsValid
            && (!same || !source.SequenceEqual(reordered));
    }

    public static IReadOnlyList<StepValidationResult> IntroducedValidationErrors(Job before, Job after,
        IReadOnlyList<ValueProviderSourceDescriptor>? providers = null)
    {
        var known = JobValidation.ValidateJob(before, providers).Steps.GroupBy(result => result.Step.Id).ToDictionary(group => group.Key,
            group => group.SelectMany(result => result.Errors ?? (result.Error is null ? [] : new[] { result.Error })).ToHashSet());
        return JobValidation.ValidateJob(after, providers).Steps.Where(result => !result.IsValid)
            .Select(result => new StepValidationResult(result.Step, false, result.Error,
                (result.Errors ?? (result.Error is null ? [] : new[] { result.Error }))
                .Where(error => !known.TryGetValue(result.Step.Id, out var existing) || !existing.Contains(error)).ToArray()))
            .Where(result => result.Errors is { Count: > 0 }).ToArray();
    }

    public static IReadOnlyList<int> ExpandSelection(IReadOnlyList<JobStep> steps, IEnumerable<int> selected)
    {
        var structure = ControlFlowStructureAnalyzer.Analyze(steps);
        var indices = selected.Where(index => index >= 0 && index < steps.Count).ToHashSet();
        foreach (var index in indices.ToArray())
        {
            if (steps[index] is not IControlFlowMarker) continue;
            var block = structure.GetOwningBlock(index);
            // Never infer a closing marker for a damaged job.
            if (block?.EndIndex is not int end) continue;
            for (var current = block.StartIndex; current <= end; current++) indices.Add(current);
        }
        return indices.Order().ToArray();
    }

    /// <summary>Delete whole blocks, but only the selected alternative branch and its contents.</summary>
    public static IReadOnlyList<int> ExpandDeletionSelection(IReadOnlyList<JobStep> steps, IEnumerable<int> selected)
    {
        var structure = ControlFlowStructureAnalyzer.Analyze(steps);
        var indices = selected.Where(index => index >= 0 && index < steps.Count).ToHashSet();
        foreach (var index in indices.ToArray())
        {
            if (steps[index] is not IControlFlowMarker marker) continue;
            var block = structure.GetOwningBlock(index);
            // Damaged or orphaned markers never imply deleting unrelated following steps.
            if (block?.EndIndex is not int end) continue;
            var first = block.StartIndex;
            var last = end;
            if (marker.MarkerRole == ControlFlowMarkerRole.Section)
            {
                if (!block.Sections.Any(section => section.MarkerIndex == index)) continue;
                first = index;
                last = block.SectionEndExclusive(index, steps.Count)!.Value - 1;
            }
            for (var current = first; current <= last; current++) indices.Add(current);
        }
        return indices.Order().ToArray();
    }

    public static IReadOnlyList<int> RemoveConditionMarkers(IReadOnlyList<JobStep> steps, int index)
    {
        var block = ControlFlowStructureAnalyzer.Analyze(steps).GetOwningBlock(index);
        if (block?.EndIndex is not int end) return [];
        // Sections belong to this block only. Nested conditions retain all their markers.
        return block.Sections.Select(section => section.MarkerIndex).Append(end).Distinct().Order().ToArray();
    }
}
