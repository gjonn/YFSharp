using System.Text.Json;

namespace YFSharp.Models;

public enum ScreenerQuoteType
{
    Equity,
    MutualFund,
    Etf
}

public sealed record ScreenerRequest
{
    public ScreenerQuery Query { get; init; } = default!;

    public ScreenerQuoteType QuoteType { get; init; } = ScreenerQuoteType.Equity;

    public int Offset { get; init; }

    public int Count { get; init; } = 25;

    public string SortField { get; init; } = "ticker";

    public bool SortAscending { get; init; }

    public string UserId { get; init; } = string.Empty;

    public string UserIdType { get; init; } = "guid";
}

public sealed record ScreenerResult
{
    public int? Count { get; init; }

    public int? Total { get; init; }

    public IReadOnlyList<JsonElement> Quotes { get; init; } = [];

    public JsonElement Raw { get; init; }
}

public sealed record PredefinedScreenerInfo
{
    public required string Id { get; init; }

    public required ScreenerQuoteType QuoteType { get; init; }

    public required string SortField { get; init; }

    public bool SortAscending { get; init; }

    public int DefaultCount { get; init; } = 25;
}

public sealed record ScreenerFieldValueSet
{
    public IReadOnlySet<string> TextValues { get; init; } =
        new HashSet<string>(StringComparer.Ordinal);

    public IReadOnlySet<decimal> NumericValues { get; init; } =
        new HashSet<decimal>();

    public bool Contains(object? value)
    {
        return value switch
        {
            string text => TextValues.Contains(text),
            _ when TryGetDecimal(value, out var number) => NumericValues.Contains(number),
            _ => false
        };
    }

    private static bool TryGetDecimal(object? value, out decimal number)
    {
        switch (value)
        {
            case byte typed:
                number = typed;
                return true;
            case sbyte typed:
                number = typed;
                return true;
            case short typed:
                number = typed;
                return true;
            case ushort typed:
                number = typed;
                return true;
            case int typed:
                number = typed;
                return true;
            case uint typed:
                number = typed;
                return true;
            case long typed:
                number = typed;
                return true;
            case ulong typed:
                number = typed;
                return true;
            case float typed when !float.IsNaN(typed) && !float.IsInfinity(typed):
                number = (decimal)typed;
                return true;
            case double typed when !double.IsNaN(typed) && !double.IsInfinity(typed):
                number = (decimal)typed;
                return true;
            case decimal typed:
                number = typed;
                return true;
            default:
                number = default;
                return false;
        }
    }
}

public static class ScreenerMetadata
{
    private static readonly string[] Regions =
    [
        "ae", "ar", "at", "au", "be", "br", "ca", "ch", "cl", "cn", "co", "cz",
        "de", "dk", "ee", "eg", "es", "fi", "fr", "gb", "gr", "hk", "hu", "id",
        "ie", "il", "in", "is", "it", "jp", "kr", "kw", "lk", "lt", "lv", "mx",
        "my", "nl", "no", "nz", "pe", "ph", "pk", "pl", "pt", "qa", "ro", "ru",
        "sa", "se", "sg", "sr", "th", "tr", "tw", "us", "ve", "vn", "za"
    ];

    private static readonly string[] Exchanges =
    [
        "AQS", "ASE", "ASX", "ATH", "BER", "BSE", "BTS", "BUE", "BUD", "BVB",
        "BRU", "CAI", "CCS", "CNQ", "CPH", "CSE", "CXA", "CXE", "CXI", "DFM",
        "DOH", "DUS", "DXE", "EBS", "ENX", "EUX", "FKA", "FRA", "GER", "HAM",
        "HAN", "HEL", "HKG", "ICE", "IOB", "ISE", "IST", "JKT", "JNB", "JPX",
        "KAR", "KLS", "KOE", "KSC", "KUW", "LIS", "LIT", "LSE", "MAD", "MCE",
        "MCX", "MDD", "MEX", "MIL", "MUN", "NAE", "NAS", "NCM", "NEO", "NGM",
        "NMS", "NSI", "NYQ", "NZE", "OEM", "OQB", "OQX", "OSA", "OSL", "PAR",
        "PCX", "PHP", "PHS", "PNK", "PRA", "RIS", "SAO", "SAP", "SAU", "SES",
        "SET", "SGO", "SHH", "SHZ", "STO", "STU", "TAI", "TAL", "TLV", "TLO",
        "TOR", "TWO", "VAN", "VIE", "VSE", "WSE", "YHD"
    ];

