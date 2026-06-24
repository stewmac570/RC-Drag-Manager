# RC Drag Manager - Live Server Integration

Last updated: 2026-06-06

The desktop app and live server are separate repositories that operate as one product.

| Role | Repository |
| --- | --- |
| Race control desktop app | `C:\Users\Stewart McMillan\source\repos\RC-Drag-Manager` |
| Public live scoreboard server | `C:\Users\Stewart McMillan\source\repos\RCDragLiveServer` |

## Desktop Client

Desktop live integration is in:

```text
src\RCDragManagerProd\Integration\LiveApiClient.cs
src\RCDragManagerProd\Integration\LiveRaceUpdateDto.cs
```

Current endpoints:

| Method | URL | Purpose | Auth |
| --- | --- | --- | --- |
| `POST` | `https://stewmacrc.com/api/update` | Push current live race state. | `X-API-KEY` |
| `POST` | `https://stewmacrc.com/api/reset` | Clear one event from live server state. | `X-API-KEY` |
| `GET` | `https://stewmacrc.com/api/dialin?eventId=...` | Poll submitted dial-ins. | `X-API-KEY` |

Live pushes are skipped unless `AppSettings.LiveBroadcastEnabled` is true.

`LiveApiClient` maintains one serial send channel per `(eventId, classType)`. Pending updates are latest-wins; reset operations are preserved as ordered barriers.

## Shared Payload

The desktop `LiveRaceUpdateDto` and server `LiveRaceState` currently share this shape using camelCase JSON:

```json
{
  "eventId": "",
  "eventName": "",
  "eventDate": "",
  "classType": "",
  "raceType": "",
  "currentRound": "",
  "nextUp": "",
  "rrStandings": null,
  "matches": [],
  "winners": [],
  "dialInLocked": false
}
```

`matches` is required by the server update endpoint. Missing/null `matches` returns `400 invalid_payload`.

Current match shape:

```json
{
  "roundLabel": "RR1",
  "driver1": "Driver A",
  "driver2": "Driver B",
  "leftDriver": "Driver A",
  "rightDriver": "Driver B",
  "leftDriverId": 1,
  "rightDriverId": 2,
  "winnerName": null,
  "leftDriverDialIn": 3.25,
  "rightDriverDialIn": 3.4
}
```

Current winner shape:

```json
{
  "roundLabel": "RR1",
  "winnerName": "Driver A",
  "loserName": "Driver B"
}
```

Timing fields are not currently part of the live DTO.

## Live Server Public Surface

`RCDragLiveServer` exposes:

| Method | Route | Purpose | Auth |
| --- | --- | --- | --- |
| `GET` | `/` | Public landing page listing active events. | none |
| `GET` | `/event/{eventId}` | Public scoreboard and dial-in form for one event. | none |
| `GET` | `/api/live` | Public JSON snapshot of active class states. | none |
| `GET` | `/health` | Health check. | none |
| `POST` | `/api/dialin` | Public driver dial-in submission. | none, rate-limited |
| `GET` | `/api/dialin?eventId=...` | Dial-in polling for desktop app. | `X-API-KEY` |
| `POST` | `/api/update` | Desktop live state ingestion. | `X-API-KEY` |
| `POST` | `/api/reset` | Clear an event from server state. | `X-API-KEY` |

## Live Server State Model

The live server stores state in memory:

- Events are bucketed primarily by `eventName`, falling back to `eventId` when event name is blank.
- Event aliases are registered for event name, event id, and GUID formats.
- Each event bucket stores class states keyed by `classType`.
- Active events expire after two hours without updates.
- When a new non-empty session `eventId` arrives for the same event bucket, old class state and dial-ins are cleared.

This means the server is currently volatile. Restarting the server loses active race state and submitted dial-ins until the desktop app pushes again.

## Dial-In Flow

1. Public user opens `/event/{eventId}`.
2. The page builds a driver list from active match data.
3. User submits `eventId`, `driverId`, `dialIn`, and optional 4-digit `pin` to `POST /api/dialin`.
4. Server validates event, driver, dial-in value, lock state, PIN format, and rate limit.
5. Desktop app polls protected `GET /api/dialin`.
6. Desktop app applies returned driver-id-to-dial-in values.

When `dialInLocked` is true for an event, public updates return `423 locked`.

## Config Requirements

`RCDragLiveServer` refuses to start without `ApiKey` configured through appsettings, development settings, launch settings, or environment variables.

The desktop app must have the same API key in `App.config` under `ApiKey`.

## Not Implemented Yet

- Persistent live-server storage.
- Timing/Portatree result fields.
- OBS-specific timing overlay endpoint.
- Server-side authentication for public dial-in users beyond optional per-driver PIN storage.
