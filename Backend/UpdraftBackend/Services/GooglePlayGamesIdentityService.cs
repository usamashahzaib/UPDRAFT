using System.Net.Http.Headers;
using System.Text.Json;

namespace UpdraftBackend.Services;

public sealed class GooglePlayGamesOptions
{
    public string ServerClientId { get; set; } = string.Empty;
    public string ServerClientSecret { get; set; } = string.Empty;
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ServerClientId) && !string.IsNullOrWhiteSpace(ServerClientSecret);
}

public interface IGooglePlayGamesIdentityVerifier
{
    Task<string?> VerifyPlayerIdAsync(string serverAuthCode, CancellationToken cancellationToken);
}

/// <summary>Exchanges a one-use Play Games server auth code and reads the authenticated player ID.</summary>
public sealed class GooglePlayGamesIdentityVerifier(
    HttpClient httpClient,
    Microsoft.Extensions.Options.IOptions<GooglePlayGamesOptions> options,
    ILogger<GooglePlayGamesIdentityVerifier> logger) : IGooglePlayGamesIdentityVerifier
{
    const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    const string PlayerEndpoint = "https://www.googleapis.com/games/v1/players/me";

    public async Task<string?> VerifyPlayerIdAsync(string serverAuthCode, CancellationToken cancellationToken)
    {
        if (!options.Value.IsConfigured || string.IsNullOrWhiteSpace(serverAuthCode) || serverAuthCode.Length > 4096)
            return null;

        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = serverAuthCode,
                ["client_id"] = options.Value.ServerClientId,
                ["client_secret"] = options.Value.ServerClientSecret,
                ["grant_type"] = "authorization_code",
            })
        };

        try
        {
            using var tokenResponse = await httpClient.SendAsync(tokenRequest, cancellationToken);
            if (!tokenResponse.IsSuccessStatusCode) return null;
            using var tokenJson = await JsonDocument.ParseAsync(await tokenResponse.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            if (!tokenJson.RootElement.TryGetProperty("access_token", out var accessTokenElement)) return null;
            string accessToken = accessTokenElement.GetString() ?? string.Empty;
            if (accessToken.Length is < 1 or > 8192) return null;

            using var playerRequest = new HttpRequestMessage(HttpMethod.Get, PlayerEndpoint);
            playerRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var playerResponse = await httpClient.SendAsync(playerRequest, cancellationToken);
            if (!playerResponse.IsSuccessStatusCode) return null;
            using var playerJson = await JsonDocument.ParseAsync(await playerResponse.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            if (!playerJson.RootElement.TryGetProperty("playerId", out var playerIdElement)) return null;
            string? playerId = playerIdElement.GetString();
            return string.IsNullOrWhiteSpace(playerId) || playerId.Length > 128 ? null : playerId;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning("Google Play Games identity verification request failed: {Message}", ex.Message);
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
