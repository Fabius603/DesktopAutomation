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

        if (!WindowsCapabilitySelectionRules.HasRequiredParameters(capability, change.Parameters))
            return Task.FromResult(WindowsSettingChangeResult.Failed(
                change.SettingId, WindowsCapabilityStatus.Failed, "setting.missing_parameter",
                "A required setting parameter is missing."));

        return _provider.ChangeAsync(change, cancellationToken);
    }
}
