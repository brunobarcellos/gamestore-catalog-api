using GameStore.Catalog.Application.Common.Interfaces;
using GameStore.Catalog.Application.Interfaces;
using GameStore.Contracts.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace GameStore.Catalog.Infrastructure.Messaging;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";
    public string Host { get; set; } = "localhost";
    public ushort Port { get; set; } = 5672;
    public string VirtualHost { get; set; } = "/";
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public CatalogQueueOptions Queues { get; set; } = new();
}

public class CatalogQueueOptions
{
    public string OrderPlace { get; set; } = "OrderPlaceEvent";
    public string PaymentProcessedCatalog { get; set; } = "PaymentProcessedEvent.Catalog";
}

public class MassTransitEventPublisher : IEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<MassTransitEventPublisher> _logger;

    public MassTransitEventPublisher(
        IPublishEndpoint publishEndpoint,
        ILogger<MassTransitEventPublisher> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        await _publishEndpoint.Publish(message, cancellationToken);
        _logger.LogInformation("Evento {EventType} publicado no RabbitMQ pela CatalogAPI.", typeof(T).Name);
    }
}

/// <summary>
/// Consumidor do evento <see cref="PaymentProcessedEvent"/> na CatalogAPI.
/// Se o pagamento foi Approved, adiciona o jogo à biblioteca do usuário.
/// </summary>
public class PaymentProcessedCatalogConsumer : IConsumer<PaymentProcessedEvent>
{
    private readonly ILibraryService _libraryService;
    private readonly ILogger<PaymentProcessedCatalogConsumer> _logger;

    public PaymentProcessedCatalogConsumer(
        ILibraryService libraryService,
        ILogger<PaymentProcessedCatalogConsumer> logger)
    {
        _libraryService = libraryService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentProcessedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation(
            "[CatalogAPI] Consumindo PaymentProcessedEvent: PaymentId={PaymentId}, OrderId={OrderId}, UserId={UserId}, GameId={GameId}, Status={Status}",
            message.PaymentId, message.OrderId, message.UserId, message.GameId, message.Status);

        await _libraryService.ProcessPaymentResultAsync(message, context.CancellationToken);
    }
}

