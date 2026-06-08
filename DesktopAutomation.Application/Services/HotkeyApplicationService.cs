using Common.JsonRepository;
using DesktopAutomation.Application.Interfaces;
using Microsoft.Extensions.Logging;
using TaskAutomation.Hotkeys;

namespace DesktopAutomation.Application.Services
{
    public sealed class HotkeyApplicationService : IHotkeyApplicationService
    {
        private readonly IJsonRepository<HotkeyDefinition> _repository;
        private readonly ILogger<HotkeyApplicationService> _log;

        public HotkeyApplicationService(IJsonRepository<HotkeyDefinition> repository, ILogger<HotkeyApplicationService> log)
        {
            _repository = repository;
            _log = log;
        }

        public async Task<IReadOnlyList<HotkeyDefinition>> LoadAllAsync()
        {
            var list = await _repository.LoadAllAsync();
            return list.ToList();
        }

        public async Task SaveAsync(HotkeyDefinition hotkey)
        {
            await _repository.SaveAsync(hotkey);
            _log.LogInformation("Hotkey gespeichert: {Name}", hotkey.Name);
        }

        public async Task DeleteAsync(Guid id)
        {
            await _repository.DeleteAsync(id.ToString());
            _log.LogInformation("Hotkey gelöscht: {Id}", id);
        }

        public string GetStoragePath() => _repository.DirectoryPath;
    }
}
