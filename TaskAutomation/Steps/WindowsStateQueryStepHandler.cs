using TaskAutomation.Jobs;
using TaskAutomation.WindowsIntegration;

namespace TaskAutomation.Steps;

public sealed class WindowsStateQueryStepHandler : JobStepHandler<WindowsStateQueryStep, WindowsStateQueryResult>
{
    private readonly IWindowsSystemStateService _states;
    public WindowsStateQueryStepHandler(IWindowsSystemStateService states) => _states = states;

    protected override async Task<WindowsStateQueryResult> ExecuteCoreAsync(
        WindowsStateQueryStep step, IStepPipelineContext ctx, CancellationToken ct)
    {
        var snapshot = await _states.QueryAsync(new WindowsStateQuery
        {
            QueryType = step.Settings.QueryType,
            Parameters = new Dictionary<string, string?>(step.Settings.Parameters, StringComparer.OrdinalIgnoreCase)
        }, ct).ConfigureAwait(false);
        return new WindowsStateQueryResult
        {
            WasExecuted = true, Status = snapshot.Status, IsAvailable = snapshot.IsAvailable,
            CapturedAt = snapshot.CapturedAt, ErrorCode = snapshot.ErrorCode, ErrorMessage = snapshot.ErrorMessage,
            Exists = snapshot.Exists, IsActive = snapshot.IsActive, IsConnected = snapshot.IsConnected,
            IsEnabled = snapshot.IsEnabled, IsMuted = snapshot.IsMuted, IsCharging = snapshot.IsCharging,
            PendingRestart = snapshot.PendingRestart, Count = snapshot.Count, Value = snapshot.Value,
            Percentage = snapshot.Percentage, FreeSpaceGb = snapshot.FreeSpaceGb, Name = snapshot.Name,
            Id = snapshot.Id, Text = snapshot.Text, Path = snapshot.Path, Connectivity = snapshot.Connectivity,
            ConnectionType = snapshot.ConnectionType, PowerSource = snapshot.PowerSource,
            SessionState = snapshot.SessionState, DeviceState = snapshot.DeviceState, OnOffState = snapshot.OnOffState,
            Items = snapshot.Items
        };
    }

    protected override WindowsStateQueryResult CreateDefault() => WindowsStateQueryResult.Default;
}
