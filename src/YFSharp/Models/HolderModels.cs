using System.Text.Json;
using System.Text.Json.Serialization;

namespace YFSharp.Models;

public sealed record OwnershipModule
{
    public IReadOnlyList<OwnershipHolder> OwnershipList { get; init; } = [];

    public int? MaxAge { get; init; }

    public JsonElement Raw { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record OwnershipHolder
{
    public string? Organization { get; init; }

    public DateTimeOffset? ReportDate { get; init; }

    public decimal? PctHeld { get; init; }

    public long? Position { get; init; }

    public decimal? Value { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record MajorHoldersBreakdown
{
    public decimal? InsidersPercentHeld { get; init; }

    public decimal? InstitutionsPercentHeld { get; init; }

    public decimal? InstitutionsFloatPercentHeld { get; init; }

    public long? InstitutionsCount { get; init; }

    public int? MaxAge { get; init; }

    public JsonElement Raw { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record NetSharePurchaseActivity
{
    public string? Period { get; init; }

    public int? BuyInfoCount { get; init; }

    public long? BuyInfoShares { get; init; }

    public decimal? BuyPercentInsiderShares { get; init; }

    public int? SellInfoCount { get; init; }

    public long? SellInfoShares { get; init; }

    public decimal? SellPercentInsiderShares { get; init; }

    public int? NetInfoCount { get; init; }

    public long? NetInfoShares { get; init; }

    public decimal? NetPercentInsiderShares { get; init; }

    public long? TotalInsiderShares { get; init; }

    public int? MaxAge { get; init; }

    public JsonElement Raw { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record InsiderTransactionsModule
{
    public IReadOnlyList<InsiderTransaction> Transactions { get; init; } = [];

    public int? MaxAge { get; init; }

    public JsonElement Raw { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record InsiderTransaction
{
    public string? FilerName { get; init; }

    public string? FilerRelation { get; init; }

    public string? MoneyText { get; init; }

    public DateTimeOffset? StartDate { get; init; }

    public string? Ownership { get; init; }

    public string? Transaction { get; init; }

    public long? Shares { get; init; }

    public decimal? Value { get; init; }

    public string? FilerUrl { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record InsiderHoldersModule
{
    public IReadOnlyList<InsiderRosterHolder> Holders { get; init; } = [];

    public int? MaxAge { get; init; }

    public JsonElement Raw { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record InsiderRosterHolder
{
    public string? Name { get; init; }

    public string? Relation { get; init; }

    public string? Url { get; init; }

    public string? TransactionDescription { get; init; }

    public DateTimeOffset? LatestTransDate { get; init; }

    public long? PositionDirect { get; init; }

    public DateTimeOffset? PositionDirectDate { get; init; }

    public long? PositionIndirect { get; init; }

    public DateTimeOffset? PositionIndirectDate { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}
