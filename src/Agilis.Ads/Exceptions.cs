using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Agilis.Ads;

/// <summary>
/// The base type of every error the SDK raises. It carries the HTTP status, the API's
/// <c>return_code</c> and message, the parsed body and the response headers. The API key is
/// never part of the message.
/// </summary>
public class AgilisException : Exception
{
    /// <summary>Creates an error.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status, if there was a response.</param>
    /// <param name="returnCode">The API's <c>return_code</c>, if any.</param>
    /// <param name="body">The parsed JSON body, if any.</param>
    /// <param name="headers">The response headers, if any.</param>
    /// <param name="innerException">The underlying exception, if any.</param>
    public AgilisException(string message, int? statusCode = null, string? returnCode = null,
        JsonElement? body = null, IReadOnlyDictionary<string, IReadOnlyList<string>>? headers = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ReturnCode = returnCode;
        Body = body;
        Headers = headers ?? ResponseInfo.EmptyHeaders;
    }

    /// <summary>The HTTP status, or <c>null</c> when there was no response.</summary>
    public int? StatusCode { get; }

    /// <summary>The API's <c>return_code</c> (for example <c>E1</c>), when the body had one.</summary>
    public string? ReturnCode { get; }

    /// <summary>The parsed JSON body, when the response had one.</summary>
    public JsonElement? Body { get; }

    /// <summary>The response headers (case-insensitive names). Empty when there was no response.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Headers { get; }
}

/// <summary>HTTP 401: the API key is missing, unknown, disabled or expired.</summary>
public class AuthenticationException : AgilisException
{
    /// <summary>Creates the error.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status.</param>
    /// <param name="returnCode">The API's <c>return_code</c>, if any.</param>
    /// <param name="body">The parsed JSON body, if any.</param>
    /// <param name="headers">The response headers.</param>
    public AuthenticationException(string message, int? statusCode = null, string? returnCode = null,
        JsonElement? body = null, IReadOnlyDictionary<string, IReadOnlyList<string>>? headers = null)
        : base(message, statusCode, returnCode, body, headers) { }
}

/// <summary>HTTP 403: the key may not do this (for example an <c>environment</c> of the other mode).</summary>
public class PermissionException : AgilisException
{
    /// <summary>Creates the error.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status.</param>
    /// <param name="returnCode">The API's <c>return_code</c>, if any.</param>
    /// <param name="body">The parsed JSON body, if any.</param>
    /// <param name="headers">The response headers.</param>
    public PermissionException(string message, int? statusCode = null, string? returnCode = null,
        JsonElement? body = null, IReadOnlyDictionary<string, IReadOnlyList<string>>? headers = null)
        : base(message, statusCode, returnCode, body, headers) { }
}

/// <summary>HTTP 404: the resource was not found.</summary>
public class NotFoundException : AgilisException
{
    /// <summary>Creates the error.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status.</param>
    /// <param name="returnCode">The API's <c>return_code</c>, if any.</param>
    /// <param name="body">The parsed JSON body, if any.</param>
    /// <param name="headers">The response headers.</param>
    public NotFoundException(string message, int? statusCode = null, string? returnCode = null,
        JsonElement? body = null, IReadOnlyDictionary<string, IReadOnlyList<string>>? headers = null)
        : base(message, statusCode, returnCode, body, headers) { }
}

/// <summary>HTTP 409: a conflict, for example an Idempotency-Key whose first request is still being processed.</summary>
public class ConflictException : AgilisException
{
    /// <summary>Creates the error.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status.</param>
    /// <param name="returnCode">The API's <c>return_code</c>, if any.</param>
    /// <param name="body">The parsed JSON body, if any.</param>
    /// <param name="headers">The response headers.</param>
    public ConflictException(string message, int? statusCode = null, string? returnCode = null,
        JsonElement? body = null, IReadOnlyDictionary<string, IReadOnlyList<string>>? headers = null)
        : base(message, statusCode, returnCode, body, headers) { }
}

/// <summary>
/// HTTP 400 or 422, or an HTTP 200 whose <c>return_code</c> starts with <c>E</c> (for example
/// <c>E3</c> when the balance is too low for an Offer).
/// </summary>
public class InvalidRequestException : AgilisException
{
    /// <summary>Creates the error.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status.</param>
    /// <param name="returnCode">The API's <c>return_code</c>, if any.</param>
    /// <param name="body">The parsed JSON body, if any.</param>
    /// <param name="headers">The response headers.</param>
    public InvalidRequestException(string message, int? statusCode = null, string? returnCode = null,
        JsonElement? body = null, IReadOnlyDictionary<string, IReadOnlyList<string>>? headers = null)
        : base(message, statusCode, returnCode, body, headers) { }
}

/// <summary>HTTP 429: too many requests.</summary>
public class RateLimitException : AgilisException
{
    /// <summary>Creates the error.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status.</param>
    /// <param name="returnCode">The API's <c>return_code</c>, if any.</param>
    /// <param name="body">The parsed JSON body, if any.</param>
    /// <param name="headers">The response headers.</param>
    /// <param name="retryAfter">The <c>Retry-After</c> wait, if the response had one.</param>
    public RateLimitException(string message, int? statusCode = null, string? returnCode = null,
        JsonElement? body = null, IReadOnlyDictionary<string, IReadOnlyList<string>>? headers = null,
        TimeSpan? retryAfter = null)
        : base(message, statusCode, returnCode, body, headers)
    {
        RetryAfter = retryAfter;
    }

    /// <summary>How long to wait before trying again, from the <c>Retry-After</c> header.</summary>
    public TimeSpan? RetryAfter { get; }
}

/// <summary>HTTP 5xx, or any other status the SDK does not map more precisely.</summary>
public class ApiException : AgilisException
{
    /// <summary>Creates the error.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status.</param>
    /// <param name="returnCode">The API's <c>return_code</c>, if any.</param>
    /// <param name="body">The parsed JSON body, if any.</param>
    /// <param name="headers">The response headers.</param>
    public ApiException(string message, int? statusCode = null, string? returnCode = null,
        JsonElement? body = null, IReadOnlyDictionary<string, IReadOnlyList<string>>? headers = null)
        : base(message, statusCode, returnCode, body, headers) { }
}

/// <summary>No response: the connection failed or the request timed out, on every attempt.</summary>
public class ConnectionException : AgilisException
{
    /// <summary>Creates the error.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The underlying exception, if any.</param>
    public ConnectionException(string message, Exception? innerException = null)
        : base(message, null, null, null, null, innerException) { }
}

/// <summary>What was wrong with a webhook signature.</summary>
public enum SignatureFailure
{
    /// <summary>The <c>Agilis-Signature</c> header is missing or malformed, or has no <c>v1</c> signature.</summary>
    Header,

    /// <summary>The timestamp is outside the tolerance.</summary>
    Timestamp,

    /// <summary>No <c>v1</c> signature matches the body and secret.</summary>
    Signature,

    /// <summary>The signature is valid but the body is not a JSON object.</summary>
    Payload,
}

/// <summary>Raised by <see cref="Webhooks.ConstructEvent(string, string, string, int)"/> when a message cannot be trusted.</summary>
public class SignatureVerificationException : AgilisException
{
    /// <summary>Creates the error.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="reason">Why verification failed.</param>
    public SignatureVerificationException(string message, SignatureFailure reason)
        : base(message)
    {
        Reason = reason;
    }

    /// <summary>Why verification failed.</summary>
    public SignatureFailure Reason { get; }
}
