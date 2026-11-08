using Finam.TradeApi;
using Grpc.Tradeapi.V1;
using Grpc.Tradeapi.V1.Marketdata;

// ============================================================
// Пример работы с Finam Trade API через библиотеку FinamClient.
//
// Перед запуском задайте переменные окружения:
//   export FINAM_API_SECRET="ваш-api-токен"
//   export FINAM_ACCOUNT_ID="идентификатор-счёта"   (опционально)
// ============================================================

const string SberSymbol = "SBER@MISX"; // Тикер на Московской бирже

var options = FinamOptions.FromEnvironment();
using var client = new FinamClient(options);

// 1. Аутентификация: JWT-токен получается и обновляется автоматически.
var jwt = await client.GetJwtAsync();
Console.WriteLine("JWT получен (первые 24 символа): {0}...", jwt[..Math.Min(24, jwt.Length)]);

// 2. Информация о счёте (требуется FINAM_ACCOUNT_ID).
var accountId = Environment.GetEnvironmentVariable("FINAM_ACCOUNT_ID");
if (!string.IsNullOrEmpty(accountId))
{
    var account = await client.GetAccountAsync(accountId);
    Console.WriteLine($"\nСчёт: {account.AccountId} | тип: {account.Type} | статус: {account.Status}");
    Console.WriteLine($"Equity: {account.Equity.Value}, нереализованная прибыль: {account.UnrealizedProfit.Value}");
    Console.WriteLine($"Позиций: {account.Positions.Count}");

    // 3. Активные заявки.
    var orders = await client.GetOrdersAsync(accountId);
    Console.WriteLine($"\nАктивных заявок: {orders.Orders.Count}");
}

// 4. Последняя котировка.
var quote = await client.GetLastQuoteAsync(SberSymbol);
Console.WriteLine($"\nПоследняя котировка {quote.Symbol}: {quote.Quote}");

// 5. Исторические свечи (D1 за последние 10 дней).
var bars = await client.GetBarsAsync(
    SberSymbol,
    TimeFrame.D,
    DateTimeOffset.UtcNow.AddDays(-10),
    DateTimeOffset.UtcNow);
Console.WriteLine($"\nПолучено свечей: {bars.Bars.Count}");
foreach (var bar in bars.Bars.TakeLast(5))
    Console.WriteLine($"  {bar.Timestamp.ToDateTimeOffset():yyyy-MM-dd}  O={bar.Open.Value} H={bar.High.Value} L={bar.Low.Value} C={bar.Close.Value} V={bar.Volume.Value}");

// 6. Поток котировок в реальном времени (5 секунд).
Console.WriteLine($"\nПодписка на котировки {SberSymbol} (5 секунд)...");
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
var quotes = await client.StreamQuotesAsync([SberSymbol], cts.Token);

try
{
    await foreach (var q in quotes)
        Console.WriteLine($"  Котировка: {q}");
}
catch (OperationCanceledException)
{
    // Нормальное завершение по таймауту.
}

Console.WriteLine("\nПример завершён. Пример выставления заявки (раскомментируйте самостоятельно):");
Console.WriteLine("// var order = await client.PlaceLimitOrderAsync(accountId, \"SBER@MISX\", 10, Side.Buy, 250.00m);");
