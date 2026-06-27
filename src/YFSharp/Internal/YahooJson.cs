using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace YFSharp.Internal;

internal static class YahooJson
{
    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        PropertyNameCaseInsensitive = true
    };

    public static string? GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind is JsonValueKind.String
            ? property.GetString()
            : null;
    }

    public static decimal? GetDecimal(JsonElement element)
    {
        if (element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        if (element.ValueKind is JsonValueKind.Object)
        {
            if (element.TryGetProperty("raw", out var raw))
            {
                return GetDecimal(raw);
            }

            if (element.TryGetProperty("fmt", out var formatted))
            {
                return GetDecimal(formatted);
            }
        }

        if (element.ValueKind is JsonValueKind.Number)
        {
            if (element.TryGetDecimal(out var value))
            {
                return value;
            }

            if (element.TryGetDouble(out var doubleValue))
            {
                return (decimal)doubleValue;
            }
        }

        if (element.ValueKind is JsonValueKind.String
            && decimal.TryParse(element.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    public static long? GetInt64(JsonElement element)
    {
        if (element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        if (element.ValueKind is JsonValueKind.Object && element.TryGetProperty("raw", out var raw))
        {
            return GetInt64(raw);
        }

        if (element.ValueKind is JsonValueKind.Number)
        {
            if (element.TryGetInt64(out var value))
            {
                return value;
            }

            if (element.TryGetDouble(out var doubleValue))
            {
                return Convert.ToInt64(doubleValue, CultureInfo.InvariantCulture);
            }
        }

        if (element.ValueKind is JsonValueKind.String
            && long.TryParse(element.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    public static decimal? GetArrayDecimal(JsonElement array, int index)
    {
        return TryGetArrayItem(array, index, out var item) ? GetDecimal(item) : null;
    }

    public static long? GetArrayInt64(JsonElement array, int index)
    {
        return TryGetArrayItem(array, index, out var item) ? GetInt64(item) : null;
    }

    public static bool TryGetArrayItem(JsonElement array, int index, out JsonElement item)
    {
        item = default;

        if (array.ValueKind != JsonValueKind.Array || index < 0 || index >= array.GetArrayLength())
        {
            return false;
        }

        item = array[index];
        return true;
    }
}
