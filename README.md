# CopperMonitor

Automated copper price monitor. Every weekday at **08:30 Asia/Taipei** it builds a copper report
(COMEX HG close, daily/weekly change %, USD/TWD, TWD/ton) and pushes it to a LINE group.
No database — historical prices are queried live (Yahoo Finance chart API) when users ask for a
date or date range.

Follows the same DDD / Clean Architecture layout as `uServeCore.MerchantManagement`
(Domain / Application / Infrastructure / Api, MediatR CQRS, `ResponseResult<T>` envelope,
domain exceptions).

## Run

```bash
export Line__ChannelAccessToken="<LINE Messaging API channel access token>"
export Line__GroupId="<LINE group id, starts with C...>"
dotnet run --project src/CopperMonitor/CopperMonitor.Api
```

Without the LINE env vars the API still works; only the push fails with `LineNotConfigured`.

## Endpoints

| Method | Path | Description |
|---|---|---|
| GET | `/api/v1/copper-price/report` | Latest report (same text as the daily LINE message). Optional `?date=yyyy-MM-dd`. |
| GET | `/api/v1/copper-price/history?date=2026-08-27` | One day's price. |
| GET | `/api/v1/copper-price/history?from=2026-08-01&to=2026-08-28` | Date range (max 366 days). |
| POST | `/api/v1/copper-price/report/send` | Manually trigger the LINE push. |

## Configuration (`appsettings.json`)

- `ReportSchedule` — `LocalTime` (default `08:30`), `TimeZone` (`Asia/Taipei`), `WeekdaysOnly`, `Enabled`.
- `Alert` — `DailyChangeThresholdPercent` (3), `WeeklyChangeThresholdPercent` (5). When exceeded, a ⚠️ line is appended to the report.
- `Line` — `ChannelAccessToken`, `GroupId`. Keep these in environment variables (`Line__ChannelAccessToken`, `Line__GroupId`), not in the file.

## Data sources

- Copper: Yahoo Finance `HG=F` (COMEX copper futures, USD/lb).
- FX: Yahoo Finance `TWD=X` (USD/TWD).
- TWD/ton = USD/lb × 2204.62262 × USD/TWD.

## Notes

- Daily change compares the latest close with the previous trading day; weekly change compares with the most recent trading day on/before 7 calendar days earlier.
- API errors and LINE delivery failures are logged via `ILogger`; the scheduler logs and survives failures (next day's run still fires).
- V2 ideas: OpenAI market summaries, LME source, holiday calendar.
