# Changelog

All notable changes to YFSharp are documented in this file.

## 0.2.0 - 2026-07-01

- Added `TryDownloadAsync` for resilient multi-symbol history downloads with per-symbol errors.
- Added structured `ILogger<YahooFinanceClient>` diagnostics for HTTP calls, retry behavior, crumb refreshes, and resilient download failures.
- Added validation for Yahoo base URLs and negative search, lookup, and screener counts.
- Marked `HistoryRequest.Rounding` obsolete in favor of `HistoryRequest.Round`.
- Added raw JSON convention helpers via `YahooJsonConventions`.
- Enabled XML documentation file generation for NuGet packages.
- Split configuration and resilient-download implementation into focused client partials.
- Fixed live quote-summary helpers when Yahoo returns module names with casing that differs from the requested module name.

## 0.1.0 - Initial Preview

- Initial Yahoo Finance client preview with quotes, history, quote summaries, fundamentals, funds, options, search, lookup, screeners, calendars, markets, sectors, industries, auth state, DI, and streaming prices.
