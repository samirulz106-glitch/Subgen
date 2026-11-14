; Inno Setup script for SubGen. Built automatically by GitHub Actions.

#define MyAppName "SubGen"
#define MyAppVersion "1.1.8"

[Setup]
SetupIconFile=..\Subgen\subgen.ico
AppId={{B6E2A2B4-7C1D-4C57-9B8E-5A1F0C3D9E21}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=output
OutputBaseFilename=SubGen-Setup
Compression=lzma2/fast
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\SubGen.exe

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked
Name: "contextmenu"; Description: "Add ""Generate subtitles (SubGen)"" to the right-click menu of video files"

[Files]
Source: "..\stage\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{autoprograms}\SubGen"; Filename: "{app}\SubGen.exe"
Name: "{autodesktop}\SubGen"; Filename: "{app}\SubGen.exe"; Tasks: desktopicon

[Registry]
; Right-click menu entries (current user only, removed on uninstall)
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\.mp4\shell\SubGen"; ValueType: string; ValueName: ""; ValueData: "Generate subtitles (SubGen)"; Flags: uninsdeletekey; Tasks: contextmenu
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\.mp4\shell\SubGen\command"; ValueType: string; ValueName: ""; ValueData: """{app}\SubGen.exe"" ""%1"""; Tasks: contextmenu
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\.mkv\shell\SubGen"; ValueType: string; ValueName: ""; ValueData: "Generate subtitles (SubGen)"; Flags: uninsdeletekey; Tasks: contextmenu
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\.mkv\shell\SubGen\command"; ValueType: string; ValueName: ""; ValueData: """{app}\SubGen.exe"" ""%1"""; Tasks: contextmenu
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\.avi\shell\SubGen"; ValueType: string; ValueName: ""; ValueData: "Generate subtitles (SubGen)"; Flags: uninsdeletekey; Tasks: contextmenu
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\.avi\shell\SubGen\command"; ValueType: string; ValueName: ""; ValueData: """{app}\SubGen.exe"" ""%1"""; Tasks: contextmenu
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\.mov\shell\SubGen"; ValueType: string; ValueName: ""; ValueData: "Generate subtitles (SubGen)"; Flags: uninsdeletekey; Tasks: contextmenu
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\.mov\shell\SubGen\command"; ValueType: string; ValueName: ""; ValueData: """{app}\SubGen.exe"" ""%1"""; Tasks: contextmenu
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\.m4v\shell\SubGen"; ValueType: string; ValueName: ""; ValueData: "Generate subtitles (SubGen)"; Flags: uninsdeletekey; Tasks: contextmenu
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\.m4v\shell\SubGen\command"; ValueType: string; ValueName: ""; ValueData: """{app}\SubGen.exe"" ""%1"""; Tasks: contextmenu
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\.webm\shell\SubGen"; ValueType: string; ValueName: ""; ValueData: "Generate subtitles (SubGen)"; Flags: uninsdeletekey; Tasks: contextmenu
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\.webm\shell\SubGen\command"; ValueType: string; ValueName: ""; ValueData: """{app}\SubGen.exe"" ""%1"""; Tasks: contextmenu

[Run]
Filename: "{app}\SubGen.exe"; Description: "Launch SubGen"; Flags: nowait postinstall skipifsilent
