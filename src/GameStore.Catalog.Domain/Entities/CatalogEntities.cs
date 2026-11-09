using GameStore.Catalog.Domain.Common;
using GameStore.Catalog.Domain.Exceptions;
using GameStore.Catalog.Domain.ValueObjects;

namespace GameStore.Catalog.Domain.Entities;

public class Game : Entity, IAggregateRoot
{
    public string Title { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string Category { get; private set; } = null!;
    public Money Price { get; private set; } = null!;
    public DateTime ReleaseDate { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; } = true;

    protected Game() { }

    public Game(
        string title,
        string description,
        string category,
        Money price,
        DateTime releaseDate,
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        ValidateTitle(title);
        ValidateDescription(description);
        ValidateCategory(category);

        Title = title.Trim();
        Description = description.Trim();
        Category = category.Trim();
        Price = price ?? throw new ArgumentNullException(nameof(price));
        ReleaseDate = releaseDate;
        CreatedAt = DateTime.UtcNow;
        IsActive = true;
    }

    public void Update(
        string title,
        string description,
        string category,
        Money price,
        DateTime releaseDate)
    {
        ValidateTitle(title);
        ValidateDescription(description);
        ValidateCategory(category);

        Title = title.Trim();
        Description = description.Trim();
        Category = category.Trim();
        Price = price ?? throw new ArgumentNullException(nameof(price));
        ReleaseDate = releaseDate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (!IsActive)
            throw new ConflictException("O jogo já está desativado.");

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        if (IsActive)
            throw new ConflictException("O jogo já está ativado.");

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    private void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ValidationException(nameof(Title), "O título do jogo é obrigatório.");

        if (title.Trim().Length < 2 || title.Trim().Length > 150)
            throw new ValidationException(nameof(Title), "O título do jogo deve conter entre 2 e 150 caracteres.");
    }

    private void ValidateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ValidationException(nameof(Description), "A descrição do jogo é obrigatória.");

        if (description.Trim().Length > 2000)
            throw new ValidationException(nameof(Description), "A descrição não pode exceder 2000 caracteres.");
    }

    private void ValidateCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            throw new ValidationException(nameof(Category), "A categoria do jogo é obrigatória.");

        if (category.Trim().Length < 2 || category.Trim().Length > 50)
            throw new ValidationException(nameof(Category), "A categoria do jogo deve conter entre 2 e 50 caracteres.");
    }
}

public class Promotion : Entity, IAggregateRoot
{
    public string Title { get; private set; } = null!;
    public decimal DiscountPercentage { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    protected Promotion() { }

    public Promotion(
        string title,
        decimal discountPercentage,
        DateTime startDate,
        DateTime endDate)
    {
        Validate(title, discountPercentage, startDate, endDate);

        Title = title.Trim();
        DiscountPercentage = discountPercentage;
        StartDate = startDate;
        EndDate = endDate;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;

    public bool IsValidNow()
    {
        var now = DateTime.UtcNow;
        return IsActive && now >= StartDate && now <= EndDate;
    }

    private static void Validate(string title, decimal discountPercentage, DateTime startDate, DateTime endDate)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ValidationException(nameof(Title), "O título da promoção é obrigatório.");

        if (discountPercentage <= 0 || discountPercentage > 100)
            throw new ValidationException(nameof(DiscountPercentage), "O percentual de desconto deve estar entre 1% e 100%.");

        if (endDate <= startDate)
            throw new ValidationException(nameof(EndDate), "A data final da promoção deve ser posterior à data inicial.");
    }
}

public class UserGameLibraryItem : Entity, IAggregateRoot
{
    public Guid UserId { get; private set; }
    public Guid GameId { get; private set; }
    public Money PricePaid { get; private set; } = null!;
    public DateTime AcquisitionDate { get; private set; }
    public virtual Game? Game { get; private set; }

    protected UserGameLibraryItem() { }

    public UserGameLibraryItem(
        Guid userId,
        Guid gameId,
        Money pricePaid,
        Game? game = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("O identificador do usuário é obrigatório.", nameof(userId));

        if (gameId == Guid.Empty)
            throw new ArgumentException("O identificador do jogo é obrigatório.", nameof(gameId));

        UserId = userId;
        GameId = gameId;
        PricePaid = pricePaid ?? throw new ArgumentNullException(nameof(pricePaid));
        AcquisitionDate = DateTime.UtcNow;
        Game = game;
    }
}

