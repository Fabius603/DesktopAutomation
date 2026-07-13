using Common.JsonRepository;
using DesktopAutomation.Application.Interfaces;
using Microsoft.Extensions.Logging;
using TaskAutomation.Automations;

namespace DesktopAutomation.Application.Services
{
    public sealed class AutomationApplicationService : IAutomationApplicationService
    {
        private readonly IJsonRepository<AutomationDefinition> _repository;
        private readonly IAutomationEngine _engine;
        private readonly ILogger<AutomationApplicationService> _log;

        public AutomationApplicationService(
            IJsonRepository<AutomationDefinition> repository,
            IAutomationEngine engine,
            ILogger<AutomationApplicationService> log)
        {
            _repository = repository;
            _engine = engine;
            _log = log;
        }

        public async Task<IReadOnlyList<AutomationDefinition>> LoadAllAsync()
        {
            var automations = await _repository.LoadAllAsync().ConfigureAwait(false);
            foreach (var automation in automations)
                automation.Runtime = _engine.GetRuntimeInfo(automation.Id);
            return automations;
        }

        public async Task SaveAsync(AutomationDefinition automation)
        {
            ArgumentNullException.ThrowIfNull(automation);
            var persisted = await _repository.LoadAsync(automation.Id.ToString()).ConfigureAwait(false);
            if (persisted?.LastRunAt is { } persistedLast
                && (automation.LastRunAt is null || persistedLast > automation.LastRunAt.Value))
                automation.LastRunAt = persistedLast;
            automation.UpdatedAt = DateTimeOffset.Now;
            await _repository.SaveAsync(automation).ConfigureAwait(false);
            await _engine.ReloadAsync().ConfigureAwait(false);
            _log.LogInformation("Automation gespeichert und registriert: {Name}", automation.Name);
        }

        public async Task DeleteAsync(Guid id)
        {
            await _repository.DeleteAsync(id.ToString()).ConfigureAwait(false);
            await _engine.ReloadAsync().ConfigureAwait(false);
            _log.LogInformation("Automation gelöscht und deregistriert: {Id}", id);
        }

        public Task TriggerAsync(Guid id) => _engine.TriggerAsync(id);

        public string GetStoragePath() => _repository.DirectoryPath;
    }
}
