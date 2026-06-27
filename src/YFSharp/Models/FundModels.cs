using System.Text.Json;
using System.Text.Json.Serialization;

namespace YFSharp.Models;

public sealed record FundData
{
    public string Symbol { get; init; } = string.Empty;

    public string? QuoteType { get; init; }

    public string? Description { get; init; }

    public FundOverview? FundOverview { get; init; }

    public IReadOnlyList<FundOperationMetric> FundOperations { get; init; } = [];

    public FundAssetClasses AssetClasses { get; init; } = new();

    public IReadOnlyList<FundHolding> TopHoldings { get; init; } = [];

    public IReadOnlyList<FundPeerMetric> EquityHoldings { get; init; } = [];

    public IReadOnlyList<FundPeerMetric> BondHoldings { get; init; } = [];

    public IReadOnlyDictionary<string, decimal?> BondRatings { get; init; } =
        new Dictionary<string, decimal?>();

    public IReadOnlyDictionary<string, decimal?> SectorWeightings { get; init; } =
        new Dictionary<string, decimal?>();

    public QuoteSummary RawSummary { get; init; } = new();
}

public sealed record FundOverview
{
    public string? CategoryName { get; init; }

    public string? Family { get; init; }

    public string? LegalType { get; init; }
}

public sealed record FundAssetClasses
{
    public decimal? CashPosition { get; init; }

    public decimal? StockPosition { get; init; }

    public decimal? BondPosition { get; init; }

    public decimal? PreferredPosition { get; init; }

    public decimal? ConvertiblePosition { get; init; }

    public decimal? OtherPosition { get; init; }
}

public sealed record FundHolding
{
    public string Symbol { get; init; } = string.Empty;

    public string? Name { get; init; }

    public decimal? HoldingPercent { get; init; }

    public JsonElement Raw { get; init; }
}

public sealed record FundOperationMetric
{
    public string Attribute { get; init; } = string.Empty;

    public decimal? Fund { get; init; }

    public decimal? CategoryAverage { get; init; }
}

public sealed record FundPeerMetric
{
    public string Average { get; init; } = string.Empty;

    public decimal? Fund { get; init; }

    public decimal? CategoryAverage { get; init; }
}

public sealed record FundProfile
{
    public string? CategoryName { get; init; }

    public string? Family { get; init; }

    public string? LegalType { get; init; }

    public FundFeesExpenses? FeesExpensesInvestment { get; init; }

    public FundFeesExpenses? FeesExpensesInvestmentCat { get; init; }

    public JsonElement Raw { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record FundFeesExpenses
{
    public decimal? AnnualReportExpenseRatio { get; init; }

    public decimal? AnnualHoldingsTurnover { get; init; }

    public decimal? TotalNetAssets { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record TopHoldings
{
    public decimal? CashPosition { get; init; }

    public decimal? StockPosition { get; init; }

    public decimal? BondPosition { get; init; }

    public decimal? PreferredPosition { get; init; }

    public decimal? ConvertiblePosition { get; init; }

    public decimal? OtherPosition { get; init; }

    public IReadOnlyList<YahooFundHolding> Holdings { get; init; } = [];

    public FundEquityHoldings? EquityHoldings { get; init; }

    public FundBondHoldings? BondHoldings { get; init; }

    public IReadOnlyList<Dictionary<string, decimal?>> BondRatings { get; init; } = [];

    public IReadOnlyList<Dictionary<string, decimal?>> SectorWeightings { get; init; } = [];

    public JsonElement Raw { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record YahooFundHolding
{
    public string? Symbol { get; init; }

    public string? HoldingName { get; init; }

    public decimal? HoldingPercent { get; init; }

    public JsonElement Raw { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record FundEquityHoldings
{
    public decimal? PriceToEarnings { get; init; }

    public decimal? PriceToEarningsCat { get; init; }

    public decimal? PriceToBook { get; init; }

    public decimal? PriceToBookCat { get; init; }

    public decimal? PriceToSales { get; init; }

    public decimal? PriceToSalesCat { get; init; }

    public decimal? PriceToCashflow { get; init; }

    public decimal? PriceToCashflowCat { get; init; }

    public decimal? MedianMarketCap { get; init; }

    public decimal? MedianMarketCapCat { get; init; }

    public decimal? ThreeYearEarningsGrowth { get; init; }

    public decimal? ThreeYearEarningsGrowthCat { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record FundBondHoldings
{
    public decimal? Duration { get; init; }

    public decimal? DurationCat { get; init; }

    public decimal? Maturity { get; init; }

    public decimal? MaturityCat { get; init; }

    public decimal? CreditQuality { get; init; }

    public decimal? CreditQualityCat { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}
