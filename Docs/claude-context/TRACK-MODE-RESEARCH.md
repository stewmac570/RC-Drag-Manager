# Track Mode — Phase 0 Research Findings

*Status: **SHELVED 2026-06-15** — Phase 0 done, build not started and not planned.
Spec: [`OFFLINE-LOCAL-SERVER-SPEC.md`](OFFLINE-LOCAL-SERVER-SPEC.md).*

## Why this is shelved

The original concept had the race director's **PC** broadcast the Wi-Fi and host the
scoreboard. Two problems killed that form of it:

1. **RF reality.** A laptop's internal Wi-Fi (and on the reference PC, only Wi-Fi
   Direct — Soft AP/hosted network is unsupported) is a weak, short-range radio.
   Across a track's pits and parking, racers walking to their cars drop out of range,
   and phones abandon a no-internet network rather than rejoining it cleanly. The
   coverage would be small and flaky.
2. **The simpler fix wins.** Stew already owns a **Mercusys MB230-4G** 4G LTE router.
   With a data SIM it provides real internet at the venue, so the **existing**
   stewmacrc.com live site works with **zero new code**. A real router also broadcasts
   a far stronger, more stable network than any laptop. So the practical answer to
   "no internet at the venue" is "bring internet" (SIM in the router), not "build an
   offline server."

**Track Mode (PC-hosted offline scoreboard) is only worth revisiting if venues with
no cellular coverage at all prove common.** If that happens, the same router with no
SIM still works as a strong standalone AP — PC plugs into a LAN port by Ethernet and
serves the page — which removes the riskiest part of the build below (the whole
SoftAP / Wi-Fi Direct research item in §1 becomes moot). The findings below are kept
for that scenario.

---

*Original Phase 0 findings (read-only research, no implementation done):*

*Researched: 2026-06-15. Reference hardware: Stew's PC (the only validation bench).*

---

## TL;DR — the two findings that change the plan

1. **SoftAP is dead on the reference PC; Wi-Fi Direct is the only path.** The Intel
   Wi-Fi 6 AX201 reports **`Hosted network supported: No`** and **`Soft AP: Not
   supported`**, but **`Wi-Fi Direct GO: Supported`** (up to 8 clients, 5 GHz GO
   OK). So the `netsh wlan set hostednetwork` fallback in the spec is *not a
   fallback on this machine* — it cannot run at all. The design must be
   **Wi-Fi Direct (legacy GO) first**, and the only meaningful "fallback" is the
   modern Mobile Hotspot API, not hostednetwork. Detection-first messaging matters
   even more than the spec assumed.

2. **The live scoreboard is NOT a static site that polls `/api/live`.** It is
   **server-rendered HTML built as C# strings** inside `PublicLiveController` on the
   .NET 8 server, and the phone page **does a full `location.reload()` every 5s** —
   it never consumes `/api/live` as JSON. Spec constraints **C4 ("reuse the existing
   mobile scoreboard UI… bundle the frontend assets")** and **C5 ("serve the same
   JSON shape `/api/live`")** are based on an assumption that does not hold. There
   are no frontend asset files to bundle, and `/api/live` is not what renders the
   page. This needs a decision before Phase 1 (see §2).

Everything else (DTO hook, HttpListener plumbing, captive portal) is as the spec
expected and is low-risk.

---

## 1. SoftAP mechanism — the critical unknown

### Reference PC capability probe (actual output)

`netsh wlan show drivers`:
- `Hosted network supported  : No`

`netsh wlan show wirelesscapabilities`:
- `Soft AP                  : Not supported`
- `Wi-Fi Direct Device      : Supported`
- `Wi-Fi Direct GO          : Supported`
- `P2P GO on 5 GHz          : Supported`
- `P2P Max Mobile AP Clients: 8`
- Adapter: Intel(R) Wi-Fi 6 AX201 160MHz, driver 24.20.0.4

**Conclusion:** the only standalone-AP mechanism this adapter exposes is **Wi-Fi
Direct acting as Group Owner**. 8 simultaneous clients is enough for a club race.

### Mechanism ranking (revised from the spec)

| Rank | Mechanism | Verdict on this PC | Notes |
|------|-----------|--------------------|-------|
| 1 | **Wi-Fi Direct legacy GO** via `WiFiDirectAdvertisementPublisher` + `WiFiDirectLegacySettings` | **Only viable option** | `WiFiDirectLegacySettings` lets the GO advertise a normal SSID + WPA2 passphrase so non-Wi-Fi-Direct phones join it "like a normal network". No internet required. Hands out DHCP on its own subnet (typically 192.168.137.x). |
| 2 | Mobile Hotspot (`NetworkOperatorTetheringManager`) | Untested; historically refuses with no upstream connection | Keep as a *detection-time* alternative only. Do **not** assume it works offline. |
| 3 | `netsh wlan set hostednetwork` | **Impossible here** (`Hosted network supported: No`) | Drop as the primary fallback the spec named. Only relevant on older adapters that still expose it. |

### Calling Wi-Fi Direct from .NET Framework 4.8

Feasible but needs interop plumbing — these are WinRT APIs, not classic Win32:
- Reference `Windows.winmd` (the union contract) **and** `System.Runtime.WindowsRuntime`
  so the project can consume `Windows.Devices.WiFiDirect.*` and await `IAsyncOperation`s.
- The relevant types are `WiFiDirectAdvertisementPublisher`,
  `.Advertisement.IsAutonomousGroupOwnerEnabled = true`, and
  `.Advertisement.LegacySettings` (`WiFiDirectLegacySettings` → `IsEnabled`, `Ssid`,
  `Passphrase`).
- Recommend isolating all WinRT calls behind the planned `SoftApManager` so the rest
  of the .NET 4.8 app never touches `winmd` types directly. If interop in-process
  proves fragile, the spec's "small helper exe" option (a tiny UWP/console host that
  the app launches) is the fallback — decide during Phase 3, not now.

