using TaskAutomation.Jobs;
using TaskAutomation.Jobs.ControlFlow;

namespace TaskAutomation.Tests.Jobs;

public sealed class ControlFlowStructureAnalyzerTests
{
    [Fact]
    public void Analyze_ProjectsConditionalBlockAndSections()
    {
        JobStep[] steps =
        [
            new IfStep(),
            new TimeoutStep(),
            new ElseIfStep(),
            new ElseStep(),
            new EndIfStep()
        ];

        var structure = ControlFlowStructureAnalyzer.Analyze(steps);

        Assert.True(structure.IsValid);
        var block = Assert.Single(structure.Blocks);
        Assert.Equal(ControlFlowBlockKind.Conditional, block.Kind);
        Assert.Equal(0, block.StartIndex);
        Assert.Equal(4, block.EndIndex);
        Assert.Equal([0, 2, 3], block.Sections.Select(section => section.MarkerIndex));
        Assert.Equal(0, structure.GetOwningBlock(1)?.StartIndex);
        Assert.Equal(1, structure.GetOpenBlockCountAt(2));
        Assert.Equal(0, structure.GetOpenBlockCountAt(5));
        Assert.False(block.IsSectionEmpty(0, steps.Length));
        Assert.True(block.IsSectionEmpty(2, steps.Length));
        Assert.True(block.IsSectionEmpty(3, steps.Length));
    }

    [Fact]
    public void Analyze_ReportsExistingConditionalStructureRules()
    {
        JobStep[] steps =
        [
            new IfStep(),
            new ElseStep(),
            new ElseIfStep(),
            new ElseStep(),
            new EndIfStep(),
            new EndIfStep()
        ];

        var diagnostics = ControlFlowStructureAnalyzer.Analyze(steps).Diagnostics;

        Assert.Contains(diagnostics, issue => issue.Code == ControlFlowDiagnosticCodes.SectionAfterElse);
        Assert.Contains(diagnostics, issue => issue.Code == ControlFlowDiagnosticCodes.DuplicateElse);
        Assert.Contains(diagnostics, issue => issue.Code == ControlFlowDiagnosticCodes.OrphanEnd);
    }

    [Fact]
    public void Analyze_ProjectsNestedRangesAsValidBlocks()
    {
        JobStep[] steps =
        [
            new IfStep(),
            new IfStep(),
            new TimeoutStep(),
            new EndIfStep(),
            new EndIfStep()
        ];

        var structure = ControlFlowStructureAnalyzer.Analyze(steps);

        Assert.True(structure.IsValid);
        Assert.Equal(2, structure.Blocks.Count);
        var outer = Assert.Single(structure.Blocks, block => block.StartIndex == 0);
        var inner = Assert.Single(structure.Blocks, block => block.StartIndex == 1);
        Assert.Equal(0, outer.Depth);
        Assert.Equal(1, inner.Depth);
        Assert.Equal(0, inner.ParentStartIndex);
        Assert.Same(inner, structure.GetOwningBlock(2));
        Assert.Equal(2, structure.GetOpenBlockCountAt(2));
    }

    [Fact]
    public void Analyze_ReportsMissingEndOnTheOpeningStep()
    {
        var opening = new IfStep();

        var structure = ControlFlowStructureAnalyzer.Analyze([opening, new TimeoutStep()]);

        var diagnostic = Assert.Single(structure.Diagnostics);
        Assert.Equal(ControlFlowDiagnosticCodes.MissingEnd, diagnostic.Code);
        Assert.Same(opening, diagnostic.Step);
        Assert.Null(Assert.Single(structure.Blocks).EndIndex);
    }
}
