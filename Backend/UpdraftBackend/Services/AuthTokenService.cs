using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using UpdraftBackend.Data;
using UpdraftBackend.Domain;

namespace UpdraftBackend.Services;

public sealed record JwtOptions(string Issuer, string Audience, string SigningKey, TimeSpan AccessLifetime, TimeSpan RefreshLifetime);

public sealed record IssuedSession(Guid PlayerId, string AccessToken, string RefreshToken, DateTimeOffset AccessExpiresAt);

public sealed class AuthTokenService
{
    readonly JwtOptions _options;
    readonly SigningCredentials _credentials;

    public AuthTokenService(JwtOptions options)
    {
        _options = options;
        _credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
            SecurityAlgorithms.HmacSha256);
    }

    public async Task<IssuedSession> IssueAsync(Guid playerId, UpdraftDbContext db, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        db.PlayerSessions.Add(new PlayerSession
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            RefreshTokenHash = Hash(refreshToken),
            CreatedAt = now,
            ExpiresAt = now.Add(_options.RefreshLifetime),
        });
        await db.SaveChangesAsync(cancellationToken);
        return new IssuedSession(playerId, CreateAccessToken(playerId, now), refreshToken, now.Add(_options.AccessLifetime));
    }

    public async Task<IssuedSession?> RefreshAsync(string refreshToken, UpdraftDbContext db, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return null;

        var now = DateTimeOffset.UtcNow;
        var hash = Hash(refreshToken);
        var session = await db.PlayerSessions.SingleOrDefaultAsync(s => s.RefreshTokenHash == hash, cancellationToken);
        if (session is null || session.RevokedAt is not null || session.ExpiresAt <= now) return null;

        session.RevokedAt = now;
        return await IssueAsync(session.PlayerId, db, cancellationToken);
    }

    public async Task RevokePlayerAsync(Guid playerId, UpdraftDbContext db, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var active = await db.PlayerSessions
            .Where(s => s.PlayerId == playerId && s.RevokedAt == null && s.ExpiresAt > now)
            .ToListAsync(cancellationToken);
        foreach (var session in active) session.RevokedAt = now;
        await db.SaveChangesAsync(cancellationToken);
    }

    string CreateAccessToken(Guid playerId, DateTimeOffset now)
    {
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, playerId.ToString()),
                new Claim("player_id", playerId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            ],
            notBefore: now.UtcDateTime,
            expires: now.Add(_options.AccessLifetime).UtcDateTime,
            signingCredentials: _credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
