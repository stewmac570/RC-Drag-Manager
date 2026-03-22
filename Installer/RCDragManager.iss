#define MyAppName        "RC Drag Manager"
#define MyAppPublisher   "Stewart McMillan"
#define MyAppURL         "https://github.com/stewmac570/RC-Drag-Manager"
#define MyAppExeName     "RCDragManagerProd.exe"
#define MyCompanyAppData "RC_Drag_Manager"
#define MyAppId          "{{A41B3B69-3B2F-4C2F-9D0A-3A2C3F1F8F8B}}"

#ifndef MyAppVersion
  #define MyAppVersion "1.0.0.0"
#endif

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
AllowNoIcons=yes
OutputDir=output
OutputBaseFilename=RC-Drag-Manager-Setup-{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
UsePreviousAppDir=yes
UsePreviousLanguage=yes
SetupLogging=yes
SetupIconFile=..\src\RCDragManagerProd\Assets\retro trans icon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
VersionInfoVersion={#MyAppVersion}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Dirs]
; Preserve roaming AppData content because the app stores its DB, logs, and settings there.
Name: "{userappdata}\{#MyCompanyAppData}"

[Files]
Source: "Payload\*"; DestDir: "{app}"; Excludes: "README.txt"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{userdesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; WorkingDir: "{app}"; Flags: nowait postinstall skipifsilent runasoriginaluser

[Code]
const
  DOTNET_48_RELEASE_MIN = 528040;

function IsDotNet48OrHigherInstalled: Boolean;
var
  Release: Cardinal;
  Success: Boolean;
begin
  Success := RegQueryDWordValue(HKLM,
    'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release);
  if not Success and IsWin64 then
    Success := RegQueryDWordValue(HKLM32,
      'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release);
  Result := Success and (Release >= DOTNET_48_RELEASE_MIN);
end;

function InitializeSetup: Boolean;
begin
  if not IsDotNet48OrHigherInstalled then
  begin
    MsgBox(
      'Microsoft .NET Framework 4.8 or newer is required.'#13#10 +
      'Install .NET Framework 4.8 and run this setup again.',
      mbError, MB_OK);
    Result := False;
    exit;
  end;

  Result := True;
end;
