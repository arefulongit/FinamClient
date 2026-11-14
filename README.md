# FinamClient (C# / .NET)

C#-библиотека для работы с **Finam Trade API** ([https://api.finam.ru/docs/grpc/](https://api.finam.ru/docs/grpc/)) — gRPC-интерфейсом брокера Финам, предназначенная для использования в проектах автоматической торговли.

> Важно: официального NuGet-пакета Finam для C# не существует (официальный SDK только для Python — `finam-trade-api`). Поэтому gRPC-клиент генерируется напрямую из **официальных protobuf-схем** брокера, опубликованных в репозитории [FinamWeb/finam-trade-api](https://github.com/FinamWeb/finam-trade-api) (каталог `proto/`).

## Структура решения

| Проект | Назначение |
|---|---|
| [`src/Finam.TradeApi.Grpc`](src/Finam.TradeApi.Grpc/Finam.TradeApi.Grpc.csproj) | Автогенерируемый gRPC-клиент из официальных `.proto` (все 8 сервисов API) |
| [`src/FinamClient`](src/FinamClient/FinamClient.csproj) | Высокоуровневая обёртка: автоматический JWT, заявки, рыночные данные |
| [`samples/FinamClient.Samples`](samples/FinamClient.Samples/Program.cs) | Консольный пример: аутентификация, счёт, котировки, свечи, стримы |

## Быстрый старт

Требуется .NET SDK 8.0+.

```bash
dotnet build FinamClient.slnx
export FINAM_API_SECRET="ваш-api-токен-от-finam"
export FINAM_ACCOUNT_ID="идентификатор-счёта"     # опционально
dotnet run --project samples/FinamClient.Samples
```

API-токен создаётся в приложении Finam → Trade API (см. [документацию по аутентификации](https://api.finam.ru/docs/grpc/auth/)).

## Использование в своём проекте

```bash
dotnet add path/to/YourProject.csproj reference /path/to/FinamClient/src/FinamClient/FinamClient.csproj
```

### Базовый пример

```csharp
using Finam.TradeApi;
using Grpc.Tradeapi.V1;

var options = new FinamOptions
{
    ApiSecret = Environment.GetEnvironmentVariable("FINAM_API_SECRET")!,
};

using var client = new FinamClient(options);

// JWT получается и обновляется автоматически
var quote = await client.GetLastQuoteAsync("SBER@MISX");

// Свечи за период
var bars = await client.GetBarsAsync(
    "SBER@MISX", TimeFrame.D,
    DateTimeOffset.UtcNow.AddDays(-30), DateTimeOffset.UtcNow);

// Лимитная заявка (снял бы деньги — по умолчанию закомментировано)
// var order = await client.PlaceLimitOrderAsync(accountId, "SBER@MISX", 10, Side.Buy, 250.00m);

// Поток котировок в реальном времени
var quotes = await client.StreamQuotesAsync(["SBER@MISX", "GAZP@MISX"]);
await foreach (var q in quotes)
    Console.WriteLine(q);
```

## Возможности обёртки [`FinamClient`](src/FinamClient/FinamClient.cs)

- **Автоматическая аутентификация** — JWT-токен кэшируется и автоматически обновляется перед истечением срока ([`GetJwtAsync`](src/FinamClient/FinamClient.cs:72));
- **Заявки**: [`PlaceMarketOrderAsync`](src/FinamClient/FinamClient.cs:148), [`PlaceLimitOrderAsync`](src/FinamClient/FinamClient.cs:155), [`CancelOrderAsync`](src/FinamClient/FinamClient.cs:185), [`GetOrdersAsync`](src/FinamClient/FinamClient.cs:195);
- **Рыночные данные**: [`GetBarsAsync`](src/FinamClient/FinamClient.cs:288), [`GetLastQuoteAsync`](src/FinamClient/FinamClient.cs:309), [`GetOrderBookAsync`](src/FinamClient/FinamClient.cs:318);
- **Стриминг** (`IAsyncEnumerable<T>`): [`StreamQuotesAsync`](src/FinamClient/FinamClient.cs:327), [`StreamOrderBookAsync`](src/FinamClient/FinamClient.cs:342), [`StreamBarsAsync`](src/FinamClient/FinamClient.cs:357), [`StreamOrdersAsync`](src/FinamClient/FinamClient.cs:226), [`StreamTradesAsync`](src/FinamClient/FinamClient.cs:240);
- **Прямой доступ ко всем 8 сервисам** API через свойства [`Accounts`](src/FinamClient/FinamClient.cs:54), [`Assets`](src/FinamClient/FinamClient.cs:55), [`MarketData`](src/FinamClient/FinamClient.cs:56), [`Orders`](src/FinamClient/FinamClient.cs:57), [`Reports`](src/FinamClient/FinamClient.cs:58), [`Metrics`](src/FinamClient/FinamClient.cs:59), [`CorporateActions`](src/FinamClient/FinamClient.cs:60), [`Auth`](src/FinamClient/FinamClient.cs:53) — с заголовками из [`GetMetadataAsync`](src/FinamClient/FinamClient.cs:118).

## Доступные сервисы Finam Trade API

| Сервис | Описание |
|---|---|
| `AuthService` | Получение JWT, детали токена, подписка на обновление |
| `AccountsService` | Счёт, позиции, сделки, транзакции |
| `OrdersService` | Рыночные/лимитные/стоп-заявки, SL/TP, стримы заявок и сделок |
| `MarketDataService` | Свечи, котировки, стакан, ленты сделок, стримы |
| `AssetsService` | Инструменты, биржи, торговые параметры, расписание |
| `CorporateActionsService` | Сплиты, дивиденды, купоны |
| `ReportsService` | Брокерские отчёты |
| `UsageMetricsService` | Квоты и лимиты API |

## Обновление protobuf-схем

Схемы лежат в [`src/Finam.TradeApi.Grpc/proto/`](src/Finam.TradeApi.Grpc/proto). При обновлении API Финамом заново скопируйте каталог `proto/` из [FinamWeb/finam-trade-api](https://github.com/FinamWeb/finam-trade-api) — C#-код перегенерируется автоматически при сборке ([`Grpc.Tools`](src/Finam.TradeApi.Grpc/Finam.TradeApi.Grpc.csproj:15)).

## Сборка NuGet-пакета

```bash
# Сборка пакетов
dotnet pack FinamClient.slnx -c Release

# Публикация в локальный фид
dotnet nuget push src/Finam.TradeApi.Grpc/bin/Release/Finam.TradeApi.Grpc.*.nupkg --source local --api-key "not-needed"
dotnet nuget push src/FinamClient/bin/Release/FinamClient.*.nupkg --source local --api-key "not-needed"
```

Пакеты создаются в `src/*/bin/Release/`. Локальный фид находится в каталоге `nuget/local/`.

## Подключение через NuGet

```bash
# Установка FinamClient (автоматически подтянет Finam.TradeApi.Grpc)
dotnet add path/to/YourProject.csproj package FinamClient --version 1.0.0
```

Пакет `FinamClient` содержит транзитивную зависимость от `Finam.TradeApi.Grpc` — устанавливать второй пакет отдельно не нужно.

## Безопасность

- Не храните API-токен в коде: используйте переменные окружения (`FINAM_API_SECRET`), user-secrets или `*.secret.json` (в [.gitignore](.gitignore)).
- Начинайте с readonly-токена и небольших объёмов; проверяйте квоты через `Metrics.GetUsageMetricsAsync`.
