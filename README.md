# Agilis Advertising API: .NET SDK

The official .NET library for the [Agilis Advertising REST API](https://www.agilis.dating/api-doc): create and manage Offers and Events, read their results, and receive webhooks.

- `netstandard2.0` and `net8.0`, so it runs on .NET Framework 4.6.2+, .NET 8, .NET 10 and later, from C#, F# or Visual Basic
- Uses the built-in `HttpClient` and `System.Text.Json`
- Async throughout, with a `CancellationToken` on every call
- Typed parameters and responses; unknown response fields are kept, so a new API field never breaks your code
- Retries with backoff, idempotency keys, cursor pagination and webhook signature checks built in

## Install

```bash
dotnet add package Agilis.Ads
```

## Quick start (Test mode)

Create an API key in the Ad Portal under **Manage API Keys**, with the Ad Portal in Test mode. A `pk_test_` key works in Test mode only: members never see what you create and nothing is charged.

```bash
export AGILIS_API_KEY=pk_test_...
```

```csharp
using Agilis.Ads;

using var client = new AgilisClient(); // reads AGILIS_API_KEY

Console.WriteLine(client.Mode); // Test or Live, decided by the key

var categories = await client.Categories.ListAsync();
foreach (var category in categories.Categories)
{
    Console.WriteLine($"{category.CategoryId} {category.CategoryName}");
}

var estimate = await client.Offers.EstimatePriceAsync(new OfferPriceEstimateParams
{
    StartDatetime = DateTimeOffset.Parse("2027-07-10T08:00:00Z"),
    EndDatetime = DateTimeOffset.Parse("2027-07-12T20:30:00Z"),
    Location = new GeoLocation(51.5074, -0.1278),
    Radius = 10,
});
Console.WriteLine($"{estimate.EstimatedCost} {estimate.Currency}");

// DryRun checks everything and says what it would cost, without creating anything.
var result = await client.Offers.CreateAsync(new OfferCreateParams
{
    CategoryId = 1,
    Title = "20% off summer sale",
    Description = "Discount valid through July",
    PushNotificationTitle = "20% off this summer",
    PushNotificationMessage = "Show this at the till for 20% off everything.",
    Url = "https://example.com/offer",
    Image = FileUpload.FromPath("offer.png"),
    Location = new GeoLocation(51.5074, -0.1278),
    Radius = 10,
    StartDatetime = DateTimeOffset.Parse("2027-07-10T08:00:00Z"),
    ExpiryDatetime = DateTimeOffset.Parse("2027-07-12T20:30:00Z"),
    PaymentMethod = PaymentMethod.Balance,
    DryRun = true,
});
Console.WriteLine($"{result.Price} {result.BalanceAfter}");
```

Runnable samples are in [`samples/`](samples): a C# Test-mode tour (`QuickStart`), a webhook receiver (`WebhookReceiver`) and a Visual Basic program (`VbQuickStart`).

### Visual Basic

```vb
Using client As New AgilisClient()
    Dim categories As CategoryList = Await client.Categories.ListAsync()
    For Each category As Category In categories.Categories
        Console.WriteLine($"{category.CategoryId}: {category.CategoryName}")
    Next
End Using
```

An Event is `AgilisEvent`, because `Event` is a keyword in Visual Basic.

## The client

```csharp
var client = new AgilisClient(new AgilisClientOptions
{
    ApiKey = "pk_test_...",                    // default: the AGILIS_API_KEY environment variable
    BaseUrl = new Uri("https://ads.agilis.dating"), // the default
    Timeout = TimeSpan.FromSeconds(60),         // per attempt; the default
    MaxRetries = 2,                             // the default
    HttpClient = httpClient,                    // optional: your own HttpClient, for example from IHttpClientFactory
});
```

- **Mode.** The key decides it: `pk_test_` is Test and `pk_live_` is Live. `client.Mode` (and `client.IsLive`) let you refuse to run against Live. Operations that take `environment` fill it in from the key when you leave it out.
- **Reuse one client.** It is safe to share between threads. If you pass your own `HttpClient`, the SDK never disposes it.
- **Your key stays private.** It is sent only in the `x-api-key` header: never in a URL, a log or an exception message.

## Resources and methods

Every method is async and takes an optional `CancellationToken` last. Path parameters are arguments; everything else goes in one parameters object whose properties are the API's fields in PascalCase (`start_datetime` is `StartDatetime`).

| Resource | Method | HTTP |
|---|---|---|
| `client.Categories` | `ListAsync()` | `GET /categories` |
| `client.Interests` | `ListAsync()` | `GET /interests` |
| `client.Offers` | `EstimatePriceAsync(OfferPriceEstimateParams)` | `POST /offers/price-estimate` |
| | `CreateAsync(OfferCreateParams)` | `POST /offers` (multipart) |
| | `ListAsync(OfferListParams?)` | `GET /offers` |
| | `ListAutoPagingAsync(OfferListParams?)` | every page of `GET /offers` |
| | `PauseAsync(offerId)` | `POST /offers/{offer_id}/pause` |
| | `ResumeAsync(offerId)` | `POST /offers/{offer_id}/resume` |
| | `GetAnalyticsAsync(offerId, interval)` | `GET /offers/{offer_id}/analytics` |
| | `DeleteAsync(offerId, dryRun)` | `DELETE /offers/{offer_id}` |
| `client.EventCategories` | `ListAsync()` | `GET /event-categories` |
| `client.EventCurrencies` | `ListAsync()` | `GET /event-currencies` |
| `client.EventConfig` | `GetAsync()` | `GET /event-config` |
| `client.Events` | `PreviewAsync(EventPreviewParams)` | `POST /events/preview` (multipart) |
| | `PublishAsync(EventPublishParams)` | `POST /events` (multipart) |
| | `ListAsync(EventListParams?)` | `GET /events` |
| | `ListAutoPagingAsync(EventListParams?)` | every page of `GET /events` |
| | `GetAsync(eventId)` | `GET /events/{event_id}` |
| | `EditAsync(eventId, EventEditParams)` | `PATCH /events/{event_id}` (multipart, sent as POST with `X-HTTP-Method-Override: PATCH`) |
| | `DeleteAsync(eventId, dryRun)` | `DELETE /events/{event_id}` |
| | `RescheduleAsync(eventId, EventRescheduleParams)` | `POST /events/{event_id}/reschedule` |
| | `CancelAsync(eventId, dryRun)` | `POST /events/{event_id}/cancel` |
| | `GetAnalyticsAsync(eventId, interval)` | `GET /events/{event_id}/analytics` |
| `client.WebhookEndpoints` | `ListAsync()` | `GET /webhook-endpoints` |
| | `CreateAsync(WebhookEndpointCreateParams)` | `POST /webhook-endpoints` (the response holds the secret, once) |
| | `GetAsync(endpointId)` | `GET /webhook-endpoints/{endpoint_id}` |
| | `UpdateAsync(endpointId, WebhookEndpointUpdateParams)` | `PATCH /webhook-endpoints/{endpoint_id}` |
| | `DeleteAsync(endpointId)` | `DELETE /webhook-endpoints/{endpoint_id}` |
| | `RollSecretAsync(endpointId, WebhookSecretRollParams?)` | `POST /webhook-endpoints/{endpoint_id}/roll-secret` |
| | `SendTestAsync(endpointId, WebhookTestParams?)` | `POST /webhook-endpoints/{endpoint_id}/test` |
| `client.WebhookEvents` | `ListAsync(WebhookEventListParams?)` | `GET /webhook-events` |
| | `ListAutoPagingAsync(WebhookEventListParams?)` | every page of `GET /webhook-events` |
| | `GetAsync(webhookEventId)` | `GET /webhook-events/{webhook_event_id}` |
| | `ResendAsync(webhookEventId, WebhookEventResendParams?)` | `POST /webhook-events/{webhook_event_id}/resend` |
| `Webhooks` (static) | `ConstructEvent(payload, signatureHeader, secret, toleranceSeconds = 300)` | verifies a received webhook |

Constants for the API's fixed values are in `Gender`, `PaymentMethod`, `ButtonPreset`, `AnalyticsInterval`, `SecretExpiry` and `WebhookEventTypes`.

### Images

`FileUpload.FromPath("offer.png")`, `FileUpload.FromBytes(bytes, "offer.png")` or `FileUpload.FromStream(stream, "offer.jpg")`. The content type comes from the file name. Array fields go as repeated `location[]`, `gender[]` and `interests[]` parts, and an Event's crop as `crop[x]`, `crop[y]` and `crop[w]` (`Crop = new ImageCrop(x, y, w)`).

### Responses

Each method returns a typed object. Fields the SDK does not know yet are kept in `ExtensionData`. The HTTP status and headers are on `HttpResponse`:

```csharp
var page = await client.Offers.ListAsync();
Console.WriteLine(page.HttpResponse?.StatusCode);
Console.WriteLine(page.HttpResponse?.RateLimitRemaining); // X-RateLimit-Remaining
```

## Pagination

`Offers.ListAsync`, `Events.ListAsync` and `WebhookEvents.ListAsync` return one page (`Data`, `Pagination.HasMore`, `Pagination.NextCursor`). `ListAutoPagingAsync` follows `next_cursor` for you:

```csharp
await foreach (var offer in client.Offers.ListAutoPagingAsync(new OfferListParams { Limit = 100 }))
{
    Console.WriteLine($"{offer.OfferId} {offer.Title} {offer.OfferStatus}");
}
```

## Idempotency, dry runs and retries

- **Idempotency.** `Offers.CreateAsync` and `Events.PublishAsync` send an `Idempotency-Key`: yours (`IdempotencyKey = "..."`), or a random UUID generated once per call and reused on every retry of that call, so a retry never creates or charges twice.
- **Dry runs.** `DryRun = true` on `Offers.CreateAsync` and `Events.PublishAsync`, and `dryRun: true` on `Offers.DeleteAsync`, `Events.DeleteAsync` and `Events.CancelAsync`, send `dry_run=true`: the API checks everything and answers with what would happen, and nothing changes.
- **Retries.** Up to `MaxRetries` (default 2) for connection errors and timeouts, for HTTP 429, and for HTTP 500, 502, 503 and 504 on GET and DELETE requests and on POSTs that carry an Idempotency-Key. Other POST and PATCH requests are not retried on a 5xx, because the change may have happened. The wait is `Retry-After` when the API sends it, otherwise 0.5 s × 2^attempt with ±25% jitter, at most 8 s.

## Errors

Every error is an `AgilisException`, with `StatusCode`, `ReturnCode`, `Message`, the parsed `Body` and the response `Headers`.

| Type | When |
|---|---|
| `AuthenticationException` | 401 |
| `PermissionException` | 403 |
| `NotFoundException` | 404 |
| `ConflictException` | 409 |
| `InvalidRequestException` | 400, 422, or HTTP 200 with a `return_code` starting with `E` (for example `E3`, balance too low) |
| `RateLimitException` | 429, with `RetryAfter` |
| `ApiException` | 5xx and anything else |
| `ConnectionException` | no response (connection failed or timed out) |
| `SignatureVerificationException` | `Webhooks.ConstructEvent`, with `Reason` |

```csharp
try
{
    await client.Offers.CreateAsync(parameters);
}
catch (InvalidRequestException ex) when (ex.ReturnCode == "E3")
{
    Console.WriteLine("Top up the balance, then retry with the same IdempotencyKey.");
}
catch (RateLimitException ex)
{
    Console.WriteLine($"Wait {ex.RetryAfter} and try again.");
}
```

## Webhooks

Agilis signs every webhook with the `Agilis-Signature` header. `Webhooks.ConstructEvent` checks it, in constant time, against the raw body and your endpoint's secret, accepts any matching `v1` signature (so a rolled secret keeps working during its overlap), refuses a timestamp more than 300 seconds from now, and returns the parsed `WebhookEvent`.

A minimal ASP.NET Core receiver (the full one is [`samples/WebhookReceiver`](samples/WebhookReceiver)):

```csharp
app.MapPost("/webhooks/agilis", async (HttpRequest request) =>
{
    using var buffer = new MemoryStream();
    await request.Body.CopyToAsync(buffer); // the raw body, exactly as received

    WebhookEvent evt;
    try
    {
        evt = Webhooks.ConstructEvent(buffer.ToArray(), request.Headers["Agilis-Signature"].ToString(), secret);
    }
    catch (SignatureVerificationException)
    {
        return Results.BadRequest();
    }

    if (evt.Type == WebhookEventTypes.EventPublished)
    {
        AgilisEvent? published = evt.GetEvent(); // typed by evt.Type; GetOffer() for offer.* events
    }

    return Results.Ok(); // answer within 10 seconds; use evt.Id to ignore repeats
});
```

## Links

- API reference: https://www.agilis.dating/api-doc
- OpenAPI: https://www.agilis.dating/openapi.json
- AsyncAPI (webhooks): https://www.agilis.dating/asyncapi.json
- API documentation repository: https://github.com/agilisdating/agilis-ads-api

## Licence

MIT. See [LICENSE](LICENSE). Security reports: see [SECURITY.md](SECURITY.md).
