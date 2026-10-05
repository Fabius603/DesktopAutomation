using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace TaskAutomation.Logging;

/// <summary>One privacy boundary for disk, live events and exports. Never register secrets as log parameters.</summary>
public sealed partial class LogPrivacy
{
    private readonly ConcurrentDictionary<string, byte> _knownSecrets = new(StringComparer.Ordinal);
    public void RegisterSecrets(IEnumerable<string> values)
    {
        foreach (var value in values.Where(value => !string.IsNullOrEmpty(value))) _knownSecrets.TryAdd(value, 0);
    }

    public string? Sanitize(string? value)
    {
        if (value is null) return null;
        foreach (var secret in _knownSecrets.Keys.OrderByDescending(value => value.Length))
            value = value.Replace(secret, "[redacted]", StringComparison.Ordinal);
        value = CredentialAssignment().Replace(value, "$1[redacted]");
        value = BearerCredential().Replace(value, "Bearer [redacted]");
        return value.Length > 16000 ? value[..16000] + " [truncated]" : value;
    }

    public LogEvent Sanitize(LogEvent entry) => entry with
    {
        Message = Sanitize(entry.Message) ?? "",
        Details = Sanitize(entry.Details),
        SourceName = Sanitize(entry.SourceName) ?? "",
        Parameters = entry.Parameters.ToDictionary(pair => pair.Key,
            pair => SensitiveKey().IsMatch(pair.Key) ? "[redacted]" : Sanitize(pair.Value))
    };

    public LogRun Sanitize(LogRun run) => run with
    {
        Name = Sanitize(run.Name) ?? "",
        OriginName = Sanitize(run.OriginName),
        CompletionReason = Sanitize(run.CompletionReason),
        Origin = Sanitize(run.Origin) ?? "Unknown",
        Steps = run.Steps.ToArray()
    };

    [GeneratedRegex(@"(?i)((?:password|passwd|secret|token|api[-_]?key|authorization)\s*[=:]\s*)(?:""[^""]*""|'[^']*'|[^\s;,]+)")]
    private static partial Regex CredentialAssignment();
    [GeneratedRegex(@"(?i)\bBearer\s+[^\s;,]+")]
    private static partial Regex BearerCredential();
    [GeneratedRegex(@"(?i)password|passwd|secret|token|api[-_]?key|authorization|payload|body")]
    private static partial Regex SensitiveKey();
}