### Self-IP discovery for the QR code

Wi-Fi Direct GO brings up its own virtual adapter ("Microsoft Wi-Fi Direct Virtual
Adapter") with a gateway IP it assigns (commonly `192.168.137.1`). The server must
read **that adapter's** IPv4 address at runtime (enumerate `NetworkInterface`s, match
the Wi-Fi Direct virtual adapter) rather than assuming `192.168.137.1`. Embed that IP
in QR #2.

### ⚠️ iOS risk (unchanged, now the top field risk)

Apple devices **cannot create** a Wi-Fi Direct group and have historically been
finicky joining one. The mitigation — and the reason `WiFiDirectLegacySettings`
matters — is that legacy mode makes the GO present as an ordinary WPA2 AP, which iOS
*can* join like any infrastructure AP. **This must be validated with a real iPhone in
Phase 6 before release.** If iOS cannot join reliably, Track Mode is Android-only on
this hardware, which is a product decision for Stew.

---

## 2. Frontend contract — the big surprise

**The public scoreboard has no static assets.** The entire page is generated as C#
string concatenation in
`RCDragLiveServer/src/RCDragLiveServer/Controllers/PublicLiveController.cs`:

- `GET /` → `BuildLandingPage()` — full HTML doc, lists active events.
- `GET /event/{eventId}` → `BuildHomePage()` → `BuildClassPanel()` +
  `BuildDialInForm()` — full HTML doc with **inline `<style>` and inline `<script>`**.
- `GET /api/live` → returns `List<LiveRaceState>` JSON — **exists, but the phone page
  does not poll it.**

The refresh model is **whole-page reload**: the inline script calls
`location.reload()` every 5 seconds (the bracket cycles tabs client-side between
reloads). All the presentation logic — round sorting/relabelling (`RR1`→"Round 1",
`LB-R1`→"Buyback Round 1"), dial-in merge, winner/loser styling, multi-class tabs,
the dial-in submission form — lives **server-side in .NET 8**.

### Why this breaks C4/C5

- **C4 ("bundle the existing frontend assets, don't redesign")** — there are no asset
  files to bundle. The "frontend" is ~700 lines of C# in a .NET 8 controller.
- **C5 ("serve the same JSON shape `/api/live`")** — serving `/api/live` would *not*
  reproduce the page, because the page isn't built from `/api/live`.

### Options (decision needed before Phase 1)

