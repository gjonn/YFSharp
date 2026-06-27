using System.Text.Json;

namespace YFSharp.Models;

public sealed record LookupResult
{
    public string Query { get; init; } = string.Empty;

    public LookupType Type { get; init; }

    public IReadOnlyList<LookupDocument> Documents { get; init; } = [];

    public JsonElement Raw { get; init; }
}

public sealed record LookupDocument
{
    public string Symbol { get; init; } = string.Empty;

    public string? Name { get; init; }

    public string? QuoteType { get; init; }

    public string? Exchange { get; init; }

    public JsonElement Raw { get; init; }
}
