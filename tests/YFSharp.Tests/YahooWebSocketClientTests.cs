namespace YFSharp.Tests;

public sealed class YahooWebSocketClientTests
{
    [Fact]
    public void DecodeJsonMessage_ParsesPricingDataFixture()
    {
        var json = File.ReadAllText(Path.Combine("Fixtures", "Yahoo", "streaming-price-aapl.json"));

        var price = YahooWebSocketClient.DecodeJsonMessage(json);

        Assert.Equal("AAPL", price.Id);
        Assert.Equal("AAPL", price.Symbol);
        Assert.Equal(199.5m, price.Price);
        Assert.Equal(new DateTimeOffset(2024, 4, 5, 19, 34, 38, TimeSpan.Zero), price.Time);
        Assert.Equal("USD", price.Currency);
        Assert.Equal("NMS", price.Exchange);
        Assert.Equal(8, price.QuoteType);
        Assert.Equal(1, price.MarketHours);
        Assert.Equal(1.25m, price.ChangePercent);
        Assert.Equal(123456789, price.DayVolume);
        Assert.Equal(201m, price.DayHigh);
        Assert.Equal(198m, price.DayLow);
        Assert.Equal(2.5m, price.Change);
        Assert.Equal("Apple Inc.", price.ShortName);
        Assert.Equal(198.75m, price.OpenPrice);
        Assert.Equal(197m, price.PreviousClose);
        Assert.Equal(199.45m, price.Bid);
        Assert.Equal(2, price.BidSize);
        Assert.Equal(199.55m, price.Ask);
        Assert.Equal(3, price.AskSize);
        Assert.Equal(2, price.PriceHint);
        Assert.Equal(3_000_000_000_000d, price.MarketCap);
    }

    [Fact]
    public void DecodeMessage_ThrowsYahooFinanceExceptionForInvalidBase64()
    {
        var exception = Assert.Throws<YahooFinanceException>(() =>
            YahooWebSocketClient.DecodeMessage("not base64"));

        Assert.IsType<FormatException>(exception.InnerException);
    }

    [Fact]
    public async Task SubscribeAsync_RejectsInvalidSymbolsBeforeConnecting()
    {
        await using var client = new YahooWebSocketClient();

        await Assert.ThrowsAsync<ArgumentException>(() => client.SubscribeAsync(["AAPL", " "]));
        Assert.Empty(client.Subscriptions);
    }
}
