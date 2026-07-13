using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using System.Threading;
using System.Threading.Tasks;
using TaskAutomation.Logging;
using DesktopAutomationApp.Localization;

namespace DesktopAutomationApp.ViewModels
{
    public sealed class ExecutionLogsViewModel : ViewModelBase
    {
        private const int MaxBufferedEntries = 3000;
        private const int MaxBufferedSessions = 200;
        private const int MaxPendingLiveEntries = 5000;

        private readonly IExecutionLogService _logService;
        private readonly ObservableRangeCollection<ExecutionLogEntryItem> _entryBuffer = new();
        private readonly ConcurrentQueue<ExecutionLogEntry> _pendingEntries = new();
        private readonly DispatcherTimer _liveUpdateTimer;
        private ExecutionLogSessionItem? _selectedSession;
        private ExecutionLogLevel _selectedMinimumLevel = ExecutionLogLevel.Information;
        private CancellationTokenSource? _loadCancellation;
        private int _pendingEntryCount;
        private int _skippedLiveEntries;
        private bool _isLoading;
        private bool _isTruncated;
        private string _searchText = string.Empty;
        private DateTimeOffset _latestLoadedTimestamp = DateTimeOffset.MinValue;
        private int _refreshVersion;

        public ObservableCollection<ExecutionLogSessionItem> Sessions { get; } = new();
        public ICollectionView Entries { get; }
        public ObservableCollection<ExecutionLogLevel> AvailableLevels { get; } = new(
            new[]
            {
                ExecutionLogLevel.Debug,
                ExecutionLogLevel.Information,
                ExecutionLogLevel.Warning,
                ExecutionLogLevel.Error
            });

