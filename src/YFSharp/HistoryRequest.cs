namespace YFSharp;

public sealed record HistoryRequest
{
    public string? Period { get; init; } = "1mo";

    public DateTimeOffset? Start { get; init; }

    public DateTimeOffset? End { get; init; }

    public string Interval { get; init; } = "1d";

    public bool IncludePrePost { get; init; }

    public bool IncludeActions { get; init; } = true;

    public bool AutoAdjust { get; init; } = true;

    public bool BackAdjust { get; init; }

    public bool Repair { get; init; }

    public bool IgnoreTimezone { get; init; }

    public bool KeepNullRows { get; init; }

    public bool Round { get; init; }

    public bool Rounding { get; init; }

    public static HistoryRequest ForPeriod(string period, string interval = "1d") =>
        new()
        {
            Period = period,
            Interval = interval,
            Start = null,
            End = null
        };

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