| Option | What Track Mode's HttpListener serves | Pros | Cons |
|--------|---------------------------------------|------|------|
| **A. Port the renderer** | Re-implement `BuildHomePage`/`BuildClassPanel`/etc. in the WinForms app, fed from the local DTO cache; same whole-page-reload model | Pixel-identical to stewmacrc.com; reuses proven markup | Copies ~700 lines of C# into the desktop repo; two copies drift over time (spec risk "frontend assets drift" — now worse) |
| **B. New static SPA** | One bundled `index.html` + JS that polls `/api/live` and renders client-side (the spec's original mental model) | One source of truth the local server serves; matches C5; no server-side HTML in WinForms | Net-new frontend; diverges from the Render page unless the server is *also* migrated to serve it; more work |
| **C. Migrate server first** | Convert `RCDragLiveServer` to the static+`/api/live` model, then both repos share the same static bundle | Cleanest end state; true single source | Largest scope; touches the "out of scope" server repo; not a Phase-0-sized change |

**Recommendation for review:** Option **A** for the first shippable Track Mode (least
risk, identical UX, no server-repo changes — honours "Render is out of scope"), with a
note in the release runbook that the rendering code is now duplicated and must be kept
in sync. Revisit Option C as a later unification if Track Mode proves valuable. **Stew
to decide** — this is the one open product/architecture question from Phase 0.

### Dial-in submission offline

The page includes a dial-in form posting to `POST /api/dialin`, backed on the server
by `IDialInStore` + rate limiter + lock state. If racers should be able to set
dial-ins from their phones in Track Mode, the local server must replicate that store
and the lock semantics. **Suggest deferring dial-in submission for v1 of Track Mode**
(read-only scoreboard) and revisit — it keeps Phase 1–4 much smaller and matches the
spec's "read-only data on the local AP" framing in §8. Flag for Stew.

---

## 3. DTO reuse point — confirmed, clean

- `BuildLiveRaceUpdateDto()` lives in
  [`RaceController.LiveUpdate.cs:18`](../../src/RCDragManagerProd/Controllers/RaceController.LiveUpdate.cs)
  and is private to the controller.
- The single broadcast choke point is **`QueueLiveUpdate(reason)`** (same file,
  line 173): it early-returns when `AppSettings.LiveBroadcastEnabled` is false, else
  builds the DTO and calls `_liveApiClient.SendAsync(dto)`.
- **Cleanest hook:** in `QueueLiveUpdate`, after `BuildLiveRaceUpdateDto()` returns a
  valid DTO, also write the serialized DTO into a Track Mode cache — **before** the
  `LiveBroadcastEnabled` gate so Track Mode works even when Render push is off. (The
  gate currently returns early at the top; the Track Mode write must be reordered to
  sit ahead of it, or live in `BroadcastLiveSnapshot`, which is already the
  "make this class visible" entry point and calls both `QueueLiveUpdate` and
  `BroadcastInitialState`.)
- **Multi-class:** one DTO is built **per class/session** (one `RaceController` per
  tab). The server buckets by event then by `classType` and `/api/live` returns
  `GetAll().Values.ToList()` — a flat list of per-class states. So the Track Mode
  cache must be **keyed by classType and the `/api/live` equivalent must return the
  list of all current class DTOs**, not a single one. `BroadcastInitialState` shows
  the pattern for emitting a roster-only DTO so a class appears before its bracket
  exists — Track Mode should cache those too.

No engine access from request threads is needed: the cache holds pre-serialized JSON,
refreshed on the controller thread during `QueueLiveUpdate`. Matches spec C6/§4.

## 4. HttpListener plumbing — as expected

- `System.Net.HttpListener` on net48 is the right call (spec C6). No port setting
  exists yet in `AppSettings`; add one (default 5000) when Phase 1 lands.
- Binding: prefer the **specific Wi-Fi Direct adapter IP** (`http://<go-ip>:5000/`)
  over `http://+:5000/` so the listener is only reachable on the AP subnet, not on any
  other network the PC might later join. But a specific-IP URL ACL can't be created at
  install time (the IP isn't known until the GO starts). Practical resolution:
  - Install-time: `netsh http add urlacl url=http://+:5000/ user="…"` (wildcard, so no
    admin needed at runtime), **plus** a Windows Firewall inbound allow rule scoped to
    **Private** networks only.
  - Runtime: bind the listener to the wildcard prefix but the firewall + the fact that
    only the AP subnet can route to the PC provides the boundary. Revisit if a tighter
    bind is wanted.
- Server start must self-check the bind and surface a plain-English status, never throw
  to the UI thread (spec §4).

## 5. Captive-portal behaviour — document, don't fight

When a phone joins a Wi-Fi network with no internet:
- **iOS** shows "No Internet Connection" and may pop a captive-portal sheet; the user
  taps to confirm staying connected. Once dismissed, Safari to `http://<go-ip>:5000`
  works.
- **Android** shows "Internet may not be available / stay connected?" — tap keep.

This is a **racer-instruction / UX** matter, not architecture. Put a one-line note on
the QR screen ("Phone says no internet? Tap 'Keep / Stay connected'.") and verify both
platforms in Phase 6. No captive-portal server needed for v1.

---

## Recommended decisions to unblock the build phases

1. **SoftAP:** commit to **Wi-Fi Direct legacy GO** as the primary (and on this
   hardware, only) mechanism. Treat Mobile Hotspot as a detection-time alternative;
   drop hostednetwork as a named fallback. Keep the "Test Track Mode" pre-flight.
2. **Frontend:** choose **Option A (port the renderer into the WinForms app)** for
   Track Mode v1 unless Stew wants to invest in unification (Option C). This is the
   one decision that blocks Phase 1.
3. **Dial-in:** ship Track Mode v1 **read-only** (no phone dial-in submission); revisit.
4. **iOS:** accept it as the top field risk; gate release on a real-iPhone test in
   Phase 6.

Once 1–3 are signed off, Phases 1–6 in the spec can proceed in order. Nothing in Phase
0 contradicts the phase *sequencing* — only the frontend-bundling assumption and the
SoftAP mechanism ranking.

## Sources

- [About the Wi-Fi Direct API — Microsoft Learn](https://learn.microsoft.com/en-us/windows/win32/nativewifi/about-the-wi-fi-direct-api)
- [Wi-Fi Direct sample — Microsoft Learn](https://learn.microsoft.com/en-us/samples/microsoft/windows-universal-samples/wifidirect/)
- [Windows.Devices.WiFiDirect WinRT API reference](https://github.com/MicrosoftDocs/winrt-api/blob/docs//windows.devices.wifidirect/windows_devices_wifidirect.md)
- [Wi-Fi Direct / hotspot on iOS, Android, WP — 7labs](https://7labs.io/mobile/wi-fi-direct-hotspot-ios-android-windows-phone.html)
- [iOS and Wi-Fi Direct — Apple Developer Forums](https://developer.apple.com/forums/thread/12885)
