using GameStore.Catalog.Domain.Entities;
using GameStore.Catalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameStore.Catalog.Infrastructure.Persistence.Seeding;

public static class CatalogDbInitializer
{
    public static readonly Guid DefaultGamerId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static async Task InitializeAsync(
        CatalogDbContext context,
        ILogger logger)
    {
        try
        {
            await context.Database.EnsureCreatedAsync();

            if (await context.Games.AnyAsync())
            {
                return;
            }

            logger.LogInformation("Iniciando o seeding inicial do banco de dados da CatalogAPI...");

            var games = new List<Game>
            {
                new(
                    title: "Cyber Realm 2077",
                    description: "RPG de ação e mundo aberto em uma metrópole futurista obcecada por poder, glamour e modificações biológicas.",
                    price: Money.Create(199.90m),
                    category: "RPG",
                    releaseDate: new DateTime(2023, 5, 15),
                    id: Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111")
                ),
                new(
                    title: "Speed Horizon Pro",
                    description: "Simulador de corrida de última geração com pistas realistas e centenas de carros customizáveis.",
                    price: Money.Create(149.50m),
                    category: "Corrida",
                    releaseDate: new DateTime(2024, 2, 20),
                    id: Guid.Parse("bbbbbbbb-2222-2222-2222-222222222222")
                ),
                new(
                    title: "Space Odyssey Tactics",
                    description: "Jogo de estratégia espacial em turnos com gestão de frotas e conquista galáctica.",
                    price: Money.Create(89.90m),
                    category: "Estratégia",
                    releaseDate: new DateTime(2023, 11, 10),
                    id: Guid.Parse("cccccccc-3333-3333-3333-333333333333")
                ),
                new(
                    title: "Pixel Dungeon Quest",
                    description: "Roguelike clássico com arte em pixel art, masmorras geradas proceduralmente e centenas de itens.",
                    price: Money.Create(49.90m),
                    category: "Indie",
                    releaseDate: new DateTime(2024, 1, 5),
                    id: Guid.Parse("dddddddd-4444-4444-4444-444444444444")
                )
            };

            await context.Games.AddRangeAsync(games);

            var promo = new Promotion(
                title: "Semana Gamer de Lançamento",
                discountPercentage: 15.0m,
                startDate: DateTime.UtcNow.AddDays(-2),
                endDate: DateTime.UtcNow.AddDays(15)
            );
            await context.Promotions.AddAsync(promo);

            // Jogo inicial na biblioteca do jogador padrão (jogador@gamestore.com)
            var initialItem = new UserGameLibraryItem(
                DefaultGamerId,
                games[0].Id,
                games[0].Price.ApplyDiscount(15.0m),
                games[0]
            );
            await context.UserGameLibraryItems.AddAsync(initialItem);

            await context.SaveChangesAsync();
            logger.LogInformation("Seeding da CatalogAPI concluído com sucesso!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro durante a inicialização/seeding do banco de dados da CatalogAPI.");
            throw;
        }
    }
}

