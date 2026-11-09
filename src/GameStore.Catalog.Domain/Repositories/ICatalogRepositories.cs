using GameStore.Catalog.Domain.Entities;

namespace GameStore.Catalog.Domain.Repositories;

public interface IGameRepository
{
    Task<Game?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Game>> GetAllAsync(string? query = null, string? category = null, bool? activeOnly = null, CancellationToken cancellationToken = default);
    Task AddAsync(Game game, CancellationToken cancellationToken = default);
    void Update(Game game);
    void Remove(Game game);
}

public interface IPromotionRepository
{
    Task<Promotion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Promotion>> GetActivePromotionsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Promotion>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Promotion promotion, CancellationToken cancellationToken = default);
    void Update(Promotion promotion);
    void Remove(Promotion promotion);
}

public interface ILibraryRepository
{
    Task<bool> ExistsAsync(Guid userId, Guid gameId, CancellationToken cancellationToken = default);
    Task<UserGameLibraryItem?> GetByUserAndGameAsync(Guid userId, Guid gameId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserGameLibraryItem>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(UserGameLibraryItem item, CancellationToken cancellationToken = default);
}

public interface IUnitOfWork
{
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
}

