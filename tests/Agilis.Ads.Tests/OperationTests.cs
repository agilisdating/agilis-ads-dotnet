using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Agilis.Ads.Tests;

public class OperationTests
{
    private static readonly DateTimeOffset Start = new(2027, 7, 10, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset End = new(2027, 7, 12, 20, 30, 0, TimeSpan.Zero);

    public static IEnumerable<object[]> SimpleOperations()
    {
        yield return Op("listCategories", c => c.Categories.ListAsync(), "GET", "/categories");
        yield return Op("listInterests", c => c.Interests.ListAsync(), "GET", "/interests");
        yield return Op("listOffers", c => c.Offers.ListAsync(), "GET", "/offers?environment=test");
        yield return Op("listOffersFiltered", c => c.Offers.ListAsync(new OfferListParams
        {
            Limit = 50,
            Cursor = "abc",
            OfferStatus = "active",
            CreatedAfter = Start,
            CreatedBefore = End,
        }), "GET", "/offers?environment=test&limit=50&cursor=abc&offer_status=active&created_after=2027-07-10T08%3A00%3A00Z&created_before=2027-07-12T20%3A30%3A00Z");
        yield return Op("pauseOffer", c => c.Offers.PauseAsync(45), "POST", "/offers/45/pause");
        yield return Op("resumeOffer", c => c.Offers.ResumeAsync(45), "POST", "/offers/45/resume");
        yield return Op("getOfferAnalytics", c => c.Offers.GetAnalyticsAsync(45, AnalyticsInterval.Last30Days), "GET", "/offers/45/analytics?interval=30d");
        yield return Op("deleteOffer", c => c.Offers.DeleteAsync(45), "DELETE", "/offers/45");
        yield return Op("deleteOfferDryRun", c => c.Offers.DeleteAsync(45, dryRun: true), "DELETE", "/offers/45?dry_run=true");
        yield return Op("listEventCategories", c => c.EventCategories.ListAsync(), "GET", "/event-categories");
        yield return Op("listEventCurrencies", c => c.EventCurrencies.ListAsync(), "GET", "/event-currencies");
        yield return Op("getEventConfig", c => c.EventConfig.GetAsync(), "GET", "/event-config");
        yield return Op("listEvents", c => c.Events.ListAsync(new EventListParams { EventStatus = "upcoming", OwnerUserId = 41, Limit = 10 }), "GET", "/events?environment=test&limit=10&event_status=upcoming&owner_user_id=41");
        yield return Op("getEvent", c => c.Events.GetAsync(3104), "GET", "/events/3104");
        yield return Op("deleteEvent", c => c.Events.DeleteAsync(3104), "DELETE", "/events/3104");
        yield return Op("deleteEventDryRun", c => c.Events.DeleteAsync(3104, dryRun: true), "DELETE", "/events/3104?dry_run=true");
        yield return Op("cancelEvent", c => c.Events.CancelAsync(3104), "POST", "/events/3104/cancel");
        yield return Op("cancelEventDryRun", c => c.Events.CancelAsync(3104, dryRun: true), "POST", "/events/3104/cancel?dry_run=true");
        yield return Op("getEventAnalytics", c => c.Events.GetAnalyticsAsync(3104, AnalyticsInterval.All), "GET", "/events/3104/analytics?interval=all");
        yield return Op("listWebhookEndpoints", c => c.WebhookEndpoints.ListAsync(), "GET", "/webhook-endpoints");
        yield return Op("getWebhookEndpoint", c => c.WebhookEndpoints.GetAsync("we_123"), "GET", "/webhook-endpoints/we_123");
        yield return Op("deleteWebhookEndpoint", c => c.WebhookEndpoints.DeleteAsync("we_123"), "DELETE", "/webhook-endpoints/we_123");
        yield return Op("listWebhookEvents", c => c.WebhookEvents.ListAsync(new WebhookEventListParams { Type = "offer.started", DeliverySuccess = false, Limit = 5, Cursor = "c1" }), "GET", "/webhook-events?type=offer.started&delivery_success=false&limit=5&cursor=c1");
        yield return Op("getWebhookEvent", c => c.WebhookEvents.GetAsync("whevt_1"), "GET", "/webhook-events/whevt_1");
        yield return Op("escapedId", c => c.WebhookEndpoints.GetAsync("a/b c"), "GET", "/webhook-endpoints/a%2Fb%20c");
    }

    private static object[] Op(string name, Func<AgilisClient, Task> call, string method, string pathAndQuery) =>
        new object[] { name, call, method, pathAndQuery };

    [Theory]
    [MemberData(nameof(SimpleOperations))]
    public async Task Operation_sends_method_path_and_query(string name, Func<AgilisClient, Task> call, string method, string pathAndQuery)
    {
        var (client, handler, _) = TestClient.Create();
        handler.Fallback = (_, _) => Task.FromResult(FakeHandler.Build(200, "{\"return_code\":\"S1\"}"));

        await call(client);

        var request = Assert.Single(handler.Requests);
        Assert.True(method == request.Method.Method, name);
        Assert.Equal(pathAndQuery, request.PathAndQuery);
        Assert.Equal("ads.agilis.dating", request.Uri.Host);
        Assert.Equal(TestClient.TestKey, request.Header("x-api-key"));
        Assert.Equal("application/json", request.Header("Accept"));
        Assert.Null(request.Header("Idempotency-Key"));
    }

    [Fact]
    public void User_agent_names_sdk_version_and_runtime()
    {
        Assert.Matches(@"^agilis-ads-dotnet/0\.1\.0 \(\.NET.*\)$", AgilisClient.UserAgent);
        var assemblyVersion = typeof(AgilisClient).Assembly.GetName().Version!;
        Assert.Equal(AgilisClient.Version, $"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}");
    }

    [Fact]
    public async Task Sends_user_agent_header()
    {
        var (client, handler, _) = TestClient.Create();
        await client.Categories.ListAsync();
        Assert.Equal(AgilisClient.UserAgent, handler.Requests[0].Header("User-Agent"));
    }

    [Fact]
    public async Task Live_key_fills_live_environment()
    {
        var (client, handler, _) = TestClient.Create(TestClient.LiveKey);
        handler.Respond(200, "{\"data\":[],\"pagination\":{\"has_more\":false}}");
        await client.Events.ListAsync();
        Assert.Equal("/events?environment=live", handler.Requests[0].PathAndQuery);
    }

    [Fact]
    public async Task Price_estimate_sends_json_body()
    {
        var (client, handler, _) = TestClient.Create();
        handler.Respond(200, "{\"return_code\":\"S1\",\"price_estimate\":110110,\"matching_users\":1820,\"ad_units\":110110,\"ad_unit_price\":0.001,\"estimated_cost\":110.11,\"currency\":\"GBP\",\"exchange_rate_per_gbp\":1,\"exchange_rate_date\":null}");

        var estimate = await client.Offers.EstimatePriceAsync(new OfferPriceEstimateParams
        {
            StartDatetime = Start,
            EndDatetime = End,
            Location = new GeoLocation(51.5074, -0.1278),
            Radius = 10,
            Gender = new[] { Gender.Male, Gender.Female },
            Interests = new[] { 3, 7 },
        });

        var request = handler.Requests[0];
        Assert.Equal("POST", request.Method.Method);
        Assert.Equal("/offers/price-estimate", request.PathAndQuery);
        Assert.Equal("application/json", request.ContentType);
        using var doc = JsonDocument.Parse(request.Body!);
        var root = doc.RootElement;
        Assert.Equal("2027-07-10T08:00:00Z", root.GetProperty("start_datetime").GetString());
        Assert.Equal("2027-07-12T20:30:00Z", root.GetProperty("end_datetime").GetString());
        Assert.Equal(51.5074, root.GetProperty("location")[0].GetDouble());
        Assert.Equal(-0.1278, root.GetProperty("location")[1].GetDouble());
        Assert.Equal(10, root.GetProperty("radius").GetInt32());
        Assert.Equal(new[] { "Male", "Female" }, root.GetProperty("gender").EnumerateArray().Select(e => e.GetString()));
        Assert.Equal(new[] { 3, 7 }, root.GetProperty("interests").EnumerateArray().Select(e => e.GetInt32()));
        Assert.Equal("TEST", root.GetProperty("environment").GetString());

        Assert.Equal(110.11m, estimate.EstimatedCost);
        Assert.Equal(1820, estimate.MatchingUsers);
        Assert.Null(estimate.ExchangeRateDate);
        Assert.Equal(200, estimate.HttpResponse!.StatusCode);
    }

    [Fact]
    public async Task Offer_create_sends_multipart_with_repeated_array_parts_and_image()
    {
        var (client, handler, _) = TestClient.Create();
        handler.Respond(200, "{\"return_code\":\"S1\",\"offer_id\":45,\"status\":\"Awaiting Payment\",\"payment_url\":\"https://checkout.example/x\",\"currency\":\"GBP\"}");
        var image = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "TestData", "sample-offer.png"));

