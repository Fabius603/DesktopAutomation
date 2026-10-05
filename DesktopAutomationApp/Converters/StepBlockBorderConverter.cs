using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using TaskAutomation.Jobs;
using TaskAutomation.Jobs.ControlFlow;

namespace DesktopAutomationApp.Converters
{
    /// <summary>
    /// Returns a SolidColorBrush for the left-border accent of a step card,
    /// indicating which If-block the step belongs to.
    ///
    /// values[0]  = the step (JobStep)
    /// values[1]  = the full Steps collection (IList)
    /// values[2]  = StepsVersion (int) — cache key; incremented by ViewModel on each collection change
    /// parameter  = "border"      → returns the full-opacity Brush for the left bar
    ///              "background"  → returns a very subtle tinted Brush for the card background
    ///              (any other / null) → same as "border"
    ///
    /// Each If-group gets its own unique color cycling through the palette.
    /// Nested blocks use the shared control-flow projection and keep a constant indent per depth.
    /// Steps outside any block return Transparent.
    /// </summary>
    public sealed class StepBlockBorderConverter : IMultiValueConverter
    {
        // Palette: 6 visually distinct hues that work on a dark theme.
        private static readonly Color[] Palette =
        {
            Color.FromRgb(0x00, 0xB4, 0xD8), // teal/cyan  – group 0
            Color.FromRgb(0x9B, 0x5D, 0xE5), // purple     – group 1
            Color.FromRgb(0xF1, 0x5B, 0x2A), // orange     – group 2
            Color.FromRgb(0x06, 0xD6, 0x7E), // green      – group 3
            Color.FromRgb(0xF7, 0xC5, 0x48), // amber      – group 4
            Color.FromRgb(0xEF, 0x48, 0x6E), // rose       – group 5
        };

        // Pre-built frozen brushes — never allocate new ones at runtime.
        private static readonly SolidColorBrush[] _borderBrushes;
        private static readonly SolidColorBrush[] _bgBrushes;

        static StepBlockBorderConverter()
        {
            _borderBrushes = new SolidColorBrush[Palette.Length];
            _bgBrushes = new SolidColorBrush[Palette.Length];
            for (int i = 0; i < Palette.Length; i++)
            {
                var c = Palette[i];
                _borderBrushes[i] = new SolidColorBrush(c);
                _borderBrushes[i].Freeze();
                _bgBrushes[i] = new SolidColorBrush(Color.FromArgb(0x2A, c.R, c.G, c.B));
                _bgBrushes[i].Freeze();
            }
        }

        // ── Cache: group-index map, rebuilt only when StepsVersion changes ────────
        private int _cacheVersion = int.MinValue;
        private Dictionary<JobStep, int>? _groupIndexMap;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values?.Length < 2 || values![0] is not JobStep currentStep || values[1] is not IList steps)
                return DependencyProperty.UnsetValue;

            bool wantBackground = parameter is string p &&
                                  p.Equals("background", StringComparison.OrdinalIgnoreCase);

            if (wantBackground)
                return Brushes.Transparent;

            // Rebuild map only when the version counter changes (once per collection change).
            int version = values.Length > 2 && values[2] is int v ? v : 0;
            if (_groupIndexMap == null || version != _cacheVersion)
            {
                _groupIndexMap = BuildGroupIndexMap(steps);
                _cacheVersion = version;
            }

            int groupIndex = _groupIndexMap.TryGetValue(currentStep, out var idx) ? idx : -1;
            if (groupIndex < 0)
                return Brushes.Transparent;

            return _borderBrushes[groupIndex % _borderBrushes.Length];
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();

