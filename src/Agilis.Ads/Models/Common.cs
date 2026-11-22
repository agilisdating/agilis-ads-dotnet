using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Agilis.Ads;

/// <summary>
/// The base of every object the API returns. Fields the SDK does not know yet are kept in
/// <see cref="ExtensionData"/>, so a new API field never breaks a client.
/// </summary>
public abstract class AgilisObject
{
    /// <summary>Fields in the JSON that this type does not declare, by their API name.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; set; }
}

/// <summary>A top-level API response: its <c>return_code</c>, plus the HTTP status and headers.</summary>
public abstract class AgilisResponse : AgilisObject
{
    /// <summary>The API's <c>return_code</c>: <c>S1</c> or <c>S2</c> on success. Not every endpoint sends one.</summary>
    [JsonPropertyName("return_code")]
    public string? ReturnCode { get; set; }

    /// <summary>The HTTP status and headers of the response this object came from.</summary>
    [JsonIgnore]
    public ResponseInfo? HttpResponse { get; internal set; }
}

/// <summary>The HTTP status and headers of a response.</summary>
public sealed class ResponseInfo
{
    internal static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> EmptyHeaders =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);

    internal ResponseInfo(int statusCode, IReadOnlyDictionary<string, IReadOnlyList<string>> headers)
    {
        StatusCode = statusCode;
        Headers = headers;
    }

    /// <summary>The HTTP status.</summary>
    public int StatusCode { get; }

    /// <summary>The response headers. Names are case-insensitive.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Headers { get; }

    /// <summary>The first value of a header, or <c>null</c>.</summary>
    /// <param name="name">The header name (case-insensitive).</param>
    public string? GetHeader(string name) =>
        Headers.TryGetValue(name, out var values) ? values.FirstOrDefault() : null;

    /// <summary>The <c>X-RateLimit-Limit</c> header: requests allowed in the current minute.</summary>
    public int? RateLimitLimit => ParseInt(GetHeader("X-RateLimit-Limit"));

    /// <summary>The <c>X-RateLimit-Remaining</c> header: requests left in the current minute.</summary>
    public int? RateLimitRemaining => ParseInt(GetHeader("X-RateLimit-Remaining"));

    private static int? ParseInt(string? value) =>
        int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var n) ? n : null;
}

/// <summary>A latitude and longitude, sent to the API as <c>location[]</c>.</summary>
public readonly struct GeoLocation
{
    /// <summary>Creates a location.</summary>
    /// <param name="latitude">Latitude in degrees.</param>
    /// <param name="longitude">Longitude in degrees.</param>
    public GeoLocation(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    /// <summary>Latitude in degrees.</summary>
    public double Latitude { get; }

    /// <summary>Longitude in degrees.</summary>
    public double Longitude { get; }
}

/// <summary>The paging block of a list response.</summary>
public sealed class Pagination : AgilisObject
{
    /// <summary>The page size used.</summary>
    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    /// <summary>How many items this page holds.</summary>
    [JsonPropertyName("count")]
    public int? Count { get; set; }

    /// <summary>Whether there is another page.</summary>
    [JsonPropertyName("has_more")]
    public bool HasMore { get; set; }

    /// <summary>Pass as <c>cursor</c> to get the next page; <c>null</c> on the last page.</summary>
    [JsonPropertyName("next_cursor")]
    public string? NextCursor { get; set; }
}

/// <summary>One page of a list.</summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed class Page<T> : AgilisResponse
{
    /// <summary>The items on this page.</summary>
    [JsonPropertyName("data")]
    public List<T> Data { get; set; } = new List<T>();

    /// <summary>The paging block.</summary>
    [JsonPropertyName("pagination")]
    public Pagination Pagination { get; set; } = new Pagination();

    /// <summary>Whether there is another page.</summary>
    [JsonIgnore]
    public bool HasMore => Pagination.HasMore && !string.IsNullOrEmpty(Pagination.NextCursor);

    /// <summary>The cursor of the next page, or <c>null</c>.</summary>
    [JsonIgnore]
    public string? NextCursor => Pagination.NextCursor;
}

/// <summary>A simple result: a <c>return_code</c> and a message.</summary>
public class ActionResult : AgilisResponse
{
    /// <summary>The API's message.</summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary><c>true</c> when this answered a <c>dry_run</c> request: nothing changed.</summary>
    [JsonPropertyName("dry_run")]
    public bool? DryRun { get; set; }
}
