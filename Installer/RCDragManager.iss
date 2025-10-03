; ===== RC Drag Manager — Inno Setup Script (FINAL) =====
; Layout:
;   installer\
;     payload\   (bin\Release\ contents)
;     RCDragManager.iss

#define MyAppName        "RC Drag Manager"
#define MyAppVersion     "1.0.0"
#define MyAppPublisher   "Stewart McMillan"
#define MyAppURL         "https://github.com/stewmac570/RC-Drag-Manager"
#define MyAppExeName     "RCDragManagerProd.exe"
#define MyCompanyAppData "RC_Drag_Manager"
#define MyAppId          "{{A41B3B69-3B2F-4C2F-9D0A-3A2C3F1F8F8B}}"

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
; install per-user in writable location
DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
AllowNoIcons=yes
OutputDir=output
OutputBaseFilename=RC-Drag-Manager-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64 x86
ArchitecturesInstallIn64BitMode=x64
; no elevation, avoids admin %APPDATA% issues
PrivilegesRequired=lowest
UsePreviousAppDir=yes
UsePreviousLanguage=yes
SetupLogging=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Dirs]
; Logger target: %APPDATA%\RC_Drag_Manager\
Name: "{userappdata}\{#MyCompanyAppData}"; Flags: uninsalwaysuninstall

[Files]
Source: "payload\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Optional app icon:
; SetupIconFile=payload\app.ico

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
; Per-user desktop shortcut (no admin needed)
Name: "{userdesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon


[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; WorkingDir: "{app}"; Flags: nowait postinstall skipifsilent runasoriginaluser


[Code]
const
  DOTNET_472_RELEASE_MIN = 461808;

function IsDotNet472OrHigherInstalled: Boolean;
var
  Release: Cardinal;
  Success: Boolean;
begin
  Success := RegQueryDWordValue(HKLM,
    'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release);
  if not Success and IsWin64 then
    Success := RegQueryDWordValue(HKLM32,
      'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release);
  Result := Success and (Release >= DOTNET_472_RELEASE_MIN);
end;

function InitializeSetup: Boolean;
begin
  if not IsDotNet472OrHigherInstalled then
  begin
    MsgBox(
      'Microsoft .NET Framework 4.7.2 or newer is required.'#13#10 +
      'Install .NET 4.7.2+ and run this setup again.',
      mbError, MB_OK);
    Result := False;
    exit;
  end;
  Result := True;
end;
