using System.Text.Json;
using YFSharp.Internal;

namespace YFSharp.Models;

public sealed record QuoteSummary
{
    public string Symbol { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, JsonElement> Modules { get; init; } =
        new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

    public bool TryGetModule(string moduleName, out JsonElement module) =>
        Modules.TryGetValue(moduleName, out module);

    public T? GetModule<T>(string moduleName)
        where T : class =>
        TryGetModule(moduleName, out var module)
            ? YahooQuoteSummaryJson.DeserializeModule<T>(module)
            : null;

    public AssetProfile? AssetProfile => GetModule<AssetProfile>(QuoteSummaryModules.AssetProfile);

    public AssetProfile? SummaryProfile => GetModule<AssetProfile>(QuoteSummaryModules.SummaryProfile);

    public SummaryDetail? SummaryDetail => GetModule<SummaryDetail>(QuoteSummaryModules.SummaryDetail);

    public FundProfile? FundProfile => GetModule<FundProfile>(QuoteSummaryModules.FundProfile);

    public TopHoldings? TopHoldings => GetModule<TopHoldings>(QuoteSummaryModules.TopHoldings);

    public DefaultKeyStatistics? DefaultKeyStatistics =>
        GetModule<DefaultKeyStatistics>(QuoteSummaryModules.DefaultKeyStatistics);

    public FinancialData? FinancialData => GetModule<FinancialData>(QuoteSummaryModules.FinancialData);

    public CalendarEvents? CalendarEvents => GetModule<CalendarEvents>(QuoteSummaryModules.CalendarEvents);

    public SecFilings? SecFilings => GetModule<SecFilings>(QuoteSummaryModules.SecFilings);

    public RecommendationTrend? RecommendationTrend =>
        GetModule<RecommendationTrend>(QuoteSummaryModules.RecommendationTrend);

    public UpgradeDowngradeHistory? UpgradeDowngradeHistory =>
        GetModule<UpgradeDowngradeHistory>(QuoteSummaryModules.UpgradeDowngradeHistory);

    public EsgScores? EsgScores => GetModule<EsgScores>(QuoteSummaryModules.EsgScores);

    public IncomeStatementModule? IncomeStatementHistory =>
        GetModule<IncomeStatementModule>(QuoteSummaryModules.IncomeStatementHistory);

    public IncomeStatementModule? IncomeStatementHistoryQuarterly =>
        GetModule<IncomeStatementModule>(QuoteSummaryModules.IncomeStatementHistoryQuarterly);

    public BalanceSheetModule? BalanceSheetHistory =>
        GetModule<BalanceSheetModule>(QuoteSummaryModules.BalanceSheetHistory);

    public BalanceSheetModule? BalanceSheetHistoryQuarterly =>
        GetModule<BalanceSheetModule>(QuoteSummaryModules.BalanceSheetHistoryQuarterly);

    public CashFlowStatementModule? CashFlowStatementHistory =>
        GetModule<CashFlowStatementModule>(QuoteSummaryModules.CashFlowStatementHistory);

    public CashFlowStatementModule? CashFlowStatementHistoryQuarterly =>
        GetModule<CashFlowStatementModule>(QuoteSummaryModules.CashFlowStatementHistoryQuarterly);

    public EarningsHistoryModule? EarningsHistory =>
        GetModule<EarningsHistoryModule>(QuoteSummaryModules.EarningsHistory);

    public EarningsTrendModule? EarningsTrend =>
        GetModule<EarningsTrendModule>(QuoteSummaryModules.EarningsTrend);

    public OwnershipModule? InstitutionOwnership =>
        GetModule<OwnershipModule>(QuoteSummaryModules.InstitutionOwnership);

    public OwnershipModule? FundOwnership =>
        GetModule<OwnershipModule>(QuoteSummaryModules.FundOwnership);

    public MajorHoldersBreakdown? MajorHoldersBreakdown =>
        GetModule<MajorHoldersBreakdown>(QuoteSummaryModules.MajorHoldersBreakdown);

    public InsiderHoldersModule? MajorDirectHolders =>
        GetModule<InsiderHoldersModule>(QuoteSummaryModules.MajorDirectHolders);

    public InsiderTransactionsModule? InsiderTransactions =>
        GetModule<InsiderTransactionsModule>(QuoteSummaryModules.InsiderTransactions);

    public InsiderHoldersModule? InsiderHolders =>
        GetModule<InsiderHoldersModule>(QuoteSummaryModules.InsiderHolders);

    public NetSharePurchaseActivity? NetSharePurchaseActivity =>
        GetModule<NetSharePurchaseActivity>(QuoteSummaryModules.NetSharePurchaseActivity);

    public TickerInfo ToTickerInfo() => new()
    {
        Symbol = Symbol,
        AssetProfile = AssetProfile,
        SummaryDetail = SummaryDetail,
        DefaultKeyStatistics = DefaultKeyStatistics,
        FinancialData = FinancialData,
        QuoteType = TryGetModule(QuoteSummaryModules.QuoteType, out var quoteType)
            ? quoteType.Clone()
            : null,
        RawSummary = this
    };
}
