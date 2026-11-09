# GameStore - CatalogAPI

Microsserviço responsável pelo **CRUD de jogos**, **promoções** e **biblioteca de usuários**.
- Ao receber uma requisição de compra (`POST /api/library/acquire/{gameId}`), publica `OrderPlaceEvent` (`UserId`, `GameId`, `Price`) na fila MQ.
- Consome `PaymentProcessedEvent`: se o pagamento foi `Approved`, adiciona o jogo à biblioteca do usuário.

## Executar Localmente (.NET 10)
```bash
dotnet run --project src/GameStore.Catalog.Api
```

## Build Docker (Multi-Stage)
```bash
docker build -t gamestore/catalog-api:latest .
```

## Deploy Kubernetes
```bash
kubectl apply -f k8s/
```

## Executar Testes
```bash
dotnet test CatalogAPI.slnx
```

