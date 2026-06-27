namespace YFSharp.Tests;

internal sealed class LiveYahooFactAttribute : FactAttribute
{
    public LiveYahooFactAttribute()
    {
        if (!string.Equals(
            Environment.GetEnvironmentVariable("YFSHARP_LIVE_TESTS"),
            "1",
            StringComparison.Ordinal))
        {
            Skip = "Set YFSHARP_LIVE_TESTS=1 to run live Yahoo Finance tests.";
        }
    }
}
