using YFSharp.Models;

namespace YFSharp;

public sealed class Sector
{
    private readonly IYahooFinanceClient _client;

    internal Sector(IYahooFinanceClient client, string key, string region)
    {
        _client = client;
        Key = key;
        Region = region;
    }

    public string Key { get; }

    public string Region { get; }

    public Task<SectorData> DataAsync(CancellationToken cancellationToken = default) =>
        _client.GetSectorAsync(Key, Region, cancellationToken);

    public async Task<IReadOnlyDictionary<string, string?>> TopEtfsAsync(
        CancellationToken cancellationToken = default)
    {
        var data = await DataAsync(cancellationToken).ConfigureAwait(false);
        return data.TopEtfs;
    }

    public async Task<IReadOnlyDictionary<string, string?>> TopMutualFundsAsync(
        CancellationToken cancellationToken = default)
    {
        var data = await DataAsync(cancellationToken).ConfigureAwait(false);
        return data.TopMutualFunds;
    }

    public async Task<IReadOnlyList<IndustryReference>> IndustriesAsync(
        CancellationToken cancellationToken = default)
    {
        var data = await DataAsync(cancellationToken).ConfigureAwait(false);
        return data.Industries;
    }

    public async Task<IReadOnlyList<DomainCompany>> TopCompaniesAsync(
        CancellationToken cancellationToken = default)
    {
        var data = await DataAsync(cancellationToken).ConfigureAwait(false);
        return data.TopCompanies;
    }
}
