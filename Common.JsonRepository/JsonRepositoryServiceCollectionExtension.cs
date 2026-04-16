using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Common.JsonRepository
{
    public static class JsonRepositoryServiceCollectionExtensions
    {
        public static IServiceCollection AddJsonRepository<T>(
            this IServiceCollection services,
            string relativeFolderPath,     // z.B. "Configs/Jobs"
            JsonSerializerOptions jsonOptions,
            Func<T, string> keySelector)
            where T : class
        {
            var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var configDir = Path.Combine(baseDir, "DesktopAutomation", relativeFolderPath);
            Directory.CreateDirectory(configDir);

            services.AddSingleton<IJsonRepository<T>>(_ =>
                new JsonRepository<T>(
                    new JsonRepositoryOptions
                    {
                        DirectoryPath = configDir,
                        JsonOptions = jsonOptions,
                    },
                    keySelector
                )
            );

            return services;
        }
    }
}
