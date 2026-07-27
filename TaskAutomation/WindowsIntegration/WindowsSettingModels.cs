using TaskAutomation.Steps;

namespace TaskAutomation.WindowsIntegration;

public sealed class WindowsSettingChange
{
    public string SettingId { get; init; } = string.Empty;
    public Dictionary<string, string?> Parameters { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public interface IWindowsSettingProvider
{
    Task<WindowsSettingChangeResult> ChangeAsync(WindowsSettingChange change, CancellationToken cancellationToken);
}

public interface IWindowsSystemSettingService
{
    Task<WindowsSettingChangeResult> ChangeAsync(WindowsSettingChange change, CancellationToken cancellationToken);
}

public sealed class WindowsSystemSettingService : IWindowsSystemSettingService
{
    private readonly IWindowsCapabilityCatalog _catalog;
    private readonly IWindowsSettingProvider _provider;

    public WindowsSystemSettingService(IWindowsCapabilityCatalog catalog, IWindowsSettingProvider provider)
    {
        _catalog = catalog;
        _provider = provider;
    }

    public Task<WindowsSettingChangeResult> ChangeAsync(
        WindowsSettingChange change,
        CancellationToken cancellationToken)
    {
        var capability = _catalog.Find(change.SettingId);
        if (capability?.SupportsSettingChange != true)
            return Task.FromResult(WindowsSettingChangeResult.Failed(
                change.SettingId, WindowsCapabilityStatus.Unsupported, "setting.unsupported",
                "The selected Windows setting is not supported."));

        var missing = (capability.Parameters ?? [])
            .FirstOrDefault(parameter => parameter.Required
                && (!change.Parameters.TryGetValue(parameter.Name, out var value)
                    || string.IsNullOrWhiteSpace(value)));
        if (missing is not null)
            return Task.FromResult(WindowsSettingChangeResult.Failed(
                change.SettingId, WindowsCapabilityStatus.Failed, "setting.missing_parameter",
                $"The required parameter '{missing.Name}' is missing."));

        return _provider.ChangeAsync(change, cancellationToken);
    }
}
