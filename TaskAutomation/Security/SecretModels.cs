using System.Text.Json.Serialization;

namespace TaskAutomation.Security;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SecretKind
{
    Generic,
    ApiKey,
    BearerToken,
    UsernamePassword
}

public sealed record SecretReference
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonIgnore]
    public bool IsConfigured => Id != Guid.Empty;
}

public sealed record SecretDescriptor(
    Guid Id,
    string Name,
    string Description,
    SecretKind Kind,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record SecretCreateRequest(string Name, string Description, string Value);

public enum SecretReadStatus
{
    Success,
    NotFound,
    Unavailable
}

public sealed record SecretReadResult(
    SecretReadStatus Status,
    string? Value = null,
    string? ErrorCode = null)
{
    public static SecretReadResult Success(string value) => new(SecretReadStatus.Success, value);
    public static SecretReadResult NotFound() => new(SecretReadStatus.NotFound, ErrorCode: SecretStoreErrorCodes.NotFound);
    public static SecretReadResult Unavailable(string errorCode) =>
        new(SecretReadStatus.Unavailable, ErrorCode: errorCode);
}

public static class SecretStoreErrorCodes
{
    public const string NotFound = "secret_not_found";
    public const string FileInvalid = "secret_file_invalid";
    public const string EncryptionFailed = "secret_encryption_failed";
    public const string DecryptionFailed = "secret_decryption_failed";
    public const string AccessDenied = "secret_access_denied";
    public const string StorageUnavailable = "secret_storage_unavailable";
}

public sealed class SecretStoreException : Exception
{
    public SecretStoreException(string errorCode, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}
