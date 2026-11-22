using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Agilis.Ads.Internal;

internal sealed class ApiRequest
{
    public ApiRequest(HttpMethod method, string path)
    {
        Method = method;
        Path = path;
    }

    public HttpMethod Method { get; }

    public string Path { get; }

    public Query Query { get; } = new Query();

    public MultipartForm? Form { get; set; }

    public JsonBody? Json { get; set; }

    public string? IdempotencyKey { get; set; }

    public Dictionary<string, string> Headers { get; } = new Dictionary<string, string>();
}

internal sealed class HttpCore : IDisposable
{
    internal static readonly HttpMethod Patch = new HttpMethod("PATCH");

    private static readonly int[] RetryableServerStatuses = { 500, 502, 503, 504 };
    private static readonly object RandomLock = new object();
    private static readonly Random Random = new Random();

    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly TimeSpan _timeout;
    private readonly int _maxRetries;
    private readonly HttpClient _http;
    private readonly bool _ownsHttp;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;

    public HttpCore(string apiKey, AgilisClientOptions options)
    {
        _apiKey = apiKey;
        _baseUrl = (options.BaseUrl ?? AgilisClientOptions.DefaultBaseUrl).ToString().TrimEnd('/');
        _timeout = options.Timeout <= TimeSpan.Zero ? TimeSpan.FromSeconds(60) : options.Timeout;
        _maxRetries = Math.Max(0, options.MaxRetries);
        _delay = options.Delay ?? ((wait, ct) => Task.Delay(wait, ct));
        if (options.HttpClient != null)
        {
            _http = options.HttpClient;
            _ownsHttp = false;
        }
        else
        {
            _http = new HttpClient { Timeout = System.Threading.Timeout.InfiniteTimeSpan };
            _ownsHttp = true;
        }
    }

    public static string UserAgent { get; } = BuildUserAgent();

    public async Task<T> SendAsync<T>(ApiRequest request, CancellationToken cancellationToken)
        where T : AgilisResponse
    {
        var (status, headers, text) = await SendRawAsync(request, cancellationToken).ConfigureAwait(false);
        var info = new ResponseInfo(status, headers);
        JsonElement? body = TryParse(text);

        if (status < 200 || status > 299)
        {
            throw CreateError(status, headers, body, text);
        }

        var returnCode = GetString(body, "return_code");
        if (returnCode != null && returnCode.StartsWith("E", StringComparison.Ordinal))
        {
            throw new InvalidRequestException(Redact(GetMessage(body, status, text)), status, returnCode, body, headers);
        }

        T? result;
        try
        {
            result = string.IsNullOrWhiteSpace(text) ? null : JsonSerializer.Deserialize<T>(text, Json.Options);
        }
        catch (JsonException ex)
        {
            throw new ApiException("The API's response could not be read as JSON (" + ex.Message + ").", status, returnCode, body, headers);
        }

        if (result is null)
        {
            throw new ApiException("The API returned an empty response.", status, returnCode, body, headers);
        }

        result.HttpResponse = info;
        return result;
    }

