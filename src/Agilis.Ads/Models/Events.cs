using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Agilis.Ads.Internal;

namespace Agilis.Ads;

/// <summary>An Event category.</summary>
public sealed class EventCategory : AgilisObject
{
    /// <summary>The category id.</summary>
    [JsonPropertyName("category_id")]
    public int CategoryId { get; set; }

    /// <summary>A stable code for the category.</summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <summary>The category's name.</summary>
    [JsonPropertyName("category_name")]
    public string? CategoryName { get; set; }
}

/// <summary>The response of <c>GET /event-categories</c>.</summary>
public sealed class EventCategoryList : AgilisResponse
{
    /// <summary>The categories.</summary>
    [JsonPropertyName("categories")]
    public List<EventCategory> Categories { get; set; } = new List<EventCategory>();
}

/// <summary>The response of <c>GET /event-currencies</c>.</summary>
public sealed class EventCurrencyList : AgilisResponse
{
    /// <summary>The currency codes a ticket price may be quoted in.</summary>
    [JsonPropertyName("currencies")]
    public List<string> Currencies { get; set; } = new List<string>();
}

/// <summary>Image limits for Events.</summary>
public sealed class EventImageLimits : AgilisObject
{
    /// <summary>Maximum size in megabytes.</summary>
    [JsonPropertyName("max_mb")]
    public decimal? MaxMb { get; set; }

    /// <summary>Minimum width in pixels.</summary>
    [JsonPropertyName("min_width")]
    public int? MinWidth { get; set; }

    /// <summary>Minimum height in pixels.</summary>
    [JsonPropertyName("min_height")]
    public int? MinHeight { get; set; }
}

/// <summary>The response of <c>GET /event-config</c>.</summary>
public sealed class EventConfig : AgilisResponse
{
    /// <summary>The fee to publish one Event, in <see cref="Currency"/>.</summary>
    [JsonPropertyName("listing_fee")]
    public decimal? ListingFee { get; set; }

    /// <summary>The organisation's billing currency.</summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    /// <summary>The exchange rate used, per pound sterling.</summary>
    [JsonPropertyName("exchange_rate_per_gbp")]
    public decimal? ExchangeRatePerGbp { get; set; }

    /// <summary>The rate's date, or <c>null</c>.</summary>
    [JsonPropertyName("exchange_rate_date")]
    public string? ExchangeRateDate { get; set; }

    /// <summary>The key's mode: <c>TEST</c> or <c>LIVE</c>.</summary>
    [JsonPropertyName("environment")]
    public string? Environment { get; set; }

    /// <summary>The balance for the key's mode.</summary>
    [JsonPropertyName("balance")]
    public decimal? Balance { get; set; }

    /// <summary>The organisation's balance for the key's mode.</summary>
    [JsonPropertyName("organisation_balance")]
    public decimal? OrganisationBalance { get; set; }

    /// <summary>Whether LIVE publishing is open.</summary>
    [JsonPropertyName("live_publishing_open")]
    public bool? LivePublishingOpen { get; set; }

    /// <summary>The accepted ticket currencies.</summary>
    [JsonPropertyName("currencies")]
    public List<string>? Currencies { get; set; }

    /// <summary>The image limits.</summary>
    [JsonPropertyName("image")]
    public EventImageLimits? Image { get; set; }

    /// <summary>How many years ahead an Event may start.</summary>
    [JsonPropertyName("max_years_ahead")]
    public int? MaxYearsAhead { get; set; }
}

/// <summary>A 16:9 image area: top-left corner and width in pixels (the height is derived).</summary>
public sealed class ImageCrop : AgilisObject
{
    /// <summary>Creates an empty crop.</summary>
    public ImageCrop() { }

    /// <summary>Creates a crop.</summary>
    /// <param name="x">Left edge in pixels.</param>
    /// <param name="y">Top edge in pixels.</param>
    /// <param name="w">Width in pixels (at least 1280).</param>
    public ImageCrop(int x, int y, int w)
    {
        X = x;
        Y = y;
        W = w;
    }

