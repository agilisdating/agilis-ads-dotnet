using System;
using Agilis.Ads.Internal;

namespace Agilis.Ads;

/// <summary>
/// The client for the Agilis Advertising REST API. Create one and reuse it: it is safe to use from
/// several threads at once.
/// </summary>
/// <example>
/// <code>
/// using var client = new AgilisClient("pk_test_...");
/// var categories = await client.Categories.ListAsync();
/// </code>
/// </example>
public sealed class AgilisClient : IDisposable
{
    /// <summary>The SDK's version.</summary>
    public const string Version = "0.1.0";

    /// <summary>The environment variable read when no API key is given.</summary>
    public const string ApiKeyEnvironmentVariable = "AGILIS_API_KEY";

    /// <summary>Creates a client with an API key, or with <c>AGILIS_API_KEY</c> when <paramref name="apiKey"/> is <c>null</c>.</summary>
    /// <param name="apiKey">A <c>pk_test_</c> or <c>pk_live_</c> key.</param>
    public AgilisClient(string? apiKey = null)
        : this(new AgilisClientOptions { ApiKey = apiKey })
    {
    }

    /// <summary>Creates a client with options.</summary>
    /// <param name="options">The options. When <see cref="AgilisClientOptions.ApiKey"/> is <c>null</c>, <c>AGILIS_API_KEY</c> is read.</param>
    public AgilisClient(AgilisClientOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var opts = options.Clone();
        var key = opts.ApiKey;
        if (string.IsNullOrWhiteSpace(key))
        {
            key = Environment.GetEnvironmentVariable(ApiKeyEnvironmentVariable);
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException(
                "An API key is required: pass one, or set the " + ApiKeyEnvironmentVariable + " environment variable.",
                nameof(options));
        }

        key = key!.Trim();
        Mode = ModeOf(key);

        if (opts.MaxRetries < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "MaxRetries cannot be negative.");
        }

        Core = new HttpCore(key, opts);
        Categories = new CategoriesResource(this);
        Interests = new InterestsResource(this);
        Offers = new OffersResource(this);
        EventCategories = new EventCategoriesResource(this);
        EventCurrencies = new EventCurrenciesResource(this);
        EventConfig = new EventConfigResource(this);
        Events = new EventsResource(this);
        WebhookEndpoints = new WebhookEndpointsResource(this);
        WebhookEvents = new WebhookEventsResource(this);
    }

    /// <summary>The key's mode: <see cref="AgilisMode.Test"/> or <see cref="AgilisMode.Live"/>.</summary>
    public AgilisMode Mode { get; }

    /// <summary>Whether the key is a LIVE key, so callers can refuse to run against Live.</summary>
    public bool IsLive => Mode == AgilisMode.Live;

    /// <summary>Offer categories.</summary>
    public CategoriesResource Categories { get; }

    /// <summary>Member interests.</summary>
    public InterestsResource Interests { get; }

    /// <summary>Offers.</summary>
    public OffersResource Offers { get; }

    /// <summary>Event categories.</summary>
    public EventCategoriesResource EventCategories { get; }

    /// <summary>Event ticket currencies.</summary>
    public EventCurrenciesResource EventCurrencies { get; }

    /// <summary>Event configuration.</summary>
    public EventConfigResource EventConfig { get; }

    /// <summary>Events.</summary>
    public EventsResource Events { get; }

    /// <summary>Webhook endpoints.</summary>
    public WebhookEndpointsResource WebhookEndpoints { get; }

    /// <summary>Webhook events.</summary>
    public WebhookEventsResource WebhookEvents { get; }

    /// <summary>The <c>User-Agent</c> the client sends.</summary>
    public static string UserAgent => HttpCore.UserAgent;

    internal HttpCore Core { get; }

    internal string EnvironmentUpper => Mode == AgilisMode.Live ? "LIVE" : "TEST";

    internal string EnvironmentLower => Mode == AgilisMode.Live ? "live" : "test";

    /// <summary>The mode of an API key, from its prefix.</summary>
    /// <param name="apiKey">The key.</param>
    /// <exception cref="ArgumentException">The key starts with neither <c>pk_test_</c> nor <c>pk_live_</c>.</exception>
    public static AgilisMode ModeOf(string apiKey)
    {
        if (apiKey != null && apiKey.StartsWith("pk_live_", StringComparison.Ordinal))
        {
            return AgilisMode.Live;
        }

        if (apiKey != null && apiKey.StartsWith("pk_test_", StringComparison.Ordinal))
        {
            return AgilisMode.Test;
        }

        throw new ArgumentException("The API key should start with pk_test_ or pk_live_.", nameof(apiKey));
    }

    /// <summary>Releases the HTTP client, if this client created it.</summary>
    public void Dispose() => Core.Dispose();

    /// <summary>Describes the client without its key.</summary>
    public override string ToString() => "AgilisClient (" + EnvironmentUpper + ")";
}
