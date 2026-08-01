using System.Text.Json.Serialization;

namespace OuterloopLabApi.Models;

public sealed class ConversionResult
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    [JsonPropertyName("requestedAmount")]
    public decimal RequestedAmount { get; set; }
    [JsonPropertyName("convertedAmount")]
    public decimal ConvertedAmount { get; set; }
    [JsonPropertyName("exchangeRate")]
    public decimal ExchangeRate { get; set; }

    [JsonPropertyName("sourceCurrency")]
    public string SourceCurrency { get; set; } = string.Empty;
    [JsonPropertyName("targetCurrency")]
    public string TargetCurrency { get; set; } = string.Empty;

    [JsonPropertyName("providerDateMarker")]
    public string ProviderDateMarker { get; set; } = string.Empty;
    [JsonPropertyName("providerSequenceMarker")]
    public string? ProviderSequenceMarker { get; set; }

    [JsonPropertyName("executedAtUtc")]
    public DateTime ExecutedAtUtc { get; set; }
}
