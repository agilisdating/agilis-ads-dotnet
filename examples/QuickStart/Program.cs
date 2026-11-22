// A short Test-mode tour of the SDK. Set AGILIS_API_KEY to a pk_test_ key, then:
//   dotnet run --project examples/QuickStart
// It only reads, plus one dry run that creates and charges nothing.
using System;
using System.IO;
using System.Linq;
using Agilis.Ads;

using var client = new AgilisClient();
if (client.Mode != AgilisMode.Test)
{
    Console.Error.WriteLine("This sample runs with a Test-mode (pk_test_) key only.");
    return 1;
}

var categories = await client.Categories.ListAsync();
Console.WriteLine($"{categories.Categories.Count} Offer categories, for example {categories.Categories.FirstOrDefault()?.CategoryName}.");
Console.WriteLine($"Requests left this minute: {categories.HttpResponse?.RateLimitRemaining}");

var start = DateTimeOffset.UtcNow.AddDays(1);
var estimate = await client.Offers.EstimatePriceAsync(new OfferPriceEstimateParams
{
    StartDatetime = start,
    EndDatetime = start.AddDays(2),
    Location = new GeoLocation(51.5074, -0.1278),
    Radius = 10,
});
Console.WriteLine($"A two-day Offer in central London would reach {estimate.MatchingUsers} members for {estimate.EstimatedCost} {estimate.Currency}.");

await foreach (var offer in client.Offers.ListAutoPagingAsync(new OfferListParams { Limit = 20 }))
{
    Console.WriteLine($"Offer {offer.OfferId}: {offer.Title} ({offer.OfferStatus})");
}

try
{
    var dryRun = await client.Offers.CreateAsync(new OfferCreateParams
    {
        CategoryId = categories.Categories[0].CategoryId,
        Title = "Dry run",
        Description = "A dry run from the .NET SDK sample. Nothing is created.",
        PushNotificationTitle = "Dry run",
        PushNotificationMessage = "A dry run from the .NET SDK sample.",
        Url = "https://www.agilis.dating",
        Image = FileUpload.FromPath(Path.Combine(AppContext.BaseDirectory, "sample-offer.png")),
        Location = new GeoLocation(51.5074, -0.1278),
        Radius = 10,
        StartDatetime = start,
        ExpiryDatetime = start.AddDays(1),
        PaymentMethod = PaymentMethod.Balance,
        DryRun = true,
    });
    Console.WriteLine($"Dry run: it would cost {dryRun.Price}, leaving {dryRun.BalanceAfter}.");
}
catch (InvalidRequestException ex)
{
    Console.WriteLine($"The dry run was refused ({ex.ReturnCode}): {ex.Message}");
}

return 0;
