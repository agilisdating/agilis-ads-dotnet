using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Agilis.Ads.Internal;

namespace Agilis.Ads;

/// <summary>An Offer category.</summary>
public sealed class Category : AgilisObject
{
    /// <summary>The category id, used as <c>category_id</c> when creating an Offer.</summary>
    [JsonPropertyName("category_id")]
    public int CategoryId { get; set; }

    /// <summary>The category's name.</summary>
    [JsonPropertyName("category_name")]
    public string? CategoryName { get; set; }
}

/// <summary>The response of <c>GET /categories</c>.</summary>
public sealed class CategoryList : AgilisResponse
{
    /// <summary>The categories.</summary>
    [JsonPropertyName("categories")]
    public List<Category> Categories { get; set; } = new List<Category>();
}

/// <summary>A member interest that Offers can target.</summary>
public sealed class Interest : AgilisObject
{
    /// <summary>The interest id.</summary>
    [JsonPropertyName("interest_id")]
    public int InterestId { get; set; }

    /// <summary>The interest's name.</summary>
    [JsonPropertyName("interest_name")]
    public string? InterestName { get; set; }
}

/// <summary>The response of <c>GET /interests</c>.</summary>
public sealed class InterestList : AgilisResponse
{
    /// <summary>The interests.</summary>
    [JsonPropertyName("interests")]
    public List<Interest> Interests { get; set; } = new List<Interest>();
}

/// <summary>Parameters of <c>POST /offers/price-estimate</c>.</summary>
public sealed class OfferPriceEstimateParams
{
    /// <summary><c>start_datetime</c>: now or later, sent as UTC ISO 8601.</summary>
    public DateTimeOffset StartDatetime { get; set; }

    /// <summary><c>end_datetime</c>, sent as UTC ISO 8601.</summary>
    public DateTimeOffset EndDatetime { get; set; }

    /// <summary><c>location</c>: latitude and longitude.</summary>
    public GeoLocation Location { get; set; }

    /// <summary><c>radius</c> in miles, 5 to 30.</summary>
    public int Radius { get; set; }

    /// <summary><c>gender</c>: values from <see cref="Gender"/>. Omit to reach every gender.</summary>
    public IList<string>? Gender { get; set; }

    /// <summary><c>interests</c>: interest ids. Omit for no interest filter.</summary>
    public IList<int>? Interests { get; set; }

    /// <summary><c>environment</c>: <c>TEST</c> or <c>LIVE</c>. Filled in from the key's mode when omitted.</summary>
    public string? Environment { get; set; }
}

/// <summary>The response of <c>POST /offers/price-estimate</c>.</summary>
public sealed class OfferPriceEstimate : AgilisResponse
{
    /// <summary>The Offer's size in Ad Units (not money).</summary>
    [JsonPropertyName("price_estimate")]
    public decimal? PriceEstimate { get; set; }

    /// <summary>The members the Offer would reach.</summary>
    [JsonPropertyName("matching_users")]
    public int? MatchingUsers { get; set; }

    /// <summary>The Offer's size in Ad Units.</summary>
    [JsonPropertyName("ad_units")]
    public decimal? AdUnits { get; set; }

    /// <summary>The price of one Ad Unit in <see cref="Currency"/>.</summary>
    [JsonPropertyName("ad_unit_price")]
    public decimal? AdUnitPrice { get; set; }

    /// <summary>The money the Offer will cost: compare this with your balance.</summary>
    [JsonPropertyName("estimated_cost")]
    public decimal? EstimatedCost { get; set; }

    /// <summary>The organisation's billing currency.</summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    /// <summary>The exchange rate used, per pound sterling.</summary>
    [JsonPropertyName("exchange_rate_per_gbp")]
    public decimal? ExchangeRatePerGbp { get; set; }

    /// <summary>The rate's date (<c>YYYY-MM-DD</c>), or <c>null</c>.</summary>
    [JsonPropertyName("exchange_rate_date")]
    public string? ExchangeRateDate { get; set; }
}

/// <summary>Parameters of <c>POST /offers</c>.</summary>
public sealed class OfferCreateParams
{
    /// <summary><c>category_id</c>, from <c>Categories.ListAsync</c>.</summary>
    public int CategoryId { get; set; }

    /// <summary><c>title</c>, up to 40 characters.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary><c>description</c>, up to 500 characters.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary><c>terms_and_conditions</c>, up to 500 characters.</summary>
    public string? TermsAndConditions { get; set; }

    /// <summary><c>advertised_name</c>: the business name members see instead of your trading name.</summary>
    public string? AdvertisedName { get; set; }

