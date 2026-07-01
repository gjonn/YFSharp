using System.Net;
using System.Net.Http.Headers;

namespace YFSharp;

public sealed partial class YahooFinanceClient
{
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

        ValidateBaseUrl(options.Query1BaseUrl, nameof(YahooFinanceClientOptions.Query1BaseUrl));
        ValidateBaseUrl(options.Query2BaseUrl, nameof(YahooFinanceClientOptions.Query2BaseUrl));

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

    private static void ValidateBaseUrl(string baseUrl, string optionName)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new ArgumentException("Base URL cannot be empty.", optionName);
        }

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https")
            || string.IsNullOrWhiteSpace(uri.Host))
        {
            throw new ArgumentException("Base URL must be an absolute HTTP or HTTPS URL.", optionName);
        }
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
}
