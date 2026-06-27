namespace YFSharp;

public sealed record SearchOptions
{
    public int QuotesCount { get; init; } = 8;

    public int NewsCount { get; init; } = 8;

    public int ListsCount { get; init; } = 8;

    public bool IncludeCompanyBreakdown { get; init; } = true;

    public bool IncludeNavLinks { get; init; }

    public bool IncludeResearchReports { get; init; }

    public bool IncludeCulturalAssets { get; init; }

    public bool EnableFuzzyQuery { get; init; }

    public int RecommendedCount { get; init; } = 8;
}
