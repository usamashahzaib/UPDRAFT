using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace HotdropBackend.Services;

/// <summary>
/// Spec section 21: short-lived, account-bound join tickets. A ticket names one
/// player, one match and one team and expires quickly; the dedicated server
/// presents it back to /api/matches/validate-ticket before admitting a client.
/// Also issues the game-server's own service credential from a configured secret.
/// </summary>
public sealed class MatchTicketService
{
    public const string TicketAudience = "updraft-match-server";
    public static readonly TimeSpan TicketLifetime = TimeSpan.FromSeconds(90);
    public static readonly TimeSpan ServerTokenLifetime = TimeSpan.FromMinutes(10);

    readonly JwtOptions _options;
    readonly SigningCredentials _credentials;
    readonly TokenValidationParameters _ticketValidation;
    readonly byte[]? _serverKeyHash;

    public MatchTicketService(JwtOptions options, IConfiguration configuration)
    {
        _options = options;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
        _credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        _ticketValidation = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = options.Issuer,
            ValidateAudience = true, ValidAudience = TicketAudience,
            ValidateIssuerSigningKey = true, IssuerSigningKey = key,
            ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(10),
        };
        var serverKey = configuration["GameServer:ApiKey"];
        _serverKeyHash = string.IsNullOrWhiteSpace(serverKey) ? null : SHA256.HashData(Encoding.UTF8.GetBytes(serverKey));
    }

    public bool ServerAuthConfigured => _serverKeyHash != null;

    public (string Ticket, DateTimeOffset ExpiresAt) IssueJoinTicket(Guid playerId, Guid matchId, string team, bool hasBots)
    {
        var now = DateTimeOffset.UtcNow;
        var expires = now.Add(TicketLifetime);
        var token = new JwtSecurityToken(_options.Issuer, TicketAudience,
        [
            new Claim("player_id", playerId.ToString()),
            new Claim("match_id", matchId.ToString()),
            new Claim("team", team),
            new Claim("has_bots", hasBots ? "true" : "false"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        ], now.UtcDateTime, expires.UtcDateTime, _credentials);
        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    public ClaimsPrincipal? ValidateJoinTicket(string ticket)
    {
        try { return new JwtSecurityTokenHandler().ValidateToken(ticket, _ticketValidation, out _); }
        catch (Exception ex) when (ex is SecurityTokenException or ArgumentException) { return null; }
    }

    /// <summary>Constant-time check of the dedicated server's configured secret.</summary>
    public bool IsValidServerKey(string? presented)
    {
        if (_serverKeyHash == null || string.IsNullOrEmpty(presented)) return false;
        return CryptographicOperations.FixedTimeEquals(_serverKeyHash, SHA256.HashData(Encoding.UTF8.GetBytes(presented)));
    }

    public string IssueServerToken(string serverId)
    {
        var now = DateTimeOffset.UtcNow;
        var token = new JwtSecurityToken(_options.Issuer, _options.Audience,
        [
            new Claim("service", "game-server"),
            new Claim("server_id", serverId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        ], now.UtcDateTime, now.Add(ServerTokenLifetime).UtcDateTime, _credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

/// <summary>Spec sections 20/29: controlled game-server allocation settings.</summary>
public sealed class AllocatorOptions
{
    public string FusionRegion { get; set; } = "eu";
    public int MaxConcurrentMatches { get; set; } = 4;
}

/// <summary>Spec section 31: independent matchmaking and purchase kill switches, reloadable from configuration.</summary>
public sealed class FeatureFlags
{
    public bool MatchmakingEnabled { get; set; } = true;
    public bool PurchasesEnabled { get; set; } = false;
}

