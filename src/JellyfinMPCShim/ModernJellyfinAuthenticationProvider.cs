using System.Text;
using Jellyfin.Sdk;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;

namespace JellyfinMPCShim;

/// <summary>
/// Provides Jellyfin 12.x-compatible <c>Authorization: MediaBrowser ...</c> headers
/// for all Kiota SDK requests and WebSocket/HTTP calls.
/// </summary>
internal sealed class ModernJellyfinAuthenticationProvider : IAuthenticationProvider
{
    private readonly JellyfinSdkSettings _settings;
    private readonly JellyfinClientIdentity _identity;

    public ModernJellyfinAuthenticationProvider(JellyfinSdkSettings settings, JellyfinClientIdentity identity)
    {
        _settings = settings;
        _identity = identity;
    }

    public Task AuthenticateRequestAsync(
        RequestInformation request,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        request.Headers.Remove("X-Emby-Authorization");
        request.Headers.Remove("X-Emby-Token");
        request.Headers.Remove("X-MediaBrowser-Token");
        request.Headers.TryAdd("Authorization", BuildAuthorizationHeader(_settings, _identity));

        return Task.CompletedTask;
    }

    internal static string BuildAuthorizationHeader(JellyfinSdkSettings settings, JellyfinClientIdentity identity)
    {
        var sb = new StringBuilder("MediaBrowser ");
        sb.Append($"Client=\"{Escape(identity.ClientName)}\", ");
        sb.Append($"Device=\"{Escape(identity.DeviceName)}\", ");
        sb.Append($"DeviceId=\"{Escape(identity.DeviceId)}\", ");
        sb.Append($"Version=\"{Escape(identity.ClientVersion)}\"");

        if (!string.IsNullOrEmpty(settings.AccessToken))
        {
            sb.Append($", Token=\"{Escape(settings.AccessToken)}\"");
        }

        return sb.ToString();
    }

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
             .Replace("\"", "\\\"", StringComparison.Ordinal);
}
