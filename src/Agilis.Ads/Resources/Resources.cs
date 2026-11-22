using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Agilis.Ads.Internal;

namespace Agilis.Ads;

/// <summary>Shared plumbing for the resource groups.</summary>
public abstract class ResourceBase
{
    internal ResourceBase(AgilisClient client)
    {
        Client = client;
    }

    internal AgilisClient Client { get; }

    internal Task<T> SendAsync<T>(ApiRequest request, CancellationToken cancellationToken)
        where T : AgilisResponse => Client.Core.SendAsync<T>(request, cancellationToken);

    internal static string Id(int id) => id.ToString(CultureInfo.InvariantCulture);

    internal static string Id(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            throw new ArgumentException("An id is required.", nameof(id));
        }

        return Uri.EscapeDataString(id);
    }

    internal static string NewIdempotencyKey() => Guid.NewGuid().ToString();

    internal static void AddDryRun(ApiRequest request, bool dryRun)
    {
        if (dryRun)
        {
            request.Query.Add("dry_run", "true");
        }
    }
}

/// <summary>Offer categories: <c>GET /categories</c>.</summary>
public sealed class CategoriesResource : ResourceBase
{
    internal CategoriesResource(AgilisClient client) : base(client) { }

    /// <summary>Lists the Offer categories (<c>GET /categories</c>).</summary>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<CategoryList> ListAsync(CancellationToken cancellationToken = default) =>
        SendAsync<CategoryList>(new ApiRequest(HttpMethod.Get, "/categories"), cancellationToken);
}

/// <summary>Member interests: <c>GET /interests</c>.</summary>
public sealed class InterestsResource : ResourceBase
{
    internal InterestsResource(AgilisClient client) : base(client) { }

    /// <summary>Lists the interests Offers can target (<c>GET /interests</c>).</summary>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<InterestList> ListAsync(CancellationToken cancellationToken = default) =>
        SendAsync<InterestList>(new ApiRequest(HttpMethod.Get, "/interests"), cancellationToken);
}

/// <summary>Offers.</summary>
public sealed class OffersResource : ResourceBase
{
    internal OffersResource(AgilisClient client) : base(client) { }

    /// <summary>Estimates what an Offer will cost (<c>POST /offers/price-estimate</c>).</summary>
    /// <param name="parameters">The targeting and dates.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<OfferPriceEstimate> EstimatePriceAsync(OfferPriceEstimateParams parameters, CancellationToken cancellationToken = default)
    {
        if (parameters is null)
        {
            throw new ArgumentNullException(nameof(parameters));
        }

        var request = new ApiRequest(HttpMethod.Post, "/offers/price-estimate")
        {
            Json = new JsonBody()
                .Add("start_datetime", (DateTimeOffset?)parameters.StartDatetime)
                .Add("end_datetime", (DateTimeOffset?)parameters.EndDatetime)
                .Add("location", (GeoLocation?)parameters.Location)
                .Add("radius", parameters.Radius)
                .Add("gender", parameters.Gender)
                .Add("interests", parameters.Interests)
                .Add("environment", parameters.Environment ?? Client.EnvironmentUpper),
        };
        return SendAsync<OfferPriceEstimate>(request, cancellationToken);
    }

