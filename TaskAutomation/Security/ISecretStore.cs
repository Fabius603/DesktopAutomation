namespace TaskAutomation.Security;

public interface ISecretStore
{
    Task<IReadOnlyList<SecretDescriptor>> ListAsync(CancellationToken cancellationToken = default);
    Task<SecretDescriptor?> GetDescriptorAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SecretReadResult> ReadAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SecretDescriptor> CreateAsync(SecretCreateRequest request, CancellationToken cancellationToken = default);
    Task<SecretDescriptor> UpdateMetadataAsync(
        Guid id,
        string name,
        string description,
        CancellationToken cancellationToken = default);
    Task ReplaceValueAsync(Guid id, string value, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
