using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Agilis.Ads;

/// <summary>Options for <see cref="AgilisClient"/>.</summary>
public sealed class AgilisClientOptions
{
    /// <summary>The default base URL, <c>https://ads.agilis.dating</c>.</summary>
    public static readonly Uri DefaultBaseUrl = new Uri("https://ads.agilis.dating");

    /// <summary>
    /// The API key. When <c>null</c>, the client reads the <c>AGILIS_API_KEY</c> environment variable.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>The API's base URL. Defaults to <see cref="DefaultBaseUrl"/>.</summary>
    public Uri BaseUrl { get; set; } = DefaultBaseUrl;

    /// <summary>How long one attempt may take before it is treated as timed out. Defaults to 60 seconds.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>The maximum number of retries after the first attempt. Defaults to 2.</summary>
    public int MaxRetries { get; set; } = 2;

    /// <summary>
    /// The <see cref="System.Net.Http.HttpClient"/> to send requests with, for example one from
    /// <c>IHttpClientFactory</c>. When <c>null</c>, the client creates and owns its own.
    /// A supplied client is never disposed by <see cref="AgilisClient"/>.
    /// </summary>
    public HttpClient? HttpClient { get; set; }

    /// <summary>Replaces the wait between retries (used by tests).</summary>
    internal Func<TimeSpan, CancellationToken, Task>? Delay { get; set; }

    internal AgilisClientOptions Clone() => (AgilisClientOptions)MemberwiseClone();
}
