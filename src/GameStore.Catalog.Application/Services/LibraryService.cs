using GameStore.Catalog.Application.Common.Interfaces;
using GameStore.Catalog.Application.DTOs;
using GameStore.Catalog.Application.Interfaces;
using GameStore.Catalog.Domain.Entities;
using GameStore.Catalog.Domain.Exceptions;
using GameStore.Catalog.Domain.Repositories;
using GameStore.Catalog.Domain.ValueObjects;
using GameStore.Contracts.Events;
using Microsoft.Extensions.Logging;

namespace GameStore.Catalog.Application.Services;

/// <summary>
/// Serviço responsável por:
/// 1. Iniciar o fluxo de compra de um jogo publicando <see cref="OrderPlaceEvent"/> na fila MQ.
/// 2. Consumir <see cref="PaymentProcessedEvent"/> e, caso o pagamento seja Approved, adicionar o jogo à biblioteca do usuário.
/// </summary>
public class LibraryService : ILibraryService
{
    private readonly ILibraryRepository _libraryRepository;
    private readonly IGameRepository _gameRepository;
    private readonly IPromotionRepository _promotionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<LibraryService> _logger;

    public LibraryService(
        ILibraryRepository libraryRepository,
        IGameRepository gameRepository,
        IPromotionRepository promotionRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IEventPublisher eventPublisher,
        ILogger<LibraryService> logger)
    {
        _libraryRepository = libraryRepository;
        _gameRepository = gameRepository;
        _promotionRepository = promotionRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<OrderPlacedResponse> RequestPurchaseAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedException("É necessário estar autenticado para adquirir jogos.");
        }

        var userId = _currentUserService.UserId.Value;

        var game = await _gameRepository.GetByIdAsync(gameId, cancellationToken);
        if (game is null)
        {
            throw new NotFoundException("Jogo", gameId);
        }

        if (!game.IsActive)
        {
            throw new ValidationException("Game", $"O jogo '{game.Title}' não está disponível para aquisição.");
        }

        var alreadyOwned = await _libraryRepository.ExistsAsync(userId, gameId, cancellationToken);
        if (alreadyOwned)
        {
            throw new GameAlreadyOwnedException(game.Title);
        }

        var activePromotions = await _promotionRepository.GetActivePromotionsAsync(cancellationToken);
        var highestDiscountPromotion = activePromotions
            .Where(p => p.IsValidNow())
            .OrderByDescending(p => p.DiscountPercentage)
            .FirstOrDefault();

        Money priceToPay = game.Price;
        if (highestDiscountPromotion is not null)
        {
            priceToPay = game.Price.ApplyDiscount(highestDiscountPromotion.DiscountPercentage);
        }

        var orderId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        // Publica OrderPlaceEvent contendo UserId, GameId e Price na fila MQ
        var orderEvent = new OrderPlaceEvent(
            OrderId: orderId,
            UserId: userId,
            GameId: game.Id,
            Price: priceToPay.Amount,
            Currency: priceToPay.Currency,
            UserEmail: _currentUserService.UserEmail,
            GameTitle: game.Title,
            CreatedAt: createdAt
        );

        await _eventPublisher.PublishAsync(orderEvent, cancellationToken);

        _logger.LogInformation(
            "Evento OrderPlaceEvent publicado com sucesso. OrderId={OrderId}, UserId={UserId}, GameId={GameId}, Price={Price}",
            orderId, userId, game.Id, priceToPay.Amount);

        return new OrderPlacedResponse(
            OrderId: orderId,
            UserId: userId,
            GameId: game.Id,
            GameTitle: game.Title,
            Price: priceToPay.Amount,
            Currency: priceToPay.Currency,
            Status: "PendingPayment",
            Message: "Pedido de compra enviado para processamento de pagamento.",
            CreatedAt: createdAt
        );
    }

    public async Task<bool> ProcessPaymentResultAsync(PaymentProcessedEvent paymentEvent, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(paymentEvent.Status, "Approved", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Pagamento do pedido {OrderId} para UserId={UserId}, GameId={GameId} foi {Status}. O jogo não será adicionado à biblioteca.",
                paymentEvent.OrderId, paymentEvent.UserId, paymentEvent.GameId, paymentEvent.Status);
            return false;
        }

        // Idempotência: verifica se o jogo já foi adicionado anteriormente
        var alreadyOwned = await _libraryRepository.ExistsAsync(paymentEvent.UserId, paymentEvent.GameId, cancellationToken);
        if (alreadyOwned)
        {
            _logger.LogInformation(
                "Usuário {UserId} já possui o jogo {GameId} na biblioteca (processamento idempotente).",
                paymentEvent.UserId, paymentEvent.GameId);
            return true;
        }

        var game = await _gameRepository.GetByIdAsync(paymentEvent.GameId, cancellationToken);
        if (game is null)
        {
            _logger.LogError("Jogo {GameId} não encontrado no catálogo ao processar pagamento aprovado {PaymentId}.",
                paymentEvent.GameId, paymentEvent.PaymentId);
            return false;
        }

        var pricePaid = Money.Create(paymentEvent.Price, string.IsNullOrWhiteSpace(paymentEvent.Currency) ? "BRL" : paymentEvent.Currency);
        var libraryItem = new UserGameLibraryItem(paymentEvent.UserId, paymentEvent.GameId, pricePaid, game);

        await _libraryRepository.AddAsync(libraryItem, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "Pagamento APROVADO ({PaymentId})! Jogo '{GameTitle}' ({GameId}) adicionado à biblioteca do usuário {UserId}.",
            paymentEvent.PaymentId, game.Title, game.Id, paymentEvent.UserId);

        return true;
    }

    public async Task<UserLibrarySummaryResponse> GetCurrentUserLibraryAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedException("Usuário não autenticado.");
        }

        return await GetUserLibraryAsync(_currentUserService.UserId.Value, cancellationToken);
    }

    public async Task<UserLibrarySummaryResponse> GetUserLibraryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var items = (await _libraryRepository.GetByUserIdAsync(userId, cancellationToken)).ToList();

        var games = items.Select(item => new LibraryItemResponse(
            Id: item.Id,
            UserId: item.UserId,
            GameId: item.GameId,
            GameTitle: item.Game?.Title ?? "Jogo não disponível",
            GameDescription: item.Game?.Description ?? string.Empty,
            GameCategory: item.Game?.Category ?? "Geral",
            PricePaid: item.PricePaid.Amount,
            Currency: item.PricePaid.Currency,
            AcquisitionDate: item.AcquisitionDate
        ));

        return new UserLibrarySummaryResponse(
            UserId: userId,
            TotalGames: items.Count,
            Games: games
        );
    }

    public async Task<OwnershipStatusResponse> CheckOwnershipAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedException("Usuário não autenticado.");
        }

        var item = await _libraryRepository.GetByUserAndGameAsync(_currentUserService.UserId.Value, gameId, cancellationToken);
        return new OwnershipStatusResponse(
            GameId: gameId,
            IsOwned: item is not null,
            AcquisitionDate: item?.AcquisitionDate
        );
    }
}

