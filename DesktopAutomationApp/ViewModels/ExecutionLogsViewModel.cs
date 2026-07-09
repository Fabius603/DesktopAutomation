using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using TaskAutomation.Logging;

namespace DesktopAutomationApp.ViewModels
{
    public sealed class ExecutionLogsViewModel : ViewModelBase
    {
        private const int MaxBufferedEntries = 3000;
        private const int MaxBufferedSessions = 200;

        private readonly IExecutionLogService _logService;
        private readonly ObservableRangeCollection<ExecutionLogEntryItem> _entryBuffer = new();
        private ExecutionLogSessionItem? _selectedSession;
        private ExecutionLogLevel _selectedMinimumLevel = ExecutionLogLevel.Information;

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

        public ExecutionLogLevel SelectedMinimumLevel
        {
            get => _selectedMinimumLevel;
            set
            {
                if (Equals(_selectedMinimumLevel, value)) return;
                _selectedMinimumLevel = value;
                OnPropertyChanged();
                Entries.Refresh();
            }
        }

        public ExecutionLogSessionItem? SelectedSession
        {
            get => _selectedSession;
            set
            {
                SetProperty(ref _selectedSession, value);
                LoadEntries();
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

            _logService.SessionChanged += OnSessionChanged;
            _logService.EntryWritten += OnEntryWritten;
            Refresh();
        }

        private void Refresh()
        {
            RunOnUi(() =>
            {
                _logService.ReloadSessions();
                var selectedId = SelectedSession?.Id;
                Sessions.Clear();
                foreach (var session in _logService.Sessions.OrderByDescending(s => s.StartedAt))
                    Sessions.Add(new ExecutionLogSessionItem(session));
                TrimSessions();

                SelectedSession = selectedId.HasValue
                    ? Sessions.FirstOrDefault(s => s.Id == selectedId.Value) ?? Sessions.FirstOrDefault()
                    : Sessions.FirstOrDefault();
            });
        }

        private void LoadEntries()
        {
            if (SelectedSession == null)
            {
                _entryBuffer.Clear();
                return;
            }

            _entryBuffer.ReplaceRange(
                _logService.ReadEntries(SelectedSession.Id, MaxBufferedEntries)
                    .Select(entry => new ExecutionLogEntryItem(entry)));
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
            RunOnUi(() =>
            {
                if (SelectedSession?.Id != entry.SessionId)
                    return;

                _entryBuffer.Add(new ExecutionLogEntryItem(entry));
                while (_entryBuffer.Count > MaxBufferedEntries)
                    _entryBuffer.RemoveAt(0);
            });
        }

        private bool IsVisibleItem(object item)
            => item is ExecutionLogEntryItem entry && entry.Level >= SelectedMinimumLevel;

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
        public string StatusText => IsRunning ? "läuft" : "beendet";

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
