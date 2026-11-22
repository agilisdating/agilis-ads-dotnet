using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Agilis.Ads.Tests;

public class RetryTests
{
    private static OfferCreateParams Offer() => new()
    {
        CategoryId = 1,
        Title = "t",
        Description = "d",
        PushNotificationTitle = "title",
        PushNotificationMessage = "message",
        Url = "https://example.com",
        Image = FileUpload.FromStream(new MemoryStream(new byte[] { 1, 2, 3 }), "a.png"),
        Location = new GeoLocation(1, 2),
        Radius = 5,
        StartDatetime = DateTimeOffset.UtcNow.AddDays(1),
        ExpiryDatetime = DateTimeOffset.UtcNow.AddDays(2),
    };

    [Fact]
    public async Task Idempotency_key_and_body_are_kept_across_retries()
    {
        var (client, handler, waits) = TestClient.Create();
        handler.Respond(500, "{\"message\":\"oops\"}")
            .Throw(new HttpRequestException("connection reset"))
            .Respond(200, "{\"return_code\":\"S1\",\"offer_id\":7}");

        var result = await client.Offers.CreateAsync(Offer());

        Assert.Equal(7, result.OfferId);
        Assert.Equal(3, handler.Requests.Count);
        var keys = handler.Requests.Select(r => r.Header("Idempotency-Key")).Distinct().ToList();
        Assert.Single(keys);
        Assert.NotNull(keys[0]);
        Assert.All(handler.Requests, r => Assert.Equal(new byte[] { 1, 2, 3 }, r.Parts.Single(p => p.Name == "image").Bytes));
        Assert.Equal(2, waits.Count);
    }

    [Fact]
    public async Task Each_call_gets_its_own_idempotency_key()
    {
        var (client, handler, _) = TestClient.Create();
        var p = Offer();
        await client.Offers.CreateAsync(p);
        await client.Offers.CreateAsync(p);
        Assert.NotEqual(handler.Requests[0].Header("Idempotency-Key"), handler.Requests[1].Header("Idempotency-Key"));
    }

