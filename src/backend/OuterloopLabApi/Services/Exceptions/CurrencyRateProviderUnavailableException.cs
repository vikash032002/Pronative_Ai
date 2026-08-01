namespace OuterloopLabApi.Services.Exceptions;

public sealed class CurrencyRateProviderUnavailableException : Exception
{
    public CurrencyRateProviderUnavailableException(string message) : base(message)
    {
    }
}
