# RCDM — Offline Local Server ("Track Mode") — Spec & Scope
*Status: **SHELVED 2026-06-15.** Not being built. Decision: rather than have the PC
host an offline scoreboard, take a 4G router (Mercusys MB230-4G, already owned) to the
venue with a data SIM — that gives real internet, so the existing stewmacrc.com live
site works with zero new code. Track Mode only becomes worth building if venues with
**no cellular coverage at all** turn out to be common. See
[`TRACK-MODE-RESEARCH.md`](TRACK-MODE-RESEARCH.md) §"Why this is shelved" for the full
reasoning (RF/range of a PC-hosted AP, and the SIM-router alternative). Do not
implement without revisiting that decision.*

---

## 1. Problem Statement

RC Drag Manager (the WinForms desktop app) is fully functional offline — brackets, lanes, dial-ins, classes all run locally with no internet dependency. The live scoreboard at stewmacrc.com does NOT work offline, because:

- The desktop app pushes race state to RCDragLiveServer (Render) via `LiveApiClient` — **one-way, push only**. Render never sends data back to the app.
- With no internet at the venue, the app cannot push. Render holds stale/no data.
- Racers at the venue have phones but the venue has **no internet, no venue Wi-Fi, no mobile coverage, no hotspots**. The race director has only a PC on a desk.

Result: racers cannot see who they race next, what lane, or what dial-in — they have to keep asking the race director.

## 2. Solution Overview

Make the race director's PC the server.

1. RCDM embeds a **local HTTP server** that serves the existing mobile scoreboard website, fed **directly from the app's in-memory race state** (the same data currently shaped into `LiveRaceUpdateDto`).
2. The PC broadcasts its **own standalone Wi-Fi network** (software access point — no internet required or involved).
3. RCDM displays a **QR code**; racers scan it, join the PC's Wi-Fi, and their browser opens the local scoreboard at the PC's IP (e.g. `http://192.168.137.1:5000`).
4. The scoreboard page keeps its existing ~5-second polling refresh — it just polls the local server instead of Render.

Render and RCDragLiveServer are **completely out of scope**. No changes to the server repo. When the venue has internet, the normal Render push path is used exactly as today; Track Mode is the offline alternative.

## 3. Hard Constraints (Non-Negotiable)

| # | Constraint |
|---|-----------|
| C1 | **Zero-config for the race director.** One toggle/button: "Enable Track Mode". No command lines, no settings files, no network knowledge. Installer/elevated setup does ALL plumbing in advance. |
| C2 | **Off by default, closed by default.** Installing RCDM must NOT leave any port open, hotspot enabled, or server listening. Everything starts only when the toggle is switched ON, and is torn down cleanly when toggled OFF or the app exits. |
| C3 | **No internet dependency anywhere in the feature.** The hotspot must broadcast with no upstream connection. Assume the PC has a Wi-Fi adapter but no connectivity of any kind. |
| C4 | **Reuse the existing mobile scoreboard UI.** Racers see the same site they'd see on stewmacrc.com. Frontend assets are bundled into the desktop app; do not redesign the page. |
| C5 | **Reuse the existing DTO pipeline.** The local server's data endpoint must serve the same JSON shape the live site already consumes (`LiveRaceUpdateDto`), built by the existing `BuildLiveRaceUpdateDto()` path in `RaceController.LiveUpdate.cs`. One source of truth for the payload. |
| C6 | **.NET Framework 4.8 / WinForms.** No ASP.NET Core, no Kestrel, no out-of-process services. `System.Net.HttpListener` is the expected server. |
| C7 | **Multi-class aware.** Events run via `MultiClassRaceForm` with multiple `RaceSession` tabs. The local scoreboard must show the same class structure the Render site shows. |

## 4. Architecture

```
Race Director's PC (no internet)
│
├─ RCDM (WinForms, .NET 4.8)
│   ├─ Settings: [Enable Track Mode] toggle
│   │     ON → start SoftAP → start HttpListener → show QR code
│   │     OFF / app exit → stop listener → stop SoftAP
│   │
│   ├─ TrackModeServer (new, HttpListener)
│   │     GET /                → bundled scoreboard index.html
│   │     GET /assets/*        → bundled css/js/img
│   │     GET /api/live        → JSON from BuildLiveRaceUpdateDto()
│   │                            (same shape Render receives today)
│   │
│   └─ SoftAP manager (new)
│         Creates standalone Wi-Fi AP (SSID e.g. "RCDM-TRACK", WPA2 PSK)
│         Mechanism decided by Phase 0 research (see §6)
│
└─ Racer phones
      Scan QR #1 (Wi-Fi join: WIFI:T:WPA;S:RCDM-TRACK;P:<pwd>;;)
      Scan QR #2 (URL: http://<pc-ip>:5000)
      Browser polls /api/live every ~5s — identical UX to stewmacrc.com
```

