; ----------------------------------------------------------------------------
;  Ledger Accounting — Inno Setup installer script
;  Build:
;     1) Publish the WPF app as a self-contained single-file EXE:
;          dotnet publish src\Ledger.Desktop\Ledger.Desktop.csproj `
;                 -c Release -r win-x64 --self-contained true
;     2) Run Inno Setup Compiler against this .iss file:
;          ISCC.exe deploy\installer\installer.iss
;     3) Sign the resulting Output\Setup-Ledger-x.y.z.exe with your code-signing
;        certificate (signtool.exe sign /fd SHA256 /a ...).
; ----------------------------------------------------------------------------

#define MyAppName         "Ledger Accounting"
#define MyAppShortName    "Ledger"
#define MyAppVersion      "0.1.0"
#define MyAppPublisher    "Ledger Software"
#define MyAppURL          "https://example.com/ledger"
#define MyAppExeName      "Ledger.Desktop.exe"
#define MyAppId           "{{B2E7B0F1-2B6A-4D45-AA00-LEDGER0000001}}"

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/support
AppUpdatesURL={#MyAppURL}/updates
DefaultDirName={autopf}\{#MyAppShortName}
DefaultGroupName={#MyAppShortName}
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#MyAppExeName}
PrivilegesRequired=admin
OutputBaseFilename=Setup-{#MyAppShortName}-{#MyAppVersion}
SetupIconFile=..\icons\app.ico
Compression=lzma2/ultra
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64
ArchitecturesAllowed=x64
MinVersion=10.0

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "ar"; MessagesFile: "compiler:Languages\Arabic.isl"

[Tasks]
Name: "desktopicon";   Description: "{cm:CreateDesktopIcon}";       GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startmenuicon"; Description: "Create a Start Menu shortcut"; GroupDescription: "{cm:AdditionalIcons}";

[Files]
; Source path is relative to this .iss file's location (deploy\installer\).
; The publish folder must already exist (build the app first — see header).
Source: "..\..\src\Ledger.Desktop\bin\Release\net8.0-windows\win-x64\publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\src\Ledger.Desktop\bin\Release\net8.0-windows\win-x64\publish\*.dll";          DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist

[Icons]
Name: "{group}\{#MyAppName}";              Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}";    Filename: "{uninstallexe}"
Name: "{commondesktop}\{#MyAppName}";      Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Keep user data on uninstall by default. Uncomment to wipe:
; Type: filesandordirs; Name: "{userappdata}\Ledger"
