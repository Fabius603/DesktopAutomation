using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Common.Logging
{
    public static class Log
    {
        private static readonly ILoggerFactory _factory;

        static Log()
        {
            _factory = LoggerFactory.Create(builder =>
            {
                builder.ClearProviders();

                builder
                    .AddConsole(options =>
                    {
                        options.FormatterName = ConsoleFormatterNames.Simple;
                    })
                    .AddSimpleConsole(opts =>
                    {
                        opts.TimestampFormat = "[HH:mm:ss] ";
                        opts.IncludeScopes = false;
                    });

                builder.AddProvider(new FileLoggerProvider("logs/app.log"));

                builder.SetMinimumLevel(LogLevel.Information);
            });
        }

        /// <summary>
        /// Erstellt einen Logger mit freier Kategorie (Name).
        /// </summary>
        public static ILogger Create(string category) =>
            _factory.CreateLogger(category);

        /// <summary>
        /// Erstellt einen Logger für den Typ T.
        /// </summary>
        public static ILogger<T> Create<T>() =>
            _factory.CreateLogger<T>();
    }
}
