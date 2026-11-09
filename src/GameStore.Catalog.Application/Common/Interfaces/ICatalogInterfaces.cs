namespace GameStore.Catalog.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? UserEmail { get; }
    string? UserName { get; }
    string? UserRole { get; }
    bool IsAuthenticated { get; }
}

public interface IEventPublisher
{
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;
}

