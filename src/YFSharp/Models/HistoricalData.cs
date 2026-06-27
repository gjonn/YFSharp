using System.Globalization;
using System.Text;
using System.Text.Json;

namespace YFSharp.Models;

public sealed record HistoricalData
{
    public string Symbol { get; init; } = string.Empty;

    public string? Currency { get; init; }

    public string? ExchangeName { get; init; }

    public string? ExchangeTimezoneName { get; init; }

    public decimal? RegularMarketPrice { get; init; }

    public IReadOnlyList<PriceBar> Bars { get; init; } = [];

    public IReadOnlyDictionary<string, JsonElement> Metadata { get; init; } =
        new Dictionary<string, JsonElement>();

    public string ToCsv(bool includeHeader = true)
    {
        var builder = new StringBuilder();
        if (includeHeader)
        {
            AppendCsvRow(builder, SingleSymbolHeaders);
        }

        foreach (var bar in Bars.OrderBy(bar => bar.Time))
        {
            AppendCsvRow(builder,
            [
                Symbol,
                FormatValue(bar.Time),
                FormatValue(bar.Open),
                FormatValue(bar.High),
                FormatValue(bar.Low),
                FormatValue(bar.Close),
                FormatValue(bar.AdjustedClose),
                FormatValue(bar.Volume),
                FormatValue(bar.Dividend),
                FormatValue(bar.StockSplit),
                FormatValue(bar.CapitalGain),
                FormatValue(bar.Repaired)
            ]);
        }

        return builder.ToString();
    }

    public static string ToCsv(
        IReadOnlyDictionary<string, HistoricalData> histories,
        HistoryGroupBy groupBy = HistoryGroupBy.Ticker,
        bool includeHeader = true) =>
        ToCsv(histories.Select(pair => string.IsNullOrWhiteSpace(pair.Value.Symbol)
            ? pair.Value with { Symbol = pair.Key }
            : pair.Value), groupBy, includeHeader);

    public static string ToCsv(
        IEnumerable<HistoricalData> histories,
        HistoryGroupBy groupBy = HistoryGroupBy.Ticker,
        bool includeHeader = true)
    {
        var orderedHistories = histories
            .Where(history => !string.IsNullOrWhiteSpace(history.Symbol))
            .OrderBy(history => history.Symbol, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (orderedHistories.Length == 0)
        {
            return includeHeader ? "Time" + Environment.NewLine : string.Empty;
        }

        var fields = groupBy switch
        {
            HistoryGroupBy.Column => ColumnFieldOrder
                .SelectMany(field => orderedHistories.Select(history => $"{field}.{history.Symbol}"))
                .ToArray(),
            _ => orderedHistories
                .SelectMany(history => ColumnFieldOrder.Select(field => $"{history.Symbol}.{field}"))
                .ToArray()
        };

        var barsBySymbol = orderedHistories.ToDictionary(
            history => history.Symbol,
            history => history.Bars.ToDictionary(bar => bar.Time),
            StringComparer.OrdinalIgnoreCase);

        var times = orderedHistories
            .SelectMany(history => history.Bars.Select(bar => bar.Time))
            .Distinct()
            .Order()
            .ToArray();

        var builder = new StringBuilder();
        if (includeHeader)
        {
            AppendCsvRow(builder, new[] { "Time" }.Concat(fields));
        }

        foreach (var time in times)
        {
            var row = new List<string> { FormatValue(time) };
            if (groupBy == HistoryGroupBy.Column)
            {
                foreach (var field in ColumnFieldOrder)
                {
                    foreach (var history in orderedHistories)
                    {
                        row.Add(FormatBarField(GetBar(barsBySymbol, history.Symbol, time), field));
                    }
                }
            }
            else
            {
                foreach (var history in orderedHistories)
                {
                    foreach (var field in ColumnFieldOrder)
                    {
                        row.Add(FormatBarField(GetBar(barsBySymbol, history.Symbol, time), field));
                    }
                }
            }

            AppendCsvRow(builder, row);
        }

        return builder.ToString();
    }

    public static IReadOnlyDictionary<string, IReadOnlyList<PriceBar>> GroupByTicker(
        IEnumerable<HistoricalData> histories) =>
        histories
            .Where(history => !string.IsNullOrWhiteSpace(history.Symbol))
            .OrderBy(history => history.Symbol, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                history => history.Symbol,
                history => (IReadOnlyList<PriceBar>)history.Bars.OrderBy(bar => bar.Time).ToArray(),
                StringComparer.OrdinalIgnoreCase);

    private static readonly string[] SingleSymbolHeaders =
    [
        "Symbol",
        "Time",
        "Open",
        "High",
        "Low",
        "Close",
        "AdjustedClose",
        "Volume",
        "Dividend",
        "StockSplit",
        "CapitalGain",
        "Repaired"
    ];

    private static readonly string[] ColumnFieldOrder =
    [
        "Open",
        "High",
        "Low",
        "Close",
        "AdjustedClose",
        "Volume",
        "Dividend",
        "StockSplit",
        "CapitalGain",
        "Repaired"
    ];

    private static PriceBar? GetBar(
        IReadOnlyDictionary<string, Dictionary<DateTimeOffset, PriceBar>> barsBySymbol,
        string symbol,
        DateTimeOffset time) =>
        barsBySymbol.TryGetValue(symbol, out var bars) && bars.TryGetValue(time, out var bar)
            ? bar
            : null;

    private static string FormatBarField(PriceBar? bar, string field)
    {
        if (bar is null)
        {
            return string.Empty;
        }

        return field switch
        {
            "Open" => FormatValue(bar.Open),
            "High" => FormatValue(bar.High),
            "Low" => FormatValue(bar.Low),
            "Close" => FormatValue(bar.Close),
            "AdjustedClose" => FormatValue(bar.AdjustedClose),
            "Volume" => FormatValue(bar.Volume),
            "Dividend" => FormatValue(bar.Dividend),
            "StockSplit" => FormatValue(bar.StockSplit),
            "CapitalGain" => FormatValue(bar.CapitalGain),
            "Repaired" => FormatValue(bar.Repaired),
            _ => string.Empty
        };
    }

    private static string FormatValue(DateTimeOffset value) => value.ToString("O", CultureInfo.InvariantCulture);

    private static string FormatValue(decimal? value) =>
        value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    private static string FormatValue(long? value) =>
        value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    private static string FormatValue(bool value) => value ? "true" : "false";

    private static void AppendCsvRow(StringBuilder builder, IEnumerable<string> fields)
    {
        var first = true;
        foreach (var field in fields)
        {
            if (!first)
            {
                builder.Append(',');
            }

            builder.Append(EscapeCsvField(field));
            first = false;
        }

        builder.AppendLine();
    }

    private static string EscapeCsvField(string field)
    {
        if (!field.Contains(',') && !field.Contains('"') && !field.Contains('\n') && !field.Contains('\r'))
        {
            return field;
        }

        return "\"" + field.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }
}
