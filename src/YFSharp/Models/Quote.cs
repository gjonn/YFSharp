using System.Text.Json;
using System.Text.Json.Serialization;

namespace YFSharp.Models;

public sealed record Quote
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; init; } = string.Empty;

    [JsonPropertyName("shortName")]
    public string? ShortName { get; init; }

    [JsonPropertyName("longName")]
    public string? LongName { get; init; }

    [JsonPropertyName("quoteType")]
    public string? QuoteType { get; init; }

    [JsonPropertyName("currency")]
    public string? Currency { get; init; }

    [JsonPropertyName("exchange")]
    public string? Exchange { get; init; }

    [JsonPropertyName("fullExchangeName")]
    public string? FullExchangeName { get; init; }

    [JsonPropertyName("marketState")]
    public string? MarketState { get; init; }

    [JsonPropertyName("regularMarketPrice")]
    public decimal? RegularMarketPrice { get; init; }

    [JsonPropertyName("regularMarketChange")]
    public decimal? RegularMarketChange { get; init; }

    [JsonPropertyName("regularMarketChangePercent")]
    public decimal? RegularMarketChangePercent { get; init; }

    [JsonPropertyName("regularMarketPreviousClose")]
    public decimal? RegularMarketPreviousClose { get; init; }

    [JsonPropertyName("regularMarketOpen")]
    public decimal? RegularMarketOpen { get; init; }

    [JsonPropertyName("regularMarketDayHigh")]
    public decimal? RegularMarketDayHigh { get; init; }

    [JsonPropertyName("regularMarketDayLow")]
    public decimal? RegularMarketDayLow { get; init; }

    [JsonPropertyName("fiftyTwoWeekHigh")]
    public decimal? FiftyTwoWeekHigh { get; init; }

    [JsonPropertyName("fiftyTwoWeekLow")]
    public decimal? FiftyTwoWeekLow { get; init; }

    [JsonPropertyName("marketCap")]
    public decimal? MarketCap { get; init; }

    [JsonPropertyName("regularMarketVolume")]
    public long? RegularMarketVolume { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}
