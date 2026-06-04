using TaskAutomation.Jobs;

namespace DesktopAutomation.Application.Interfaces
{
    public interface IJobApplicationService
    {
        IReadOnlyDictionary<string, Job> Jobs { get; }
        Task<Job> CreateJobAsync(string name);
        Task SaveJobAsync(Job job);
        Task DeleteJobAsync(Guid id);
        Task ReloadAsync();
        string GetStoragePath();
    }
}
