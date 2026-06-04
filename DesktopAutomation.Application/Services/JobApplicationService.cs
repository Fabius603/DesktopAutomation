using Common.JsonRepository;
using DesktopAutomation.Application.Interfaces;
using Microsoft.Extensions.Logging;
using TaskAutomation.Jobs;

namespace DesktopAutomation.Application.Services
{
    public sealed class JobApplicationService : IJobApplicationService
    {
        private readonly IJsonRepository<Job> _repository;
        private readonly IJobExecutor _executor;
        private readonly ILogger<JobApplicationService> _log;

        public IReadOnlyDictionary<string, Job> Jobs => _executor.AllJobs;

        public JobApplicationService(IJsonRepository<Job> repository, IJobExecutor executor, ILogger<JobApplicationService> log)
        {
            _repository = repository;
            _executor = executor;
            _log = log;
        }

        public async Task<Job> CreateJobAsync(string name)
        {
            var job = new Job { Name = name, Repeating = false, Steps = new() };
            await _repository.SaveAsync(job);
            await _executor.ReloadJobsAsync();
            _log.LogInformation("Job erstellt: {Name}", name);
            return job;
        }

        public async Task SaveJobAsync(Job job)
        {
            await _repository.SaveAsync(job);
            await _executor.ReloadJobsAsync();
            _log.LogInformation("Job gespeichert: {Name}", job.Name);
        }

        public async Task DeleteJobAsync(Guid id)
        {
            await _repository.DeleteAsync(id.ToString());
            await _executor.ReloadJobsAsync();
            _log.LogInformation("Job gelöscht: {Id}", id);
        }

        public Task ReloadAsync() => _executor.ReloadJobsAsync();

        public string GetStoragePath() => _repository.DirectoryPath;
    }
}
