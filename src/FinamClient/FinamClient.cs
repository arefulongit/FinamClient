using System.Runtime.CompilerServices;
using Google.Protobuf.WellKnownTypes;
using Google.Type;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Tradeapi.V1;
using Grpc.Tradeapi.V1.Accounts;
using Grpc.Tradeapi.V1.Assets;
using Grpc.Tradeapi.V1.Auth;
using Grpc.Tradeapi.V1.Corporateactions;
using Grpc.Tradeapi.V1.Marketdata;
using Grpc.Tradeapi.V1.Metrics;
using Grpc.Tradeapi.V1.Orders;
using Grpc.Tradeapi.V1.Reports;

namespace Finam.TradeApi;

/// <summary>
/// Высокоуровневый клиент Finam Trade API (gRPC, https://api.finam.ru/docs/grpc/).
/// <para>
/// Автоматически получает и обновляет JWT-токен и предоставляет удобные методы
/// для работы со счетами, заявками и рыночными данными. Для полного доступа к API
/// используйте низкоуровневые gRPC-клиенты из свойств <see cref="Accounts"/>,
/// <see cref="Assets"/>, <see cref="MarketData"/>, <see cref="Orders"/> и т.д.
/// в сочетании с <see cref="GetMetadataAsync"/> (заголовок Authorization: Bearer).
/// </para>
/// </summary>
public sealed class FinamClient : IDisposable
{
    private readonly FinamOptions _options;
    private readonly GrpcChannel _channel;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private readonly TimeSpan _tokenSafetyWindow = TimeSpan.FromMinutes(1);

    private string? _jwt;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