    /// <summary>
    /// Creates an Offer (<c>POST /offers</c>, multipart). Sends an <c>Idempotency-Key</c>: yours, or a
    /// random UUID generated once for this call and reused on every retry.
    /// </summary>
    /// <param name="parameters">The Offer.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<OfferCreateResult> CreateAsync(OfferCreateParams parameters, CancellationToken cancellationToken = default)
    {
        if (parameters is null)
        {
            throw new ArgumentNullException(nameof(parameters));
        }

        var form = new MultipartForm()
            .Add("category_id", parameters.CategoryId)
            .Add("title", parameters.Title)
            .Add("description", parameters.Description)
            .Add("terms_and_conditions", parameters.TermsAndConditions)
            .Add("advertised_name", parameters.AdvertisedName)
            .Add("push_notification_title", parameters.PushNotificationTitle)
            .Add("push_notification_message", parameters.PushNotificationMessage)
            .Add("button_preset", parameters.ButtonPreset)
            .Add("button_text", parameters.ButtonText)
            .Add("url", parameters.Url)
            .Add("location", (GeoLocation?)parameters.Location)
            .Add("radius", parameters.Radius)
            .AddArray("gender", parameters.Gender)
            .AddArray("interests", parameters.Interests)
            .Add("start_datetime", (DateTimeOffset?)parameters.StartDatetime)
            .Add("expiry_datetime", (DateTimeOffset?)parameters.ExpiryDatetime)
            .Add("payment_method", parameters.PaymentMethod)
            .Add("environment", parameters.Environment ?? Client.EnvironmentUpper)
            .AddFile("image", parameters.Image);

        var request = new ApiRequest(HttpMethod.Post, "/offers")
        {
            Form = form,
            IdempotencyKey = parameters.IdempotencyKey ?? NewIdempotencyKey(),
        };
        AddDryRun(request, parameters.DryRun);
        return SendAsync<OfferCreateResult>(request, cancellationToken);
    }

    /// <summary>Gets one page of your Offers (<c>GET /offers</c>).</summary>
    /// <param name="parameters">Filters and paging; <c>environment</c> defaults to the key's mode.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<Page<Offer>> ListAsync(OfferListParams? parameters = null, CancellationToken cancellationToken = default)
    {
        var p = parameters ?? new OfferListParams();
        var request = new ApiRequest(HttpMethod.Get, "/offers");
        request.Query
            .Add("environment", p.Environment ?? Client.EnvironmentLower)
            .Add("limit", p.Limit)
            .Add("cursor", p.Cursor)
            .Add("offer_status", p.OfferStatus)
            .Add("created_after", p.CreatedAfter)
            .Add("created_before", p.CreatedBefore);
        return SendAsync<Page<Offer>>(request, cancellationToken);
    }

    /// <summary>Every Offer, following <c>next_cursor</c> page by page until <c>has_more</c> is false.</summary>
    /// <param name="parameters">Filters and page size.</param>
    /// <param name="cancellationToken">Cancels the requests.</param>
    public async IAsyncEnumerable<Offer> ListAutoPagingAsync(OfferListParams? parameters = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var p = parameters?.Copy() ?? new OfferListParams();
        while (true)
        {
            var page = await ListAsync(p, cancellationToken).ConfigureAwait(false);
            foreach (var item in page.Data)
            {
                yield return item;
            }

            if (!page.HasMore)
            {
                yield break;
            }

            p.Cursor = page.NextCursor;
        }
    }

    /// <summary>Pauses an Offer (<c>POST /offers/{offer_id}/pause</c>).</summary>
    /// <param name="offerId">The Offer's id.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<ActionResult> PauseAsync(int offerId, CancellationToken cancellationToken = default) =>
        SendAsync<ActionResult>(new ApiRequest(HttpMethod.Post, "/offers/" + Id(offerId) + "/pause"), cancellationToken);

    /// <summary>Resumes a paused Offer (<c>POST /offers/{offer_id}/resume</c>).</summary>
    /// <param name="offerId">The Offer's id.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<ActionResult> ResumeAsync(int offerId, CancellationToken cancellationToken = default) =>
        SendAsync<ActionResult>(new ApiRequest(HttpMethod.Post, "/offers/" + Id(offerId) + "/resume"), cancellationToken);

    /// <summary>Gets an Offer's views and clicks (<c>GET /offers/{offer_id}/analytics</c>).</summary>
    /// <param name="offerId">The Offer's id.</param>
    /// <param name="interval">A value from <see cref="AnalyticsInterval"/>.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<OfferAnalytics> GetAnalyticsAsync(int offerId, string interval, CancellationToken cancellationToken = default)
    {
        var request = new ApiRequest(HttpMethod.Get, "/offers/" + Id(offerId) + "/analytics");
        request.Query.Add("interval", interval);
        return SendAsync<OfferAnalytics>(request, cancellationToken);
    }

