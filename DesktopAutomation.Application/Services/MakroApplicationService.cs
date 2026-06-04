using Common.JsonRepository;
using DesktopAutomation.Application.Interfaces;
using Microsoft.Extensions.Logging;
using TaskAutomation.Jobs;
using TaskAutomation.Makros;
using System.Collections.ObjectModel;

namespace DesktopAutomation.Application.Services
{
    public sealed class MakroApplicationService : IMakroApplicationService
    {
        private readonly IJsonRepository<Makro> _repository;
        private readonly IJobExecutor _executor;
        private readonly ILogger<MakroApplicationService> _log;

        public IReadOnlyDictionary<string, Makro> Makros => _executor.AllMakros;

        public MakroApplicationService(IJsonRepository<Makro> repository, IJobExecutor executor, ILogger<MakroApplicationService> log)
        {
            _repository = repository;
            _executor = executor;
            _log = log;
        }

        public async Task<Makro> CreateMakroAsync(string name)
        {
            var makro = new Makro { Name = name, Befehle = new ObservableCollection<TaskAutomation.Makros.MakroBefehl>() };
            await _repository.SaveAsync(makro);
            await _executor.ReloadMakrosAsync();
            _log.LogInformation("Makro erstellt: {Name}", name);
            return makro;
        }

        public async Task SaveMakroAsync(Makro makro)
        {
            await _repository.SaveAsync(makro);
            await _executor.ReloadMakrosAsync();
            _log.LogInformation("Makro gespeichert: {Name}", makro.Name);
        }

        public async Task DeleteMakroAsync(Guid id)
        {
            await _repository.DeleteAsync(id.ToString());
            await _executor.ReloadMakrosAsync();
            _log.LogInformation("Makro gelöscht: {Id}", id);
        }

        public Task ReloadAsync() => _executor.ReloadMakrosAsync();

        public string GetStoragePath() => _repository.DirectoryPath;
    }
}
