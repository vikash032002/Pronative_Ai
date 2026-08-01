using System.Net;
using System.Text.Json;
using OuterloopLabApi.Services.Exceptions;

namespace OuterloopLabApi.Services;

public sealed class CurrencyRateProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public CurrencyRateProvider(HttpClient httpClient, AppConfig config)
    {
        _httpClient = httpClient;
        _baseUrl = config.CurrencyApiBaseUrl;
    }

    public async Task<NormalizedRate> GetNormalizedRateAsync(
        string sourceCurrency,
        string targetCurrency,
        CancellationToken cancellationToken)
    {
        var url = BuildLatestRateUrl(_baseUrl, sourceCurrency, targetCurrency);
        HttpResponseMessage response;

        try
        {
            response = await _httpClient.GetAsync(url, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new CurrencyRateProviderUnavailableException("External currency provider call failed.");
        }

        if (!response.IsSuccessStatusCode)
            throw new CurrencyRateProviderUnavailableException("External currency provider returned a non-success status.");

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        try
        {
            using var doc = JsonDocument.Parse(payload);
            return NormalizeFrankfurterLikePayload(doc.RootElement, sourceCurrency, targetCurrency);
        }
        catch (JsonException)
        {
            throw new CurrencyRateProviderUnavailableException("External currency provider returned invalid JSON.");
        }
    }

    private static string BuildLatestRateUrl(string baseUrl, string sourceCurrency, string targetCurrency)
    {
        var trimmed = baseUrl.TrimEnd('/');
        var from = WebUtility.UrlEncode(sourceCurrency);
        var to = WebUtility.UrlEncode(targetCurrency);
        return $"{trimmed}/latest?base={from}&symbols={to}";
    }

    private static NormalizedRate NormalizeFrankfurterLikePayload(
        JsonElement root,
        string sourceCurrency,
        string targetCurrency)
    {
        // date marker
        var dateMarker = TryGetString(root, "date") ?? TryGetString(root, "provider_date") ?? "";
        if (string.IsNullOrWhiteSpace(dateMarker))
            dateMarker = DateTime.UtcNow.ToString("yyyy-MM-dd");

        // sequence marker (optional)
        var sequenceMarker = TryGetString(root, "timestamp") ?? TryGetString(root, "time");

        // rates object
        JsonElement ratesElement;
        if (TryGetProperty(root, "rates", out ratesElement))
        {
            // ok
        }
        else if (TryGetProperty(root, "conversion_rates", out ratesElement))
        {
            // ok
        }
        else if (TryGetProperty(root, "conversionRates", out ratesElement))
        {
            // ok
        }
        else
        {
            throw new CurrencyRateProviderUnavailableException("External currency provider payload did not include rates.");
        }

        var rate = TryGetDecimalFromRates(ratesElement, targetCurrency);
        return new NormalizedRate(sourceCurrency, targetCurrency, rate, dateMarker, sequenceMarker);
    }

    private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement found)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var p))
        {
            found = p;
            return true;
        }

        found = default;
        return false;
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;
        if (!element.TryGetProperty(propertyName, out var p))
            return null;
        if (p.ValueKind == JsonValueKind.String)
            return p.GetString();
        return p.ValueKind == JsonValueKind.Number ? p.ToString() : null;
    }

    private static decimal TryGetDecimalFromRates(JsonElement ratesElement, string targetCurrency)
    {
        if (ratesElement.ValueKind != JsonValueKind.Object)
            throw new CurrencyRateProviderUnavailableException("Rates element was not an object.");

        if (!ratesElement.TryGetProperty(targetCurrency, out var value))
            throw new CurrencyRateProviderUnavailableException("External currency provider did not include requested target currency in rates.");

        if (value.ValueKind == JsonValueKind.Number)
        {
            if (value.TryGetDecimal(out var d))
                return d;

            // Fallback: parse as string representation.
            if (decimal.TryParse(value.ToString(), out var parsed))
                return parsed;
        }

        if (value.ValueKind == JsonValueKind.String && decimal.TryParse(value.GetString(), out var d2))
            return d2;

        throw new CurrencyRateProviderUnavailableException("External currency provider returned an invalid rate value.");
    }

    public sealed record NormalizedRate(
        string SourceCurrency,
        string TargetCurrency,
        decimal Rate,
        string ProviderDateMarker,
        string? ProviderSequenceMarker);
}
