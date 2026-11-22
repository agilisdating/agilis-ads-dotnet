using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Agilis.Ads.Internal;

namespace Agilis.Ads;

/// <summary>A webhook endpoint.</summary>
public sealed class WebhookEndpoint : AgilisResponse
{
    /// <summary>The endpoint's id.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>The HTTPS URL events are sent to.</summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>Your description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>The event types it receives (<c>*</c> for every type).</summary>
    [JsonPropertyName("enabled_events")]
    public List<string>? EnabledEvents { get; set; }

    /// <summary><c>enabled</c> or <c>disabled</c>.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Whether it is a LIVE endpoint.</summary>
    [JsonPropertyName("livemode")]
    public bool? Livemode { get; set; }

    /// <summary>When it was created.</summary>
    [JsonPropertyName("created")]
    public DateTimeOffset? Created { get; set; }

    /// <summary>The last successful delivery.</summary>
    [JsonPropertyName("last_success_at")]
    public DateTimeOffset? LastSuccessAt { get; set; }

    /// <summary>When deliveries started failing, if they are.</summary>
    [JsonPropertyName("failing_since")]
    public DateTimeOffset? FailingSince { get; set; }

    /// <summary>Who disabled it, if anyone.</summary>
    [JsonPropertyName("disabled_by")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? DisabledBy { get; set; }

    /// <summary>Why it was disabled.</summary>
    [JsonPropertyName("disabled_reason")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? DisabledReason { get; set; }

    /// <summary>When the previous secret stops working, after a roll.</summary>
    [JsonPropertyName("previous_secret_expires_at")]
    public DateTimeOffset? PreviousSecretExpiresAt { get; set; }

    /// <summary>The signing secret: only in the responses of create and roll-secret, shown once.</summary>
    [JsonPropertyName("secret")]
    public string? Secret { get; set; }
}

/// <summary>The response of <c>GET /webhook-endpoints</c>.</summary>
public sealed class WebhookEndpointList : AgilisResponse
{
    /// <summary>The endpoints.</summary>
    [JsonPropertyName("data")]
    public List<WebhookEndpoint> Data { get; set; } = new List<WebhookEndpoint>();
}

/// <summary>Parameters of <c>POST /webhook-endpoints</c>.</summary>
public sealed class WebhookEndpointCreateParams
{
    /// <summary><c>url</c>: an HTTPS URL.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary><c>enabled_events</c>: values from <see cref="WebhookEventTypes"/>.</summary>
    public IList<string> EnabledEvents { get; set; } = new List<string>();

    /// <summary><c>description</c>.</summary>
    public string? Description { get; set; }
}

/// <summary>Parameters of <c>PATCH /webhook-endpoints/{endpoint_id}</c>. Send only the fields to change.</summary>
public sealed class WebhookEndpointUpdateParams
{
    /// <summary><c>url</c>.</summary>
    public string? Url { get; set; }

    /// <summary><c>enabled_events</c>.</summary>
    public IList<string>? EnabledEvents { get; set; }

    /// <summary><c>description</c>.</summary>
    public string? Description { get; set; }

    /// <summary><c>status</c>: <c>enabled</c> or <c>disabled</c>.</summary>
    public string? Status { get; set; }
}

/// <summary>Parameters of <c>POST /webhook-endpoints/{endpoint_id}/roll-secret</c>.</summary>
public sealed class WebhookSecretRollParams
{
    /// <summary><c>expires_in</c>: a value from <see cref="SecretExpiry"/>.</summary>
    public string? ExpiresIn { get; set; }
}

/// <summary>Parameters of <c>POST /webhook-endpoints/{endpoint_id}/test</c>.</summary>
public sealed class WebhookTestParams
{
    /// <summary><c>type</c>: the event type to send.</summary>
    public string? Type { get; set; }
}

/// <summary>The response of <c>DELETE /webhook-endpoints/{endpoint_id}</c>.</summary>
public sealed class WebhookEndpointDeleted : AgilisResponse
{
    /// <summary>The endpoint's id.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>Whether it was deleted.</summary>
    [JsonPropertyName("deleted")]
    public bool? Deleted { get; set; }
}

/// <summary>A delivery of a webhook event to one endpoint.</summary>
public sealed class WebhookDelivery : AgilisObject
{
    /// <summary>The endpoint's id.</summary>
    [JsonPropertyName("endpoint_id")]
    public string? EndpointId { get; set; }

    /// <summary>The delivery's status.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Attempts so far.</summary>
    [JsonPropertyName("attempts")]
    public int? Attempts { get; set; }

    /// <summary>The HTTP status of the last attempt.</summary>
    [JsonPropertyName("last_http_status")]
    public int? LastHttpStatus { get; set; }

    /// <summary>The last error.</summary>
    [JsonPropertyName("last_error")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? LastError { get; set; }

    /// <summary>When the next attempt is due.</summary>
    [JsonPropertyName("next_attempt_at")]
    public DateTimeOffset? NextAttemptAt { get; set; }

    /// <summary>When it was delivered.</summary>
    [JsonPropertyName("delivered_at")]
    public DateTimeOffset? DeliveredAt { get; set; }
}

/// <summary>The response of <c>POST /webhook-endpoints/{endpoint_id}/test</c>.</summary>
public sealed class WebhookTestResult : AgilisResponse
{
    /// <summary>The test event's id.</summary>
    [JsonPropertyName("webhook_event_id")]
    public string? WebhookEventId { get; set; }

    /// <summary>The event type sent.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>Always <c>true</c>.</summary>
    [JsonPropertyName("test")]
    public bool? Test { get; set; }

    /// <summary>The delivery.</summary>
    [JsonPropertyName("delivery")]
    public WebhookDelivery? Delivery { get; set; }
}

/// <summary>Parameters of <c>GET /webhook-events</c>.</summary>
public sealed class WebhookEventListParams
{
    /// <summary><c>type</c>: one event type.</summary>
    public string? Type { get; set; }

    /// <summary><c>delivery_success</c>.</summary>
    public bool? DeliverySuccess { get; set; }

    /// <summary><c>limit</c>: page size.</summary>
    public int? Limit { get; set; }

    /// <summary><c>cursor</c>: the previous page's <c>next_cursor</c>.</summary>
    public string? Cursor { get; set; }

    internal WebhookEventListParams Copy() => (WebhookEventListParams)MemberwiseClone();
}

/// <summary>Parameters of <c>POST /webhook-events/{webhook_event_id}/resend</c>.</summary>
public sealed class WebhookEventResendParams
{
    /// <summary><c>endpoint_id</c>: resend to this endpoint only.</summary>
    public string? EndpointId { get; set; }
}

/// <summary>The response of <c>POST /webhook-events/{webhook_event_id}/resend</c>.</summary>
public sealed class WebhookResendResult : AgilisResponse
{
    /// <summary>The event's id.</summary>
    [JsonPropertyName("webhook_event_id")]
    public string? WebhookEventId { get; set; }

    /// <summary>The deliveries queued.</summary>
    [JsonPropertyName("deliveries")]
    public List<WebhookDelivery>? Deliveries { get; set; }
}

/// <summary>What caused a webhook event.</summary>
public sealed class WebhookEventRequest : AgilisObject
{
    /// <summary><c>api</c>, <c>mcp</c>, <c>ad_portal</c>, <c>agilis</c> or <c>system</c>.</summary>
    [JsonPropertyName("source")]
    public string? Source { get; set; }

    /// <summary>The Idempotency-Key of the request that caused it, if any.</summary>
    [JsonPropertyName("idempotency_key")]
    public string? IdempotencyKey { get; set; }
}

/// <summary>The <c>data</c> of a webhook event: <c>object</c> plus any extra facts.</summary>
public sealed class WebhookEventData : AgilisObject
{
    /// <summary>The Offer, Event or invoice, as the API returns it elsewhere.</summary>
    [JsonPropertyName("object")]
    public JsonElement? Object { get; set; }
}

/// <summary>
/// A webhook event: what <see cref="Webhooks.ConstructEvent(string, string, string, int)"/> returns,
/// and an item of <c>GET /webhook-events</c>.
/// </summary>
public sealed class WebhookEvent : AgilisResponse
{
    /// <summary>The event's id (<c>whevt_…</c>). Use it to ignore repeats.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>Always <c>webhook_event</c>.</summary>
    [JsonPropertyName("object")]
    public string? Object { get; set; }

    /// <summary>The event type, for example <c>event.published</c>.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>When it happened, in Unix seconds. Use it to order events.</summary>
    [JsonPropertyName("created")]
    public long? Created { get; set; }

    /// <summary>Whether it is a LIVE event.</summary>
    [JsonPropertyName("livemode")]
    public bool? Livemode { get; set; }

    /// <summary>The payload version.</summary>
    [JsonPropertyName("version")]
    public int? Version { get; set; }

    /// <summary>Whether it was sent as a test.</summary>
    [JsonPropertyName("test")]
    public bool? Test { get; set; }

    /// <summary>The payload.</summary>
    [JsonPropertyName("data")]
    public WebhookEventData? Data { get; set; }

    /// <summary>What caused it.</summary>
    [JsonPropertyName("request")]
    public WebhookEventRequest? Request { get; set; }

    /// <summary>Its deliveries (from <c>GET /webhook-events</c> only).</summary>
    [JsonPropertyName("deliveries")]
    public List<WebhookDelivery>? Deliveries { get; set; }

    /// <summary><see cref="Created"/> as a date.</summary>
    [JsonIgnore]
    public DateTimeOffset? CreatedAt => Created.HasValue ? DateTimeOffset.FromUnixTimeSeconds(Created.Value) : null;

    /// <summary><c>data.object</c> as an <see cref="Offer"/>, for <c>offer.*</c> types; otherwise <c>null</c>.</summary>
    public Offer? GetOffer() => IsType("offer.") ? GetDataObject<Offer>() : null;

    /// <summary><c>data.object</c> as an <see cref="AgilisEvent"/>, for <c>event.*</c> types; otherwise <c>null</c>.</summary>
    public AgilisEvent? GetEvent() => IsType("event.") ? GetDataObject<AgilisEvent>() : null;

    /// <summary><c>data.object</c> as any type, or <c>null</c> when absent.</summary>
    /// <typeparam name="T">The type to read it as.</typeparam>
    public T? GetDataObject<T>()
        where T : class
    {
        var obj = Data?.Object;
        if (obj is null || obj.Value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return JsonSerializer.Deserialize<T>(obj.Value.GetRawText(), Json.Options);
    }

    private bool IsType(string prefix) => Type != null && Type.StartsWith(prefix, StringComparison.Ordinal);
}
