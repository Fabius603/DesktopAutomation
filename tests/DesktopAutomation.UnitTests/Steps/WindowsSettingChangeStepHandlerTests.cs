using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Tests.TestDoubles;
using TaskAutomation.WindowsIntegration;

namespace TaskAutomation.Tests.Steps;

public sealed class WindowsSettingChangeStepHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_ForwardsStableIdAndParametersAndStoresSuccess()
    {
        var service = new RecordingSettingService(new WindowsSettingChangeResult
        {
            Success = true,
            Status = WindowsCapabilityStatus.Success,
            SettingId = "audio.master_volume",
            PreviousValue = "25",
            AppliedValue = "60"
        });
        var context = new PipelineContextStub();
        var step = new WindowsSettingChangeStep
        {
            Id = "setting",
            Settings = new()
            {
                SettingId = "audio.master_volume",
                Parameters = new(StringComparer.OrdinalIgnoreCase) { ["VALUE"] = "60" }
            }
        };

        var result = Assert.IsType<WindowsSettingChangeResult>(
            await new WindowsSettingChangeStepHandler(service)
                .ExecuteAsync(step, context, CancellationToken.None));

        Assert.True(result.WasExecuted);
        Assert.True(result.Success);
        Assert.Equal("25", result.PreviousValue);
        Assert.Equal("60", result.AppliedValue);
        Assert.Equal("audio.master_volume", service.Changes.Single().SettingId);
        Assert.Equal("60", service.Changes.Single().Parameters["value"]);
        Assert.Same(result, context.Results.GetRaw("setting"));
    }

    [Fact]
    public async Task ExecuteAsync_PreservesConcreteFailureStatus()
    {
        var service = new RecordingSettingService(WindowsSettingChangeResult.Failed(
            "printer.default", WindowsCapabilityStatus.AccessDenied,
            "setting.access_denied", "denied"));

        var result = Assert.IsType<WindowsSettingChangeResult>(
            await new WindowsSettingChangeStepHandler(service).ExecuteAsync(
                new WindowsSettingChangeStep
                {
                    Settings = new()
                    {
                        SettingId = "printer.default",
                        Parameters = new() { ["printer_name"] = "Office" }
                    }
                },
                new PipelineContextStub(),
                CancellationToken.None));

        Assert.True(result.WasExecuted);
        Assert.False(result.Success);
        Assert.Equal(WindowsCapabilityStatus.AccessDenied, result.Status);
        Assert.Equal("setting.access_denied", result.ErrorCode);
    }

    [Fact]
    public async Task ExecuteAsync_CancelledTokenDoesNotChangeSetting()
    {
        var service = new RecordingSettingService(WindowsSettingChangeResult.Default);
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new WindowsSettingChangeStepHandler(service).ExecuteAsync(
                new WindowsSettingChangeStep(), new PipelineContextStub(), source.Token));

        Assert.Empty(service.Changes);
    }

    [Fact]
    public async Task Service_RejectsMissingRequiredParameterBeforeProvider()
    {
        var provider = new RecordingSettingProvider();
        var service = new WindowsSystemSettingService(new WindowsCapabilityCatalog(), provider);

        var result = await service.ChangeAsync(new WindowsSettingChange
        {
            SettingId = "audio.master_volume"
        }, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("setting.missing_parameter", result.ErrorCode);
        Assert.Empty(provider.Changes);
    }

    [Fact]
    public void Catalog_ExposesExactlyTheFirstFifteenSettingCapabilities()
    {
        var settings = new WindowsCapabilityCatalog().Capabilities
            .Where(capability => capability.SupportsSettingChange)
            .ToArray();

        Assert.Equal(15, settings.Length);
        Assert.All(settings, setting =>
        {
            Assert.False(string.IsNullOrWhiteSpace(setting.Id));
            Assert.NotEmpty(setting.Parameters ?? []);
            Assert.Equal(nameof(WindowsSettingChangeResult), setting.ResultTypeName);
        });
    }

    [Fact]
    public void Catalog_AssignsDynamicOptionsToAudioDevicesAndWlanProfile()
    {
        var catalog = new WindowsCapabilityCatalog();

        Assert.Equal(WindowsDynamicOptionSource.AudioRenderDevices,
            Parameter(catalog, "audio.default_output", "device_name").DynamicOptionSource);
        Assert.Equal(WindowsDynamicOptionSource.AudioCaptureDevices,
            Parameter(catalog, "audio.default_input", "device_name").DynamicOptionSource);
        Assert.Equal(WindowsDynamicOptionSource.WlanProfiles,
            Parameter(catalog, "network.wifi_connection", "profile").DynamicOptionSource);
        Assert.Equal(WindowsDynamicOptionSource.Displays,
            Parameter(catalog, "display.primary", "display_name").DynamicOptionSource);
        Assert.Null(Parameter(catalog, "audio.master_volume", "value").DynamicOptionSource);
    }

    [Fact]
    public void DynamicOptions_PreserveUnavailableSavedValueWithoutDuplicatingAvailableValue()
    {
        var discovered = new[]
        {
            new WindowsSettingOption("available", "Available device")
        };

        var unavailable = WindowsSettingOptionList.PreserveCurrent(
            discovered, "missing", "Missing device (unavailable)");
        var available = WindowsSettingOptionList.PreserveCurrent(
            discovered, "AVAILABLE", "Should not be used");

        Assert.Equal(["missing", "available"], unavailable.Select(option => option.Value));
        Assert.Equal("Missing device (unavailable)", unavailable[0].DisplayName);
        Assert.Single(available);
    }

    [Fact]
    public void AudioDeviceDisplayName_PrefersTheNameUsedByWindows()
    {
        var displayName = AudioDeviceDisplayName.Resolve(
            "soundcore V20i", "Headphones", "Intel Smart Sound Technology");

        Assert.Equal("soundcore V20i", displayName);
    }

    [Fact]
    public void AudioDeviceDisplayName_CombinesEndpointAndDriverAsFallback()
    {
        var displayName = AudioDeviceDisplayName.Resolve(
            null, "Lautsprecher", "Realtek(R) Audio");

        Assert.Equal("Lautsprecher (Realtek(R) Audio)", displayName);
    }

    [Fact]
    public void AudioOutputOptions_HideHandsFreeEndpointWhenStereoEndpointExists()
    {
        var options = new[]
        {
            new WindowsSettingOption("stereo", "soundcore V20i"),
            new WindowsSettingOption("hands-free", "soundcore V20i Hands-Free"),
            new WindowsSettingOption("speakers", "Lautsprecher")
        };

        var filtered = AudioDeviceOptionFilter.RemoveRedundantHandsFreeOutputs(options);

        Assert.Equal(["stereo", "speakers"], filtered.Select(option => option.Value));
    }

    [Fact]
    public void AudioOutputOptions_KeepHandsFreeEndpointWhenItIsTheOnlyAvailableEndpoint()
    {
        var options = new[]
        {
            new WindowsSettingOption("hands-free", "Telefoniegerät Hands-Free")
        };

        var filtered = AudioDeviceOptionFilter.RemoveRedundantHandsFreeOutputs(options);

        Assert.Single(filtered);
    }

    private static WindowsParameterDescriptor Parameter(
        WindowsCapabilityCatalog catalog,
        string capabilityId,
        string parameterName) =>
        Assert.Single(catalog.Find(capabilityId)!.Parameters!,
            parameter => parameter.Name == parameterName);

    private sealed class RecordingSettingService(params WindowsSettingChangeResult[] results)
        : IWindowsSystemSettingService
    {
        private readonly Queue<WindowsSettingChangeResult> _results = new(results);
        public List<WindowsSettingChange> Changes { get; } = [];

        public Task<WindowsSettingChangeResult> ChangeAsync(
            WindowsSettingChange change,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Changes.Add(change);
            return Task.FromResult(_results.Dequeue());
        }
    }

    private sealed class RecordingSettingProvider : IWindowsSettingProvider
    {
        public List<WindowsSettingChange> Changes { get; } = [];

        public Task<WindowsSettingChangeResult> ChangeAsync(
            WindowsSettingChange change,
            CancellationToken cancellationToken)
        {
            Changes.Add(change);
            return Task.FromResult(WindowsSettingChangeResult.Default);
        }
    }
}
