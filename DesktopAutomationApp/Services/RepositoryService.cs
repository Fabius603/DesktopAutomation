using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Common.JsonRepository;

namespace DesktopAutomationApp.Services
{
    /// <summary>
    /// Zentraler Service für Repository-Management ohne komplexe Infrastruktur.
    /// Vereinfacht CRUD-Operationen für alle Entitäten.
    /// </summary>
    public interface IRepositoryService
    {
        Task<IReadOnlyList<T>> LoadAllAsync<T>() where T : class;
        Task<T?> LoadAsync<T>(string key) where T : class;
        Task SaveAsync<T>(T item) where T : class;
        Task SaveAllAsync<T>(IEnumerable<T> items) where T : class;
        Task DeleteAsync<T>(string key) where T : class;
        string GetDirectoryPath<T>() where T : class;

        event EventHandler<RepositoryChangedEventArgs>? DataChanged;
    }

    public class RepositoryChangedEventArgs : EventArgs
    {
        public Type EntityType { get; }
        public string Operation { get; }
        public string? EntityKey { get; }

        public RepositoryChangedEventArgs(Type entityType, string operation, string? entityKey = null)
        {
            EntityType = entityType;
            Operation = operation;
            EntityKey = entityKey;
        }
    }

    public class RepositoryService : IRepositoryService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RepositoryService> _logger;
        public event EventHandler<RepositoryChangedEventArgs>? DataChanged;

        public RepositoryService(IServiceProvider serviceProvider, ILogger<RepositoryService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        private IJsonRepository<T> GetRepository<T>() where T : class
        {
            return _serviceProvider.GetRequiredService<IJsonRepository<T>>();
        }

        public async Task<IReadOnlyList<T>> LoadAllAsync<T>() where T : class
        {
            try
            {
                var repository = GetRepository<T>();
                var items = await repository.LoadAllAsync();
                _logger.LogDebug("Geladen: {Count} {Type}", items.Count, typeof(T).Name);
                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Laden aller {Type}", typeof(T).Name);
                throw;
            }
        }

        public async Task<T?> LoadAsync<T>(string key) where T : class
        {
            try
            {
                var repository = GetRepository<T>();
                var item = await repository.LoadAsync(key);
                _logger.LogDebug("Geladen: {Type} mit Key '{Key}' {Found}", typeof(T).Name, key, item != null ? "gefunden" : "nicht gefunden");
                return item;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Laden von {Type} mit Key '{Key}'", typeof(T).Name, key);
                throw;
            }
        }

        public async Task SaveAsync<T>(T item) where T : class
        {
            try
            {
                var repository = GetRepository<T>();
                await repository.SaveAsync(item);
                _logger.LogInformation("Gespeichert: {Type}", typeof(T).Name);
                DataChanged?.Invoke(this, new RepositoryChangedEventArgs(typeof(T), "Saved"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Speichern von {Type}", typeof(T).Name);
                throw;
            }
        }

        public async Task SaveAllAsync<T>(IEnumerable<T> items) where T : class
        {
            try
            {
                var repository = GetRepository<T>();
                var itemList = items.ToList();
                await repository.SaveAllAsync(itemList);
                _logger.LogInformation("Alle gespeichert: {Count} {Type}", itemList.Count, typeof(T).Name);
                DataChanged?.Invoke(this, new RepositoryChangedEventArgs(typeof(T), "SavedAll"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Speichern aller {Type}", typeof(T).Name);
                throw;
            }
        }

        public async Task DeleteAsync<T>(string key) where T : class
        {
            try
            {
                var repository = GetRepository<T>();
                await repository.DeleteAsync(key);
                _logger.LogInformation("Gelöscht: {Type} mit Key '{Key}'", typeof(T).Name, key);
                DataChanged?.Invoke(this, new RepositoryChangedEventArgs(typeof(T), "Deleted", key));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Löschen von {Type} mit Key '{Key}'", typeof(T).Name, key);
                throw;
            }
        }

        public string GetDirectoryPath<T>() where T : class
        {
            return GetRepository<T>().DirectoryPath;
        }
    }
}
