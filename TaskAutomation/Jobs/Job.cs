using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using OpenCvSharp;

namespace TaskAutomation.Jobs
{
    public sealed class Job
    {
        public const int DefaultEndPhaseTimeoutSeconds = 10;
        public const int MinEndPhaseTimeoutSeconds = 1;
        public const int MaxEndPhaseTimeoutSeconds = 3600;

        [JsonPropertyName("id")] public Guid Id { get; set; } = Guid.NewGuid();
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("repeating")] public bool Repeating { get; set; }
        [JsonPropertyName("startSteps")] public List<JobStep> StartSteps { get; set; } = new();
        [JsonPropertyName("steps")] public List<JobStep> Steps { get; set; } = new();
        [JsonPropertyName("endSteps")] public List<JobStep> EndSteps { get; set; } = new();
        [JsonPropertyName("endPhaseTimeoutSeconds")]
        public int EndPhaseTimeoutSeconds { get; set; } = DefaultEndPhaseTimeoutSeconds;

        [JsonIgnore]
        public int ActiveStepCount => EnumerateAllSteps().Count(s => s.IsEnabled && !IsFlowControlStep(s));

        public IEnumerable<JobStep> EnumerateAllSteps()
            => (StartSteps ?? []).Concat(Steps ?? []).Concat(EndSteps ?? []);

        private static bool IsFlowControlStep(JobStep step)
            => step is IfStep or ElseIfStep or ElseStep or EndIfStep;
    }
}
