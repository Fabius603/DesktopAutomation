using System.Collections;
using System.Windows;
using System.Windows.Controls;
using DesktopAutomationApp.Behaviors;
using DesktopAutomationApp.ViewModels;
using TaskAutomation.Jobs;
using TaskAutomation.Jobs.ControlFlow;

namespace DesktopAutomationApp.Services.Jobs;

internal sealed record StepDropRow(int Index, double Top, double Height);

/// <summary>Maps measured WPF rows to the shared control-flow insertion rules. Preview displacement is removed before hit testing.</summary>
internal static class JobStepDropPlacement
{
    public static StepDragDrop.InsertionPlacement Resolve(ListBox list, Point pointer)
    {
        if (list.ItemsSource is not IList items) return new(0, 4, StepListProjection.GutterWidth);
        var viewModel = list.DataContext as JobStepsViewModel;
        var projection = StepListProjection.Get(items, viewModel?.StepsVersion ?? 0);
        var rows = new List<StepDropRow>();
        for (var index = 0; index < list.Items.Count; index++)
            if (list.ItemContainerGenerator.ContainerFromIndex(index) is ListBoxItem { Visibility: Visibility.Visible, ActualHeight: > 0 } item)
                rows.Add(new(index, item.TranslatePoint(new Point(0, 0), list).Y - StepDragDrop.GetPreviewDisplacement(item), item.ActualHeight));
        var horizontalOffset = StepDragDrop.GetHorizontalScrollOffset(list);
        var placement = Resolve(projection.Steps, rows, new Point(pointer.X + horizontalOffset, pointer.Y), viewModel?.CollapsedBlockIds ?? []);
        var depth = (placement.X - StepListProjection.GutterWidth) / StepListProjection.Indentation;
        return placement with { X = placement.X - horizontalOffset, Width = projection.Width((int)depth, list.ActualWidth) };
    }

    internal static StepDragDrop.InsertionPlacement Resolve(IReadOnlyList<JobStep> steps,
        IReadOnlyList<StepDropRow> rows, Point pointer, IReadOnlyCollection<string> collapsed)
    {
        if (rows.Count == 0) return new(0, 18, StepListProjection.GutterWidth);
        var row = rows.FirstOrDefault(candidate => pointer.Y <= candidate.Top + candidate.Height) ?? rows[^1];
        var structure = ControlFlowStructureAnalyzer.Analyze(steps);
        var closing = structure.GetBlockEndingAt(row.Index) is not null;
        var cardHeight = closing ? 14 : 64;
        var after = pointer.Y >= row.Top + Math.Min(cardHeight, row.Height) / 2;
        var requestedDepth = Math.Max(0, (int)Math.Floor((pointer.X - StepListProjection.GutterWidth) / StepListProjection.Indentation));
        var target = ControlFlowEditRules.ResolveInsertionTarget(steps, row.Index, after, requestedDepth,
            afterWholeBlock: after && collapsed.Contains(steps[row.Index].Id));
        var next = rows.FirstOrDefault(candidate => candidate.Index >= target.Index);
        var y = next is not null ? next.Top - 4 : rows[^1].Top + rows[^1].Height + 4;
        // Closing markers have no rows. Keep each insertion boundary in its own
        // footer lane rather than placing an inner-branch preview below the block.
        if (next is null || next.Index > target.Index)
        {
            var previous = rows.LastOrDefault(candidate => candidate.Index < target.Index);
            if (previous is not null && steps.Skip(previous.Index + 1)
                    .Take((next?.Index ?? steps.Count) - previous.Index - 1).Any(step => step is EndIfStep))
            {
                var passedClosures = steps.Skip(previous.Index + 1).Take(target.Index - previous.Index - 1)
                    .Count(step => step is EndIfStep);
                y = previous.Top + previous.Height + 4 + passedClosures * 24;
            }
        }
        return new(target.Index, Math.Max(2, y), StepListProjection.GutterWidth + target.Depth * StepListProjection.Indentation,
            Math.Abs(pointer.Y - Math.Max(2, y)));
    }
}
