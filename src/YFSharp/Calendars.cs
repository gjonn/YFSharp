using YFSharp.Models;

namespace YFSharp;

public sealed class Calendars
{
    private readonly IYahooFinanceClient _client;

    internal Calendars(IYahooFinanceClient client, DateOnly? start, DateOnly? end)
    {
        _client = client;
        Start = start;
        End = end;
    }

    public DateOnly? Start { get; }

    public DateOnly? End { get; }

    public Task<CalendarResult<EarningsCalendarRow>> GetEarningsCalendarAsync(
        decimal? marketCap = null,
        bool filterMostActive = true,
        DateOnly? start = null,
        DateOnly? end = null,
        int limit = 12,
        int offset = 0,
        CancellationToken cancellationToken = default) =>
        _client.GetEarningsCalendarAsync(
            start ?? Start,
            end ?? End,
            marketCap,
            filterMostActive,
            limit,
            offset,
            cancellationToken);

    public Task<CalendarResult<IpoCalendarRow>> GetIpoCalendarAsync(
        DateOnly? start = null,
        DateOnly? end = null,
        int limit = 12,
        int offset = 0,
        CancellationToken cancellationToken = default) =>
        _client.GetIpoCalendarAsync(start ?? Start, end ?? End, limit, offset, cancellationToken);

    public Task<CalendarResult<EconomicEventCalendarRow>> GetEconomicEventsCalendarAsync(
        DateOnly? start = null,
        DateOnly? end = null,
        int limit = 12,
        int offset = 0,
        CancellationToken cancellationToken = default) =>
        _client.GetEconomicEventsCalendarAsync(start ?? Start, end ?? End, limit, offset, cancellationToken);

    public Task<CalendarResult<SplitCalendarRow>> GetSplitsCalendarAsync(
        DateOnly? start = null,
        DateOnly? end = null,
        int limit = 12,
        int offset = 0,
        CancellationToken cancellationToken = default) =>
        _client.GetSplitsCalendarAsync(start ?? Start, end ?? End, limit, offset, cancellationToken);
}
