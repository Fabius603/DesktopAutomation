using Microsoft.Extensions.Logging.Abstractions;
using TaskAutomation.WindowsIntegration;

namespace TaskAutomation.Tests.WindowsIntegration;

public sealed class WindowsSystemStateServiceTests
{
    [Fact]
    public async Task QueryAsync_SelectsSupportingProviderCaseInsensitively()
    {
        var provider = new ProviderStub("audio.volume", new WindowsStateSnapshot { Percentage = 33 });
        var service = new WindowsSystemStateService([provider]);
        var result = await service.QueryAsync(new WindowsStateQuery { QueryType = "AUDIO.VOLUME" });
        Assert.Equal(33, result.Percentage);
        Assert.Equal(1, provider.CallCount);
    }

    [Fact]
    public async Task QueryAsync_UnknownQuery_ReturnsUnsupportedWithoutCallingProvider()
    {
        var provider = new ProviderStub("audio.volume", new WindowsStateSnapshot());
        var result = await new WindowsSystemStateService([provider])
            .QueryAsync(new WindowsStateQuery { QueryType = "unknown" });
        Assert.Equal(WindowsCapabilityStatus.Unsupported, result.Status);
        Assert.False(result.IsAvailable);
        Assert.Equal("UNSUPPORTED_QUERY", result.ErrorCode);
        Assert.Equal(0, provider.CallCount);
    }

    [Fact]
    public void SnapshotFingerprint_IgnoresCaptureTimeButIncludesState()
    {
        var first = new WindowsStateSnapshot { CapturedAt = DateTime.UtcNow, Percentage = 10 };
        var later = first with { CapturedAt = DateTime.UtcNow.AddHours(1) };
        var changed = later with { Percentage = 11 };
        Assert.Equal(first.Fingerprint(), later.Fingerprint());
        Assert.NotEqual(first.Fingerprint(), changed.Fingerprint());
    }

    [Fact]
    public void CapabilityCatalog_FindIsCaseInsensitiveAndEveryIdIsUnique()
    {
        var catalog = new WindowsCapabilityCatalog();
        Assert.NotNull(catalog.Find("AUDIO.VOLUME"));
        Assert.Equal(catalog.Capabilities.Count,
            catalog.Capabilities.Select(capability => capability.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    private sealed class ProviderStub(string query, WindowsStateSnapshot snapshot) : IWindowsStateProvider
    {
        public IReadOnlyCollection<string> SupportedQueries { get; } = [query];
        public int CallCount { get; private set; }
        public Task<WindowsStateSnapshot> QueryAsync(WindowsStateQuery query, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(snapshot);
        }
    }
}
