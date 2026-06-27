namespace YFSharp;

public class YahooFinanceException : Exception
{
    public YahooFinanceException(string message)
        : base(message)
    {
    }

    public YahooFinanceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public sealed class YahooFinanceRateLimitException : YahooFinanceException
{
    public YahooFinanceRateLimitException()
        : base("Yahoo Finance returned a rate-limit response.")
    {
    }
}

public sealed class YahooFinanceHttpException : YahooFinanceException
{
    public YahooFinanceHttpException(System.Net.HttpStatusCode statusCode, string? reasonPhrase, string responseBody)
        : base($"Yahoo Finance returned {(int)statusCode} {reasonPhrase}: {responseBody}")
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    public System.Net.HttpStatusCode StatusCode { get; }

    public string ResponseBody { get; }
}
