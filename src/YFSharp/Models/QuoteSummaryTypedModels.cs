using System.Text.Json;
using System.Text.Json.Serialization;

namespace YFSharp.Models;

public sealed record TickerInfo
{
    public string Symbol { get; init; } = string.Empty;

    public AssetProfile? AssetProfile { get; init; }

    public SummaryDetail? SummaryDetail { get; init; }

    public DefaultKeyStatistics? DefaultKeyStatistics { get; init; }

    public FinancialData? FinancialData { get; init; }

    public JsonElement? QuoteType { get; init; }

    public QuoteSummary RawSummary { get; init; } = new();
}

public sealed record FastInfo
{
    public string Symbol { get; init; } = string.Empty;

    public string? ShortName { get; init; }

    public string? LongName { get; init; }

    public string? Currency { get; init; }

    public string? Exchange { get; init; }

    public string? FullExchangeName { get; init; }

    public string? MarketState { get; init; }

    public decimal? LastPrice { get; init; }

    public decimal? LastChange { get; init; }

    public decimal? LastChangePercent { get; init; }

    public decimal? PreviousClose { get; init; }

    public decimal? Open { get; init; }

    public decimal? DayHigh { get; init; }

    public decimal? DayLow { get; init; }

    public decimal? FiftyTwoWeekHigh { get; init; }

    public decimal? FiftyTwoWeekLow { get; init; }

    public decimal? MarketCap { get; init; }

    public long? LastVolume { get; init; }

    public Quote RawQuote { get; init; } = new();

    public static FastInfo FromQuote(Quote quote) => new()
    {
        Symbol = quote.Symbol,
        ShortName = quote.ShortName,
        LongName = quote.LongName,
        Currency = quote.Currency,
        Exchange = quote.Exchange,
        FullExchangeName = quote.FullExchangeName,
        MarketState = quote.MarketState,
        LastPrice = quote.RegularMarketPrice,
        LastChange = quote.RegularMarketChange,
        LastChangePercent = quote.RegularMarketChangePercent,
        PreviousClose = quote.RegularMarketPreviousClose,
        Open = quote.RegularMarketOpen,
        DayHigh = quote.RegularMarketDayHigh,
        DayLow = quote.RegularMarketDayLow,
        FiftyTwoWeekHigh = quote.FiftyTwoWeekHigh,
        FiftyTwoWeekLow = quote.FiftyTwoWeekLow,
        MarketCap = quote.MarketCap,
        LastVolume = quote.RegularMarketVolume,
        RawQuote = quote
    };
}

public sealed record AssetProfile
{
    public string? Address1 { get; init; }

    public string? Address2 { get; init; }

    public string? City { get; init; }

    public string? State { get; init; }

    public string? Zip { get; init; }

    public string? Country { get; init; }

    public string? Phone { get; init; }

    public string? Website { get; init; }

    public string? Industry { get; init; }

    public string? IndustryKey { get; init; }

    public string? IndustryDisp { get; init; }

    public string? Sector { get; init; }

    public string? SectorKey { get; init; }

    public string? SectorDisp { get; init; }

    public string? LongBusinessSummary { get; init; }

    public long? FullTimeEmployees { get; init; }

    public IReadOnlyList<CompanyOfficer> CompanyOfficers { get; init; } = [];

    public int? AuditRisk { get; init; }

    public int? BoardRisk { get; init; }

    public int? CompensationRisk { get; init; }

    public int? ShareHolderRightsRisk { get; init; }

    public int? OverallRisk { get; init; }

    public DateTimeOffset? GovernanceEpochDate { get; init; }

    public DateTimeOffset? CompensationAsOfEpochDate { get; init; }

    public string? IrWebsite { get; init; }

    public int? MaxAge { get; init; }

