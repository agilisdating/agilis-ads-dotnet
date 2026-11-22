using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Agilis.Ads.Internal;

namespace Agilis.Ads;

/// <summary>Verifies the webhook messages Agilis sends to your endpoints.</summary>
public static class Webhooks
{
    /// <summary>The header that carries the signature.</summary>
    public const string SignatureHeader = "Agilis-Signature";

    /// <summary>The default tolerance, in seconds, between the signature's timestamp and now.</summary>
    public const int DefaultToleranceSeconds = 300;

    /// <summary>
    /// Checks a received message's <c>Agilis-Signature</c> and returns the parsed event.
    /// Pass the body exactly as received, before any JSON parsing.
    /// </summary>
    /// <param name="payload">The raw request body.</param>
    /// <param name="signatureHeader">The <c>Agilis-Signature</c> header.</param>
    /// <param name="secret">The endpoint's signing secret (<c>whsec_…</c>).</param>
    /// <param name="toleranceSeconds">How far the timestamp may be from now, in seconds. Default 300.</param>
    /// <returns>The event.</returns>
    /// <exception cref="SignatureVerificationException">The message cannot be trusted.</exception>
    public static WebhookEvent ConstructEvent(string payload, string signatureHeader, string secret,
        int toleranceSeconds = DefaultToleranceSeconds)
    {
        if (payload is null)
        {
            throw new ArgumentNullException(nameof(payload));
        }

        return ConstructEvent(Encoding.UTF8.GetBytes(payload), signatureHeader, secret, toleranceSeconds);
    }

    /// <summary>
    /// Checks a received message's <c>Agilis-Signature</c> and returns the parsed event.
    /// </summary>
    /// <param name="payload">The raw request body bytes.</param>
    /// <param name="signatureHeader">The <c>Agilis-Signature</c> header.</param>
    /// <param name="secret">The endpoint's signing secret (<c>whsec_…</c>).</param>
    /// <param name="toleranceSeconds">How far the timestamp may be from now, in seconds. Default 300.</param>
    /// <returns>The event.</returns>
    /// <exception cref="SignatureVerificationException">The message cannot be trusted.</exception>
    public static WebhookEvent ConstructEvent(byte[] payload, string signatureHeader, string secret,
        int toleranceSeconds = DefaultToleranceSeconds) =>
        ConstructEvent(payload, signatureHeader, secret, toleranceSeconds, DateTimeOffset.UtcNow);

    internal static WebhookEvent ConstructEvent(byte[] payload, string? signatureHeader, string secret,
        int toleranceSeconds, DateTimeOffset now)
    {
        if (payload is null)
        {
            throw new ArgumentNullException(nameof(payload));
        }

        if (string.IsNullOrEmpty(secret))
        {
            throw new ArgumentException("The endpoint's signing secret is required.", nameof(secret));
        }

        VerifySignature(payload, signatureHeader, secret, toleranceSeconds, now);

        WebhookEvent? evt;
        try
        {
            evt = JsonSerializer.Deserialize<WebhookEvent>(payload, Json.Options);
        }
        catch (JsonException)
        {
            evt = null;
        }

        if (evt is null)
        {
            throw new SignatureVerificationException("The webhook body is not a JSON object.", SignatureFailure.Payload);
        }

        return evt;
    }

    /// <summary>Checks a signature without parsing the body.</summary>
    /// <param name="payload">The raw request body bytes.</param>
    /// <param name="signatureHeader">The <c>Agilis-Signature</c> header.</param>
    /// <param name="secret">The endpoint's signing secret.</param>
    /// <param name="toleranceSeconds">How far the timestamp may be from now, in seconds.</param>
    /// <exception cref="SignatureVerificationException">The message cannot be trusted.</exception>
    public static void VerifySignature(byte[] payload, string? signatureHeader, string secret,
        int toleranceSeconds = DefaultToleranceSeconds) =>
        VerifySignature(payload, signatureHeader, secret, toleranceSeconds, DateTimeOffset.UtcNow);

    internal static void VerifySignature(byte[] payload, string? signatureHeader, string secret,
        int toleranceSeconds, DateTimeOffset now)
    {
        var (timestamp, signatures) = ParseHeader(signatureHeader);

        var prefix = Encoding.UTF8.GetBytes(timestamp.ToString(CultureInfo.InvariantCulture) + ".");
        var signed = new byte[prefix.Length + payload.Length];
        Buffer.BlockCopy(prefix, 0, signed, 0, prefix.Length);
        Buffer.BlockCopy(payload, 0, signed, prefix.Length, payload.Length);

        byte[] expected;
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
        {
            expected = hmac.ComputeHash(signed);
        }

        var expectedHex = Encoding.ASCII.GetBytes(ToHex(expected));
        var matched = false;
        foreach (var signature in signatures)
        {
            // Every candidate is compared, in constant time, so timing reveals nothing.
            matched |= FixedTimeEquals(expectedHex, Encoding.ASCII.GetBytes(signature.ToLowerInvariant()));
        }

        if (!matched)
        {
            throw new SignatureVerificationException("No signature matches the body and secret.", SignatureFailure.Signature);
        }

        var age = Math.Abs(now.ToUnixTimeSeconds() - timestamp);
        if (age > toleranceSeconds)
        {
            throw new SignatureVerificationException("The signature's timestamp is outside the tolerance.", SignatureFailure.Timestamp);
        }
    }

    private static (long Timestamp, List<string> Signatures) ParseHeader(string? header)
    {
        if (string.IsNullOrWhiteSpace(header))
        {
            throw new SignatureVerificationException("The Agilis-Signature header is missing.", SignatureFailure.Header);
        }

        long? timestamp = null;
        var signatures = new List<string>();
        foreach (var rawPart in header!.Split(','))
        {
            var part = rawPart.Trim();
            var eq = part.IndexOf('=');
            if (eq <= 0)
            {
                continue;
            }

            var key = part.Substring(0, eq).Trim();
            var value = part.Substring(eq + 1).Trim();
            if (key == "t")
            {
                if (value.Length == 0 || !IsDigits(value) || !long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var t))
                {
                    throw new SignatureVerificationException("The Agilis-Signature timestamp is not a number.", SignatureFailure.Header);
                }

                timestamp = t;
            }
            else if (key == "v1" && value.Length > 0)
            {
                signatures.Add(value);
            }
        }

        if (timestamp is null)
        {
            throw new SignatureVerificationException("The Agilis-Signature header has no timestamp.", SignatureFailure.Header);
        }

        if (signatures.Count == 0)
        {
            throw new SignatureVerificationException("The Agilis-Signature header has no v1 signature.", SignatureFailure.Header);
        }

        return (timestamp.Value, signatures);
    }

    private static bool IsDigits(string value)
    {
        foreach (var c in value)
        {
            if (c < '0' || c > '9')
            {
                return false;
            }
        }

        return true;
    }

    private static string ToHex(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
        {
            sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
        }

        return sb.ToString();
    }

    private static bool FixedTimeEquals(byte[] a, byte[] b)
    {
        if (a.Length != b.Length)
        {
            return false;
        }

        var diff = 0;
        for (var i = 0; i < a.Length; i++)
        {
            diff |= a[i] ^ b[i];
        }

        return diff == 0;
    }
}
