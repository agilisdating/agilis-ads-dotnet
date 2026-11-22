# Changelog

All notable changes to this package are listed here. It follows [Semantic Versioning](https://semver.org/).

## 0.1.0 (unreleased)

- First release: `AgilisClient` with every operation of the Agilis Advertising REST API (categories, interests, Offers, Events, webhook endpoints and webhook events), for `netstandard2.0` and `net8.0`.
- Typed parameters and responses, with unknown fields kept in `ExtensionData`.
- Automatic `Idempotency-Key` on Offer creation and Event publishing, kept across retries.
- Retries with `Retry-After` and exponential backoff.
- Cursor pagination with `IAsyncEnumerable` (`ListAutoPagingAsync`).
- Webhook signature verification (`Webhooks.ConstructEvent`).
- C# and Visual Basic samples.
