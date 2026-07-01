namespace YFSharp;

/// <summary>
/// Describes a Yahoo Finance historical price request.
/// </summary>
public sealed record HistoryRequest
{
    /// <summary>
    /// Yahoo range value such as <c>1mo</c>, <c>1y</c>, <c>ytd</c>, or <c>max</c>.
    /// Ignored when <see cref="Start"/> is specified.
    /// </summary>
    public string? Period { get; init; } = "1mo";

    /// <summary>
    /// Inclusive start time for date-based history requests.
    /// </summary>
    public DateTimeOffset? Start { get; init; }

    /// <summary>
    /// Exclusive end time for date-based history requests. Defaults to now when <see cref="Start"/> is specified.
    /// </summary>
    public DateTimeOffset? End { get; init; }

    /// <summary>
    /// Yahoo interval value such as <c>1d</c>, <c>1wk</c>, <c>1mo</c>, or an intraday interval.
    /// </summary>
    public string Interval { get; init; } = "1d";

    /// <summary>
    /// Includes pre-market and post-market rows when Yahoo supports them for the interval.
    /// </summary>
    public bool IncludePrePost { get; init; }

    /// <summary>
    /// Includes dividend, split, and capital-gain event columns.
    /// </summary>
    public bool IncludeActions { get; init; } = true;

    /// <summary>
    /// Adjusts OHLC prices using Yahoo adjusted-close data.
    /// </summary>
    public bool AutoAdjust { get; init; } = true;

    /// <summary>
    /// Back-adjusts historical OHLC values while preserving the raw close.
    /// </summary>
    public bool BackAdjust { get; init; }

    /// <summary>
    /// Attempts to repair obvious Yahoo unit, dividend, and split anomalies.
    /// </summary>
    public bool Repair { get; init; }

    /// <summary>
    /// Leaves timestamps in UTC instead of converting to the exchange timezone.
    /// </summary>
    public bool IgnoreTimezone { get; init; }

    /// <summary>
    /// Keeps rows where Yahoo returned only null OHLCV values.
    /// </summary>
    public bool KeepNullRows { get; init; }

    /// <summary>
    /// Rounds price-like fields using Yahoo's <c>priceHint</c> metadata.
    /// </summary>
    public bool Round { get; init; }

    /// <summary>
    /// Obsolete alias for <see cref="Round"/>.
    /// </summary>
    [Obsolete("Use Round instead. Rounding will be removed in a future major version.")]
    public bool Rounding { get; init; }

    /// <summary>
    /// Creates a history request using Yahoo's range syntax.
    /// </summary>
    public static HistoryRequest ForPeriod(string period, string interval = "1d") =>
        new()
        {
            Period = period,
            Interval = interval,
            Start = null,
            End = null
        };

    /// <summary>
    /// Creates a history request using explicit start and end dates.
    /// </summary>
    public static HistoryRequest ForDates(
        DateTimeOffset start,
        DateTimeOffset? end = null,
        string interval = "1d") =>
        new()
        {
            Period = null,
            Start = start,
            End = end,
            Interval = interval
        };
}
