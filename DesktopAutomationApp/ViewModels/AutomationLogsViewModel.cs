using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using TaskAutomation.Logging;

namespace DesktopAutomationApp.ViewModels;

public sealed class AutomationLogsViewModel : ViewModelBase
{
    private readonly IAutomationLogService _service;
    private readonly ObservableCollection<AutomationLogEntryItem> _entries = new();
    private AutomationLog? _selectedLog;
    private ExecutionLogLevel _selectedMinimumLevel = ExecutionLogLevel.Information;
    private string _searchText = string.Empty;

    public AutomationLogsViewModel(IAutomationLogService service)
    {
        _service = service;
        Entries = CollectionViewSource.GetDefaultView(_entries);
        Entries.Filter = IsVisible;
        RefreshCommand = new RelayCommand(async () => await RefreshAsync());
        OpenLogFolderCommand = new RelayCommand(OpenLogFolder);
        BackCommand = new RelayCommand(() => RequestBack?.Invoke());
        _service.EntryWritten += OnEntryWritten;
        _service.LogsChanged += OnLogsChanged;
        _ = RefreshAsync();
    }

    public ObservableCollection<AutomationLog> Logs { get; } = new();
    public ObservableCollection<ExecutionLogLevel> AvailableLevels { get; } = new(Enum.GetValues<ExecutionLogLevel>());
    public ICollectionView Entries { get; }
    public ICommand RefreshCommand { get; }
    public ICommand OpenLogFolderCommand { get; }
    public ICommand BackCommand { get; }
    public event Action? RequestBack;

    public AutomationLog? SelectedLog
    {
        get => _selectedLog;
        set
        {
            if (Equals(_selectedLog, value)) return;
            SetProperty(ref _selectedLog, value);
            _ = LoadEntriesAsync();
        }
    }

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
        var selectedId = SelectedLog?.AutomationId;
        var logs = await Task.Run(() => _service.Logs.ToArray());
        Logs.Clear();
        foreach (var log in logs) Logs.Add(log);
        SelectedLog = Logs.FirstOrDefault(log => log.AutomationId == selectedId) ?? Logs.FirstOrDefault();
        if (SelectedLog != null) await LoadEntriesAsync();
    }

    private async Task LoadEntriesAsync()
    {
        _entries.Clear();
        if (SelectedLog == null) return;
        var selectedId = SelectedLog.AutomationId;
        var entries = await Task.Run(() => _service.ReadEntries(selectedId));
        if (SelectedLog?.AutomationId != selectedId) return;
        foreach (var entry in entries)
            _entries.Add(new AutomationLogEntryItem(entry));
        Entries.Refresh();
    }

    private bool IsVisible(object item) => item is AutomationLogEntryItem entry
        && entry.Level >= SelectedMinimumLevel
        && (string.IsNullOrWhiteSpace(SearchText)
            || entry.Message.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)
            || entry.Details.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase));

    private void OpenLogFolder()
    {
        var directory = SelectedLog == null
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DesktopAutomation", "Logs", "Automations")
            : Path.GetDirectoryName(SelectedLog.FilePath)!;
        Directory.CreateDirectory(directory);
        Process.Start(new ProcessStartInfo(directory) { UseShellExecute = true });
    }

    private void OnLogsChanged(object? sender, EventArgs e) => RunOnUi(() => _ = RefreshAsync());
    private void OnEntryWritten(object? sender, AutomationLogEntry entry) => RunOnUi(() =>
    {
        if (SelectedLog?.AutomationId != entry.AutomationId) return;
        _entries.Add(new AutomationLogEntryItem(entry));
        while (_entries.Count > 3000) _entries.RemoveAt(0);
    });

    private static void RunOnUi(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess()) action();
        else dispatcher.InvokeAsync(action);
    }
}

public sealed class AutomationLogEntryItem
{
    public AutomationLogEntryItem(AutomationLogEntry entry)
    {
        Timestamp = entry.Timestamp;
        Level = entry.Level;
        Message = entry.Message;
        Details = entry.Details ?? string.Empty;
    }
    public DateTimeOffset Timestamp { get; }
    public ExecutionLogLevel Level { get; }
    public string Message { get; }
    public string Details { get; }
    public string TimestampText => Timestamp.LocalDateTime.ToString("dd.MM.yyyy HH:mm:ss", System.Globalization.CultureInfo.GetCultureInfo("de-DE"));
}
