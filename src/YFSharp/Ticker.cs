using YFSharp.Models;

namespace YFSharp;

public sealed class Ticker
{
    private readonly IYahooFinanceClient _client;

    internal Ticker(IYahooFinanceClient client, string symbol)
    {
        _client = client;
        Symbol = symbol;
    }

    public string Symbol { get; }

    public Task<HistoricalData> HistoryAsync(
        HistoryRequest? request = null,
        CancellationToken cancellationToken = default) =>
        _client.GetHistoryAsync(Symbol, request, cancellationToken);

    public Task<Quote?> QuoteAsync(CancellationToken cancellationToken = default) =>
        _client.GetQuoteAsync(Symbol, cancellationToken);

    public Task<QuoteSummary> InfoAsync(CancellationToken cancellationToken = default) =>
        _client.GetQuoteSummaryAsync(Symbol, QuoteSummaryModules.Info, cancellationToken);

    public async Task<TickerInfo> GetInfoAsync(CancellationToken cancellationToken = default)
    {
        var summary = await _client.GetQuoteSummaryAsync(Symbol, QuoteSummaryModules.Info, cancellationToken)
            .ConfigureAwait(false);

        return summary.ToTickerInfo();
    }

    public async Task<FastInfo?> GetFastInfoAsync(CancellationToken cancellationToken = default)
    {
        var quote = await _client.GetQuoteAsync(Symbol, cancellationToken).ConfigureAwait(false);
        return quote is null ? null : FastInfo.FromQuote(quote);
    }

    public Task<QuoteSummary> QuoteSummaryAsync(
        IEnumerable<string> modules,
        CancellationToken cancellationToken = default) =>
        _client.GetQuoteSummaryAsync(Symbol, modules, cancellationToken);

    public Task<QuoteSummary> FinancialStatementsAsync(CancellationToken cancellationToken = default) =>
        _client.GetQuoteSummaryAsync(Symbol, QuoteSummaryModules.FinancialStatements, cancellationToken);

    public Task<FundData> FundsDataAsync(CancellationToken cancellationToken = default) =>
        _client.GetFundDataAsync(Symbol, cancellationToken);

    public async Task<IReadOnlyList<FundHolding>> FundTopHoldingsAsync(
        CancellationToken cancellationToken = default)
    {
        var data = await FundsDataAsync(cancellationToken).ConfigureAwait(false);
        return data.TopHoldings;
    }

    public async Task<FundOverview?> FundProfileAsync(CancellationToken cancellationToken = default)
    {
        var data = await FundsDataAsync(cancellationToken).ConfigureAwait(false);
        return data.FundOverview;
    }

    public async Task<IReadOnlyList<FundOperationMetric>> FundOperationsAsync(
        CancellationToken cancellationToken = default)
    {
        var data = await FundsDataAsync(cancellationToken).ConfigureAwait(false);
        return data.FundOperations;
    }

    public async Task<FundAssetClasses> FundAssetClassesAsync(
        CancellationToken cancellationToken = default)
    {
        var data = await FundsDataAsync(cancellationToken).ConfigureAwait(false);
        return data.AssetClasses;
    }

    public async Task<IReadOnlyDictionary<string, decimal?>> FundBondRatingsAsync(
        CancellationToken cancellationToken = default)
    {
        var data = await FundsDataAsync(cancellationToken).ConfigureAwait(false);
        return data.BondRatings;
    }

    public async Task<IReadOnlyDictionary<string, decimal?>> FundSectorWeightingsAsync(
        CancellationToken cancellationToken = default)
    {
        var data = await FundsDataAsync(cancellationToken).ConfigureAwait(false);
        return data.SectorWeightings;
    }

    public async Task<IReadOnlyList<IncomeStatementRow>> IncomeStatementAsync(
        CancellationToken cancellationToken = default)
    {
        var module = await GetQuoteSummaryModuleAsync<IncomeStatementModule>(
                QuoteSummaryModules.IncomeStatements,
                QuoteSummaryModules.IncomeStatementHistory,
                cancellationToken)
            .ConfigureAwait(false);

        return module?.Statements ?? [];
    }

