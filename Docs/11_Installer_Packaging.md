# RC Drag Manager — Installer & Packaging Guide  
**File:** 11_Installer_Packaging.md  
**Version:** 1.01  
**Status:** ✅ Stable (ChatGPT-Pack Ready)  
**Last Updated:** 2025-10-12  
**Owner:** Stewart McMillan  
**Source of Truth:** Based on `Installer\RCDragManager.iss` and verified Inno Setup folder structure.

---

## 🤖 How ChatGPT Should Use This Doc

Use this document to understand how RC Drag Manager is built and packaged for installation using **Inno Setup**.  
It explains:
- Folder structure under `/Installer/`.  
- How to update and compile the `.iss` script.  
- Version tagging and build workflow.  
- Output file naming and release process.  

See also:  
- `10_Race_Log_and_Reporting.md` — race log export locations.  
- `09_Error_Handling_Logging.md` — log persistence and structure.  
- `13_Project_Status_Summary.md` — release milestone tracking.  

---

## 🎯 Purpose

Provide a **repeatable and automated build process** that produces a clean, versioned Windows installer using Inno Setup.  
This installer bundles all runtime files, assets, and configuration folders required for end users.

---

## 🧱 Build Overview

| Mode | Output | Purpose |
|------|---------|----------|
| **Debug** | `/bin/Debug/RCDragManagerProd.exe` | Development testing. |
| **Release** | `/bin/Release/RCDragManagerProd.exe` | Production-ready build used for installer packaging. |

### Requirements
- Visual Studio 2022+  
- .NET Framework 4.8  
- Inno Setup Compiler (v6.2 or later)  
- 64-bit Windows build environment  

---

## 🧩 Folder Structure

```
RC-Drag-Manager/
│
├── src/
│   └── RCDragManagerProd/
│       └── bin/
│           └── Release/
│               └── RCDragManagerProd.exe
│
├── Installer/
│   │   RCDragManager.iss         ← Inno Setup script
│   │
│   ├── Payload/                  ← Files copied from /bin/Release before building
│   │   ├── RCDragManagerProd.exe
│   │   ├── Configs/
│   │   ├── Assets/
│   │   ├── RaceLogs/
│   │   └── Reports/
│   │
│   └── output/                   ← Compiled installer (.exe)
│
└── Releases/
    └── (archived installers)
```

---

## ⚙️ Inno Setup Script (`RCDragManager.iss`)

```ini
; ========================================================
; RC Drag Manager — Installer Script
; Author: Stewart McMillan
; Last Updated: 2025-10-12
; ========================================================

[Setup]
AppName=RC Drag Manager
AppVersion=1.0.0
AppPublisher=GJAMES Software
DefaultDirName={autopf}\RC Drag Manager
DefaultGroupName=RC Drag Manager
OutputDir=output
OutputBaseFilename=RCDragManager_Setup
Compression=lzma
SolidCompression=yes
PrivilegesRequired=lowest
SetupIconFile=Payload\Assets\Icons\app.ico

[Files]
Source: "Payload\RCDragManagerProd.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "Payload\Configs\*"; DestDir: "{app}\Configs"; Flags: recursesubdirs createallsubdirs
Source: "Payload\Assets\*"; DestDir: "{app}\Assets"; Flags: recursesubdirs createallsubdirs
Source: "Payload\RaceLogs\*"; DestDir: "{app}\RaceLogs"; Flags: recursesubdirs createallsubdirs
Source: "Payload\Reports\*"; DestDir: "{app}\Reports"; Flags: recursesubdirs createallsubdirs
Source: "Payload\LICENSE.txt"; DestDir: "{app}"
Source: "Payload\README.txt"; DestDir: "{app}"

[Icons]
Name: "{autoprograms}\RC Drag Manager"; Filename: "{app}\RCDragManagerProd.exe"
Name: "{desktop}\RC Drag Manager"; Filename: "{app}\RCDragManagerProd.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a &Desktop Shortcut"; GroupDescription: "Additional Icons:"; Flags: unchecked

[Run]
Filename: "{app}\RCDragManagerProd.exe"; Description: "Launch RC Drag Manager"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}\Logs"
Type: filesandordirs; Name: "{app}\RaceLogs"
Type: filesandordirs; Name: "{app}\Reports"
```

