using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Common.Logging
{
    public static class LoggingBuilderExtensions
    {
        public static ILoggingBuilder AddCommonLogging(this ILoggingBuilder builder, string logFilePath)
        {
            // Optional: Konfiguration aus appsettings.json zusätzlich einbinden:
            // -> builder.AddConfiguration(configuration.GetSection("Logging"));

            builder.ClearProviders();

            // Eine Console-Variante reicht. Simple-Formatter direkt über AddConsole konfigurieren:
            builder.AddConsole(options => options.FormatterName = ConsoleFormatterNames.Simple)
                   .AddSimpleConsole(opts =>
                   {
                       opts.TimestampFormat = "[HH:mm:ss] ";
                       opts.IncludeScopes = false;
                   });

            // Dein File-Provider
            builder.AddProvider(new FileLoggerProvider(logFilePath));

            builder.SetMinimumLevel(LogLevel.Information);
            return builder;
        }
    }
}
