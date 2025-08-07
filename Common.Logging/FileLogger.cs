using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Logging
{
    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly StreamWriter _writer;
        private readonly object _sync = new();

        public FileLoggerProvider(string relativePath)
        {
            // absoluten Pfad ermitteln und Verzeichnis anlegen
            var logFile = Path.Combine(AppContext.BaseDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(logFile)!);
            _writer = new StreamWriter(new FileStream(logFile, FileMode.Append, FileAccess.Write, FileShare.Read))
            {
                AutoFlush = true
            };
        }

        public ILogger CreateLogger(string categoryName) =>
            new FileLogger(categoryName, _writer, _sync);

        public void Dispose() => _writer.Dispose();
    }

    public class FileLogger : ILogger
    {
        private readonly string _category;
        private readonly StreamWriter _writer;
        private readonly object _sync;

        public FileLogger(string category, StreamWriter writer, object sync)
        {
            _category = category;
            _writer = writer;
            _sync = sync;
        }

        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
        public bool IsEnabled(LogLevel level) => level != LogLevel.None;

        public void Log<TState>(LogLevel level, EventId id, TState state,
                               Exception? ex, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(level)) return;

            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {_category}: {formatter(state, ex)}";
            lock (_sync)
            {
                _writer.WriteLine(line);
                if (ex != null) _writer.WriteLine(ex);
            }
        }

        private class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}
