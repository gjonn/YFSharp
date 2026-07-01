using System.Text.Json;

namespace YFSharp.Models;

/// <summary>
/// Helpers for working with raw Yahoo JSON exposed by YFSharp models.
/// </summary>
public static class YahooJsonConventions
{
    /// <summary>
    /// Returns true when a raw <see cref="JsonElement"/> contains Yahoo data.
    /// </summary>
    public static bool IsDefined(JsonElement element) =>
        element.ValueKind is not JsonValueKind.Undefined;

    /// <summary>
    /// Returns true when a raw <see cref="JsonElement"/> contains an object or array payload.
    /// </summary>
    public static bool HasStructuredValue(JsonElement element) =>
        element.ValueKind is JsonValueKind.Object or JsonValueKind.Array;

    /// <summary>
    /// Tries to get extension data captured by <see cref="System.Text.Json.Serialization.JsonExtensionDataAttribute"/>.
    /// </summary>
    public static bool TryGetAdditionalData(
        IReadOnlyDictionary<string, JsonElement> additionalData,
        string propertyName,
        out JsonElement value)
    {
        ArgumentNullException.ThrowIfNull(additionalData);

        if (string.IsNullOrWhiteSpace(propertyName))
        {
            value = default;
            return false;
        }

        return additionalData.TryGetValue(propertyName, out value);
    }
}
