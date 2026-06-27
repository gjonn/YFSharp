using YFSharp.Models;

namespace YFSharp;

public static class YF
{
    private static readonly Lazy<YahooFinanceClient> SharedClient = new(() => new YahooFinanceClient());

    public static Ticker Ticker(string symbol) => SharedClient.Value.Ticker(symbol);

    public static Sector Sector(string key, string region = "US") => SharedClient.Value.Sector(key, region);

    public static Industry Industry(string key, string region = "US") => SharedClient.Value.Industry(key, region);

    public static Calendars Calendars(DateOnly? start = null, DateOnly? end = null) =>
        SharedClient.Value.Calendars(start, end);

    public static Tickers Tickers(params string[] symbols) => SharedClient.Value.Tickers(symbols);

    public static YahooWebSocketClient WebSocket(string url = YahooWebSocketClient.DefaultUrl) => new(url);

    public static Task<IReadOnlyDictionary<string, HistoricalData>> DownloadAsync(
        IEnumerable<string> symbols,
        HistoryRequest? request = null,
        int maxConcurrency = 8,
        CancellationToken cancellationToken = default) =>
        SharedClient.Value.DownloadAsync(symbols, request, maxConcurrency, cancellationToken);

    public static Task<SearchResult> SearchAsync(
        string query,
        SearchOptions? options = null,
        CancellationToken cancellationToken = default) =>
        SharedClient.Value.SearchAsync(query, options, cancellationToken);

    public static Task<LookupResult> LookupAsync(
        string query,
        LookupType type = LookupType.All,
        int count = 25,
        CancellationToken cancellationToken = default) =>
        SharedClient.Value.LookupAsync(query, type, count, cancellationToken);

    public static Task<LookupResult> GetStocksAsync(
        string query,
        int count = 25,
        CancellationToken cancellationToken = default) =>
        SharedClient.Value.GetStocksAsync(query, count, cancellationToken);

    public static Task<LookupResult> GetEtfsAsync(
        string query,
        int count = 25,
        CancellationToken cancellationToken = default) =>
        SharedClient.Value.GetEtfsAsync(query, count, cancellationToken);

    public static Task<LookupResult> GetMutualFundsAsync(
        string query,
        int count = 25,
        CancellationToken cancellationToken = default) =>
        SharedClient.Value.GetMutualFundsAsync(query, count, cancellationToken);

    public static Task<LookupResult> GetIndexesAsync(
        string query,
        int count = 25,
        CancellationToken cancellationToken = default) =>
        SharedClient.Value.GetIndexesAsync(query, count, cancellationToken);

    public static Task<LookupResult> GetFuturesAsync(
        string query,
        int count = 25,
        CancellationToken cancellationToken = default) =>
        SharedClient.Value.GetFuturesAsync(query, count, cancellationToken);

    public static Task<LookupResult> GetCurrenciesAsync(
        string query,
        int count = 25,
        CancellationToken cancellationToken = default) =>
        SharedClient.Value.GetCurrenciesAsync(query, count, cancellationToken);

    public static Task<LookupResult> GetCryptocurrenciesAsync(
        string query,
        int count = 25,
        CancellationToken cancellationToken = default) =>
        SharedClient.Value.GetCryptocurrenciesAsync(query, count, cancellationToken);

    public static Task<ScreenerResult> ScreenAsync(
        string predefinedScreener,
        int? count = null,
        CancellationToken cancellationToken = default) =>
        SharedClient.Value.ScreenAsync(predefinedScreener, count, cancellationToken);

    public static Task<CalendarResult<EarningsCalendarRow>> GetEarningsCalendarAsync(
        DateOnly? start = null,
        DateOnly? end = null,
        decimal? marketCap = null,
        bool filterMostActive = true,
        int limit = 12,
        int offset = 0,
        CancellationToken cancellationToken = default) =>
        SharedClient.Value.GetEarningsCalendarAsync(
            start,
            end,
            marketCap,
            filterMostActive,
            limit,
            offset,
            cancellationToken);

    public static Task<CalendarResult<IpoCalendarRow>> GetIpoCalendarAsync(
        DateOnly? start = null,
        DateOnly? end = null,
        int limit = 12,
        int offset = 0,
        CancellationToken cancellationToken = default) =>
        SharedClient.Value.GetIpoCalendarAsync(start, end, limit, offset, cancellationToken);

    public static Task<CalendarResult<EconomicEventCalendarRow>> GetEconomicEventsCalendarAsync(
        DateOnly? start = null,
        DateOnly? end = null,
        int limit = 12,
        int offset = 0,
        CancellationToken cancellationToken = default) =>
        SharedClient.Value.GetEconomicEventsCalendarAsync(start, end, limit, offset, cancellationToken);

    public static Task<CalendarResult<SplitCalendarRow>> GetSplitsCalendarAsync(
        DateOnly? start = null,
        DateOnly? end = null,
        int limit = 12,
        int offset = 0,
        CancellationToken cancellationToken = default) =>
        SharedClient.Value.GetSplitsCalendarAsync(start, end, limit, offset, cancellationToken);

    public static Task<MarketData> GetMarketAsync(
        MarketRegion region,
        CancellationToken cancellationToken = default) =>
        SharedClient.Value.GetMarketAsync(region, cancellationToken);
}