    public FinamClient(FinamOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));

        _channel = GrpcChannel.ForAddress(options.Endpoint, new GrpcChannelOptions
        {
            Credentials = options.UseTls ? ChannelCredentials.SecureSsl : ChannelCredentials.Insecure,
            MaxReceiveMessageSize = 64 * 1024 * 1024,
        });

        Auth = new AuthService.AuthServiceClient(_channel);
        Accounts = new AccountsService.AccountsServiceClient(_channel);
        Assets = new AssetsService.AssetsServiceClient(_channel);
        MarketData = new MarketDataService.MarketDataServiceClient(_channel);
        Orders = new OrdersService.OrdersServiceClient(_channel);
        Reports = new ReportsService.ReportsServiceClient(_channel);
        Metrics = new UsageMetricsService.UsageMetricsServiceClient(_channel);
        CorporateActions = new CorporateActionsService.CorporateActionsServiceClient(_channel);
    }

    // ------------------- Низкоуровневые gRPC-клиенты -------------------

    public AuthService.AuthServiceClient Auth { get; }
    public AccountsService.AccountsServiceClient Accounts { get; }
    public AssetsService.AssetsServiceClient Assets { get; }
    public MarketDataService.MarketDataServiceClient MarketData { get; }
    public OrdersService.OrdersServiceClient Orders { get; }
    public ReportsService.ReportsServiceClient Reports { get; }
    public UsageMetricsService.UsageMetricsServiceClient Metrics { get; }
    public CorporateActionsService.CorporateActionsServiceClient CorporateActions { get; }

    public GrpcChannel Channel => _channel;

    // ------------------- Аутентификация -------------------

    /// <summary>
    /// Возвращает действующий JWT-токен. При первом вызове или по истечении срока
    /// выполняет метод <c>AuthService/Auth</c> и кэширует токен.
    /// </summary>
    public async Task<string> GetJwtAsync(CancellationToken ct = default)
    {
        if (IsTokenFresh)
            return _jwt!;

        await _tokenLock.WaitAsync(ct);
        try
        {
            if (IsTokenFresh)
                return _jwt!;

            var response = await Auth.AuthAsync(new AuthRequest
            {
                Secret = _options.ApiSecret,
                SourceAppId = _options.SourceAppId,
            }, cancellationToken: ct);

            if (string.IsNullOrEmpty(response.Token))
                throw new FinamApiException("Сервис аутентификации не вернул JWT-токен.");

            _jwt = response.Token;
            _expiresAt = DateTimeOffset.UtcNow.AddHours(1); // безопасное значение по умолчанию

            try
            {
                var details = await Auth.TokenDetailsAsync(
                    new TokenDetailsRequest { Token = _jwt },
                    headers: BearerMetadata(_jwt),
                    cancellationToken: ct);

                if (details.ExpiresAt is not null)
                    _expiresAt = details.ExpiresAt.ToDateTimeOffset();
            }
            catch (RpcException)
            {
                // TokenDetails не критичен: оставляем срок по умолчанию.
            }

            return _jwt;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private bool IsTokenFresh => _jwt is not null && DateTimeOffset.UtcNow + _tokenSafetyWindow < _expiresAt;

    /// <summary>Metadata с актуальным Bearer-токеном для прямых вызовов сервисов.</summary>
    public async Task<Metadata> GetMetadataAsync(CancellationToken ct = default)
    {
        var token = await GetJwtAsync(ct);
        return BearerMetadata(token);
    }

    private static Metadata BearerMetadata(string jwt) => new()
    {
        { "Authorization", $"Bearer {jwt}" },
    };

    // ------------------- Счета (AccountsService) -------------------

    /// <summary>Информация по счёту: статус, эквити, позиции, деньги.</summary>
    public async Task<GetAccountResponse> GetAccountAsync(string accountId, CancellationToken ct = default)
    {
        var headers = await GetMetadataAsync(ct);
        return await Accounts.GetAccountAsync(
            new GetAccountRequest { AccountId = accountId },
            headers: headers, cancellationToken: ct);
    }

    // ------------------- Заявки (OrdersService) -------------------

    /// <summary>Выставить рыночную заявку.</summary>
    public async Task<OrderState> PlaceMarketOrderAsync(
        string accountId, string symbol, decimal quantity, Side side, CancellationToken ct = default)
    {
        return await PlaceOrderAsync(accountId, symbol, quantity, side,
            OrderType.Market, limitPrice: null, ct);
    }

    /// <summary>Выставить лимитную заявку.</summary>
    public async Task<OrderState> PlaceLimitOrderAsync(
        string accountId, string symbol, decimal quantity, Side side, decimal limitPrice,
        CancellationToken ct = default)
    {
        return await PlaceOrderAsync(accountId, symbol, quantity, side,
            OrderType.Limit, limitPrice, ct);
    }

    private async Task<OrderState> PlaceOrderAsync(
        string accountId, string symbol, decimal quantity, Side side,
        OrderType type, decimal? limitPrice, CancellationToken ct)
    {
        if (type == OrderType.Limit && limitPrice is null)
            throw new ArgumentException("Для лимитной заявки необходимо указать цену.", nameof(limitPrice));

        var headers = await GetMetadataAsync(ct);
        var order = new Order
        {
            AccountId = accountId,
            Symbol = symbol,
            Quantity = ToDecimal(quantity),
            Side = side,
            Type = type,
        };

        if (limitPrice is not null)
            order.LimitPrice = ToDecimal(limitPrice.Value);

        var response = await Orders.PlaceOrderAsync(order, headers: headers, cancellationToken: ct);
        return response;
    }

    /// <summary>Отменить заявку по идентификатору.</summary>
    public async Task<OrderState> CancelOrderAsync(string accountId, string orderId, CancellationToken ct = default)
    {
        var headers = await GetMetadataAsync(ct);
        return await Orders.CancelOrderAsync(
            new CancelOrderRequest { AccountId = accountId, OrderId = orderId },
            headers: headers, cancellationToken: ct);
    }

    /// <summary>Список активных заявок по счёту.</summary>
    public async Task<OrdersResponse> GetOrdersAsync(string accountId, CancellationToken ct = default)
    {
        var headers = await GetMetadataAsync(ct);
        return await Orders.GetOrdersAsync(
            new OrdersRequest { AccountId = accountId },
            headers: headers, cancellationToken: ct);
    }

    /// <summary>Поток изменений собственных заявок (server-streaming).</summary>
    public async Task<IAsyncEnumerable<OrderState>> StreamOrdersAsync(
        string accountId, CancellationToken ct = default)
    {
        var headers = await GetMetadataAsync(ct);
        var call = Orders.SubscribeOrders(
            new SubscribeOrdersRequest { AccountId = accountId },
            headers: headers, cancellationToken: ct);
        return Enumerate(call.ResponseStream, static r => r.Orders, ct);
    }

    /// <summary>Поток собственных сделок (server-streaming).</summary>
    public async Task<IAsyncEnumerable<AccountTrade>> StreamTradesAsync(
        string accountId, CancellationToken ct = default)
    {
        var headers = await GetMetadataAsync(ct);
        var call = Orders.SubscribeTrades(
            new SubscribeTradesRequest { AccountId = accountId },
            headers: headers, cancellationToken: ct);
        return Enumerate(call.ResponseStream, static r => r.Trades, ct);
    }

    // ------------------- Рыночные данные (MarketDataService) -------------------

    /// <summary>Исторические свечи по инструменту.</summary>
    public async Task<BarsResponse> GetBarsAsync(
        string symbol, TimeFrame timeframe, DateTimeOffset from, DateTimeOffset to,
        CancellationToken ct = default)
    {
        var headers = await GetMetadataAsync(ct);
        return await MarketData.BarsAsync(new BarsRequest
        {
            Symbol = symbol,
            Timeframe = timeframe,
            Interval = new Interval
            {
                StartTime = Timestamp.FromDateTimeOffset(from),
                EndTime = Timestamp.FromDateTimeOffset(to),
            },
        }, headers: headers, cancellationToken: ct);
    }

    /// <summary>Последняя котировка по инструменту.</summary>
    public async Task<QuoteResponse> GetLastQuoteAsync(string symbol, CancellationToken ct = default)
    {
        var headers = await GetMetadataAsync(ct);
        return await MarketData.LastQuoteAsync(
            new QuoteRequest { Symbol = symbol },
            headers: headers, cancellationToken: ct);
    }

    /// <summary>Текущий стакан по инструменту.</summary>
    public async Task<OrderBookResponse> GetOrderBookAsync(string symbol, CancellationToken ct = default)
    {
        var headers = await GetMetadataAsync(ct);
        return await MarketData.OrderBookAsync(
            new OrderBookRequest { Symbol = symbol },
            headers: headers, cancellationToken: ct);
    }

    /// <summary>Поток котировок по списку инструментов (server-streaming).</summary>
    public async Task<IAsyncEnumerable<Quote>> StreamQuotesAsync(
        IReadOnlyCollection<string> symbols, CancellationToken ct = default)
    {
        var headers = await GetMetadataAsync(ct);
        var call = MarketData.SubscribeQuote(
            new SubscribeQuoteRequest { Symbols = { symbols } },
            headers: headers, cancellationToken: ct);
        return Enumerate(call.ResponseStream, static r => r.Quote, ct);
    }

    /// <summary>Поток стакана по инструменту (server-streaming).</summary>
    public async Task<IAsyncEnumerable<StreamOrderBook>> StreamOrderBookAsync(
        string symbol, CancellationToken ct = default)
    {
        var headers = await GetMetadataAsync(ct);
        var call = MarketData.SubscribeOrderBook(
            new SubscribeOrderBookRequest { Symbol = symbol },
            headers: headers, cancellationToken: ct);
        return Enumerate(call.ResponseStream, static r => r.OrderBook, ct);
    }

    /// <summary>Поток агрегированных свечей по инструменту (server-streaming).</summary>
    public async Task<IAsyncEnumerable<Bar>> StreamBarsAsync(
        string symbol, TimeFrame timeframe, CancellationToken ct = default)
    {
        var headers = await GetMetadataAsync(ct);
        var call = MarketData.SubscribeBars(
            new SubscribeBarsRequest { Symbol = symbol, Timeframe = timeframe },
            headers: headers, cancellationToken: ct);
        return Enumerate(call.ResponseStream, static r => r.Bars, ct);
    }

    // ------------------- Утилиты -------------------

    private static async IAsyncEnumerable<T> Enumerate<TResp, T>(
        IAsyncStreamReader<TResp> reader,
        Func<TResp, IEnumerable<T>> selector,
        [EnumeratorCancellation] CancellationToken ct)
    {
        while (await reader.MoveNext(ct))
        {
            foreach (var item in selector(reader.Current))
                yield return item;
        }
    }

    private static Google.Type.Decimal ToDecimal(decimal value) =>
        new() { Value = value.ToString(System.Globalization.CultureInfo.InvariantCulture) };

    public void Dispose()
    {
        _channel.Dispose();
        _tokenLock.Dispose();
    }
}