    /// <summary><c>crop[x]</c>: left edge in pixels.</summary>
    [JsonPropertyName("x")]
    public int X { get; set; }

    /// <summary><c>crop[y]</c>: top edge in pixels.</summary>
    [JsonPropertyName("y")]
    public int Y { get; set; }

    /// <summary><c>crop[w]</c>: width in pixels.</summary>
    [JsonPropertyName("w")]
    public int W { get; set; }

    /// <summary>The height (in responses only).</summary>
    [JsonPropertyName("h")]
    public int? H { get; set; }
}

/// <summary>The fields shared by <c>POST /events/preview</c> and <c>POST /events</c>.</summary>
public abstract class EventFieldsBase
{
    /// <summary><c>title</c>, up to 120 characters.</summary>
    public string? Title { get; set; }

    /// <summary><c>description</c>, up to 5,000 characters.</summary>
    public string? Description { get; set; }

    /// <summary><c>venue_name</c>.</summary>
    public string? VenueName { get; set; }

    /// <summary><c>address</c>.</summary>
    public string? Address { get; set; }

    /// <summary><c>city</c>.</summary>
    public string? City { get; set; }

    /// <summary><c>country_code</c>: ISO 3166-1 alpha-2, for example <c>GB</c>.</summary>
    public string? CountryCode { get; set; }

    /// <summary><c>location[]</c>: the venue's latitude and longitude.</summary>
    public GeoLocation? Location { get; set; }

    /// <summary><c>starts_at</c>, sent as UTC ISO 8601.</summary>
    public DateTimeOffset? StartsAt { get; set; }

    /// <summary><c>ends_at</c>, sent as UTC ISO 8601.</summary>
    public DateTimeOffset? EndsAt { get; set; }

    /// <summary><c>url</c>: your booking page.</summary>
    public string? Url { get; set; }

    /// <summary><c>advertised_name</c>: the name shown as "Listed by".</summary>
    public string? AdvertisedName { get; set; }

    /// <summary><c>price_min</c>.</summary>
    public decimal? PriceMin { get; set; }

    /// <summary><c>price_max</c>.</summary>
    public decimal? PriceMax { get; set; }

    /// <summary><c>currency</c> of the ticket price; required with a price.</summary>
    public string? Currency { get; set; }

    /// <summary><c>image</c>: JPG or PNG, up to 10 MB, at least 1280 × 720.</summary>
    public FileUpload? Image { get; set; }

    /// <summary><c>crop[x]</c>, <c>crop[y]</c>, <c>crop[w]</c>.</summary>
    public ImageCrop? Crop { get; set; }

    /// <summary><c>payment_method</c>: <c>B</c>.</summary>
    public string? PaymentMethod { get; set; }

    /// <summary><c>environment</c>: <c>TEST</c> or <c>LIVE</c>. Filled in from the key's mode when omitted.</summary>
    public string? Environment { get; set; }

    internal virtual void AddTo(MultipartForm form)
    {
        form.Add("title", Title)
            .Add("description", Description)
            .Add("venue_name", VenueName)
            .Add("address", Address)
            .Add("city", City)
            .Add("country_code", CountryCode)
            .Add("location", Location)
            .Add("starts_at", StartsAt)
            .Add("ends_at", EndsAt)
            .Add("url", Url)
            .Add("advertised_name", AdvertisedName)
            .Add("price_min", PriceMin)
            .Add("price_max", PriceMax)
            .Add("currency", Currency)
            .AddCrop(Crop)
            .Add("payment_method", PaymentMethod)
            .AddFile("image", Image);
    }
}

/// <summary>Parameters of <c>POST /events/preview</c>.</summary>
public sealed class EventPreviewParams : EventFieldsBase
{
}

/// <summary>Parameters of <c>POST /events</c>.</summary>
public sealed class EventPublishParams : EventFieldsBase
{
    /// <summary>Creates the parameters, with <c>payment_method</c> set to <c>B</c>.</summary>
    public EventPublishParams()
    {
        PaymentMethod = global::Agilis.Ads.PaymentMethod.Balance;
    }