        // ── helpers ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Iterates the list ONCE (O(n)) and returns a map of step → group-index.
        /// Group-index is -1 for steps outside any block.
        /// </summary>
        private static Dictionary<JobStep, int> BuildGroupIndexMap(IList steps)
        {
            var map = new Dictionary<JobStep, int>(steps.Count, ReferenceEqualityComparer.Instance);
            var typedSteps = steps.Cast<object>().OfType<JobStep>().ToArray();
            var structure = ControlFlowStructureAnalyzer.Analyze(typedSteps);
            var groupByStart = structure.Blocks
                .Select((block, index) => (block.StartIndex, index))
                .ToDictionary(item => item.StartIndex, item => item.index);

            for (var index = 0; index < typedSteps.Length; index++)
            {
                var block = structure.GetOwningBlock(index);
                map[typedSteps[index]] = block is not null && groupByStart.TryGetValue(block.StartIndex, out var group)
                    ? group
                    : -1;
            }

            return map;
        }
    }

    /// <summary>Returns connected card geometry for steps inside an If/EndIf block.</summary>
    public sealed class StepBlockShapeConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] is not JobStep step || values[1] is not IList steps)
                return DependencyProperty.UnsetValue;
            var typedSteps = steps.Cast<object>().OfType<JobStep>().ToArray();
            var index = Array.FindIndex(typedSteps, candidate => ReferenceEquals(candidate, step));
            var block = index >= 0
                ? ControlFlowStructureAnalyzer.Analyze(typedSteps).GetOwningBlock(index)
                : null;
            var inside = block is not null;
            var start = block?.StartIndex == index;
            var end = block?.EndIndex == index;
            var branchMarker = step is IControlFlowMarker;
            var mode = parameter as string;
            if (!inside)
                return mode switch { "margin" => new Thickness(0, 0, 10, 6), "border" => new Thickness(1), _ => new CornerRadius(8) };
            return mode switch
            {
                "margin" => new Thickness(branchMarker ? 0 : 14, 0, 10, end ? 10 : 3),
                "border" => new Thickness(1),
                _ => start ? new CornerRadius(8, 8, 4, 4) : end ? new CornerRadius(4, 4, 8, 8) : new CornerRadius(4)
            };
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    public sealed class StepBlockVisualConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var preview = values.Length >= 4;
            var items = preview ? values[1] as IList ?? values[2] as IList : values.ElementAtOrDefault(1) as IList;
            if (values[0] is not JobStep step || items is null) return DependencyProperty.UnsetValue;
            var version = values.ElementAtOrDefault(preview ? 3 : 2) is int number ? number : 0;
            var projection = DesktopAutomationApp.Services.Jobs.StepListProjection.Get(items, version);
            var index = Array.IndexOf(projection.Steps, step);
            if (index < 0) return DependencyProperty.UnsetValue;
            var block = projection.Structure.GetOwningBlock(index);
            var marker = step is IControlFlowMarker;
            var depth = projection.Depth(index);
            var collapsed = values.ElementAtOrDefault(4) as IReadOnlyCollection<string> ?? Array.Empty<string>();
            var viewport = values.ElementAtOrDefault(5) is double width && width > 0 ? width : 430;
            var hidden = projection.IsHidden(index, collapsed);
            var isEnd = block?.EndIndex == index;
            var closed = collapsed.Contains(step.Id);
            return (parameter as string) switch
            {
                "visibility" => hidden || step is EndIfStep ? Visibility.Collapsed : Visibility.Visible,
                "collapseVisibility" => projection.CanCollapse(index) ? Visibility.Visible : Visibility.Collapsed,
                "collapseIcon" => closed ? MahApps.Metro.IconPacks.PackIconMaterialKind.ChevronRight : MahApps.Metro.IconPacks.PackIconMaterialKind.ChevronDown,
                "emptyBranchVisibility" => !hidden && !closed && projection.IsEmptyBranch(index) ? Visibility.Visible : Visibility.Collapsed,
                "collapseLabel" => DesktopAutomationApp.Localization.Loc.Get(closed ? "Ui.Job.Steps.ExpandBlock" : "Ui.Job.Steps.CollapseBlock"),
                "itemMargin" => hidden ? new Thickness(0) : new Thickness(0, step is IfStep ? 8 : 0, 8, 8 + 24 * projection.ClosuresAfter(index, collapsed).Count),
                "headerMargin" => new Thickness(marker && block is not null ? 36 : 12, 6, 12, 6),
                "frameMargin" => new Thickness(depth * DesktopAutomationApp.Services.Jobs.StepListProjection.Indentation, 0, 0, 0),
                "frameWidth" or "cardWidth" => projection.Width(depth, viewport),
                "cardHeight" => isEnd ? 14d : 64d,
                // An unmatched EndIf is a visible, diagnosable row.
                "contentVisibility" => isEnd ? Visibility.Collapsed : Visibility.Visible,
                "frameBorder" => new Thickness(0),
                "frameCorner" => new CornerRadius(0),
                "cardMargin" => new Thickness(0),
                "cardBorder" => marker && block is not null ? new Thickness(0) : new Thickness(1),
                "cardCorner" => new CornerRadius(8),
                "frameBackground" => Brushes.Transparent,
                "cardBackground" => marker && block is not null ? Brushes.Transparent : FindBrush("App.Brush.StepCard"),
                _ => DependencyProperty.UnsetValue
            };
        }
        private static Brush FindBrush(string key) => Application.Current?.TryFindResource(key) as Brush ?? Brushes.Transparent;
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