    public async Task<IReadOnlyList<IncomeStatementRow>> QuarterlyIncomeStatementAsync(
        CancellationToken cancellationToken = default)
    {
        var module = await GetQuoteSummaryModuleAsync<IncomeStatementModule>(
                QuoteSummaryModules.QuarterlyIncomeStatements,
                QuoteSummaryModules.IncomeStatementHistoryQuarterly,
                cancellationToken)
            .ConfigureAwait(false);

        return module?.Statements ?? [];
    }

    public async Task<IReadOnlyList<BalanceSheetRow>> BalanceSheetAsync(
        CancellationToken cancellationToken = default)
    {
        var module = await GetQuoteSummaryModuleAsync<BalanceSheetModule>(
                QuoteSummaryModules.BalanceSheets,
                QuoteSummaryModules.BalanceSheetHistory,
                cancellationToken)
            .ConfigureAwait(false);

        return module?.BalanceSheetStatements ?? [];
    }

    public async Task<IReadOnlyList<BalanceSheetRow>> QuarterlyBalanceSheetAsync(
        CancellationToken cancellationToken = default)
    {
        var module = await GetQuoteSummaryModuleAsync<BalanceSheetModule>(
                QuoteSummaryModules.QuarterlyBalanceSheets,
                QuoteSummaryModules.BalanceSheetHistoryQuarterly,
                cancellationToken)
            .ConfigureAwait(false);

        return module?.BalanceSheetStatements ?? [];
    }

    public async Task<IReadOnlyList<CashFlowStatementRow>> CashFlowAsync(
        CancellationToken cancellationToken = default)
    {
        var module = await GetQuoteSummaryModuleAsync<CashFlowStatementModule>(
                QuoteSummaryModules.CashFlowStatements,
                QuoteSummaryModules.CashFlowStatementHistory,
                cancellationToken)
            .ConfigureAwait(false);

        return module?.CashflowStatements ?? [];
    }

    public async Task<IReadOnlyList<CashFlowStatementRow>> QuarterlyCashFlowAsync(
        CancellationToken cancellationToken = default)
    {
        var module = await GetQuoteSummaryModuleAsync<CashFlowStatementModule>(
                QuoteSummaryModules.QuarterlyCashFlowStatements,
                QuoteSummaryModules.CashFlowStatementHistoryQuarterly,
                cancellationToken)
            .ConfigureAwait(false);

        return module?.CashflowStatements ?? [];
    }

    public async Task<IncomeStatementRow?> TtmIncomeStatementAsync(
        CancellationToken cancellationToken = default)
    {
        var quarterlyRows = await QuarterlyIncomeStatementAsync(cancellationToken).ConfigureAwait(false);
        return IncomeStatementRow.SumTrailingTwelveMonths(quarterlyRows);
    }

    public async Task<CashFlowStatementRow?> TtmCashFlowAsync(
        CancellationToken cancellationToken = default)
    {
        var quarterlyRows = await QuarterlyCashFlowAsync(cancellationToken).ConfigureAwait(false);
        return CashFlowStatementRow.SumTrailingTwelveMonths(quarterlyRows);
    }

    public Task<QuoteSummary> AnalysisAsync(CancellationToken cancellationToken = default) =>
        _client.GetQuoteSummaryAsync(Symbol, QuoteSummaryModules.Analysis, cancellationToken);

    public async Task<AnalystPriceTargets?> AnalystPriceTargetsAsync(
        CancellationToken cancellationToken = default)
    {
        var financialData = await GetQuoteSummaryModuleAsync<FinancialData>(
                QuoteSummaryModules.AnalystPriceTargets,
                QuoteSummaryModules.FinancialData,
                cancellationToken)
            .ConfigureAwait(false);

        return financialData is null ? null : AnalystPriceTargets.FromFinancialData(financialData);
    }

    public async Task<IReadOnlyList<EarningsEstimateRow>> EarningsEstimateAsync(
        CancellationToken cancellationToken = default)
    {
        var module = await GetEarningsTrendModuleAsync(
                QuoteSummaryModules.EarningsEstimates,
                cancellationToken)
            .ConfigureAwait(false);

        return module?.Trend
            .Where(row => row.EarningsEstimate is not null)
            .Select(row => row.EarningsEstimate! with { Period = row.Period, EndDate = row.EndDate })
            .ToArray() ?? [];
    }

