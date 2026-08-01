using System.Text.Json.Serialization;

namespace OuterloopLabApi.Models;

public sealed class ConversionRequest
{
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("sourceCurrency")]
    public string SourceCurrency { get; set; } = string.Empty;

    [JsonPropertyName("targetCurrency")]
    public string TargetCurrency { get; set; } = string.Empty;
}
