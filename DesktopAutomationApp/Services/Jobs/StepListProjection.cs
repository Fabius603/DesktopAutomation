using System.Collections;
using System.Runtime.CompilerServices;
using TaskAutomation.Jobs;
using TaskAutomation.Jobs.ControlFlow;

namespace DesktopAutomationApp.Services.Jobs;

/// <summary>One presentation projection shared by cards, contours and breadcrumbs.</summary>
internal sealed class StepListProjection
{
    public const double GutterWidth = 64;
    public const double Indentation = 44;
    public const double RightInset = 16;
    private static readonly ConditionalWeakTable<IList, Cache> Caches = new();
    private sealed class Cache { public int Version = int.MinValue; public StepListProjection? Projection; }
    public JobStep[] Steps { get; }
    public ControlFlowStructure Structure { get; }
    public int MaxDepth { get; }
    private StepListProjection(IList items)
    {
        Steps = items.Cast<object>().OfType<JobStep>().ToArray();
        Structure = ControlFlowStructureAnalyzer.Analyze(Steps);
        MaxDepth = Structure.Blocks.Select(block => block.Depth + 1).DefaultIfEmpty(0).Max();
    }
    public static StepListProjection Get(IList items, int version)
    {
        var cache = Caches.GetOrCreateValue(items);
        if (cache.Projection is null || cache.Version != version)
        { cache.Projection = new StepListProjection(items); cache.Version = version; }
        return cache.Projection;
    }
    public int Depth(int index)
    {
        var block = Structure.GetOwningBlock(index);
        if (block is null) return 0;
        return block.Depth + (Steps[index] is IControlFlowMarker ? 0 : 1);
    }
    public IEnumerable<string> CollapsedOwners(int index, IReadOnlyCollection<string> collapsed)
    {
        foreach (var block in Structure.Blocks.Where(block => block.StartIndex < index && block.Contains(index, Steps.Length)))
        {
            if (collapsed.Contains(Steps[block.StartIndex].Id)) yield return Steps[block.StartIndex].Id;
            foreach (var section in block.Sections.Where(section => section.Role == ControlFlowMarkerRole.Section))
                if (section.MarkerIndex < index && index < block.SectionEndExclusive(section.MarkerIndex, Steps.Length)
                    && collapsed.Contains(section.Marker.Id)) yield return section.Marker.Id;
        }
    }
    public bool IsHidden(int index, IReadOnlyCollection<string> collapsed) => CollapsedOwners(index, collapsed).Any();
    public bool CanCollapse(int index) => Steps[index] is IfStep or ElseIfStep or ElseStep
        && Structure.GetOwningBlock(index) is { EndIndex: not null } block
        && block.Sections.Any(section => section.MarkerIndex == index);
    public bool IsEmptyBranch(int index) => Steps[index] is IfStep or ElseIfStep or ElseStep
        && Structure.GetOwningBlock(index)?.IsSectionEmpty(index, Steps.Length) == true;
    public IReadOnlyList<ControlFlowBlock> ClosuresAfter(int index, IReadOnlyCollection<string> collapsed)
    {
        var next = Enumerable.Range(index + 1, Steps.Length - index - 1)
            .FirstOrDefault(candidate => Steps[candidate] is not EndIfStep && !IsHidden(candidate, collapsed), Steps.Length);
        return Structure.Blocks.Where(block => block.EndIndex > index && block.EndIndex < next
            && !collapsed.Contains(Steps[block.StartIndex].Id) && !IsHidden(block.StartIndex, collapsed))
            .OrderBy(block => block.EndIndex).ToArray();
    }
    public JobStep? VisibleOwner(JobStep step)
    {
        var index = Array.IndexOf(Steps, step);
        if (index < 0 || step is not EndIfStep) return step;
        var block = Structure.GetBlockEndingAt(index);
        return block is not null ? Steps[block.StartIndex]
            : Steps.Take(index).LastOrDefault(candidate => candidate is not EndIfStep);
    }
    public double Width(int depth, double viewport) => Math.Max(320 + (MaxDepth - depth) * (Indentation + RightInset),
        viewport - GutterWidth - 52 - depth * (Indentation + RightInset));
}
