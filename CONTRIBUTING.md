# Contributing

Thanks for helping improve YFSharp.

## Development

```bash
dotnet restore YFSharp.slnx
dotnet build YFSharp.slnx
dotnet test YFSharp.slnx
```

Live Yahoo checks are off by default:

```bash
YFSHARP_LIVE_TESTS=1 dotnet test YFSharp.slnx
```

Live failures can be caused by Yahoo endpoint changes, rate limits, regional behavior, or network state. Prefer adding mocked tests and saved fixtures for deterministic coverage, then use live tests as canaries.

## Public API Changes

YFSharp is pre-1.0, but public API changes should still be intentional:

- Keep source compatibility when practical.
- Mark renamed members obsolete before removing them.
- Document new public entry points in the README and changelog.
- Add tests for validation, parsing, and failure behavior.

## Yahoo Payloads

Yahoo Finance is not an official public API. When endpoint shapes change, keep typed models conservative and preserve fluid fields through `Raw`, `AdditionalData`, or module dictionaries.
