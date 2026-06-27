namespace YFSharp.Models;

public sealed record PriceBar
{
    public DateTimeOffset Time { get; init; }

    public decimal? Open { get; init; }

    public decimal? High { get; init; }

    public decimal? Low { get; init; }

    public decimal? Close { get; init; }

    public decimal? AdjustedClose { get; init; }

    public long? Volume { get; init; }

    public decimal? Dividend { get; init; }

    public decimal? StockSplit { get; init; }

    public decimal? CapitalGain { get; init; }

    public bool Repaired { get; init; }
}
