# External market rate providers

The server uses `IMarketRateProvider` to isolate external rate APIs from business logic.

Configured providers:

1. **Frankfurter** — primary provider, no API key required.
2. **ExchangeRate-API Open Access** — fallback provider, no API key required.

`MarketRateRefreshService` tries providers in registration order. The first provider that returns at least one rate is used and saved to SQLite. This intentionally avoids mixing rates from different providers in the same history.

## Refresh endpoint

```http
POST /api/rates/refresh
```

Example response:

```json
{
  "providersUsed": 1,
  "ratesSaved": 2,
  "refreshedAtUtc": "2026-08-25T10:00:00Z"
}
```

The configured currency pairs are taken from `CurrencyPairs`. Currently the database initializer creates `EUR/USD` and `USD/UAH`.
