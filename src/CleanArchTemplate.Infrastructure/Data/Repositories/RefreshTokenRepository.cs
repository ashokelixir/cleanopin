using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.Interfaces;
using CleanArchTemplate.Infrastructure.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace CleanArchTemplate.Infrastructure.Data.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<RefreshToken> _dbSet;

    public RefreshTokenRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = _context.Set<RefreshToken>();
    }

    public async Task<RefreshToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);
    }

    public async Task<IEnumerable<RefreshToken>> GetActiveTokensByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(rt => rt.UserId == userId && rt.IsActive && rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        if (refreshToken == null)
            throw new ArgumentNullException(nameof(refreshToken));

        await _dbSet.AddAsync(refreshToken, cancellationToken);
    }

    public Task UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        if (refreshToken == null)
            throw new ArgumentNullException(nameof(refreshToken));

        _dbSet.Update(refreshToken);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        if (refreshToken == null)
            throw new ArgumentNullException(nameof(refreshToken));

        _dbSet.Remove(refreshToken);
        return Task.CompletedTask;
    }

    public async Task RevokeAllUserTokensAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var tokens = await _dbSet
            .Where(rt => rt.UserId == userId && rt.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.Revoke();
        }
    }

    public async Task RemoveExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        var expiredTokens = await _dbSet
            .Where(rt => rt.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        _dbSet.RemoveRange(expiredTokens);
    }
}