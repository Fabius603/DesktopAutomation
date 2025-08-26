using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Text.Json;
using TaskAutomation.Persistence;

namespace TaskAutomation.Persistence
{
    public static class JsonRepositoryServiceCollectionExtensions
    {
        public static IServiceCollection AddJsonRepository<T>(
            this IServiceCollection services,
            string relativeFolderPath,     // z.B. "Configs/Jobs"
            string saveFileName,           // z.B. "jobs.json"
            JsonSerializerOptions jsonOptions,
            Func<T, string> keySelector)
            where T : class
        {
            var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var configDir = Path.Combine(baseDir, "DesktopAutomation", relativeFolderPath);
            Directory.CreateDirectory(configDir);

            // NEU: eine Zieldatei statt Ordner+Pattern
            var filePath = Path.Combine(configDir, saveFileName);

            services.AddSingleton<IJsonRepository<T>>(_ =>
                new JsonRepository<T>(
                    new JsonRepositoryOptions
                    {
                        FilePath = filePath,
                        JsonOptions = jsonOptions,
                        CreateBackup = true,                // optional
                                                            // BackupSuffixFormat = "yyyyMMddHHmmssfff"
                    },
                    keySelector
                )
            );

            return services;
        }
    }

}
