using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace YFSharp;

public static class YFSharpServiceCollectionExtensions
{
    public static IHttpClientBuilder AddYFSharp(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddYFSharp(static options => options);
    }

    public static IHttpClientBuilder AddYFSharp(
        this IServiceCollection services,
        YahooFinanceClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        return AddYFSharpCore(services, options);
    }

    public static IHttpClientBuilder AddYFSharp(
        this IServiceCollection services,
        Func<YahooFinanceClientOptions, YahooFinanceClientOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        return AddYFSharpCore(services, configure(new YahooFinanceClientOptions()));
    }

    private static IHttpClientBuilder AddYFSharpCore(
        IServiceCollection services,
        YahooFinanceClientOptions options)
    {
        services.Replace(ServiceDescriptor.Singleton(options));

        var builder = services.AddHttpClient<YahooFinanceClient>((serviceProvider, httpClient) =>
        {
            var clientOptions = serviceProvider.GetRequiredService<YahooFinanceClientOptions>();
            httpClient.Timeout = clientOptions.RequestTimeout;
            YahooFinanceClient.ApplyDefaultRequestHeaders(httpClient, clientOptions);
        });

        builder.ConfigurePrimaryHttpMessageHandler(serviceProvider =>
        {
            var clientOptions = serviceProvider.GetRequiredService<YahooFinanceClientOptions>();
            var handler = new HttpClientHandler
            {
                AutomaticDecompression =
                    DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
            };
            if (clientOptions.Proxy is not null)
            {
                handler.Proxy = clientOptions.Proxy;
                handler.UseProxy = true;
            }

            return handler;
        });

        services.TryAddTransient<IYahooFinanceClient>(serviceProvider =>
            serviceProvider.GetRequiredService<YahooFinanceClient>());

        return builder;
    }
}
