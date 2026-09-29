using DesktopAutomation.Application.Interfaces;
using DesktopAutomationApp.Localization;
using DesktopAutomationApp.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using TaskAutomation.Security;

namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class CredentialsSettingsViewModelTests
{
    [Fact]
    public async Task CreateAndEditMetadata_AcceptsValueOnceAndNeverReadsItForDisplay()
    {
        var store = new RecordingSecretStore();
        var viewModel = ViewModel(store);
        viewModel.BeginCreate();
        viewModel.EditorName = "Home Assistant";
        viewModel.EditorDescription = "Production token";
        viewModel.SecretValue = "line one\nline two";

        await viewModel.SaveAsync();

        var created = Assert.Single(store.Descriptors.Values);
        Assert.Equal("Production token", created.Description);
        Assert.Equal("line one\nline two", store.Values[created.Id]);
        viewModel.BeginEdit();
        viewModel.EditorName = "Home Assistant Production";
        viewModel.EditorDescription = "Updated description";
        await viewModel.SaveAsync();

        Assert.Equal("Home Assistant Production", store.Descriptors[created.Id].Name);
        Assert.Equal("Updated description", store.Descriptors[created.Id].Description);
        Assert.Equal("line one\nline two", store.Values[created.Id]);
        Assert.Equal(0, store.ReadCount);
        Assert.Equal(0, store.ReplaceCount);
    }

    [Fact]
    public async Task ReplaceAndDelete_AcceptSingleValueAndUseExplicitConfirmation()
    {
        var store = new RecordingSecretStore();
        var dialogs = new DialogStub { ConfirmResult = true };
        var viewModel = ViewModel(store, dialogs);
        viewModel.BeginCreate();
        viewModel.EditorName = "Service";
        viewModel.EditorDescription = "API secret";
        viewModel.SecretValue = "initial";
        await viewModel.SaveAsync();

        var created = Assert.Single(store.Descriptors.Values);
        viewModel.BeginReplace();
        viewModel.SecretValue = "replacement";
        await viewModel.SaveAsync();
        Assert.Equal("replacement", store.Values[created.Id]);

        await viewModel.DeleteAsync();
        Assert.Empty(store.Descriptors);
        Assert.Equal(1, dialogs.ConfirmCount);
    }

    [Fact]
    public async Task QuickCreateSecret_RequiresNameAndValueAndReturnsOnlyDescriptor()
    {
        var store = new RecordingSecretStore();
        var viewModel = new QuickCreateSecretViewModel(store);

        Assert.False(viewModel.CanCreate);
        Assert.False(await viewModel.CreateAsync());

        viewModel.Name = "API token";
        viewModel.Description = "Created from a job input";
        viewModel.Value = "super-secret";

        Assert.True(await viewModel.CreateAsync());
        Assert.NotNull(viewModel.CreatedSecret);
        Assert.Equal("API token", viewModel.CreatedSecret!.Name);
        Assert.Equal(string.Empty, viewModel.Value);
        Assert.Equal("super-secret", store.Values[viewModel.CreatedSecret.Id]);
        Assert.Equal(0, store.ReadCount);
    }

    private static CredentialsSettingsViewModel ViewModel(RecordingSecretStore store, DialogStub? dialogs = null) =>
        new(store, dialogs ?? new DialogStub(), LocalizationService.Instance, NullLogger<CredentialsSettingsViewModel>.Instance);

    private sealed class RecordingSecretStore : ISecretStore
    {
        public Dictionary<Guid, SecretDescriptor> Descriptors { get; } = [];
        public Dictionary<Guid, string> Values { get; } = [];
        public int ReadCount { get; private set; }
        public int ReplaceCount { get; private set; }

        public Task<IReadOnlyList<SecretDescriptor>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SecretDescriptor>>(Descriptors.Values.ToArray());
        public Task<SecretDescriptor?> GetDescriptorAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Descriptors.GetValueOrDefault(id));
        public Task<SecretReadResult> ReadAsync(Guid id, CancellationToken cancellationToken = default)
        {
            ReadCount++;
            return Task.FromResult(Values.TryGetValue(id, out var value) ? SecretReadResult.Success(value) : SecretReadResult.NotFound());
        }
        public Task<SecretDescriptor> CreateAsync(SecretCreateRequest request, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var descriptor = new SecretDescriptor(Guid.NewGuid(), request.Name, request.Description, SecretKind.Generic, now, now);
            Descriptors[descriptor.Id] = descriptor;
            Values[descriptor.Id] = request.Value;
            return Task.FromResult(descriptor);
        }
        public Task<SecretDescriptor> UpdateMetadataAsync(Guid id, string name, string description, CancellationToken cancellationToken = default)
        {
            var descriptor = Descriptors[id] with { Name = name, Description = description, UpdatedAtUtc = DateTime.UtcNow };
            Descriptors[id] = descriptor;
            return Task.FromResult(descriptor);
        }
        public Task ReplaceValueAsync(Guid id, string value, CancellationToken cancellationToken = default)
        {
            ReplaceCount++;
            Values[id] = value;
            return Task.CompletedTask;
        }
        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Values.Remove(id);
            return Task.FromResult(Descriptors.Remove(id));
        }
    }

    private sealed class DialogStub : IDialogService
    {
        public bool ConfirmResult { get; init; }
        public int ConfirmCount { get; private set; }
        public Task<bool> ConfirmAsync(string message, string title)
        {
            ConfirmCount++;
            return Task.FromResult(ConfirmResult);
        }
        public Task<bool?> ConfirmWithCancelAsync(string message, string title) => Task.FromResult<bool?>(null);
        public Task<string?> AskForNameAsync(string title, string prompt, string? defaultValue = null) => Task.FromResult<string?>(null);
        public void ShowError(string message, string title) { }
    }
}
