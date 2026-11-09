namespace GameStore.Contracts.Events;

/// <summary>
/// Evento de integração publicado na fila OrderPlaceEvent pela CatalogAPI
/// quando o usuário solicita a compra/adição de um jogo à sua biblioteca.
/// </summary>
public record OrderPlaceEvent(
    Guid OrderId,
    Guid UserId,
    Guid GameId,
    decimal Price,
    string Currency = "BRL",
    string? UserEmail = null,
    string? GameTitle = null,
    DateTime CreatedAt = default
);

/// <summary>
/// Evento de integração publicado pela PaymentsAPI e consumido pela CatalogAPI
/// (para liberar o jogo na biblioteca se Approved) e pela NotificationsAPI (para envio de e-mail).
/// </summary>
public record PaymentProcessedEvent(
    Guid PaymentId,
    Guid OrderId,
    Guid UserId,
    Guid GameId,
    decimal Price,
    string Status,
    string Currency = "BRL",
    string? UserEmail = null,
    string? GameTitle = null,
    DateTime ProcessedAt = default
);

