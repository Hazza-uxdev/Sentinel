#define MyAppName "Sentinel"
#define MyAppVersion "0.1.0"
#define MyAppPublisher "Hazza-uxdev"
#define MyAppExeName "Sentinel.exe"

[Setup]
AppId={{B1F9F5F9-4D4F-42C5-9A98-72E70A73FA3E}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=Output
OutputBaseFilename=SentinelSetup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
SetupIconFile=..\Assets\Sentinel.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional icons:"; Flags: unchecked

[Files]
Source: "..\bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Sentinel"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall Sentinel"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Sentinel"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch Sentinel"; Flags: nowait postinstall skipifsilent
