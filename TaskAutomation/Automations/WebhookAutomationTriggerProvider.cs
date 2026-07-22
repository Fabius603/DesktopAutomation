using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TaskAutomation.Automations;

public sealed class WebhookAutomationTriggerProvider : IAutomationTriggerProvider
{
    private const long MaxRequestBodySize = 64 * 1024;

    private sealed record Registration(Guid AutomationId, WebhookAutomationTrigger Trigger);

    private readonly ConcurrentDictionary<Guid, Registration> _registrations = new();
    private readonly SemaphoreSlim _serverGate = new(1, 1);
    private readonly ILogger<WebhookAutomationTriggerProvider> _log;
    private WebApplication? _application;
    private string _listenerSignature = string.Empty;
    private volatile bool _started;

    public WebhookAutomationTriggerProvider(ILogger<WebhookAutomationTriggerProvider> log) => _log = log;

    public IReadOnlyCollection<AutomationTriggerKind> SupportedKinds { get; } = [AutomationTriggerKind.Webhook];
    public event Action<Guid>? Triggered;

    public async Task StartAsync(CancellationToken ct = default)
    {
        _started = true;
        await RefreshServerAsync(ct).ConfigureAwait(false);
    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        _started = false;
        await _serverGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await StopServerCoreAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _serverGate.Release();
        }
    }

    public async Task RegisterAsync(AutomationDefinition automation, CancellationToken ct = default)
    {
        if (automation.Trigger is not WebhookAutomationTrigger trigger)
            return;

        _registrations[trigger.HookId] = new Registration(automation.Id, trigger);
        await RefreshServerAsync(ct).ConfigureAwait(false);
    }

    public async Task UnregisterAsync(Guid automationId, CancellationToken ct = default)
    {
        foreach (var registration in _registrations.Where(pair => pair.Value.AutomationId == automationId).ToArray())
            _registrations.TryRemove(registration.Key, out _);
        await RefreshServerAsync(ct).ConfigureAwait(false);
    }

    public DateTimeOffset? GetNextRun(Guid automationId) => null;

    private async Task RefreshServerAsync(CancellationToken ct)
    {
        if (!_started)
            return;

        await _serverGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var registrations = _registrations.Values.ToArray();
            var listeners = registrations
                .GroupBy(r => r.Trigger.Port)
                .Select(group => new
                {
                    Port = group.Key,
                    Lan = group.Any(r => r.Trigger.NetworkMode is WebhookNetworkMode.Lan or WebhookNetworkMode.Online)
                })
                .OrderBy(listener => listener.Port)
                .ToArray();
            var desiredSignature = string.Join(";", listeners.Select(listener => $"{listener.Port}:{listener.Lan}"));

            if (_application != null && desiredSignature == _listenerSignature)
                return;

            await StopServerCoreAsync(ct).ConfigureAwait(false);
            if (listeners.Length == 0)
                return;

            var builder = WebApplication.CreateSlimBuilder();
            builder.Logging.ClearProviders();
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Limits.MaxRequestBodySize = MaxRequestBodySize;
                foreach (var listener in listeners)
                {
                    if (listener.Lan)
                        options.ListenAnyIP(listener.Port);
                    else
                        options.Listen(IPAddress.Loopback, listener.Port);
                }
            });

            var application = builder.Build();
            application.MapPost("/api/v1/webhooks/{hookId:guid}", HandleRequestAsync);
            _application = application;
            await application.StartAsync(ct).ConfigureAwait(false);

            _listenerSignature = desiredSignature;
            _log.LogInformation("Webhook-Listener gestartet: {Listeners}.", desiredSignature);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Webhook-Listener konnte nicht gestartet werden.");
            await StopServerCoreAsync(CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            _serverGate.Release();
        }
    }

    private async Task HandleRequestAsync(HttpContext context)
    {
        if (!Guid.TryParse(context.Request.RouteValues["hookId"]?.ToString(), out var hookId)
            || !_registrations.TryGetValue(hookId, out var registration))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var trigger = registration.Trigger;
        if (trigger.NetworkMode == WebhookNetworkMode.Offline
            && context.Connection.RemoteIpAddress is { } remoteAddress
            && !IPAddress.IsLoopback(remoteAddress))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        var suppliedSecret = GetSuppliedSecret(context.Request);
        if (!SecretEquals(trigger.Secret, suppliedSecret))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers.WWWAuthenticate = "Bearer";
            return;
        }

        if (context.Request.ContentLength is > MaxRequestBodySize)
        {
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            return;
        }

        try
        {
            await context.Request.Body.CopyToAsync(Stream.Null, context.RequestAborted).ConfigureAwait(false);
        }
        catch (BadHttpRequestException ex) when (ex.StatusCode == StatusCodes.Status413PayloadTooLarge)
        {
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            return;
        }

        try
        {
            Triggered?.Invoke(registration.AutomationId);
            context.Response.StatusCode = StatusCodes.Status202Accepted;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Webhook {HookId} konnte Automation {AutomationId} nicht ausloesen.", hookId, registration.AutomationId);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        }
    }

    private static string? GetSuppliedSecret(HttpRequest request)
    {
        if (request.Headers.TryGetValue("X-Webhook-Secret", out var headerSecret))
            return headerSecret.ToString();

        var authorization = request.Headers.Authorization.ToString();
        const string bearerPrefix = "Bearer ";
        return authorization.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase)
            ? authorization[bearerPrefix.Length..].Trim()
            : null;
    }

    private static bool SecretEquals(string expected, string? supplied)
    {
        if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(supplied))
            return false;
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expected));
        var suppliedHash = SHA256.HashData(Encoding.UTF8.GetBytes(supplied));
        return CryptographicOperations.FixedTimeEquals(expectedHash, suppliedHash);
    }

    private async Task StopServerCoreAsync(CancellationToken ct)
    {
        var application = _application;
        _application = null;
        _listenerSignature = string.Empty;
        if (application == null)
            return;

        try
        {
            await application.StopAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            await application.DisposeAsync().ConfigureAwait(false);
        }
    }
}
