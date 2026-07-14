using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using DesktopAutomationApp.Logging;
using TaskAutomation.Logging;

namespace DesktopAutomationApp.ViewModels;

public sealed class ApplicationLogsViewModel : ViewModelBase
{
    private readonly IApplicationLogService _service;
    private readonly ObservableCollection<ApplicationLogEntry> _entries = new();
    private ExecutionLogLevel _selectedMinimumLevel = ExecutionLogLevel.Information;
    private string _searchText = string.Empty;

    public ApplicationLogsViewModel(IApplicationLogService service)
    {
        _service = service;
        Entries = CollectionViewSource.GetDefaultView(_entries);
        Entries.Filter = IsVisible;
        RefreshCommand = new RelayCommand(async () => await RefreshAsync());
        OpenLogFolderCommand = new RelayCommand(OpenLogFolder);
        BackCommand = new RelayCommand(() => RequestBack?.Invoke());
        _service.EntryWritten += OnEntryWritten;
        _ = RefreshAsync();
    }

    public ICollectionView Entries { get; }
    public ObservableCollection<ExecutionLogLevel> AvailableLevels { get; } = new(Enum.GetValues<ExecutionLogLevel>());
    public ICommand RefreshCommand { get; }
    public ICommand OpenLogFolderCommand { get; }
    public ICommand BackCommand { get; }
    public event Action? RequestBack;

    public ExecutionLogLevel SelectedMinimumLevel
    {
        get => _selectedMinimumLevel;
        set { if (_selectedMinimumLevel != value) { SetProperty(ref _selectedMinimumLevel, value); Entries.Refresh(); } }
    }
    public string SearchText
    {
        get => _searchText;
        set { var next = value ?? string.Empty; if (_searchText != next) { SetProperty(ref _searchText, next); Entries.Refresh(); } }
    }

    public async Task RefreshAsync()
    {
        var entries = await Task.Run(() => _service.ReadEntries());
        _entries.Clear();
        foreach (var entry in entries) _entries.Add(entry);
        Entries.Refresh();
    }

    private bool IsVisible(object item) => item is ApplicationLogEntry entry
        && entry.Level >= SelectedMinimumLevel
        && (string.IsNullOrWhiteSpace(SearchText)
            || entry.Message.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)
            || entry.Source.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)
            || entry.Details?.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) == true);

    private void OpenLogFolder() => Process.Start(new ProcessStartInfo(_service.LogDirectory) { UseShellExecute = true });
    private void OnEntryWritten(object? sender, ApplicationLogEntry entry)
    {
        var dispatcher = Application.Current?.Dispatcher;
        void Add()
        {
            _entries.Add(entry);
            while (_entries.Count > 5000) _entries.RemoveAt(0);
        }
        if (dispatcher == null || dispatcher.CheckAccess()) Add(); else dispatcher.InvokeAsync(Add);
    }
}
