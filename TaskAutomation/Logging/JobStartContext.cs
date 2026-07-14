namespace TaskAutomation.Logging;

public enum JobStartSource
{
    Manual,
    Automation,
    Job,
    Application,
    Unknown
}

public sealed record JobStartContext(
    JobStartSource Source,
    string? SourceName = null,
    Guid? SourceId = null)
{
    public static JobStartContext Manual { get; } = new(JobStartSource.Manual);
    public static JobStartContext Unknown { get; } = new(JobStartSource.Unknown);
}
