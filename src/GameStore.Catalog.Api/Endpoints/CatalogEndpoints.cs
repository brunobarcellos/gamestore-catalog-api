using GameStore.Catalog.Application.DTOs;
using GameStore.Catalog.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Catalog.Api.Endpoints;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapGameEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/games")
            .WithTags("1. Catálogo e Gestão de Jogos");

        group.MapGet("/", async (
            [FromQuery] string? query,
            [FromQuery] string? category,
            [FromQuery] bool? activeOnly,
            IGameService gameService,
            CancellationToken ct) =>
        {
            var filter = new GameFilterRequest(query, category, activeOnly);
            var games = await gameService.GetAllAsync(filter, ct);
            return Results.Ok(games);
        })
        .AllowAnonymous()
        .WithName("GetAllGames")
        .WithSummary("Listar jogos disponíveis no catálogo")
        .Produces<IEnumerable<GameResponse>>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}", async (
            Guid id,
            IGameService gameService,
            CancellationToken ct) =>
        {
            var game = await gameService.GetByIdAsync(id, ct);
            return Results.Ok(game);
        })
        .AllowAnonymous()
        .WithName("GetGameById")
        .WithSummary("Consultar detalhes de um jogo pelo ID")
        .Produces<GameResponse>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/", async (
            [FromBody] CreateGameRequest request,
            IGameService gameService,
            CancellationToken ct) =>
        {
            var createdGame = await gameService.CreateAsync(request, ct);
            return Results.Created($"/api/games/{createdGame.Id}", createdGame);
        })
        .RequireAuthorization("AdminOnly")
        .WithName("CreateGame")
        .WithSummary("Cadastrar um novo jogo no catálogo (Apenas Administrador)")
        .Produces<GameResponse>(StatusCodes.Status201Created);

        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateGameRequest request,
            IGameService gameService,
            CancellationToken ct) =>
        {
            var updatedGame = await gameService.UpdateAsync(id, request, ct);
            return Results.Ok(updatedGame);
        })
        .RequireAuthorization("AdminOnly")
        .WithName("UpdateGame")
        .WithSummary("Atualizar informações de um jogo (Apenas Administrador)")
        .Produces<GameResponse>(StatusCodes.Status200OK);

        group.MapDelete("/{id:guid}", async (
            Guid id,
            IGameService gameService,
            CancellationToken ct) =>
        {
            await gameService.DeactivateAsync(id, ct);
            return Results.NoContent();
        })
        .RequireAuthorization("AdminOnly")
        .WithName("DeactivateGame")
        .WithSummary("Desativar jogo do catálogo (Apenas Administrador)")
        .Produces(StatusCodes.Status204NoContent);

        return app;
    }

    public static IEndpointRouteBuilder MapLibraryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/library")
            .WithTags("2. Biblioteca e Fluxo de Compra (OrderPlaceEvent)");

        group.MapGet("/my-games", async (
            ILibraryService libraryService,
            CancellationToken ct) =>
        {
            var library = await libraryService.GetCurrentUserLibraryAsync(ct);
            return Results.Ok(library);
        })
        .RequireAuthorization()
        .WithName("GetMyLibrary")
        .WithSummary("Consultar jogos adquiridos pelo usuário autenticado")
        .Produces<UserLibrarySummaryResponse>(StatusCodes.Status200OK);

        group.MapPost("/acquire/{gameId:guid}", async (
            Guid gameId,
            ILibraryService libraryService,
            CancellationToken ct) =>
        {
            var order = await libraryService.RequestPurchaseAsync(gameId, ct);
            return Results.Accepted($"/api/library/check/{gameId}", order);
        })
        .RequireAuthorization()
        .WithName("AcquireGame")
        .WithSummary("Iniciar compra de jogo para a biblioteca (publica OrderPlaceEvent)")
        .WithDescription("Inicia o fluxo de compra publicando OrderPlaceEvent (UserId, GameId, Price) na fila MQ. Quando PaymentsAPI aprovar via PaymentProcessedEvent, o jogo é adicionado à biblioteca.")
        .Produces<OrderPlacedResponse>(StatusCodes.Status202Accepted)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        group.MapGet("/check/{gameId:guid}", async (
            Guid gameId,
            ILibraryService libraryService,
            CancellationToken ct) =>
        {
            var status = await libraryService.CheckOwnershipAsync(gameId, ct);
            return Results.Ok(status);
        })
        .RequireAuthorization()
        .WithName("CheckOwnership")
        .WithSummary("Verificar se o usuário já possui um determinado jogo")
        .Produces<OwnershipStatusResponse>(StatusCodes.Status200OK);

        group.MapGet("/user/{userId:guid}", async (
            Guid userId,
            ILibraryService libraryService,
            CancellationToken ct) =>
        {
            var library = await libraryService.GetUserLibraryAsync(userId, ct);
            return Results.Ok(library);
        })
        .RequireAuthorization("AdminOnly")
        .WithName("GetUserLibraryByAdmin")
        .WithSummary("Consultar biblioteca de um usuário específico (Apenas Administrador)")
        .Produces<UserLibrarySummaryResponse>(StatusCodes.Status200OK);

        return app;
    }

    public static IEndpointRouteBuilder MapPromotionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/promotions")
            .WithTags("3. Promoções e Descontos");

        group.MapGet("/active", async (
            IPromotionService promotionService,
            CancellationToken ct) =>
        {
            var promotions = await promotionService.GetActivePromotionsAsync(ct);
            return Results.Ok(promotions);
        })
        .AllowAnonymous()
        .WithName("GetActivePromotions")
        .WithSummary("Listar promoções ativas vigentes no momento")
        .Produces<IEnumerable<PromotionResponse>>(StatusCodes.Status200OK);

        group.MapGet("/", async (
            IPromotionService promotionService,
            CancellationToken ct) =>
        {
            var promotions = await promotionService.GetAllAsync(ct);
            return Results.Ok(promotions);
        })
        .RequireAuthorization("AdminOnly")
        .WithName("GetAllPromotions")
        .WithSummary("Listar todas as promoções cadastradas (Apenas Administrador)")
        .Produces<IEnumerable<PromotionResponse>>(StatusCodes.Status200OK);

        group.MapPost("/", async (
            [FromBody] CreatePromotionRequest request,
            IPromotionService promotionService,
            CancellationToken ct) =>
        {
            var createdPromo = await promotionService.CreateAsync(request, ct);
            return Results.Created($"/api/promotions/{createdPromo.Id}", createdPromo);
        })
        .RequireAuthorization("AdminOnly")
        .WithName("CreatePromotion")
        .WithSummary("Criar nova promoção e desconto (Apenas Administrador)")
        .Produces<PromotionResponse>(StatusCodes.Status201Created);

        group.MapDelete("/{id:guid}", async (
            Guid id,
            IPromotionService promotionService,
            CancellationToken ct) =>
        {
            await promotionService.DeactivateAsync(id, ct);
            return Results.NoContent();
        })
        .RequireAuthorization("AdminOnly")
        .WithName("DeactivatePromotion")
        .WithSummary("Desativar promoção (Apenas Administrador)")
        .Produces(StatusCodes.Status204NoContent);

        return app;
    }
}

