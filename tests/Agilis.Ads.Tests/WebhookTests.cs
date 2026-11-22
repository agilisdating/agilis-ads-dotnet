using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Agilis.Ads.Tests;

public class WebhookTests
{
    private static readonly JsonElement Vectors = LoadVectors();

    private static JsonElement LoadVectors()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "webhook-signature-vectors.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(path, Encoding.UTF8));
        return doc.RootElement.Clone();
    }

    private static string Body => Vectors.GetProperty("body").GetString()!;

    private static string Secret => Vectors.GetProperty("secret").GetString()!;

    private static DateTimeOffset Now => DateTimeOffset.FromUnixTimeSeconds(Vectors.GetProperty("now_unix").GetInt64());

    private static int Tolerance => Vectors.GetProperty("tolerance_seconds").GetInt32();

    public static IEnumerable<object[]> Cases() =>
        Vectors.GetProperty("cases").EnumerateArray().Select(c => new object[] { c.GetProperty("name").GetString()! });

    [Theory]
    [MemberData(nameof(Cases))]
    public void Shared_signature_vectors(string name)
    {
        var c = Vectors.GetProperty("cases").EnumerateArray().Single(x => x.GetProperty("name").GetString() == name);
        var header = c.GetProperty("header").GetString()!;
        var payload = Encoding.UTF8.GetBytes(Body);

        if (c.GetProperty("valid").GetBoolean())
        {
            var evt = Webhooks.ConstructEvent(payload, header, Secret, Tolerance, Now);
            Assert.Equal("whevt_01JD0S6Q4M8K1Z0X7C2V9B3N5R", evt.Id);
            Assert.Equal("event.published", evt.Type);
        }
        else
        {
            var ex = Assert.Throws<SignatureVerificationException>(() => Webhooks.ConstructEvent(payload, header, Secret, Tolerance, Now));
            var expected = c.GetProperty("error").GetString() switch
            {
                "header" => SignatureFailure.Header,
                "timestamp" => SignatureFailure.Timestamp,
                "signature" => SignatureFailure.Signature,
                var other => throw new InvalidOperationException("Unknown error kind " + other),
            };
            Assert.Equal(expected, ex.Reason);
            Assert.IsAssignableFrom<AgilisException>(ex);
        }
    }

    [Fact]
    public void Parsed_event_is_typed_by_type()
    {
        var header = Vectors.GetProperty("cases")[0].GetProperty("header").GetString()!;
        var evt = Webhooks.ConstructEvent(Encoding.UTF8.GetBytes(Body), header, Secret, Tolerance, Now);

        Assert.Equal("webhook_event", evt.Object);
        Assert.Equal(1795075200, evt.Created);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1795075200), evt.CreatedAt);
        Assert.True(evt.Livemode);
        Assert.Equal(1, evt.Version);
        Assert.Equal("api", evt.Request!.Source);
        Assert.Equal("6f1c2a9e-0b7d-4c55-9a51-3d2f8e7b1c04", evt.Request.IdempotencyKey);
        Assert.Equal(10.0m, evt.Data!.ExtensionData!["amount_charged"].GetDecimal());

        var published = evt.GetEvent();
        Assert.NotNull(published);
        Assert.Equal(3104, published!.EventId);
        Assert.Equal("Bar Salsa", published.VenueName);
        Assert.Equal("Classes & workshops", published.Category!.CategoryName);
        Assert.Equal("10-25", published.Attendees);
        Assert.Null(evt.GetOffer());
    }

    [Fact]
    public void String_overload_uses_the_current_time()
    {
        var t = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var body = "{\"id\":\"whevt_x\",\"type\":\"offer.started\",\"data\":{\"object\":{\"offer_id\":5,\"title\":\"T\"}}}";
        var header = "t=" + t + ",v1=" + Sign(Secret, t, body);

        var evt = Webhooks.ConstructEvent(body, header, Secret);

        Assert.Equal(5, evt.GetOffer()!.OfferId);
        Assert.Null(evt.GetEvent());

        // The fixed vectors are long out of date in real time.
        var old = Vectors.GetProperty("cases")[0].GetProperty("header").GetString()!;
        var ex = Assert.Throws<SignatureVerificationException>(() => Webhooks.ConstructEvent(Body, old, Secret));
        Assert.Equal(SignatureFailure.Timestamp, ex.Reason);
    }

    [Fact]
    public void Valid_signature_over_non_json_is_refused()
    {
        var t = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var ex = Assert.Throws<SignatureVerificationException>(() =>
            Webhooks.ConstructEvent("not json", "t=" + t + ",v1=" + Sign(Secret, t, "not json"), Secret));
        Assert.Equal(SignatureFailure.Payload, ex.Reason);
    }

    [Fact]
    public void Errors_do_not_contain_the_secret()
    {
        var ex = Assert.Throws<SignatureVerificationException>(() => Webhooks.ConstructEvent(Body, "t=1,v1=00", Secret));
        Assert.DoesNotContain(Secret, ex.ToString());
    }

    private static string Sign(string secret, long t, string body)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(t + "." + body))).ToLowerInvariant();
    }
}
