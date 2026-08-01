namespace OuterloopLabApi;

public sealed class AppConfig
{
    public string CosmosDbUri { get; init; } = string.Empty;
    public string CosmosDbDatabase { get; init; } = string.Empty;
    public string CosmosDbContainer { get; init; } = string.Empty;
    public string CosmosDbAccountName { get; init; } = string.Empty;
    public string CosmosDbResourceGroup { get; init; } = string.Empty;
    public string CosmosDbRegion { get; init; } = string.Empty;
    public string ManagedIdentityClientId { get; init; } = string.Empty;
    public string CurrencyApiBaseUrl { get; init; } = "https://frankfurter.dev";
}