    /// <summary><c>preview_token</c> from a preview: publishes exactly the image previewed.</summary>
    public string? PreviewToken { get; set; }

    /// <summary><c>source_event_id</c>: reuse the image of one of your Events.</summary>
    public int? SourceEventId { get; set; }

    /// <summary>The <c>Idempotency-Key</c> header. A random UUID is generated (once per call) when omitted.</summary>
    public string? IdempotencyKey { get; set; }

    /// <summary>Sends <c>dry_run=true</c>: everything is checked, nothing is created or charged.</summary>
    public bool DryRun { get; set; }

    internal override void AddTo(MultipartForm form)
    {
        base.AddTo(form);
        form.Add("preview_token", PreviewToken).Add("source_event_id", SourceEventId);
    }
}

/// <summary>Parameters of <c>PATCH /events/{event_id}</c>. Send only the fields to change.</summary>
public sealed class EventEditParams
{
    /// <summary><c>title</c>.</summary>
    public string? Title { get; set; }

    /// <summary><c>description</c>.</summary>
    public string? Description { get; set; }

    /// <summary><c>url</c>.</summary>
    public string? Url { get; set; }

    /// <summary><c>advertised_name</c>. Send an empty string to go back to your trading name.</summary>
    public string? AdvertisedName { get; set; }

    /// <summary><c>venue_name</c>.</summary>
    public string? VenueName { get; set; }

    /// <summary><c>address</c>.</summary>
    public string? Address { get; set; }

    /// <summary><c>city</c>.</summary>
    public string? City { get; set; }

    /// <summary><c>country_code</c>.</summary>
    public string? CountryCode { get; set; }

    /// <summary><c>location[]</c>.</summary>
    public GeoLocation? Location { get; set; }

    /// <summary><c>price_min</c>. A price change sends all three of price_min, price_max and currency.</summary>
    public decimal? PriceMin { get; set; }

    /// <summary><c>price_max</c>.</summary>
    public decimal? PriceMax { get; set; }

    /// <summary><c>currency</c>.</summary>
    public string? Currency { get; set; }

    /// <summary>Sends <c>price_min</c>, <c>price_max</c> and <c>currency</c> blank, to show no price.</summary>
    public bool ClearPrice { get; set; }

    /// <summary><c>image</c>: a replacement picture.</summary>
    public FileUpload? Image { get; set; }

    /// <summary><c>preview_token</c>.</summary>
    public string? PreviewToken { get; set; }

    /// <summary><c>source_event_id</c>.</summary>
    public int? SourceEventId { get; set; }

    /// <summary><c>crop[x]</c>, <c>crop[y]</c>, <c>crop[w]</c>: on its own, re-frames the existing image.</summary>
    public ImageCrop? Crop { get; set; }

    internal void AddTo(MultipartForm form)
    {
        form.Add("title", Title)
            .Add("description", Description)
            .Add("url", Url)
            .Add("advertised_name", AdvertisedName)
            .Add("venue_name", VenueName)
            .Add("address", Address)
            .Add("city", City)
            .Add("country_code", CountryCode)
            .Add("location", Location);
        if (ClearPrice)
        {
            form.Add("price_min", string.Empty).Add("price_max", string.Empty).Add("currency", string.Empty);
        }
        else
        {
            form.Add("price_min", PriceMin).Add("price_max", PriceMax).Add("currency", Currency);
        }

        form.Add("preview_token", PreviewToken)
            .Add("source_event_id", SourceEventId)
            .AddCrop(Crop)
            .AddFile("image", Image);
    }
}

/// <summary>An Event category as it appears on an Event.</summary>
public sealed class EventCategoryRef : AgilisObject
{
    /// <summary>The category id.</summary>
    [JsonPropertyName("category_id")]
    public int? CategoryId { get; set; }

    /// <summary>The category's name.</summary>
    [JsonPropertyName("category_name")]
    public string? CategoryName { get; set; }
}

/// <summary>The processed image of a preview.</summary>
public sealed class EventPreviewImage : AgilisObject
{
    /// <summary>A short-lived URL of the processed image.</summary>
    [JsonPropertyName("preview_url")]
    public string? PreviewUrl { get; set; }