    /// <summary>Deletes an Offer (<c>DELETE /offers/{offer_id}</c>).</summary>
    /// <param name="offerId">The Offer's id.</param>
    /// <param name="dryRun">Sends <c>dry_run=true</c>: shows what deleting would do, deletes nothing.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<OfferDeleteResult> DeleteAsync(int offerId, bool dryRun = false, CancellationToken cancellationToken = default)
    {
        var request = new ApiRequest(HttpMethod.Delete, "/offers/" + Id(offerId));
        AddDryRun(request, dryRun);
        return SendAsync<OfferDeleteResult>(request, cancellationToken);
    }
}

/// <summary>Event categories: <c>GET /event-categories</c>.</summary>
public sealed class EventCategoriesResource : ResourceBase
{
    internal EventCategoriesResource(AgilisClient client) : base(client) { }

    /// <summary>Lists the Event categories (<c>GET /event-categories</c>).</summary>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<EventCategoryList> ListAsync(CancellationToken cancellationToken = default) =>
        SendAsync<EventCategoryList>(new ApiRequest(HttpMethod.Get, "/event-categories"), cancellationToken);
}

/// <summary>Event ticket currencies: <c>GET /event-currencies</c>.</summary>
public sealed class EventCurrenciesResource : ResourceBase
{
    internal EventCurrenciesResource(AgilisClient client) : base(client) { }

    /// <summary>Lists the currencies a ticket price may be quoted in (<c>GET /event-currencies</c>).</summary>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<EventCurrencyList> ListAsync(CancellationToken cancellationToken = default) =>
        SendAsync<EventCurrencyList>(new ApiRequest(HttpMethod.Get, "/event-currencies"), cancellationToken);
}

/// <summary>Event configuration: <c>GET /event-config</c>.</summary>
public sealed class EventConfigResource : ResourceBase
{
    internal EventConfigResource(AgilisClient client) : base(client) { }

    /// <summary>Gets the listing fee, balance, currencies and image limits (<c>GET /event-config</c>).</summary>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<EventConfig> GetAsync(CancellationToken cancellationToken = default) =>
        SendAsync<EventConfig>(new ApiRequest(HttpMethod.Get, "/event-config"), cancellationToken);
}

/// <summary>Events.</summary>
public sealed class EventsResource : ResourceBase
{
    internal EventsResource(AgilisClient client) : base(client) { }

    /// <summary>Shows what publishing would do, without publishing or charging (<c>POST /events/preview</c>, multipart).</summary>
    /// <param name="parameters">The Event's fields; all optional except <c>environment</c>, which defaults to the key's mode.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<EventPreview> PreviewAsync(EventPreviewParams parameters, CancellationToken cancellationToken = default)
    {
        if (parameters is null)
        {
            throw new ArgumentNullException(nameof(parameters));
        }

        var form = new MultipartForm();
        parameters.AddTo(form);
        form.Add("environment", parameters.Environment ?? Client.EnvironmentUpper);
        return SendAsync<EventPreview>(new ApiRequest(HttpMethod.Post, "/events/preview") { Form = form }, cancellationToken);
    }

    /// <summary>
    /// Publishes an Event (<c>POST /events</c>, multipart). Sends an <c>Idempotency-Key</c>: yours, or a
    /// random UUID generated once for this call and reused on every retry.
    /// </summary>
    /// <param name="parameters">The Event.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<EventPublishResult> PublishAsync(EventPublishParams parameters, CancellationToken cancellationToken = default)
    {
        if (parameters is null)
        {
            throw new ArgumentNullException(nameof(parameters));
        }

        var form = new MultipartForm();
        parameters.AddTo(form);
        form.Add("environment", parameters.Environment ?? Client.EnvironmentUpper);
        var request = new ApiRequest(HttpMethod.Post, "/events")
        {
            Form = form,
            IdempotencyKey = parameters.IdempotencyKey ?? NewIdempotencyKey(),
        };
        AddDryRun(request, parameters.DryRun);
        return SendAsync<EventPublishResult>(request, cancellationToken);
    }

