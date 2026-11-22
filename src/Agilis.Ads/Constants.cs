namespace Agilis.Ads;

/// <summary>Values for <c>gender</c>.</summary>
public static class Gender
{
    /// <summary>Male.</summary>
    public const string Male = "Male";

    /// <summary>Female.</summary>
    public const string Female = "Female";

    /// <summary>Non-binary.</summary>
    public const string NonBinary = "Non-binary";

    /// <summary>Every gender.</summary>
    public const string All = "All";
}

/// <summary>Values for <c>payment_method</c>.</summary>
public static class PaymentMethod
{
    /// <summary><c>E</c>: a Stripe payment link (Offers only).</summary>
    public const string StripeLink = "E";

    /// <summary><c>B</c>: deducted from the balance of the request's mode.</summary>
    public const string Balance = "B";
}

/// <summary>Values for an Offer's <c>button_preset</c>.</summary>
public static class ButtonPreset
{
    /// <summary>Claim Offer (the default).</summary>
    public const string ClaimOffer = "claim_offer";

    /// <summary>Visit our website.</summary>
    public const string VisitWebsite = "visit_website";

    /// <summary>Install our app.</summary>
    public const string InstallApp = "install_app";

    /// <summary>Book now.</summary>
    public const string BookNow = "book_now";

    /// <summary>Get directions.</summary>
    public const string GetDirections = "get_directions";

    /// <summary>Your own words, in <c>button_text</c>.</summary>
    public const string Custom = "custom";
}

/// <summary>Values for an analytics <c>interval</c>.</summary>
public static class AnalyticsInterval
{
    /// <summary>The last 24 hours.</summary>
    public const string Last24Hours = "24h";

    /// <summary>The last 7 days.</summary>
    public const string Last7Days = "7d";

    /// <summary>The last 30 days.</summary>
    public const string Last30Days = "30d";

    /// <summary>The last 60 days.</summary>
    public const string Last60Days = "60d";

    /// <summary>The last 90 days.</summary>
    public const string Last90Days = "90d";

    /// <summary>All time.</summary>
    public const string All = "all";
}

/// <summary>Values for <c>expires_in</c> when rolling a webhook secret.</summary>
public static class SecretExpiry
{
    /// <summary>The old secret stops working at once.</summary>
    public const string Now = "now";

    /// <summary>The old secret keeps working for an hour.</summary>
    public const string OneHour = "1h";

    /// <summary>The old secret keeps working for 24 hours.</summary>
    public const string OneDay = "24h";

    /// <summary>The old secret keeps working for 7 days.</summary>
    public const string SevenDays = "7d";
}

/// <summary>Webhook event types.</summary>
public static class WebhookEventTypes
{
    /// <summary>Every type (for <c>enabled_events</c>).</summary>
    public const string All = "*";

    /// <summary><c>offer.created</c>.</summary>
    public const string OfferCreated = "offer.created";

    /// <summary><c>offer.payment_succeeded</c>.</summary>
    public const string OfferPaymentSucceeded = "offer.payment_succeeded";

    /// <summary><c>offer.payment_failed</c>.</summary>
    public const string OfferPaymentFailed = "offer.payment_failed";

    /// <summary><c>offer.started</c>.</summary>
    public const string OfferStarted = "offer.started";

    /// <summary><c>offer.paused</c>.</summary>
    public const string OfferPaused = "offer.paused";

    /// <summary><c>offer.resumed</c>.</summary>
    public const string OfferResumed = "offer.resumed";

    /// <summary><c>offer.ended</c>.</summary>
    public const string OfferEnded = "offer.ended";

    /// <summary><c>offer.deleted</c>.</summary>
    public const string OfferDeleted = "offer.deleted";

    /// <summary><c>offer.disabled</c>.</summary>
    public const string OfferDisabled = "offer.disabled";

    /// <summary><c>offer.enabled</c>.</summary>
    public const string OfferEnabled = "offer.enabled";

    /// <summary><c>event.published</c>.</summary>
    public const string EventPublished = "event.published";

    /// <summary><c>event.updated</c>.</summary>
    public const string EventUpdated = "event.updated";

    /// <summary><c>event.rescheduled</c>.</summary>
    public const string EventRescheduled = "event.rescheduled";

    /// <summary><c>event.cancelled</c>.</summary>
    public const string EventCancelled = "event.cancelled";

    /// <summary><c>event.deleted</c>.</summary>
    public const string EventDeleted = "event.deleted";

    /// <summary><c>event.ended</c>.</summary>
    public const string EventEnded = "event.ended";

    /// <summary><c>balance.topped_up</c>.</summary>
    public const string BalanceToppedUp = "balance.topped_up";

    /// <summary><c>balance.low</c>.</summary>
    public const string BalanceLow = "balance.low";

    /// <summary><c>invoice.created</c>.</summary>
    public const string InvoiceCreated = "invoice.created";

    /// <summary><c>terms.version_issued</c>.</summary>
    public const string TermsVersionIssued = "terms.version_issued";
}