    /// <summary>Width in pixels.</summary>
    [JsonPropertyName("width")]
    public int? Width { get; set; }

    /// <summary>Height in pixels.</summary>
    [JsonPropertyName("height")]
    public int? Height { get; set; }

    /// <summary>The uploaded image's width.</summary>
    [JsonPropertyName("master_width")]
    public int? MasterWidth { get; set; }

    /// <summary>The uploaded image's height.</summary>
    [JsonPropertyName("master_height")]
    public int? MasterHeight { get; set; }

    /// <summary>The crop applied.</summary>
    [JsonPropertyName("crop")]
    public ImageCrop? Crop { get; set; }

    /// <summary>Pass as <c>preview_token</c> when publishing.</summary>
    [JsonPropertyName("preview_token")]
    public string? PreviewToken { get; set; }

    /// <summary>When the token expires.</summary>
    [JsonPropertyName("expires_at")]
    public DateTimeOffset? ExpiresAt { get; set; }
}

/// <summary>The response of <c>POST /events/preview</c>.</summary>
public sealed class EventPreview : AgilisResponse
{
    /// <summary>The category your wording gets.</summary>
    [JsonPropertyName("category")]
    public EventCategoryRef? Category { get; set; }

    /// <summary>The listing fee.</summary>
    [JsonPropertyName("listing_fee")]
    public decimal? ListingFee { get; set; }

    /// <summary>The billing currency.</summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    /// <summary>The balance for the mode.</summary>
    [JsonPropertyName("balance")]
    public decimal? Balance { get; set; }

    /// <summary>The balance after publishing.</summary>
    [JsonPropertyName("balance_after")]
    public decimal? BalanceAfter { get; set; }

    /// <summary>Whether the balance covers the fee.</summary>
    [JsonPropertyName("balance_sufficient")]
    public bool? BalanceSufficient { get; set; }

    /// <summary>Whether LIVE publishing is open.</summary>
    [JsonPropertyName("live_publishing_open")]
    public bool? LivePublishingOpen { get; set; }

    /// <summary>The fee in pounds sterling.</summary>
    [JsonPropertyName("price_gbp")]
    public decimal? PriceGbp { get; set; }

    /// <summary>The processed image, when one was sent.</summary>
    [JsonPropertyName("image")]
    public EventPreviewImage? Image { get; set; }
}

/// <summary>The response of <c>POST /events</c>.</summary>
public sealed class EventPublishResult : AgilisResponse
{
    /// <summary>The new Event's id (not present on a dry run).</summary>
    [JsonPropertyName("event_id")]
    public int? EventId { get; set; }

    /// <summary>The Event's status.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Always <c>null</c> for Events.</summary>
    [JsonPropertyName("payment_url")]
    public string? PaymentUrl { get; set; }

    /// <summary>The amount charged.</summary>
    [JsonPropertyName("amount_charged")]
    public decimal? AmountCharged { get; set; }

    /// <summary>The billing currency.</summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    /// <summary>The category the Event got.</summary>
    [JsonPropertyName("category")]
    public EventCategoryRef? Category { get; set; }

    /// <summary><c>true</c> when this answered a dry run.</summary>
    [JsonPropertyName("dry_run")]
    public bool? DryRun { get; set; }

    /// <summary>Dry run: the listing fee.</summary>
    [JsonPropertyName("listing_fee")]
    public decimal? ListingFee { get; set; }

    /// <summary>Dry run: what would be charged.</summary>
    [JsonPropertyName("amount_to_charge")]
    public decimal? AmountToCharge { get; set; }

    /// <summary>Dry run: the balance afterwards.</summary>
    [JsonPropertyName("balance_after")]
    public decimal? BalanceAfter { get; set; }
}

/// <summary>The member or user who owns an Event.</summary>
public sealed class EventOwner : AgilisObject
{
    /// <summary>The user id.</summary>
    [JsonPropertyName("user_id")]
    public int? UserId { get; set; }