    public async Task<IReadOnlyList<RevenueEstimateRow>> RevenueEstimateAsync(
        CancellationToken cancellationToken = default)
    {
        var module = await GetEarningsTrendModuleAsync(
                QuoteSummaryModules.RevenueEstimates,
                cancellationToken)
            .ConfigureAwait(false);

        return module?.Trend
            .Where(row => row.RevenueEstimate is not null)
            .Select(row => row.RevenueEstimate! with { Period = row.Period, EndDate = row.EndDate })
            .ToArray() ?? [];
    }

    public async Task<IReadOnlyList<EarningsHistoryRow>> EarningsHistoryAsync(
        CancellationToken cancellationToken = default)
    {
        var module = await GetQuoteSummaryModuleAsync<EarningsHistoryModule>(
                QuoteSummaryModules.EarningsHistoryDetails,
                QuoteSummaryModules.EarningsHistory,
                cancellationToken)
            .ConfigureAwait(false);

        return module?.History ?? [];
    }

    public async Task<IReadOnlyList<EpsTrendRow>> EpsTrendAsync(
        CancellationToken cancellationToken = default)
    {
        var module = await GetEarningsTrendModuleAsync(
                QuoteSummaryModules.EpsTrendDetails,
                cancellationToken)
            .ConfigureAwait(false);

        return module?.Trend
            .Where(row => row.EpsTrend is not null)
            .Select(row => row.EpsTrend! with { Period = row.Period, EndDate = row.EndDate })
            .ToArray() ?? [];
    }

    public async Task<IReadOnlyList<EpsRevisionsRow>> EpsRevisionsAsync(
        CancellationToken cancellationToken = default)
    {
        var module = await GetEarningsTrendModuleAsync(
                QuoteSummaryModules.EpsRevisionDetails,
                cancellationToken)
            .ConfigureAwait(false);

        return module?.Trend
            .Where(row => row.EpsRevisions is not null)
            .Select(row => row.EpsRevisions! with { Period = row.Period, EndDate = row.EndDate })
            .ToArray() ?? [];
    }

    public async Task<IReadOnlyList<GrowthEstimateRow>> GrowthEstimatesAsync(
        CancellationToken cancellationToken = default)
    {
        var module = await GetEarningsTrendModuleAsync(
                QuoteSummaryModules.GrowthEstimates,
                cancellationToken)
            .ConfigureAwait(false);

        return module?.Trend
            .Where(row => row.Growth is not null)
            .Select(row => new GrowthEstimateRow
            {
                Period = row.Period,
                EndDate = row.EndDate,
                Growth = row.Growth,
                AdditionalData = row.AdditionalData
            })
            .ToArray() ?? [];
    }

    public Task<QuoteSummary> HoldersAsync(CancellationToken cancellationToken = default) =>
        _client.GetQuoteSummaryAsync(Symbol, QuoteSummaryModules.Holders, cancellationToken);

    public Task<MajorHoldersBreakdown?> MajorHoldersAsync(CancellationToken cancellationToken = default) =>
        GetQuoteSummaryModuleAsync<MajorHoldersBreakdown>(
            QuoteSummaryModules.MajorHolders,
            QuoteSummaryModules.MajorHoldersBreakdown,
            cancellationToken);

    public async Task<IReadOnlyList<OwnershipHolder>> InstitutionalHoldersAsync(
        CancellationToken cancellationToken = default)
    {
        var module = await GetQuoteSummaryModuleAsync<OwnershipModule>(
                QuoteSummaryModules.InstitutionalHolders,
                QuoteSummaryModules.InstitutionOwnership,
                cancellationToken)
            .ConfigureAwait(false);

        return module?.OwnershipList ?? [];
    }

    public async Task<IReadOnlyList<OwnershipHolder>> MutualFundHoldersAsync(
        CancellationToken cancellationToken = default)
    {
        var module = await GetQuoteSummaryModuleAsync<OwnershipModule>(
                QuoteSummaryModules.MutualFundHolders,
                QuoteSummaryModules.FundOwnership,
                cancellationToken)
            .ConfigureAwait(false);

        return module?.OwnershipList ?? [];
    }