    /// <summary>Gets one page of your Events (<c>GET /events</c>).</summary>
    /// <param name="parameters">Filters and paging; <c>environment</c> defaults to the key's mode.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<Page<AgilisEvent>> ListAsync(EventListParams? parameters = null, CancellationToken cancellationToken = default)
    {
        var p = parameters ?? new EventListParams();
        var request = new ApiRequest(HttpMethod.Get, "/events");
        request.Query
            .Add("environment", p.Environment ?? Client.EnvironmentLower)
            .Add("limit", p.Limit)
            .Add("cursor", p.Cursor)
            .Add("event_status", p.EventStatus)
            .Add("owner_user_id", p.OwnerUserId)
            .Add("created_after", p.CreatedAfter)
            .Add("created_before", p.CreatedBefore);
        return SendAsync<Page<AgilisEvent>>(request, cancellationToken);
    }

    /// <summary>Every Event, following <c>next_cursor</c> page by page until <c>has_more</c> is false.</summary>
    /// <param name="parameters">Filters and page size.</param>
    /// <param name="cancellationToken">Cancels the requests.</param>
    public async IAsyncEnumerable<AgilisEvent> ListAutoPagingAsync(EventListParams? parameters = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var p = parameters?.Copy() ?? new EventListParams();
        while (true)
        {
            var page = await ListAsync(p, cancellationToken).ConfigureAwait(false);
            foreach (var item in page.Data)
            {
                yield return item;
            }

            if (!page.HasMore)
            {
                yield break;
            }

            p.Cursor = page.NextCursor;
        }
    }

    /// <summary>Gets one Event with its details and history (<c>GET /events/{event_id}</c>).</summary>
    /// <param name="eventId">The Event's id.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<AgilisEventDetails> GetAsync(int eventId, CancellationToken cancellationToken = default) =>
        SendAsync<AgilisEventDetails>(new ApiRequest(HttpMethod.Get, "/events/" + Id(eventId)), cancellationToken);

    /// <summary>
    /// Changes a published Event, free of charge (<c>PATCH /events/{event_id}</c>, multipart). It is
    /// always sent as a multipart POST with <c>X-HTTP-Method-Override: PATCH</c>, with or without an image.
    /// </summary>
    /// <param name="eventId">The Event's id.</param>
    /// <param name="parameters">Only the fields to change.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<EventEditResult> EditAsync(int eventId, EventEditParams parameters, CancellationToken cancellationToken = default)
    {
        if (parameters is null)
        {
            throw new ArgumentNullException(nameof(parameters));
        }

        var form = new MultipartForm();
        parameters.AddTo(form);

        // Multipart bodies are only parsed on POST, so an edit is always a POST with the override header.
        var request = new ApiRequest(HttpMethod.Post, "/events/" + Id(eventId)) { Form = form };
        request.Headers["X-HTTP-Method-Override"] = "PATCH";
        return SendAsync<EventEditResult>(request, cancellationToken);
    }

    /// <summary>Deletes an Event (<c>DELETE /events/{event_id}</c>).</summary>
    /// <param name="eventId">The Event's id.</param>
    /// <param name="dryRun">Sends <c>dry_run=true</c>: checks it can be deleted, deletes nothing.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<ActionResult> DeleteAsync(int eventId, bool dryRun = false, CancellationToken cancellationToken = default)
    {
        var request = new ApiRequest(HttpMethod.Delete, "/events/" + Id(eventId));
        AddDryRun(request, dryRun);
        return SendAsync<ActionResult>(request, cancellationToken);
    }

    /// <summary>Moves an Event to new dates (<c>POST /events/{event_id}/reschedule</c>).</summary>
    /// <param name="eventId">The Event's id.</param>
    /// <param name="parameters">The new dates.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<EventRescheduleResult> RescheduleAsync(int eventId, EventRescheduleParams parameters, CancellationToken cancellationToken = default)
    {
        if (parameters is null)
        {
            throw new ArgumentNullException(nameof(parameters));
        }

        var request = new ApiRequest(HttpMethod.Post, "/events/" + Id(eventId) + "/reschedule")
        {
            Json = new JsonBody()
                .Add("starts_at", (DateTimeOffset?)parameters.StartsAt)
                .Add("ends_at", (DateTimeOffset?)parameters.EndsAt),
        };
        return SendAsync<EventRescheduleResult>(request, cancellationToken);
    }

