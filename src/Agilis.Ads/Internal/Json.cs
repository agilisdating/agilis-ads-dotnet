using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Agilis.Ads.Internal;

internal static class Json
{
    public static readonly JsonSerializerOptions Options = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            PropertyNameCaseInsensitive = false,
        };
        options.Converters.Add(new LenientDateTimeOffsetConverter());
        options.Converters.Add(new GeoLocationConverter());
        return options;
    }

    public static string FormatDate(DateTimeOffset value) =>
        value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

    public static string FormatNumber(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    public static string FormatNumber(decimal value) => value.ToString(CultureInfo.InvariantCulture);

    public static string FormatNumber(int value) => value.ToString(CultureInfo.InvariantCulture);
}

/// <summary>Writes UTC ISO 8601 with <c>Z</c>; reads ISO 8601 and, failing that, gives <c>null</c> rather than throwing.</summary>
internal sealed class LenientDateTimeOffsetConverter : JsonConverter<DateTimeOffset?>
{
    public override bool HandleNull => true;

    public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                var text = reader.GetString();
                if (!string.IsNullOrEmpty(text) && DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
                {
                    return parsed;
                }
                return null;
            case JsonTokenType.Number:
                return reader.TryGetInt64(out var seconds) ? DateTimeOffset.FromUnixTimeSeconds(seconds) : null;
            default:
                reader.Skip();
                return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStringValue(Json.FormatDate(value.Value));
        }
    }
}

/// <summary>A location goes on the wire as <c>[latitude, longitude]</c>.</summary>
internal sealed class GeoLocationConverter : JsonConverter<GeoLocation>
{
    public override GeoLocation Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var values = JsonSerializer.Deserialize<double[]>(ref reader, options) ?? Array.Empty<double>();
        return values.Length >= 2 ? new GeoLocation(values[0], values[1]) : default;
    }

    public override void Write(Utf8JsonWriter writer, GeoLocation value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.Latitude);
        writer.WriteNumberValue(value.Longitude);
        writer.WriteEndArray();
    }
}

/// <summary>Reads a string, a number or a boolean as text, so a field that changes type does not break parsing.</summary>
internal sealed class LenientStringConverter : JsonConverter<string?>
{
    public override bool HandleNull => true;

    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                return reader.GetString();
            case JsonTokenType.Number:
            case JsonTokenType.True:
            case JsonTokenType.False:
                using (var doc = JsonDocument.ParseValue(ref reader))
                {
                    return doc.RootElement.GetRawText();
                }
            case JsonTokenType.Null:
                return null;
            default:
                using (var doc = JsonDocument.ParseValue(ref reader))
                {
                    return doc.RootElement.GetRawText();
                }
        }
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStringValue(value);
        }
    }
}
