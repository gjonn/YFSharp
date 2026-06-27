using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using YFSharp.Internal;
using YFSharp.Models;

namespace YFSharp;

public sealed class YahooFinanceClient : IYahooFinanceClient, IDisposable
{
    private static readonly HashSet<string> SupportedHistoryPeriods = new(StringComparer.OrdinalIgnoreCase)
    {
        "1d",
        "5d",
        "1mo",
        "3mo",
        "6mo",
        "1y",
        "2y",
        "5y",
        "10y",
        "ytd",
        "max"
    };

    private static readonly Regex CustomHistoryPeriodPattern =
        new("^[1-9][0-9]*(d|wk|mo|y)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly HashSet<string> SupportedHistoryIntervals = new(StringComparer.OrdinalIgnoreCase)
    {
        "1m",
        "2m",
        "5m",
        "15m",
        "30m",
        "60m",
        "90m",
        "1h",
        "1d",
        "5d",
        "1wk",
        "1mo",
        "3mo"
    };

    private static readonly IReadOnlyDictionary<string, TimeSpan> IntradayRetentionLimits =
        new Dictionary<string, TimeSpan>(StringComparer.OrdinalIgnoreCase)
        {
            ["1m"] = TimeSpan.FromDays(30),
            ["2m"] = TimeSpan.FromDays(60),
            ["5m"] = TimeSpan.FromDays(60),
            ["15m"] = TimeSpan.FromDays(60),
            ["30m"] = TimeSpan.FromDays(60),
            ["60m"] = TimeSpan.FromDays(730),
            ["90m"] = TimeSpan.FromDays(60),
            ["1h"] = TimeSpan.FromDays(730)
        };

    private readonly HttpClient _httpClient;
    private readonly bool _disposeHttpClient;
    private readonly YahooFinanceClientOptions _options;
    private readonly SemaphoreSlim _authLock = new(1, 1);
    private readonly object _cookieLock = new();
    private readonly Dictionary<string, string> _cookies = new(StringComparer.OrdinalIgnoreCase);
    private YahooFinanceCookieStrategy _cookieStrategy = YahooFinanceCookieStrategy.Basic;
    private bool _authStateLoaded;
    private string? _crumb;

    public YahooFinanceClient()
        : this(new YahooFinanceClientOptions())
    {
    }

    public YahooFinanceClient(YahooFinanceClientOptions options)
        : this(CreateHttpClient(options), disposeHttpClient: true, options)
    {
    }

    public YahooFinanceClient(HttpClient httpClient, YahooFinanceClientOptions? options = null)
        : this(httpClient, disposeHttpClient: false, options)
    {
    }

    private YahooFinanceClient(HttpClient httpClient, bool disposeHttpClient, YahooFinanceClientOptions? options)
    {
        _httpClient = httpClient;
        _disposeHttpClient = disposeHttpClient;
        _options = options ?? new YahooFinanceClientOptions();
        ValidateOptions(_options);

        ApplyDefaultRequestHeaders(_httpClient, _options);
    }

    private static HttpClient CreateHttpClient(YahooFinanceClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ValidateOptions(options);

        var handler = new HttpClientHandler();
        if (options.Proxy is not null)
        {
            handler.Proxy = options.Proxy;
            handler.UseProxy = true;
        }

        handler.AutomaticDecompression =
            DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli;

        return new HttpClient(handler, disposeHandler: true)
        {
            Timeout = options.RequestTimeout
        };
    }

    private static void ValidateOptions(YahooFinanceClientOptions options)
    {
        if (options.RequestTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(YahooFinanceClientOptions.RequestTimeout),
                "Request timeout must be greater than zero.");
        }

        if (options.MaxRetries < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(YahooFinanceClientOptions.MaxRetries),
                "Max retries cannot be negative.");
        }

        if (options.AuthStateTtl < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(YahooFinanceClientOptions.AuthStateTtl),
                "Auth state TTL cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(options.UserAgent))
        {
            throw new ArgumentException(
                "User agent cannot be empty.",
                nameof(YahooFinanceClientOptions.UserAgent));
        }

        if (string.IsNullOrWhiteSpace(options.AcceptLanguage))
        {
            throw new ArgumentException(
                "Accept-Language cannot be empty.",
                nameof(YahooFinanceClientOptions.AcceptLanguage));
        }

        ArgumentNullException.ThrowIfNull(options.TimeProvider);
    }

