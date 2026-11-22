using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Agilis.Ads.Tests;

/// <summary>One part of a captured multipart body.</summary>
public sealed class CapturedPart
{
    public string Name { get; init; } = "";
    public string? FileName { get; init; }
    public string? ContentType { get; init; }
    public byte[] Bytes { get; init; } = Array.Empty<byte>();
    public string Text => Encoding.UTF8.GetString(Bytes);
}

/// <summary>A request as the fake handler saw it.</summary>
public sealed class CapturedRequest
{
    public HttpMethod Method { get; init; } = HttpMethod.Get;
    public Uri Uri { get; init; } = new Uri("http://localhost");
    public Dictionary<string, string> Headers { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public string? ContentType { get; init; }
    public string? Body { get; init; }
    public List<CapturedPart> Parts { get; init; } = new();

    public string PathAndQuery => Uri.PathAndQuery;

    public string? Header(string name) => Headers.TryGetValue(name, out var v) ? v : null;

    public IEnumerable<string> Values(string name) => Parts.Where(p => p.Name == name).Select(p => p.Text);

    public string? Value(string name) => Values(name).FirstOrDefault();
}

/// <summary>A fake <see cref="HttpMessageHandler"/> that records requests and plays back queued responses.</summary>
public sealed class FakeHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>> _responses = new();

    public List<CapturedRequest> Requests { get; } = new();

    public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? Fallback { get; set; }

    public FakeHandler Respond(int status, string json, params (string Name, string Value)[] headers)
    {
        _responses.Enqueue((_, _) => Task.FromResult(Build(status, json, headers)));
        return this;
    }

    public FakeHandler Throw(Exception ex)
    {
        _responses.Enqueue((_, _) => Task.FromException<HttpResponseMessage>(ex));
        return this;
    }

    public FakeHandler Hang()
    {
        _responses.Enqueue(async (_, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            throw new InvalidOperationException("unreachable");
        });
        return this;
    }

    public static HttpResponseMessage Build(int status, string json, params (string Name, string Value)[] headers)
    {
        var response = new HttpResponseMessage((HttpStatusCode)status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        foreach (var (name, value) in headers)
        {
            if (!response.Headers.TryAddWithoutValidation(name, value))
            {
                response.Content.Headers.TryAddWithoutValidation(name, value);
            }
        }

        return response;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var h in request.Headers)
        {
            headers[h.Key] = string.Join(h.Key.Equals("User-Agent", StringComparison.OrdinalIgnoreCase) ? " " : ",", h.Value);
        }

        string? body = null;
        var parts = new List<CapturedPart>();
        string? contentType = null;
        if (request.Content != null)
        {
            contentType = request.Content.Headers.ContentType?.MediaType;
            if (request.Content is MultipartFormDataContent multipart)
            {
                foreach (var part in multipart)
                {
                    var disposition = part.Headers.ContentDisposition;
                    parts.Add(new CapturedPart
                    {
                        Name = Unquote(disposition?.Name),
                        FileName = disposition?.FileName is null ? null : Unquote(disposition.FileName),
                        ContentType = part.Headers.ContentType?.MediaType,
                        Bytes = await part.ReadAsByteArrayAsync(cancellationToken),
                    });
                }
            }
            else
            {
                body = await request.Content.ReadAsStringAsync(cancellationToken);
            }
        }

        Requests.Add(new CapturedRequest
        {
            Method = request.Method,
            Uri = request.RequestUri!,
            Headers = headers,
            ContentType = contentType,
            Body = body,
            Parts = parts,
        });

        if (_responses.Count > 0)
        {
            return await _responses.Dequeue()(request, cancellationToken);
        }

        if (Fallback != null)
        {
            return await Fallback(request, cancellationToken);
        }

        return Build(200, "{\"return_code\":\"S1\"}");
    }

    private static string Unquote(string? value) => (value ?? "").Trim('"');
}

public static class TestClient
{
    public const string TestKey = "pk_test_abcdefghijklmnopqrstuvwxyz0123456789";
    public const string LiveKey = "pk_live_abcdefghijklmnopqrstuvwxyz0123456789";

    public static (AgilisClient Client, FakeHandler Handler, List<TimeSpan> Waits) Create(string key = TestKey, int maxRetries = 2, TimeSpan? timeout = null)
    {
        var handler = new FakeHandler();
        var waits = new List<TimeSpan>();
        var options = new AgilisClientOptions
        {
            ApiKey = key,
            HttpClient = new HttpClient(handler),
            MaxRetries = maxRetries,
            Timeout = timeout ?? TimeSpan.FromSeconds(60),
            Delay = (wait, _) =>
            {
                waits.Add(wait);
                return Task.CompletedTask;
            },
        };
        return (new AgilisClient(options), handler, waits);
    }
}
