using System.Collections;
using System.Globalization;
using System.Windows.Data;
using DesktopAutomationApp.Services.Jobs;
using TaskAutomation.Jobs;

namespace DesktopAutomationApp.Converters;

public sealed record StepDetailItem(string Name, string Value);
public sealed record StepDetailGroup(string Title, IReadOnlyList<StepDetailItem> Items);
public sealed record StepResultPropertyDetails(
    string Name,
    string TypeName,
    string Description,
    IReadOnlyList<StepResultPropertyDetails> Children);
public sealed record StepResultDetails(
    string TypeName,
    IReadOnlyList<StepResultPropertyDetails> Properties);
public sealed record JobStepDetails(IReadOnlyList<StepDetailGroup> Groups, StepResultDetails? Result);

/// <summary>Creates a complete, read-only description directly from a step's settings and result types.</summary>
public sealed class JobStepDetailsConverter : IMultiValueConverter
{
    private static readonly JobStepDetailsProvider Provider = new();
    private int _cacheVersion = int.MinValue;
    private IEnumerable? _cacheSteps;
    private IReadOnlyList<JobVariable>? _cacheVariables;
    private IReadOnlyList<ValueProviderSourceDescriptor>? _cacheProviderSources;
    private Dictionary<JobStep, JobStepDetails> _cache =
        new(ReferenceEqualityComparer.Instance);

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.FirstOrDefault() is not JobStep step)
            return new JobStepDetails([], null);

        var steps = values.Skip(1).FirstOrDefault() as IEnumerable;
        var version = values.Length > 2 && values[2] is int value ? value : 0;
        var variables = values.Length > 3 ? values[3] as IReadOnlyList<JobVariable> : null;
        var providerSources = values.Length > 4
            ? values[4] as IReadOnlyList<ValueProviderSourceDescriptor>
            : null;
        if (!ReferenceEquals(steps, _cacheSteps)
            || !ReferenceEquals(variables, _cacheVariables)
            || !ReferenceEquals(providerSources, _cacheProviderSources)
            || version != _cacheVersion)
        {
            _cache = new Dictionary<JobStep, JobStepDetails>(ReferenceEqualityComparer.Instance);
            _cacheSteps = steps;
            _cacheVariables = variables;
            _cacheProviderSources = providerSources;
            _cacheVersion = version;
        }
        if (!_cache.TryGetValue(step, out var details))
            _cache[step] = details = Provider.GetDetails(step, steps, variables, providerSources);
        return details;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class StepHasDetailsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is EndIfStep ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class StepDetailsEnabledConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is not EndIfStep;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class StepCanAddBranchesConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is IfStep or ElseIfStep ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