    private static readonly string[] FundCategories =
    [
        "Allocation--15% to 30% Equity",
        "Allocation--30% to 50% Equity",
        "Allocation--50% to 70% Equity",
        "Allocation--70% to 85% Equity",
        "Allocation--85%+ Equity",
        "Bank Loan",
        "Bear Market",
        "China Region",
        "Commodities Agriculture",
        "Commodities Broad Basket",
        "Convertibles",
        "Corporate Bond",
        "Diversified Emerging Mkts",
        "Diversified Pacific/Asia",
        "Emerging Markets Bond",
        "Emerging-Markets Local-Currency Bond",
        "Energy Limited Partnership",
        "Equity Energy",
        "Equity Precious Metals",
        "Europe Stock",
        "Financial",
        "Foreign Large Blend",
        "Foreign Large Growth",
        "Foreign Large Value",
        "Foreign Small/Mid Blend",
        "Foreign Small/Mid Growth",
        "Foreign Small/Mid Value",
        "Global Real Estate",
        "Health",
        "High Yield Bond",
        "High Yield Muni",
        "Inflation-Protected Bond",
        "Infrastructure",
        "Intermediate Government",
        "Intermediate-Term Bond",
        "Japan Stock",
        "Large Blend",
        "Large Growth",
        "Large Value",
        "Long Government",
        "Long-Short Credit",
        "Long-Short Equity",
        "Long-Term Bond",
        "Managed Futures",
        "Market Neutral",
        "Mid-Cap Blend",
        "Mid-Cap Growth",
        "Mid-Cap Value",
        "Miscellaneous Region",
        "Multialternative",
        "Multicurrency",
        "Multisector Bond",
        "Muni California Intermediate",
        "Muni California Long",
        "Muni Massachusetts",
        "Muni Minnesota",
        "Muni National Interm",
        "Muni National Long",
        "Muni National Short",
        "Muni New Jersey",
        "Muni New York Intermediate",
        "Muni New York Long",
        "Muni Ohio",
        "Muni Pennsylvania",
        "Muni Single State Interm",
        "Muni Single State Long",
        "Muni Single State Short",
        "Natural Resources",
        "Nontraditional Bond",
        "Option Writing",
        "Other",
        "Other Allocation",
        "Pacific/Asia ex-Japan Stk",
        "Preferred Stock",
        "Real Estate",
        "Short Government",
        "Short-Term Bond",
        "Small Blend",
        "Small Growth",
        "Small Value",
        "Tactical Allocation",
        "Target-Date 2000-2010",
        "Target-Date 2015",
        "Target-Date 2020",
        "Target-Date 2025",
        "Target-Date 2030",
        "Target-Date 2035",
        "Target-Date 2040",
        "Target-Date 2045",
        "Target-Date 2050",
        "Target-Date 2055",
        "Target-Date 2060+",
        "Target-Date Retirement",
        "Technology",
        "Trading - Leveraged/Inverse Commodities",
        "Trading - Leveraged/Inverse Equity",
        "Trading--Inverse Equity",
        "Trading--Leveraged Equity",
        "Ultrashort Bond",
        "Utilities",
        "World Allocation",
        "World Bond",
        "World Stock"
    ];

    private static readonly string[] EquitySectors =
    [
        "Basic Materials",
        "Communication Services",
        "Consumer Cyclical",
        "Consumer Defensive",
        "Energy",
        "Financial Services",
        "Healthcare",
        "Industrials",
        "Real Estate",
        "Technology",
        "Utilities"
    ];

