using System.Text.Json;
using System.Text.Json.Serialization;

namespace YFSharp.Models;

public sealed record SearchResult
{
    public IReadOnlyList<SearchQuote> Quotes { get; init; } = [];

    public IReadOnlyList<SearchNews> News { get; init; } = [];

    public IReadOnlyList<JsonElement> Lists { get; init; } = [];

    public IReadOnlyList<SearchList> ListResults { get; init; } = [];

    public IReadOnlyList<JsonElement> Research { get; init; } = [];

    public IReadOnlyList<SearchResearchReport> ResearchReports { get; init; } = [];

    public IReadOnlyList<JsonElement> Navigation { get; init; } = [];

    public IReadOnlyList<SearchNavigationLink> NavigationLinks { get; init; } = [];

    public IReadOnlyList<SearchQuote> RecommendedSymbols { get; init; } = [];

    public IReadOnlyList<JsonElement> CulturalAssets { get; init; } = [];

    public JsonElement Raw { get; init; }
}

public sealed record SearchQuote
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; init; } = string.Empty;

    [JsonPropertyName("shortname")]
    public string? ShortName { get; init; }

    [JsonPropertyName("longname")]
    public string? LongName { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("quoteType")]
    public string? QuoteType { get; init; }

    [JsonPropertyName("exchange")]
    public string? Exchange { get; init; }

    [JsonPropertyName("exchDisp")]
    public string? ExchangeDisplay { get; init; }

    [JsonPropertyName("index")]
    public string? Index { get; init; }

    [JsonPropertyName("score")]
    public decimal? Score { get; init; }

    [JsonPropertyName("typeDisp")]
    public string? TypeDisplay { get; init; }

    [JsonPropertyName("isYahooFinance")]
    public bool? IsYahooFinance { get; init; }

    [JsonPropertyName("permalink")]
    public string? Permalink { get; init; }

    [JsonPropertyName("sector")]
    public string? Sector { get; init; }

    [JsonPropertyName("sectorDisp")]
    public string? SectorDisplay { get; init; }

    [JsonPropertyName("industry")]
    public string? Industry { get; init; }

    [JsonPropertyName("industryDisp")]
    public string? IndustryDisplay { get; init; }

    public JsonElement Raw { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record SearchNews
{
    [JsonPropertyName("uuid")]
    public string? Uuid { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("link")]
    public string? Link { get; init; }

    [JsonPropertyName("publisher")]
    public string? Publisher { get; init; }

    [JsonPropertyName("providerPublishTime")]
    public long? ProviderPublishTime { get; init; }

    [JsonIgnore]
    public DateTimeOffset? ProviderPublishedAt =>
        ProviderPublishTime is null ? null : DateTimeOffset.FromUnixTimeSeconds(ProviderPublishTime.Value);

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("thumbnail")]
    public SearchNewsThumbnail? Thumbnail { get; init; }

    [JsonPropertyName("relatedTickers")]
    public IReadOnlyList<string> RelatedTickers { get; init; } = [];

    public JsonElement Raw { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record SearchNewsThumbnail
{
    [JsonPropertyName("originalUrl")]
    public string? OriginalUrl { get; init; }

    [JsonPropertyName("resolutions")]
    public IReadOnlyList<SearchNewsThumbnailResolution> Resolutions { get; init; } = [];

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record SearchNewsThumbnailResolution
{
    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("width")]
    public int? Width { get; init; }

    [JsonPropertyName("height")]
    public int? Height { get; init; }

    [JsonPropertyName("tag")]
    public string? Tag { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record SearchList
{
    [JsonPropertyName("slug")]
    public string? Slug { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("canonicalName")]
    public string? CanonicalName { get; init; }

    [JsonPropertyName("symbolCount")]
    public int? SymbolCount { get; init; }

    [JsonPropertyName("dailyPercentGain")]
    public decimal? DailyPercentGain { get; init; }

    [JsonPropertyName("iconUrl")]
    public string? IconUrl { get; init; }

    public JsonElement Raw { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record SearchResearchReport
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("reportId")]
    public string? ReportId { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("headline")]
    public string? Headline { get; init; }

    [JsonPropertyName("provider")]
    public string? Provider { get; init; }

    [JsonPropertyName("publisher")]
    public string? Publisher { get; init; }

    [JsonPropertyName("symbol")]
    public string? Symbol { get; init; }

    [JsonPropertyName("companyName")]
    public string? CompanyName { get; init; }

    [JsonPropertyName("reportType")]
    public string? ReportType { get; init; }

    [JsonPropertyName("rating")]
    public string? Rating { get; init; }

    [JsonPropertyName("investmentRating")]
    public string? InvestmentRating { get; init; }

    [JsonPropertyName("targetPrice")]
    public decimal? TargetPrice { get; init; }

    [JsonPropertyName("providerPublishTime")]
    public long? ProviderPublishTime { get; init; }

    [JsonPropertyName("publishedOn")]
    public long? PublishedOn { get; init; }

    [JsonPropertyName("reportDate")]
    public long? ReportDate { get; init; }

    [JsonIgnore]
    public DateTimeOffset? ProviderPublishedAt =>
        ProviderPublishTime is null ? null : DateTimeOffset.FromUnixTimeSeconds(ProviderPublishTime.Value);

    [JsonIgnore]
    public DateTimeOffset? PublishedAt =>
        PublishedOn is null ? null : DateTimeOffset.FromUnixTimeSeconds(PublishedOn.Value);

    [JsonIgnore]
    public DateTimeOffset? ReportedAt =>
        ReportDate is null ? null : DateTimeOffset.FromUnixTimeSeconds(ReportDate.Value);

    [JsonPropertyName("link")]
    public string? Link { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonIgnore]
    public string? DisplayUrl => Link ?? Url;

    [JsonPropertyName("summary")]
    public string? Summary { get; init; }

    [JsonPropertyName("abstract")]
    public string? Abstract { get; init; }

    public JsonElement Raw { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record SearchNavigationLink
{
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("navTitle")]
    public string? NavigationTitle { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonIgnore]
    public string? DisplayTitle => Title ?? NavigationTitle ?? Name;

    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("navUrl")]
    public string? NavigationUrl { get; init; }

    [JsonIgnore]
    public string? DisplayUrl => Url ?? NavigationUrl;

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("category")]
    public string? Category { get; init; }

    public JsonElement Raw { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}
