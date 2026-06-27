# YFSharp

YFSharp is a .NET Yahoo Finance client inspired by
[ranaroussi/yfinance](https://github.com/ranaroussi/yfinance). It keeps familiar
yfinance concepts such as `Ticker`, `Tickers`, `Download`, search, screeners,
options chains, quote summaries, calendars, funds, sectors, industries, and
streaming prices while exposing idiomatic async C# APIs.

Yahoo Finance is not an official public API. Use YFSharp for research,
education, personal tooling, and prototypes, and review Yahoo's terms before
using Yahoo data in a product.

## Requirements

- .NET 8 or newer for consuming the package.
- .NET 10 SDK or newer for building and testing this repository.
- Network access to Yahoo Finance endpoints.
- Optional: `Microsoft.Extensions.DependencyInjection` if you want typed-client
  registration through `services.AddYFSharp(...)`.

## Installation

Use the package when it is available from your configured NuGet feed:

```bash
dotnet add package YFSharp
```

Or reference a local checkout directly:

```bash
dotnet add <your-app>.csproj reference /path/to/YFSharp/src/YFSharp/YFSharp.csproj
```

Then import the namespaces you need:

```csharp
using YFSharp;
using YFSharp.Models;
```

## Quick Start

```csharp
using YFSharp;
using YFSharp.Models;

using var yf = new YahooFinanceClient();

var aapl = yf.Ticker("AAPL");

var quote = await aapl.QuoteAsync();
Console.WriteLine($"{quote?.Symbol}: {quote?.RegularMarketPrice}");

var history = await aapl.HistoryAsync(HistoryRequest.ForPeriod("1mo", "1d"));
foreach (var bar in history.Bars.TakeLast(5))
{
    Console.WriteLine($"{bar.Time:yyyy-MM-dd}: {bar.Close}");
}

var chain = await aapl.OptionChainAsync();
var expirations = await aapl.OptionsAsync();

var multi = await yf.DownloadAsync(
    ["AAPL", "MSFT"],
    HistoryRequest.ForDates(DateTimeOffset.UtcNow.AddMonths(-3), interval: "1d"));

var csv = HistoricalData.ToCsv(multi, HistoryGroupBy.Ticker);
```

For scripts and notebooks, the static `YF` facade owns a shared client:

```csharp
var msft = await YF.Ticker("MSFT").GetFastInfoAsync();
var histories = await YF.DownloadAsync(["AAPL", "MSFT", "NVDA"]);
var search = await YF.SearchAsync("vanguard total market");
```

For applications, prefer `YahooFinanceClient` or dependency injection so you
control lifetime, retries, and storage.

## Dependency Injection

```csharp
using YFSharp;
using YFSharp.Models;

services.AddYFSharp(options => options with
{
    MaxRetries = 2,
    RateLimitBackoff = attempt =>
        TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, attempt))),
    AuthStore = new FileYahooFinanceAuthStore("yfinance-auth.json")
});
```

You can inject either type:

```csharp
public sealed class MarketDataJob(IYahooFinanceClient yf)
{
    public Task<Quote?> GetAppleAsync(CancellationToken cancellationToken) =>
        yf.GetQuoteAsync("AAPL", cancellationToken);
}
```

`AddYFSharp` registers `YahooFinanceClient` as a typed `HttpClient` client and
maps `IYahooFinanceClient` to it. The default request profile uses a
browser-like user agent, accept-language header, fetch metadata headers, and
automatic gzip/deflate/brotli decompression.

## Client Options

`YahooFinanceClientOptions` controls the network behavior:

| Option | Purpose |
| --- | --- |
| `RequestProfile` | `Chrome` by default, or `Default` for a lighter JSON profile. |
| `UserAgent`, `AcceptLanguage` | Override outgoing headers. |
| `RequestTimeout` | Per-request timeout, defaulting to 30 seconds. |
| `MaxRetries`, `RateLimitBackoff` | Retry Yahoo `429` responses with caller-controlled backoff. |
| `AuthStore` | Persist Yahoo cookie/crumb state with `InMemoryYahooFinanceAuthStore` or `FileYahooFinanceAuthStore`. |
| `AuthStateTtl` | Expiration window for stored cookie/crumb state. |
| `Proxy` | Route requests through an `IWebProxy`. |

Treat a file-backed auth store like local credentials: it only contains Yahoo
cookie/crumb state, but it should not be committed.

## Common Workflows

### Quotes and History

```csharp
var ticker = yf.Ticker("AAPL");

var quote = await ticker.QuoteAsync();
var daily = await ticker.HistoryAsync(HistoryRequest.ForPeriod("6mo"));
var intraday = await ticker.HistoryAsync(
    HistoryRequest.ForPeriod("5d", "5m") with { IncludePrePost = true });

var adjustedOff = await ticker.HistoryAsync(
    HistoryRequest.ForPeriod("1y") with { AutoAdjust = false });
```

`HistoryRequest` supports yfinance-style period and interval values, plus
`AutoAdjust`, `BackAdjust`, `Repair`, `KeepNullRows`, `Round`, and
`IncludeActions`. Intraday intervals are limited by Yahoo retention windows.

### Multiple Symbols

```csharp
var tickers = yf.Tickers(["AAPL", "MSFT", "NVDA"]);

var quotes = await tickers.QuotesAsync();
var histories = await tickers.DownloadAsync(
    HistoryRequest.ForPeriod("1mo"),
    maxConcurrency: 4);
```

`DownloadAsync` returns a dictionary keyed by normalized symbol. Use
`HistoricalData.ToCsv(...)` or `HistoricalData.GroupByTicker(...)` when you want
tabular output.

### Quote Summary and Fundamentals

```csharp
var ticker = yf.Ticker("MSFT");

var info = await ticker.GetInfoAsync();
var fastInfo = await ticker.GetFastInfoAsync();
var income = await ticker.IncomeStatementAsync();
var quarterlyCashFlow = await ticker.QuarterlyCashFlowAsync();
var ttmCashFlow = await ticker.TtmCashFlowAsync();
var holders = await ticker.InstitutionalHoldersAsync();
var recommendations = await ticker.GetRecommendationsAsync();
var filings = await ticker.GetSecFilingsAsync();

var summary = await ticker.QuoteSummaryAsync(
    [QuoteSummaryModules.Price, QuoteSummaryModules.SummaryDetail]);
```

YFSharp returns typed models for stable Yahoo shapes and cloned `JsonElement`
values for wide or fluid modules. `QuoteSummary.TryGetModule(...)` and
`QuoteSummary.GetModule<T>(...)` are available when you need to work directly
with a module.

### Funds

```csharp
var vti = yf.Ticker("VTI");

var fund = await vti.FundsDataAsync();
var holdings = await vti.FundTopHoldingsAsync();
var profile = await vti.FundProfileAsync();
var sectors = await vti.FundSectorWeightingsAsync();
```

### Options

```csharp
var aapl = yf.Ticker("AAPL");

var expirations = await aapl.OptionsAsync();
var currentChain = await aapl.OptionChainAsync();
var datedChain = expirations.Count > 0
    ? await aapl.OptionChainAsync(expirations[0])
    : currentChain;
```

### Search, Lookup, and Screeners

```csharp
var search = await yf.SearchAsync(
    "microsoft",
    new SearchOptions { QuotesCount = 3, NewsCount = 1 });

var stocks = await yf.GetStocksAsync("apple", count: 10);
var gainers = await yf.ScreenAsync(PredefinedScreeners.DayGainers, count: 25);

var custom = await yf.ScreenAsync(new ScreenerRequest
{
    Count = 10,
    SortField = "percentchange",
    Query = new EquityQuery("and",
    [
        new EquityQuery("gt", ["percentchange", 3]),
        new EquityQuery("eq", ["region", "us"])
    ])
});
```

Typed query classes (`EquityQuery`, `FundQuery`, and `EtfQuery`) validate Yahoo
screener fields, operators, and restricted values before sending the request.

### Calendars, Markets, Sectors, and Industries

```csharp
var calendars = yf.Calendars(
    new DateOnly(2026, 7, 1),
    new DateOnly(2026, 7, 31));

var earnings = await calendars.GetEarningsCalendarAsync(limit: 50);
var ipos = await calendars.GetIpoCalendarAsync();
var events = await calendars.GetEconomicEventsCalendarAsync();
var splits = await calendars.GetSplitsCalendarAsync();

var market = await yf.GetMarketAsync(MarketRegion.US);
var technology = await yf.Sector("technology").DataAsync();
var softwareInfrastructure =
    await yf.Industry("software-infrastructure").DataAsync();
```

### Streaming Prices

```csharp
await using var stream = YF.WebSocket();

await foreach (var price in stream.StreamAsync(["AAPL", "MSFT"]))
{
    Console.WriteLine($"{price.Symbol}: {price.Price} {price.Currency}");
}
```

`YahooWebSocketClient` also supports explicit `ConnectAsync`,
`SubscribeAsync`, `UnsubscribeAsync`, reconnect delay configuration, and
subscription refreshes.

## API Map

| Area | Entry points |
| --- | --- |
| Client | `YahooFinanceClient`, `IYahooFinanceClient`, `YF` |
| Facades | `Ticker`, `Tickers`, `Sector`, `Industry`, `Calendars` |
| Quotes | `GetQuoteAsync`, `GetQuotesAsync`, `Ticker.QuoteAsync` |
| History | `GetHistoryAsync`, `DownloadAsync`, `Ticker.HistoryAsync`, `Tickers.DownloadAsync` |
| Quote summary | `GetQuoteSummaryAsync`, `Ticker.GetInfoAsync`, financial statement, analysis, holder, insider, recommendation, filing, and ESG helpers |
| Funds | `Ticker.FundsDataAsync` and fund-specific helper methods |
| Options | `GetOptionsAsync`, `GetOptionExpirationsAsync`, `Ticker.OptionChainAsync`, `Ticker.OptionsAsync` |
| Discovery | `SearchAsync`, `LookupAsync`, typed lookup helpers |
| Screeners | `ScreenAsync`, `PredefinedScreeners`, `ScreenerRequest` |
| Calendars | Earnings, IPO, economic event, and split calendars |
| Domains | `GetSectorAsync`, `GetIndustryAsync`, `GetMarketAsync` |
| Streaming | `YahooWebSocketClient`, `YF.WebSocket()` |

## Error Handling

YFSharp throws `YahooFinanceException` for Yahoo-specific failures.
`YahooFinanceHttpException` includes the HTTP status code and response body.
`YahooFinanceRateLimitException` is thrown for rate-limit responses when retries
are exhausted or disabled.

Yahoo may return `429 Edge: Too Many Requests`, maintenance pages, missing
modules, or shape changes. Code against nullable properties, set a retry policy
appropriate for your app, and prefer bounded concurrency for bulk downloads.

## Local Development

```bash
dotnet restore YFSharp.slnx
dotnet build YFSharp.slnx
dotnet test YFSharp.slnx
```

Live Yahoo tests are skipped by default:

```bash
YFSHARP_LIVE_TESTS=1 dotnet test YFSharp.slnx
```

Live tests depend on Yahoo's current behavior and your network. Rate limits are
expected on some networks and should be treated as inconclusive rather than as
proof that the local implementation is broken.

## Current Scope

Implemented:

- `YahooFinanceClient` with injectable `HttpClient`.
- `services.AddYFSharp(...)` typed-client registration.
- Static `YF` convenience API.
- `Ticker` and `Tickers` yfinance-style facades.
- Historical chart data with adjustment, repair, validation, exchange-time
  handling, and CSV export helpers.
- Multi-symbol downloads with bounded concurrency.
- Quote snapshots, quote summaries, typed fundamentals, analyst data, holders,
  insiders, filings, sustainability, funds, options, search, lookup, screeners,
  calendars, market status, sectors, industries, and websocket streaming.
- Cookie/crumb acquisition and optional in-memory or file-backed auth state.
- Unit tests with mocked Yahoo responses and saved-payload contract fixtures.
- Optional live tests behind `YFSHARP_LIVE_TESTS=1`.
