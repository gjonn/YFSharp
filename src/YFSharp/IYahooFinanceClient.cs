using System.Text.Json;
using YFSharp.Models;

namespace YFSharp;

public interface IYahooFinanceClient
{
    Task<IReadOnlyList<Quote>> GetQuotesAsync(
        IEnumerable<string> symbols,
        CancellationToken cancellationToken = default);

    Task<Quote?> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default);

    Task<QuoteSummary> GetQuoteSummaryAsync(
        string symbol,
        IEnumerable<string> modules,
        CancellationToken cancellationToken = default);

    Task<HistoricalData> GetHistoryAsync(
        string symbol,
        HistoryRequest? request = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, HistoricalData>> DownloadAsync(
        IEnumerable<string> symbols,
        HistoryRequest? request = null,
        int maxConcurrency = 8,
        CancellationToken cancellationToken = default);

    Task<OptionChain> GetOptionsAsync(
        string symbol,
        DateOnly? expiration = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DateOnly>> GetOptionExpirationsAsync(
        string symbol,
        CancellationToken cancellationToken = default);

    Task<SearchResult> SearchAsync(
        string query,
        SearchOptions? options = null,
        CancellationToken cancellationToken = default);

    Task<LookupResult> LookupAsync(
        string query,
        LookupType type = LookupType.All,
        int count = 25,
        CancellationToken cancellationToken = default);

    Task<LookupResult> GetStocksAsync(
        string query,
        int count = 25,
        CancellationToken cancellationToken = default);

    Task<LookupResult> GetEtfsAsync(
        string query,
        int count = 25,
        CancellationToken cancellationToken = default);

    Task<LookupResult> GetMutualFundsAsync(
        string query,
        int count = 25,
        CancellationToken cancellationToken = default);

    Task<LookupResult> GetIndexesAsync(
        string query,
        int count = 25,
        CancellationToken cancellationToken = default);

    Task<LookupResult> GetFuturesAsync(
        string query,
        int count = 25,
        CancellationToken cancellationToken = default);

    Task<LookupResult> GetCurrenciesAsync(
        string query,
        int count = 25,
        CancellationToken cancellationToken = default);

    Task<LookupResult> GetCryptocurrenciesAsync(
        string query,
        int count = 25,
        CancellationToken cancellationToken = default);

    Task<ScreenerResult> ScreenAsync(
        string predefinedScreener,
        int? count = null,
        CancellationToken cancellationToken = default);

    Task<ScreenerResult> ScreenAsync(
        ScreenerRequest request,
        CancellationToken cancellationToken = default);

    Task<CalendarResult<EarningsCalendarRow>> GetEarningsCalendarAsync(
        DateOnly? start = null,
        DateOnly? end = null,
        decimal? marketCap = null,
        bool filterMostActive = true,
        int limit = 12,
        int offset = 0,
        CancellationToken cancellationToken = default);

    Task<CalendarResult<IpoCalendarRow>> GetIpoCalendarAsync(
        DateOnly? start = null,
        DateOnly? end = null,
        int limit = 12,
        int offset = 0,
        CancellationToken cancellationToken = default);

    Task<CalendarResult<EconomicEventCalendarRow>> GetEconomicEventsCalendarAsync(
        DateOnly? start = null,
        DateOnly? end = null,
        int limit = 12,
        int offset = 0,
        CancellationToken cancellationToken = default);

    Task<CalendarResult<SplitCalendarRow>> GetSplitsCalendarAsync(
        DateOnly? start = null,
        DateOnly? end = null,
        int limit = 12,
        int offset = 0,
        CancellationToken cancellationToken = default);

    Task<CalendarResult<JsonElement>> GetCalendarAsync(
        CalendarRequest request,
        CancellationToken cancellationToken = default);

    Task<FundData> GetFundDataAsync(
        string symbol,
        CancellationToken cancellationToken = default);

    Task<SectorData> GetSectorAsync(
        string key,
        string region = "US",
        CancellationToken cancellationToken = default);

    Task<IndustryData> GetIndustryAsync(
        string key,
        string region = "US",
        CancellationToken cancellationToken = default);

    Task<MarketData> GetMarketAsync(
        MarketRegion region,
        CancellationToken cancellationToken = default);
}
