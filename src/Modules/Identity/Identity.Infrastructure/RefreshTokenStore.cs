using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Identity.Application.Abstractions;

namespace Identity.Infrastructure;

public sealed class RefreshTokenStore : IRefreshTokenStore
{
    private readonly IdentityDbContext _context;

    public RefreshTokenStore(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task StoreAsync(Guid userId, string refreshToken, DateTimeOffset expiresAtUtc, CancellationToken ct)
    {
        _context.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = Hash(refreshToken),
            ExpiresAtUtc = expiresAtUtc,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await _context.SaveChangesAsync(ct);
    }

    public async Task<Guid?> ValidateAndRotateAsync(
        string refreshToken, string newRefreshToken, DateTimeOffset newExpiresAtUtc, CancellationToken ct)
    {
        var hash = Hash(refreshToken);
        var existing = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (existing is null || !existing.IsActive)
            return null;

        existing.RevokedAtUtc = DateTimeOffset.UtcNow;

        _context.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = existing.UserId,
            TokenHash = Hash(newRefreshToken),
            ExpiresAtUtc = newExpiresAtUtc,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await _context.SaveChangesAsync(ct);
        return existing.UserId;
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken ct)
    {
        var hash = Hash(refreshToken);
        var existing = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (existing is null)
            return;

        existing.RevokedAtUtc = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(ct);
    }

    private static string Hash(string token) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
