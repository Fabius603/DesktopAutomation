using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using TaskAutomation.Security;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Security;

public sealed class DpapiSecretStoreTests
{
    [Fact]
    public async Task Crud_RoundTripsMetadataAndValueWithoutPersistingPlaintext()
    {
        using var directory = new TemporaryDirectory();
        using var store = Store(directory.Path);
        const string originalValue = "home-assistant-token-super-secret";

        var created = await store.CreateAsync(new("Home Assistant", "Production token", originalValue));

        var filePath = Assert.Single(Directory.GetFiles(directory.Path, "*.secret.json"));
        var persisted = await File.ReadAllTextAsync(filePath);
        Assert.DoesNotContain(originalValue, persisted, StringComparison.Ordinal);
        Assert.DoesNotContain(Convert.ToBase64String(Encoding.UTF8.GetBytes(originalValue)), persisted, StringComparison.Ordinal);
        Assert.Equal(originalValue, (await store.ReadAsync(created.Id)).Value);

        var renamed = await store.UpdateMetadataAsync(created.Id, "Home Assistant Production", "Primary instance");
        Assert.Equal("Home Assistant Production", renamed.Name);
        Assert.Equal("Primary instance", renamed.Description);
        Assert.True(renamed.UpdatedAtUtc >= created.UpdatedAtUtc);

        await store.ReplaceValueAsync(created.Id, "replacement-value");
        Assert.Equal("replacement-value", (await store.ReadAsync(created.Id)).Value);
        Assert.Equal("Home Assistant Production", Assert.Single(await store.ListAsync()).Name);
        Assert.Empty(Directory.GetFiles(directory.Path, "*.tmp"));

        Assert.True(await store.DeleteAsync(created.Id));
        Assert.False(await store.DeleteAsync(created.Id));
        Assert.Equal(SecretReadStatus.NotFound, (await store.ReadAsync(created.Id)).Status);
    }

    [Fact]
    public async Task List_IgnoresCorruptRecordAndPreservesValidSecrets()
    {
        using var directory = new TemporaryDirectory();
        using var store = Store(directory.Path);
        var valid = await store.CreateAsync(new("Valid", "Description", "value"));
        await File.WriteAllTextAsync(
            Path.Combine(directory.Path, $"{Guid.NewGuid():D}.secret.json"),
            "{invalid-json");

        var descriptor = Assert.Single(await store.ListAsync());

        Assert.Equal(valid.Id, descriptor.Id);
        Assert.Equal("value", (await store.ReadAsync(valid.Id)).Value);
    }

    [Fact]
    public async Task List_LoadsExistingRecordWithoutDescription()
    {
        using var directory = new TemporaryDirectory();
        using var store = Store(directory.Path);
        var created = await store.CreateAsync(new("Legacy", "Temporary description", "value"));
        var filePath = Assert.Single(Directory.GetFiles(directory.Path, "*.secret.json"));
        var json = JsonNode.Parse(await File.ReadAllTextAsync(filePath))!.AsObject();
        json.Remove("description");
        await File.WriteAllTextAsync(filePath, json.ToJsonString());

        var descriptor = Assert.Single(await store.ListAsync());

        Assert.Equal(created.Id, descriptor.Id);
        Assert.Empty(descriptor.Description);
        Assert.Equal("value", (await store.ReadAsync(created.Id)).Value);
    }

    [Fact]
    public async Task Read_ReturnsUnavailableWhenProtectedValueWasTamperedWith()
    {
        using var directory = new TemporaryDirectory();
        using var store = Store(directory.Path);
        var created = await store.CreateAsync(new("API", string.Empty, "value"));
        var filePath = Assert.Single(Directory.GetFiles(directory.Path, "*.secret.json"));
        var json = JsonNode.Parse(await File.ReadAllTextAsync(filePath))!.AsObject();
        json["protected_value"] = Convert.ToBase64String([1, 2, 3, 4]);
        await File.WriteAllTextAsync(filePath, json.ToJsonString());

        var result = await store.ReadAsync(created.Id);

        Assert.Equal(SecretReadStatus.Unavailable, result.Status);
        Assert.Equal(SecretStoreErrorCodes.DecryptionFailed, result.ErrorCode);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task CancelledAndConcurrentOperations_DoNotCreatePartialOrDuplicateRecords()
    {
        using var directory = new TemporaryDirectory();
        using var store = Store(directory.Path);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            store.CreateAsync(new("Cancelled", string.Empty, "value"), cancellation.Token));
        Assert.Empty(Directory.GetFiles(directory.Path));

        var creates = Enumerable.Range(0, 12)
            .Select(index => store.CreateAsync(new($"Secret {index}", $"Description {index}", $"value-{index}")));
        var created = await Task.WhenAll(creates);

        Assert.Equal(12, created.Select(secret => secret.Id).Distinct().Count());
        Assert.Equal(12, (await store.ListAsync()).Count);
        Assert.Empty(Directory.GetFiles(directory.Path, "*.tmp"));
    }

    private static DpapiSecretStore Store(string path) =>
        new(path, NullLogger<DpapiSecretStore>.Instance);
}