    public static IReadOnlyDictionary<string, IReadOnlySet<string>> EquityFields { get; } =
        FieldMap(
            ("eq_fields", ["region", "sector", "peer_group", "industry", "exchange"]),
            ("price",
            [
                "lastclosemarketcap.lasttwelvemonths",
                "percentchange",
                "lastclose52weekhigh.lasttwelvemonths",
                "fiftytwowkpercentchange",
                "lastclose52weeklow.lasttwelvemonths",
                "intradaymarketcap",
                "eodprice",
                "intradaypricechange",
                "intradayprice"
            ]),
            ("trading", ["beta", "avgdailyvol3m", "pctheldinsider", "pctheldinst", "dayvolume", "eodvolume"]),
            ("short_interest",
            [
                "short_percentage_of_shares_outstanding.value",
                "short_interest.value",
                "short_percentage_of_float.value",
                "days_to_cover_short.value",
                "short_interest_percentage_change.value"
            ]),
            ("valuation",
            [
                "bookvalueshare.lasttwelvemonths",
                "lastclosemarketcaptotalrevenue.lasttwelvemonths",
                "lastclosetevtotalrevenue.lasttwelvemonths",
                "pricebookratio.quarterly",
                "peratio.lasttwelvemonths",
                "lastclosepricetangiblebookvalue.lasttwelvemonths",
                "lastclosepriceearnings.lasttwelvemonths",
                "pegratio_5y"
            ]),
            ("profitability",
            [
                "consecutive_years_of_dividend_growth_count",
                "returnonassets.lasttwelvemonths",
                "returnonequity.lasttwelvemonths",
                "forward_dividend_per_share",
                "forward_dividend_yield",
                "returnontotalcapital.lasttwelvemonths"
            ]),
            ("leverage",
            [
                "lastclosetevebit.lasttwelvemonths",
                "netdebtebitda.lasttwelvemonths",
                "totaldebtequity.lasttwelvemonths",
                "ltdebtequity.lasttwelvemonths",
                "ebitinterestexpense.lasttwelvemonths",
                "ebitdainterestexpense.lasttwelvemonths",
                "lastclosetevebitda.lasttwelvemonths",
                "totaldebtebitda.lasttwelvemonths"
            ]),
            ("liquidity",
            [
                "quickratio.lasttwelvemonths",
                "altmanzscoreusingtheaveragestockinformationforaperiod.lasttwelvemonths",
                "currentratio.lasttwelvemonths",
                "operatingcashflowtocurrentliabilities.lasttwelvemonths"
            ]),
            ("income_statement",
            [
                "totalrevenues.lasttwelvemonths",
                "netincomemargin.lasttwelvemonths",
                "grossprofit.lasttwelvemonths",
                "ebitda1yrgrowth.lasttwelvemonths",
                "dilutedepscontinuingoperations.lasttwelvemonths",
                "quarterlyrevenuegrowth.quarterly",
                "epsgrowth.lasttwelvemonths",
                "netincomeis.lasttwelvemonths",
                "ebitda.lasttwelvemonths",
                "dilutedeps1yrgrowth.lasttwelvemonths",
                "totalrevenues1yrgrowth.lasttwelvemonths",
                "operatingincome.lasttwelvemonths",
                "netincome1yrgrowth.lasttwelvemonths",
                "grossprofitmargin.lasttwelvemonths",
                "ebitdamargin.lasttwelvemonths",
                "ebit.lasttwelvemonths",
                "basicepscontinuingoperations.lasttwelvemonths",
                "netepsbasic.lasttwelvemonths",
                "netepsdiluted.lasttwelvemonths"
            ]),
            ("balance_sheet",
            [
                "totalassets.lasttwelvemonths",
                "totalcommonsharesoutstanding.lasttwelvemonths",
                "totaldebt.lasttwelvemonths",
                "totalequity.lasttwelvemonths",
                "totalcurrentassets.lasttwelvemonths",
                "totalcashandshortterminvestments.lasttwelvemonths",
                "totalcommonequity.lasttwelvemonths",
                "totalcurrentliabilities.lasttwelvemonths",
                "totalsharesoutstanding"
            ]),
            ("cash_flow",
            [
                "forward_dividend_yield",
                "leveredfreecashflow.lasttwelvemonths",
                "capitalexpenditure.lasttwelvemonths",
                "cashfromoperations.lasttwelvemonths",
                "leveredfreecashflow1yrgrowth.lasttwelvemonths",
                "unleveredfreecashflow.lasttwelvemonths",
                "cashfromoperations1yrgrowth.lasttwelvemonths"
            ]),
            ("esg", ["esg_score", "environmental_score", "governance_score", "social_score", "highest_controversy"]));

