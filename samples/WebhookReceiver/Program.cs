// A minimal webhook receiver. Set AGILIS_WEBHOOK_SECRET to your endpoint's signing secret, then:
//   dotnet run --project samples/WebhookReceiver
// and point a TEST webhook endpoint at https://<your host>/webhooks/agilis.
using System;
using System.IO;
using Agilis.Ads;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

var secret = Environment.GetEnvironmentVariable("AGILIS_WEBHOOK_SECRET")
    ?? throw new InvalidOperationException("Set AGILIS_WEBHOOK_SECRET.");

var app = WebApplication.CreateBuilder(args).Build();

app.MapPost("/webhooks/agilis", async (HttpRequest request) =>
{
    // Read the body exactly as received: the signature covers these bytes.
    using var buffer = new MemoryStream();
    await request.Body.CopyToAsync(buffer);

    WebhookEvent evt;
    try
    {
        evt = Webhooks.ConstructEvent(buffer.ToArray(), request.Headers[Webhooks.SignatureHeader].ToString(), secret);
    }
    catch (SignatureVerificationException)
    {
        return Results.BadRequest();
    }

    // Answer within 10 seconds; do slow work afterwards. Use evt.Id to ignore repeats.
    switch (evt.Type)
    {
        case WebhookEventTypes.OfferStarted:
            Console.WriteLine($"Offer {evt.GetOffer()?.OfferId} started.");
            break;
        case WebhookEventTypes.EventPublished:
            Console.WriteLine($"Event {evt.GetEvent()?.EventId} published.");
            break;
        default:
            Console.WriteLine($"Received {evt.Type} ({evt.Id}).");
            break;
    }

    return Results.Ok();
});

app.Run();