    /// <summary><c>push_notification_title</c>, 5 to 50 characters.</summary>
    public string PushNotificationTitle { get; set; } = string.Empty;

    /// <summary><c>push_notification_message</c>, 5 to 150 characters.</summary>
    public string PushNotificationMessage { get; set; } = string.Empty;

    /// <summary><c>button_preset</c>: a value from <see cref="ButtonPreset"/>.</summary>
    public string? ButtonPreset { get; set; }

    /// <summary><c>button_text</c>: required when <see cref="ButtonPreset"/> is <c>custom</c>.</summary>
    public string? ButtonText { get; set; }

    /// <summary><c>url</c>: your Offer page.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary><c>image</c>: JPG, PNG or GIF, up to 5 MB.</summary>
    public FileUpload? Image { get; set; }

    /// <summary><c>location[]</c>: latitude and longitude.</summary>
    public GeoLocation Location { get; set; }

    /// <summary><c>radius</c> in miles, 5 to 30.</summary>
    public int Radius { get; set; }

    /// <summary><c>gender[]</c>: values from <see cref="Gender"/>.</summary>
    public IList<string>? Gender { get; set; }

    /// <summary><c>interests[]</c>: interest ids.</summary>
    public IList<int>? Interests { get; set; }

    /// <summary><c>start_datetime</c>, sent as UTC ISO 8601.</summary>
    public DateTimeOffset StartDatetime { get; set; }

    /// <summary><c>expiry_datetime</c>, sent as UTC ISO 8601.</summary>
    public DateTimeOffset ExpiryDatetime { get; set; }

    /// <summary><c>payment_method</c>: a value from <see cref="PaymentMethod"/>.</summary>
    public string PaymentMethod { get; set; } = global::Agilis.Ads.PaymentMethod.Balance;

    /// <summary><c>environment</c>: <c>TEST</c> or <c>LIVE</c>. Filled in from the key's mode when omitted.</summary>
    public string? Environment { get; set; }

    /// <summary>The <c>Idempotency-Key</c> header. A random UUID is generated (once per call) when omitted.</summary>
    public string? IdempotencyKey { get; set; }

    /// <summary>Sends <c>dry_run=true</c>: everything is checked and priced, nothing is created or charged.</summary>
    public bool DryRun { get; set; }
}

/// <summary>The response of <c>POST /offers</c>.</summary>
public sealed class OfferCreateResult : AgilisResponse
{
    /// <summary>The new Offer's id (not present on a dry run).</summary>
    [JsonPropertyName("offer_id")]
    public int? OfferId { get; set; }

    /// <summary>The Offer's status, for example <c>Awaiting Payment</c>, <c>Active</c> or <c>Scheduled</c>.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>The Stripe payment page, for <c>payment_method=E</c>.</summary>
    [JsonPropertyName("payment_url")]
    public string? PaymentUrl { get; set; }

    /// <summary>The billing currency.</summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    /// <summary><c>true</c> when this answered a dry run.</summary>
    [JsonPropertyName("dry_run")]
    public bool? DryRun { get; set; }

    /// <summary>Dry run: what the Offer would cost.</summary>
    [JsonPropertyName("price")]
    public decimal? Price { get; set; }

    /// <summary>Dry run: the members it would reach.</summary>
    [JsonPropertyName("matching_members")]
    public int? MatchingMembers { get; set; }

    /// <summary>Dry run, paying from balance: the balance afterwards.</summary>
    [JsonPropertyName("balance_after")]
    public decimal? BalanceAfter { get; set; }

    /// <summary>Dry run, paying by card: the amount the card would be charged.</summary>
    [JsonPropertyName("card_amount")]
    public decimal? CardAmount { get; set; }
}

/// <summary>An Offer.</summary>
public sealed class Offer : AgilisObject
{
    /// <summary>The Offer's id.</summary>
    [JsonPropertyName("offer_id")]
    public int OfferId { get; set; }