    public static IReadOnlyDictionary<string, IReadOnlySet<string>> FundFields { get; } =
        FieldMap(
            ("eq_fields",
            [
                "categoryname",
                "performanceratingoverall",
                "initialinvestment",
                "annualreturnnavy1categoryrank",
                "riskratingoverall",
                "exchange"
            ]),
            ("price", ["eodprice", "intradaypricechange", "intradayprice"]));

    public static IReadOnlyDictionary<string, IReadOnlySet<string>> EtfFields { get; } =
        FieldMap(
            ("eq_fields",
            [
                "categoryname",
                "fundfamilyname",
                "region",
                "primary_sector",
                "morningstar_economic_moat",
                "morningstar_stewardship",
                "morningstar_uncertainty",
                "morningstar_moat_trend",
                "morningstar_rating_change",
                "exchange"
            ]),
            ("fundamentals", ["fundnetassets", "ticker"]),
            ("feesandexpenses", ["annualreportgrossexpenseratio", "annualreportnetexpenseratio", "turnoverratio"]),
            ("historicalperformance", ["annualreturnnavy1", "annualreturnnavy1categoryrank", "annualreturnnavy3", "annualreturnnavy5"]),
            ("keystats", ["avgdailyvol3m", "dayvolume", "eodvolume", "fiftytwowkpercentchange", "percentchange"]),
            ("morningstar_rating", ["morningstar_last_close_price_to_fair_value", "morningstar_rating", "morningstar_rating_updated_time"]),
            ("portfoliostatistics", ["marketcapitalvaluelong"]),
            ("purchasedetails", ["initialinvestment"]),
            ("trailingperformance",
            [
                "performanceratingoverall",
                "quarterendtrailingreturnytd",
                "riskratingoverall",
                "trailing_3m_return",
                "trailing_ytd_return"
            ]),
            ("price", ["eodprice", "intradaypricechange", "intradayprice"]));

    public static IReadOnlyDictionary<string, ScreenerFieldValueSet> EquityRestrictedValues { get; } =
        ValueMap(
            ("region", Values(Regions)),
            ("exchange", Values(Exchanges)),
            ("sector", Values(EquitySectors)));

    public static IReadOnlyDictionary<string, ScreenerFieldValueSet> FundRestrictedValues { get; } =
        ValueMap(
            ("categoryname", Values(FundCategories)),
            ("exchange", Values("NAS")),
            ("performanceratingoverall", Values([1, 2, 3, 4, 5])),
            ("riskratingoverall", Values([1, 2, 3, 4, 5])));

    public static IReadOnlyDictionary<string, ScreenerFieldValueSet> EtfRestrictedValues { get; } =
        ValueMap(
            ("region", Values(Regions)),
            ("exchange", Values(Exchanges)),
            ("categoryname", Values(FundCategories)),
            ("performanceratingoverall", Values([1, 2, 3, 4, 5])),
            ("riskratingoverall", Values([1, 2, 3, 4, 5])),
            ("morningstar_economic_moat", Values("Wide", "Narrow", "None")),
            ("morningstar_stewardship", Values("Exemplary", "Standard", "Poor")),
            ("morningstar_uncertainty", Values("Low", "Medium", "High", "Very High", "Extreme")),
            ("morningstar_moat_trend", Values("Stable", "Positive", "Negative")),
            ("morningstar_rating_change", Values("Upgrade", "Downgrade")));

