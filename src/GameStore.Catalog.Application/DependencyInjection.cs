using FluentValidation;
using GameStore.Catalog.Application.Interfaces;
using GameStore.Catalog.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GameStore.Catalog.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<IGameService, GameService>();
        services.AddScoped<IPromotionService, PromotionService>();
        services.AddScoped<ILibraryService, LibraryService>();
        return services;
    }
}