    /// <summary>The title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>The category's name.</summary>
    [JsonPropertyName("category")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Category { get; set; }

    /// <summary>The description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>The terms and conditions.</summary>
    [JsonPropertyName("terms_and_conditions")]
    public string? TermsAndConditions { get; set; }

    /// <summary>The business name members see, or <c>null</c> for your trading name.</summary>
    [JsonPropertyName("advertised_name")]
    public string? AdvertisedName { get; set; }

    /// <summary>The notification title.</summary>
    [JsonPropertyName("push_notification_title")]
    public string? PushNotificationTitle { get; set; }

    /// <summary>The notification message.</summary>
    [JsonPropertyName("push_notification_message")]
    public string? PushNotificationMessage { get; set; }

    /// <summary>The button's preset.</summary>
    [JsonPropertyName("button_preset")]
    public string? ButtonPreset { get; set; }

    /// <summary>Your own button text, when the preset is <c>custom</c>.</summary>
    [JsonPropertyName("button_text")]
    public string? ButtonText { get; set; }

    /// <summary>The Offer page.</summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>A signed image URL. It expires, so do not store it.</summary>
    [JsonPropertyName("image")]
    public string? Image { get; set; }

    /// <summary>Latitude.</summary>
    [JsonPropertyName("latitude")]
    public double? Latitude { get; set; }

    /// <summary>Longitude.</summary>
    [JsonPropertyName("longitude")]
    public double? Longitude { get; set; }

    /// <summary>Radius in miles.</summary>
    [JsonPropertyName("radius")]
    public int? Radius { get; set; }

    /// <summary>Targeted genders; empty when every gender is targeted.</summary>
    [JsonPropertyName("gender")]
    public List<string>? Gender { get; set; }

    /// <summary>Targeted interests; empty when there is no interest targeting.</summary>
    [JsonPropertyName("interests")]
    public List<Interest>? Interests { get; set; }

    /// <summary>When the Offer starts.</summary>
    [JsonPropertyName("start_datetime")]
    public DateTimeOffset? StartDatetime { get; set; }

    /// <summary>When the Offer ends.</summary>
    [JsonPropertyName("expiry_datetime")]
    public DateTimeOffset? ExpiryDatetime { get; set; }

    /// <summary>When the Offer was created.</summary>
    [JsonPropertyName("creation_datetime")]
    public DateTimeOffset? CreationDatetime { get; set; }

    /// <summary>Awaiting Payment, Active, Scheduled, Paused or Ended.</summary>
    [JsonPropertyName("offer_status")]
    public string? OfferStatus { get; set; }

    /// <summary><c>enabled</c> or <c>disabled</c>.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

/// <summary>Parameters of <c>GET /offers</c>.</summary>
public sealed class OfferListParams
{
    /// <summary><c>environment</c>: <c>test</c> or <c>live</c>. Filled in from the key's mode when omitted.</summary>
    public string? Environment { get; set; }

    /// <summary><c>limit</c>: page size, default 20, maximum 100.</summary>
    public int? Limit { get; set; }

    /// <summary><c>cursor</c>: the previous page's <c>next_cursor</c>.</summary>
    public string? Cursor { get; set; }

    /// <summary><c>offer_status</c>: <c>awaiting_payment</c>, <c>active</c>, <c>paused</c> or <c>ended</c>.</summary>
    public string? OfferStatus { get; set; }

    /// <summary><c>created_after</c>.</summary>
    public DateTimeOffset? CreatedAfter { get; set; }

    /// <summary><c>created_before</c>.</summary>
    public DateTimeOffset? CreatedBefore { get; set; }

    internal OfferListParams Copy() => (OfferListParams)MemberwiseClone();
}

/// <summary>The response of <c>DELETE /offers/{offer_id}</c>.</summary>
public sealed class OfferDeleteResult : ActionResult
{
    /// <summary>The Unused Time credited (or, on a dry run, that would be credited).</summary>
    [JsonPropertyName("refund")]
    public decimal? Refund { get; set; }
}

/// <summary>An Offer's metrics.</summary>
public sealed class OfferMetrics : AgilisObject
{
    /// <summary>Views.</summary>
    [JsonPropertyName("views")]
    public long? Views { get; set; }

    /// <summary>Clicks.</summary>
    [JsonPropertyName("clicks")]
    public long? Clicks { get; set; }
}

/// <summary>The response of <c>GET /offers/{offer_id}/analytics</c>.</summary>
public sealed class OfferAnalytics : AgilisResponse
{
    /// <summary>The Offer's id.</summary>
    [JsonPropertyName("offer_id")]
    public int? OfferId { get; set; }

    /// <summary>The interval asked for.</summary>
    [JsonPropertyName("interval")]
    public string? Interval { get; set; }

    /// <summary>The start of the period.</summary>
    [JsonPropertyName("start_datetime")]
    public DateTimeOffset? StartDatetime { get; set; }

    /// <summary>The end of the period.</summary>
    [JsonPropertyName("end_datetime")]
    public DateTimeOffset? EndDatetime { get; set; }

    /// <summary>The Offer's status.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>The metrics.</summary>
    [JsonPropertyName("metrics")]
    public OfferMetrics? Metrics { get; set; }
}
