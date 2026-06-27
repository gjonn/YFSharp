using YFSharp.Models;

namespace YFSharp;

public sealed class Industry
{
    private readonly IYahooFinanceClient _client;

    internal Industry(IYahooFinanceClient client, string key, string region)
    {
        _client = client;
        Key = key;
        Region = region;
    }

    public string Key { get; }

    public string Region { get; }

    public Task<IndustryData> DataAsync(CancellationToken cancellationToken = default) =>
        _client.GetIndustryAsync(Key, Region, cancellationToken);

    public async Task<IReadOnlyList<DomainCompany>> TopCompaniesAsync(
        CancellationToken cancellationToken = default)
    {
        var data = await DataAsync(cancellationToken).ConfigureAwait(false);
        return data.TopCompanies;
    }

    public async Task<IReadOnlyList<IndustryPerformingCompany>> TopPerformingCompaniesAsync(
        CancellationToken cancellationToken = default)
    {
        var data = await DataAsync(cancellationToken).ConfigureAwait(false);
        return data.TopPerformingCompanies;
    }

    public async Task<IReadOnlyList<IndustryGrowthCompany>> TopGrowthCompaniesAsync(
        CancellationToken cancellationToken = default)
    {
        var data = await DataAsync(cancellationToken).ConfigureAwait(false);
        return data.TopGrowthCompanies;
    }
}
