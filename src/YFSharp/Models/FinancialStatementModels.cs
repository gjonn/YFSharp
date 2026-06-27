using System.Text.Json;
using System.Text.Json.Serialization;

namespace YFSharp.Models;

public sealed record IncomeStatementModule
{
    [JsonPropertyName("incomeStatementHistory")]
    public IReadOnlyList<IncomeStatementRow> Statements { get; init; } = [];

    public int? MaxAge { get; init; }

    public JsonElement Raw { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record IncomeStatementRow
{
    public DateTimeOffset? EndDate { get; init; }

    public decimal? TotalRevenue { get; init; }

    public decimal? CostOfRevenue { get; init; }

    public decimal? GrossProfit { get; init; }

    public decimal? ResearchDevelopment { get; init; }

    public decimal? SellingGeneralAdministrative { get; init; }

    public decimal? NonRecurring { get; init; }

    public decimal? OtherOperatingExpenses { get; init; }

    public decimal? TotalOperatingExpenses { get; init; }

    public decimal? OperatingIncome { get; init; }

    public decimal? TotalOtherIncomeExpenseNet { get; init; }

    public decimal? Ebit { get; init; }

    public decimal? InterestExpense { get; init; }

    public decimal? IncomeBeforeTax { get; init; }

    public decimal? IncomeTaxExpense { get; init; }

    public decimal? MinorityInterest { get; init; }

    public decimal? NetIncomeFromContinuingOps { get; init; }

    public decimal? DiscontinuedOperations { get; init; }

    public decimal? ExtraordinaryItems { get; init; }

    public decimal? EffectOfAccountingCharges { get; init; }

    public decimal? OtherItems { get; init; }

    public decimal? NetIncome { get; init; }

    public decimal? NetIncomeApplicableToCommonShares { get; init; }

    public int? MaxAge { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];

    public static IncomeStatementRow? SumTrailingTwelveMonths(IEnumerable<IncomeStatementRow> quarterlyRows)
    {
        var rows = GetLatestFour(quarterlyRows);
        if (rows.Length == 0)
        {
            return null;
        }

        return new IncomeStatementRow
        {
            EndDate = rows.Max(row => row.EndDate),
            TotalRevenue = Sum(rows, row => row.TotalRevenue),
            CostOfRevenue = Sum(rows, row => row.CostOfRevenue),
            GrossProfit = Sum(rows, row => row.GrossProfit),
            ResearchDevelopment = Sum(rows, row => row.ResearchDevelopment),
            SellingGeneralAdministrative = Sum(rows, row => row.SellingGeneralAdministrative),
            NonRecurring = Sum(rows, row => row.NonRecurring),
            OtherOperatingExpenses = Sum(rows, row => row.OtherOperatingExpenses),
            TotalOperatingExpenses = Sum(rows, row => row.TotalOperatingExpenses),
            OperatingIncome = Sum(rows, row => row.OperatingIncome),
            TotalOtherIncomeExpenseNet = Sum(rows, row => row.TotalOtherIncomeExpenseNet),
            Ebit = Sum(rows, row => row.Ebit),
            InterestExpense = Sum(rows, row => row.InterestExpense),
            IncomeBeforeTax = Sum(rows, row => row.IncomeBeforeTax),
            IncomeTaxExpense = Sum(rows, row => row.IncomeTaxExpense),
            MinorityInterest = Sum(rows, row => row.MinorityInterest),
            NetIncomeFromContinuingOps = Sum(rows, row => row.NetIncomeFromContinuingOps),
            DiscontinuedOperations = Sum(rows, row => row.DiscontinuedOperations),
            ExtraordinaryItems = Sum(rows, row => row.ExtraordinaryItems),
            EffectOfAccountingCharges = Sum(rows, row => row.EffectOfAccountingCharges),
            OtherItems = Sum(rows, row => row.OtherItems),
            NetIncome = Sum(rows, row => row.NetIncome),
            NetIncomeApplicableToCommonShares = Sum(rows, row => row.NetIncomeApplicableToCommonShares)
        };
    }

    private static IncomeStatementRow[] GetLatestFour(IEnumerable<IncomeStatementRow> rows) =>
        rows.OrderByDescending(row => row.EndDate ?? DateTimeOffset.MinValue)
            .Take(4)
            .ToArray();

    private static decimal? Sum(IEnumerable<IncomeStatementRow> rows, Func<IncomeStatementRow, decimal?> selector)
    {
        var values = rows.Select(selector).Where(value => value is not null).Select(value => value!.Value).ToArray();
        return values.Length == 0 ? null : values.Sum();
    }
}

public sealed record BalanceSheetModule
{
    public IReadOnlyList<BalanceSheetRow> BalanceSheetStatements { get; init; } = [];

    public int? MaxAge { get; init; }

    public JsonElement Raw { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record BalanceSheetRow
{
    public DateTimeOffset? EndDate { get; init; }

    public decimal? Cash { get; init; }

    public decimal? ShortTermInvestments { get; init; }

    public decimal? NetReceivables { get; init; }

    public decimal? Inventory { get; init; }

    public decimal? OtherCurrentAssets { get; init; }

    public decimal? TotalCurrentAssets { get; init; }

    public decimal? LongTermInvestments { get; init; }

    public decimal? PropertyPlantEquipment { get; init; }

    public decimal? GoodWill { get; init; }

    public decimal? IntangibleAssets { get; init; }

    public decimal? AccumulatedAmortization { get; init; }

    public decimal? OtherAssets { get; init; }

    public decimal? DeferredLongTermAssetCharges { get; init; }

    public decimal? TotalAssets { get; init; }

    public decimal? AccountsPayable { get; init; }

    public decimal? ShortLongTermDebt { get; init; }

    public decimal? ShortTermDebt { get; init; }

    public decimal? OtherCurrentLiab { get; init; }

    public decimal? LongTermDebt { get; init; }

    public decimal? OtherLiab { get; init; }

    public decimal? DeferredLongTermLiab { get; init; }

    public decimal? MinorityInterest { get; init; }

    public decimal? TotalCurrentLiabilities { get; init; }

    public decimal? TotalLiab { get; init; }

    public decimal? CommonStock { get; init; }

    public decimal? RetainedEarnings { get; init; }

    public decimal? TreasuryStock { get; init; }

    public decimal? CapitalSurplus { get; init; }

    public decimal? OtherStockholderEquity { get; init; }

    public decimal? TotalStockholderEquity { get; init; }

    public decimal? NetTangibleAssets { get; init; }

    public int? MaxAge { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record CashFlowStatementModule
{
    public IReadOnlyList<CashFlowStatementRow> CashflowStatements { get; init; } = [];

    public int? MaxAge { get; init; }

    public JsonElement Raw { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];
}

public sealed record CashFlowStatementRow
{
    public DateTimeOffset? EndDate { get; init; }

    public decimal? NetIncome { get; init; }

    public decimal? Depreciation { get; init; }

    public decimal? ChangeToNetincome { get; init; }

    public decimal? ChangeToAccountReceivables { get; init; }

    public decimal? ChangeToInventory { get; init; }

    public decimal? ChangeToLiabilities { get; init; }

    public decimal? ChangeToOperatingActivities { get; init; }

    public decimal? TotalCashFromOperatingActivities { get; init; }

    public decimal? CapitalExpenditures { get; init; }

    public decimal? Investments { get; init; }

    public decimal? OtherCashflowsFromInvestingActivities { get; init; }

    public decimal? TotalCashflowsFromInvestingActivities { get; init; }

    public decimal? DividendsPaid { get; init; }

    public decimal? NetBorrowings { get; init; }

    public decimal? RepurchaseOfStock { get; init; }

    public decimal? OtherCashflowsFromFinancingActivities { get; init; }

    public decimal? TotalCashFromFinancingActivities { get; init; }

    public decimal? ChangeInCash { get; init; }

    public int? MaxAge { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = [];

    public static CashFlowStatementRow? SumTrailingTwelveMonths(IEnumerable<CashFlowStatementRow> quarterlyRows)
    {
        var rows = quarterlyRows.OrderByDescending(row => row.EndDate ?? DateTimeOffset.MinValue)
            .Take(4)
            .ToArray();

        if (rows.Length == 0)
        {
            return null;
        }

        return new CashFlowStatementRow
        {
            EndDate = rows.Max(row => row.EndDate),
            NetIncome = Sum(rows, row => row.NetIncome),
            Depreciation = Sum(rows, row => row.Depreciation),
            ChangeToNetincome = Sum(rows, row => row.ChangeToNetincome),
            ChangeToAccountReceivables = Sum(rows, row => row.ChangeToAccountReceivables),
            ChangeToInventory = Sum(rows, row => row.ChangeToInventory),
            ChangeToLiabilities = Sum(rows, row => row.ChangeToLiabilities),
            ChangeToOperatingActivities = Sum(rows, row => row.ChangeToOperatingActivities),
            TotalCashFromOperatingActivities = Sum(rows, row => row.TotalCashFromOperatingActivities),
            CapitalExpenditures = Sum(rows, row => row.CapitalExpenditures),
            Investments = Sum(rows, row => row.Investments),
            OtherCashflowsFromInvestingActivities = Sum(rows, row => row.OtherCashflowsFromInvestingActivities),
            TotalCashflowsFromInvestingActivities = Sum(rows, row => row.TotalCashflowsFromInvestingActivities),
            DividendsPaid = Sum(rows, row => row.DividendsPaid),
            NetBorrowings = Sum(rows, row => row.NetBorrowings),
            RepurchaseOfStock = Sum(rows, row => row.RepurchaseOfStock),
            OtherCashflowsFromFinancingActivities = Sum(rows, row => row.OtherCashflowsFromFinancingActivities),
            TotalCashFromFinancingActivities = Sum(rows, row => row.TotalCashFromFinancingActivities),
            ChangeInCash = Sum(rows, row => row.ChangeInCash)
        };
    }

    private static decimal? Sum(IEnumerable<CashFlowStatementRow> rows, Func<CashFlowStatementRow, decimal?> selector)
    {
        var values = rows.Select(selector).Where(value => value is not null).Select(value => value!.Value).ToArray();
        return values.Length == 0 ? null : values.Sum();
    }
}
