using OuterloopLabApi.Models;
using OuterloopLabApi.Services.Exceptions;

namespace OuterloopLabApi.Services;

public sealed class ConversionService
{
    private readonly CurrencyRateProvider _provider;
    private readonly ConversionAuditRepository _repository;

    public ConversionService(CurrencyRateProvider provider, ConversionAuditRepository repository)
    {
        _provider = provider;
        _repository = repository;
    }

    public async Task<ConversionResult> ConvertAsync(ConversionRequest request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.");

        var source = NormalizeCurrencyCode(request.SourceCurrency, nameof(request.SourceCurrency));
        var target = NormalizeCurrencyCode(request.TargetCurrency, nameof(request.TargetCurrency));

        var normalized = await _provider.GetNormalizedRateAsync(source, target, cancellationToken);
        var convertedAmount = request.Amount * normalized.Rate;
        var executedAtUtc = DateTime.UtcNow;

        var record = new AuditTrailRecord
        {
            Id = Guid.NewGuid().ToString("n"),
            PartitionKey = $"{source}-{target}",
            SourceCurrency = source,
            TargetCurrency = target,
            RequestedAmount = request.Amount,
            ConvertedAmount = convertedAmount,
            ExchangeRate = normalized.Rate,
            ProviderDateMarker = normalized.ProviderDateMarker,
            ProviderSequenceMarker = normalized.ProviderSequenceMarker,
            ExecutedAtUtc = executedAtUtc,
            CreatedAtUtc = executedAtUtc
        };

        await _repository.AddAsync(record, cancellationToken);

        return new ConversionResult
        {
            Id = record.Id,
            SourceCurrency = record.SourceCurrency,
            TargetCurrency = record.TargetCurrency,
            RequestedAmount = record.RequestedAmount,
            ConvertedAmount = record.ConvertedAmount,
            ExchangeRate = record.ExchangeRate,
            ProviderDateMarker = record.ProviderDateMarker,
            ProviderSequenceMarker = record.ProviderSequenceMarker,
            ExecutedAtUtc = record.ExecutedAtUtc
        };
    }

    private static string NormalizeCurrencyCode(string input, string paramName)
    {
        var normalized = input.Trim().ToUpperInvariant();
        if (normalized.Length != 3 || !normalized.All(char.IsLetter))
            throw new ArgumentException($"{paramName} must be a 3-letter currency code.");
        return normalized;
    }
}
