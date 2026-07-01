using System.Net;

namespace YFSharp;

/// <summary>
/// Selects the outgoing request profile used for Yahoo Finance HTTP calls.
/// </summary>
public enum YahooFinanceRequestProfile
{
    /// <summary>
    /// Sends a small JSON-oriented header set.
    /// </summary>
    Default,

    /// <summary>
    /// Sends browser-like headers that more closely match Yahoo Finance web traffic.
    /// </summary>
    Chrome
}

/// <summary>
/// Configures network, retry, authentication, and endpoint behavior for <see cref="YahooFinanceClient"/>.
/// </summary>
public sealed record YahooFinanceClientOptions
{
    /// <summary>
    /// Default Chrome-like user agent used by the <see cref="YahooFinanceRequestProfile.Chrome"/> profile.
    /// </summary>
    public const string ChromeUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/133.0.0.0 Safari/537.36";

    /// <summary>
    /// Base URL for Yahoo query1 endpoints.
    /// </summary>
    public string Query1BaseUrl { get; init; } = "https://query1.finance.yahoo.com";

    /// <summary>
    /// Base URL for Yahoo query2 endpoints.
    /// </summary>
    public string Query2BaseUrl { get; init; } = "https://query2.finance.yahoo.com";

    /// <summary>
    /// Request profile used when adding default headers.
    /// </summary>
    public YahooFinanceRequestProfile RequestProfile { get; init; } = YahooFinanceRequestProfile.Chrome;

    /// <summary>
    /// User-Agent header value used when the supplied <see cref="HttpClient"/> has no user agent.
    /// </summary>
    public string UserAgent { get; init; } = ChromeUserAgent;

    /// <summary>
    /// Accept-Language header value used when the supplied <see cref="HttpClient"/> has no accept-language header.
    /// </summary>
    public string AcceptLanguage { get; init; } = "en-US,en;q=0.9";

    /// <summary>
    /// Per-request timeout. Cancellation tokens passed by the caller are linked with this timeout.
    /// </summary>
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Maximum number of retry attempts after a Yahoo rate-limit response.
    /// </summary>
    public int MaxRetries { get; init; }

    /// <summary>
    /// Optional backoff function. The argument is the one-based retry attempt number.
    /// </summary>
    public Func<int, TimeSpan>? RateLimitBackoff { get; init; }

    /// <summary>
    /// Optional store for Yahoo cookie and crumb state.
    /// </summary>
    public IYahooFinanceAuthStore? AuthStore { get; init; }

    /// <summary>
    /// Maximum age for persisted Yahoo cookie and crumb state.
    /// </summary>
    public TimeSpan AuthStateTtl { get; init; } = TimeSpan.FromHours(12);

    /// <summary>
    /// Time provider used for request validation, auth-state freshness, and tests.
    /// </summary>
    public TimeProvider TimeProvider { get; init; } = TimeProvider.System;

    /// <summary>
    /// Optional proxy used by internally created HTTP handlers and DI registration.
    /// </summary>
    public IWebProxy? Proxy { get; init; }
}
