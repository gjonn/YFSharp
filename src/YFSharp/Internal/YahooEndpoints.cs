namespace YFSharp.Internal;

internal static class YahooEndpoints
{
    public const string Chart = "/v8/finance/chart/{0}";
    public const string Quote = "/v7/finance/quote";
    public const string QuoteSummary = "/v10/finance/quoteSummary/{0}";
    public const string Options = "/v7/finance/options/{0}";
    public const string Search = "/v1/finance/search";
    public const string Lookup = "/v1/finance/lookup";
    public const string Screener = "/v1/finance/screener";
    public const string PredefinedScreener = "/v1/finance/screener/predefined/saved";
    public const string Visualization = "/v1/finance/visualization";
    public const string MarketSummary = "/v6/finance/quote/marketSummary";
    public const string MarketTime = "/v6/finance/markettime";
    public const string Sector = "/v1/finance/sectors/{0}";
    public const string Industry = "/v1/finance/industries/{0}";
}
