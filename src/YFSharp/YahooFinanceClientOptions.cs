using System.Net;

namespace YFSharp;

public enum YahooFinanceRequestProfile
{
    Default,
    Chrome
}

public sealed record YahooFinanceClientOptions
{
    public const string ChromeUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/133.0.0.0 Safari/537.36";

    public string Query1BaseUrl { get; init; } = "https://query1.finance.yahoo.com";

    public string Query2BaseUrl { get; init; } = "https://query2.finance.yahoo.com";

    public YahooFinanceRequestProfile RequestProfile { get; init; } = YahooFinanceRequestProfile.Chrome;

    public string UserAgent { get; init; } = ChromeUserAgent;

    public string AcceptLanguage { get; init; } = "en-US,en;q=0.9";

    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(30);

    public int MaxRetries { get; init; }

    public Func<int, TimeSpan>? RateLimitBackoff { get; init; }

    public IYahooFinanceAuthStore? AuthStore { get; init; }

    public TimeSpan AuthStateTtl { get; init; } = TimeSpan.FromHours(12);

    public TimeProvider TimeProvider { get; init; } = TimeProvider.System;

    public IWebProxy? Proxy { get; init; }
}
