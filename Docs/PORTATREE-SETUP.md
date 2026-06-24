# Portatree Eliminator Competition — Setup Status

> Status: hardware/software setup notes. This is not evidence that Portatree result ingestion is implemented in RC Drag Manager. See `PORTATREE-RCDM-INTEGRATION.md` for the planned integration design.
*Last updated: May 2026*

---

## Hardware

**Unit:** Portatree Eliminator Competition (Next Gen)
- Full-colour touchscreen, standalone operation
- USB Type A-to-B connection to PC (top-left of unit)
- Enumerates in Windows as **"Communication Device Class ASF Example"** under Ports (COM & LPT)
- **Expected COM port: COM3** (USB CDC / Atmel driver)
- Firmware: v1.15C, Serial: P0039900 (from CCConnect screenshot)

**USB-to-serial adapter (FTDI):** present on **COM6** — this is for scoreboards / dial-in boards, NOT the Eliminator box itself.

**Cable required:** USB Type A (PC) to Type B (Eliminator box top-left port). Standard printer-style cable.

---

## Software Installed

| App | Location | Purpose |
|-----|----------|---------|
| ElimComp.exe | C:\Portatree\ | Full race software — bracket, results, DB |
| newannou.exe | C:\Portatree\ | Announcer screen (second PC on LAN) |
| Project2.exe | C:\Portatree\ | Post-processor / results reporting |
| CCConnect.exe | C:\Portatree\CompetitionConnect\ | Free monitoring tool — download race files, update time slip message |

**Runtime:** All three EXEs are Delphi/CLX apps (not VB6). Runtime dependency is `qtintf70.dll` — confirmed present at `C:\Windows\SysWOW64\qtintf70.dll`.

**BDE:** Borland Database Engine 5.2 installed at `C:\Program Files (x86)\Borland\Common Files\BDE\`. Paradox driver (IDPDX32.DLL) present. Config file `IDAPI32.CFG` last modified 13/09/2024 — was configured during initial setup.

---

## Current Status

### What Works
- ElimComp.exe launches cleanly (run as Administrator from C:\Portatree\)
- Paradox databases load — category "Outlaw" visible, race screen fully functional
- Race Screen, bracket queue, scoreboard controls all render correctly
- AutoSave is ON — results will write to the dated `.db` file automatically after each run

### What Is Blocked

**Error on launch:**
> *"Can Not Read PtsPro.ini Com Port value to Connect to Gold Box"*

**Root cause:** `C:\Portatree\PTSPRO.INI` has no COM port section. The app expects a section (exact key name TBD — not exposed as a static string in the binary, likely in a compiled DFM resource) specifying which COM port to open for the Eliminator box.

**Why we parked it:** Cannot resolve without the box physically present on the other end. With the box plugged in on COM3, the correct port number will be known and testable.

### What To Do When The Box Arrives

1. Plug USB Type A-to-B cable from PC to Eliminator box (top-left port on box).
2. Power on the box. Navigate to **Main Menu** on the touchscreen (not Race Screen — CCConnect requires Main Menu to connect).
3. Open Device Manager → Ports (COM & LPT). Confirm the box appears as **"Communication Device Class ASF Example"**. Note the COM number (expected COM3).
4. If driver doesn't install automatically: right-click the device → Update Driver → Browse my computer → `C:\Portatree\CompetitionConnect\` (Atmel CDC .inf files are there).
5. Launch **CCConnect.exe** first (simpler tool). Click Connect. If it goes green and shows serial number + firmware version, hardware comms are confirmed.
6. Then launch **ElimComp.exe** (as Administrator). Dismiss the COM port popup with OK. Go to **Setup menu** and look for a Com Port / Communications option, OR use the "Open PTSPRO.ini" button on the main screen and manually add:

   ```ini
   [ComPort]
   Port=3
   ```
   (Substitute the actual COM number from step 3 if different.)

7. Restart ElimComp. If the popup is gone and the status panel shows serial/version/config data in green — connected and ready.

---

## File Locations

| File | Path | Purpose |
|------|------|---------|
| PTSPRO.INI | C:\Portatree\PTSPRO.INI | Master config — track settings, paths, COM port (missing) |
| PTSPATHS.INI | C:\Portatree\PTSPATHS.INI | Pointer to PTSPRO.INI location |
| racer.db | C:\Portatree\racer.db | Racer roster (76-field Paradox table) |
| category.db | C:\Portatree\category.db | Category settings (20-field Paradox table) |
| Results DB | C:\Res2024\{YYMMDD}\{YYMMDD}.db | Per-event results, created by ElimComp on session start |
| CCConnect AppData | C:\Users\...\AppData\Roaming\Portatree\Competition\Standalone\ | CCConnect INI only — no race files stored here |

---

## Key Facts About The Next Gen Box

- **Standalone operation** (no PC): full touchscreen race control, stores up to 20 result files internally
- **Entry field:** 6-character alphanumeric (e.g. "007", "1643") — this is what links to RCDM's entry number mapping
- **Dial-in:** entered per lane per race on the touchscreen (0.00–30.00). Both lanes need valid dials or they are cleared at race start
- **Results saved to internal storage:** operator presses clipboard icon → Save. Up to 20 files.
- **CCConnect downloads** internal files to PC as proprietary binary format, then Export converts to CSV
- **ElimComp AutoSave** writes directly to the dated Paradox `.db` results file after each run — no manual save step needed when using PC software

---

## Notes

- ElimComp references "Gold Box" in its strings — this is Portatree's older generation hardware name. The Next Gen box is compatible but the ElimComp version on disk may predate the Next Gen. If connection issues persist after the INI fix, check with Portatree (508-278-2189 / sales@portatree.com) that this ElimComp version supports the Next Gen box.
- Do NOT use "Quick Session" path in ElimComp (the empty RaceSession path in older builds) — use the full Setup → Race flow.
- Build the `RCDragManagerProd` project directly in Visual Studio, not Build Solution — the test project has pre-existing errors that don't affect the main app.
