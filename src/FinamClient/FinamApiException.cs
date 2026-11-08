namespace Finam.TradeApi;

/// <summary>Ошибка при обращении к Finam Trade API.</summary>
public sealed class FinamApiException : Exception
{
    public FinamApiException(string message, Exception? inner = null)
        : base(message, inner)
    {
    }
}
