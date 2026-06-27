using System.Text.Json;

namespace YFSharp.Models;

public sealed record SectorData
{
    public string Key { get; init; } = string.Empty;

    public string Region { get; init; } = "US";

    public string? Name { get; init; }

    public string? Symbol { get; init; }

    public DomainOverview Overview { get; init; } = new();

    public IReadOnlyList<DomainCompany> TopCompanies { get; init; } = [];

    public IReadOnlyList<JsonElement> ResearchReports { get; init; } = [];

    public IReadOnlyDictionary<string, string?> TopEtfs { get; init; } =
        new Dictionary<string, string?>();

    public IReadOnlyDictionary<string, string?> TopMutualFunds { get; init; } =
        new Dictionary<string, string?>();

    public IReadOnlyList<IndustryReference> Industries { get; init; } = [];

    public JsonElement Raw { get; init; }
}

public sealed record IndustryData
{
    public string Key { get; init; } = string.Empty;

    public string Region { get; init; } = "US";

    public string? Name { get; init; }

    public string? Symbol { get; init; }

    public string? SectorKey { get; init; }

    public string? SectorName { get; init; }

    public DomainOverview Overview { get; init; } = new();

    public IReadOnlyList<DomainCompany> TopCompanies { get; init; } = [];

    public IReadOnlyList<JsonElement> ResearchReports { get; init; } = [];

    public IReadOnlyList<IndustryPerformingCompany> TopPerformingCompanies { get; init; } = [];

    public IReadOnlyList<IndustryGrowthCompany> TopGrowthCompanies { get; init; } = [];

    public JsonElement Raw { get; init; }
}

public sealed record DomainOverview
{
    public long? CompaniesCount { get; init; }

    public decimal? MarketCap { get; init; }

    public string? MessageBoardId { get; init; }

    public string? Description { get; init; }

    public long? IndustriesCount { get; init; }

    public decimal? MarketWeight { get; init; }

    public long? EmployeeCount { get; init; }

    public JsonElement Raw { get; init; }
}

public sealed record DomainCompany
{
    public string Symbol { get; init; } = string.Empty;

    public string? Name { get; init; }

    public string? Rating { get; init; }

    public decimal? MarketWeight { get; init; }

    public JsonElement Raw { get; init; }
}

public sealed record IndustryReference
{
    public string Key { get; init; } = string.Empty;

    public string? Name { get; init; }

    public string? Symbol { get; init; }

    public decimal? MarketWeight { get; init; }

    public JsonElement Raw { get; init; }
}

public sealed record IndustryPerformingCompany
{
    public string Symbol { get; init; } = string.Empty;

    public string? Name { get; init; }

    public decimal? YtdReturn { get; init; }

    public decimal? LastPrice { get; init; }

    public decimal? TargetPrice { get; init; }

    public JsonElement Raw { get; init; }
}

public sealed record IndustryGrowthCompany
{
    public string Symbol { get; init; } = string.Empty;

    public string? Name { get; init; }

    public decimal? YtdReturn { get; init; }

    public decimal? GrowthEstimate { get; init; }

    public JsonElement Raw { get; init; }
}
