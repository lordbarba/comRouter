#define ApplicationName "ComRouter"
#define ApplicationVersion "1.0"
#define Comapny "JBTechnology"
#define MainExecutable "CommRouter.exe"
#define PublishFolder "..\src\Backend\publish"

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{A3F2C7D1-84BE-4F92-B1C6-E057A83D6B12}
AppName={#ApplicationName}
AppVersion={#ApplicationVersion}
;AppVerName={#ApplicationName} {#ApplicationVersion}
AppPublisher={#Comapny}
DefaultDirName={autopf}\{#Comapny}\ComRouter
DisableDirPage=yes
UninstallDisplayIcon={app}\{#MainExecutable}
; "ArchitecturesAllowed=x64compatible" specifies that Setup cannot run
; on anything but x64 and Windows 11 on Arm.
ArchitecturesAllowed=x64compatible
; "ArchitecturesInstallIn64BitMode=x64compatible" requests that the
; install be done in "64-bit mode" on x64 or Windows 11 on Arm,
; meaning it should use the native 64-bit Program Files directory and
; the 64-bit view of the registry.
ArchitecturesInstallIn64BitMode=x64compatible
DisableProgramGroupPage=yes
; Uncomment the following line to run in non administrative install mode (install for current user only).
;PrivilegesRequired=lowest
OutputBaseFilename=ComRouterSetup
SolidCompression=yes
WizardStyle=modern
ShowLanguageDialog=no

[Languages]
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[InstallDelete]
; Pulisce la wwwroot del WebServer (SPA React) ad ogni aggiornamento
Type: filesandordirs; Name: "{app}\server\wwwroot"

[Files]
; WinForms client (root publish) + WebServer (publish\server\) — copiati ricorsivamente
Source: "{#PublishFolder}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{autoprograms}\{#ApplicationName}"; Filename: "{app}\{#MainExecutable}"
Name: "{autodesktop}\{#ApplicationName}"; Filename: "{app}\{#MainExecutable}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MainExecutable}"; Description: "{cm:LaunchProgram,{#StringChange(ApplicationName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