    public static IReadOnlyDictionary<string, PredefinedScreenerInfo> PredefinedScreeners { get; } =
        new Dictionary<string, PredefinedScreenerInfo>(StringComparer.Ordinal)
        {
            [YFSharp.Models.PredefinedScreeners.AggressiveSmallCaps] = Predefined(YFSharp.Models.PredefinedScreeners.AggressiveSmallCaps, ScreenerQuoteType.Equity, "eodvolume"),
            [YFSharp.Models.PredefinedScreeners.DayGainers] = Predefined(YFSharp.Models.PredefinedScreeners.DayGainers, ScreenerQuoteType.Equity, "percentchange"),
            [YFSharp.Models.PredefinedScreeners.DayLosers] = Predefined(YFSharp.Models.PredefinedScreeners.DayLosers, ScreenerQuoteType.Equity, "percentchange", sortAscending: true),
            [YFSharp.Models.PredefinedScreeners.GrowthTechnologyStocks] = Predefined(YFSharp.Models.PredefinedScreeners.GrowthTechnologyStocks, ScreenerQuoteType.Equity, "eodvolume"),
            [YFSharp.Models.PredefinedScreeners.MostActives] = Predefined(YFSharp.Models.PredefinedScreeners.MostActives, ScreenerQuoteType.Equity, "dayvolume"),
            [YFSharp.Models.PredefinedScreeners.MostShortedStocks] = Predefined(YFSharp.Models.PredefinedScreeners.MostShortedStocks, ScreenerQuoteType.Equity, "short_percentage_of_shares_outstanding.value"),
            [YFSharp.Models.PredefinedScreeners.SmallCapGainers] = Predefined(YFSharp.Models.PredefinedScreeners.SmallCapGainers, ScreenerQuoteType.Equity, "eodvolume"),
            [YFSharp.Models.PredefinedScreeners.UndervaluedGrowthStocks] = Predefined(YFSharp.Models.PredefinedScreeners.UndervaluedGrowthStocks, ScreenerQuoteType.Equity, "eodvolume"),
            [YFSharp.Models.PredefinedScreeners.UndervaluedLargeCaps] = Predefined(YFSharp.Models.PredefinedScreeners.UndervaluedLargeCaps, ScreenerQuoteType.Equity, "eodvolume"),
            [YFSharp.Models.PredefinedScreeners.ConservativeForeignFunds] = Predefined(YFSharp.Models.PredefinedScreeners.ConservativeForeignFunds, ScreenerQuoteType.MutualFund, "fundnetassets"),
            [YFSharp.Models.PredefinedScreeners.HighYieldBond] = Predefined(YFSharp.Models.PredefinedScreeners.HighYieldBond, ScreenerQuoteType.MutualFund, "fundnetassets"),
            [YFSharp.Models.PredefinedScreeners.PortfolioAnchors] = Predefined(YFSharp.Models.PredefinedScreeners.PortfolioAnchors, ScreenerQuoteType.MutualFund, "fundnetassets"),
            [YFSharp.Models.PredefinedScreeners.SolidLargeGrowthFunds] = Predefined(YFSharp.Models.PredefinedScreeners.SolidLargeGrowthFunds, ScreenerQuoteType.MutualFund, "fundnetassets"),
            [YFSharp.Models.PredefinedScreeners.SolidMidcapGrowthFunds] = Predefined(YFSharp.Models.PredefinedScreeners.SolidMidcapGrowthFunds, ScreenerQuoteType.MutualFund, "fundnetassets"),
            [YFSharp.Models.PredefinedScreeners.TopMutualFunds] = Predefined(YFSharp.Models.PredefinedScreeners.TopMutualFunds, ScreenerQuoteType.MutualFund, "percentchange"),
            [YFSharp.Models.PredefinedScreeners.TopEtfsUs] = Predefined(YFSharp.Models.PredefinedScreeners.TopEtfsUs, ScreenerQuoteType.Etf, "percentchange"),
            [YFSharp.Models.PredefinedScreeners.TopPerformingEtfs] = Predefined(YFSharp.Models.PredefinedScreeners.TopPerformingEtfs, ScreenerQuoteType.Etf, "annualreportnetexpenseratio", sortAscending: true),
            [YFSharp.Models.PredefinedScreeners.TechnologyEtfs] = Predefined(YFSharp.Models.PredefinedScreeners.TechnologyEtfs, ScreenerQuoteType.Etf, "annualreportnetexpenseratio", sortAscending: true),
            [YFSharp.Models.PredefinedScreeners.BondEtfs] = Predefined(YFSharp.Models.PredefinedScreeners.BondEtfs, ScreenerQuoteType.Etf, "annualreportnetexpenseratio", sortAscending: true)
        };

