using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using YFSharp.Models;

namespace YFSharp.Internal;

internal static class YahooQuoteSummaryJson
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    public static T? DeserializeModule<T>(JsonElement element)
        where T : class
    {
        var model = element.Deserialize<T>(Options);
        if (model is null)
        {
            return null;
        }

        return model switch
        {
            AssetProfile value => value with { Raw = element.Clone() } as T,
            SummaryDetail value => value with { Raw = element.Clone() } as T,
            FundProfile value => value with { Raw = element.Clone() } as T,
            TopHoldings value => value with { Raw = element.Clone() } as T,
            DefaultKeyStatistics value => value with { Raw = element.Clone() } as T,
            FinancialData value => value with { Raw = element.Clone() } as T,
            CalendarEvents value => value with { Raw = element.Clone() } as T,
            SecFilings value => value with { Raw = element.Clone() } as T,
            RecommendationTrend value => value with { Raw = element.Clone() } as T,
            UpgradeDowngradeHistory value => value with { Raw = element.Clone() } as T,
            EsgScores value => value with { Raw = element.Clone() } as T,
            IncomeStatementModule value => value with { Raw = element.Clone() } as T,
            BalanceSheetModule value => value with { Raw = element.Clone() } as T,
            CashFlowStatementModule value => value with { Raw = element.Clone() } as T,
            EarningsHistoryModule value => value with { Raw = element.Clone() } as T,
            EarningsTrendModule value => value with { Raw = element.Clone() } as T,
            OwnershipModule value => value with { Raw = element.Clone() } as T,
            MajorHoldersBreakdown value => value with { Raw = element.Clone() } as T,
            NetSharePurchaseActivity value => value with { Raw = element.Clone() } as T,
            InsiderTransactionsModule value => value with { Raw = element.Clone() } as T,
            InsiderHoldersModule value => value with { Raw = element.Clone() } as T,
            _ => model
        };
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(YahooJson.SerializerOptions);
        options.Converters.Add(new YahooNullableDecimalConverter());
        options.Converters.Add(new YahooNullableLongConverter());
        options.Converters.Add(new YahooNullableIntConverter());
        options.Converters.Add(new YahooNullableDateTimeOffsetConverter());
        options.Converters.Add(new YahooNullableBoolConverter());
        options.Converters.Add(new YahooStringConverter());
        return options;
    }

    private static JsonElement Unwrap(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return element;
        }

        if (element.TryGetProperty("raw", out var raw))
        {
            return raw;
        }

        return element.TryGetProperty("fmt", out var formatted)
            ? formatted
            : element;
    }

    private sealed class YahooNullableDecimalConverter : JsonConverter<decimal?>
    {
        public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            return YahooJson.GetDecimal(Unwrap(document.RootElement));
        }

        public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteNumberValue(value.Value);
        }
    }

    private sealed class YahooNullableLongConverter : JsonConverter<long?>
    {
        public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            return YahooJson.GetInt64(Unwrap(document.RootElement));
        }

        public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteNumberValue(value.Value);
        }
    }

    private sealed class YahooNullableIntConverter : JsonConverter<int?>
    {
        public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            var value = YahooJson.GetInt64(Unwrap(document.RootElement));
            return value is >= int.MinValue and <= int.MaxValue ? (int)value.Value : null;
        }

        public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteNumberValue(value.Value);
        }
    }

    private sealed class YahooNullableDateTimeOffsetConverter : JsonConverter<DateTimeOffset?>
    {
        public override DateTimeOffset? Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            var element = Unwrap(document.RootElement);
            if (element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                return null;
            }

            var unixTime = YahooJson.GetInt64(element);
            if (unixTime is not null)
            {
                return DateTimeOffset.FromUnixTimeSeconds(unixTime.Value);
            }

            if (element.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            var text = element.GetString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedUnixTime))
            {
                return DateTimeOffset.FromUnixTimeSeconds(parsedUnixTime);
            }

            if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
            {
                return parsed;
            }

            if (DateOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                return new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            }

            return null;
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStringValue(value.Value);
        }
    }

    private sealed class YahooNullableBoolConverter : JsonConverter<bool?>
    {
        public override bool? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            var element = Unwrap(document.RootElement);
            return element.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number => YahooJson.GetInt64(element) is { } value ? value != 0 : null,
                JsonValueKind.String when bool.TryParse(element.GetString(), out var parsed) => parsed,
                JsonValueKind.String when element.GetString() == "1" => true,
                JsonValueKind.String when element.GetString() == "0" => false,
                _ => null
            };
        }

        public override void Write(Utf8JsonWriter writer, bool? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteBooleanValue(value.Value);
        }
    }

    private sealed class YahooStringConverter : JsonConverter<string?>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            var element = Unwrap(document.RootElement);
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => element.ToString(),
                _ => null
            };
        }

        public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value);
    }
}