### Components

**`TrackModeServer` (new)** — `Integration/TrackModeServer.cs`
- Wraps `HttpListener`. Prefix bound to the SoftAP adapter IP on a configurable port (default 5000, stored in AppSettings).
- Serves embedded/bundled static frontend assets + the `/api/live` JSON endpoint.
- Thread-safe snapshot of the latest DTO: the controller's existing live-update hook also writes the serialized DTO into the TrackModeServer's current-state cache whenever race state changes. The endpoint just returns the cached JSON — no engine access from request threads.
- Graceful start/stop; never throws to the UI thread; failures surface as a plain-English status label ("Track Mode could not start: …").

**`SoftApManager` (new)** — `Integration/SoftApManager.cs`
- Starts/stops the standalone access point. Implementation mechanism is the #1 Phase 0 research item (§6).
- Exposes: `Start() → (ssid, passphrase, gatewayIp)`, `Stop()`, status events.

**`TrackModePanel` (new UI)** — settings area
- Toggle, status indicator (AP up / server up / N phones not required), and the two QR codes rendered large enough to scan from a phone at arm's length.
- **"Test Track Mode" button**: runs the full capability check (adapter present, SoftAP mechanism supported, listener can bind) and reports a clear pass/fail with reasons. Directors run this at home before race day — never discover an unsupported adapter trackside.
- QR generation via **QRCoder** (NuGet, MIT, works on net48).
  - QR #1: Wi-Fi join string (`WIFI:T:WPA;S:<ssid>;P:<pwd>;;`) — phones auto-join without typing a password.
  - QR #2: scoreboard URL.

**Bundled frontend (new content)**
- Copy the current scoreboard frontend (HTML/CSS/JS) from RCDragLiveServer into the desktop repo (e.g. `src/RCDragManagerProd/Assets/TrackMode/`), with its API base URL made relative (`/api/live`) so the same files work locally. Phase 0 must confirm exactly which endpoint(s) and JSON shape the page consumes so the local server mimics them 1:1.

**Installer / elevated one-time setup (modified)**
- `netsh http add urlacl url=http://+:5000/ user=Everyone` (or app-SID-scoped) so HttpListener can bind without running RCDM as admin.
- Windows Firewall inbound allow rule for the port, scoped to Private networks.
- Both are **plumbing only** — they do not open anything until the listener actually starts. This satisfies C1 + C2 together: elevation happens once at install; the runtime toggle needs no admin rights.

## 5. Director Workflow (Acceptance Walkthrough)

1. Director opens RCDM at the track. No internet, doesn't matter.
2. Settings → flips **Enable Track Mode** ON.
3. App brings up the Wi-Fi AP and local server automatically (< ~10s) and shows two QR codes with one line of instructions: "1. Scan to join Wi-Fi. 2. Scan to open scoreboard."
4. Racers scan both. Scoreboard appears on their phones and refreshes live as the director enters results.
5. End of day: toggle OFF (or just close the app) — AP and server shut down completely.

## 6. Phase 0 — MANDATORY Read-Only Research Pass

Per project rules: research before implementation, report findings, no code changes. CC must answer:

1. **SoftAP mechanism on Windows 10/11 with NO internet — the critical unknown.**
   - Windows "Mobile Hotspot" (Settings UI / `NetworkOperatorTetheringManager`) historically refuses to start without an active connection to share. Verify current behaviour.
   - Legacy `netsh wlan set hostednetwork` is deprecated and unsupported by many modern Wi-Fi drivers. Check support detection (`netsh wlan show drivers` → "Hosted network supported").
   - **Wi-Fi Direct legacy AP** (`Windows.Devices.WiFiDirect.WiFiDirectAdvertisementPublisher` with `IsAutonomousGroupOwnerEnabled` / legacy settings) creates a standalone WPA2 AP with no internet requirement. Confirm it is callable from .NET Framework 4.8 (UWP API via `Windows.winmd` reference or a small helper exe) and that phones can join it like a normal network.
   - Recommend ONE primary mechanism + ONE fallback, with driver-support detection and a clear failure message for unsupported adapters.
   - Note: Wi-Fi Direct/SoftAP gateways typically hand out IPs via their own DHCP (e.g. 192.168.137.x). Confirm the server can discover its own AP-side IP to embed in the QR code.
