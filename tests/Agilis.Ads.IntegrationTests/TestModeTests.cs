using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Agilis.Ads.IntegrationTests;

/// <summary>
/// A read-only run against the API in Test mode, with AGILIS_TEST_API_KEY set to a pk_test_ key.
/// It reads, and makes two dry-run calls that create and charge nothing. Without the key every
/// test returns at once.
/// </summary>
public class TestModeTests
{
    // Refusals that depend only on the Test organisation's own state, not on the SDK.
    private static readonly HashSet<string> AccountStateCodes = new() { "E3", "E4", "E5", "E6", "E9" };

    private readonly ITestOutputHelper _output;

    public TestModeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private AgilisClient? CreateClient()
    {
        var key = Environment.GetEnvironmentVariable("AGILIS_TEST_API_KEY")?.Trim();
        if (string.IsNullOrEmpty(key))
        {
            _output.WriteLine("AGILIS_TEST_API_KEY is not set: nothing to run.");
            return null;
        }

        if (!key.StartsWith("pk_test_", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("AGILIS_TEST_API_KEY must be a Test-mode (pk_test_) key; refusing to run.");
        }

        var client = new AgilisClient(key);
        Assert.Equal(AgilisMode.Test, client.Mode);
        return client;
    }

    private static string SampleImage => Path.Combine(AppContext.BaseDirectory, "sample-offer.png");

    [Fact]
    public async Task Reads()
    {
        using var client = CreateClient();
        if (client is null)
        {
            return;
        }

        var categories = await client.Categories.ListAsync();
        Assert.NotEmpty(categories.Categories);
        Assert.Equal(200, categories.HttpResponse!.StatusCode);

        await client.Interests.ListAsync();
        await client.EventCategories.ListAsync();
        await client.EventCurrencies.ListAsync();
        var config = await client.EventConfig.GetAsync();
        Assert.Equal("TEST", config.Environment);

        await client.Offers.ListAsync(new OfferListParams { Limit = 5 });
        await client.Events.ListAsync(new EventListParams { Limit = 5 });

        var firstThree = new List<Offer>();
        await foreach (var offer in client.Offers.ListAutoPagingAsync(new OfferListParams { Limit = 1 }))
        {
            firstThree.Add(offer);
            if (firstThree.Count == 3)
            {
                break;
            }
        }

        await client.WebhookEndpoints.ListAsync();
        await client.WebhookEvents.ListAsync(new WebhookEventListParams { Limit = 5 });
    }

    [Fact]
    public async Task Price_estimate()
    {
        using var client = CreateClient();
        if (client is null)
        {
            return;
        }

        var start = DateTimeOffset.UtcNow.AddDays(1);
        var estimate = await client.Offers.EstimatePriceAsync(new OfferPriceEstimateParams
        {
            StartDatetime = start,
            EndDatetime = start.AddDays(2),
            Location = new GeoLocation(51.5074, -0.1278),
            Radius = 10,
        });
        Assert.Equal("S1", estimate.ReturnCode);
    }

    [Fact]
    public async Task Offer_create_dry_run()
    {
        using var client = CreateClient();
        if (client is null)
        {
            return;
        }

        var categoryId = (await client.Categories.ListAsync()).Categories.First().CategoryId;
        var start = DateTimeOffset.UtcNow.AddDays(1);
        try
        {
            var result = await client.Offers.CreateAsync(new OfferCreateParams
            {
                CategoryId = categoryId,
                Title = "SDK dry run",
                Description = "A dry run from the .NET SDK tests. Nothing is created.",
                PushNotificationTitle = "SDK dry run",
                PushNotificationMessage = "A dry run from the .NET SDK tests.",
                Url = "https://www.agilis.dating",
                Image = FileUpload.FromPath(SampleImage),
                Location = new GeoLocation(51.5074, -0.1278),
                Radius = 10,
                StartDatetime = start,
                ExpiryDatetime = start.AddDays(1),
                PaymentMethod = PaymentMethod.Balance,
                DryRun = true,
            });
            Assert.True(result.DryRun);
            Assert.Null(result.OfferId);
        }
        catch (InvalidRequestException ex) when (ex.ReturnCode != null && AccountStateCodes.Contains(ex.ReturnCode))
        {
            _output.WriteLine($"Refused by the Test organisation's state ({ex.ReturnCode}).");
        }
    }

    [Fact]
    public async Task Event_publish_dry_run()
    {
        using var client = CreateClient();
        if (client is null)
        {
            return;
        }

        var day = DateTimeOffset.UtcNow.AddDays(14);
        var start = new DateTimeOffset(day.Year, day.Month, day.Day, 18, 0, 0, TimeSpan.Zero);
        try
        {
            var result = await client.Events.PublishAsync(new EventPublishParams
            {
                Title = "SDK dry run: evening social",
                Description = "A dry run from the .NET SDK tests. Nothing is published.",
                VenueName = "Test Venue",
                Address = "1 Test Street",
                City = "London",
                CountryCode = "GB",
                Location = new GeoLocation(51.5133, -0.13),
                StartsAt = start,
                EndsAt = start.AddHours(3),
                Url = "https://www.agilis.dating",
                Image = FileUpload.FromPath(SampleImage),
                DryRun = true,
            });
            Assert.True(result.DryRun);
            Assert.Null(result.EventId);
        }
        catch (InvalidRequestException ex) when (ex.ReturnCode != null && AccountStateCodes.Contains(ex.ReturnCode))
        {
            _output.WriteLine($"Refused by the Test organisation's state ({ex.ReturnCode}).");
        }
    }
}