    /// <summary>Cancels an Event (<c>POST /events/{event_id}/cancel</c>). Listings are not refunded.</summary>
    /// <param name="eventId">The Event's id.</param>
    /// <param name="dryRun">Sends <c>dry_run=true</c>: checks it can be cancelled, changes nothing.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<EventCancelResult> CancelAsync(int eventId, bool dryRun = false, CancellationToken cancellationToken = default)
    {
        var request = new ApiRequest(HttpMethod.Post, "/events/" + Id(eventId) + "/cancel");
        AddDryRun(request, dryRun);
        return SendAsync<EventCancelResult>(request, cancellationToken);
    }

    /// <summary>Gets an Event's clicks, attendees and invitations (<c>GET /events/{event_id}/analytics</c>).</summary>
    /// <param name="eventId">The Event's id.</param>
    /// <param name="interval">A value from <see cref="AnalyticsInterval"/>.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<EventAnalytics> GetAnalyticsAsync(int eventId, string interval, CancellationToken cancellationToken = default)
    {
        var request = new ApiRequest(HttpMethod.Get, "/events/" + Id(eventId) + "/analytics");
        request.Query.Add("interval", interval);
        return SendAsync<EventAnalytics>(request, cancellationToken);
    }
}

/// <summary>Webhook endpoints.</summary>
public sealed class WebhookEndpointsResource : ResourceBase
{
    internal WebhookEndpointsResource(AgilisClient client) : base(client) { }

    /// <summary>Lists the endpoints of your key's mode (<c>GET /webhook-endpoints</c>).</summary>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<WebhookEndpointList> ListAsync(CancellationToken cancellationToken = default) =>
        SendAsync<WebhookEndpointList>(new ApiRequest(HttpMethod.Get, "/webhook-endpoints"), cancellationToken);

    /// <summary>Adds an endpoint (<c>POST /webhook-endpoints</c>). The response holds the signing secret, shown once.</summary>
    /// <param name="parameters">The endpoint.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<WebhookEndpoint> CreateAsync(WebhookEndpointCreateParams parameters, CancellationToken cancellationToken = default)
    {
        if (parameters is null)
        {
            throw new ArgumentNullException(nameof(parameters));
        }

        var request = new ApiRequest(HttpMethod.Post, "/webhook-endpoints")
        {
            Json = new JsonBody()
                .Add("url", parameters.Url)
                .Add("enabled_events", parameters.EnabledEvents)
                .Add("description", parameters.Description),
        };
        return SendAsync<WebhookEndpoint>(request, cancellationToken);
    }

    /// <summary>Gets an endpoint (<c>GET /webhook-endpoints/{endpoint_id}</c>).</summary>
    /// <param name="endpointId">The endpoint's id.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<WebhookEndpoint> GetAsync(string endpointId, CancellationToken cancellationToken = default) =>
        SendAsync<WebhookEndpoint>(new ApiRequest(HttpMethod.Get, "/webhook-endpoints/" + Id(endpointId)), cancellationToken);

    /// <summary>Changes an endpoint (<c>PATCH /webhook-endpoints/{endpoint_id}</c>).</summary>
    /// <param name="endpointId">The endpoint's id.</param>
    /// <param name="parameters">Only the fields to change.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<WebhookEndpoint> UpdateAsync(string endpointId, WebhookEndpointUpdateParams parameters, CancellationToken cancellationToken = default)
    {
        if (parameters is null)
        {
            throw new ArgumentNullException(nameof(parameters));
        }

        var request = new ApiRequest(HttpCore.Patch, "/webhook-endpoints/" + Id(endpointId))
        {
            Json = new JsonBody()
                .Add("url", parameters.Url)
                .Add("enabled_events", parameters.EnabledEvents)
                .Add("description", parameters.Description)
                .Add("status", parameters.Status),
        };
        return SendAsync<WebhookEndpoint>(request, cancellationToken);
    }

    /// <summary>Deletes an endpoint (<c>DELETE /webhook-endpoints/{endpoint_id}</c>).</summary>
    /// <param name="endpointId">The endpoint's id.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<WebhookEndpointDeleted> DeleteAsync(string endpointId, CancellationToken cancellationToken = default) =>
        SendAsync<WebhookEndpointDeleted>(new ApiRequest(HttpMethod.Delete, "/webhook-endpoints/" + Id(endpointId)), cancellationToken);

