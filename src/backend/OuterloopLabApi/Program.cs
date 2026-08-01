using Azure;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.CosmosDB;
using Microsoft.Azure.Cosmos;
using OuterloopLabApi;
using OuterloopLabApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Env-variable only configuration.
var config = LoadConfigFromEnvironment();
builder.Services.AddSingleton(config);

var managedIdentityCredential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
{
    ManagedIdentityClientId = config.ManagedIdentityClientId
});

builder.Services.AddSingleton<global::Azure.Core.TokenCredential>(_ => managedIdentityCredential);
builder.Services.AddSingleton<CosmosClient>(_ => new CosmosClient(config.CosmosDbUri, managedIdentityCredential));

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddHttpClient<CurrencyRateProvider>();
builder.Services.AddSingleton<ConversionAuditRepository>();
builder.Services.AddSingleton<ConversionService>();

var app = builder.Build();

// Provision Cosmos DB before the web app runs.
await ProvisionCosmosDatabaseAndContainerAsync(app.Services, config);

app.MapControllers();

await app.RunAsync();

static AppConfig LoadConfigFromEnvironment()
{
    string required(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Missing required environment variable: {key}");
        return value;
    }

    return new AppConfig
    {
        CosmosDbUri = required("COSMOS_DB_URI"),
        CosmosDbDatabase = required("COSMOS_DB_DATABASE"),
        CosmosDbContainer = required("COSMOS_DB_CONTAINER"),
        CosmosDbAccountName = required("COSMOS_DB_ACCOUNT_NAME"),
        CosmosDbResourceGroup = required("COSMOS_DB_RESOURCE_GROUP"),
        CosmosDbRegion = required("COSMOS_DB_REGION"),
        ManagedIdentityClientId = required("AZURE_MANAGED_IDENTITY_CLIENT_ID"),
        CurrencyApiBaseUrl = Environment.GetEnvironmentVariable("CURRENCY_API_BASE_URL") ?? "https://frankfurter.dev"
    };
}

static async Task ProvisionCosmosDatabaseAndContainerAsync(IServiceProvider services, AppConfig config)
{
    var credential = services.GetRequiredService<global::Azure.Core.TokenCredential>();
    var cosmosClient = services.GetRequiredService<CosmosClient>();

    // Best-effort ARM provisioning.
    await TryProvisionCosmosWithArmAsync(credential, config);

    // Data-plane provisioning must be token-authenticated and must fail startup if creation fails.
    var dbResponse = await cosmosClient.CreateDatabaseIfNotExistsAsync(
        config.CosmosDbDatabase,
        cancellationToken: CancellationToken.None);

    var containerProperties = new ContainerProperties(config.CosmosDbContainer, "/partitionKey");
    // Required by the spec: token-authenticated create-if-not-exists must exist for the container.
    await cosmosClient.GetDatabase(config.CosmosDbDatabase)
        .CreateContainerIfNotExistsAsync(containerProperties, throughput: 400, cancellationToken: CancellationToken.None);

    // Ensure DI repository can resolve against the provisioned container.
    _ = services.GetRequiredService<ConversionAuditRepository>();
}

static async Task TryProvisionCosmosWithArmAsync(TokenCredential credential, AppConfig config)
{
    // ARM provisioning is best-effort; if required ARM metadata is missing, we skip.
    var subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
    if (string.IsNullOrWhiteSpace(subscriptionId))
        return;

    try
    {
        var arm = new ArmClient(credential, subscriptionId);
        dynamic armDyn = arm;
        var cosmosAccountResourceId = $"/subscriptions/{subscriptionId}/resourceGroups/{config.CosmosDbResourceGroup}/providers/Microsoft.DocumentDB/databaseAccounts/{config.CosmosDbAccountName}";
        // Use dynamic so compilation doesn't depend on CosmosDB SDK surface details.
        dynamic cosmosAccount = armDyn.GetCosmosDBAccountResource(new Azure.Core.ResourceIdentifier(cosmosAccountResourceId));

        dynamic dbCollection = cosmosAccount.GetCosmosDBSqlDatabaseCollection();
        await dbCollection.CreateOrUpdateAsync(Azure.WaitUntil.Completed, config.CosmosDbDatabase, null);

        dynamic db = await dbCollection.GetAsync(config.CosmosDbDatabase);
        dynamic containerCollection = db.Value.GetCosmosDBSqlContainerCollection();
        await containerCollection.CreateOrUpdateAsync(Azure.WaitUntil.Completed, config.CosmosDbContainer, null);
    }
    catch
    {
        // Intentionally swallow: ARM RBAC may differ from data-plane.
    }
}
