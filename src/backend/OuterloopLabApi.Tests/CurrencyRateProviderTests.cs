using System.Net;
using System.Text;
using System.Text.Json;
using OuterloopLabApi;
using OuterloopLabApi.Services;

namespace OuterloopLabApi.Tests;

public sealed class CurrencyRateProviderTests
{
    [Fact]
    public async Task Normalizes_RatesPayload()
    {
        var json = "{\"base\":\"USD\",\"date\":\"2026-08-01\",\"rates\":{\"EUR\":0.92}}";
        var provider = CreateProvider(json);

        var rate = await provider.GetNormalizedRateAsync("USD", "EUR", CancellationToken.None);
        Assert.Equal("EUR", rate.TargetCurrency);
        Assert.Equal(0.92m, rate.Rate);
        Assert.Equal("2026-08-01", rate.ProviderDateMarker);
    }

    [Fact]
    public async Task Normalizes_ConversionRatesPayload()
    {
        var json = "{\"base\":\"USD\",\"date\":\"2026-08-01\",\"conversion_rates\":{\"EUR\":1.2345}}";
        var provider = CreateProvider(json);

        var rate = await provider.GetNormalizedRateAsync("USD", "EUR", CancellationToken.None);
        Assert.Equal(1.2345m, rate.Rate);
        Assert.Equal("2026-08-01", rate.ProviderDateMarker);
    }

    [Fact]
    public async Task MissingRates_ThrowsDomainException()
    {
        var json = "{\"base\":\"USD\",\"date\":\"2026-08-01\"}";
        var provider = CreateProvider(json);

        await Assert.ThrowsAsync<OuterloopLabApi.Services.Exceptions.CurrencyRateProviderUnavailableException>(
            () => provider.GetNormalizedRateAsync("USD", "EUR", CancellationToken.None));
    }

    private static CurrencyRateProvider CreateProvider(string json)
    {
        var handler = new FakeHttpMessageHandler(json);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") };

        var config = new AppConfig
        {
            CosmosDbUri = "https://example.test/",
            CosmosDbDatabase = "db",
            CosmosDbContainer = "container",
            CosmosDbAccountName = "account",
            CosmosDbResourceGroup = "rg",
            CosmosDbRegion = "region",
            ManagedIdentityClientId = "mi",
            CurrencyApiBaseUrl = "https://frankfurter.dev"
        };

        return new CurrencyRateProvider(httpClient, config);
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _json;

        public FakeHttpMessageHandler(string json)
        {
            _json = json;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var msg = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_json, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(msg);
        }
    }
}
