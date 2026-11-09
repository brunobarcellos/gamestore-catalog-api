using GameStore.Catalog.Domain.Entities;
using GameStore.Catalog.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Catalog.Infrastructure.Persistence.Repositories;

public class GameRepository : IGameRepository
{
    private readonly CatalogDbContext _context;

    public GameRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<Game?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Games.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Game>> GetAllAsync(
        string? query = null,
        string? category = null,
        bool? activeOnly = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Game> dbQuery = _context.Games;

        if (activeOnly.HasValue && activeOnly.Value)
            dbQuery = dbQuery.Where(g => g.IsActive);

        if (!string.IsNullOrWhiteSpace(category))
            dbQuery = dbQuery.Where(g => g.Category.ToLower() == category.ToLower().Trim());

        if (!string.IsNullOrWhiteSpace(query))
        {
            var cleanQuery = query.ToLower().Trim();
            dbQuery = dbQuery.Where(g => g.Title.ToLower().Contains(cleanQuery) || g.Description.ToLower().Contains(cleanQuery));
        }

        return await dbQuery.OrderByDescending(g => g.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Game game, CancellationToken cancellationToken = default)
        => await _context.Games.AddAsync(game, cancellationToken);

    public void Update(Game game) => _context.Games.Update(game);

    public void Remove(Game game) => _context.Games.Remove(game);
}

public class PromotionRepository : IPromotionRepository
{
    private readonly CatalogDbContext _context;

    public PromotionRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<Promotion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Promotions.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IEnumerable<Promotion>> GetActivePromotionsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.Promotions
            .Where(p => p.IsActive && p.StartDate <= now && p.EndDate >= now)
            .OrderByDescending(p => p.DiscountPercentage)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Promotion>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.Promotions.OrderByDescending(p => p.CreatedAt).ToListAsync(cancellationToken);

    public async Task AddAsync(Promotion promotion, CancellationToken cancellationToken = default)
        => await _context.Promotions.AddAsync(promotion, cancellationToken);

    public void Update(Promotion promotion) => _context.Promotions.Update(promotion);

    public void Remove(Promotion promotion) => _context.Promotions.Remove(promotion);
}

public class LibraryRepository : ILibraryRepository
{
    private readonly CatalogDbContext _context;

    public LibraryRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsAsync(Guid userId, Guid gameId, CancellationToken cancellationToken = default)
    {
        return await _context.UserGameLibraryItems
            .AnyAsync(li => li.UserId == userId && li.GameId == gameId, cancellationToken);
    }

    public async Task<UserGameLibraryItem?> GetByUserAndGameAsync(Guid userId, Guid gameId, CancellationToken cancellationToken = default)
    {
        return await _context.UserGameLibraryItems
            .Include(li => li.Game)
            .FirstOrDefaultAsync(li => li.UserId == userId && li.GameId == gameId, cancellationToken);
    }

    public async Task<IEnumerable<UserGameLibraryItem>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserGameLibraryItems
            .Include(li => li.Game)
            .Where(li => li.UserId == userId)
            .OrderByDescending(li => li.AcquisitionDate)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(UserGameLibraryItem item, CancellationToken cancellationToken = default)
    {
        await _context.UserGameLibraryItems.AddAsync(item, cancellationToken);
    }
}

