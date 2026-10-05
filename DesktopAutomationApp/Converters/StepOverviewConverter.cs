using System.Globalization;
using System.Windows.Data;
using DesktopAutomationApp.Services.Jobs;
using TaskAutomation.Jobs;
using TaskAutomation.Steps.Definitions;
using MahApps.Metro.IconPacks;
using DesktopAutomationApp.Localization;
using System.Collections;
using System.Windows;
using System.Windows.Media;

namespace DesktopAutomationApp.Converters;

public sealed class StepOverviewConverter : IMultiValueConverter
{
    private readonly JobStepDetailsProvider _provider = new();
    private int _version = int.MinValue;
    private object? _steps;
    private readonly Dictionary<JobStep, string> _summaries = new(ReferenceEqualityComparer.Instance);
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter as string == "color")
        {
            var category = values[0] is JobStep coloredStep
                ? StepIconPresentation.CategoryForType(coloredStep.GetType()) : values[0] as string ?? "Unknown";
            return StepIconPresentation.ForCategory(category, values.ElementAtOrDefault(1) as FrameworkElement);
        }
        if (values[0] is not JobStep step) return string.Empty;
        if (parameter as string is "validationMessage" or "hasValidationError")
        {
            var issue = (values.ElementAtOrDefault(1) as IReadOnlyList<DesktopAutomationApp.ViewModels.JobStepsViewModel.StepValidationIssue>)
                ?.FirstOrDefault(candidate => candidate.Step.Id == step.Id);
            return parameter as string == "hasValidationError" ? issue is not null : issue?.Message ?? string.Empty;
        }
        if (parameter as string == "branchActionsVisibility") return step is IfStep or ElseIfStep ? Visibility.Visible : Visibility.Collapsed;
        if (parameter as string == "outputVisibility") return step is UserChoiceStep ? Visibility.Visible : Visibility.Collapsed;
        if (parameter as string == "icon") return StepIconPresentation.ForType(step.GetType());
        var variables = values.Length > 3 ? values[3] as IReadOnlyList<JobVariable> : null;
        var locals = values.Length > 4 ? values[4] as IReadOnlyList<LocalValue> : null;
        var version = values.ElementAtOrDefault(2) is int number ? number : 0;
        if (_version != version || !ReferenceEquals(_steps, values.ElementAtOrDefault(1)))
        { _summaries.Clear(); _version = version; _steps = values.ElementAtOrDefault(1); }
        if (!_summaries.TryGetValue(step, out var summary))
            _summaries[step] = summary = _provider.GetSummary(step, _steps as IEnumerable,
                (variables ?? []).Concat<JobVariable>(locals ?? []).ToArray());
        if (values.ElementAtOrDefault(5) is IReadOnlyCollection<string> collapsed && collapsed.Contains(step.Id)
            && values.ElementAtOrDefault(6) is IList items)
        {
            var projection = StepListProjection.Get(items, version);
            var index = Array.IndexOf(projection.Steps, step);
            if (projection.Structure.GetBlockStartingAt(index) is { } block)
            {
                var children = projection.Steps.Skip(index + 1).Take(block.LastIndex(items.Count) - index).ToArray();
                var count = children.Count(StepLocalization.HasListPosition);
                var parts = new List<string> { summary, count == 1 ? Loc.Get("Ui.Job.Steps.Summary.Child")
                    : Loc.Format("Ui.Job.Steps.Summary.Children", count) };
                var issues = values.ElementAtOrDefault(7) as IReadOnlyList<DesktopAutomationApp.ViewModels.JobStepsViewModel.StepValidationIssue>;
                var problems = issues is null ? children.Count(child => !child.IsValid)
                    : issues.Count(issue => children.Any(child => child.Id == issue.Step.Id));
                var breakpoints = children.Count(child => child.IsBreakpoint);
                if (problems > 0) parts.Add(Loc.Format("Ui.Job.Steps.Summary.Problems", problems));
                if (breakpoints > 0) parts.Add(Loc.Format("Ui.Job.Steps.Summary.Breakpoints", breakpoints));
                summary = string.Join(" · ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
            }
        }
        return summary;
    }
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