    public JsonElement Raw { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record CompanyOfficer
{
    public int? MaxAge { get; init; }

    public string? Name { get; init; }

    public int? Age { get; init; }

    public string? Title { get; init; }

    public int? YearBorn { get; init; }

    public int? FiscalYear { get; init; }

    public decimal? TotalPay { get; init; }

    public decimal? ExercisedValue { get; init; }

    public decimal? UnexercisedValue { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record SummaryDetail
{
    public int? MaxAge { get; init; }

    public decimal? PreviousClose { get; init; }

    public decimal? RegularMarketOpen { get; init; }

    public decimal? Open { get; init; }

    public decimal? TwoHundredDayAverage { get; init; }

    public decimal? TrailingAnnualDividendYield { get; init; }

    public decimal? TrailingAnnualDividendRate { get; init; }

    public decimal? PayoutRatio { get; init; }

    public long? Volume { get; init; }

    public long? RegularMarketVolume { get; init; }

    public long? AverageVolume { get; init; }

    public long? AverageVolume10days { get; init; }

    public long? AverageDailyVolume10Day { get; init; }

    public decimal? Bid { get; init; }

    public decimal? Ask { get; init; }

    public int? BidSize { get; init; }

    public int? AskSize { get; init; }

    public decimal? MarketCap { get; init; }

    public decimal? FiftyTwoWeekLow { get; init; }

    public decimal? FiftyTwoWeekHigh { get; init; }

    public decimal? PriceToSalesTrailing12Months { get; init; }

    public decimal? FiftyDayAverage { get; init; }

    public decimal? DividendRate { get; init; }

    public decimal? DividendYield { get; init; }

    public DateTimeOffset? ExDividendDate { get; init; }

    public DateTimeOffset? StartDate { get; init; }

    public DateTimeOffset? ExpireDate { get; init; }

    public decimal? Beta { get; init; }

    public long? CirculatingSupply { get; init; }

    public decimal? RegularMarketDayLow { get; init; }

    public decimal? RegularMarketDayHigh { get; init; }

    public decimal? FiveYearAvgDividendYield { get; init; }

    public string? Currency { get; init; }

    public string? FromCurrency { get; init; }

    public string? ToCurrency { get; init; }

    public string? Algorithm { get; init; }

    public string? FiftyTwoWeekRange { get; init; }

    public string? Exchange { get; init; }

    public string? QuoteType { get; init; }

    public string? Symbol { get; init; }

    public string? UnderlyingSymbol { get; init; }

    public string? ShortName { get; init; }

    public string? LongName { get; init; }

    public DateTimeOffset? FirstTradeDateEpochUtc { get; init; }

    public string? TimeZoneFullName { get; init; }

    public string? TimeZoneShortName { get; init; }

    public string? Uuid { get; init; }

    public string? MessageBoardId { get; init; }

    public int? GmtOffSetMilliseconds { get; init; }

    public int? PriceHint { get; init; }

    public JsonElement Raw { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record DefaultKeyStatistics
{
    public int? MaxAge { get; init; }

    public decimal? EnterpriseValue { get; init; }

    public decimal? ForwardPe { get; init; }

    public decimal? ProfitMargins { get; init; }

    public long? FloatShares { get; init; }

    public long? SharesOutstanding { get; init; }

    public long? SharesShort { get; init; }

    public DateTimeOffset? SharesShortPriorMonthDate { get; init; }

    public long? SharesShortPriorMonth { get; init; }

    public DateTimeOffset? DateShortInterest { get; init; }

    public decimal? SharesPercentSharesOut { get; init; }

    public decimal? HeldPercentInsiders { get; init; }

    public decimal? HeldPercentInstitutions { get; init; }

    public decimal? ShortRatio { get; init; }

    public decimal? ShortPercentOfFloat { get; init; }

    public decimal? Beta { get; init; }

    public long? ImpliedSharesOutstanding { get; init; }

    public decimal? BookValue { get; init; }

    public decimal? PriceToBook { get; init; }

    public DateTimeOffset? LastFiscalYearEnd { get; init; }

    public DateTimeOffset? NextFiscalYearEnd { get; init; }

    public DateTimeOffset? MostRecentQuarter { get; init; }

    public decimal? EarningsQuarterlyGrowth { get; init; }

    public decimal? NetIncomeToCommon { get; init; }

    public decimal? TrailingEps { get; init; }

    public decimal? ForwardEps { get; init; }

    public decimal? PegRatio { get; init; }

    public string? LastSplitFactor { get; init; }

    public DateTimeOffset? LastSplitDate { get; init; }

    public decimal? EnterpriseToRevenue { get; init; }

    public decimal? EnterpriseToEbitda { get; init; }

    [JsonPropertyName("52WeekChange")]
    public decimal? FiftyTwoWeekChange { get; init; }

    [JsonPropertyName("SandP52WeekChange")]
    public decimal? SandP52WeekChange { get; init; }

    public decimal? LastDividendValue { get; init; }

    public DateTimeOffset? LastDividendDate { get; init; }

    public JsonElement Raw { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record FinancialData
{
    public int? MaxAge { get; init; }

    public decimal? CurrentPrice { get; init; }

    public decimal? TargetHighPrice { get; init; }

    public decimal? TargetLowPrice { get; init; }

    public decimal? TargetMeanPrice { get; init; }

    public decimal? TargetMedianPrice { get; init; }

    public decimal? RecommendationMean { get; init; }

    public string? RecommendationKey { get; init; }

    public int? NumberOfAnalystOpinions { get; init; }

    public decimal? TotalCash { get; init; }

    public decimal? TotalCashPerShare { get; init; }

    public decimal? Ebitda { get; init; }

    public decimal? TotalDebt { get; init; }

    public decimal? QuickRatio { get; init; }

    public decimal? CurrentRatio { get; init; }

    public decimal? TotalRevenue { get; init; }

    public decimal? DebtToEquity { get; init; }

    public decimal? RevenuePerShare { get; init; }

    public decimal? ReturnOnAssets { get; init; }

    public decimal? ReturnOnEquity { get; init; }

    public decimal? GrossProfits { get; init; }

    public decimal? FreeCashflow { get; init; }

    public decimal? OperatingCashflow { get; init; }

    public decimal? EarningsGrowth { get; init; }

    public decimal? RevenueGrowth { get; init; }

    public decimal? GrossMargins { get; init; }

    public decimal? EbitdaMargins { get; init; }

    public decimal? OperatingMargins { get; init; }

    public decimal? ProfitMargins { get; init; }

    public string? FinancialCurrency { get; init; }

    public JsonElement Raw { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record CalendarEvents
{
    public int? MaxAge { get; init; }

    public EarningsCalendar? Earnings { get; init; }

    public DateTimeOffset? ExDividendDate { get; init; }

    public DateTimeOffset? DividendDate { get; init; }

    public JsonElement Raw { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record EarningsCalendar
{
    [JsonPropertyName("earningsDate")]
    public IReadOnlyList<DateTimeOffset?> EarningsDates { get; init; } = [];

    public decimal? EarningsAverage { get; init; }

    public decimal? EarningsLow { get; init; }

    public decimal? EarningsHigh { get; init; }

    public decimal? RevenueAverage { get; init; }

    public decimal? RevenueLow { get; init; }

    public decimal? RevenueHigh { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record SecFilings
{
    public int? MaxAge { get; init; }

    public IReadOnlyList<SecFiling> Filings { get; init; } = [];

    public JsonElement Raw { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record SecFiling
{
    public DateTimeOffset? EpochDate { get; init; }

    public string? Date { get; init; }

    public string? Type { get; init; }

    public string? Title { get; init; }

    public string? EdgarUrl { get; init; }

    public IReadOnlyList<SecFilingExhibit> Exhibits { get; init; } = [];

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record SecFilingExhibit
{
    public string? Type { get; init; }

    public string? Url { get; init; }

    public string? Description { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record RecommendationTrend
{
    public int? MaxAge { get; init; }

    public IReadOnlyList<RecommendationTrendItem> Trend { get; init; } = [];

    public JsonElement Raw { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record RecommendationTrendItem
{
    public string? Period { get; init; }

    public int? StrongBuy { get; init; }

    public int? Buy { get; init; }

    public int? Hold { get; init; }

    public int? Sell { get; init; }

    public int? StrongSell { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record UpgradeDowngradeHistory
{
    public int? MaxAge { get; init; }

    public IReadOnlyList<UpgradeDowngrade> History { get; init; } = [];

    public JsonElement Raw { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record UpgradeDowngrade
{
    [JsonPropertyName("epochGradeDate")]
    public DateTimeOffset? GradeDate { get; init; }

    public string? Firm { get; init; }

    public string? ToGrade { get; init; }

    public string? FromGrade { get; init; }

    public string? Action { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record EsgScores
{
    public int? MaxAge { get; init; }

    public decimal? TotalEsg { get; init; }

    public decimal? EnvironmentScore { get; init; }

    public decimal? SocialScore { get; init; }

    public decimal? GovernanceScore { get; init; }

    public int? RatingYear { get; init; }

    public int? RatingMonth { get; init; }

    public int? HighestControversy { get; init; }

    public int? PeerCount { get; init; }

    public string? EsgPerformance { get; init; }

    public string? PeerGroup { get; init; }

    public IReadOnlyList<string> RelatedControversy { get; init; } = [];

    public decimal? Percentile { get; init; }

    public decimal? PeerEsgScorePerformanceMin { get; init; }

    public decimal? PeerEsgScorePerformanceAvg { get; init; }

    public decimal? PeerEsgScorePerformanceMax { get; init; }

    public JsonElement Raw { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}
