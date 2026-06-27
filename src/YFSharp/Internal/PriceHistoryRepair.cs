using YFSharp.Models;

namespace YFSharp.Internal;

internal static class PriceHistoryRepair
{
    private static readonly TimeSpan DuplicateDividendWindow = TimeSpan.FromDays(7);

    public static IReadOnlyList<PriceBar> Repair(IReadOnlyList<PriceBar> bars)
    {
        if (bars.Count == 0)
        {
            return bars;
        }

        var repaired = bars.OrderBy(bar => bar.Time).ToArray();

        RepairDividendAmounts(repaired);
        RepairMissingDividendAdjustments(repaired);
        RepairCurrencyUnitMixups(repaired);
        RepairBadSplits(repaired);

        return repaired;
    }

    private static void RepairDividendAmounts(PriceBar[] bars)
    {
        PriceBar? previousDividendBar = null;
        for (var i = 0; i < bars.Length; i++)
        {
            var bar = bars[i];
            if (!bar.Dividend.HasValue || bar.Dividend.Value <= 0)
            {
                continue;
            }

            var dividend = bar.Dividend.Value;
            var previousBar = i > 0 ? bars[i - 1] : null;
            var correctedDividend = CorrectDividendUnit(dividend, previousBar?.Close, bar.Close);
            if (correctedDividend != dividend)
            {
                bar = bar with { Dividend = correctedDividend, Repaired = true };
                bars[i] = bar;
                dividend = correctedDividend;
            }

            if (previousDividendBar?.Dividend is { } previousDividend
                && bar.Time - previousDividendBar.Time <= DuplicateDividendWindow
                && AreClose(dividend, previousDividend, 0.001m))
            {
                bars[i] = bar with { Dividend = null, Repaired = true };
                continue;
            }

            previousDividendBar = bars[i];
        }
    }

    private static decimal CorrectDividendUnit(decimal dividend, decimal? previousClose, decimal? close)
    {
        if (previousClose.HasValue && previousClose.Value > 0 && close.HasValue && close.Value > 0)
        {
            var drop = previousClose.Value - close.Value;
            if (drop > 0)
            {
                if (IsNearRatio(dividend / drop, 100m))
                {
                    return dividend / 100m;
                }

                if (IsNearRatio(drop / dividend, 100m))
                {
                    return dividend * 100m;
                }
            }
        }

        if (close.HasValue && close.Value > 0 && dividend / close.Value > 0.25m && dividend / 100m / close.Value < 0.10m)
        {
            return dividend / 100m;
        }

        return dividend;
    }

    private static void RepairMissingDividendAdjustments(PriceBar[] bars)
    {
        for (var i = 1; i < bars.Length; i++)
        {
            var bar = bars[i];
            var previous = bars[i - 1];
            if (bar.Dividend is not { } dividend
                || dividend <= 0
                || previous.Close is not { } previousClose
                || previousClose <= dividend
                || previous.AdjustedClose is null
                || !AreClose(previous.AdjustedClose.Value, previousClose, 0.0005m))
            {
                continue;
            }

            var factor = (previousClose - dividend) / previousClose;
            if (factor <= 0 || factor >= 1)
            {
                continue;
            }

            for (var j = 0; j < i; j++)
            {
                var target = bars[j];
                var adjustedClose = target.AdjustedClose ?? target.Close;
                if (adjustedClose is null)
                {
                    continue;
                }

                bars[j] = target with
                {
                    AdjustedClose = adjustedClose.Value * factor,
                    Repaired = true
                };
            }
        }
    }

    private static void RepairCurrencyUnitMixups(PriceBar[] bars)
    {
        var closes = bars.Select(bar => bar.Close).ToArray();
        var repairFactors = new decimal?[bars.Length];

        for (var i = 0; i < bars.Length; i++)
        {
            repairFactors[i] = GetCurrencyUnitRepairFactor(closes, bars, i);
        }

        for (var i = 0; i < bars.Length; i++)
        {
            if (repairFactors[i] is { } factor)
            {
                bars[i] = ScalePriceFields(bars[i], factor);
            }
        }
    }

    private static decimal? GetCurrencyUnitRepairFactor(decimal?[] closes, PriceBar[] bars, int index)
    {
        var close = closes[index];
        if (!close.HasValue || close.Value <= 0 || HasNearbySplit(bars, index))
        {
            return null;
        }

        var previous = index > 0 ? closes[index - 1] : null;
        var next = index + 1 < closes.Length ? closes[index + 1] : null;
        if (!previous.HasValue || previous.Value <= 0 || !next.HasValue || next.Value <= 0)
        {
            return null;
        }

        var previousRatio = close.Value / previous.Value;
        var nextRatio = close.Value / next.Value;
        if (IsNearRatio(previousRatio, 100m) && IsNearRatio(nextRatio, 100m))
        {
            return 0.01m;
        }

        if (IsNearRatio(previousRatio, 0.01m) && IsNearRatio(nextRatio, 0.01m))
        {
            return 100m;
        }

        return null;
    }

    private static void RepairBadSplits(PriceBar[] bars)
    {
        for (var splitIndex = 1; splitIndex < bars.Length; splitIndex++)
        {
            if (bars[splitIndex].StockSplit is not { } splitFactor
                || splitFactor <= 0
                || AreClose(splitFactor, 1m, 0.0005m)
                || bars[splitIndex - 1].Close is not { } previousClose
                || bars[splitIndex].Close is not { } splitClose
                || splitClose <= 0)
            {
                continue;
            }

            var closeRatio = previousClose / splitClose;
            if (!IsNearRatio(closeRatio, splitFactor))
            {
                continue;
            }

            var factor = 1m / splitFactor;
            for (var i = 0; i < splitIndex; i++)
            {
                bars[i] = ScalePriceFields(bars[i], factor);
            }
        }
    }

    private static PriceBar ScalePriceFields(PriceBar bar, decimal factor) => bar with
    {
        Open = Scale(bar.Open, factor),
        High = Scale(bar.High, factor),
        Low = Scale(bar.Low, factor),
        Close = Scale(bar.Close, factor),
        AdjustedClose = Scale(bar.AdjustedClose, factor),
        Repaired = true
    };

    private static decimal? Scale(decimal? value, decimal factor) => value is null ? null : value.Value * factor;

    private static bool HasNearbySplit(PriceBar[] bars, int index)
    {
        for (var i = Math.Max(0, index - 1); i <= Math.Min(bars.Length - 1, index + 1); i++)
        {
            if (bars[i].StockSplit is { } split
                && split > 0
                && !AreClose(split, 1m, 0.0005m))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsNearRatio(decimal value, decimal target) => AreClose(value, target, 0.20m);

    private static bool AreClose(decimal value, decimal target, decimal relativeTolerance)
    {
        if (target == 0)
        {
            return Math.Abs(value) <= relativeTolerance;
        }

        return Math.Abs(value - target) / Math.Abs(target) <= relativeTolerance;
    }
}