    internal static void ApplyDefaultRequestHeaders(HttpClient httpClient, YahooFinanceClientOptions options)
    {
        if (!httpClient.DefaultRequestHeaders.UserAgent.Any())
        {
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
        }

        if (!httpClient.DefaultRequestHeaders.AcceptLanguage.Any())
        {
            httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd(options.AcceptLanguage);
        }

        if (!httpClient.DefaultRequestHeaders.Accept.Any())
        {
            if (options.RequestProfile == YahooFinanceRequestProfile.Chrome)
            {
                httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/json, text/javascript, */*; q=0.01");
            }
            else
            {
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*", 0.8));
            }
        }
    }

    public async Task<IReadOnlyList<Quote>> GetQuotesAsync(
        IEnumerable<string> symbols,
        CancellationToken cancellationToken = default)
    {
        var normalizedSymbols = symbols.Select(NormalizeSymbol)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalizedSymbols.Length == 0)
        {
            return [];
        }

        var uri = BuildUri(
            _options.Query1BaseUrl,
            YahooEndpoints.Quote,
            new Dictionary<string, string?>
            {
                ["symbols"] = string.Join(',', normalizedSymbols),
                ["formatted"] = "false",
                ["lang"] = "en-US",
                ["region"] = "US"
            });

        using var json = await GetJsonAsync(uri, cancellationToken).ConfigureAwait(false);
        if (!json.RootElement.TryGetProperty("quoteResponse", out var response)
            || !response.TryGetProperty("result", out var result)
            || result.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var quotes = new List<Quote>();
        foreach (var item in result.EnumerateArray())
        {
            var quote = item.Deserialize<Quote>(YahooJson.SerializerOptions);
            if (quote is not null)
            {
                quotes.Add(quote);
            }
        }

        return quotes;
    }

    public async Task<Quote?> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var quotes = await GetQuotesAsync([symbol], cancellationToken).ConfigureAwait(false);
        return quotes.FirstOrDefault();
    }

    public async Task<QuoteSummary> GetQuoteSummaryAsync(
        string symbol,
        IEnumerable<string> modules,
        CancellationToken cancellationToken = default)
    {
        var normalizedSymbol = NormalizeSymbol(symbol);
        var moduleList = modules.Where(m => !string.IsNullOrWhiteSpace(m))
            .Select(m => m.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (moduleList.Length == 0)
        {
            throw new ArgumentException("At least one quoteSummary module is required.", nameof(modules));
        }

        var uri = BuildUri(
            _options.Query2BaseUrl,
            string.Format(CultureInfo.InvariantCulture, YahooEndpoints.QuoteSummary, Uri.EscapeDataString(normalizedSymbol)),
            new Dictionary<string, string?>
            {
                ["modules"] = string.Join(',', moduleList),
                ["corsDomain"] = "finance.yahoo.com",
                ["formatted"] = "false",
                ["symbol"] = normalizedSymbol,
                ["lang"] = "en-US",
                ["region"] = "US"
            });

        using var json = await GetJsonAsync(uri, cancellationToken).ConfigureAwait(false);
        var quoteSummary = json.RootElement.GetProperty("quoteSummary");

        if (quoteSummary.TryGetProperty("error", out var error) && error.ValueKind is not JsonValueKind.Null)
        {
            throw new YahooFinanceException($"Yahoo quoteSummary error for {normalizedSymbol}: {error.GetRawText()}");
        }

        var result = quoteSummary.GetProperty("result");
        if (result.ValueKind != JsonValueKind.Array || result.GetArrayLength() == 0)
        {
            return new QuoteSummary { Symbol = normalizedSymbol };
        }

        var moduleData = result[0].EnumerateObject()
            .ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.Ordinal);

        return new QuoteSummary
        {
            Symbol = normalizedSymbol,
            Modules = moduleData
        };
    }

    public async Task<HistoricalData> GetHistoryAsync(
        string symbol,
        HistoryRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        request = NormalizeHistoryRequest(request ?? new HistoryRequest());
        var normalizedSymbol = NormalizeSymbol(symbol);

        var query = new Dictionary<string, string?>
        {
            ["interval"] = request.Interval,
            ["includePrePost"] = FormatBool(request.IncludePrePost),
            ["events"] = request.IncludeActions ? "div,splits,capitalGains" : null,
            ["formatted"] = "false"
        };

        if (request.Start is not null)
        {
            query["period1"] = request.Start.Value.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
            query["period2"] = (request.End ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        }
        else
        {
            query["range"] = string.IsNullOrWhiteSpace(request.Period) ? "1mo" : request.Period;
        }

        var uri = BuildUri(
            _options.Query2BaseUrl,
            string.Format(CultureInfo.InvariantCulture, YahooEndpoints.Chart, Uri.EscapeDataString(normalizedSymbol)),
            query);

        using var json = await GetJsonAsync(uri, cancellationToken).ConfigureAwait(false);
        return ParseHistoricalData(normalizedSymbol, request, json.RootElement);
    }

    public async Task<IReadOnlyDictionary<string, HistoricalData>> DownloadAsync(
        IEnumerable<string> symbols,
        HistoryRequest? request = null,
        int maxConcurrency = 8,
        CancellationToken cancellationToken = default)
    {
        var normalizedSymbols = symbols.Select(NormalizeSymbol)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (maxConcurrency < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxConcurrency), "Concurrency must be at least 1.");
        }

        var results = new ConcurrentDictionary<string, HistoricalData>(StringComparer.OrdinalIgnoreCase);
        using var gate = new SemaphoreSlim(maxConcurrency);

        var tasks = normalizedSymbols.Select(async ticker =>
        {
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                results[ticker] = await GetHistoryAsync(ticker, request, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                gate.Release();
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);

        return normalizedSymbols
            .Where(results.ContainsKey)
            .ToDictionary(ticker => ticker, ticker => results[ticker], StringComparer.OrdinalIgnoreCase);
    }

    public async Task<OptionChain> GetOptionsAsync(
        string symbol,
        DateOnly? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedSymbol = NormalizeSymbol(symbol);
        var query = new Dictionary<string, string?>();

        if (expiration is not null)
        {
            query["date"] = DateOnlyToUnix(expiration.Value).ToString(CultureInfo.InvariantCulture);
        }

        var uri = BuildUri(
            _options.Query2BaseUrl,
            string.Format(CultureInfo.InvariantCulture, YahooEndpoints.Options, Uri.EscapeDataString(normalizedSymbol)),
            query);

        using var json = await GetJsonAsync(uri, cancellationToken).ConfigureAwait(false);
        return ParseOptionChain(normalizedSymbol, expiration, json.RootElement);
    }

    public async Task<IReadOnlyList<DateOnly>> GetOptionExpirationsAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        var chain = await GetOptionsAsync(symbol, null, cancellationToken).ConfigureAwait(false);
        return chain.ExpirationDates;
    }

    public async Task<SearchResult> SearchAsync(
        string query,
        SearchOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Search query cannot be empty.", nameof(query));
        }

        options ??= new SearchOptions();
        var uri = BuildUri(
            _options.Query2BaseUrl,
            YahooEndpoints.Search,
            new Dictionary<string, string?>
            {
                ["q"] = query,
                ["quotesCount"] = options.QuotesCount.ToString(CultureInfo.InvariantCulture),
                ["enableFuzzyQuery"] = FormatBool(options.EnableFuzzyQuery),
                ["newsCount"] = options.NewsCount.ToString(CultureInfo.InvariantCulture),
                ["quotesQueryId"] = "tss_match_phrase_query",
                ["newsQueryId"] = "news_cie_vespa",
                ["listsCount"] = options.ListsCount.ToString(CultureInfo.InvariantCulture),
                ["enableCb"] = FormatBool(options.IncludeCompanyBreakdown),
                ["enableNavLinks"] = FormatBool(options.IncludeNavLinks),
                ["enableResearchReports"] = FormatBool(options.IncludeResearchReports),
                ["enableCulturalAssets"] = FormatBool(options.IncludeCulturalAssets),
                ["recommendedCount"] = options.RecommendedCount.ToString(CultureInfo.InvariantCulture)
            });

        using var json = await GetJsonAsync(uri, cancellationToken).ConfigureAwait(false);
        var root = json.RootElement;

        return new SearchResult
        {
            Quotes = DeserializeArray<SearchQuote>(root, "quotes", SetSearchQuoteRaw)
                .Where(quote => !string.IsNullOrWhiteSpace(quote.Symbol))
                .ToArray(),
            News = DeserializeArray<SearchNews>(root, "news", SetSearchNewsRaw),
            Lists = CloneArray(root, "lists"),
            ListResults = DeserializeArray<SearchList>(root, "lists", SetSearchListRaw),
            Research = CloneArray(root, "researchReports"),
            ResearchReports = DeserializeArray<SearchResearchReport>(
                root,
                "researchReports",
                SetSearchResearchReportRaw),
            Navigation = CloneArray(root, "nav"),
            NavigationLinks = DeserializeArray<SearchNavigationLink>(root, "nav", SetSearchNavigationLinkRaw),
            RecommendedSymbols = DeserializeFirstArray<SearchQuote>(
                    root,
                    ["recommendedSymbols", "recommended"],
                    SetSearchQuoteRaw)
                .Where(quote => !string.IsNullOrWhiteSpace(quote.Symbol))
                .ToArray(),
            CulturalAssets = CloneArray(root, "culturalAssets"),
            Raw = root.Clone()
        };
    }

    public async Task<LookupResult> LookupAsync(
        string query,
        LookupType type = LookupType.All,
        int count = 25,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Lookup query cannot be empty.", nameof(query));
        }

        var uri = BuildUri(
            _options.Query1BaseUrl,
            YahooEndpoints.Lookup,
            new Dictionary<string, string?>
            {
                ["query"] = query,
                ["type"] = LookupTypeToYahoo(type),
                ["start"] = "0",
                ["count"] = count.ToString(CultureInfo.InvariantCulture),
                ["formatted"] = "false",
                ["fetchPricingData"] = "true",
                ["lang"] = "en-US",
                ["region"] = "US"
            });

        using var json = await GetJsonAsync(uri, cancellationToken).ConfigureAwait(false);
        var documents = new List<LookupDocument>();

        if (json.RootElement.TryGetProperty("finance", out var finance)
            && finance.TryGetProperty("result", out var result)
            && result.ValueKind == JsonValueKind.Array
            && result.GetArrayLength() > 0
            && result[0].TryGetProperty("documents", out var docs)
            && docs.ValueKind == JsonValueKind.Array)
        {
            foreach (var doc in docs.EnumerateArray())
            {
                documents.Add(new LookupDocument
                {
                    Symbol = YahooJson.GetString(doc, "symbol") ?? string.Empty,
                    Name = YahooJson.GetString(doc, "shortName")
                        ?? YahooJson.GetString(doc, "longName")
                        ?? YahooJson.GetString(doc, "name"),
                    QuoteType = YahooJson.GetString(doc, "quoteType"),
                    Exchange = YahooJson.GetString(doc, "exchange"),
                    Raw = doc.Clone()
                });
            }
        }

        return new LookupResult
        {
            Query = query,
            Type = type,
            Documents = documents.Where(doc => !string.IsNullOrWhiteSpace(doc.Symbol)).ToArray(),
            Raw = json.RootElement.Clone()
        };
    }

    public Task<LookupResult> GetStocksAsync(
        string query,
        int count = 25,
        CancellationToken cancellationToken = default) =>
        LookupAsync(query, LookupType.Equity, count, cancellationToken);

    public Task<LookupResult> GetEtfsAsync(
        string query,
        int count = 25,
        CancellationToken cancellationToken = default) =>
        LookupAsync(query, LookupType.Etf, count, cancellationToken);

    public Task<LookupResult> GetMutualFundsAsync(
        string query,
        int count = 25,
        CancellationToken cancellationToken = default) =>
        LookupAsync(query, LookupType.MutualFund, count, cancellationToken);

    public Task<LookupResult> GetIndexesAsync(
        string query,
        int count = 25,
        CancellationToken cancellationToken = default) =>
        LookupAsync(query, LookupType.Index, count, cancellationToken);

    public Task<LookupResult> GetFuturesAsync(
        string query,
        int count = 25,
        CancellationToken cancellationToken = default) =>
        LookupAsync(query, LookupType.Future, count, cancellationToken);

    public Task<LookupResult> GetCurrenciesAsync(
        string query,
        int count = 25,
        CancellationToken cancellationToken = default) =>
        LookupAsync(query, LookupType.Currency, count, cancellationToken);

    public Task<LookupResult> GetCryptocurrenciesAsync(
        string query,
        int count = 25,
        CancellationToken cancellationToken = default) =>
        LookupAsync(query, LookupType.Cryptocurrency, count, cancellationToken);

    public async Task<ScreenerResult> ScreenAsync(
        string predefinedScreener,
        int? count = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(predefinedScreener))
        {
            throw new ArgumentException("Predefined screener name cannot be empty.", nameof(predefinedScreener));
        }

        if (count is > 250)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Yahoo limits screener count to 250.");
        }

        var query = new Dictionary<string, string?>
        {
            ["corsDomain"] = "finance.yahoo.com",
            ["formatted"] = "false",
            ["lang"] = "en-US",
            ["region"] = "US",
            ["scrIds"] = predefinedScreener
        };

        if (count is not null)
        {
            query["count"] = count.Value.ToString(CultureInfo.InvariantCulture);
        }

        var uri = BuildUri(_options.Query1BaseUrl, YahooEndpoints.PredefinedScreener, query);
        using var json = await GetJsonAsync(uri, cancellationToken).ConfigureAwait(false);
        return ParseScreenerResult(json.RootElement);
    }

    public async Task<ScreenerResult> ScreenAsync(
        ScreenerRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Query);

        if (request.Count is < 0 or > 250)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Yahoo limits screener count to 250.");
        }

        if (request.Offset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Screener offset cannot be negative.");
        }

        var quoteType = request.Query.QueryQuoteType ?? request.QuoteType;
        var body = new
        {
            offset = request.Offset,
            count = request.Count,
            sortField = request.SortField,
            sortType = request.SortAscending ? "ASC" : "DESC",
            userId = request.UserId,
            userIdType = request.UserIdType,
            quoteType = ScreenerQuoteTypeToYahoo(quoteType),
            query = request.Query.ToYahooObject()
        };

        var uri = BuildUri(
            _options.Query1BaseUrl,
            YahooEndpoints.Screener,
            new Dictionary<string, string?>
            {
                ["corsDomain"] = "finance.yahoo.com",
                ["formatted"] = "false",
                ["lang"] = "en-US",
                ["region"] = "US"
            });

        using var json = await PostJsonAsync(uri, body, cancellationToken).ConfigureAwait(false);
        return ParseScreenerResult(json.RootElement);
    }

    public async Task<CalendarResult<EarningsCalendarRow>> GetEarningsCalendarAsync(
        DateOnly? start = null,
        DateOnly? end = null,
        decimal? marketCap = null,
        bool filterMostActive = true,
        int limit = 12,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        var (startDate, endDate) = NormalizeCalendarRange(start, end);
        var query = new CalendarQuery("and",
        [
            new CalendarQuery("eq", ["region", "us"]),
            new CalendarQuery("or",
            [
                new CalendarQuery("eq", ["eventtype", "EAD"]),
                new CalendarQuery("eq", ["eventtype", "ERA"])
            ]),
            new CalendarQuery("gte", ["startdatetime", FormatCalendarDate(startDate)]),
            new CalendarQuery("lte", ["startdatetime", FormatCalendarDate(endDate)])
        ]);

        if (marketCap is not null)
        {
            query.Append(new CalendarQuery("gte", ["intradaymarketcap", marketCap.Value]));
        }

        if (filterMostActive && offset == 0)
        {
            var mostActive = await TryBuildMostActiveCalendarQueryAsync(marketCap, cancellationToken)
                .ConfigureAwait(false);
            if (mostActive is not null)
            {
                query.Append(mostActive);
            }
        }

        var request = new CalendarRequest
        {
            Type = CalendarType.Earnings,
            Query = query,
            Limit = limit,
            Offset = offset
        };

        return await GetTypedCalendarAsync(
                request,
                row => new EarningsCalendarRow
                {
                    Symbol = GetCalendarString(row, "Symbol", "ticker") ?? string.Empty,
                    CompanyName = GetCalendarString(row, "Company", "Company Name", "companyshortname"),
                    MarketCap = GetCalendarDecimal(row, "Marketcap", "Market Cap (Intraday)", "intradaymarketcap"),
                    EventName = GetCalendarString(row, "Event Name", "eventname"),
                    EventStartDate = GetCalendarDate(row, "Event Start Date", "startdatetime"),
                    Timing = GetCalendarString(row, "Timing", "startdatetimetype"),
                    EpsEstimate = GetCalendarDecimal(row, "EPS Estimate", "epsestimate"),
                    ReportedEps = GetCalendarDecimal(row, "Reported EPS", "epsactual"),
                    SurprisePercent = GetCalendarDecimal(row, "Surprise(%)", "Surprise (%)", "epssurprisepct"),
                    Raw = row.Raw.Clone()
                },
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<CalendarResult<IpoCalendarRow>> GetIpoCalendarAsync(
        DateOnly? start = null,
        DateOnly? end = null,
        int limit = 12,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        var (startDate, endDate) = NormalizeCalendarRange(start, end);
        var startText = FormatCalendarDate(startDate);
        var endText = FormatCalendarDate(endDate);
        var request = new CalendarRequest
        {
            Type = CalendarType.Ipo,
            Query = new CalendarQuery("or",
            [
                new CalendarQuery("gtelt", ["startdatetime", startText, endText]),
                new CalendarQuery("gtelt", ["filingdate", startText, endText]),
                new CalendarQuery("gtelt", ["amendeddate", startText, endText])
            ]),
            Limit = limit,
            Offset = offset
        };

        return await GetTypedCalendarAsync(
                request,
                row => new IpoCalendarRow
                {
                    Symbol = GetCalendarString(row, "Symbol", "ticker") ?? string.Empty,
                    CompanyName = GetCalendarString(row, "Company", "Company Name", "companyshortname"),
                    Exchange = GetCalendarString(row, "Exchange", "Exchange Short Name", "exchange_short_name"),
                    FilingDate = GetCalendarDate(row, "Filing Date", "filingdate"),
                    Date = GetCalendarDate(row, "Date", "startdatetime"),
                    AmendedDate = GetCalendarDate(row, "Amended Date", "amendeddate"),
                    PriceFrom = GetCalendarDecimal(row, "Price From", "pricefrom"),
                    PriceTo = GetCalendarDecimal(row, "Price To", "priceto"),
                    OfferPrice = GetCalendarDecimal(row, "Price", "offerprice"),
                    Currency = GetCalendarString(row, "Currency", "currencyname"),
                    Shares = GetCalendarDecimal(row, "Shares", "shares"),
                    DealType = GetCalendarString(row, "Deal Type", "dealtype"),
                    Raw = row.Raw.Clone()
                },
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<CalendarResult<EconomicEventCalendarRow>> GetEconomicEventsCalendarAsync(
        DateOnly? start = null,
        DateOnly? end = null,
        int limit = 12,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        var request = new CalendarRequest
        {
            Type = CalendarType.EconomicEvent,
            Query = BuildStartDateTimeCalendarQuery(start, end),
            Limit = limit,
            Offset = offset
        };

        return await GetTypedCalendarAsync(
                request,
                row => new EconomicEventCalendarRow
                {
                    Event = GetCalendarString(row, "Event", "econ_release") ?? string.Empty,
                    Region = GetCalendarString(row, "Region", "Country Code", "country_code"),
                    EventTime = GetCalendarDate(row, "Event Time", "startdatetime"),
                    Period = GetCalendarString(row, "Period", "period"),
                    Actual = GetCalendarDecimal(row, "Actual", "after_release_actual"),
                    Expected = GetCalendarDecimal(row, "Expected", "Market Expectation", "consensus_estimate"),
                    Last = GetCalendarDecimal(row, "Last", "Prior to This", "prior_release_actual"),
                    Revised = GetCalendarDecimal(row, "Revised", "Revised from", "originally_reported_actual"),
                    Raw = row.Raw.Clone()
                },
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<CalendarResult<SplitCalendarRow>> GetSplitsCalendarAsync(
        DateOnly? start = null,
        DateOnly? end = null,
        int limit = 12,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        var request = new CalendarRequest
        {
            Type = CalendarType.Splits,
            Query = BuildStartDateTimeCalendarQuery(start, end),
            Limit = limit,
            Offset = offset
        };

        return await GetTypedCalendarAsync(
                request,
                row => new SplitCalendarRow
                {
                    Symbol = GetCalendarString(row, "Symbol", "ticker") ?? string.Empty,
                    CompanyName = GetCalendarString(row, "Company", "Company Name", "companyshortname"),
                    PayableOn = GetCalendarDate(row, "Payable On", "startdatetime"),
                    Optionable = GetCalendarBool(row, "Optionable", "Optionable?", "optionable"),
                    OldShareWorth = GetCalendarDecimal(row, "Old Share Worth", "old_share_worth"),
                    ShareWorth = GetCalendarDecimal(row, "Share Worth", "share_worth"),
                    Raw = row.Raw.Clone()
                },
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<CalendarResult<JsonElement>> GetCalendarAsync(
        CalendarRequest request,
        CancellationToken cancellationToken = default)
    {
        return await GetTypedCalendarAsync(
                request,
                row => row.Raw.Clone(),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<FundData> GetFundDataAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        var normalizedSymbol = NormalizeSymbol(symbol);
        var summary = await GetQuoteSummaryAsync(
                normalizedSymbol,
                QuoteSummaryModules.Funds,
                cancellationToken)
            .ConfigureAwait(false);

        return ParseFundData(normalizedSymbol, summary);
    }

    public async Task<SectorData> GetSectorAsync(
        string key,
        string region = "US",
        CancellationToken cancellationToken = default)
    {
        var normalizedKey = NormalizeDomainKey(key, nameof(key));
        var normalizedRegion = NormalizeRegion(region);
        var uri = BuildUri(
            _options.Query1BaseUrl,
            string.Format(CultureInfo.InvariantCulture, YahooEndpoints.Sector, Uri.EscapeDataString(normalizedKey)),
            new Dictionary<string, string?>
            {
                ["formatted"] = "true",
                ["withReturns"] = "true",
                ["lang"] = "en-US",
                ["region"] = normalizedRegion
            });

        using var json = await GetJsonAsync(uri, cancellationToken).ConfigureAwait(false);
        return ParseSectorData(normalizedKey, normalizedRegion, json.RootElement);
    }

    public async Task<IndustryData> GetIndustryAsync(
        string key,
        string region = "US",
        CancellationToken cancellationToken = default)
    {
        var normalizedKey = NormalizeDomainKey(key, nameof(key));
        var normalizedRegion = NormalizeRegion(region);
        var uri = BuildUri(
            _options.Query1BaseUrl,
            string.Format(CultureInfo.InvariantCulture, YahooEndpoints.Industry, Uri.EscapeDataString(normalizedKey)),
            new Dictionary<string, string?>
            {
                ["formatted"] = "true",
                ["withReturns"] = "true",
                ["lang"] = "en-US",
                ["region"] = normalizedRegion
            });

        using var json = await GetJsonAsync(uri, cancellationToken).ConfigureAwait(false);
        return ParseIndustryData(normalizedKey, normalizedRegion, json.RootElement);
    }

    public async Task<MarketData> GetMarketAsync(
        MarketRegion region,
        CancellationToken cancellationToken = default)
    {
        var market = MarketRegionToYahoo(region);
        var summaryUri = BuildUri(
            _options.Query1BaseUrl,
            YahooEndpoints.MarketSummary,
            new Dictionary<string, string?>
            {
                ["fields"] = "shortName,regularMarketPrice,regularMarketChange,regularMarketChangePercent",
                ["formatted"] = "false",
                ["lang"] = "en-US",
                ["market"] = market
            });

        var statusUri = BuildUri(
            _options.Query1BaseUrl,
            YahooEndpoints.MarketTime,
            new Dictionary<string, string?>
            {
                ["formatted"] = "true",
                ["key"] = "finance",
                ["lang"] = "en-US",
                ["market"] = market
            });

        using var summaryJson = await GetJsonAsync(summaryUri, cancellationToken).ConfigureAwait(false);
        using var statusJson = await GetJsonAsync(statusUri, cancellationToken).ConfigureAwait(false);

        var summary = new Dictionary<string, MarketSummaryItem>(StringComparer.OrdinalIgnoreCase);
        if (summaryJson.RootElement.TryGetProperty("marketSummaryResponse", out var response)
            && response.TryGetProperty("result", out var result)
            && result.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in result.EnumerateArray())
            {
                var exchange = YahooJson.GetString(item, "exchange");
                if (string.IsNullOrWhiteSpace(exchange))
                {
                    continue;
                }

                summary[exchange] = new MarketSummaryItem
                {
                    Exchange = exchange,
                    ShortName = YahooJson.GetString(item, "shortName"),
                    RegularMarketPrice = TryGetPropertyDecimal(item, "regularMarketPrice"),
                    RegularMarketChange = TryGetPropertyDecimal(item, "regularMarketChange"),
                    RegularMarketChangePercent = TryGetPropertyDecimal(item, "regularMarketChangePercent"),
                    Raw = item.Clone()
                };
            }
        }

        JsonElement? rawStatus = null;
        MarketStatus? status = null;
        if (statusJson.RootElement.TryGetProperty("finance", out var finance)
            && finance.TryGetProperty("marketTimes", out var times)
            && times.ValueKind == JsonValueKind.Array
            && times.GetArrayLength() > 0
            && times[0].TryGetProperty("marketTime", out var marketTime)
            && marketTime.ValueKind == JsonValueKind.Array
            && marketTime.GetArrayLength() > 0)
        {
            rawStatus = marketTime[0].Clone();
            status = ParseMarketStatus(market, marketTime[0]);
        }

        return new MarketData
        {
            Region = region,
            SummaryByExchange = summary,
            Status = status,
            RawStatus = rawStatus
        };
    }

    public Ticker Ticker(string symbol) => new(this, NormalizeSymbol(symbol));

    public Tickers Tickers(IEnumerable<string> symbols) => new(this, symbols.Select(NormalizeSymbol).ToArray());

    public Sector Sector(string key, string region = "US") =>
        new(this, NormalizeDomainKey(key, nameof(key)), NormalizeRegion(region));

    public Industry Industry(string key, string region = "US") =>
        new(this, NormalizeDomainKey(key, nameof(key)), NormalizeRegion(region));

    public Calendars Calendars(DateOnly? start = null, DateOnly? end = null) =>
        new(this, start, end);

    public void Dispose()
    {
        _authLock.Dispose();

        if (_disposeHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    private CalendarQuery BuildStartDateTimeCalendarQuery(DateOnly? start, DateOnly? end)
    {
        var (startDate, endDate) = NormalizeCalendarRange(start, end);
        return new CalendarQuery("and",
        [
            new CalendarQuery("gte", ["startdatetime", FormatCalendarDate(startDate)]),
            new CalendarQuery("lte", ["startdatetime", FormatCalendarDate(endDate)])
        ]);
    }

    private (DateOnly Start, DateOnly End) NormalizeCalendarRange(DateOnly? start, DateOnly? end)
    {
        var normalizedStart = start ?? DateOnly.FromDateTime(_options.TimeProvider.GetUtcNow().DateTime);
        var normalizedEnd = end ?? normalizedStart.AddDays(7);
        if (normalizedEnd < normalizedStart)
        {
            throw new ArgumentOutOfRangeException(nameof(end), "Calendar end date must be on or after start date.");
        }

        return (normalizedStart, normalizedEnd);
    }

    private async Task<CalendarQuery?> TryBuildMostActiveCalendarQueryAsync(
        decimal? marketCap,
        CancellationToken cancellationToken)
    {
        ScreenerResult screen;
        try
        {
            screen = await ScreenAsync(PredefinedScreeners.MostActives, count: 200, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (YahooFinanceHttpException)
        {
            return null;
        }

        var operands = new List<object?>();
        foreach (var quote in screen.Quotes)
        {
            var symbol = YahooJson.GetString(quote, "symbol");
            if (string.IsNullOrWhiteSpace(symbol))
            {
                continue;
            }

            if (marketCap is not null
                && TryGetPropertyDecimal(quote, "marketCap") is { } quoteMarketCap
                && quoteMarketCap < marketCap.Value)
            {
                continue;
            }

            operands.Add(new CalendarQuery("eq", ["ticker", symbol]));
        }

        return operands.Count == 0 ? null : new CalendarQuery("or", operands);
    }

    private HistoryRequest NormalizeHistoryRequest(HistoryRequest request)
    {
        var interval = NormalizeHistoryInterval(request.Interval);

        if (request.End is not null && request.Start is null)
        {
            throw new ArgumentException("History request End requires Start.");
        }

        if (request.Start is not null && request.End is not null && request.End <= request.Start)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "History request End must be after Start.");
        }

        var period = request.Start is null
            ? NormalizeHistoryPeriod(request.Period)
            : null;

        ValidateIntradayRetention(request.Start, request.End, period, interval);

        return request with
        {
            Interval = interval,
            Period = period
        };
    }

    private static string NormalizeHistoryInterval(string? interval)
    {
        if (string.IsNullOrWhiteSpace(interval))
        {
            throw new ArgumentException("History interval cannot be empty.");
        }

        var normalized = interval.Trim().ToLowerInvariant();
        if (!SupportedHistoryIntervals.Contains(normalized))
        {
            throw new ArgumentException(
                $"Unsupported Yahoo history interval '{interval}'. Supported intervals: {string.Join(", ", SupportedHistoryIntervals.Order())}.");
        }

        return normalized;
    }

    private static string NormalizeHistoryPeriod(string? period)
    {
        var normalized = string.IsNullOrWhiteSpace(period)
            ? "1mo"
            : period.Trim().ToLowerInvariant();

        if (!SupportedHistoryPeriods.Contains(normalized) && !CustomHistoryPeriodPattern.IsMatch(normalized))
        {
            throw new ArgumentException(
                $"Unsupported Yahoo history period '{period}'. Supported periods include: {string.Join(", ", SupportedHistoryPeriods.Order())}.");
        }

        return normalized;
    }

    private void ValidateIntradayRetention(
        DateTimeOffset? start,
        DateTimeOffset? end,
        string? period,
        string interval)
    {
        if (!IntradayRetentionLimits.TryGetValue(interval, out var retentionLimit))
        {
            return;
        }

        var now = _options.TimeProvider.GetUtcNow();
        var effectiveEnd = end ?? now;
        var effectiveStart = start ?? GetPeriodStart(period, effectiveEnd);

        if (effectiveStart < now - retentionLimit || effectiveEnd - effectiveStart > retentionLimit)
        {
            throw new ArgumentOutOfRangeException(
                nameof(HistoryRequest),
                $"Yahoo only keeps {interval} history for about {retentionLimit.TotalDays:0} days. Use a coarser interval or a shorter date range.");
        }
    }

    private static DateTimeOffset GetPeriodStart(string? period, DateTimeOffset end)
    {
        if (string.IsNullOrWhiteSpace(period))
        {
            return end.AddMonths(-1);
        }

        if (period.Equals("max", StringComparison.OrdinalIgnoreCase))
        {
            return DateTimeOffset.MinValue;
        }

        if (period.Equals("ytd", StringComparison.OrdinalIgnoreCase))
        {
            return new DateTimeOffset(end.Year, 1, 1, 0, 0, 0, end.Offset);
        }

        var match = Regex.Match(period, "^([1-9][0-9]*)(d|wk|mo|y)$", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return end.AddMonths(-1);
        }

        var amount = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        return match.Groups[2].Value.ToLowerInvariant() switch
        {
            "d" => end.AddDays(-amount),
            "wk" => end.AddDays(-7 * amount),
            "mo" => end.AddMonths(-amount),
            "y" => end.AddYears(-amount),
            _ => end.AddMonths(-1)
        };
    }

    private async Task<JsonDocument> GetJsonAsync(Uri uri, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_options.RequestTimeout);

        var response = await SendWithRateLimitRetriesAsync(
            HttpMethod.Get,
            uri,
            contentFactory: null,
            cts.Token).ConfigureAwait(false);

        if (RequiresCrumbRetry(response.StatusCode))
        {
            response = await RetryWithCrumbAsync(HttpMethod.Get, uri, contentFactory: null, cts.Token).ConfigureAwait(false);
        }

        return ReadJson(response.StatusCode, response.ReasonPhrase, response.Content);
    }

    private async Task<JsonDocument> PostJsonAsync(Uri uri, object body, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_options.RequestTimeout);

        var json = JsonSerializer.Serialize(body, YahooJson.SerializerOptions);
        var response = await SendWithRateLimitRetriesAsync(
            HttpMethod.Post,
            uri,
            () => new StringContent(json, Encoding.UTF8, "application/json"),
            cts.Token).ConfigureAwait(false);

        if (RequiresCrumbRetry(response.StatusCode))
        {
            response = await RetryWithCrumbAsync(
                HttpMethod.Post,
                uri,
                () => new StringContent(json, Encoding.UTF8, "application/json"),
                cts.Token).ConfigureAwait(false);
        }

        return ReadJson(response.StatusCode, response.ReasonPhrase, response.Content);
    }

    private async Task<ResponseContent> RetryWithCrumbAsync(
        HttpMethod method,
        Uri uri,
        Func<HttpContent>? contentFactory,
        CancellationToken cancellationToken)
    {
        var crumb = await GetCrumbAsync(refresh: false, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(crumb))
        {
            var retryResponse = await SendWithRateLimitRetriesAsync(
                method,
                WithQueryParameter(uri, "crumb", crumb),
                contentFactory,
                cancellationToken).ConfigureAwait(false);

            if (!RequiresCrumbRetry(retryResponse.StatusCode))
            {
                return retryResponse;
            }
        }

        crumb = await GetCrumbAsync(refresh: true, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(crumb))
        {
            return new ResponseContent(HttpStatusCode.Unauthorized, "Unauthorized", "Unable to acquire Yahoo crumb.");
        }

        return await SendWithRateLimitRetriesAsync(
            method,
            WithQueryParameter(uri, "crumb", crumb),
            contentFactory,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<ResponseContent> SendWithRateLimitRetriesAsync(
        HttpMethod method,
        Uri uri,
        Func<HttpContent?>? contentFactory,
        CancellationToken cancellationToken)
    {
        await EnsureAuthStateLoadedAsync(cancellationToken).ConfigureAwait(false);

        for (var attempt = 0; ; attempt++)
        {
            var response = await SendForContentAsync(
                method,
                uri,
                contentFactory?.Invoke(),
                cancellationToken).ConfigureAwait(false);

            if (response.StatusCode != HttpStatusCode.TooManyRequests)
            {
                return response;
            }

            if (attempt >= _options.MaxRetries)
            {
                throw new YahooFinanceRateLimitException();
            }

            var retryAttempt = attempt + 1;
            var delay = GetRateLimitDelay(retryAttempt);
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task<ResponseContent> SendForContentAsync(
        HttpMethod method,
        Uri uri,
        HttpContent? content,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, uri)
        {
            Content = content
        };
        ApplyRequestProfileHeaders(request);
        AddCookieHeader(request);

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);

        var cookiesChanged = CaptureCookies(response);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (cookiesChanged)
        {
            await SaveAuthStateAsync(cancellationToken).ConfigureAwait(false);
        }

        return new ResponseContent(response.StatusCode, response.ReasonPhrase, responseContent);
    }

    private void ApplyRequestProfileHeaders(HttpRequestMessage request)
    {
        if (_options.RequestProfile != YahooFinanceRequestProfile.Chrome)
        {
            return;
        }

        AddHeaderIfMissing(request, "Sec-CH-UA", "\"Chromium\";v=\"133\", \"Google Chrome\";v=\"133\", \"Not A(Brand\";v=\"99\"");
        AddHeaderIfMissing(request, "Sec-CH-UA-Mobile", "?0");
        AddHeaderIfMissing(request, "Sec-CH-UA-Platform", "\"Windows\"");
        AddHeaderIfMissing(request, "Sec-Fetch-Site", "same-site");
        AddHeaderIfMissing(request, "Sec-Fetch-Mode", IsDocumentRequest(request.RequestUri) ? "navigate" : "cors");
        AddHeaderIfMissing(request, "Sec-Fetch-Dest", IsDocumentRequest(request.RequestUri) ? "document" : "empty");
        AddHeaderIfMissing(request, "Referer", "https://finance.yahoo.com/");

        if (request.Method != HttpMethod.Get || IsYahooFinanceApiRequest(request.RequestUri))
        {
            AddHeaderIfMissing(request, "Origin", "https://finance.yahoo.com");
        }

        if (IsDocumentRequest(request.RequestUri))
        {
            AddHeaderIfMissing(request, "Upgrade-Insecure-Requests", "1");
        }
    }

    private static bool IsDocumentRequest(Uri? uri)
    {
        if (uri is null)
        {
            return false;
        }

        return uri.Host.Equals("fc.yahoo.com", StringComparison.OrdinalIgnoreCase)
            || uri.Host.Equals("guce.yahoo.com", StringComparison.OrdinalIgnoreCase)
            || uri.Host.Equals("consent.yahoo.com", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsYahooFinanceApiRequest(Uri? uri)
    {
        if (uri is null)
        {
            return false;
        }

        return uri.Host.Equals("query1.finance.yahoo.com", StringComparison.OrdinalIgnoreCase)
            || uri.Host.Equals("query2.finance.yahoo.com", StringComparison.OrdinalIgnoreCase);
    }

    private static void AddHeaderIfMissing(HttpRequestMessage request, string name, string value)
    {
        if (request.Headers.Contains(name))
        {
            return;
        }

        request.Headers.TryAddWithoutValidation(name, value);
    }

    private TimeSpan GetRateLimitDelay(int retryAttempt)
    {
        var delay = _options.RateLimitBackoff?.Invoke(retryAttempt)
            ?? GetJitteredExponentialBackoff(retryAttempt);

        return delay > TimeSpan.Zero ? delay : TimeSpan.Zero;
    }

    private static TimeSpan GetJitteredExponentialBackoff(int retryAttempt)
    {
        var exponent = Math.Min(Math.Max(retryAttempt - 1, 0), 6);
        var baseMilliseconds = Math.Min(30_000d, 500d * Math.Pow(2d, exponent));
        var jitter = 0.5d + Random.Shared.NextDouble();

        return TimeSpan.FromMilliseconds(baseMilliseconds * jitter);
    }

    private async ValueTask EnsureAuthStateLoadedAsync(CancellationToken cancellationToken)
    {
        var authStore = _options.AuthStore;
        if (authStore is null || _authStateLoaded)
        {
            return;
        }

        await _authLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_authStateLoaded)
            {
                return;
            }

            var state = await authStore.LoadAsync(cancellationToken).ConfigureAwait(false);
            if (state is not null && IsAuthStateFresh(state))
            {
                ApplyAuthState(state);
            }
            else if (state is not null)
            {
                await authStore.ClearAsync(cancellationToken).ConfigureAwait(false);
            }

            _authStateLoaded = true;
        }
        finally
        {
            _authLock.Release();
        }
    }

    private bool IsAuthStateFresh(YahooFinanceAuthState state)
    {
        if (_options.AuthStateTtl == TimeSpan.Zero)
        {
            return false;
        }

        var now = _options.TimeProvider.GetUtcNow();
        if (state.Timestamp > now.AddMinutes(5))
        {
            return false;
        }

        return now - state.Timestamp <= _options.AuthStateTtl;
    }

    private void ApplyAuthState(YahooFinanceAuthState state)
    {
        lock (_cookieLock)
        {
            _cookies.Clear();
            foreach (var cookie in state.Cookies)
            {
                if (!string.IsNullOrWhiteSpace(cookie.Key) && !string.IsNullOrWhiteSpace(cookie.Value))
                {
                    _cookies[cookie.Key] = cookie.Value;
                }
            }
        }

        _crumb = string.IsNullOrWhiteSpace(state.Crumb) ? null : state.Crumb;
        _cookieStrategy = Enum.IsDefined(state.Strategy)
            ? state.Strategy
            : YahooFinanceCookieStrategy.Basic;
    }

    private async ValueTask SaveAuthStateAsync(CancellationToken cancellationToken)
    {
        var authStore = _options.AuthStore;
        if (authStore is null)
        {
            return;
        }

        await authStore.SaveAsync(SnapshotAuthState(), cancellationToken).ConfigureAwait(false);
    }

    private YahooFinanceAuthState SnapshotAuthState()
    {
        Dictionary<string, string> cookies;
        lock (_cookieLock)
        {
            cookies = new Dictionary<string, string>(_cookies, StringComparer.OrdinalIgnoreCase);
        }

        return new YahooFinanceAuthState
        {
            Cookies = cookies,
            Crumb = _crumb,
            Strategy = _cookieStrategy,
            Timestamp = _options.TimeProvider.GetUtcNow()
        };
    }

    private async Task<string?> GetCrumbAsync(bool refresh, CancellationToken cancellationToken)
    {
        await EnsureAuthStateLoadedAsync(cancellationToken).ConfigureAwait(false);

        if (!refresh && !string.IsNullOrWhiteSpace(_crumb))
        {
            return _crumb;
        }

        await _authLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!refresh && !string.IsNullOrWhiteSpace(_crumb))
            {
                return _crumb;
            }

            if (refresh)
            {
                _crumb = null;
            }

            var primary = _cookieStrategy;
            var secondary = primary == YahooFinanceCookieStrategy.Basic
                ? YahooFinanceCookieStrategy.Csrf
                : YahooFinanceCookieStrategy.Basic;

            foreach (var strategy in new[] { primary, secondary })
            {
                var crumb = await TryGetCrumbAsync(strategy, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(crumb))
                {
                    _cookieStrategy = strategy;
                    _crumb = crumb;
                    await SaveAuthStateAsync(cancellationToken).ConfigureAwait(false);
                    return crumb;
                }
            }

            return null;
        }
        finally
        {
            _authLock.Release();
        }
    }

    private async Task<string?> TryGetCrumbAsync(
        YahooFinanceCookieStrategy strategy,
        CancellationToken cancellationToken)
    {
        return strategy switch
        {
            YahooFinanceCookieStrategy.Basic => await TryGetBasicCrumbAsync(cancellationToken).ConfigureAwait(false),
            YahooFinanceCookieStrategy.Csrf => await TryGetCsrfCrumbAsync(cancellationToken).ConfigureAwait(false),
            _ => null
        };
    }

    private async Task<string?> TryGetBasicCrumbAsync(CancellationToken cancellationToken)
    {
        try
        {
            await SendWithRateLimitRetriesAsync(
                HttpMethod.Get,
                new Uri("https://fc.yahoo.com"),
                contentFactory: null,
                cancellationToken).ConfigureAwait(false);

            var response = await SendWithRateLimitRetriesAsync(
                HttpMethod.Get,
                new Uri("https://query1.finance.yahoo.com/v1/test/getcrumb"),
                contentFactory: null,
                cancellationToken).ConfigureAwait(false);

            return ValidateCrumbResponse(response);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    private async Task<string?> TryGetCsrfCrumbAsync(CancellationToken cancellationToken)
    {
        try
        {
            var consent = await SendWithRateLimitRetriesAsync(
                HttpMethod.Get,
                new Uri("https://guce.yahoo.com/consent"),
                contentFactory: null,
                cancellationToken).ConfigureAwait(false);

            var csrfToken = ExtractInputValue(consent.Content, "csrfToken");
            var sessionId = ExtractInputValue(consent.Content, "sessionId");
            if (string.IsNullOrWhiteSpace(csrfToken) || string.IsNullOrWhiteSpace(sessionId))
            {
                var crumbOnlyResponse = await SendWithRateLimitRetriesAsync(
                    HttpMethod.Get,
                    new Uri("https://query2.finance.yahoo.com/v1/test/getcrumb"),
                    contentFactory: null,
                    cancellationToken).ConfigureAwait(false);

                return ValidateCrumbResponse(crumbOnlyResponse);
            }

            await SendWithRateLimitRetriesAsync(
                HttpMethod.Post,
                new Uri($"https://consent.yahoo.com/v2/collectConsent?sessionId={Uri.EscapeDataString(sessionId)}"),
                () => new FormUrlEncodedContent(
                [
                    new KeyValuePair<string, string>("agree", "agree"),
                    new KeyValuePair<string, string>("agree", "agree"),
                    new KeyValuePair<string, string>("consentUUID", "default"),
                    new KeyValuePair<string, string>("sessionId", sessionId),
                    new KeyValuePair<string, string>("csrfToken", csrfToken),
                    new KeyValuePair<string, string>("originalDoneUrl", "https://finance.yahoo.com/"),
                    new KeyValuePair<string, string>("namespace", "yahoo")
                ]),
                cancellationToken).ConfigureAwait(false);

            await SendWithRateLimitRetriesAsync(
                HttpMethod.Get,
                new Uri($"https://guce.yahoo.com/copyConsent?sessionId={Uri.EscapeDataString(sessionId)}"),
                contentFactory: null,
                cancellationToken).ConfigureAwait(false);

            var response = await SendWithRateLimitRetriesAsync(
                HttpMethod.Get,
                new Uri("https://query2.finance.yahoo.com/v1/test/getcrumb"),
                contentFactory: null,
                cancellationToken).ConfigureAwait(false);

            return ValidateCrumbResponse(response);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    private static string? ValidateCrumbResponse(ResponseContent response)
    {
        if (response.StatusCode == HttpStatusCode.TooManyRequests
            || response.Content.Contains("Too Many Requests", StringComparison.OrdinalIgnoreCase))
        {
            throw new YahooFinanceRateLimitException();
        }

        var crumb = response.Content.Trim();
        if (!response.IsSuccessStatusCode
            || string.IsNullOrWhiteSpace(crumb)
            || crumb.Contains("<html", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return crumb;
    }

    private static JsonDocument ReadJson(HttpStatusCode statusCode, string? reasonPhrase, string content)
    {
        if (statusCode == HttpStatusCode.TooManyRequests
            || content.Contains("Too Many Requests", StringComparison.OrdinalIgnoreCase))
        {
            throw new YahooFinanceRateLimitException();
        }

        if (content.Contains("Will be right back", StringComparison.OrdinalIgnoreCase))
        {
            throw new YahooFinanceException("Yahoo Finance is currently returning a maintenance page.");
        }

        if ((int)statusCode is < 200 or > 299)
        {
            throw new YahooFinanceHttpException(statusCode, reasonPhrase, Truncate(content, 512));
        }

        try
        {
            return JsonDocument.Parse(content);
        }
        catch (JsonException ex)
        {
            throw new YahooFinanceException($"Yahoo Finance returned invalid JSON: {Truncate(content, 512)}", ex);
        }
    }

    private void AddCookieHeader(HttpRequestMessage request)
    {
        string cookieHeader;
        lock (_cookieLock)
        {
            if (_cookies.Count == 0)
            {
                return;
            }

            cookieHeader = string.Join("; ", _cookies.Select(cookie => $"{cookie.Key}={cookie.Value}"));
        }

        if (cookieHeader.Length == 0)
        {
            return;
        }

        request.Headers.Remove("Cookie");
        request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
    }

    private bool CaptureCookies(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            return false;
        }

        var changed = false;
        foreach (var setCookie in values)
        {
            var firstSegment = setCookie.Split(';', 2)[0];
            var separator = firstSegment.IndexOf('=', StringComparison.Ordinal);
            if (separator <= 0)
            {
                continue;
            }

            var name = firstSegment[..separator].Trim();
            var value = firstSegment[(separator + 1)..].Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            lock (_cookieLock)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    changed |= _cookies.Remove(name);
                }
                else
                {
                    if (!_cookies.TryGetValue(name, out var currentValue)
                        || !string.Equals(currentValue, value, StringComparison.Ordinal))
                    {
                        _cookies[name] = value;
                        changed = true;
                    }
                }
            }
        }

        return changed;
    }

    private static string? ExtractInputValue(string html, string name)
    {
        foreach (Match inputMatch in Regex.Matches(html, "<input\\b[^>]*>", RegexOptions.IgnoreCase))
        {
            var input = inputMatch.Value;
            if (!Regex.IsMatch(
                    input,
                    $"\\bname\\s*=\\s*['\"]{Regex.Escape(name)}['\"]",
                    RegexOptions.IgnoreCase))
            {
                continue;
            }

            var valueMatch = Regex.Match(
                input,
                "\\bvalue\\s*=\\s*['\"](?<value>[^'\"]*)['\"]",
                RegexOptions.IgnoreCase);

            return valueMatch.Success ? WebUtility.HtmlDecode(valueMatch.Groups["value"].Value) : null;
        }

        return null;
    }

    private static HistoricalData ParseHistoricalData(
        string symbol,
        HistoryRequest request,
        JsonElement root)
    {
        var chart = root.GetProperty("chart");
        if (chart.TryGetProperty("error", out var error) && error.ValueKind is not JsonValueKind.Null)
        {
            throw new YahooFinanceException($"Yahoo chart error for {symbol}: {error.GetRawText()}");
        }

        var result = chart.GetProperty("result");
        if (result.ValueKind != JsonValueKind.Array || result.GetArrayLength() == 0)
        {
            return new HistoricalData { Symbol = symbol };
        }

        var payload = result[0];
        var meta = payload.GetProperty("meta");
        var metadata = meta.EnumerateObject()
            .ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.Ordinal);

        var timestamps = payload.TryGetProperty("timestamp", out var timestampElement)
            && timestampElement.ValueKind == JsonValueKind.Array
            ? timestampElement
            : default;

        var quote = payload.GetProperty("indicators").GetProperty("quote")[0];
        var open = GetPropertyOrDefault(quote, "open");
        var high = GetPropertyOrDefault(quote, "high");
        var low = GetPropertyOrDefault(quote, "low");
        var close = GetPropertyOrDefault(quote, "close");
        var volume = GetPropertyOrDefault(quote, "volume");

        JsonElement adjustedClose = default;
        if (payload.GetProperty("indicators").TryGetProperty("adjclose", out var adjustedArray)
            && adjustedArray.ValueKind == JsonValueKind.Array
            && adjustedArray.GetArrayLength() > 0)
        {
            adjustedClose = GetPropertyOrDefault(adjustedArray[0], "adjclose");
        }

        var exchangeTimezoneName = YahooJson.GetString(meta, "exchangeTimezoneName");
        var priceHint = GetPriceHint(meta);
        var shouldRound = request.Round || request.Rounding;
        var dividends = ParseEventAmounts(payload, "dividends", "amount");
        var capitalGains = ParseEventAmounts(payload, "capitalGains", "amount");
        var splits = ParseSplitEvents(payload);
        var rawBars = new List<PriceBar>();

        for (var i = 0; i < timestamps.GetArrayLength(); i++)
        {
            var timestamp = YahooJson.GetArrayInt64(timestamps, i);
            if (timestamp is null)
            {
                continue;
            }

            var rawOpen = YahooJson.GetArrayDecimal(open, i);
            var rawHigh = YahooJson.GetArrayDecimal(high, i);
            var rawLow = YahooJson.GetArrayDecimal(low, i);
            var rawClose = YahooJson.GetArrayDecimal(close, i);
            var rawAdjustedClose = YahooJson.GetArrayDecimal(adjustedClose, i);
            var rawVolume = YahooJson.GetArrayInt64(volume, i);

            if (!request.KeepNullRows
                && rawOpen is null
                && rawHigh is null
                && rawLow is null
                && rawClose is null
                && rawAdjustedClose is null
                && rawVolume is null)
            {
                continue;
            }

            rawBars.Add(new PriceBar
            {
                Time = ConvertChartTimestamp(timestamp.Value, exchangeTimezoneName, request.IgnoreTimezone),
                Open = rawOpen,
                High = rawHigh,
                Low = rawLow,
                Close = rawClose,
                AdjustedClose = rawAdjustedClose,
                Volume = rawVolume,
                Dividend = request.IncludeActions && dividends.TryGetValue(timestamp.Value, out var dividend)
                    ? dividend
                    : null,
                StockSplit = request.IncludeActions && splits.TryGetValue(timestamp.Value, out var split)
                    ? split
                    : null,
                CapitalGain = request.IncludeActions && capitalGains.TryGetValue(timestamp.Value, out var capitalGain)
                    ? capitalGain
                    : null
            });
        }

        var repairedBars = request.Repair
            ? PriceHistoryRepair.Repair(rawBars)
            : rawBars;

        var bars = repairedBars.Select(bar =>
        {
            var adjusted = ApplyAdjustment(
                request.AutoAdjust,
                request.BackAdjust,
                bar.Open,
                bar.High,
                bar.Low,
                bar.Close,
                bar.AdjustedClose);

            return bar with
            {
                Open = MaybeRound(adjusted.Open, shouldRound, priceHint),
                High = MaybeRound(adjusted.High, shouldRound, priceHint),
                Low = MaybeRound(adjusted.Low, shouldRound, priceHint),
                Close = MaybeRound(adjusted.Close, shouldRound, priceHint),
                AdjustedClose = MaybeRound(bar.AdjustedClose, shouldRound, priceHint),
                Dividend = MaybeRound(bar.Dividend, shouldRound, priceHint),
                CapitalGain = MaybeRound(bar.CapitalGain, shouldRound, priceHint)
            };
        }).ToArray();

        return new HistoricalData
        {
            Symbol = symbol,
            Currency = YahooJson.GetString(meta, "currency"),
            ExchangeName = YahooJson.GetString(meta, "exchangeName"),
            ExchangeTimezoneName = exchangeTimezoneName,
            RegularMarketPrice = TryGetPropertyDecimal(meta, "regularMarketPrice"),
            Bars = bars,
            Metadata = metadata
        };
    }

    private static OptionChain ParseOptionChain(string symbol, DateOnly? requestedExpiration, JsonElement root)
    {
        if (!root.TryGetProperty("optionChain", out var chain)
            || !chain.TryGetProperty("result", out var result)
            || result.ValueKind != JsonValueKind.Array
            || result.GetArrayLength() == 0)
        {
            return new OptionChain { Symbol = symbol, Expiration = requestedExpiration };
        }

        var payload = result[0];
        var expirationDates = new List<DateOnly>();
        if (payload.TryGetProperty("expirationDates", out var expirationElement)
            && expirationElement.ValueKind == JsonValueKind.Array)
        {
            expirationDates.AddRange(expirationElement.EnumerateArray()
                .Select(YahooJson.GetInt64)
                .Where(value => value is not null)
                .Select(value => DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(value!.Value).UtcDateTime)));
        }

        Quote? underlying = null;
        if (payload.TryGetProperty("quote", out var quoteElement))
        {
            underlying = quoteElement.Deserialize<Quote>(YahooJson.SerializerOptions);
        }

        if (!payload.TryGetProperty("options", out var options)
            || options.ValueKind != JsonValueKind.Array
            || options.GetArrayLength() == 0)
        {
            return new OptionChain
            {
                Symbol = symbol,
                Underlying = underlying,
                Expiration = requestedExpiration,
                ExpirationDates = expirationDates
            };
        }

        var firstOption = options[0];
        return new OptionChain
        {
            Symbol = symbol,
            Underlying = underlying,
            Expiration = requestedExpiration ?? expirationDates.FirstOrDefault(),
            ExpirationDates = expirationDates,
            Calls = firstOption.TryGetProperty("calls", out var calls) ? ParseContracts(calls) : [],
            Puts = firstOption.TryGetProperty("puts", out var puts) ? ParseContracts(puts) : []
        };
    }

    private static ScreenerResult ParseScreenerResult(JsonElement root)
    {
        JsonElement result = default;
        if (root.TryGetProperty("finance", out var finance)
            && finance.TryGetProperty("result", out var results)
            && results.ValueKind == JsonValueKind.Array
            && results.GetArrayLength() > 0)
        {
            result = results[0];
        }
        else
        {
            result = root;
        }

        return new ScreenerResult
        {
            Count = result.TryGetProperty("count", out var count) ? (int?)YahooJson.GetInt64(count) : null,
            Total = result.TryGetProperty("total", out var total) ? (int?)YahooJson.GetInt64(total) : null,
            Quotes = CloneArray(result, "quotes"),
            Raw = result.Clone()
        };
    }

    private async Task<CalendarResult<T>> GetTypedCalendarAsync<T>(
        CalendarRequest request,
        Func<CalendarParsedRow, T> rowFactory,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Query);

        if (request.Limit < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Calendar limit cannot be negative.");
        }

        if (request.Offset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Calendar offset cannot be negative.");
        }

        var body = new
        {
            sortType = request.SortAscending ? "ASC" : "DESC",
            entityIdType = CalendarTypeToYahoo(request.Type),
            sortField = string.IsNullOrWhiteSpace(request.SortField)
                ? DefaultCalendarSortField(request.Type)
                : request.SortField,
            includeFields = request.IncludeFields is { Count: > 0 }
                ? request.IncludeFields
                : DefaultCalendarIncludeFields(request.Type),
            size = Math.Min(request.Limit, 100),
            offset = request.Offset,
            query = request.Query.ToYahooObject()
        };

        var uri = BuildUri(
            _options.Query1BaseUrl,
            YahooEndpoints.Visualization,
            new Dictionary<string, string?>
            {
                ["lang"] = "en-US",
                ["region"] = "US"
            });

        using var json = await PostJsonAsync(uri, body, cancellationToken).ConfigureAwait(false);
        var payload = ParseCalendarPayload(request.Type, json.RootElement);
        return new CalendarResult<T>
        {
            Type = request.Type,
            Rows = payload.Rows.Select(rowFactory).ToArray(),
            Columns = payload.Columns,
            Raw = payload.Raw.Clone()
        };
    }

    private static CalendarPayload ParseCalendarPayload(CalendarType type, JsonElement root)
    {
        if (!root.TryGetProperty("finance", out var finance))
        {
            return new CalendarPayload(type, [], [], root.Clone());
        }

        if (finance.TryGetProperty("error", out var error)
            && error.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined)
        {
            throw new YahooFinanceException($"Yahoo calendar error: {error.GetRawText()}");
        }

        if (!finance.TryGetProperty("result", out var results)
            || results.ValueKind != JsonValueKind.Array
            || results.GetArrayLength() == 0)
        {
            return new CalendarPayload(type, [], [], finance.Clone());
        }

        var result = results[0];
        if (!result.TryGetProperty("documents", out var documents)
            || documents.ValueKind != JsonValueKind.Array
            || documents.GetArrayLength() == 0)
        {
            return new CalendarPayload(type, [], [], result.Clone());
        }

        var document = documents[0];
        var columns = ParseCalendarColumns(document);
        var rows = ParseCalendarRows(document, columns);
        return new CalendarPayload(type, columns, rows, document.Clone());
    }

    private static IReadOnlyList<CalendarColumn> ParseCalendarColumns(JsonElement document)
    {
        if (!document.TryGetProperty("columns", out var columns) || columns.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var parsed = new List<CalendarColumn>();
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var column in columns.EnumerateArray())
        {
            var label = YahooJson.GetString(column, "label");
            var field = YahooJson.GetString(column, "field")
                ?? YahooJson.GetString(column, "name")
                ?? YahooJson.GetString(column, "key");
            var key = label ?? field ?? $"Column{parsed.Count}";
            if (counts.TryGetValue(key, out var count))
            {
                counts[key] = count + 1;
                key = key.Equals("Event Start Date", StringComparison.OrdinalIgnoreCase)
                    ? "Timing"
                    : $"{key}#{count + 1}";
            }
            else
            {
                counts[key] = 1;
            }

            parsed.Add(new CalendarColumn
            {
                Key = key,
                Label = label,
                Field = field,
                Type = YahooJson.GetString(column, "type"),
                Raw = column.Clone()
            });
        }

        return parsed;
    }

    private static IReadOnlyList<CalendarParsedRow> ParseCalendarRows(
        JsonElement document,
        IReadOnlyList<CalendarColumn> columns)
    {
        if (!document.TryGetProperty("rows", out var rows) || rows.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var parsed = new List<CalendarParsedRow>();
        foreach (var row in rows.EnumerateArray())
        {
            if (row.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            var values = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < columns.Count && index < row.GetArrayLength(); index++)
            {
                var value = row[index].Clone();
                var column = columns[index];
                values[column.Key] = value;

                if (!string.IsNullOrWhiteSpace(column.Label))
                {
                    values.TryAdd(column.Label, value);
                }

                if (!string.IsNullOrWhiteSpace(column.Field))
                {
                    values.TryAdd(column.Field, value);
                }
            }

            parsed.Add(new CalendarParsedRow(values, row.Clone()));
        }

        return parsed;
    }

    private static FundData ParseFundData(string symbol, QuoteSummary summary)
    {
        summary.TryGetModule(QuoteSummaryModules.QuoteType, out var quoteTypeModule);
        var profile = summary.FundProfile;
        var holdings = summary.TopHoldings;

        return new FundData
        {
            Symbol = symbol,
            QuoteType = quoteTypeModule.ValueKind == JsonValueKind.Object
                ? YahooJson.GetString(quoteTypeModule, "quoteType")
                : null,
            Description = summary.SummaryProfile?.LongBusinessSummary,
            FundOverview = profile is null
                ? null
                : new FundOverview
                {
                    CategoryName = profile.CategoryName,
                    Family = profile.Family,
                    LegalType = profile.LegalType
                },
            FundOperations = ParseFundOperations(profile),
            AssetClasses = holdings is null
                ? new FundAssetClasses()
                : new FundAssetClasses
                {
                    CashPosition = holdings.CashPosition,
                    StockPosition = holdings.StockPosition,
                    BondPosition = holdings.BondPosition,
                    PreferredPosition = holdings.PreferredPosition,
                    ConvertiblePosition = holdings.ConvertiblePosition,
                    OtherPosition = holdings.OtherPosition
                },
            TopHoldings = ParseFundHoldings(holdings),
            EquityHoldings = ParseEquityHoldings(holdings?.EquityHoldings),
            BondHoldings = ParseBondHoldings(holdings?.BondHoldings),
            BondRatings = FlattenDecimalMaps(holdings?.BondRatings),
            SectorWeightings = FlattenDecimalMaps(holdings?.SectorWeightings),
            RawSummary = summary
        };
    }

    private static IReadOnlyList<FundOperationMetric> ParseFundOperations(FundProfile? profile)
    {
        var fund = profile?.FeesExpensesInvestment;
        var category = profile?.FeesExpensesInvestmentCat;

        return
        [
            new FundOperationMetric
            {
                Attribute = "Annual Report Expense Ratio",
                Fund = fund?.AnnualReportExpenseRatio,
                CategoryAverage = category?.AnnualReportExpenseRatio
            },
            new FundOperationMetric
            {
                Attribute = "Annual Holdings Turnover",
                Fund = fund?.AnnualHoldingsTurnover,
                CategoryAverage = category?.AnnualHoldingsTurnover
            },
            new FundOperationMetric
            {
                Attribute = "Total Net Assets",
                Fund = fund?.TotalNetAssets,
                CategoryAverage = category?.TotalNetAssets
            }
        ];
    }

    private static IReadOnlyList<FundHolding> ParseFundHoldings(TopHoldings? holdings)
    {
        return holdings?.Holdings
            .Where(holding => !string.IsNullOrWhiteSpace(holding.Symbol))
            .Select(holding => new FundHolding
            {
                Symbol = holding.Symbol!,
                Name = holding.HoldingName,
                HoldingPercent = holding.HoldingPercent,
                Raw = holding.Raw
            })
            .ToArray() ?? [];
    }

    private static IReadOnlyList<FundPeerMetric> ParseEquityHoldings(FundEquityHoldings? holdings)
    {
        return
        [
            new FundPeerMetric
            {
                Average = "Price/Earnings",
                Fund = holdings?.PriceToEarnings,
                CategoryAverage = holdings?.PriceToEarningsCat
            },
            new FundPeerMetric
            {
                Average = "Price/Book",
                Fund = holdings?.PriceToBook,
                CategoryAverage = holdings?.PriceToBookCat
            },
            new FundPeerMetric
            {
                Average = "Price/Sales",
                Fund = holdings?.PriceToSales,
                CategoryAverage = holdings?.PriceToSalesCat
            },
            new FundPeerMetric
            {
                Average = "Price/Cashflow",
                Fund = holdings?.PriceToCashflow,
                CategoryAverage = holdings?.PriceToCashflowCat
            },
            new FundPeerMetric
            {
                Average = "Median Market Cap",
                Fund = holdings?.MedianMarketCap,
                CategoryAverage = holdings?.MedianMarketCapCat
            },
            new FundPeerMetric
            {
                Average = "3 Year Earnings Growth",
                Fund = holdings?.ThreeYearEarningsGrowth,
                CategoryAverage = holdings?.ThreeYearEarningsGrowthCat
            }
        ];
    }

    private static IReadOnlyList<FundPeerMetric> ParseBondHoldings(FundBondHoldings? holdings)
    {
        return
        [
            new FundPeerMetric
            {
                Average = "Duration",
                Fund = holdings?.Duration,
                CategoryAverage = holdings?.DurationCat
            },
            new FundPeerMetric
            {
                Average = "Maturity",
                Fund = holdings?.Maturity,
                CategoryAverage = holdings?.MaturityCat
            },
            new FundPeerMetric
            {
                Average = "Credit Quality",
                Fund = holdings?.CreditQuality,
                CategoryAverage = holdings?.CreditQualityCat
            }
        ];
    }

    private static IReadOnlyDictionary<string, decimal?> FlattenDecimalMaps(
        IReadOnlyList<Dictionary<string, decimal?>>? maps)
    {
        var values = new Dictionary<string, decimal?>(StringComparer.OrdinalIgnoreCase);
        if (maps is null)
        {
            return values;
        }

        foreach (var map in maps)
        {
            foreach (var (key, value) in map)
            {
                if (!string.IsNullOrWhiteSpace(key))
                {
                    values[key] = value;
                }
            }
        }

        return values;
    }

    private static SectorData ParseSectorData(string key, string region, JsonElement root)
    {
        if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
        {
            throw new YahooFinanceException($"Yahoo sector data error for {key}: {root.GetRawText()}");
        }

        return new SectorData
        {
            Key = key,
            Region = region,
            Name = YahooJson.GetString(data, "name"),
            Symbol = YahooJson.GetString(data, "symbol"),
            Overview = data.TryGetProperty("overview", out var overview)
                ? ParseDomainOverview(overview)
                : new DomainOverview(),
            TopCompanies = ParseDomainCompanies(data, "topCompanies"),
            ResearchReports = CloneArray(data, "researchReports"),
            TopEtfs = ParseSymbolNameMap(data, "topETFs"),
            TopMutualFunds = ParseSymbolNameMap(data, "topMutualFunds"),
            Industries = ParseIndustryReferences(data),
            Raw = data.Clone()
        };
    }

    private static IndustryData ParseIndustryData(string key, string region, JsonElement root)
    {
        if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
        {
            throw new YahooFinanceException($"Yahoo industry data error for {key}: {root.GetRawText()}");
        }

        return new IndustryData
        {
            Key = key,
            Region = region,
            Name = YahooJson.GetString(data, "name"),
            Symbol = YahooJson.GetString(data, "symbol"),
            SectorKey = YahooJson.GetString(data, "sectorKey"),
            SectorName = YahooJson.GetString(data, "sectorName"),
            Overview = data.TryGetProperty("overview", out var overview)
                ? ParseDomainOverview(overview)
                : new DomainOverview(),
            TopCompanies = ParseDomainCompanies(data, "topCompanies"),
            ResearchReports = CloneArray(data, "researchReports"),
            TopPerformingCompanies = ParseIndustryPerformingCompanies(data),
            TopGrowthCompanies = ParseIndustryGrowthCompanies(data),
            Raw = data.Clone()
        };
    }

    private static DomainOverview ParseDomainOverview(JsonElement overview)
    {
        return new DomainOverview
        {
            CompaniesCount = TryGetPropertyInt64(overview, "companiesCount"),
            MarketCap = TryGetPropertyDecimal(overview, "marketCap"),
            MessageBoardId = YahooJson.GetString(overview, "messageBoardId"),
            Description = YahooJson.GetString(overview, "description"),
            IndustriesCount = TryGetPropertyInt64(overview, "industriesCount"),
            MarketWeight = TryGetPropertyDecimal(overview, "marketWeight"),
            EmployeeCount = TryGetPropertyInt64(overview, "employeeCount"),
            Raw = overview.Clone()
        };
    }

    private static IReadOnlyList<DomainCompany> ParseDomainCompanies(JsonElement data, string propertyName)
    {
        if (!data.TryGetProperty(propertyName, out var companies) || companies.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return companies.EnumerateArray()
            .Select(company => new DomainCompany
            {
                Symbol = YahooJson.GetString(company, "symbol") ?? string.Empty,
                Name = YahooJson.GetString(company, "name"),
                Rating = YahooJson.GetString(company, "rating"),
                MarketWeight = TryGetPropertyDecimal(company, "marketWeight"),
                Raw = company.Clone()
            })
            .Where(company => !string.IsNullOrWhiteSpace(company.Symbol))
            .ToArray();
    }

    private static IReadOnlyDictionary<string, string?> ParseSymbolNameMap(JsonElement data, string propertyName)
    {
        var map = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (!data.TryGetProperty(propertyName, out var items) || items.ValueKind != JsonValueKind.Array)
        {
            return map;
        }

        foreach (var item in items.EnumerateArray())
        {
            var symbol = YahooJson.GetString(item, "symbol");
            if (!string.IsNullOrWhiteSpace(symbol))
            {
                map[symbol] = YahooJson.GetString(item, "name");
            }
        }

        return map;
    }

    private static IReadOnlyList<IndustryReference> ParseIndustryReferences(JsonElement data)
    {
        if (!data.TryGetProperty("industries", out var industries) || industries.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return industries.EnumerateArray()
            .Where(industry => !string.Equals(
                YahooJson.GetString(industry, "name"),
                "All Industries",
                StringComparison.OrdinalIgnoreCase))
            .Select(industry => new IndustryReference
            {
                Key = YahooJson.GetString(industry, "key") ?? string.Empty,
                Name = YahooJson.GetString(industry, "name"),
                Symbol = YahooJson.GetString(industry, "symbol"),
                MarketWeight = TryGetPropertyDecimal(industry, "marketWeight"),
                Raw = industry.Clone()
            })
            .Where(industry => !string.IsNullOrWhiteSpace(industry.Key))
            .ToArray();
    }

    private static IReadOnlyList<IndustryPerformingCompany> ParseIndustryPerformingCompanies(JsonElement data)
    {
        if (!data.TryGetProperty("topPerformingCompanies", out var companies)
            || companies.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return companies.EnumerateArray()
            .Select(company => new IndustryPerformingCompany
            {
                Symbol = YahooJson.GetString(company, "symbol") ?? string.Empty,
                Name = YahooJson.GetString(company, "name"),
                YtdReturn = TryGetPropertyDecimal(company, "ytdReturn"),
                LastPrice = TryGetPropertyDecimal(company, "lastPrice"),
                TargetPrice = TryGetPropertyDecimal(company, "targetPrice"),
                Raw = company.Clone()
            })
            .Where(company => !string.IsNullOrWhiteSpace(company.Symbol))
            .ToArray();
    }

    private static IReadOnlyList<IndustryGrowthCompany> ParseIndustryGrowthCompanies(JsonElement data)
    {
        if (!data.TryGetProperty("topGrowthCompanies", out var companies)
            || companies.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return companies.EnumerateArray()
            .Select(company => new IndustryGrowthCompany
            {
                Symbol = YahooJson.GetString(company, "symbol") ?? string.Empty,
                Name = YahooJson.GetString(company, "name"),
                YtdReturn = TryGetPropertyDecimal(company, "ytdReturn"),
                GrowthEstimate = TryGetPropertyDecimal(company, "growthEstimate"),
                Raw = company.Clone()
            })
            .Where(company => !string.IsNullOrWhiteSpace(company.Symbol))
            .ToArray();
    }

    private static MarketStatus? ParseMarketStatus(string requestedMarket, JsonElement status)
    {
        var id = YahooJson.GetString(status, "id");
        if (!requestedMarket.Equals("US", StringComparison.OrdinalIgnoreCase)
            && string.Equals(id, "us", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return new MarketStatus
        {
            Id = id,
            Name = YahooJson.GetString(status, "name"),
            Status = YahooJson.GetString(status, "status"),
            Message = YahooJson.GetString(status, "message"),
            Open = TryGetPropertyDateTimeOffset(status, "open"),
            Close = TryGetPropertyDateTimeOffset(status, "close"),
            Timezone = ParseMarketTimezone(status),
            Raw = status.Clone()
        };
    }

    private static MarketTimezone? ParseMarketTimezone(JsonElement status)
    {
        if (!status.TryGetProperty("timezone", out var timezone)
            || timezone.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        var timezoneValue = timezone;
        if (timezone.ValueKind == JsonValueKind.Array)
        {
            if (timezone.GetArrayLength() == 0)
            {
                return null;
            }

            timezoneValue = timezone[0];
        }

        if (timezoneValue.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return new MarketTimezone
        {
            Tz = GetPropertyAsString(timezoneValue, "tz"),
            Short = GetPropertyAsString(timezoneValue, "short"),
            Long = GetPropertyAsString(timezoneValue, "long"),
            GmtOffset = GetPropertyAsString(timezoneValue, "gmtoffset"),
            Raw = timezoneValue.Clone()
        };
    }

    private static IReadOnlyList<OptionContract> ParseContracts(JsonElement contracts)
    {
        if (contracts.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var parsed = new List<OptionContract>();
        foreach (var contract in contracts.EnumerateArray())
        {
            var lastTradeDate = TryGetPropertyInt64(contract, "lastTradeDate");
            var additionalData = contract.EnumerateObject()
                .ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.Ordinal);

            parsed.Add(new OptionContract
            {
                ContractSymbol = YahooJson.GetString(contract, "contractSymbol") ?? string.Empty,
                LastTradeDate = lastTradeDate is null ? null : DateTimeOffset.FromUnixTimeSeconds(lastTradeDate.Value),
                Strike = TryGetPropertyDecimal(contract, "strike"),
                LastPrice = TryGetPropertyDecimal(contract, "lastPrice"),
                Bid = TryGetPropertyDecimal(contract, "bid"),
                Ask = TryGetPropertyDecimal(contract, "ask"),
                Change = TryGetPropertyDecimal(contract, "change"),
                PercentChange = TryGetPropertyDecimal(contract, "percentChange"),
                Volume = TryGetPropertyInt64(contract, "volume"),
                OpenInterest = TryGetPropertyInt64(contract, "openInterest"),
                ImpliedVolatility = TryGetPropertyDecimal(contract, "impliedVolatility"),
                InTheMoney = contract.TryGetProperty("inTheMoney", out var inTheMoney)
                    && inTheMoney.ValueKind is JsonValueKind.True or JsonValueKind.False
                        ? inTheMoney.GetBoolean()
                        : null,
                ContractSize = YahooJson.GetString(contract, "contractSize"),
                Currency = YahooJson.GetString(contract, "currency"),
                AdditionalData = additionalData
            });
        }

        return parsed;
    }

    private static SearchQuote SetSearchQuoteRaw(SearchQuote quote, JsonElement raw) =>
        quote with { Raw = raw.Clone() };

    private static SearchNews SetSearchNewsRaw(SearchNews news, JsonElement raw) =>
        news with { Raw = raw.Clone() };

    private static SearchList SetSearchListRaw(SearchList list, JsonElement raw) =>
        list with { Raw = raw.Clone() };

    private static SearchResearchReport SetSearchResearchReportRaw(SearchResearchReport report, JsonElement raw) =>
        report with { Raw = raw.Clone() };

    private static SearchNavigationLink SetSearchNavigationLinkRaw(SearchNavigationLink link, JsonElement raw) =>
        link with { Raw = raw.Clone() };

    private static IReadOnlyList<T> DeserializeArray<T>(
        JsonElement root,
        string propertyName,
        Func<T, JsonElement, T>? transform = null)
    {
        if (!root.TryGetProperty(propertyName, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return DeserializeArray(array, transform);
    }

    private static IReadOnlyList<T> DeserializeFirstArray<T>(
        JsonElement root,
        IReadOnlyList<string> propertyNames,
        Func<T, JsonElement, T>? transform = null)
    {
        foreach (var propertyName in propertyNames)
        {
            if (root.TryGetProperty(propertyName, out var array) && array.ValueKind == JsonValueKind.Array)
            {
                return DeserializeArray(array, transform);
            }
        }

        return [];
    }

    private static IReadOnlyList<T> DeserializeArray<T>(
        JsonElement array,
        Func<T, JsonElement, T>? transform = null)
    {
        var values = new List<T>();
        foreach (var item in array.EnumerateArray())
        {
            var value = item.Deserialize<T>(YahooJson.SerializerOptions);
            if (value is not null)
            {
                values.Add(transform is null ? value : transform(value, item));
            }
        }

        return values;
    }

    private static IReadOnlyList<JsonElement> CloneArray(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return array.EnumerateArray().Select(item => item.Clone()).ToArray();
    }

    private static Dictionary<long, decimal?> ParseEventAmounts(JsonElement payload, string eventName, string amountProperty)
    {
        var values = new Dictionary<long, decimal?>();
        if (!payload.TryGetProperty("events", out var events)
            || !events.TryGetProperty(eventName, out var eventCollection)
            || eventCollection.ValueKind != JsonValueKind.Object)
        {
            return values;
        }

        foreach (var eventItem in eventCollection.EnumerateObject())
        {
            if (!long.TryParse(eventItem.Name, NumberStyles.Integer, CultureInfo.InvariantCulture, out var timestamp))
            {
                timestamp = TryGetPropertyInt64(eventItem.Value, "date") ?? 0;
            }

            if (timestamp != 0 && eventItem.Value.TryGetProperty(amountProperty, out var amount))
            {
                values[timestamp] = YahooJson.GetDecimal(amount);
            }
        }

        return values;
    }

    private static Dictionary<long, decimal?> ParseSplitEvents(JsonElement payload)
    {
        var values = new Dictionary<long, decimal?>();
        if (!payload.TryGetProperty("events", out var events)
            || !events.TryGetProperty("splits", out var splits)
            || splits.ValueKind != JsonValueKind.Object)
        {
            return values;
        }

        foreach (var splitItem in splits.EnumerateObject())
        {
            if (!long.TryParse(splitItem.Name, NumberStyles.Integer, CultureInfo.InvariantCulture, out var timestamp))
            {
                timestamp = TryGetPropertyInt64(splitItem.Value, "date") ?? 0;
            }

            if (timestamp == 0)
            {
                continue;
            }

            var numerator = TryGetPropertyDecimal(splitItem.Value, "numerator");
            var denominator = TryGetPropertyDecimal(splitItem.Value, "denominator");
            if (numerator is not null && denominator is > 0)
            {
                values[timestamp] = numerator.Value / denominator.Value;
            }
            else if (splitItem.Value.TryGetProperty("splitRatio", out var splitRatio)
                && splitRatio.ValueKind == JsonValueKind.String)
            {
                values[timestamp] = ParseSplitRatio(splitRatio.GetString());
            }
        }

        return values;
    }

    private static decimal? ParseSplitRatio(string? splitRatio)
    {
        if (string.IsNullOrWhiteSpace(splitRatio))
        {
            return null;
        }

        var parts = splitRatio.Split(':', StringSplitOptions.TrimEntries);
        if (parts.Length != 2
            || !decimal.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var numerator)
            || !decimal.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var denominator)
            || denominator == 0)
        {
            return null;
        }

        return numerator / denominator;
    }

    private static (decimal? Open, decimal? High, decimal? Low, decimal? Close) ApplyAdjustment(
        bool autoAdjust,
        bool backAdjust,
        decimal? open,
        decimal? high,
        decimal? low,
        decimal? close,
        decimal? adjustedClose)
    {
        if (close is null or 0 || adjustedClose is null)
        {
            return (open, high, low, close);
        }

        var ratio = adjustedClose.Value / close.Value;
        if (autoAdjust)
        {
            return (open * ratio, high * ratio, low * ratio, adjustedClose);
        }

        return backAdjust
            ? (open * ratio, high * ratio, low * ratio, close)
            : (open, high, low, close);
    }

    private static decimal? TryGetPropertyDecimal(JsonElement element, string propertyName)
    {
        return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var property)
            ? YahooJson.GetDecimal(property)
            : null;
    }

    private static long? TryGetPropertyInt64(JsonElement element, string propertyName)
    {
        return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var property)
            ? YahooJson.GetInt64(property)
            : null;
    }

    private static DateTimeOffset? TryGetPropertyDateTimeOffset(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Object && property.TryGetProperty("raw", out var raw))
        {
            property = raw;
        }

        if (YahooJson.GetInt64(property) is { } unixTime)
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixTime);
        }

        if (property.ValueKind == JsonValueKind.String
            && DateTimeOffset.TryParse(
                property.GetString(),
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static string? GetPropertyAsString(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Object && property.TryGetProperty("raw", out var raw))
        {
            property = raw;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => property.ToString(),
            _ => null
        };
    }

    private static JsonElement GetPropertyOrDefault(JsonElement element, string propertyName)
    {
        return element.ValueKind != JsonValueKind.Undefined && element.TryGetProperty(propertyName, out var property)
            ? property
            : default;
    }

    private static DateTimeOffset ConvertChartTimestamp(
        long timestamp,
        string? exchangeTimezoneName,
        bool ignoreTimezone)
    {
        var utc = DateTimeOffset.FromUnixTimeSeconds(timestamp);
        if (ignoreTimezone || string.IsNullOrWhiteSpace(exchangeTimezoneName))
        {
            return utc;
        }

        try
        {
            return TimeZoneInfo.ConvertTime(utc, TimeZoneInfo.FindSystemTimeZoneById(exchangeTimezoneName));
        }
        catch (TimeZoneNotFoundException)
        {
            return utc;
        }
        catch (InvalidTimeZoneException)
        {
            return utc;
        }
    }

    private static int GetPriceHint(JsonElement meta)
    {
        var priceHint = TryGetPropertyInt64(meta, "priceHint");
        if (priceHint is null)
        {
            return 2;
        }

        return Math.Clamp((int)priceHint.Value, 0, 12);
    }

    private static decimal? MaybeRound(decimal? value, bool round, int priceHint)
    {
        return value is not null && round ? decimal.Round(value.Value, priceHint) : value;
    }

    private static long DateOnlyToUnix(DateOnly date)
    {
        return new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).ToUnixTimeSeconds();
    }

    private static Uri BuildUri(string baseUrl, string path, IReadOnlyDictionary<string, string?> query)
    {
        var builder = new UriBuilder(baseUrl.TrimEnd('/') + "/" + path.TrimStart('/'));
        var parameters = query
            .Where(pair => pair.Value is not null)
            .Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value!)}");

        builder.Query = string.Join('&', parameters);
        return builder.Uri;
    }

    private static Uri WithQueryParameter(Uri uri, string name, string value)
    {
        var builder = new UriBuilder(uri);
        var existingQuery = builder.Query.TrimStart('?');
        var newParameter = $"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value)}";
        builder.Query = string.IsNullOrWhiteSpace(existingQuery)
            ? newParameter
            : $"{existingQuery}&{newParameter}";

        return builder.Uri;
    }

    private static bool RequiresCrumbRetry(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;

    private static string NormalizeSymbol(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new ArgumentException("Ticker symbol cannot be empty.", nameof(symbol));
        }

        return symbol.Trim().ToUpperInvariant();
    }

    private static string NormalizeDomainKey(string key, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Yahoo domain key cannot be empty.", parameterName);
        }

        return key.Trim().ToLowerInvariant();
    }

    private static string NormalizeRegion(string region)
    {
        if (string.IsNullOrWhiteSpace(region))
        {
            throw new ArgumentException("Yahoo region cannot be empty.", nameof(region));
        }

        return region.Trim().ToUpperInvariant();
    }

    private static string FormatBool(bool value) => value ? "true" : "false";

    private static string FormatCalendarDate(DateOnly date) =>
        date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string CalendarTypeToYahoo(CalendarType type) =>
        type switch
        {
            CalendarType.Earnings => "sp_earnings",
            CalendarType.Ipo => "ipo_info",
            CalendarType.EconomicEvent => "economic_event",
            CalendarType.Splits => "splits",
            _ => "sp_earnings"
        };

    private static string DefaultCalendarSortField(CalendarType type) =>
        type switch
        {
            CalendarType.Earnings => "intradaymarketcap",
            CalendarType.Ipo or CalendarType.EconomicEvent or CalendarType.Splits => "startdatetime",
            _ => "startdatetime"
        };

    private static IReadOnlyList<string> DefaultCalendarIncludeFields(CalendarType type) =>
        type switch
        {
            CalendarType.Earnings =>
            [
                "ticker",
                "companyshortname",
                "intradaymarketcap",
                "eventname",
                "startdatetime",
                "startdatetimetype",
                "epsestimate",
                "epsactual",
                "epssurprisepct"
            ],
            CalendarType.Ipo =>
            [
                "ticker",
                "companyshortname",
                "exchange_short_name",
                "filingdate",
                "startdatetime",
                "amendeddate",
                "pricefrom",
                "priceto",
                "offerprice",
                "currencyname",
                "shares",
                "dealtype"
            ],
            CalendarType.EconomicEvent =>
            [
                "econ_release",
                "country_code",
                "startdatetime",
                "period",
                "after_release_actual",
                "consensus_estimate",
                "prior_release_actual",
                "originally_reported_actual"
            ],
            CalendarType.Splits =>
            [
                "ticker",
                "companyshortname",
                "startdatetime",
                "optionable",
                "old_share_worth",
                "share_worth"
            ],
            _ => []
        };

    private static string? GetCalendarString(CalendarParsedRow row, params string[] keys)
    {
        if (!TryGetCalendarCell(row, keys, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
            if (value.TryGetProperty("raw", out var raw))
            {
                return CalendarValueToString(raw);
            }

            if (value.TryGetProperty("fmt", out var formatted))
            {
                return CalendarValueToString(formatted);
            }
        }

        return CalendarValueToString(value);
    }

    private static decimal? GetCalendarDecimal(CalendarParsedRow row, params string[] keys) =>
        TryGetCalendarCell(row, keys, out var value) ? YahooJson.GetDecimal(value) : null;

    private static DateTimeOffset? GetCalendarDate(CalendarParsedRow row, params string[] keys)
    {
        if (!TryGetCalendarCell(row, keys, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Object && value.TryGetProperty("raw", out var raw))
        {
            value = raw;
        }

        if (YahooJson.GetInt64(value) is { } unixTime)
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixTime);
        }

        if (value.ValueKind == JsonValueKind.String
            && DateTimeOffset.TryParse(
                value.GetString(),
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static bool? GetCalendarBool(CalendarParsedRow row, params string[] keys)
    {
        if (!TryGetCalendarCell(row, keys, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Object && value.TryGetProperty("raw", out var raw))
        {
            value = raw;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(value.GetString(), out var parsed) => parsed,
            JsonValueKind.String when string.Equals(value.GetString(), "Yes", StringComparison.OrdinalIgnoreCase) => true,
            JsonValueKind.String when string.Equals(value.GetString(), "No", StringComparison.OrdinalIgnoreCase) => false,
            JsonValueKind.Number when YahooJson.GetInt64(value) is { } numeric => numeric != 0,
            _ => null
        };
    }

    private static bool TryGetCalendarCell(CalendarParsedRow row, IReadOnlyList<string> keys, out JsonElement value)
    {
        foreach (var key in keys)
        {
            if (row.Values.TryGetValue(key, out value)
                && value.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined)
            {
                return true;
            }
        }

        value = default;
        return false;
    }

    private static string? CalendarValueToString(JsonElement value) =>
        value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => value.ToString(),
            _ => null
        };

    private static string LookupTypeToYahoo(LookupType type) =>
        type switch
        {
            LookupType.All => "all",
            LookupType.Equity => "equity",
            LookupType.MutualFund => "mutualfund",
            LookupType.Etf => "etf",
            LookupType.Index => "index",
            LookupType.Future => "future",
            LookupType.Currency => "currency",
            LookupType.Cryptocurrency => "cryptocurrency",
            _ => "all"
        };

    private static string ScreenerQuoteTypeToYahoo(ScreenerQuoteType type) =>
        type switch
        {
            ScreenerQuoteType.Equity => "EQUITY",
            ScreenerQuoteType.MutualFund => "MUTUALFUND",
            ScreenerQuoteType.Etf => "ETF",
            _ => "EQUITY"
        };

    private static string MarketRegionToYahoo(MarketRegion region) =>
        region switch
        {
            MarketRegion.US => "US",
            MarketRegion.GB => "GB",
            MarketRegion.Asia => "ASIA",
            MarketRegion.Europe => "EUROPE",
            MarketRegion.Rates => "RATES",
            MarketRegion.Commodities => "COMMODITIES",
            MarketRegion.Currencies => "CURRENCIES",
            MarketRegion.Cryptocurrencies => "CRYPTOCURRENCIES",
            _ => "US"
        };

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private sealed record CalendarParsedRow(IReadOnlyDictionary<string, JsonElement> Values, JsonElement Raw);

    private sealed record CalendarPayload(
        CalendarType Type,
        IReadOnlyList<CalendarColumn> Columns,
        IReadOnlyList<CalendarParsedRow> Rows,
        JsonElement Raw);

    private sealed record ResponseContent(HttpStatusCode StatusCode, string? ReasonPhrase, string Content)
    {
        public bool IsSuccessStatusCode => (int)StatusCode is >= 200 and <= 299;
    }
}
