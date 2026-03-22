# Live Integration Local Setup Fix

## What Was Broken
Running `RCDragLiveServer` locally with:

```bash
dotnet run --project src/RCDragLiveServer
```

threw:

```text
System.InvalidOperationException: Configuration key 'ApiKey' is required.
```

Root cause:
- `Program.cs` requires `builder.Configuration["ApiKey"]`.
- `appsettings.json` had `"ApiKey": ""`.
- No `appsettings.Development.json` existed.
- `launchSettings.json` did not define `ApiKey`.

## What Was Fixed
1. Added `ApiKey` to launch profile environment variables (preferred source for local `dotnet run`).
2. Added `appsettings.Development.json` with `ApiKey` as development fallback.
3. Improved the startup exception message to clearly list valid configuration sources.
4. Changed local launch URL to a dedicated local port to avoid common conflicts.

## Exact Files Changed
### RCDragLiveServer
- `C:\Users\Stewart McMillan\source\repos\RCDragLiveServer\src\RCDragLiveServer\Program.cs`
  - Still reads `builder.Configuration["ApiKey"]`.
  - Error message now explains where to configure key.
- `C:\Users\Stewart McMillan\source\repos\RCDragLiveServer\src\RCDragLiveServer\Properties\launchSettings.json`
  - Added:
    - `ApiKey=86561451-e7cf-4c01-87f1-0ae7e34e26d0`
  - Updated:
    - `applicationUrl=http://localhost:5005`
- `C:\Users\Stewart McMillan\source\repos\RCDragLiveServer\src\RCDragLiveServer\appsettings.Development.json`
  - Added:
    - `"ApiKey": "86561451-e7cf-4c01-87f1-0ae7e34e26d0"`

## Where ApiKey Is Now Defined
- Primary local source (A):
  - `launchSettings.json` profile `RCDragLiveServer` -> `environmentVariables.ApiKey`
- Development fallback (B):
  - `appsettings.Development.json` -> `ApiKey`
- Production/global fallback:
  - external environment variable `ApiKey` or `appsettings.json` (if desired)

## ASP.NET Core Config Flow (Relevant)
With `WebApplication.CreateBuilder(args)`, configuration is layered in standard order, with later sources overriding earlier ones:
1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. environment variables
4. command-line args

For local `dotnet run` with launch profile:
- `ASPNETCORE_ENVIRONMENT=Development` enables `appsettings.Development.json`.
- `launchSettings.json` environment variables (including `ApiKey`) are applied and override JSON values.

## RC Drag Manager Alignment Check
Inspected:
- `C:\Users\Stewart McMillan\source\repos\RC-Drag-Manager\src\RCDragManagerProd\App.config`
- `C:\Users\Stewart McMillan\source\repos\RC-Drag-Manager\src\RCDragManagerProd\Integration\LiveRaceUpdateClient.cs`

Confirmed:
- Manager sends header `X-API-KEY` from config key `LiveUpdateApiKey`.
- Server expects config key `ApiKey` but validates request header `X-API-KEY` value.
- No naming mismatch in wire contract:
  - Server-side config name: `ApiKey`
  - Client-side config name: `LiveUpdateApiKey`
  - HTTP header: `X-API-KEY`

## Exact Steps To Run Both Apps Locally
1. Start live server:

```bash
cd C:\Users\Stewart McMillan\source\repos\RCDragLiveServer
dotnet run --project src/RCDragLiveServer
```

Expected local URL:
- `http://localhost:5005`
- Live state endpoint: `http://localhost:5005/api/live`
- Update endpoint: `http://localhost:5005/api/update`

2. Point RC Drag Manager to local server:
- Edit `src/RCDragManagerProd/App.config`:
  - `LiveUpdateUrl = http://localhost:5005/api/update`
  - `LiveUpdateApiKey = 86561451-e7cf-4c01-87f1-0ae7e34e26d0`

3. Run RC Drag Manager and trigger updates (Generate Bracket / Submit Winner / Advance Round).

4. Verify server received updates:

```bash
curl -i http://localhost:5005/api/live
```

## How To Switch Local vs Production Endpoints
### Local
- `LiveUpdateUrl = http://localhost:5005/api/update`
- `LiveUpdateApiKey = 86561451-e7cf-4c01-87f1-0ae7e34e26d0`

### Production
- `LiveUpdateUrl = https://stewmacrc.com/api/update`
- `LiveUpdateApiKey = 86561451-e7cf-4c01-87f1-0ae7e34e26d0`

No code changes are required in RC Drag Manager to switch endpoints; config-only change.

## Verification Summary
- `RCDragLiveServer` build: success
- `RCDragManagerProd` build: success
- Local server health check confirmed on `http://localhost:5005/api/live` (HTTP 200)