        public ICommand RefreshCommand { get; }
        public ICommand OpenLogFileCommand { get; }

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (_isLoading == value) return;
                _isLoading = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EntryStatusText));
            }
        }

        public string EntryStatusText
            => IsLoading
                ? Loc.Get("Execution.LogsLoading")
                : Loc.Format(
                    _isTruncated ? "Execution.LogEntryCountTruncated" : "Execution.LogEntryCount",
                    _entryBuffer.Count);

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (string.Equals(_searchText, value, StringComparison.Ordinal)) return;
                _searchText = value ?? string.Empty;
                OnPropertyChanged();
                Entries.Refresh();
            }
        }

        public ExecutionLogLevel SelectedMinimumLevel
        {
            get => _selectedMinimumLevel;
            set
            {
                if (Equals(_selectedMinimumLevel, value)) return;
                _selectedMinimumLevel = value;
                OnPropertyChanged();
                Entries.Refresh();
                OnPropertyChanged(nameof(EntryStatusText));
            }
        }

        public ExecutionLogSessionItem? SelectedSession
        {
            get => _selectedSession;
            set
            {
                if (Equals(_selectedSession, value)) return;
                SetProperty(ref _selectedSession, value);
                ClearPendingEntries();
                _ = LoadEntriesAsync();
                (OpenLogFileCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public ExecutionLogsViewModel(IExecutionLogService logService)
        {
            _logService = logService;
            Entries = CollectionViewSource.GetDefaultView(_entryBuffer);
            Entries.Filter = IsVisibleItem;
            RefreshCommand = new RelayCommand(Refresh);
            OpenLogFileCommand = new RelayCommand(OpenSelectedLogFile, () => SelectedSession != null);

            _liveUpdateTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(150)
            };
            _liveUpdateTimer.Tick += OnLiveUpdateTimerTick;
            _liveUpdateTimer.Start();

            _logService.SessionChanged += OnSessionChanged;
            _logService.EntryWritten += OnEntryWritten;
            Refresh();
        }

        private async void Refresh()
        {
            var version = Interlocked.Increment(ref _refreshVersion);
            var selectedId = SelectedSession?.Id;
            try
            {
                await Task.Run(_logService.ReloadSessions);
            }
            catch (IOException)
            {
                return;
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }
            if (version != _refreshVersion)
                return;

            Sessions.Clear();
            foreach (var session in _logService.Sessions.OrderByDescending(s => s.StartedAt))
                Sessions.Add(new ExecutionLogSessionItem(session));
            TrimSessions();

            SelectedSession = selectedId.HasValue
                ? Sessions.FirstOrDefault(s => s.Id == selectedId.Value) ?? Sessions.FirstOrDefault()
                : Sessions.FirstOrDefault();
        }

        private async Task LoadEntriesAsync()
        {
            _loadCancellation?.Cancel();
            _loadCancellation?.Dispose();
            _loadCancellation = new CancellationTokenSource();
            var cancellationToken = _loadCancellation.Token;

            if (SelectedSession == null)
            {
                _entryBuffer.Clear();
                _isTruncated = false;
                IsLoading = false;
                OnPropertyChanged(nameof(EntryStatusText));
                return;
            }

            var sessionId = SelectedSession.Id;
            IsLoading = true;
            try
            {
                var entries = await _logService.ReadEntriesAsync(sessionId, MaxBufferedEntries, cancellationToken);
                if (cancellationToken.IsCancellationRequested || SelectedSession?.Id != sessionId)
                    return;

                _isTruncated = entries.Count >= MaxBufferedEntries;
                _entryBuffer.ReplaceRange(entries.Select(entry => new ExecutionLogEntryItem(entry)));
                _latestLoadedTimestamp = entries.Count > 0
                    ? entries.Max(entry => entry.Timestamp)
                    : DateTimeOffset.MinValue;
                Entries.Refresh();
                OnPropertyChanged(nameof(EntryStatusText));
            }
            catch (OperationCanceledException)
            {
            }
            catch (IOException)
            {
                if (SelectedSession?.Id == sessionId)
                {
                    _entryBuffer.Clear();
                    _isTruncated = false;
                    OnPropertyChanged(nameof(EntryStatusText));
                }
            }
            catch (UnauthorizedAccessException)
            {
                if (SelectedSession?.Id == sessionId)
                {
                    _entryBuffer.Clear();
                    _isTruncated = false;
                    OnPropertyChanged(nameof(EntryStatusText));
                }
            }
            finally
            {
                if (!cancellationToken.IsCancellationRequested && SelectedSession?.Id == sessionId)
                    IsLoading = false;
            }
        }

        private void OpenSelectedLogFile()
        {
            if (SelectedSession == null) return;

            var directory = Path.GetDirectoryName(SelectedSession.FilePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            if (!File.Exists(SelectedSession.FilePath))
                File.WriteAllText(SelectedSession.FilePath, string.Empty);

            Process.Start(new ProcessStartInfo(SelectedSession.FilePath)
            {
                UseShellExecute = true
            });
        }

        private void OnSessionChanged(object? sender, ExecutionLogSession session)
        {
            RunOnUi(() =>
            {
                var existing = Sessions.FirstOrDefault(s => s.Id == session.Id);
                if (existing == null)
                {
                    var item = new ExecutionLogSessionItem(session);
                    Sessions.Insert(0, item);
                    TrimSessions();
                    if (SelectedSession == null || item.IsRunning && !SelectedSession.IsRunning)
                        SelectedSession = item;
                    return;
                }

                existing.Update(session);
            });
        }

        private void OnEntryWritten(object? sender, ExecutionLogEntry entry)
        {
            _pendingEntries.Enqueue(entry);
            var pendingCount = Interlocked.Increment(ref _pendingEntryCount);
            while (pendingCount > MaxPendingLiveEntries && _pendingEntries.TryDequeue(out _))
            {
                Interlocked.Decrement(ref _pendingEntryCount);
                Interlocked.Increment(ref _skippedLiveEntries);
                pendingCount--;
            }
        }

        private void OnLiveUpdateTimerTick(object? sender, EventArgs e)
        {
            if (SelectedSession == null || IsLoading)
                return;

            var sessionId = SelectedSession.Id;
            var additions = new List<ExecutionLogEntryItem>();
            while (_pendingEntries.TryDequeue(out var entry))
            {
                Interlocked.Decrement(ref _pendingEntryCount);
                if (entry.SessionId == sessionId && entry.Timestamp > _latestLoadedTimestamp)
                    additions.Add(new ExecutionLogEntryItem(entry));
            }

            if (additions.Count == 0 && Interlocked.CompareExchange(ref _skippedLiveEntries, 0, 0) == 0)
                return;

            if (Interlocked.Exchange(ref _skippedLiveEntries, 0) > 0)
                _isTruncated = true;

            if (additions.Count > 0)
            {
                _isTruncated |= _entryBuffer.AddRangeBounded(additions, MaxBufferedEntries);
                _latestLoadedTimestamp = additions.Max(entry => entry.Timestamp);
            }

            Entries.Refresh();
            OnPropertyChanged(nameof(EntryStatusText));
        }

        private void ClearPendingEntries()
        {
            while (_pendingEntries.TryDequeue(out _))
                Interlocked.Decrement(ref _pendingEntryCount);
            Interlocked.Exchange(ref _skippedLiveEntries, 0);
            _latestLoadedTimestamp = DateTimeOffset.MinValue;
        }

        private bool IsVisibleItem(object item)
        {
            if (item is not ExecutionLogEntryItem entry || entry.Level < SelectedMinimumLevel)
                return false;

            if (string.IsNullOrWhiteSpace(SearchText))
                return true;

            return entry.Message.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)
                || entry.DisplayDetails.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)
                || entry.StepType?.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) == true;
        }

        private void TrimSessions()
        {
            while (Sessions.Count > MaxBufferedSessions)
            {
                var removed = Sessions[^1];
                if (SelectedSession?.Id == removed.Id)
                    SelectedSession = Sessions.Count > 1 ? Sessions[0] : null;
                Sessions.RemoveAt(Sessions.Count - 1);
            }
        }

        private static void RunOnUi(Action action)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
                action();
            else
                dispatcher.InvokeAsync(action);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _liveUpdateTimer.Stop();
                _liveUpdateTimer.Tick -= OnLiveUpdateTimerTick;
                _loadCancellation?.Cancel();
                _loadCancellation?.Dispose();
                _logService.SessionChanged -= OnSessionChanged;
                _logService.EntryWritten -= OnEntryWritten;
            }

            base.Dispose(disposing);
        }
    }

    public sealed class ExecutionLogSessionItem : ViewModelBase
    {
        private DateTimeOffset? _endedAt;

        public ExecutionLogSessionItem(ExecutionLogSession session)
        {
            Id = session.Id;
            Kind = session.Kind;
            SourceId = session.SourceId;
            Name = session.Name;
            FilePath = session.FilePath;
            StartedAt = session.StartedAt;
            _endedAt = session.EndedAt;
        }

        public Guid Id { get; }
        public ExecutionLogKind Kind { get; }
        public Guid SourceId { get; }
        public string Name { get; }
        public string FilePath { get; }
        public DateTimeOffset StartedAt { get; }

        public DateTimeOffset? EndedAt
        {
            get => _endedAt;
            private set
            {
                SetProperty(ref _endedAt, value);
                OnPropertyChanged(nameof(IsRunning));
                OnPropertyChanged(nameof(StatusText));
            }
        }

        public bool IsRunning => EndedAt is null;
        public string StatusText => Loc.Get(IsRunning ? "Execution.Running" : "Execution.Finished");

        public void Update(ExecutionLogSession session)
        {
            EndedAt = session.EndedAt;
        }
    }

    public sealed class ExecutionLogEntryItem
    {
        public ExecutionLogEntryItem(ExecutionLogEntry entry)
        {
            Timestamp = entry.Timestamp;
            Level = entry.Level;
            StepType = entry.StepType;
            Message = entry.Message;
            DisplayDetails = BuildDetails(entry);
        }

        public DateTimeOffset Timestamp { get; }
        public ExecutionLogLevel Level { get; }
        public string? StepType { get; }
        public string Message { get; }
        public string DisplayDetails { get; }

        private static string BuildDetails(ExecutionLogEntry entry)
        {
            var duration = entry.DurationMs.HasValue ? $"Dauer={entry.DurationMs.Value} ms" : null;
            if (string.IsNullOrWhiteSpace(entry.Details))
                return duration ?? string.Empty;

            return duration == null
                ? entry.Details
                : $"{duration}, {entry.Details}";
        }
    }

    internal sealed class ObservableRangeCollection<T> : ObservableCollection<T>
    {
        public bool AddRangeBounded(IEnumerable<T> items, int maximumCount)
        {
            CheckReentrancy();
            foreach (var item in items)
                Items.Add(item);

            var removeCount = Math.Max(0, Items.Count - maximumCount);
            for (var index = 0; index < removeCount; index++)
                Items.RemoveAt(0);

            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            return removeCount > 0;
        }

        public void ReplaceRange(IEnumerable<T> items)
        {
            CheckReentrancy();
            Items.Clear();
            foreach (var item in items)
                Items.Add(item);

            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
