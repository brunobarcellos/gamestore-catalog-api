# ==============================================================================
# Stage 1: Build & Publish (Multi-Stage Build para Otimização em Produção)
# ==============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/GameStore.Catalog.Domain/GameStore.Catalog.Domain.csproj", "src/GameStore.Catalog.Domain/"]
COPY ["src/GameStore.Catalog.Application/GameStore.Catalog.Application.csproj", "src/GameStore.Catalog.Application/"]
COPY ["src/GameStore.Catalog.Infrastructure/GameStore.Catalog.Infrastructure.csproj", "src/GameStore.Catalog.Infrastructure/"]
COPY ["src/GameStore.Catalog.Api/GameStore.Catalog.Api.csproj", "src/GameStore.Catalog.Api/"]

RUN dotnet restore "src/GameStore.Catalog.Api/GameStore.Catalog.Api.csproj"

COPY src/ src/
WORKDIR "/src/src/GameStore.Catalog.Api"
RUN dotnet publish "GameStore.Catalog.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ==============================================================================
# Stage 2: Runtime enxuto (ASP.NET Core Runtime)
# ==============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN mkdir -p /app/data && chown -R $APP_UID:$APP_UID /app/data

USER $APP_UID
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "GameStore.Catalog.Api.dll"]

