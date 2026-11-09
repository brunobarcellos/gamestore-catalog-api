using FluentAssertions;
using GameStore.Catalog.Application.Common.Interfaces;
using GameStore.Catalog.Application.Services;
using GameStore.Catalog.Domain.Entities;
using GameStore.Catalog.Domain.Exceptions;
using GameStore.Catalog.Domain.Repositories;
using GameStore.Catalog.Domain.ValueObjects;
using GameStore.Contracts.Events;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GameStore.Catalog.Tests;

public class CatalogAndLibraryTests
{
    private readonly Mock<ILibraryRepository> _libraryRepositoryMock = new();
    private readonly Mock<IGameRepository> _gameRepositoryMock = new();
    private readonly Mock<IPromotionRepository> _promotionRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<IEventPublisher> _eventPublisherMock = new();

    private LibraryService CreateSut()
    {
        return new LibraryService(
            _libraryRepositoryMock.Object,
            _gameRepositoryMock.Object,
            _promotionRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserServiceMock.Object,
            _eventPublisherMock.Object,
            NullLogger<LibraryService>.Instance
        );
    }

    [Fact]
    public async Task RequestPurchaseAsync_WithValidGameAndUser_ShouldPublishOrderPlaceEventWithUserIdGameIdAndPrice()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var game = new Game("Cyber Realm", "RPG Futurista", "RPG", Money.Create(200.00m), DateTime.UtcNow.AddYears(-1));

        _currentUserServiceMock.Setup(c => c.UserId).Returns(userId);
        _currentUserServiceMock.Setup(c => c.UserEmail).Returns("gamer@gamestore.com");
        _gameRepositoryMock.Setup(r => r.GetByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);
        _libraryRepositoryMock.Setup(r => r.ExistsAsync(userId, game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _promotionRepositoryMock.Setup(r => r.GetActivePromotionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Promotion>
            {
                new("Desconto 10%", 10m, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1))
            });

        var sut = CreateSut();

        // Act
        var response = await sut.RequestPurchaseAsync(game.Id);

        // Assert
        response.Should().NotBeNull();
        response.UserId.Should().Be(userId);
        response.GameId.Should().Be(game.Id);
        response.Price.Should().Be(180.00m);
        response.Status.Should().Be("PendingPayment");

        _eventPublisherMock.Verify(p => p.PublishAsync(
            It.Is<OrderPlaceEvent>(e =>
                e.UserId == userId &&
                e.GameId == game.Id &&
                e.Price == 180.00m),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentResultAsync_WhenPaymentApproved_ShouldAddGameToUserLibrary()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var game = new Game("Speed Horizon", "Corrida", "Corrida", Money.Create(150.00m), DateTime.UtcNow);
        var paymentEvent = new PaymentProcessedEvent(
            PaymentId: Guid.NewGuid(),
            OrderId: Guid.NewGuid(),
            UserId: userId,
            GameId: game.Id,
            Price: 150.00m,
            Status: "Approved",
            Currency: "BRL"
        );

        _libraryRepositoryMock.Setup(r => r.ExistsAsync(userId, game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _gameRepositoryMock.Setup(r => r.GetByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);

        var sut = CreateSut();

        // Act
        var result = await sut.ProcessPaymentResultAsync(paymentEvent);

        // Assert
        result.Should().BeTrue();
        _libraryRepositoryMock.Verify(r => r.AddAsync(
            It.Is<UserGameLibraryItem>(li => li.UserId == userId && li.GameId == game.Id && li.PricePaid.Amount == 150.00m),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentResultAsync_WhenPaymentRejected_ShouldNotAddGameToUserLibrary()
    {
        // Arrange
        var paymentEvent = new PaymentProcessedEvent(
            PaymentId: Guid.NewGuid(),
            OrderId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            GameId: Guid.NewGuid(),
            Price: 150.00m,
            Status: "Rejected",
            Currency: "BRL"
        );

        var sut = CreateSut();

        // Act
        var result = await sut.ProcessPaymentResultAsync(paymentEvent);

        // Assert
        result.Should().BeFalse();
        _libraryRepositoryMock.Verify(r => r.AddAsync(It.IsAny<UserGameLibraryItem>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