2. **Frontend contract.** In RCDragLiveServer: identify exactly which static assets make up the public scoreboard page and exactly which endpoint(s)/JSON shape it polls. Confirm the polling interval and whether any server-side rendering or state (e.g. landing/class selection) must be replicated locally.
3. **DTO reuse point.** In RC-Drag-Manager: confirm where `BuildLiveRaceUpdateDto()` is invoked and the cleanest hook to also feed a local cache (so Track Mode works even when `LiveUpdateEnabled`/broadcast is off). Confirm multi-class: is one DTO pushed per class/session or one combined payload?
4. **HttpListener plumbing.** Confirm URL ACL + firewall rule approach, and whether binding to `http://+:5000/` vs the specific AP IP is preferable given the ACL is created at install time.
5. **Captive-portal behaviour.** When phones join a Wi-Fi network with no internet, iOS/Android show "No internet — stay connected?" prompts and may pop a captive-portal sheet. Document expected phone behaviour and any mitigation (this affects racer UX, not architecture).

Phase 0 output: a findings doc in `Docs/claude-context/TRACK-MODE-RESEARCH.md` + a recommendation. **Stop there. No implementation until findings are reviewed and the SoftAP mechanism is validated on Stew's PC (the only available test hardware).**

**Distribution reality:** RCDM is open source and installed on arbitrary, unknown PCs. We cannot validate target hardware in advance. Therefore the SoftAP design must be **detection-first**: at runtime, probe what the machine's Wi-Fi adapter/driver actually supports, pick the best available mechanism, and fail with a plain-English explanation (including what hardware would fix it, e.g. "a USB Wi-Fi adapter that supports hosted networks") rather than a cryptic error. Stew's PC validates the implementation works *somewhere*; detection + messaging handles everywhere else.

## 7. Build Phases (after Phase 0 sign-off)

Each phase = one scoped CC prompt, own branch (`feature/track-mode-*`), own PR, squash-merged. Never combine phases.

- **Phase 1 — `TrackModeServer`**: HttpListener, static asset serving from bundled files, `/api/live` from a stubbed cached DTO. Unit tests (MSTest 4.x, `Assert.ThrowsExactly<T>`) for routing, start/stop, and thread-safe cache swap.
- **Phase 2 — DTO wiring**: hook the existing live-update build path to refresh the Track Mode cache on every state change, independent of the Render push flag. Multi-class supported.
- **Phase 3 — `SoftApManager`**: implement the Phase-0-chosen mechanism with detection + fallback + plain-English errors.
- **Phase 4 — UI + QR**: settings toggle, status panel, QRCoder integration, the two QR codes. Mockup sign-off required before implementation (project rule).
- **Phase 5 — Installer**: URL ACL + firewall rule added to the Inno Setup script; uninstaller removes both.
- **Phase 6 — Field test on Stew's PC**: real phones, full race simulated offline (Wi-Fi adapter only, no ethernet/internet). "Builds clean ≠ works" applies doubly here — phones-in-hand verification required before release. This is the reference validation; other users' hardware is covered by the detection + "Test Track Mode" path.

## 8. Out of Scope

- Any change to RCDragLiveServer / Render / stewmacrc.com.
- Internet failover/sync logic (Render push already just no-ops offline).
- Authentication on the local scoreboard (read-only data on an air-gapped AP; WPA2 passphrase on the AP is the boundary).
- WebSockets/push — keep the existing 5s polling model.
- OBS overlay / Portatree integration (separate workstream).

## 9. Risks

| Risk | Severity | Mitigation |
|------|----------|-----------|
| No reliable no-internet SoftAP API on a given user's Wi-Fi driver (target hardware is unknown — open-source distribution) | **High — feature-blocking per-machine** | Detection-first design: runtime capability probe, mechanism fallback chain (Wi-Fi Direct → hostednetwork), "Test Track Mode" pre-flight button, plain-English unsupported-adapter message with hardware suggestion. Implementation validated on Stew's PC. |
| Phones drop the AP because "no internet" | Medium | Document the "stay connected" prompt in racer instructions; test iOS + Android in Phase 6. |
| HttpListener bind fails without ACL (silent dev-vs-installed difference) | Medium | Installer creates ACL; server start performs a self-check and reports plainly. |
| Frontend assets drift from RCDragLiveServer over time | Low | Note in release runbook: when scoreboard frontend changes server-side, re-copy bundled assets. |

## 10. Acceptance Criteria

1. Fresh install: no open ports, no AP, no listener until toggle is used (verify with `netstat` and Wi-Fi scan).
2. With ALL networking disabled except the Wi-Fi adapter (no ethernet, no internet): toggle ON → AP broadcasts, phone joins via QR #1, scoreboard opens via QR #2, data matches the app's current bracket state.
3. Director enters a result → phone scoreboard reflects it within one poll cycle (≤ ~5s).
4. Multi-class event shows all classes correctly.
5. Toggle OFF / app close → AP gone, port closed.
6. Entire flow achievable by a non-technical director with zero configuration beyond the toggle.
7. On a machine whose adapter supports no SoftAP mechanism, "Test Track Mode" and the toggle both fail gracefully with a plain-English message — no crash, no half-started state.
8. Criteria 1–5 verified end-to-end on Stew's PC with real phones (iOS and Android if available) — the reference test bench for this feature.
