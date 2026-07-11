# Live Integration Local Server Button

## Exact Files Inspected
- `src/RCDragManagerProd/UI/Forms/Session/LandingPageForm.cs`
- `src/RCDragManagerProd/UI/Forms/Common/SettingsForm.cs`
- `src/RCDragManagerProd/Config/AppSettings.cs`
- `C:\Users\Stewart McMillan\source\repos\RCDragLiveServer\src\RCDragLiveServer\Program.cs`
- `C:\Users\Stewart McMillan\source\repos\RCDragLiveServer\src\RCDragLiveServer\Controllers\PublicLiveController.cs`

## Where Settings UI Lives
- Settings dialog entry point:
  - `LandingPageForm.cs` -> `btnSettings_Click` -> `new SettingsForm().ShowDialog(...)`
- Settings dialog implementation:
  - `src/RCDragManagerProd/UI/Forms/Common/SettingsForm.cs`
- Existing logging controls are in this same file (`AppSettings.EnableLogging`, `AppSettings.LogFilePath`).

## Exact Files Changed
- `src/RCDragManagerProd/UI/Forms/Common/SettingsForm.cs`
- `docs/LiveIntegration_LocalServerButton.md`

## Root Cause Found
After ApiKey injection, child process output showed the app started but health never became healthy. The process was not reliably bound to `http://localhost:5005`, so the launcher checked the wrong endpoint.

## Exact Binding Fix Applied
Both launch paths now explicitly force local binding to `http://localhost:5005` using:
- Environment variable: `ASPNETCORE_URLS=http://localhost:5005`
- Command-line URL argument:
  - exe path: `--urls "http://localhost:5005"`
  - dotnet run path: `-- --urls "http://localhost:5005"`

Both launch paths also inject:
- `ASPNETCORE_ENVIRONMENT=Development`
- `ApiKey=<your-api-key>`

## Startup Diagnostics Added
During startup wait, all non-empty child process lines are captured and logged:
- `[LIVE][LOCALSERVER][STDOUT] ...`
- `[LIVE][LOCALSERVER][STDERR] ...`

Listening URL detection:
- Parses output lines containing `Now listening on:`
- Logs detected line as:
  - `[LIVE][LOCALSERVER][LISTENING] <url>`
- If health still fails, popup includes:
  - detected listening URL
  - expected URL (`http://localhost:5005`)
  - explicit mismatch message when they differ
- Failure popup also includes last stderr/stdout lines.

## Health Wait Behavior
- Kept non-blocking async UI flow.
- Increased health wait window from 10s to 15s.

## Final Button Behavior
### 1) Start Local Live Server
1. Logs `[LIVE][LOCALSERVER][START]`
2. Checks `http://localhost:5005/health`
3. If healthy: logs `[LIVE][LOCALSERVER][OK]` and shows info
4. If not healthy: launches server (exe preferred, `dotnet run` fallback)
5. Waits up to 15 seconds for health
6. On failure, popup includes listening/output diagnostics

### 2) Open Live View
1. If server is not healthy, auto-starts server first (same startup path)
2. Opens browser to `http://localhost:5005/`
3. Logs `[LIVE][LOCALSERVER][OPEN]`

Why `/` was chosen:
- It is the human-friendly live page; `/api/live` remains available for raw JSON checks.

## Launch Strategy
A. Preferred: local executable if present
- `...\RCDragLiveServer\src\RCDragLiveServer\bin\Debug\net8.0\RCDragLiveServer.exe`
- `...\RCDragLiveServer\src\RCDragLiveServer\bin\Release\net8.0\RCDragLiveServer.exe`

B. Fallback:
- `dotnet run --project "C:\Users\Stewart McMillan\source\repos\RCDragLiveServer\src\RCDragLiveServer" -- --urls "http://localhost:5005"`

## Assumptions / Limitations
- Local-dev convenience only.
- Uses fixed local repo path assumptions for RCDragLiveServer.
- Does not manage stop/restart lifecycle of spawned server process.
- Production live-update flow is unchanged.

## Build Status
- `dotnet build src/RCDragManagerProd/RCDragManagerProd.csproj -c Debug`
- Result: success.
