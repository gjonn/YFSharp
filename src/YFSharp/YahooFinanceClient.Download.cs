using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using YFSharp.Models;

namespace YFSharp;

public sealed partial class YahooFinanceClient
{
    public async Task<DownloadResult> TryDownloadAsync(
        IEnumerable<string> symbols,
        HistoryRequest? request = null,
        int maxConcurrency = 8,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(symbols);

        if (maxConcurrency < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxConcurrency), "Concurrency must be at least 1.");
        }

        var normalizedSymbols = symbols.Select(NormalizeSymbol)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalizedSymbols.Length == 0)
        {
            return new DownloadResult();
        }

        var histories = new ConcurrentDictionary<string, HistoricalData>(StringComparer.OrdinalIgnoreCase);
        var errors = new ConcurrentDictionary<string, YahooFinanceDownloadError>(StringComparer.OrdinalIgnoreCase);
        using var gate = new SemaphoreSlim(maxConcurrency);

        var tasks = normalizedSymbols.Select(async symbol =>
        {
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                histories[symbol] = await GetHistoryAsync(symbol, request, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Yahoo Finance history download failed for {Symbol}.",
                    symbol);

                errors[symbol] = new YahooFinanceDownloadError
                {
                    Symbol = symbol,
                    Message = exception.Message,
                    Exception = exception
                };
            }
            finally
            {
                gate.Release();
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);

        return new DownloadResult
        {
            Histories = normalizedSymbols
                .Where(histories.ContainsKey)
                .ToDictionary(symbol => symbol, symbol => histories[symbol], StringComparer.OrdinalIgnoreCase),
            Errors = normalizedSymbols
                .Where(errors.ContainsKey)
                .ToDictionary(symbol => symbol, symbol => errors[symbol], StringComparer.OrdinalIgnoreCase)
        };
    }
}