    internal static ScreenerValidationSet ValidationFor(ScreenerQuoteType quoteType) =>
        quoteType switch
        {
            ScreenerQuoteType.Equity => new ScreenerValidationSet(EquityFields, EquityRestrictedValues),
            ScreenerQuoteType.MutualFund => new ScreenerValidationSet(FundFields, FundRestrictedValues),
            ScreenerQuoteType.Etf => new ScreenerValidationSet(EtfFields, EtfRestrictedValues),
            _ => new ScreenerValidationSet(EquityFields, EquityRestrictedValues)
        };

    private static IReadOnlyDictionary<string, IReadOnlySet<string>> FieldMap(
        params (string Category, string[] Fields)[] groups) =>
        groups.ToDictionary(
            group => group.Category,
            group => (IReadOnlySet<string>)new HashSet<string>(group.Fields, StringComparer.Ordinal),
            StringComparer.Ordinal);

    private static IReadOnlyDictionary<string, ScreenerFieldValueSet> ValueMap(
        params (string Field, ScreenerFieldValueSet Values)[] entries) =>
        entries.ToDictionary(entry => entry.Field, entry => entry.Values, StringComparer.Ordinal);

    private static ScreenerFieldValueSet Values(params string[] textValues) =>
        new()
        {
            TextValues = new HashSet<string>(textValues, StringComparer.Ordinal)
        };

    private static ScreenerFieldValueSet Values(params decimal[] numericValues) =>
        new()
        {
            NumericValues = new HashSet<decimal>(numericValues)
        };

    private static PredefinedScreenerInfo Predefined(
        string id,
        ScreenerQuoteType quoteType,
        string sortField,
        bool sortAscending = false) =>
        new()
        {
            Id = id,
            QuoteType = quoteType,
            SortField = sortField,
            SortAscending = sortAscending
        };
}

internal sealed record ScreenerValidationSet(
    IReadOnlyDictionary<string, IReadOnlySet<string>> Fields,
    IReadOnlyDictionary<string, ScreenerFieldValueSet> RestrictedValues)
{
    public bool ContainsField(string field) => Fields.Values.Any(fields => fields.Contains(field));
}

public class ScreenerQuery
{
    private static readonly HashSet<string> ValidOperators =
        new(StringComparer.OrdinalIgnoreCase) { "eq", "is-in", "btwn", "gt", "lt", "gte", "lte", "and", "or" };

    private readonly ScreenerValidationSet? _validationSet;

    public ScreenerQuery(string @operator, IEnumerable<object?> operands)
        : this(@operator, operands, validationSet: null)
    {
    }

    private protected ScreenerQuery(
        string @operator,
        IEnumerable<object?> operands,
        ScreenerValidationSet? validationSet)
    {
        if (!ValidOperators.Contains(@operator))
        {
            throw new ArgumentException($"Unsupported screener operator '{@operator}'.", nameof(@operator));
        }

        _validationSet = validationSet;
        Operator = @operator.Equals("is-in", StringComparison.OrdinalIgnoreCase) ? "IS-IN" : @operator.ToUpperInvariant();
        Operands = operands.ToArray();

        if (Operands.Count == 0)
        {
            throw new ArgumentException("A screener query needs at least one operand.", nameof(operands));
        }

        ValidateOperands();
    }

