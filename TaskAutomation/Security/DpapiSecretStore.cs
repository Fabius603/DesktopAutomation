using System.Security;
using System.Security.Cryptography;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace TaskAutomation.Security;

public sealed class DpapiSecretStore : ISecretStore, IDisposable
{
    internal const int CurrentSchemaVersion = 1;
    internal const string CurrentProtection = "dpapi_current_user_v1";
    private const int MaximumNameLength = 120;
    private const int MaximumDescriptionLength = 2_000;
    private const string FileSuffix = ".secret.json";
    private static readonly byte[] AdditionalEntropy =
        Encoding.UTF8.GetBytes("DesktopAutomation.SecretStore.dpapi_current_user_v1");
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _directoryPath;
    private readonly ILogger<DpapiSecretStore> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _disposed;

    public DpapiSecretStore(string directoryPath, ILogger<DpapiSecretStore> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        _directoryPath = Path.GetFullPath(directoryPath);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Directory.CreateDirectory(_directoryPath);
    }

    public async Task<IReadOnlyList<SecretDescriptor>> ListAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var descriptors = new List<SecretDescriptor>();
            foreach (var filePath in Directory.EnumerateFiles(_directoryPath, $"*{FileSuffix}"))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!TryGetIdFromFilePath(filePath, out var id))
                {
                    _logger.LogWarning("Ignoring secret file with an invalid name: {FileName}", Path.GetFileName(filePath));
                    continue;
                }

                try
                {
                    var record = await ReadRecordFileAsync(filePath, id, cancellationToken).ConfigureAwait(false);
                    descriptors.Add(record.ToDescriptor());
                }
                catch (SecretStoreException exception)
                {
                    _logger.LogWarning(exception, "Ignoring unavailable secret metadata for {SecretId}", id);
                }
            }

            return descriptors
                .OrderBy(descriptor => descriptor.Name, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(descriptor => descriptor.Id)
                .ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<SecretDescriptor?> GetDescriptorAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        ValidateId(id);
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var record = await ReadRecordAsync(id, cancellationToken).ConfigureAwait(false);
            return record?.ToDescriptor();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<SecretReadResult> ReadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ValidateId(id);
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            DpapiSecretRecord? record;
            try
            {
                record = await ReadRecordAsync(id, cancellationToken).ConfigureAwait(false);
            }
            catch (SecretStoreException exception)
            {
                _logger.LogWarning(exception, "Secret {SecretId} is unavailable", id);
                return SecretReadResult.Unavailable(exception.ErrorCode);
            }

            if (record is null)
                return SecretReadResult.NotFound();

            try
            {
                var protectedBytes = Convert.FromBase64String(record.ProtectedValue);
                byte[]? plainBytes = null;
                try
                {
                    plainBytes = ProtectedData.Unprotect(
                        protectedBytes,
                        AdditionalEntropy,
                        DataProtectionScope.CurrentUser);
                    return SecretReadResult.Success(Encoding.UTF8.GetString(plainBytes));
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(protectedBytes);
                    if (plainBytes is not null)
                        CryptographicOperations.ZeroMemory(plainBytes);
                }
            }
            catch (Exception exception) when (exception is CryptographicException or FormatException)
            {
                _logger.LogWarning(exception, "Secret {SecretId} could not be decrypted", id);
                return SecretReadResult.Unavailable(SecretStoreErrorCodes.DecryptionFailed);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<SecretDescriptor> CreateAsync(
        SecretCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        var name = ValidateName(request.Name);
        var description = ValidateDescription(request.Description);
        var protectedValue = Protect(request.Value);
        var now = DateTime.UtcNow;
        var record = new DpapiSecretRecord
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Kind = SecretKind.Generic,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            ProtectedValue = protectedValue
        };

        ObjectDisposedException.ThrowIf(_disposed, this);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await WriteRecordAsync(record, cancellationToken).ConfigureAwait(false);
            return record.ToDescriptor();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<SecretDescriptor> UpdateMetadataAsync(
        Guid id,
        string name,
        string description,
        CancellationToken cancellationToken = default)
    {
        ValidateId(id);
        name = ValidateName(name);
        description = ValidateDescription(description);
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var record = await ReadRequiredRecordAsync(id, cancellationToken).ConfigureAwait(false);
            record.Name = name;
            record.Description = description;
            record.UpdatedAtUtc = DateTime.UtcNow;
            await WriteRecordAsync(record, cancellationToken).ConfigureAwait(false);
            return record.ToDescriptor();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ReplaceValueAsync(Guid id, string value, CancellationToken cancellationToken = default)
    {
        ValidateId(id);
        cancellationToken.ThrowIfCancellationRequested();
        var protectedValue = Protect(value);
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var record = await ReadRequiredRecordAsync(id, cancellationToken).ConfigureAwait(false);
            record.ProtectedValue = protectedValue;
            record.UpdatedAtUtc = DateTime.UtcNow;
            await WriteRecordAsync(record, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ValidateId(id);
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var filePath = FilePath(id);
            if (!File.Exists(filePath))
                return false;

            File.Delete(filePath);
            TryDelete(filePath + ".bak");
            TryDelete(filePath + ".tmp");
            return true;
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new SecretStoreException(
                SecretStoreErrorCodes.AccessDenied,
                $"Secret '{id}' could not be deleted.",
                exception);
        }
        catch (IOException exception)
        {
            throw new SecretStoreException(
                SecretStoreErrorCodes.StorageUnavailable,
                $"Secret '{id}' could not be deleted.",
                exception);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _gate.Dispose();
    }

    private async Task<DpapiSecretRecord> ReadRequiredRecordAsync(Guid id, CancellationToken cancellationToken) =>
        await ReadRecordAsync(id, cancellationToken).ConfigureAwait(false)
        ?? throw new SecretStoreException(SecretStoreErrorCodes.NotFound, $"Secret '{id}' does not exist.");

    private async Task<DpapiSecretRecord?> ReadRecordAsync(Guid id, CancellationToken cancellationToken)
    {
        var filePath = FilePath(id);
        if (!File.Exists(filePath))
            return null;
        return await ReadRecordFileAsync(filePath, id, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<DpapiSecretRecord> ReadRecordFileAsync(
        string filePath,
        Guid expectedId,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            var record = await JsonSerializer.DeserializeAsync<DpapiSecretRecord>(
                stream,
                JsonOptions,
                cancellationToken).ConfigureAwait(false);
            if (record is null
                || record.SchemaVersion != CurrentSchemaVersion
                || !string.Equals(record.Protection, CurrentProtection, StringComparison.Ordinal)
                || record.Id != expectedId
                || string.IsNullOrWhiteSpace(record.Name)
                || string.IsNullOrWhiteSpace(record.ProtectedValue)
                || !Enum.IsDefined(record.Kind))
            {
                throw new SecretStoreException(
                    SecretStoreErrorCodes.FileInvalid,
                    $"Secret file '{Path.GetFileName(filePath)}' has an unsupported or invalid format.");
            }
            return record;
        }
        catch (SecretStoreException)
        {
            throw;
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new SecretStoreException(
                SecretStoreErrorCodes.AccessDenied,
                $"Secret '{expectedId}' cannot be read.",
                exception);
        }
        catch (Exception exception) when (exception is IOException or JsonException)
        {
            throw new SecretStoreException(
                exception is JsonException ? SecretStoreErrorCodes.FileInvalid : SecretStoreErrorCodes.StorageUnavailable,
                $"Secret '{expectedId}' cannot be read.",
                exception);
        }
    }

    private async Task WriteRecordAsync(DpapiSecretRecord record, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(_directoryPath);
        var filePath = FilePath(record.Id);
        var temporaryPath = filePath + ".tmp";
        var backupPath = filePath + ".bak";
        try
        {
            await using (var stream = new FileStream(
                             temporaryPath,
                             FileMode.Create,
                             FileAccess.Write,
                             FileShare.None,
                             4096,
                             FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(stream, record, JsonOptions, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                stream.Flush(flushToDisk: true);
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (File.Exists(filePath))
            {
                TryDelete(backupPath);
                File.Replace(temporaryPath, filePath, backupPath, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(temporaryPath, filePath);
            }
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new SecretStoreException(
                SecretStoreErrorCodes.AccessDenied,
                $"Secret '{record.Id}' could not be written.",
                exception);
        }
        catch (IOException exception)
        {
            throw new SecretStoreException(
                SecretStoreErrorCodes.StorageUnavailable,
                $"Secret '{record.Id}' could not be written.",
                exception);
        }
        finally
        {
            TryDelete(temporaryPath);
        }
    }

    private static string Protect(string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);
        var plainBytes = Encoding.UTF8.GetBytes(value);
        try
        {
            byte[] protectedBytes;
            try
            {
                protectedBytes = ProtectedData.Protect(
                    plainBytes,
                    AdditionalEntropy,
                    DataProtectionScope.CurrentUser);
            }
            catch (CryptographicException exception)
            {
                throw new SecretStoreException(
                    SecretStoreErrorCodes.EncryptionFailed,
                    "The secret value could not be protected.",
                    exception);
            }
            try
            {
                return Convert.ToBase64String(protectedBytes);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(protectedBytes);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plainBytes);
        }
    }

    private string FilePath(Guid id) => Path.Combine(_directoryPath, $"{id:D}{FileSuffix}");

    private static bool TryGetIdFromFilePath(string filePath, out Guid id)
    {
        var fileName = Path.GetFileName(filePath);
        if (!fileName.EndsWith(FileSuffix, StringComparison.OrdinalIgnoreCase))
        {
            id = Guid.Empty;
            return false;
        }
        return Guid.TryParse(fileName[..^FileSuffix.Length], out id) && id != Guid.Empty;
    }

    private static string ValidateName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var trimmed = name.Trim();
        if (trimmed.Length > MaximumNameLength)
            throw new ArgumentOutOfRangeException(nameof(name), $"Secret names may not exceed {MaximumNameLength} characters.");
        return trimmed;
    }

    private static string ValidateDescription(string? description)
    {
        var trimmed = description?.Trim() ?? string.Empty;
        if (trimmed.Length > MaximumDescriptionLength)
            throw new ArgumentOutOfRangeException(nameof(description), $"Secret descriptions may not exceed {MaximumDescriptionLength} characters.");
        return trimmed;
    }

    private static void ValidateKind(SecretKind kind)
    {
        if (!Enum.IsDefined(kind))
            throw new ArgumentOutOfRangeException(nameof(kind));
    }

    private static void ValidateId(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("A secret ID must not be empty.", nameof(id));
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException)
        {
            // Best effort cleanup. The canonical file is never removed here.
        }
    }
}
