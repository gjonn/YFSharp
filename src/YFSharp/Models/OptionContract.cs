using System.Text.Json;

namespace YFSharp.Models;

public sealed record OptionContract
{
    public string ContractSymbol { get; init; } = string.Empty;

    public DateTimeOffset? LastTradeDate { get; init; }

    public decimal? Strike { get; init; }

    public decimal? LastPrice { get; init; }

    public decimal? Bid { get; init; }

    public decimal? Ask { get; init; }

    public decimal? Change { get; init; }

    public decimal? PercentChange { get; init; }

    public long? Volume { get; init; }

    public long? OpenInterest { get; init; }

    public decimal? ImpliedVolatility { get; init; }

    public bool? InTheMoney { get; init; }

    public string? ContractSize { get; init; }

    public string? Currency { get; init; }

    public IReadOnlyDictionary<string, JsonElement> AdditionalData { get; init; } =
        new Dictionary<string, JsonElement>();
}
