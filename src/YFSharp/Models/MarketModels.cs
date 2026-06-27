using System.Text.Json;

namespace YFSharp.Models;

public sealed record MarketData
{
    public MarketRegion Region { get; init; }

    public IReadOnlyDictionary<string, MarketSummaryItem> SummaryByExchange { get; init; } =
        new Dictionary<string, MarketSummaryItem>();

    public MarketStatus? Status { get; init; }

    public JsonElement? RawStatus { get; init; }
}

public sealed record MarketSummaryItem
{
    public string Exchange { get; init; } = string.Empty;

    public string? ShortName { get; init; }

    public decimal? RegularMarketPrice { get; init; }

    public decimal? RegularMarketChange { get; init; }

    public decimal? RegularMarketChangePercent { get; init; }

    public JsonElement Raw { get; init; }
}

public sealed record MarketStatus
{
    public string? Id { get; init; }

    public string? Name { get; init; }

    public string? Status { get; init; }

    public string? Message { get; init; }

    public DateTimeOffset? Open { get; init; }

    public DateTimeOffset? Close { get; init; }

    public MarketTimezone? Timezone { get; init; }

    public JsonElement Raw { get; init; }
}

public sealed record MarketTimezone
{
    public string? Tz { get; init; }

    public string? Short { get; init; }

    public string? Long { get; init; }

    public string? GmtOffset { get; init; }

    public JsonElement Raw { get; init; }
}
