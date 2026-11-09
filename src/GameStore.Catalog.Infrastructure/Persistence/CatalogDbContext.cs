using GameStore.Catalog.Domain.Entities;
using GameStore.Catalog.Domain.Repositories;
using GameStore.Catalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameStore.Catalog.Infrastructure.Persistence;

public class CatalogDbContext : DbContext, IUnitOfWork
{
    public DbSet<Game> Games => Set<Game>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<UserGameLibraryItem> UserGameLibraryItems => Set<UserGameLibraryItem>();

    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
    }

    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        return await SaveChangesAsync(cancellationToken);
    }
}

public class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.ToTable("Games");
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Title).IsRequired().HasMaxLength(150);
        builder.Property(g => g.Description).IsRequired().HasMaxLength(2000);
        builder.Property(g => g.Price)
            .IsRequired()
            .HasPrecision(18, 2)
            .HasConversion(price => price.Amount, value => Money.Create(value, "BRL"));
        builder.Property(g => g.Category).IsRequired().HasMaxLength(50);
        builder.Property(g => g.ReleaseDate).IsRequired();
        builder.Property(g => g.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(g => g.CreatedAt).IsRequired();
        builder.Property(g => g.UpdatedAt);

        builder.HasIndex(g => g.Category);
        builder.HasIndex(g => g.IsActive);
    }
}

public class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.ToTable("Promotions");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title).IsRequired().HasMaxLength(100);
        builder.Property(p => p.DiscountPercentage).IsRequired().HasPrecision(5, 2);
        builder.Property(p => p.StartDate).IsRequired();
        builder.Property(p => p.EndDate).IsRequired();
        builder.Property(p => p.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(p => p.CreatedAt).IsRequired();

        builder.HasIndex(p => p.IsActive);
        builder.HasIndex(p => new { p.StartDate, p.EndDate });
    }
}

public class UserGameLibraryItemConfiguration : IEntityTypeConfiguration<UserGameLibraryItem>
{
    public void Configure(EntityTypeBuilder<UserGameLibraryItem> builder)
    {
        builder.ToTable("UserGameLibraryItems");
        builder.HasKey(li => li.Id);

        builder.Property(li => li.UserId).IsRequired();
        builder.Property(li => li.GameId).IsRequired();
        builder.Property(li => li.PricePaid)
            .IsRequired()
            .HasPrecision(18, 2)
            .HasConversion(price => price.Amount, value => Money.Create(value, "BRL"));
        builder.Property(li => li.AcquisitionDate).IsRequired();

        builder.HasOne(li => li.Game)
            .WithMany()
            .HasForeignKey(li => li.GameId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(li => new { li.UserId, li.GameId }).IsUnique();
    }
}

