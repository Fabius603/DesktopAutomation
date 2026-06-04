using TaskAutomation.Makros;

namespace DesktopAutomation.Application.Interfaces
{
    public interface IMakroApplicationService
    {
        IReadOnlyDictionary<string, Makro> Makros { get; }
        Task<Makro> CreateMakroAsync(string name);
        Task SaveMakroAsync(Makro makro);
        Task DeleteMakroAsync(Guid id);
        Task ReloadAsync();
        string GetStoragePath();
    }
}