        var result = await client.Offers.CreateAsync(new OfferCreateParams
        {
            CategoryId = 1,
            Title = "20% off summer sale",
            Description = "Discount valid through July",
            TermsAndConditions = "Valid online only.",
            PushNotificationTitle = "20% off this summer",
            PushNotificationMessage = "Show this at the till for 20% off.",
            ButtonPreset = ButtonPreset.BookNow,
            Url = "https://example.com/offer",
            Image = FileUpload.FromBytes(image, "sample-offer.png"),
            Location = new GeoLocation(51.5074, -0.1278),
            Radius = 10,
            Gender = new[] { Gender.Male, Gender.Female },
            Interests = new[] { 3, 7 },
            StartDatetime = Start,
            ExpiryDatetime = End,
            PaymentMethod = PaymentMethod.StripeLink,
        });

        var request = handler.Requests[0];
        Assert.Equal("POST", request.Method.Method);
        Assert.Equal("/offers", request.PathAndQuery);
        Assert.Equal("multipart/form-data", request.ContentType);
        Assert.Equal("1", request.Value("category_id"));
        Assert.Equal("20% off summer sale", request.Value("title"));
        Assert.Equal("Valid online only.", request.Value("terms_and_conditions"));
        Assert.Equal("book_now", request.Value("button_preset"));
        Assert.Null(request.Value("button_text"));
        Assert.Null(request.Value("advertised_name"));
        Assert.Equal(new[] { "51.5074", "-0.1278" }, request.Values("location[]"));
        Assert.Equal(new[] { "Male", "Female" }, request.Values("gender[]"));
        Assert.Equal(new[] { "3", "7" }, request.Values("interests[]"));
        Assert.Equal("10", request.Value("radius"));
        Assert.Equal("2027-07-10T08:00:00Z", request.Value("start_datetime"));
        Assert.Equal("2027-07-12T20:30:00Z", request.Value("expiry_datetime"));
        Assert.Equal("E", request.Value("payment_method"));
        Assert.Equal("TEST", request.Value("environment"));

