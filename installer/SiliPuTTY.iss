#define MyAppName "SiliPuTTY"
#ifndef MyAppVersion
  #define MyAppVersion "0.3.1-alpha"
#endif
#define MyAppPublisher "CtrlAltWiz"
#define MyAppExeName "SiliPuTTY.exe"

[Setup]
AppId={{B3EF38B1-821B-4E19-97AF-6BC1B371440E}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\SiliPuTTY
DefaultGroupName=SiliPuTTY
OutputDir=..\artifacts
OutputBaseFilename=SiliPuTTY-{#MyAppVersion}-Setup
SetupIconFile=..\Assets\siliputty.ico
Compression=lzma
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest

[Files]
Source: "..\artifacts\SiliPuTTY-win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\SiliPuTTY"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\SiliPuTTY"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional icons:"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch SiliPuTTY"; Flags: nowait postinstall skipifsilent
