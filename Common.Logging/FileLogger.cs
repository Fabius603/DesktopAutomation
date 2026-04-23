using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Common.Logging
{
    /// <summary>
    /// File-Logger-Provider mit asynchronem Schreib-Queue.
    /// Log-Aufrufe blockieren den Aufrufer nicht – Nachrichten werden in einen
    /// Channel eingereiht und von einem Hintergrund-Task in die Datei geschrieben.
    /// </summary>
    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly Channel<string> _channel;
        private readonly Task _writerTask;
        private readonly StreamWriter _writer;
        private readonly LogLevel _minLevel;

        public FileLoggerProvider(string relativePath, LogLevel minLevel = LogLevel.Information)
        {
            _minLevel = minLevel;

            var logFile = Path.Combine(AppContext.BaseDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(logFile)!);

            // Großer Schreibpuffer (64 KB) → weniger Kernel-Aufrufe
            _writer = new StreamWriter(
                new FileStream(logFile, FileMode.Append, FileAccess.Write, FileShare.Read, 65536),
                System.Text.Encoding.UTF8, 65536)
            {
                AutoFlush = false
            };

            // Bounded Channel: bei Überlauf werden älteste Einträge verworfen statt zu blockieren
            _channel = Channel.CreateBounded<string>(new BoundedChannelOptions(8192)
            {
                FullMode          = BoundedChannelFullMode.DropOldest,
                SingleReader      = true,
                AllowSynchronousContinuations = false
            });

            _writerTask = Task.Run(WriteLoopAsync);
        }

        private async Task WriteLoopAsync()
        {
            await foreach (var line in _channel.Reader.ReadAllAsync())
            {
                _writer.WriteLine(line);

                // Flush nur wenn der Channel leer ist – spart Syscalls im Burst
                if (!_channel.Reader.TryPeek(out _))
                    await _writer.FlushAsync();
            }

            // Kanal geschlossen → letzten Flush und fertig
            await _writer.FlushAsync();
        }

        public ILogger CreateLogger(string categoryName) =>
            new FileLogger(categoryName, _channel.Writer, _minLevel);

        public void Dispose()
        {
            _channel.Writer.TryComplete();
            // Blockierende Wartezeit: max. 2 s damit beim App-Shutdown alles geschrieben wird
            _writerTask.Wait(TimeSpan.FromSeconds(2));
            _writer.Flush();
            _writer.Dispose();
        }
    }

    public class FileLogger : ILogger
    {
        private readonly string _category;
        private readonly ChannelWriter<string> _channelWriter;
        private readonly LogLevel _minLevel;

        public FileLogger(string category, ChannelWriter<string> channelWriter, LogLevel minLevel)
        {
            _category      = category;
            _channelWriter = channelWriter;
            _minLevel      = minLevel;
        }

        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

        public bool IsEnabled(LogLevel level) =>
            level != LogLevel.None && level >= _minLevel;

        public void Log<TState>(LogLevel level, EventId id, TState state,
                               Exception? ex, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(level)) return;

            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level,-11}] {_category}: {formatter(state, ex)}";

            // TryWrite ist lock-frei und non-blocking – kehrt sofort zurück
            _channelWriter.TryWrite(line);
            if (ex != null)
                _channelWriter.TryWrite(ex.ToString());
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}
