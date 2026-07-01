namespace YFSharp.Models;

/// <summary>
/// Captures the result of a multi-symbol historical download where individual symbols may fail.
/// </summary>
public sealed record DownloadResult
{
    /// <summary>
    /// Historical data keyed by normalized symbol for symbols that completed successfully.
    /// </summary>
    public IReadOnlyDictionary<string, HistoricalData> Histories { get; init; } =
        new Dictionary<string, HistoricalData>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Per-symbol failures keyed by normalized symbol.
    /// </summary>
    public IReadOnlyDictionary<string, YahooFinanceDownloadError> Errors { get; init; } =
        new Dictionary<string, YahooFinanceDownloadError>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// True when every requested symbol completed successfully.
    /// </summary>
    public bool Succeeded => Errors.Count == 0;
}

/// <summary>
/// Describes a failure for one symbol in a resilient multi-symbol download.
/// </summary>
public sealed record YahooFinanceDownloadError
{
    /// <summary>
    /// Normalized symbol that failed.
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// Error message produced by the failed request.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Original exception for applications that need structured failure handling.
    /// </summary>
    public required Exception Exception { get; init; }
}
