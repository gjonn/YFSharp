namespace YFSharp;

public static class QuoteSummaryModules
{
    public const string SummaryProfile = "summaryProfile";
    public const string SummaryDetail = "summaryDetail";
    public const string AssetProfile = "assetProfile";
    public const string FundProfile = "fundProfile";
    public const string TopHoldings = "topHoldings";
    public const string Price = "price";
    public const string QuoteType = "quoteType";
    public const string EsgScores = "esgScores";
    public const string IncomeStatementHistory = "incomeStatementHistory";
    public const string IncomeStatementHistoryQuarterly = "incomeStatementHistoryQuarterly";
    public const string BalanceSheetHistory = "balanceSheetHistory";
    public const string BalanceSheetHistoryQuarterly = "balanceSheetHistoryQuarterly";
    public const string CashFlowStatementHistory = "cashFlowStatementHistory";
    public const string CashFlowStatementHistoryQuarterly = "cashFlowStatementHistoryQuarterly";
    public const string DefaultKeyStatistics = "defaultKeyStatistics";
    public const string FinancialData = "financialData";
    public const string CalendarEvents = "calendarEvents";
    public const string SecFilings = "secFilings";
    public const string UpgradeDowngradeHistory = "upgradeDowngradeHistory";
    public const string InstitutionOwnership = "institutionOwnership";
    public const string FundOwnership = "fundOwnership";
    public const string MajorDirectHolders = "majorDirectHolders";
    public const string MajorHoldersBreakdown = "majorHoldersBreakdown";
    public const string InsiderTransactions = "insiderTransactions";
    public const string InsiderHolders = "insiderHolders";
    public const string NetSharePurchaseActivity = "netSharePurchaseActivity";
    public const string Earnings = "earnings";
    public const string EarningsHistory = "earningsHistory";
    public const string EarningsTrend = "earningsTrend";
    public const string IndustryTrend = "industryTrend";
    public const string IndexTrend = "indexTrend";
    public const string SectorTrend = "sectorTrend";
    public const string RecommendationTrend = "recommendationTrend";
    public const string FuturesChain = "futuresChain";

    public static readonly string[] Info =
    [
        FinancialData,
        QuoteType,
        DefaultKeyStatistics,
        AssetProfile,
        SummaryDetail
    ];

    public static readonly string[] Funds =
    [
        QuoteType,
        SummaryProfile,
        TopHoldings,
        FundProfile
    ];

    public static readonly string[] FinancialStatements =
    [
        IncomeStatementHistory,
        IncomeStatementHistoryQuarterly,
        BalanceSheetHistory,
        BalanceSheetHistoryQuarterly,
        CashFlowStatementHistory,
        CashFlowStatementHistoryQuarterly
    ];

    public static readonly string[] IncomeStatements =
    [
        IncomeStatementHistory
    ];

    public static readonly string[] QuarterlyIncomeStatements =
    [
        IncomeStatementHistoryQuarterly
    ];

    public static readonly string[] BalanceSheets =
    [
        BalanceSheetHistory
    ];

    public static readonly string[] QuarterlyBalanceSheets =
    [
        BalanceSheetHistoryQuarterly
    ];

    public static readonly string[] CashFlowStatements =
    [
        CashFlowStatementHistory
    ];

    public static readonly string[] QuarterlyCashFlowStatements =
    [
        CashFlowStatementHistoryQuarterly
    ];

    public static readonly string[] Analysis =
    [
        FinancialData,
        EarningsHistory,
        EarningsTrend,
        RecommendationTrend,
        UpgradeDowngradeHistory,
        IndustryTrend,
        IndexTrend,
        SectorTrend
    ];

    public static readonly string[] Holders =
    [
        InstitutionOwnership,
        FundOwnership,
        MajorDirectHolders,
        MajorHoldersBreakdown,
        InsiderTransactions,
        InsiderHolders,
        NetSharePurchaseActivity
    ];

    public static readonly string[] AnalystPriceTargets =
    [
        FinancialData
    ];

    public static readonly string[] EarningsEstimates =
    [
        EarningsTrend
    ];

    public static readonly string[] RevenueEstimates =
    [
        EarningsTrend
    ];

    public static readonly string[] EarningsHistoryDetails =
    [
        EarningsHistory
    ];

    public static readonly string[] EpsTrendDetails =
    [
        EarningsTrend
    ];

    public static readonly string[] EpsRevisionDetails =
    [
        EarningsTrend
    ];

    public static readonly string[] GrowthEstimates =
    [
        EarningsTrend
    ];

    public static readonly string[] MajorHolders =
    [
        MajorHoldersBreakdown
    ];

    public static readonly string[] InstitutionalHolders =
    [
        InstitutionOwnership
    ];

    public static readonly string[] MutualFundHolders =
    [
        FundOwnership
    ];

    public static readonly string[] InsiderPurchases =
    [
        NetSharePurchaseActivity
    ];

    public static readonly string[] InsiderTransactionDetails =
    [
        InsiderTransactions
    ];

    public static readonly string[] InsiderRosterHolders =
    [
        InsiderHolders
    ];

    public static readonly string[] Recommendations =
    [
        UpgradeDowngradeHistory
    ];

    public static readonly string[] RecommendationsSummary =
    [
        RecommendationTrend
    ];

    public static readonly string[] Calendar =
    [
        CalendarEvents
    ];

    public static readonly string[] Filings =
    [
        SecFilings
    ];

    public static readonly string[] Sustainability =
    [
        EsgScores
    ];
}
