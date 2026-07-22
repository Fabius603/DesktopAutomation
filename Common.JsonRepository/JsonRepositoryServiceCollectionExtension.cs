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
            string directoryPath,
            JsonSerializerOptions jsonOptions,
            Func<T, string> keySelector)
            where T : class
        {
            if (!Path.IsPathFullyQualified(directoryPath))
                throw new ArgumentException("The repository directory must be an absolute path.", nameof(directoryPath));
            Directory.CreateDirectory(directoryPath);

            services.AddSingleton<IJsonRepository<T>>(_ =>
                new JsonRepository<T>(
                    new JsonRepositoryOptions
                    {
                        DirectoryPath = directoryPath,
                        JsonOptions = jsonOptions,
                    },
                    keySelector
                )
            );

            return services;
        }
    }
}