    [Theory]
    [InlineData(500)]
    [InlineData(502)]
    [InlineData(503)]
    [InlineData(504)]
    public async Task Get_is_retried_on_server_errors(int status)
    {
        var (client, handler, _) = TestClient.Create();
        handler.Respond(status, "{}").Respond(200, "{\"categories\":[]}");
        await client.Categories.ListAsync();
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task Delete_is_retried_on_server_errors()
    {
        var (client, handler, _) = TestClient.Create();
        handler.Respond(503, "{}").Respond(200, "{\"return_code\":\"S1\"}");
        await client.Offers.DeleteAsync(1);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task Post_without_idempotency_key_is_not_retried_on_5xx()
    {
        var (client, handler, _) = TestClient.Create();
        handler.Respond(500, "{\"message\":\"failed\"}");
        var ex = await Assert.ThrowsAsync<ApiException>(() => client.Offers.PauseAsync(1));
        Assert.Equal(500, ex.StatusCode);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Patch_is_not_retried_on_5xx()
    {
        var (client, handler, _) = TestClient.Create();
        handler.Respond(502, "{}");
        await Assert.ThrowsAsync<ApiException>(() => client.WebhookEndpoints.UpdateAsync("we_1", new WebhookEndpointUpdateParams { Status = "enabled" }));
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Status_429_is_retried_for_any_method_and_honours_retry_after()
    {
        var (client, handler, waits) = TestClient.Create();
        handler.Respond(429, "{\"message\":\"Too many\"}", ("Retry-After", "3")).Respond(200, "{\"return_code\":\"S1\"}");
        await client.Offers.PauseAsync(1);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(new[] { TimeSpan.FromSeconds(3) }, waits);
    }

    [Fact]
    public async Task Gives_up_after_max_retries_with_backoff_waits()
    {
        var (client, handler, waits) = TestClient.Create();
        handler.Respond(503, "{}").Respond(503, "{}").Respond(503, "{\"message\":\"down\"}");

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.Categories.ListAsync());

        Assert.Equal(503, ex.StatusCode);
        Assert.Equal("down", ex.Message);
        Assert.Equal(3, handler.Requests.Count);
        Assert.Equal(2, waits.Count);
        Assert.InRange(waits[0].TotalSeconds, 0.375, 0.625);
        Assert.InRange(waits[1].TotalSeconds, 0.75, 1.25);
    }

    [Fact]
    public async Task Max_retries_zero_sends_once()
    {
        var (client, handler, _) = TestClient.Create(maxRetries: 0);
        handler.Respond(503, "{}");
        await Assert.ThrowsAsync<ApiException>(() => client.Categories.ListAsync());
        Assert.Single(handler.Requests);
    }

    [Fact]
    public void Backoff_is_capped_at_eight_seconds()
    {
        var empty = new Dictionary<string, IReadOnlyList<string>>();
        for (var i = 0; i < 50; i++)
        {
            Assert.InRange(Internal.HttpCore.RetryWait(10, empty).TotalSeconds, 0, 8);
            Assert.InRange(Internal.HttpCore.RetryWait(3, empty).TotalSeconds, 3.0, 5.0);
        }
    }

    [Fact]
    public async Task Connection_errors_are_retried_then_raise_connection_exception()
    {
        var (client, handler, waits) = TestClient.Create();
        handler.Throw(new HttpRequestException("refused")).Throw(new HttpRequestException("refused")).Throw(new HttpRequestException("refused"));

        var ex = await Assert.ThrowsAsync<ConnectionException>(() => client.Offers.PauseAsync(1));

        Assert.Null(ex.StatusCode);
        Assert.Equal(3, handler.Requests.Count);
        Assert.Equal(2, waits.Count);
    }

    [Fact]
    public async Task Timeouts_are_retried_then_raise_connection_exception()
    {
        var (client, handler, _) = TestClient.Create(maxRetries: 1, timeout: TimeSpan.FromMilliseconds(50));
        handler.Hang().Hang();

        var ex = await Assert.ThrowsAsync<ConnectionException>(() => client.Categories.ListAsync());

        Assert.Contains("timed out", ex.Message);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task Caller_cancellation_is_not_retried()
    {
        var (client, handler, _) = TestClient.Create();
        handler.Hang();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.Categories.ListAsync(cts.Token));
        Assert.Single(handler.Requests);
    }
}

public class ErrorTests
{
    [Theory]
    [InlineData(400, typeof(InvalidRequestException))]
    [InlineData(422, typeof(InvalidRequestException))]
    [InlineData(401, typeof(AuthenticationException))]
    [InlineData(403, typeof(PermissionException))]
    [InlineData(404, typeof(NotFoundException))]
    [InlineData(409, typeof(ConflictException))]
    [InlineData(429, typeof(RateLimitException))]
    [InlineData(500, typeof(ApiException))]
    [InlineData(418, typeof(ApiException))]
    public async Task Status_maps_to_error_type(int status, Type expected)
    {
        var (client, handler, _) = TestClient.Create(maxRetries: 0);
        handler.Respond(status, "{\"return_code\":\"E1\",\"message\":\"Something is wrong\"}", ("X-RateLimit-Remaining", "12"));

        var ex = await Assert.ThrowsAnyAsync<AgilisException>(() => client.Offers.PauseAsync(1));

        Assert.IsType(expected, ex);
        Assert.Equal(status, ex.StatusCode);
        Assert.Equal("E1", ex.ReturnCode);
        Assert.Equal("Something is wrong", ex.Message);
        Assert.Equal("Something is wrong", ex.Body!.Value.GetProperty("message").GetString());
        Assert.Equal("12", ex.Headers["x-ratelimit-remaining"].Single());
    }

    [Fact]
    public async Task Rate_limit_error_carries_retry_after()
    {
        var (client, handler, _) = TestClient.Create(maxRetries: 0);
        handler.Respond(429, "{\"return_code\":\"E8\",\"message\":\"Too many previews\"}", ("Retry-After", "42"));
        var ex = await Assert.ThrowsAsync<RateLimitException>(() => client.Events.PreviewAsync(new EventPreviewParams()));
        Assert.Equal(TimeSpan.FromSeconds(42), ex.RetryAfter);
        Assert.Equal("E8", ex.ReturnCode);
    }

    [Fact]
    public async Task Http_200_with_E_code_is_an_invalid_request()
    {
        var (client, handler, _) = TestClient.Create();
        handler.Respond(200, "{\"return_code\":\"E3\",\"message\":\"Your balance is too low.\"}");
        var ex = await Assert.ThrowsAsync<InvalidRequestException>(() => client.Offers.PauseAsync(1));
        Assert.Equal(200, ex.StatusCode);
        Assert.Equal("E3", ex.ReturnCode);
        Assert.Equal("Your balance is too low.", ex.Message);
        Assert.Single(handler.Requests);
    }

    [Theory]
    [InlineData("S1")]
    [InlineData("S2")]
    public async Task S1_and_S2_are_success(string code)
    {
        var (client, handler, _) = TestClient.Create();
        handler.Respond(200, "{\"return_code\":\"" + code + "\",\"message\":\"Offer resumed\"}");
        var result = await client.Offers.ResumeAsync(1);
        Assert.Equal(code, result.ReturnCode);
        Assert.Equal("Offer resumed", result.Message);
    }

    [Fact]
    public async Task Error_without_json_still_has_a_message()
    {
        var (client, handler, _) = TestClient.Create(maxRetries: 0);
        handler.Respond(502, "<html>Bad gateway</html>");
        var ex = await Assert.ThrowsAsync<ApiException>(() => client.Offers.PauseAsync(1));
        Assert.Equal("The Agilis API returned HTTP 502.", ex.Message);
        Assert.Null(ex.Body);
    }

    [Fact]
    public async Task Api_key_never_appears_in_error_text()
    {
        var (client, handler, _) = TestClient.Create(maxRetries: 0);
        handler.Respond(401, "{\"message\":\"Unknown key " + TestClient.TestKey + "\"}");
        var ex = await Assert.ThrowsAsync<AuthenticationException>(() => client.Categories.ListAsync());
        Assert.DoesNotContain(TestClient.TestKey, ex.Message);
        Assert.DoesNotContain(TestClient.TestKey, ex.ToString());
        Assert.Contains("[redacted]", ex.Message);

        handler.Throw(new HttpRequestException("failed with " + TestClient.TestKey));
        var conn = await Assert.ThrowsAsync<ConnectionException>(() => client.Categories.ListAsync());
        Assert.DoesNotContain(TestClient.TestKey, conn.Message);

        Assert.DoesNotContain(TestClient.TestKey, client.ToString());
        Assert.DoesNotContain(TestClient.TestKey, handler.Requests[0].Uri.ToString());
    }
}

public class ModeTests
{
    [Fact]
    public void Mode_comes_from_the_key()
    {
        Assert.Equal(AgilisMode.Test, new AgilisClient(TestClient.TestKey).Mode);
        Assert.Equal(AgilisMode.Live, new AgilisClient(TestClient.LiveKey).Mode);
        Assert.True(new AgilisClient(TestClient.LiveKey).IsLive);
        Assert.False(new AgilisClient(TestClient.TestKey).IsLive);
    }

    [Fact]
    public void Unknown_key_prefix_is_refused_without_echoing_the_key()
    {
        var ex = Assert.Throws<ArgumentException>(() => new AgilisClient("sk_secret_value_12345"));
        Assert.DoesNotContain("sk_secret_value_12345", ex.Message);
    }

    [Fact]
    public void Key_is_read_from_the_environment()
    {
        var previous = Environment.GetEnvironmentVariable("AGILIS_API_KEY");
        try
        {
            Environment.SetEnvironmentVariable("AGILIS_API_KEY", TestClient.LiveKey);
            using var client = new AgilisClient();
            Assert.Equal(AgilisMode.Live, client.Mode);

            Environment.SetEnvironmentVariable("AGILIS_API_KEY", null);
            Assert.Throws<ArgumentException>(() => new AgilisClient());
        }
        finally
        {
            Environment.SetEnvironmentVariable("AGILIS_API_KEY", previous);
        }
    }

    [Fact]
    public async Task Base_url_option_is_used()
    {
        var handler = new FakeHandler();
        using var client = new AgilisClient(new AgilisClientOptions
        {
            ApiKey = TestClient.TestKey,
            BaseUrl = new Uri("https://sandbox.example.test/v1/"),
            HttpClient = new HttpClient(handler),
        });
        await client.Interests.ListAsync();
        Assert.Equal("https://sandbox.example.test/v1/interests", handler.Requests[0].Uri.ToString());
    }

    [Fact]
    public void Defaults_match_the_specification()
    {
        var options = new AgilisClientOptions();
        Assert.Equal(new Uri("https://ads.agilis.dating"), options.BaseUrl);
        Assert.Equal(TimeSpan.FromSeconds(60), options.Timeout);
        Assert.Equal(2, options.MaxRetries);
        Assert.Null(options.HttpClient);
    }
}

public class PaginationTests
{
    [Fact]
    public async Task Auto_paging_follows_next_cursor_until_has_more_is_false()
    {
        var (client, handler, _) = TestClient.Create();
        handler.Respond(200, "{\"data\":[{\"offer_id\":1},{\"offer_id\":2}],\"pagination\":{\"limit\":2,\"count\":2,\"has_more\":true,\"next_cursor\":\"c2\"}}")
            .Respond(200, "{\"data\":[{\"offer_id\":3}],\"pagination\":{\"limit\":2,\"count\":1,\"has_more\":false,\"next_cursor\":null}}");

        var ids = new List<int>();
        var parameters = new OfferListParams { Limit = 2, OfferStatus = "active" };
        await foreach (var offer in client.Offers.ListAutoPagingAsync(parameters))
        {
            ids.Add(offer.OfferId);
        }

        Assert.Equal(new[] { 1, 2, 3 }, ids);
        Assert.Equal("/offers?environment=test&limit=2&offer_status=active", handler.Requests[0].PathAndQuery);
        Assert.Equal("/offers?environment=test&limit=2&cursor=c2&offer_status=active", handler.Requests[1].PathAndQuery);
        Assert.Null(parameters.Cursor);
    }

    [Fact]
    public async Task Events_and_webhook_events_auto_page()
    {
        var (client, handler, _) = TestClient.Create();
        handler.Respond(200, "{\"data\":[{\"event_id\":1}],\"pagination\":{\"has_more\":true,\"next_cursor\":\"n\"}}")
            .Respond(200, "{\"data\":[{\"event_id\":2}],\"pagination\":{\"has_more\":false}}")
            .Respond(200, "{\"return_code\":\"S1\",\"data\":[{\"id\":\"whevt_1\",\"type\":\"offer.started\",\"created\":1795075200}],\"pagination\":{\"has_more\":true,\"next_cursor\":\"w\"}}")
            .Respond(200, "{\"return_code\":\"S1\",\"data\":[],\"pagination\":{\"has_more\":false}}");

        var events = new List<int>();
        await foreach (var e in client.Events.ListAutoPagingAsync())
        {
            events.Add(e.EventId);
        }

        var webhookEvents = new List<string?>();
        await foreach (var e in client.WebhookEvents.ListAutoPagingAsync(new WebhookEventListParams { Limit = 1 }))
        {
            webhookEvents.Add(e.Id);
        }

        Assert.Equal(new[] { 1, 2 }, events);
        Assert.Equal(new[] { "whevt_1" }, webhookEvents);
        Assert.Equal("/events?environment=test&cursor=n", handler.Requests[1].PathAndQuery);
        Assert.Equal("/webhook-events?limit=1&cursor=w", handler.Requests[3].PathAndQuery);
    }

    [Fact]
    public async Task Single_page_exposes_paging_block()
    {
        var (client, handler, _) = TestClient.Create();
        handler.Respond(200, "{\"data\":[],\"pagination\":{\"limit\":20,\"count\":0,\"has_more\":false,\"next_cursor\":null}}");
        var page = await client.Offers.ListAsync();
        Assert.False(page.HasMore);
        Assert.Null(page.NextCursor);
        Assert.Equal(20, page.Pagination.Limit);
        Assert.Empty(page.Data);
    }
}