    public string Operator { get; }

    public IReadOnlyList<object?> Operands { get; }

    public IReadOnlyDictionary<string, IReadOnlySet<string>> ValidFields =>
        _validationSet?.Fields ?? new Dictionary<string, IReadOnlySet<string>>();

    public IReadOnlyDictionary<string, ScreenerFieldValueSet> RestrictedValues =>
        _validationSet?.RestrictedValues ?? new Dictionary<string, ScreenerFieldValueSet>();

    internal virtual ScreenerQuoteType? QueryQuoteType => null;

    public object ToYahooObject()
    {
        var op = Operator;
        var operands = Operands;

        if (Operator == "IS-IN")
        {
            if (Operands.Count < 2)
            {
                throw new InvalidOperationException("IS-IN queries need a field and at least one value.");
            }

            op = "OR";
            var field = Operands[0];
            operands = Operands.Skip(1)
                .Select(value => new ScreenerQuery("EQ", [field, value]))
                .Cast<object?>()
                .ToArray();
        }

        return new
        {
            @operator = op,
            operands = operands.Select(SerializeOperand).ToArray()
        };
    }

    private void ValidateOperands()
    {
        switch (Operator)
        {
            case "AND":
            case "OR":
                ValidateAndOrOperand();
                break;
            case "EQ":
                ValidateEqOperand();
                break;
            case "BTWN":
                ValidateBetweenOperand();
                break;
            case "GT":
            case "LT":
            case "GTE":
            case "LTE":
                ValidateComparisonOperand();
                break;
            case "IS-IN":
                ValidateIsInOperand();
                break;
        }
    }

    private void ValidateAndOrOperand()
    {
        if (Operands.Count <= 1)
        {
            throw new ArgumentException($"{Operator} queries need at least two nested queries.", nameof(Operands));
        }

        if (Operands.Any(operand => operand is not ScreenerQuery))
        {
            throw new ArgumentException($"{Operator} operands must be nested screener queries.", nameof(Operands));
        }

        if (GetType() != typeof(ScreenerQuery)
            && Operands.OfType<ScreenerQuery>().Any(operand => operand.GetType() != GetType()))
        {
            throw new ArgumentException($"{Operator} operands must use the same screener query type.", nameof(Operands));
        }
    }

    private void ValidateEqOperand()
    {
        if (Operands.Count != 2)
        {
            throw new ArgumentException("EQ queries need exactly a field and a value.", nameof(Operands));
        }

        var field = ValidateFieldOperand(Operands[0]);
        ValidateRestrictedValue(field, Operands[1]);
    }

    private void ValidateBetweenOperand()
    {
        if (Operands.Count != 3)
        {
            throw new ArgumentException("BTWN queries need exactly a field, minimum value, and maximum value.", nameof(Operands));
        }

        ValidateFieldOperand(Operands[0]);
        ValidateNumberOperand(Operands[1], "BTWN minimum value must be numeric.");
        ValidateNumberOperand(Operands[2], "BTWN maximum value must be numeric.");
    }

    private void ValidateComparisonOperand()
    {
        if (Operands.Count != 2)
        {
            throw new ArgumentException($"{Operator} queries need exactly a field and a numeric value.", nameof(Operands));
        }

        ValidateFieldOperand(Operands[0]);
        ValidateNumberOperand(Operands[1], $"{Operator} value must be numeric.");
    }

    private void ValidateIsInOperand()
    {
        if (Operands.Count < 2)
        {
            throw new ArgumentException("IS-IN queries need a field and at least one value.", nameof(Operands));
        }

        var field = ValidateFieldOperand(Operands[0]);
        foreach (var value in Operands.Skip(1))
        {
            ValidateRestrictedValue(field, value);
        }
    }

