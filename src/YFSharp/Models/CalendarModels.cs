using System.Text.Json;

namespace YFSharp.Models;

public enum CalendarType
{
    Earnings,
    Ipo,
    EconomicEvent,
    Splits
}

public sealed record CalendarQuery
{
    private static readonly HashSet<string> ValidOperators =
        new(StringComparer.OrdinalIgnoreCase) { "eq", "gt", "gte", "lt", "lte", "gtelt", "and", "or" };

    private readonly List<object?> _operands;

    public CalendarQuery(string @operator, IEnumerable<object?> operands)
    {
        if (!ValidOperators.Contains(@operator))
        {
            throw new ArgumentException($"Unsupported calendar operator '{@operator}'.", nameof(@operator));
        }

        Operator = @operator.ToUpperInvariant();
        _operands = operands.ToList();
        if (_operands.Count == 0)
        {
            throw new ArgumentException("A calendar query needs at least one operand.", nameof(operands));
        }
    }

    public string Operator { get; }

    public IReadOnlyList<object?> Operands => _operands;

    public bool IsEmpty => _operands.Count == 0;

    public void Append(object? operand) => _operands.Add(operand);

    public object ToYahooObject() =>
        new
        {
            @operator = Operator,
            operands = _operands.Select(SerializeOperand).ToArray()
        };

    private static object? SerializeOperand(object? operand) =>
        operand is CalendarQuery query ? query.ToYahooObject() : operand;
}

public sealed record CalendarRequest
{
    public CalendarType Type { get; init; } = CalendarType.Earnings;

    public CalendarQuery Query { get; init; } = default!;

    public int Limit { get; init; } = 12;

    public int Offset { get; init; }

    public bool SortAscending { get; init; }

    public string? SortField { get; init; }

    public IReadOnlyList<string>? IncludeFields { get; init; }
}

public sealed record CalendarResult<TRow>
{
    public CalendarType Type { get; init; }

    public IReadOnlyList<TRow> Rows { get; init; } = [];

    public IReadOnlyList<CalendarColumn> Columns { get; init; } = [];

    public JsonElement Raw { get; init; }
}

public sealed record CalendarColumn
{
    public string Key { get; init; } = string.Empty;

    public string? Label { get; init; }

    public string? Type { get; init; }

    public string? Field { get; init; }

    public JsonElement Raw { get; init; }
}

public sealed record EarningsCalendarRow
{
    public string Symbol { get; init; } = string.Empty;

    public string? CompanyName { get; init; }

    public decimal? MarketCap { get; init; }

    public string? EventName { get; init; }

    public DateTimeOffset? EventStartDate { get; init; }

    public string? Timing { get; init; }

    public decimal? EpsEstimate { get; init; }

    public decimal? ReportedEps { get; init; }

    public decimal? SurprisePercent { get; init; }

    public JsonElement Raw { get; init; }
}

public sealed record IpoCalendarRow
{
    public string Symbol { get; init; } = string.Empty;

    public string? CompanyName { get; init; }

    public string? Exchange { get; init; }

    public DateTimeOffset? FilingDate { get; init; }

    public DateTimeOffset? Date { get; init; }

    public DateTimeOffset? AmendedDate { get; init; }

    public decimal? PriceFrom { get; init; }

    public decimal? PriceTo { get; init; }

    public decimal? OfferPrice { get; init; }

    public string? Currency { get; init; }

    public decimal? Shares { get; init; }

    public string? DealType { get; init; }

    public JsonElement Raw { get; init; }
}

public sealed record EconomicEventCalendarRow
{
    public string Event { get; init; } = string.Empty;

    public string? Region { get; init; }

    public DateTimeOffset? EventTime { get; init; }

    public string? Period { get; init; }

    public decimal? Actual { get; init; }

    public decimal? Expected { get; init; }

    public decimal? Last { get; init; }

    public decimal? Revised { get; init; }

    public JsonElement Raw { get; init; }
}

public sealed record SplitCalendarRow
{
    public string Symbol { get; init; } = string.Empty;

    public string? CompanyName { get; init; }

    public DateTimeOffset? PayableOn { get; init; }

    public bool? Optionable { get; init; }

    public decimal? OldShareWorth { get; init; }

    public decimal? ShareWorth { get; init; }

    public JsonElement Raw { get; init; }
}
