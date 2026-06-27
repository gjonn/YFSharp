using Xunit.Abstractions;

namespace YFSharp.Tests;

public sealed class YahooFinanceLiveTests(ITestOutputHelper output)
{
    [LiveYahooFact]
    public async Task Live_GetQuoteAsync_ReturnsQuoteOrTreatsRateLimitAsInconclusive()
    {
        using var client = new YahooFinanceClient(new YahooFinanceClientOptions
        {
            RequestTimeout = TimeSpan.FromSeconds(20)
        });

        try
        {
            var quote = await client.GetQuoteAsync("AAPL");

            Assert.NotNull(quote);
            Assert.Equal("AAPL", quote.Symbol);
            Assert.NotNull(quote.RegularMarketPrice);
        }
        catch (YahooFinanceRateLimitException)
        {
            output.WriteLine("Yahoo /v7/finance/quote returned 429; treating live test as inconclusive.");
        }
        catch (YahooFinanceHttpException ex)
        {
            output.WriteLine($"Yahoo /v7/finance/quote returned {(int)ex.StatusCode} {ex.StatusCode}.");
            throw;
        }
    }
}
