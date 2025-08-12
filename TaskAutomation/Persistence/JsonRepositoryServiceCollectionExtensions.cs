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
            string relativeFolderPath,     // z.B. "Configs/Jobs" ODER "Configs\\Jobs"
            string saveFileName,           // z.B. "jobs.json"
            JsonSerializerOptions jsonOptions,
            Func<T, string> keySelector)
            where T : class
        {
            // 1) Kombinieren + Normalisieren
            var baseDir = AppContext.BaseDirectory;
            var combined = Path.Combine(baseDir, relativeFolderPath);
            var folderPath = Path.GetFullPath(combined)
                                 .Replace('/', Path.DirectorySeparatorChar)
                                 .TrimEnd(Path.DirectorySeparatorChar);

            // 2) Ordner sicherstellen
            Directory.CreateDirectory(folderPath);

            // 3) Registrierung
            services.AddSingleton<IJsonRepository<T>>(_ =>
                new FileJsonRepository<T>(
                    new FolderJsonRepositoryOptions
                    {
                        FolderPath = folderPath,
                        SearchPattern = "*.json",
                        SaveFileName = saveFileName,
                        JsonOptions = jsonOptions,
                        CreateBackup = true
                    },
                    keySelector: keySelector
                )
            );

            return services;
        }
    }
}
