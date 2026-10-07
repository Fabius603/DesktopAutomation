using System.Windows.Input;

namespace DesktopAutomationApp.ViewModels;

/// <summary>Presentation state shared by navigation and inspector panes.</summary>
public sealed class CollapsiblePaneState : ViewModelBase
{
    public const double NavigationCollapseWidth = 1280;
    public const double InspectorCollapseWidth = 900;
    public const double InspectorPeekWidth = 48;
    private readonly double _collapseWidth;
    private readonly Func<bool, Task>? _save;
    private bool _manuallyCollapsed;
    private bool _narrow;
    private bool _expandedInNarrowLayout;

    public CollapsiblePaneState(double collapseWidth, bool manuallyCollapsed = false, Func<bool, Task>? save = null)
    {
        _collapseWidth = collapseWidth;
        _manuallyCollapsed = manuallyCollapsed;
        _save = save;
        ToggleCommand = new AsyncRelayCommand(ToggleAsync);
    }

    public bool IsCollapsed => _manuallyCollapsed || (_narrow && !_expandedInNarrowLayout);
    public bool IsExpanded => !IsCollapsed;
    public ICommand ToggleCommand { get; }

    public void UpdateWidth(double width)
    {
        if (!double.IsFinite(width) || width <= 0) return;
        // A small dead band prevents flicker when resizing around the breakpoint.
        var narrow = width < (_narrow ? _collapseWidth + 32 : _collapseWidth);
        if (narrow == _narrow) return;
        _narrow = narrow;
        _expandedInNarrowLayout = false;
        NotifyState();
    }

    private async Task ToggleAsync()
    {
        var expand = IsCollapsed;
        _manuallyCollapsed = !expand;
        _expandedInNarrowLayout = expand && _narrow;
        NotifyState();
        if (_save is not null) await _save(_manuallyCollapsed);
    }

    private void NotifyState()
    {
        OnPropertyChanged(nameof(IsCollapsed));
        OnPropertyChanged(nameof(IsExpanded));
    }
}
