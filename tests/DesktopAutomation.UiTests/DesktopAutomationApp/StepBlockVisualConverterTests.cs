using System.Globalization;
using System.Windows;
using DesktopAutomationApp.Converters;
using TaskAutomation.Jobs;
using TaskAutomation.Steps.Definitions;

namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class StepBlockVisualConverterTests
{
    [Fact]
    public void AlternativesFoldOnlyTheirContentsAndKeepTheirSiblingsVisible()
    {
        var alternative = new ElseIfStep();
        var fallback = new ElseStep();
        List<JobStep> steps = [new IfStep(), new TimeoutStep(), alternative, new IfStep(), new TimeoutStep(),
            new EndIfStep(), fallback, new TimeoutStep(), new EndIfStep(), new TimeoutStep()];
        var converter = new StepBlockVisualConverter();
        for (var index = 0; index < steps.Count; index++)
        {
            var visibility = converter.Convert([steps[index], null!, steps, 0, new[] { alternative.Id }, 600d],
                typeof(Visibility), "visibility", CultureInfo.InvariantCulture);
            Assert.Equal(index is 3 or 4 or 5 or 8 ? Visibility.Collapsed : Visibility.Visible, visibility);
        }
        Assert.Equal(Visibility.Visible, converter.Convert([fallback, null!, steps, 0], typeof(Visibility), "collapseVisibility", CultureInfo.InvariantCulture));
        Assert.Equal(MahApps.Metro.IconPacks.PackIconMaterialKind.ChevronRight,
            converter.Convert([alternative, null!, steps, 0, new[] { alternative.Id }], typeof(object), "collapseIcon", CultureInfo.InvariantCulture));
        Assert.Equal(MahApps.Metro.IconPacks.PackIconMaterialKind.ChevronDown,
            converter.Convert([alternative, null!, steps, 0, Array.Empty<string>()], typeof(object), "collapseIcon", CultureInfo.InvariantCulture));
    }

    [Fact]
    public void EmptyBranchHintsReflectEachBranchAndAreHiddenWhenFoldedOrFilledByPreview()
    {
        List<JobStep> steps = [new IfStep(), new ElseIfStep(), new ElseStep(), new EndIfStep()];
        var converter = new StepBlockVisualConverter();
        foreach (var step in steps.Take(3))
        {
            Assert.Equal(Visibility.Visible, converter.Convert([step, null!, steps, 0, Array.Empty<string>()], typeof(Visibility), "emptyBranchVisibility", CultureInfo.InvariantCulture));
            Assert.Equal(Visibility.Collapsed, converter.Convert([step, null!, steps, 0, new[] { step.Id }], typeof(Visibility), "emptyBranchVisibility", CultureInfo.InvariantCulture));
        }
        List<JobStep> preview = [steps[0], new TimeoutStep(), steps[1], steps[2], steps[3]];
        Assert.Equal(Visibility.Collapsed, converter.Convert([steps[0], preview, steps, 0, Array.Empty<string>()], typeof(Visibility), "emptyBranchVisibility", CultureInfo.InvariantCulture));
    }

    [Fact]
    public void BranchCards_HaveUniformHeight()
    {
        List<JobStep> steps = [new IfStep(), new TimeoutStep(), new ElseStep(), new EndIfStep()];
        var converter = new StepBlockVisualConverter();
        foreach (var step in steps.Take(3))
            Assert.Equal(64d, converter.Convert([step, null!, steps, 0], typeof(double), "cardHeight", CultureInfo.InvariantCulture));
    }
    [Fact]
    public void PreviewProjection_UsesTheFutureControlFlowPositionForCardGeometry()
    {
        var moved = new TimeoutStep();
        var conditional = new IfStep();
        var body = new TimeoutStep();
        var endIf = new EndIfStep();
        var source = new List<JobStep> { moved, conditional, body, endIf };
        var preview = new List<JobStep> { conditional, moved, body, endIf };
        var converter = new StepBlockVisualConverter();

        var originalMargin = Assert.IsType<Thickness>(converter.Convert(
            [moved, null!, source, 0],
            typeof(Thickness),
            "cardMargin",
            CultureInfo.InvariantCulture));
        var previewMargin = Assert.IsType<Thickness>(converter.Convert(
            [moved, preview, source, 0],
            typeof(Thickness),
            "cardMargin",
            CultureInfo.InvariantCulture));

        Assert.Equal(0, originalMargin.Left);
        var originalIndent = Assert.IsType<Thickness>(converter.Convert(
            [moved, null!, source, 0], typeof(Thickness), "frameMargin", CultureInfo.InvariantCulture));
        var previewIndent = Assert.IsType<Thickness>(converter.Convert(
            [moved, preview, source, 0], typeof(Thickness), "frameMargin", CultureInfo.InvariantCulture));
        Assert.Equal(0, previewMargin.Left);
        Assert.True(previewIndent.Left > originalIndent.Left);
    }

    [Fact]
    public void CollapsedOuterBlock_HidesBranchesAndNestedBlocksButKeepsFollowingSteps()
    {
        var outer = new IfStep();
        List<JobStep> steps = [outer, new IfStep(), new TimeoutStep(), new EndIfStep(),
            new ElseStep(), new TimeoutStep(), new EndIfStep(), new TimeoutStep()];
        var converter = new StepBlockVisualConverter();
        for (var index = 0; index < steps.Count; index++)
        {
            var visibility = converter.Convert([steps[index], null!, steps, 0, new[] { outer.Id }, 600d],
                typeof(Visibility), "visibility", CultureInfo.InvariantCulture);
            Assert.Equal(index is > 0 and < 7 ? Visibility.Collapsed : Visibility.Visible, visibility);
        }
    }

    [Fact]
    public void DeepNesting_LeavesReadableWidthAndHidesAllInternalClosingMarkers()
    {
        List<JobStep> steps = Enumerable.Range(0, 10).Select(_ => (JobStep)new IfStep()).ToList();
        var body = new TimeoutStep();
        steps.Add(body);
        var converter = new StepBlockVisualConverter();
        var width = Assert.IsType<double>(converter.Convert([body, null!, steps, 0, Array.Empty<string>(), 400d],
            typeof(double), "cardWidth", CultureInfo.InvariantCulture));
        Assert.True(width >= 320);
        List<JobStep> damaged = [new EndIfStep()];
        Assert.Equal(Visibility.Collapsed, converter.Convert([damaged[0], null!, damaged, 0],
            typeof(Visibility), "visibility", CultureInfo.InvariantCulture));
    }
}