    /// <summary>Replaces the signing secret (<c>POST /webhook-endpoints/{endpoint_id}/roll-secret</c>). The new secret is shown once.</summary>
    /// <param name="endpointId">The endpoint's id.</param>
    /// <param name="parameters">How long the old secret keeps working.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<WebhookEndpoint> RollSecretAsync(string endpointId, WebhookSecretRollParams? parameters = null, CancellationToken cancellationToken = default)
    {
        var request = new ApiRequest(HttpMethod.Post, "/webhook-endpoints/" + Id(endpointId) + "/roll-secret")
        {
            Json = new JsonBody().Add("expires_in", parameters?.ExpiresIn),
        };
        return SendAsync<WebhookEndpoint>(request, cancellationToken);
    }

    /// <summary>Sends a test event to an endpoint (<c>POST /webhook-endpoints/{endpoint_id}/test</c>).</summary>
    /// <param name="endpointId">The endpoint's id.</param>
    /// <param name="parameters">The event type to send.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<WebhookTestResult> SendTestAsync(string endpointId, WebhookTestParams? parameters = null, CancellationToken cancellationToken = default)
    {
        var request = new ApiRequest(HttpMethod.Post, "/webhook-endpoints/" + Id(endpointId) + "/test")
        {
            Json = new JsonBody().Add("type", parameters?.Type),
        };
        return SendAsync<WebhookTestResult>(request, cancellationToken);
    }
}

/// <summary>Webhook events sent in the last 30 days.</summary>
public sealed class WebhookEventsResource : ResourceBase
{
    internal WebhookEventsResource(AgilisClient client) : base(client) { }

    /// <summary>Gets one page of webhook events (<c>GET /webhook-events</c>).</summary>
    /// <param name="parameters">Filters and paging.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<Page<WebhookEvent>> ListAsync(WebhookEventListParams? parameters = null, CancellationToken cancellationToken = default)
    {
        var p = parameters ?? new WebhookEventListParams();
        var request = new ApiRequest(HttpMethod.Get, "/webhook-events");
        request.Query
            .Add("type", p.Type)
            .Add("delivery_success", p.DeliverySuccess)
            .Add("limit", p.Limit)
            .Add("cursor", p.Cursor);
        return SendAsync<Page<WebhookEvent>>(request, cancellationToken);
    }

    /// <summary>Every webhook event, following <c>next_cursor</c> page by page until <c>has_more</c> is false.</summary>
    /// <param name="parameters">Filters and page size.</param>
    /// <param name="cancellationToken">Cancels the requests.</param>
    public async IAsyncEnumerable<WebhookEvent> ListAutoPagingAsync(WebhookEventListParams? parameters = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var p = parameters?.Copy() ?? new WebhookEventListParams();
        while (true)
        {
            var page = await ListAsync(p, cancellationToken).ConfigureAwait(false);
            foreach (var item in page.Data)
            {
                yield return item;
            }

            if (!page.HasMore)
            {
                yield break;
            }

            p.Cursor = page.NextCursor;
        }
    }

    /// <summary>Gets one webhook event with its deliveries (<c>GET /webhook-events/{webhook_event_id}</c>).</summary>
    /// <param name="webhookEventId">The event's id.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<WebhookEvent> GetAsync(string webhookEventId, CancellationToken cancellationToken = default) =>
        SendAsync<WebhookEvent>(new ApiRequest(HttpMethod.Get, "/webhook-events/" + Id(webhookEventId)), cancellationToken);

    /// <summary>Sends a webhook event again (<c>POST /webhook-events/{webhook_event_id}/resend</c>).</summary>
    /// <param name="webhookEventId">The event's id.</param>
    /// <param name="parameters">Optionally, one endpoint to resend to.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task<WebhookResendResult> ResendAsync(string webhookEventId, WebhookEventResendParams? parameters = null, CancellationToken cancellationToken = default)
    {
        var request = new ApiRequest(HttpMethod.Post, "/webhook-events/" + Id(webhookEventId) + "/resend")
        {
            Json = new JsonBody().Add("endpoint_id", parameters?.EndpointId),
        };
        return SendAsync<WebhookResendResult>(request, cancellationToken);
    }
}