    private string ValidateFieldOperand(object? operand)
    {
        if (operand is not string field || string.IsNullOrWhiteSpace(field))
        {
            throw new ArgumentException($"{Operator} queries need a non-empty field name.", nameof(Operands));
        }

        if (_validationSet is not null && !_validationSet.ContainsField(field))
        {
            throw new ArgumentException($"Invalid screener field '{field}' for {GetType().Name}.", nameof(Operands));
        }

        return field;
    }

    private void ValidateRestrictedValue(string field, object? value)
    {
        if (_validationSet is not null
            && _validationSet.RestrictedValues.TryGetValue(field, out var values)
            && !values.Contains(value))
        {
            throw new ArgumentException($"Invalid value '{value}' for screener field '{field}'.", nameof(Operands));
        }
    }

    private static void ValidateNumberOperand(object? operand, string message)
    {
        if (!IsRealNumber(operand))
        {
            throw new ArgumentException(message, nameof(Operands));
        }
    }

    private static bool IsRealNumber(object? value)
    {
        return value switch
        {
            byte or sbyte or short or ushort or int or uint or long or ulong or decimal => true,
            float typed => !float.IsNaN(typed) && !float.IsInfinity(typed),
            double typed => !double.IsNaN(typed) && !double.IsInfinity(typed),
            _ => false
        };
    }

    private static object? SerializeOperand(object? operand)
    {
        return operand switch
        {
            ScreenerQuery query => query.ToYahooObject(),
            _ => operand
        };
    }
}

public sealed class EquityQuery : ScreenerQuery
{
    public EquityQuery(string @operator, IEnumerable<object?> operands)
        : base(@operator, operands, ScreenerMetadata.ValidationFor(ScreenerQuoteType.Equity))
    {
    }

    internal override ScreenerQuoteType? QueryQuoteType => ScreenerQuoteType.Equity;
}

public sealed class FundQuery : ScreenerQuery
{
    public FundQuery(string @operator, IEnumerable<object?> operands)
        : base(@operator, operands, ScreenerMetadata.ValidationFor(ScreenerQuoteType.MutualFund))
    {
    }

    internal override ScreenerQuoteType? QueryQuoteType => ScreenerQuoteType.MutualFund;
}

public sealed class EtfQuery : ScreenerQuery
{
    public EtfQuery(string @operator, IEnumerable<object?> operands)
        : base(@operator, operands, ScreenerMetadata.ValidationFor(ScreenerQuoteType.Etf))
    {
    }

    internal override ScreenerQuoteType? QueryQuoteType => ScreenerQuoteType.Etf;
}

public static class PredefinedScreeners
{
    public const string AggressiveSmallCaps = "aggressive_small_caps";
    public const string DayGainers = "day_gainers";
    public const string DayLosers = "day_losers";
    public const string GrowthTechnologyStocks = "growth_technology_stocks";
    public const string MostActives = "most_actives";
    public const string MostShortedStocks = "most_shorted_stocks";
    public const string SmallCapGainers = "small_cap_gainers";
    public const string UndervaluedGrowthStocks = "undervalued_growth_stocks";
    public const string UndervaluedLargeCaps = "undervalued_large_caps";
    public const string ConservativeForeignFunds = "conservative_foreign_funds";
    public const string HighYieldBond = "high_yield_bond";
    public const string PortfolioAnchors = "portfolio_anchors";
    public const string SolidLargeGrowthFunds = "solid_large_growth_funds";
    public const string SolidMidcapGrowthFunds = "solid_midcap_growth_funds";
    public const string TopMutualFunds = "top_mutual_funds";
    public const string TopEtfsUs = "top_etfs_us";
    public const string TopPerformingEtfs = "top_performing_etfs";
    public const string TechnologyEtfs = "technology_etfs";
    public const string BondEtfs = "bond_etfs";

    public static IReadOnlyDictionary<string, PredefinedScreenerInfo> Metadata =>
        ScreenerMetadata.PredefinedScreeners;
}
