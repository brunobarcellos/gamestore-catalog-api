namespace GameStore.Catalog.Application.DTOs;

public record CreateGameRequest(
    string Title,
    string Description,
    decimal Price,
    string Category,
    DateTime ReleaseDate
);

public record UpdateGameRequest(
    string Title,
    string Description,
    decimal Price,
    string Category,
    DateTime ReleaseDate
);

public record GameResponse(
    Guid Id,
    string Title,
    string Description,
    decimal Price,
    string Currency,
    string Category,
    DateTime ReleaseDate,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record GameFilterRequest(
    string? Query,
    string? Category,
    bool? ActiveOnly
);

public record CreatePromotionRequest(
    string Title,
    decimal DiscountPercentage,
    DateTime StartDate,
    DateTime EndDate
);

public record PromotionResponse(
    Guid Id,
    string Title,
    decimal DiscountPercentage,
    DateTime StartDate,
    DateTime EndDate,
    bool IsActive,
    bool IsCurrentlyValid,
    DateTime CreatedAt
);

public record OrderPlacedResponse(
    Guid OrderId,
    Guid UserId,
    Guid GameId,
    string GameTitle,
    decimal Price,
    string Currency,
    string Status,
    string Message,
    DateTime CreatedAt
);

public record LibraryItemResponse(
    Guid Id,
    Guid UserId,
    Guid GameId,
    string GameTitle,
    string GameDescription,
    string GameCategory,
    decimal PricePaid,
    string Currency,
    DateTime AcquisitionDate
);

public record UserLibrarySummaryResponse(
    Guid UserId,
    int TotalGames,
    IEnumerable<LibraryItemResponse> Games
);

public record OwnershipStatusResponse(
    Guid GameId,
    bool IsOwned,
    DateTime? AcquisitionDate
);

