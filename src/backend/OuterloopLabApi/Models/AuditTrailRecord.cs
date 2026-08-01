using System.Text.Json.Serialization;

namespace OuterloopLabApi.Models;

public sealed class AuditTrailRecord
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    // Must be a stable string since we use it for Cosmos partition key.
    [JsonPropertyName("partitionKey")]
    public string PartitionKey { get; set; } = string.Empty;

    [JsonPropertyName("sourceCurrency")]
    public string SourceCurrency { get; set; } = string.Empty;
    [JsonPropertyName("targetCurrency")]
    public string TargetCurrency { get; set; } = string.Empty;

    [JsonPropertyName("requestedAmount")]
    public decimal RequestedAmount { get; set; }
    [JsonPropertyName("convertedAmount")]
    public decimal ConvertedAmount { get; set; }
    [JsonPropertyName("exchangeRate")]
    public decimal ExchangeRate { get; set; }

    [JsonPropertyName("providerDateMarker")]
    public string ProviderDateMarker { get; set; } = string.Empty;
    [JsonPropertyName("providerSequenceMarker")]
    public string? ProviderSequenceMarker { get; set; }

    // Backend execution timestamp in UTC.
    [JsonPropertyName("executedAtUtc")]
    public DateTime ExecutedAtUtc { get; set; }

    // Convenience fields for ordering/debugging.
    [JsonPropertyName("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }
}