---

## 🏗️ Build Workflow

### 1️⃣ Prepare Payload
- Build project in **Release** mode.  
- Copy latest files from:
  ```
  src\RCDragManagerProd\bin\Release\
  ```
  → to:
  ```
  Installer\Payload\
  ```

### 2️⃣ Update Version
Edit these fields in `RCDragManager.iss`:
```ini
AppVersion=1.1.0
OutputBaseFilename=RCDragManager_v1.1.0_Setup
```

### 3️⃣ Compile
Open `RCDragManager.iss` in **Inno Setup Compiler** and choose:  
`Build → Compile`  

Output:  
```
Installer\output\RCDragManager_v1.1.0_Setup.exe
```

---

## 🧩 Post-Install Behavior

| Action | Result |
|--------|--------|
| **Install Path** | `C:\Program Files\RC Drag Manager\` |
| **Shortcuts** | Start Menu + optional Desktop |
| **Run After Install** | App launches automatically |
| **Log Folder** | Created in `%APPDATA%\RC_Drag_Manager\Logs` |
| **Race Logs & Reports** | Saved to user-accessible directories (`C:\Temp\RaceLogs`) |

---

## 🧮 Version Tagging

| Field | Example | Purpose |
|--------|----------|----------|
| `AppVersion` | `1.1.0` | Installer version displayed in Control Panel. |
| `AssemblyVersion` | `1.1.*` | Incremented per build. |
| `OutputBaseFilename` | `RCDragManager_v1.1.0_Setup.exe` | Versioned file name. |

Build scripts may auto-update these values during release automation.

---

## 🔐 Code Signing (Optional)

If code-signing is required:
```bash
signtool sign /f "cert.pfx" /p "<password>" /tr http://timestamp.digicert.com /td sha256 "RCDragManager_v1.1.0_Setup.exe"
```

This prevents SmartScreen or Defender warnings when distributing externally.

---

## 🧩 Updating the Installer

| Step | Action |
|------|---------|
| 1 | Build Release version of app |
| 2 | Copy binaries into `Installer\Payload` |
| 3 | Update version fields in `.iss` |
| 4 | Compile via Inno Setup |
| 5 | Copy generated setup file to `/Releases/` |

---

## 🧱 Optional Enhancements

| Feature | Description |
|----------|-------------|
| **Silent Install Flag** | Add `/silent` or `/verysilent` support. |
| **Custom Uninstall Logic** | Clean up old race logs or configs. |
| **Update Channel** | Replace payload dynamically for auto-updates. |
| **Registry Keys** | Record install path under `HKCU\Software\RCDragManager`. |

---

## 🧩 README Template

Each installer includes `README.txt`:

```
=== RC Drag Manager v1.1.0 ===
Installation: Completed successfully.
Default Location: C:\Program Files\RC Drag Manager\
Logs: %APPDATA%\RC_Drag_Manager\Logs
Race Logs: C:\Temp\RaceLogs
Reports: C:\Temp\RaceReports

Support: GJAMES Software — Internal Development Team
```

---

## 🧱 Adjacent Docs

| File | Purpose |
|------|----------|
| `12_Configuration.md` | Defines runtime settings and paths. |
| `09_Error_Handling_Logging.md` | Persistent log structure. |
| `10_Race_Log_and_Reporting.md` | Output directories for event logs. |
| `13_Project_Status_Summary.md` | Build/release tracking. |

---

## ✅ Summary

RC Drag Manager’s installer is built using **Inno Setup**, providing a clean, professional installation experience.  
Each release follows a controlled pipeline:
1. Build → Copy to Payload  
2. Update version → Compile with Inno Setup  
3. Output installer → `/Installer/output/`  

The result is a lightweight `.exe` installer that:
- Creates clean directories under Program Files.  
- Sets up Start Menu and Desktop shortcuts.  
- Preserves user data under `%APPDATA%` and `C:\Temp`.  
- Is easily versioned, signed, and distributed.

---