    internal async Task<(int Status, IReadOnlyDictionary<string, IReadOnlyList<string>> Headers, string Text)> SendRawAsync(
        ApiRequest request, CancellationToken cancellationToken)
    {
        if (request.Form != null)
        {
            await request.Form.PrepareAsync(cancellationToken).ConfigureAwait(false);
        }

        var url = _baseUrl + request.Path + request.Query;
        for (var attempt = 0; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_timeout);
            Exception? failure = null;
            try
            {
                using var message = BuildMessage(request, url);
                using var response = await _http.SendAsync(message, HttpCompletionOption.ResponseContentRead, timeoutCts.Token).ConfigureAwait(false);
                var text = response.Content is null
                    ? string.Empty
                    : await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var status = (int)response.StatusCode;
                var headers = CollectHeaders(response);

                if (attempt < _maxRetries && ShouldRetry(request, status))
                {
                    await _delay(RetryWait(attempt, headers), cancellationToken).ConfigureAwait(false);
                    continue;
                }

                return (status, headers, text);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException ex)
            {
                failure = new ConnectionException("The request to the Agilis API timed out.", ex);
            }
            catch (HttpRequestException ex)
            {
                failure = new ConnectionException("Could not connect to the Agilis API: " + Redact(ex.Message), ex);
            }
            catch (IOException ex)
            {
                failure = new ConnectionException("The connection to the Agilis API failed: " + Redact(ex.Message), ex);
            }

            if (attempt >= _maxRetries)
            {
                throw failure;
            }

            await _delay(RetryWait(attempt, ResponseInfo.EmptyHeaders), cancellationToken).ConfigureAwait(false);
        }
    }

    private HttpRequestMessage BuildMessage(ApiRequest request, string url)
    {
        var message = new HttpRequestMessage(request.Method, url);
        message.Headers.TryAddWithoutValidation("x-api-key", _apiKey);
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        message.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
        if (request.IdempotencyKey != null)
        {
            message.Headers.TryAddWithoutValidation("Idempotency-Key", request.IdempotencyKey);
        }

        foreach (var header in request.Headers)
        {
            message.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (request.Form != null)
        {
            message.Content = request.Form.Build();
        }
        else if (request.Json != null)
        {
            message.Content = request.Json.Build();
        }

        return message;
    }

    internal static bool ShouldRetry(ApiRequest request, int status)
    {
        if (status == 429)
        {
            return true;
        }

        if (Array.IndexOf(RetryableServerStatuses, status) < 0)
        {
            return false;
        }

        var method = request.Method.Method.ToUpperInvariant();
        return method == "GET" || method == "DELETE" || (method == "POST" && request.IdempotencyKey != null);
    }

    internal static TimeSpan RetryWait(int attempt, IReadOnlyDictionary<string, IReadOnlyList<string>> headers)
    {
        var retryAfter = ParseRetryAfter(headers);
        if (retryAfter.HasValue)
        {
            return retryAfter.Value;
        }

        double jitter;
        lock (RandomLock)
        {
            jitter = 0.75 + (Random.NextDouble() * 0.5);
        }

        var seconds = 0.5 * Math.Pow(2, attempt) * jitter;
        return TimeSpan.FromSeconds(Math.Min(8.0, seconds));
    }

    internal static TimeSpan? ParseRetryAfter(IReadOnlyDictionary<string, IReadOnlyList<string>> headers)
    {
        if (!headers.TryGetValue("Retry-After", out var values))
        {
            return null;
        }

        var value = values.FirstOrDefault()?.Trim();
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) && seconds >= 0)
        {
            return TimeSpan.FromSeconds(seconds);
        }

        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var when))
        {
            var wait = when - DateTimeOffset.UtcNow;
            return wait > TimeSpan.Zero ? wait : TimeSpan.Zero;
        }

        return null;
    }

    private AgilisException CreateError(int status, IReadOnlyDictionary<string, IReadOnlyList<string>> headers, JsonElement? body, string text)
    {
        var message = Redact(GetMessage(body, status, text));
        var returnCode = GetString(body, "return_code");
        switch (status)
        {
            case 400:
            case 422:
                return new InvalidRequestException(message, status, returnCode, body, headers);
            case 401:
                return new AuthenticationException(message, status, returnCode, body, headers);
            case 403:
                return new PermissionException(message, status, returnCode, body, headers);
            case 404:
                return new NotFoundException(message, status, returnCode, body, headers);
            case 409:
                return new ConflictException(message, status, returnCode, body, headers);
            case 429:
                return new RateLimitException(message, status, returnCode, body, headers, ParseRetryAfter(headers));
            default:
                return new ApiException(message, status, returnCode, body, headers);
        }
    }

    private string Redact(string text) =>
        string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(text) ? text : text.Replace(_apiKey, "[redacted]");

    private static string GetMessage(JsonElement? body, int status, string text)
    {
        if (body.HasValue && body.Value.ValueKind == JsonValueKind.Object && body.Value.TryGetProperty("message", out var m))
        {
            if (m.ValueKind == JsonValueKind.String)
            {
                var s = m.GetString();
                if (!string.IsNullOrEmpty(s))
                {
                    return s!;
                }
            }
            else if (m.ValueKind != JsonValueKind.Null)
            {
                return m.GetRawText();
            }
        }

        return "The Agilis API returned HTTP " + status.ToString(CultureInfo.InvariantCulture) + ".";
    }

    private static string? GetString(JsonElement? body, string name) =>
        body.HasValue && body.Value.ValueKind == JsonValueKind.Object && body.Value.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;

    private static JsonElement? TryParse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(text);
            return doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> CollectHeaders(HttpResponseMessage response)
    {
        var headers = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var h in response.Headers)
        {
            headers[h.Key] = h.Value.ToList();
        }

        if (response.Content != null)
        {
            foreach (var h in response.Content.Headers)
            {
                headers[h.Key] = h.Value.ToList();
            }
        }

        return headers;
    }

    private static string BuildUserAgent()
    {
        var runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription.Trim();
        return "agilis-ads-dotnet/" + AgilisClient.Version + " (" + runtime + ")";
    }

    public void Dispose()
    {
        if (_ownsHttp)
        {
            _http.Dispose();
        }
    }
}
