namespace YFSharp.Models;

public sealed record OptionChain
{
    public string Symbol { get; init; } = string.Empty;

    public Quote? Underlying { get; init; }

    public DateOnly? Expiration { get; init; }

    public IReadOnlyList<DateOnly> ExpirationDates { get; init; } = [];

    public IReadOnlyList<OptionContract> Calls { get; init; } = [];

    public IReadOnlyList<OptionContract> Puts { get; init; } = [];
}
