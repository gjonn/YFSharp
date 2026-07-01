using YFSharp.Models;

namespace YFSharp;

public sealed class Tickers
{
    private readonly IYahooFinanceClient _client;

    internal Tickers(IYahooFinanceClient client, IReadOnlyList<string> symbols)
    {
        _client = client;
        Symbols = symbols;
    }

    public IReadOnlyList<string> Symbols { get; }

    public Task<IReadOnlyList<Quote>> QuotesAsync(CancellationToken cancellationToken = default) =>
        _client.GetQuotesAsync(Symbols, cancellationToken);

    public Task<IReadOnlyDictionary<string, HistoricalData>> DownloadAsync(
        HistoryRequest? request = null,
        int maxConcurrency = 8,
        CancellationToken cancellationToken = default) =>
        _client.DownloadAsync(Symbols, request, maxConcurrency, cancellationToken);

    public Task<DownloadResult> TryDownloadAsync(
        HistoryRequest? request = null,
        int maxConcurrency = 8,
        CancellationToken cancellationToken = default) =>
        _client.TryDownloadAsync(Symbols, request, maxConcurrency, cancellationToken);
}