    /// <summary>The user's name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

/// <summary>
/// An Event. (Named <c>AgilisEvent</c> because <c>Event</c> is a keyword in Visual Basic.)
/// </summary>
public class AgilisEvent : AgilisResponse
{
    /// <summary>The Event's id.</summary>
    [JsonPropertyName("event_id")]
    public int EventId { get; set; }

    /// <summary>The title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>The description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>The category.</summary>
    [JsonPropertyName("category")]
    public EventCategoryRef? Category { get; set; }

    /// <summary>The venue's name.</summary>
    [JsonPropertyName("venue_name")]
    public string? VenueName { get; set; }

    /// <summary>The address.</summary>
    [JsonPropertyName("address")]
    public string? Address { get; set; }

    /// <summary>The city.</summary>
    [JsonPropertyName("city")]
    public string? City { get; set; }

    /// <summary>The country code.</summary>
    [JsonPropertyName("country_code")]
    public string? CountryCode { get; set; }

    /// <summary>Latitude.</summary>
    [JsonPropertyName("latitude")]
    public double? Latitude { get; set; }

    /// <summary>Longitude.</summary>
    [JsonPropertyName("longitude")]
    public double? Longitude { get; set; }

    /// <summary>When the Event starts.</summary>
    [JsonPropertyName("starts_at")]
    public DateTimeOffset? StartsAt { get; set; }

    /// <summary>When the Event ends.</summary>
    [JsonPropertyName("ends_at")]
    public DateTimeOffset? EndsAt { get; set; }

    /// <summary>The booking page.</summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>The name shown as "Listed by", or <c>null</c>.</summary>
    [JsonPropertyName("advertised_name")]
    public string? AdvertisedName { get; set; }

    /// <summary>The lowest ticket price.</summary>
    [JsonPropertyName("price_min")]
    public decimal? PriceMin { get; set; }

    /// <summary>The highest ticket price.</summary>
    [JsonPropertyName("price_max")]
    public decimal? PriceMax { get; set; }

    /// <summary>The ticket price's currency.</summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    /// <summary>The original image.</summary>
    [JsonPropertyName("image")]
    public string? Image { get; set; }

    /// <summary>The large image.</summary>
    [JsonPropertyName("image_large")]
    public string? ImageLarge { get; set; }

    /// <summary>The thumbnail.</summary>
    [JsonPropertyName("image_thumb")]
    public string? ImageThumb { get; set; }

    /// <summary>The Event's status, for example <c>Scheduled</c>.</summary>
    [JsonPropertyName("event_status")]
    public string? EventStatus { get; set; }

