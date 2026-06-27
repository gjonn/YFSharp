namespace YFSharp.Models;

public sealed record StreamingPrice
{
    public string Id { get; init; } = string.Empty;

    public string Symbol => Id;

    public decimal? Price { get; init; }

    public DateTimeOffset? Time { get; init; }

    public string? Currency { get; init; }

    public string? Exchange { get; init; }

    public int? QuoteType { get; init; }

    public int? MarketHours { get; init; }

    public decimal? ChangePercent { get; init; }

    public long? DayVolume { get; init; }

    public decimal? DayHigh { get; init; }

    public decimal? DayLow { get; init; }

    public decimal? Change { get; init; }

    public string? ShortName { get; init; }

    public DateTimeOffset? ExpirationDate { get; init; }

    public decimal? OpenPrice { get; init; }

    public decimal? PreviousClose { get; init; }

    public decimal? StrikePrice { get; init; }

    public string? UnderlyingSymbol { get; init; }

    public long? OpenInterest { get; init; }

    public long? OptionsType { get; init; }

    public long? MiniOption { get; init; }

    public long? LastSize { get; init; }

    public decimal? Bid { get; init; }

    public long? BidSize { get; init; }

    public decimal? Ask { get; init; }

    public long? AskSize { get; init; }

    public long? PriceHint { get; init; }

    public long? Volume24Hour { get; init; }

    public long? VolumeAllCurrencies { get; init; }

    public string? FromCurrency { get; init; }

    public string? LastMarket { get; init; }

    public double? CirculatingSupply { get; init; }

    public double? MarketCap { get; init; }
}