    public Task<NetSharePurchaseActivity?> InsiderPurchasesAsync(
        CancellationToken cancellationToken = default) =>
        GetQuoteSummaryModuleAsync<NetSharePurchaseActivity>(
            QuoteSummaryModules.InsiderPurchases,
            QuoteSummaryModules.NetSharePurchaseActivity,
            cancellationToken);

    public async Task<IReadOnlyList<InsiderTransaction>> InsiderTransactionsAsync(
        CancellationToken cancellationToken = default)
    {
        var module = await GetQuoteSummaryModuleAsync<InsiderTransactionsModule>(
                QuoteSummaryModules.InsiderTransactionDetails,
                QuoteSummaryModules.InsiderTransactions,
                cancellationToken)
            .ConfigureAwait(false);

        return module?.Transactions ?? [];
    }

    public async Task<IReadOnlyList<InsiderRosterHolder>> InsiderRosterHoldersAsync(
        CancellationToken cancellationToken = default)
    {
        var module = await GetQuoteSummaryModuleAsync<InsiderHoldersModule>(
                QuoteSummaryModules.InsiderRosterHolders,
                QuoteSummaryModules.InsiderHolders,
                cancellationToken)
            .ConfigureAwait(false);

        return module?.Holders ?? [];
    }

    public async Task<IReadOnlyList<UpgradeDowngrade>> GetRecommendationsAsync(
        CancellationToken cancellationToken = default)
    {
        var summary = await _client.GetQuoteSummaryAsync(
                Symbol,
                QuoteSummaryModules.Recommendations,
                cancellationToken)
            .ConfigureAwait(false);

        return summary.UpgradeDowngradeHistory?.History ?? [];
    }

    public async Task<RecommendationTrend?> GetRecommendationsSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var summary = await _client.GetQuoteSummaryAsync(
                Symbol,
                QuoteSummaryModules.RecommendationsSummary,
                cancellationToken)
            .ConfigureAwait(false);

        return summary.RecommendationTrend;
    }

    public Task<IReadOnlyList<UpgradeDowngrade>> GetUpgradesDowngradesAsync(
        CancellationToken cancellationToken = default) =>
        GetRecommendationsAsync(cancellationToken);

    public async Task<CalendarEvents?> GetCalendarAsync(CancellationToken cancellationToken = default)
    {
        var summary = await _client.GetQuoteSummaryAsync(
                Symbol,
                QuoteSummaryModules.Calendar,
                cancellationToken)
            .ConfigureAwait(false);

        return summary.CalendarEvents;
    }

    public async Task<SecFilings?> GetSecFilingsAsync(CancellationToken cancellationToken = default)
    {
        var summary = await _client.GetQuoteSummaryAsync(
                Symbol,
                QuoteSummaryModules.Filings,
                cancellationToken)
            .ConfigureAwait(false);

        return summary.SecFilings;
    }

    public async Task<EsgScores?> GetSustainabilityAsync(CancellationToken cancellationToken = default)
    {
        var summary = await _client.GetQuoteSummaryAsync(
                Symbol,
                QuoteSummaryModules.Sustainability,
                cancellationToken)
            .ConfigureAwait(false);

        return summary.EsgScores;
    }

    public Task<OptionChain> OptionChainAsync(
        DateOnly? expiration = null,
        CancellationToken cancellationToken = default) =>
        _client.GetOptionsAsync(Symbol, expiration, cancellationToken);

    public Task<IReadOnlyList<DateOnly>> OptionsAsync(CancellationToken cancellationToken = default) =>
        _client.GetOptionExpirationsAsync(Symbol, cancellationToken);

    private async Task<T?> GetQuoteSummaryModuleAsync<T>(
        IEnumerable<string> modules,
        string moduleName,
        CancellationToken cancellationToken)
        where T : class
    {
        var summary = await _client.GetQuoteSummaryAsync(Symbol, modules, cancellationToken)
            .ConfigureAwait(false);

        return summary.GetModule<T>(moduleName);
    }

    private Task<EarningsTrendModule?> GetEarningsTrendModuleAsync(
        IEnumerable<string> modules,
        CancellationToken cancellationToken) =>
        GetQuoteSummaryModuleAsync<EarningsTrendModule>(
            modules,
            QuoteSummaryModules.EarningsTrend,
            cancellationToken);
}
