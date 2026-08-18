using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Common.ApplicationData;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using TaskAutomation.Logging;

namespace DesktopAutomationApp.ViewModels;

public sealed class AutomationLogsViewModel : ViewModelBase
{
    private const int MaxBufferedEntries = 3000;
    private readonly IAutomationLogService _service;
    private readonly ObservableRangeCollection<AutomationLogEntryItem> _entries = new();
    private AutomationLog? _selectedLog;
    private ExecutionLogLevel _selectedMinimumLevel = ExecutionLogLevel.Information;
    private string _searchText = string.Empty;
    private CancellationTokenSource? _loadCancellation;
    private bool _isLoading;
    private bool _isTruncated;

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

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (_isLoading == value) return;
            SetProperty(ref _isLoading, value);
            OnPropertyChanged(nameof(EntryStatusText));
        }
    }

    public string EntryStatusText => IsLoading
        ? Localization.Loc.Get("Execution.LogsLoading")
        : Localization.Loc.Format(_isTruncated ? "Execution.LogEntryCountTruncated" : "Execution.LogEntryCount", _entries.Count);

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
        var nextSelection = Logs.FirstOrDefault(log => log.AutomationId == selectedId) ?? Logs.FirstOrDefault();
        if (!Equals(_selectedLog, nextSelection)) SetProperty(ref _selectedLog, nextSelection, nameof(SelectedLog));
        await LoadEntriesAsync();
    }

    private async Task LoadEntriesAsync()
    {
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        _loadCancellation = new CancellationTokenSource();
        var cancellationToken = _loadCancellation.Token;
        if (SelectedLog == null)
        {
            _entries.Clear();
            _isTruncated = false;
            OnPropertyChanged(nameof(EntryStatusText));
            return;
        }
        var selectedId = SelectedLog.AutomationId;
        IsLoading = true;
        try
        {
            var entries = await _service.ReadEntriesAsync(selectedId, MaxBufferedEntries, cancellationToken);
            if (cancellationToken.IsCancellationRequested || SelectedLog?.AutomationId != selectedId) return;
            _isTruncated = entries.Count >= MaxBufferedEntries;
            _entries.ReplaceRange(entries.Select(entry => new AutomationLogEntryItem(entry)));
            Entries.Refresh();
            OnPropertyChanged(nameof(EntryStatusText));
        }
        catch (OperationCanceledException) { }
        finally
        {
            if (!cancellationToken.IsCancellationRequested && SelectedLog?.AutomationId == selectedId) IsLoading = false;
        }
    }

    private bool IsVisible(object item) => item is AutomationLogEntryItem entry
        && entry.Level >= SelectedMinimumLevel
        && (string.IsNullOrWhiteSpace(SearchText)
            || entry.Message.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)
            || entry.Details.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase));

    private void OpenLogFolder()
    {
        var directory = SelectedLog == null
            ? AppPaths.AutomationLogsDirectory
            : Path.GetDirectoryName(SelectedLog.FilePath)!;
        Directory.CreateDirectory(directory);
        Process.Start(new ProcessStartInfo(directory) { UseShellExecute = true });
    }

    private void OnLogsChanged(object? sender, EventArgs e) => RunOnUi(() => _ = RefreshAsync());
    private void OnEntryWritten(object? sender, AutomationLogEntry entry) => RunOnUi(() =>
    {
        if (SelectedLog?.AutomationId != entry.AutomationId) return;
        _entries.Add(new AutomationLogEntryItem(entry));
        while (_entries.Count > MaxBufferedEntries) { _entries.RemoveAt(0); _isTruncated = true; }
        OnPropertyChanged(nameof(EntryStatusText));
    });

    private static void RunOnUi(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess()) action();
        else dispatcher.InvokeAsync(action);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _loadCancellation?.Cancel();
            _loadCancellation?.Dispose();
            _service.EntryWritten -= OnEntryWritten;
            _service.LogsChanged -= OnLogsChanged;
        }
        base.Dispose(disposing);
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
