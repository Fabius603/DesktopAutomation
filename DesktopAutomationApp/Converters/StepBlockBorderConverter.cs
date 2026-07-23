using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using TaskAutomation.Jobs;

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
    /// Nesting is not supported (and is prevented by the ViewModel).
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
            _bgBrushes     = new SolidColorBrush[Palette.Length];
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
                _cacheVersion  = version;
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
            int groupIndex = -1;
            bool inBlock   = false;

            foreach (var item in steps)
            {
                if (item is not JobStep s) continue;

                if (s is IfStep) { groupIndex++; inBlock = true; }

                int assigned = (s is IfStep or ElseIfStep or ElseStep or EndIfStep)
                    ? groupIndex
                    : (inBlock ? groupIndex : -1);

                map[s] = assigned;

                if (s is EndIfStep) inBlock = false;
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
            var index = steps.IndexOf(step);
            var inside = false;
            for (var i = 0; i <= index && i < steps.Count; i++)
            {
                if (steps[i] is IfStep) inside = true;
                if (i < index && steps[i] is EndIfStep) inside = false;
            }
            var start = step is IfStep;
            var end = step is EndIfStep && inside;
            var branchMarker = step is IfStep or ElseIfStep or ElseStep or EndIfStep;
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
        private sealed record Layout(bool InBlock, bool Start, bool End, bool Branch, bool Inner);
        private int _cacheVersion = int.MinValue;
        private IList? _cacheCollection;
        private Dictionary<JobStep, Layout> _layout =
            new(ReferenceEqualityComparer.Instance);

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] is not JobStep step || values[1] is not IList steps)
                return DependencyProperty.UnsetValue;
            var version = values.Length > 2 && values[2] is int value ? value : 0;
            if (!ReferenceEquals(steps, _cacheCollection) || version != _cacheVersion)
                RebuildCache(steps, version);
            if (!_layout.TryGetValue(step, out var layout))
                return DependencyProperty.UnsetValue;

            var inBlock = layout.InBlock;
            var start = layout.Start;
            var end = layout.End;
            var branch = layout.Branch;
            var inner = layout.Inner;
            return (parameter as string) switch
            {
                // Keep the block connected internally, but separate its closing
                // EndIf card from the following top-level step like a normal card.
                "itemMargin" => new Thickness(0, 0, 10, end || !inBlock ? 7 : 0),
                "frameBorder" => !inBlock ? new Thickness(0) : start ? new Thickness(1, 1, 1, 0) : end ? new Thickness(1, 0, 1, 1) : new Thickness(1, 0, 1, 0),
                "frameCorner" => start ? new CornerRadius(9, 9, 0, 0) : end ? new CornerRadius(0, 0, 9, 9) : new CornerRadius(0),
                "cardMargin" => inner ? new Thickness(12, 4, 12, 4) : new Thickness(0),
                "cardBorder" => !inBlock || inner ? new Thickness(1) : branch ? new Thickness(0, 1, 0, 0) : new Thickness(0),
                "cardCorner" => !inBlock ? new CornerRadius(8) : inner ? new CornerRadius(6) : start ? new CornerRadius(8, 8, 0, 0) : end ? new CornerRadius(0, 0, 8, 8) : new CornerRadius(0),
                "frameBackground" => inBlock ? FindBrush("App.Brush.Surface") : Brushes.Transparent,
                "cardBackground" => start || branch ? FindBrush("App.Brush.SurfaceHover") : FindBrush("App.Brush.Surface"),
                _ => DependencyProperty.UnsetValue
            };
        }

        private void RebuildCache(IList steps, int version)
        {
            var layout = new Dictionary<JobStep, Layout>(
                steps.Count, ReferenceEqualityComparer.Instance);
            var inBlock = false;
            foreach (var item in steps)
            {
                if (item is not JobStep step) continue;
                if (step is IfStep) inBlock = true;
                var start = step is IfStep;
                var end = step is EndIfStep && inBlock;
                var branch = step is ElseIfStep or ElseStep;
                layout[step] = new Layout(
                    inBlock, start, end, branch,
                    inBlock && !start && !end && !branch);
                if (step is EndIfStep) inBlock = false;
            }
            _layout = layout;
            _cacheCollection = steps;
            _cacheVersion = version;
        }

        private static Brush FindBrush(string key) => Application.Current?.TryFindResource(key) as Brush ?? Brushes.Transparent;
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