        var file = Assert.Single(request.Parts, p => p.Name == "image");
        Assert.Equal("sample-offer.png", file.FileName);
        Assert.Equal("image/png", file.ContentType);
        Assert.Equal(image, file.Bytes);

        Assert.True(Guid.TryParse(request.Header("Idempotency-Key"), out var key));
        Assert.Equal('4', key.ToString()[14]);
        Assert.Equal(45, result.OfferId);
        Assert.Equal("Awaiting Payment", result.Status);
    }

    [Fact]
    public async Task Offer_create_uses_callers_idempotency_key_and_dry_run()
    {
        var (client, handler, _) = TestClient.Create();
        handler.Respond(200, "{\"return_code\":\"S1\",\"dry_run\":true,\"price\":12.5,\"matching_members\":900,\"balance_after\":30}");

        var result = await client.Offers.CreateAsync(new OfferCreateParams
        {
            CategoryId = 1,
            Title = "t",
            Description = "d",
            PushNotificationTitle = "title",
            PushNotificationMessage = "message",
            Url = "https://example.com",
            Image = FileUpload.FromStream(new MemoryStream(new byte[] { 1, 2, 3 }), "a.jpg"),
            Location = new GeoLocation(1, 2),
            Radius = 5,
            StartDatetime = Start,
            ExpiryDatetime = End,
            Environment = "TEST",
            IdempotencyKey = "my-key-1",
            DryRun = true,
        });

        var request = handler.Requests[0];
        Assert.Equal("/offers?dry_run=true", request.PathAndQuery);
        Assert.Equal("my-key-1", request.Header("Idempotency-Key"));
        Assert.Equal("B", request.Value("payment_method"));
        Assert.Equal("image/jpeg", request.Parts.Single(p => p.Name == "image").ContentType);
        Assert.Empty(request.Values("gender[]"));
        Assert.True(result.DryRun);
        Assert.Equal(12.5m, result.Price);
        Assert.Equal(900, result.MatchingMembers);
    }

    [Fact]
    public async Task Event_preview_sends_crop_fields_and_environment()
    {
        var (client, handler, _) = TestClient.Create();
        handler.Respond(200, "{\"return_code\":\"S1\",\"category\":{\"category_id\":9,\"category_name\":\"Classes\"},\"image\":{\"preview_token\":\"evp_1\",\"crop\":{\"x\":0,\"y\":225,\"w\":2400,\"h\":1350}}}");

        var preview = await client.Events.PreviewAsync(new EventPreviewParams
        {
            Title = "Salsa",
            Image = FileUpload.FromBytes(new byte[] { 9 }, "e.png"),
            Crop = new ImageCrop(0, 225, 2400),
        });

        var request = handler.Requests[0];
        Assert.Equal("POST", request.Method.Method);
        Assert.Equal("/events/preview", request.PathAndQuery);
        Assert.Null(request.Header("Idempotency-Key"));
        Assert.Equal("Salsa", request.Value("title"));
        Assert.Equal("0", request.Value("crop[x]"));
        Assert.Equal("225", request.Value("crop[y]"));
        Assert.Equal("2400", request.Value("crop[w]"));
        Assert.Equal("TEST", request.Value("environment"));
        Assert.Null(request.Value("payment_method"));
        Assert.Equal("evp_1", preview.Image!.PreviewToken);
        Assert.Equal(1350, preview.Image.Crop!.H);
    }

    [Fact]
    public async Task Event_publish_sends_all_fields_with_idempotency_key()
    {
        var (client, handler, _) = TestClient.Create();
        handler.Respond(200, "{\"return_code\":\"S1\",\"event_id\":3104,\"status\":\"Scheduled\",\"payment_url\":null,\"amount_charged\":10,\"currency\":\"GBP\"}");

        var result = await client.Events.PublishAsync(new EventPublishParams
        {
            Title = "Beginners Cuban Salsa",
            Description = "Drop-in class",
            VenueName = "Bar Salsa",
            Address = "96 Charing Cross Road",
            City = "London",
            CountryCode = "GB",
            Location = new GeoLocation(51.5133, -0.13),
            StartsAt = new DateTimeOffset(2027, 10, 16, 19, 30, 0, TimeSpan.FromHours(1)),
            EndsAt = new DateTimeOffset(2027, 10, 16, 23, 0, 0, TimeSpan.Zero),
            Url = "https://example.com/book",
            PriceMin = 12m,
            PriceMax = 12.5m,
            Currency = "GBP",
            PreviewToken = "evp_1",
            DryRun = true,
        });

        var request = handler.Requests[0];
        Assert.Equal("/events?dry_run=true", request.PathAndQuery);
        Assert.NotNull(request.Header("Idempotency-Key"));
        Assert.Equal("Bar Salsa", request.Value("venue_name"));
        Assert.Equal("96 Charing Cross Road", request.Value("address"));
        Assert.Equal("GB", request.Value("country_code"));
        Assert.Equal(new[] { "51.5133", "-0.13" }, request.Values("location[]"));
        Assert.Equal("2027-10-16T18:30:00Z", request.Value("starts_at"));
        Assert.Equal("2027-10-16T23:00:00Z", request.Value("ends_at"));
        Assert.Equal("12", request.Value("price_min"));
        Assert.Equal("12.5", request.Value("price_max"));
        Assert.Equal("evp_1", request.Value("preview_token"));
        Assert.Equal("B", request.Value("payment_method"));
        Assert.Equal("TEST", request.Value("environment"));
        Assert.DoesNotContain(request.Parts, p => p.Name == "image");
        Assert.Equal(3104, result.EventId);
        Assert.Equal(10m, result.AmountCharged);
    }

    [Fact]
    public async Task Event_edit_without_file_is_a_multipart_post_with_method_override()
    {
        var (client, handler, _) = TestClient.Create();
        handler.Respond(200, "{\"return_code\":\"S1\",\"event_id\":3104,\"changed_fields\":[\"title\"]}");

        var result = await client.Events.EditAsync(3104, new EventEditParams { Title = "New title", ClearPrice = true, Crop = new ImageCrop(10, 20, 1280) });

        var request = handler.Requests[0];
        Assert.Equal("POST", request.Method.Method);
        Assert.Equal("/events/3104", request.PathAndQuery);
        Assert.Equal("PATCH", request.Header("X-HTTP-Method-Override"));
        Assert.Equal("multipart/form-data", request.ContentType);
        Assert.Equal("New title", request.Value("title"));
        Assert.Equal("", request.Value("price_min"));
        Assert.Equal("", request.Value("price_max"));
        Assert.Equal("", request.Value("currency"));
        Assert.Equal("1280", request.Value("crop[w]"));
        Assert.Null(request.Value("environment"));
        Assert.DoesNotContain(request.Parts, p => p.Name == "image");
        Assert.Equal(new[] { "title" }, result.ChangedFields);
    }

    [Fact]
    public async Task Event_edit_with_file_is_a_multipart_post_with_method_override()
    {
        var (client, handler, _) = TestClient.Create();
        handler.Respond(200, "{\"return_code\":\"S1\"}");

        await client.Events.EditAsync(3104, new EventEditParams { Image = FileUpload.FromBytes(new byte[] { 1 }, "new.jpg") });

        var request = handler.Requests[0];
        Assert.Equal("POST", request.Method.Method);
        Assert.Equal("/events/3104", request.PathAndQuery);
        Assert.Equal("PATCH", request.Header("X-HTTP-Method-Override"));
        Assert.Equal("multipart/form-data", request.ContentType);
        Assert.Equal("new.jpg", request.Parts.Single(p => p.Name == "image").FileName);
    }

    [Fact]
    public async Task Event_reschedule_sends_json()
    {
        var (client, handler, _) = TestClient.Create();
        handler.Respond(200, "{\"return_code\":\"S1\",\"event_id\":3104,\"starts_at\":\"2027-10-23T18:30:00Z\"}");

        var result = await client.Events.RescheduleAsync(3104, new EventRescheduleParams { StartsAt = Start, EndsAt = End });

        var request = handler.Requests[0];
        Assert.Equal("POST", request.Method.Method);
        Assert.Equal("/events/3104/reschedule", request.PathAndQuery);
        Assert.Equal("{\"starts_at\":\"2027-07-10T08:00:00Z\",\"ends_at\":\"2027-07-12T20:30:00Z\"}", request.Body);
        Assert.Equal(new DateTimeOffset(2027, 10, 23, 18, 30, 0, TimeSpan.Zero), result.StartsAt);
    }

    [Fact]
    public async Task Webhook_endpoint_create_update_roll_test_and_resend_send_json()
    {
        var (client, handler, _) = TestClient.Create();

        var created = await WithResponse(handler, "{\"return_code\":\"S1\",\"id\":\"we_1\",\"secret\":\"whsec_x\",\"livemode\":false}",
            () => client.WebhookEndpoints.CreateAsync(new WebhookEndpointCreateParams
            {
                Url = "https://example.com/hook",
                EnabledEvents = new[] { WebhookEventTypes.OfferStarted, WebhookEventTypes.EventPublished },
                Description = "Main",
            }));
        Assert.Equal("whsec_x", created.Secret);
        AssertJson(handler.Requests[0], "POST", "/webhook-endpoints",
            "{\"url\":\"https://example.com/hook\",\"enabled_events\":[\"offer.started\",\"event.published\"],\"description\":\"Main\"}");

        await WithResponse(handler, "{\"return_code\":\"S1\",\"id\":\"we_1\"}",
            () => client.WebhookEndpoints.UpdateAsync("we_1", new WebhookEndpointUpdateParams { Status = "disabled" }));
        AssertJson(handler.Requests[1], "PATCH", "/webhook-endpoints/we_1", "{\"status\":\"disabled\"}");

        await WithResponse(handler, "{\"return_code\":\"S1\",\"id\":\"we_1\",\"secret\":\"whsec_y\"}",
            () => client.WebhookEndpoints.RollSecretAsync("we_1", new WebhookSecretRollParams { ExpiresIn = SecretExpiry.OneDay }));
        AssertJson(handler.Requests[2], "POST", "/webhook-endpoints/we_1/roll-secret", "{\"expires_in\":\"24h\"}");

        var test = await WithResponse(handler, "{\"return_code\":\"S1\",\"webhook_event_id\":\"whevt_t\",\"test\":true,\"delivery\":{\"endpoint_id\":\"we_1\",\"status\":\"delivered\",\"attempts\":1,\"last_http_status\":200}}",
            () => client.WebhookEndpoints.SendTestAsync("we_1", new WebhookTestParams { Type = WebhookEventTypes.OfferCreated }));
        AssertJson(handler.Requests[3], "POST", "/webhook-endpoints/we_1/test", "{\"type\":\"offer.created\"}");
        Assert.Equal(200, test.Delivery!.LastHttpStatus);

        await WithResponse(handler, "{\"return_code\":\"S1\",\"webhook_event_id\":\"whevt_1\",\"deliveries\":[]}",
            () => client.WebhookEvents.ResendAsync("whevt_1", new WebhookEventResendParams { EndpointId = "we_1" }));
        AssertJson(handler.Requests[4], "POST", "/webhook-events/whevt_1/resend", "{\"endpoint_id\":\"we_1\"}");

        await WithResponse(handler, "{\"return_code\":\"S1\"}", () => client.WebhookEndpoints.SendTestAsync("we_1"));
        AssertJson(handler.Requests[5], "POST", "/webhook-endpoints/we_1/test", "{}");
    }

    [Fact]
    public async Task Unknown_fields_are_kept_and_headers_reachable()
    {
        var (client, handler, _) = TestClient.Create();
        handler.Respond(200, "{\"categories\":[{\"category_id\":1,\"category_name\":\"Food\",\"brand_new\":true}],\"also_new\":{\"a\":1}}",
            ("X-RateLimit-Remaining", "57"), ("X-RateLimit-Limit", "60"));

        var list = await client.Categories.ListAsync();

        Assert.Equal("Food", list.Categories[0].CategoryName);
        Assert.True(list.Categories[0].ExtensionData!["brand_new"].GetBoolean());
        Assert.Equal(1, list.ExtensionData!["also_new"].GetProperty("a").GetInt32());
        Assert.Equal(57, list.HttpResponse!.RateLimitRemaining);
        Assert.Equal(60, list.HttpResponse.RateLimitLimit);
        Assert.Equal("57", list.HttpResponse.GetHeader("x-ratelimit-remaining"));
    }

    [Fact]
    public async Task Event_details_parse_with_history_and_bands()
    {
        var (client, handler, _) = TestClient.Create();
        handler.Respond(200, "{\"event_id\":3104,\"title\":\"Salsa\",\"attendees\":\"10-25\",\"price_min\":12,\"can_delete\":false,\"listing_price\":10,\"image_master\":{\"width\":2400,\"crop\":{\"x\":0,\"y\":225,\"w\":2400,\"h\":1350},\"url\":null},\"history\":[{\"action\":\"published\",\"details\":{\"environment\":1},\"at\":\"2027-09-20T10:00:00Z\"}],\"starts_at\":\"2027-10-16T18:30:00Z\",\"last_edited_at\":null}");

        var evt = await client.Events.GetAsync(3104);

        Assert.Equal("10-25", evt.Attendees);
        Assert.Equal(12m, evt.PriceMin);
        Assert.False(evt.CanDelete);
        Assert.Equal(2400, evt.ImageMaster!.Width);
        Assert.Equal("published", evt.History![0].Action);
        Assert.Equal(1, evt.History[0].Details!.Value.GetProperty("environment").GetInt32());
        Assert.Equal(new DateTimeOffset(2027, 10, 16, 18, 30, 0, TimeSpan.Zero), evt.StartsAt);
        Assert.Null(evt.LastEditedAt);
    }

    [Fact]
    public async Task Event_analytics_accept_bands_as_strings_or_numbers()
    {
        var (client, handler, _) = TestClient.Create();
        handler.Respond(200, "{\"event_id\":3104,\"metrics\":{\"clicks\":188,\"attendees\":\"26-50\",\"invites_sent\":12,\"invites_accepted\":\"10-25\"}}");

        var analytics = await client.Events.GetAnalyticsAsync(3104, AnalyticsInterval.Last30Days);

        Assert.Equal(188, analytics.Metrics!.Clicks);
        Assert.Equal("26-50", analytics.Metrics.Attendees);
        Assert.Equal("12", analytics.Metrics.InvitesSent);
    }

    private static async Task<T> WithResponse<T>(FakeHandler handler, string json, Func<Task<T>> call)
    {
        handler.Respond(200, json);
        return await call();
    }

    private static void AssertJson(CapturedRequest request, string method, string path, string json)
    {
        Assert.Equal(method, request.Method.Method);
        Assert.Equal(path, request.PathAndQuery);
        Assert.Equal("application/json", request.ContentType);
        Assert.Equal(json, request.Body);
    }
}
