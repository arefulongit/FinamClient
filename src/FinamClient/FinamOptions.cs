namespace Finam.TradeApi;

/// <summary>
/// Настройки подключения к Finam Trade API.
/// </summary>
public sealed class FinamOptions
{
    /// <summary>Адрес gRPC-сервера Finam Trade API по умолчанию.</summary>
    public const string DefaultEndpoint = "api.finam.ru:443";

    /// <summary>Адрес gRPC-сервера (host:port).</summary>
    public string Endpoint { get; init; } = DefaultEndpoint;

    /// <summary>API-токен (secret key), полученный в приложении Finam Trade API.</summary>
    public string ApiSecret { get; init; } = string.Empty;

    /// <summary>Идентификатор приложения-источника запроса (опционально).</summary>
    public string SourceAppId { get; init; } = string.Empty;

    /// <summary>Использовать TLS (по умолчанию — да).</summary>
    public bool UseTls { get; init; } = true;

    /// <summary>
    /// Создать настройки из переменных окружения:
    /// FINAM_API_SECRET, FINAM_SOURCE_APP_ID, FINAM_ENDPOINT (опционально).
    /// </summary>
    public static FinamOptions FromEnvironment()
    {
        var secret = Environment.GetEnvironmentVariable("FINAM_API_SECRET")
                     ?? throw new InvalidOperationException(
                         "Переменная окружения FINAM_API_SECRET не задана. Укажите API-токен Finam Trade API.");

        return new FinamOptions
        {
            ApiSecret = secret,
            SourceAppId = Environment.GetEnvironmentVariable("FINAM_SOURCE_APP_ID") ?? string.Empty,
            Endpoint = Environment.GetEnvironmentVariable("FINAM_ENDPOINT") ?? DefaultEndpoint,
        };
    }
}
