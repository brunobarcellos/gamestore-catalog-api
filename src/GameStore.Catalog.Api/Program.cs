using GameStore.Catalog.Api.Endpoints;
using GameStore.Catalog.Api.Infrastructure;
using GameStore.Catalog.Application;
using GameStore.Catalog.Application.Common.Interfaces;
using GameStore.Catalog.Infrastructure;
using GameStore.Catalog.Infrastructure.Persistence;
using GameStore.Catalog.Infrastructure.Persistence.Seeding;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddHealthChecks();

// Clientes HTTP para comunicação entre serviços no cluster via Service DNS (ex.: http://user-api:80, http://payments-api:80)
builder.Services.AddHttpClient("UserApi", client =>
{
    var url = builder.Configuration["Services:UserApiUrl"] ?? "http://user-api:80";
    client.BaseAddress = new Uri(url);
});
builder.Services.AddHttpClient("PaymentsApi", client =>
{
    var url = builder.Configuration["Services:PaymentsApiUrl"] ?? "http://payments-api:80";
    client.BaseAddress = new Uri(url);
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddSwaggerDocumentation();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "GameStore CatalogAPI v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "GameStore CatalogAPI - Swagger UI";
});

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapHealthChecks("/health").ExcludeFromDescription();

app.MapGameEndpoints();
app.MapLibraryEndpoints();
app.MapPromotionEndpoints();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var dbContext = services.GetRequiredService<CatalogDbContext>();

    await CatalogDbInitializer.InitializeAsync(dbContext, logger);
}

app.Run();

public partial class Program { }

