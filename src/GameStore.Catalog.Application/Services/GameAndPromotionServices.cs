using FluentValidation;
using GameStore.Catalog.Application.DTOs;
using GameStore.Catalog.Application.Interfaces;
using GameStore.Catalog.Domain.Entities;
using GameStore.Catalog.Domain.Exceptions;
using GameStore.Catalog.Domain.Repositories;
using GameStore.Catalog.Domain.ValueObjects;
using ValidationException = GameStore.Catalog.Domain.Exceptions.ValidationException;

namespace GameStore.Catalog.Application.Services;

public class GameService : IGameService
{
    private readonly IGameRepository _gameRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateGameRequest> _createValidator;
    private readonly IValidator<UpdateGameRequest> _updateValidator;

    public GameService(
        IGameRepository gameRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateGameRequest> createValidator,
        IValidator<UpdateGameRequest> updateValidator)
    {
        _gameRepository = gameRepository;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<GameResponse> CreateAsync(CreateGameRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            throw new ValidationException(errors);
        }

        var price = Money.Create(request.Price);
        var game = new Game(
            title: request.Title,
            description: request.Description,
            category: request.Category,
            price: price,
            releaseDate: request.ReleaseDate
        );

        await _gameRepository.AddAsync(game, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return MapToResponse(game);
    }

    public async Task<GameResponse> UpdateAsync(Guid id, UpdateGameRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            throw new ValidationException(errors);
        }

        var game = await _gameRepository.GetByIdAsync(id, cancellationToken);
        if (game is null)
            throw new NotFoundException("Jogo", id);

        var price = Money.Create(request.Price);
        game.Update(
            title: request.Title,
            description: request.Description,
            category: request.Category,
            price: price,
            releaseDate: request.ReleaseDate
        );

        _gameRepository.Update(game);
        await _unitOfWork.CommitAsync(cancellationToken);

        return MapToResponse(game);
    }

    public async Task<GameResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var game = await _gameRepository.GetByIdAsync(id, cancellationToken);
        if (game is null)
            throw new NotFoundException("Jogo", id);

        return MapToResponse(game);
    }

    public async Task<IEnumerable<GameResponse>> GetAllAsync(GameFilterRequest? filter = null, CancellationToken cancellationToken = default)
    {
        var games = await _gameRepository.GetAllAsync(
            query: filter?.Query,
            category: filter?.Category,
            activeOnly: filter?.ActiveOnly,
            cancellationToken: cancellationToken);

        return games.Select(MapToResponse);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var game = await _gameRepository.GetByIdAsync(id, cancellationToken);
        if (game is null)
            throw new NotFoundException("Jogo", id);

        game.Deactivate();
        _gameRepository.Update(game);
        await _unitOfWork.CommitAsync(cancellationToken);
    }

    public async Task ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var game = await _gameRepository.GetByIdAsync(id, cancellationToken);
        if (game is null)
            throw new NotFoundException("Jogo", id);

        game.Activate();
        _gameRepository.Update(game);
        await _unitOfWork.CommitAsync(cancellationToken);
    }

    private static GameResponse MapToResponse(Game game)
    {
        return new GameResponse(
            Id: game.Id,
            Title: game.Title,
            Description: game.Description,
            Price: game.Price.Amount,
            Currency: game.Price.Currency,
            Category: game.Category,
            ReleaseDate: game.ReleaseDate,
            IsActive: game.IsActive,
            CreatedAt: game.CreatedAt,
            UpdatedAt: game.UpdatedAt
        );
    }
}

public class PromotionService : IPromotionService
{
    private readonly IPromotionRepository _promotionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreatePromotionRequest> _createValidator;

    public PromotionService(
        IPromotionRepository promotionRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreatePromotionRequest> createValidator)
    {
        _promotionRepository = promotionRepository;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
    }

    public async Task<PromotionResponse> CreateAsync(CreatePromotionRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            throw new ValidationException(errors);
        }

        var promo = new Promotion(
            title: request.Title,
            discountPercentage: request.DiscountPercentage,
            startDate: request.StartDate,
            endDate: request.EndDate
        );

        await _promotionRepository.AddAsync(promo, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return MapToResponse(promo);
    }

    public async Task<PromotionResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var promo = await _promotionRepository.GetByIdAsync(id, cancellationToken);
        if (promo is null)
            throw new NotFoundException("Promoção", id);

        return MapToResponse(promo);
    }

    public async Task<IEnumerable<PromotionResponse>> GetActivePromotionsAsync(CancellationToken cancellationToken = default)
    {
        var promos = await _promotionRepository.GetActivePromotionsAsync(cancellationToken);
        return promos.Select(MapToResponse);
    }

    public async Task<IEnumerable<PromotionResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var promos = await _promotionRepository.GetAllAsync(cancellationToken);
        return promos.Select(MapToResponse);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var promo = await _promotionRepository.GetByIdAsync(id, cancellationToken);
        if (promo is null)
            throw new NotFoundException("Promoção", id);

        promo.Deactivate();
        _promotionRepository.Update(promo);
        await _unitOfWork.CommitAsync(cancellationToken);
    }

    public async Task ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var promo = await _promotionRepository.GetByIdAsync(id, cancellationToken);
        if (promo is null)
            throw new NotFoundException("Promoção", id);

        promo.Activate();
        _promotionRepository.Update(promo);
        await _unitOfWork.CommitAsync(cancellationToken);
    }

    private static PromotionResponse MapToResponse(Promotion promo)
    {
        return new PromotionResponse(
            Id: promo.Id,
            Title: promo.Title,
            DiscountPercentage: promo.DiscountPercentage,
            StartDate: promo.StartDate,
            EndDate: promo.EndDate,
            IsActive: promo.IsActive,
            IsCurrentlyValid: promo.IsValidNow(),
            CreatedAt: promo.CreatedAt
        );
    }
}

