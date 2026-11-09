using GameStore.Catalog.Application.DTOs;
using GameStore.Contracts.Events;

namespace GameStore.Catalog.Application.Interfaces;

public interface IGameService
{
    Task<GameResponse> CreateAsync(CreateGameRequest request, CancellationToken cancellationToken = default);
    Task<GameResponse> UpdateAsync(Guid id, UpdateGameRequest request, CancellationToken cancellationToken = default);
    Task<GameResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<GameResponse>> GetAllAsync(GameFilterRequest? filter = null, CancellationToken cancellationToken = default);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task ActivateAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IPromotionService
{
    Task<PromotionResponse> CreateAsync(CreatePromotionRequest request, CancellationToken cancellationToken = default);
    Task<PromotionResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<PromotionResponse>> GetActivePromotionsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<PromotionResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task ActivateAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface ILibraryService
{
    /// <summary>
    /// Inicia o fluxo de compra de um jogo publicando <see cref="OrderPlaceEvent"/> na fila MQ.
    /// </summary>
    Task<OrderPlacedResponse> RequestPurchaseAsync(Guid gameId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processa o evento <see cref="PaymentProcessedEvent"/>; se Approved, adiciona o jogo à biblioteca do usuário.
    /// </summary>
    Task<bool> ProcessPaymentResultAsync(PaymentProcessedEvent paymentEvent, CancellationToken cancellationToken = default);

    Task<UserLibrarySummaryResponse> GetCurrentUserLibraryAsync(CancellationToken cancellationToken = default);
    Task<UserLibrarySummaryResponse> GetUserLibraryAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<OwnershipStatusResponse> CheckOwnershipAsync(Guid gameId, CancellationToken cancellationToken = default);
}

