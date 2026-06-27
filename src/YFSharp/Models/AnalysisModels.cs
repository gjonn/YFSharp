using System.Text.Json;
using System.Text.Json.Serialization;

namespace YFSharp.Models;

public sealed record AnalystPriceTargets
{
    public decimal? CurrentPrice { get; init; }

    public decimal? TargetHighPrice { get; init; }

    public decimal? TargetLowPrice { get; init; }

    public decimal? TargetMeanPrice { get; init; }

    public decimal? TargetMedianPrice { get; init; }

    public decimal? RecommendationMean { get; init; }

    public string? RecommendationKey { get; init; }

    public int? NumberOfAnalystOpinions { get; init; }

    public FinancialData RawFinancialData { get; init; } = new();

    public static AnalystPriceTargets FromFinancialData(FinancialData data) => new()
    {
        CurrentPrice = data.CurrentPrice,
        TargetHighPrice = data.TargetHighPrice,
        TargetLowPrice = data.TargetLowPrice,
        TargetMeanPrice = data.TargetMeanPrice,
        TargetMedianPrice = data.TargetMedianPrice,
        RecommendationMean = data.RecommendationMean,
        RecommendationKey = data.RecommendationKey,
        NumberOfAnalystOpinions = data.NumberOfAnalystOpinions,
        RawFinancialData = data
    };
}

public sealed record EarningsHistoryModule
{
    public IReadOnlyList<EarningsHistoryRow> History { get; init; } = [];

    public int? MaxAge { get; init; }

    public JsonElement Raw { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record EarningsHistoryRow
{
    public DateTimeOffset? Quarter { get; init; }

    public decimal? EpsActual { get; init; }

    public decimal? EpsEstimate { get; init; }

    public decimal? EpsDifference { get; init; }

    public decimal? SurprisePercent { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record EarningsTrendModule
{
    public IReadOnlyList<EarningsTrendRow> Trend { get; init; } = [];

    public int? MaxAge { get; init; }

    public JsonElement Raw { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record EarningsTrendRow
{
    public string? Period { get; init; }

    public string? EndDate { get; init; }

    public decimal? Growth { get; init; }

    public EarningsEstimateRow? EarningsEstimate { get; init; }

    public RevenueEstimateRow? RevenueEstimate { get; init; }

    public EpsTrendRow? EpsTrend { get; init; }

    public EpsRevisionsRow? EpsRevisions { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record EarningsEstimateRow
{
    public string? Period { get; init; }

    public string? EndDate { get; init; }

    [JsonPropertyName("avg")]
    public decimal? Average { get; init; }

    public decimal? Low { get; init; }

    public decimal? High { get; init; }

    public decimal? YearAgoEps { get; init; }

    public int? NumberOfAnalysts { get; init; }

    public decimal? Growth { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record RevenueEstimateRow
{
    public string? Period { get; init; }

    public string? EndDate { get; init; }

    [JsonPropertyName("avg")]
    public decimal? Average { get; init; }

    public decimal? Low { get; init; }

    public decimal? High { get; init; }

    public decimal? YearAgoRevenue { get; init; }

    public int? NumberOfAnalysts { get; init; }

    public decimal? Growth { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record EpsTrendRow
{
    public string? Period { get; init; }

    public string? EndDate { get; init; }

    public decimal? Current { get; init; }

    [JsonPropertyName("7daysAgo")]
    public decimal? SevenDaysAgo { get; init; }

    [JsonPropertyName("30daysAgo")]
    public decimal? ThirtyDaysAgo { get; init; }

    [JsonPropertyName("60daysAgo")]
    public decimal? SixtyDaysAgo { get; init; }

    [JsonPropertyName("90daysAgo")]
    public decimal? NinetyDaysAgo { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record EpsRevisionsRow
{
    public string? Period { get; init; }

    public string? EndDate { get; init; }

    public int? UpLast7days { get; init; }

    public int? UpLast30days { get; init; }

    public int? DownLast30days { get; init; }

    public int? DownLast90days { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record GrowthEstimateRow
{
    public string? Period { get; init; }

    public string? EndDate { get; init; }

    public decimal? Growth { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}