    /// <summary>Members going, as a band (for example <c>10-25</c>).</summary>
    [JsonPropertyName("attendees")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Attendees { get; set; }

    /// <summary>Who owns the Event.</summary>
    [JsonPropertyName("owner")]
    public EventOwner? Owner { get; set; }

    /// <summary><c>TEST</c> or <c>LIVE</c>.</summary>
    [JsonPropertyName("environment")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Environment { get; set; }

    /// <summary>How many times the Event has been rescheduled.</summary>
    [JsonPropertyName("reschedule_count")]
    public int? RescheduleCount { get; set; }

    /// <summary>The Event it was copied from, if any.</summary>
    [JsonPropertyName("cloned_from_event_id")]
    public int? ClonedFromEventId { get; set; }

    /// <summary>When the Event was created.</summary>
    [JsonPropertyName("creation_datetime")]
    public DateTimeOffset? CreationDatetime { get; set; }

    /// <summary>When the Event was last edited.</summary>
    [JsonPropertyName("last_edited_at")]
    public DateTimeOffset? LastEditedAt { get; set; }
}

/// <summary>The stored original image of an Event.</summary>
public sealed class EventImageMaster : AgilisObject
{
    /// <summary>Width in pixels.</summary>
    [JsonPropertyName("width")]
    public int? Width { get; set; }

    /// <summary>Height in pixels.</summary>
    [JsonPropertyName("height")]
    public int? Height { get; set; }

    /// <summary>The crop in use.</summary>
    [JsonPropertyName("crop")]
    public ImageCrop? Crop { get; set; }

    /// <summary>The original's URL, when available.</summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

/// <summary>One entry of an Event's history.</summary>
public sealed class EventHistoryEntry : AgilisObject
{
    /// <summary>What happened, for example <c>published</c>.</summary>
    [JsonPropertyName("action")]
    public string? Action { get; set; }

    /// <summary>Where it came from, for example <c>api</c>.</summary>
    [JsonPropertyName("channel")]
    public string? Channel { get; set; }

    /// <summary>The user who acted.</summary>
    [JsonPropertyName("acted_by_user_id")]
    public int? ActedByUserId { get; set; }

    /// <summary>The name of the user who acted.</summary>
    [JsonPropertyName("acted_by")]
    public string? ActedBy { get; set; }

    /// <summary>Details of the change.</summary>
    [JsonPropertyName("details")]
    public JsonElement? Details { get; set; }

    /// <summary>Whether the entry has been acknowledged.</summary>
    [JsonPropertyName("acknowledged")]
    public bool? Acknowledged { get; set; }

    /// <summary>When it happened.</summary>
    [JsonPropertyName("at")]
    public DateTimeOffset? At { get; set; }
}

/// <summary>The response of <c>GET /events/{event_id}</c>: every list field, plus details.</summary>
public sealed class AgilisEventDetails : AgilisEvent
{
    /// <summary>Whether your key may manage this Event.</summary>
    [JsonPropertyName("can_manage")]
    public bool? CanManage { get; set; }

    /// <summary>Whether it can still be deleted.</summary>
    [JsonPropertyName("can_delete")]
    public bool? CanDelete { get; set; }

    /// <summary>What publishing it cost.</summary>
    [JsonPropertyName("listing_price")]
    public decimal? ListingPrice { get; set; }

    /// <summary>The billing currency.</summary>
    [JsonPropertyName("billing_currency")]
    public string? BillingCurrency { get; set; }

    /// <summary>The invoice.</summary>
    [JsonPropertyName("invoice_id")]
    public string? InvoiceId { get; set; }

    /// <summary>When the apps last received it.</summary>
    [JsonPropertyName("app_synced_at")]
    public DateTimeOffset? AppSyncedAt { get; set; }

    /// <summary>Whether members have interacted with it.</summary>
    [JsonPropertyName("has_member_interaction")]
    public bool? HasMemberInteraction { get; set; }

    /// <summary>The stored original image.</summary>
    [JsonPropertyName("image_master")]
    public EventImageMaster? ImageMaster { get; set; }

    /// <summary>The Event's history.</summary>
    [JsonPropertyName("history")]
    public List<EventHistoryEntry>? History { get; set; }
}

/// <summary>Parameters of <c>GET /events</c>.</summary>
public sealed class EventListParams
{
    /// <summary><c>environment</c>: <c>test</c> or <c>live</c>. Filled in from the key's mode when omitted.</summary>
    public string? Environment { get; set; }

    /// <summary><c>limit</c>: page size.</summary>
    public int? Limit { get; set; }

    /// <summary><c>cursor</c>: the previous page's <c>next_cursor</c>.</summary>
    public string? Cursor { get; set; }

    /// <summary><c>event_status</c>: <c>upcoming</c>, <c>scheduled</c>, <c>rescheduled</c>, <c>cancelled</c> or <c>ended</c>.</summary>
    public string? EventStatus { get; set; }

    /// <summary><c>owner_user_id</c>.</summary>
    public int? OwnerUserId { get; set; }

    /// <summary><c>created_after</c>.</summary>
    public DateTimeOffset? CreatedAfter { get; set; }

    /// <summary><c>created_before</c>.</summary>
    public DateTimeOffset? CreatedBefore { get; set; }

    internal EventListParams Copy() => (EventListParams)MemberwiseClone();
}

/// <summary>The response of <c>PATCH /events/{event_id}</c>.</summary>
public sealed class EventEditResult : AgilisResponse
{
    /// <summary>The API's message.</summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>The Event's id.</summary>
    [JsonPropertyName("event_id")]
    public int? EventId { get; set; }

    /// <summary>The Event's status.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>The category after the change.</summary>
    [JsonPropertyName("category")]
    public EventCategoryRef? Category { get; set; }

    /// <summary>The fields that changed.</summary>
    [JsonPropertyName("changed_fields")]
    public List<string>? ChangedFields { get; set; }

    /// <summary>The new image URL, when the image changed.</summary>
    [JsonPropertyName("image")]
    public string? Image { get; set; }

    /// <summary>The new large image URL, when the image changed.</summary>
    [JsonPropertyName("image_large")]
    public string? ImageLarge { get; set; }

    /// <summary>Whether the venue changed.</summary>
    [JsonPropertyName("venue_changed")]
    public bool? VenueChanged { get; set; }
}

/// <summary>Parameters of <c>POST /events/{event_id}/reschedule</c>.</summary>
public sealed class EventRescheduleParams
{
    /// <summary><c>starts_at</c>, sent as UTC ISO 8601.</summary>
    public DateTimeOffset StartsAt { get; set; }

    /// <summary><c>ends_at</c>, sent as UTC ISO 8601.</summary>
    public DateTimeOffset EndsAt { get; set; }
}

/// <summary>The response of <c>POST /events/{event_id}/reschedule</c>.</summary>
public sealed class EventRescheduleResult : AgilisResponse
{
    /// <summary>The API's message.</summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>The Event's id.</summary>
    [JsonPropertyName("event_id")]
    public int? EventId { get; set; }

    /// <summary>The Event's status.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>The new start.</summary>
    [JsonPropertyName("starts_at")]
    public DateTimeOffset? StartsAt { get; set; }

    /// <summary>The new end.</summary>
    [JsonPropertyName("ends_at")]
    public DateTimeOffset? EndsAt { get; set; }
}

/// <summary>The response of <c>POST /events/{event_id}/cancel</c>.</summary>
public sealed class EventCancelResult : AgilisResponse
{
    /// <summary>The API's message.</summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>The Event's id.</summary>
    [JsonPropertyName("event_id")]
    public int? EventId { get; set; }

    /// <summary>The Event's status.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary><c>true</c> when this answered a dry run.</summary>
    [JsonPropertyName("dry_run")]
    public bool? DryRun { get; set; }
}

/// <summary>An Event's metrics.</summary>
public sealed class EventMetrics : AgilisObject
{
    /// <summary>Members who opened your booking page, each counted once.</summary>
    [JsonPropertyName("clicks")]
    public long? Clicks { get; set; }

    /// <summary>Members going, as a band.</summary>
    [JsonPropertyName("attendees")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Attendees { get; set; }

    /// <summary>Invitations sent, as a band.</summary>
    [JsonPropertyName("invites_sent")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? InvitesSent { get; set; }

    /// <summary>Invitations accepted, as a band.</summary>
    [JsonPropertyName("invites_accepted")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? InvitesAccepted { get; set; }
}

/// <summary>The response of <c>GET /events/{event_id}/analytics</c>.</summary>
public sealed class EventAnalytics : AgilisResponse
{
    /// <summary>The Event's id.</summary>
    [JsonPropertyName("event_id")]
    public int? EventId { get; set; }

    /// <summary>The interval asked for.</summary>
    [JsonPropertyName("interval")]
    public string? Interval { get; set; }

    /// <summary>When the Event starts.</summary>
    [JsonPropertyName("starts_at")]
    public DateTimeOffset? StartsAt { get; set; }

    /// <summary>When the Event ends.</summary>
    [JsonPropertyName("ends_at")]
    public DateTimeOffset? EndsAt { get; set; }

    /// <summary>The Event's status.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>The metrics.</summary>
    [JsonPropertyName("metrics")]
    public EventMetrics? Metrics { get; set; }

    /// <summary>When the figures were calculated.</summary>
    [JsonPropertyName("as_of")]
    public DateTimeOffset? AsOf { get; set; }
}
