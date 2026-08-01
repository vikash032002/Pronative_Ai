using Microsoft.Azure.Cosmos;
using OuterloopLabApi.Models;

namespace OuterloopLabApi.Services;

public sealed class ConversionAuditRepository
{
    private readonly Container _container;

    public ConversionAuditRepository(CosmosClient cosmosClient, AppConfig config)
    {
        _container = cosmosClient.GetContainer(config.CosmosDbDatabase, config.CosmosDbContainer);
    }

    public async Task<AuditTrailRecord> AddAsync(AuditTrailRecord record, CancellationToken cancellationToken)
    {
        var response = await _container.CreateItemAsync(record, new PartitionKey(record.PartitionKey), cancellationToken: cancellationToken);
        return response.Resource;
    }

    public async Task<AuditTrailRecord?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        // Query-based lookup because we don't know partition key from the id alone.
        var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id").WithParameter("@id", id);

        using var iterator = _container.GetItemQueryIterator<AuditTrailRecord>(query, requestOptions: new QueryRequestOptions
        {
            MaxItemCount = 1
        });

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            var item = response.Resource.FirstOrDefault();
            if (item is not null)
                return item;
        }

        return null;
    }

    public async Task<IReadOnlyList<AuditTrailRecord>> SearchAsync(
        string? sourceCurrency,
        string? targetCurrency,
        DateTime? startUtc,
        DateTime? endUtc,
        int limit,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, object>();
        var predicates = new List<string>();

        string? partitionKey = null;
        if (!string.IsNullOrWhiteSpace(sourceCurrency) && !string.IsNullOrWhiteSpace(targetCurrency))
        {
            partitionKey = BuildPartitionKey(sourceCurrency, targetCurrency);
            predicates.Add("c.partitionKey = @pk");
            parameters["@pk"] = partitionKey;
        }

        if (!string.IsNullOrWhiteSpace(sourceCurrency))
        {
            predicates.Add("c.sourceCurrency = @source");
            parameters["@source"] = sourceCurrency;
        }
        if (!string.IsNullOrWhiteSpace(targetCurrency))
        {
            predicates.Add("c.targetCurrency = @target");
            parameters["@target"] = targetCurrency;
        }

        if (startUtc.HasValue)
        {
            predicates.Add("c.executedAtUtc >= @startUtc");
            parameters["@startUtc"] = startUtc.Value;
        }
        if (endUtc.HasValue)
        {
            predicates.Add("c.executedAtUtc <= @endUtc");
            parameters["@endUtc"] = endUtc.Value;
        }

        var whereClause = predicates.Count > 0 ? $" WHERE {string.Join(" AND ", predicates)}" : string.Empty;
        // Order by newest first for analyst usage.
        var queryText = $"SELECT * FROM c{whereClause} ORDER BY c.executedAtUtc DESC OFFSET 0 LIMIT @limit";

        var query = new QueryDefinition(queryText);
        foreach (var (key, value) in parameters)
        {
            query.WithParameter(key, value);
        }
        query.WithParameter("@limit", limit);

        var results = new List<AuditTrailRecord>(Math.Min(limit, 50));
        using var iterator = _container.GetItemQueryIterator<AuditTrailRecord>(query, requestOptions: new QueryRequestOptions
        {
            MaxItemCount = Math.Min(100, limit)
        });

        while (iterator.HasMoreResults && results.Count < limit)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(response.Resource.Take(limit - results.Count));
        }

        return results;
    }

    private static string BuildPartitionKey(string sourceCurrency, string targetCurrency)
        => $"{sourceCurrency.Trim().ToUpperInvariant()}-{targetCurrency.Trim().ToUpperInvariant()}";
}
