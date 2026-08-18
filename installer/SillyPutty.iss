#define MyAppName "SillyPutty"
#ifndef MyAppVersion
  #define MyAppVersion "0.2.1-alpha"
#endif
#define MyAppPublisher "CtrlAltWiz"
#define MyAppExeName "SillyPutty.exe"

[Setup]
AppId={{B3EF38B1-821B-4E19-97AF-6BC1B371440E}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\SillyPutty
DefaultGroupName=SillyPutty
OutputDir=..\artifacts
OutputBaseFilename=SillyPutty-{#MyAppVersion}-Setup
SetupIconFile=..\Assets\sillyputty.ico
Compression=lzma
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest

[Files]
Source: "..\artifacts\SillyPutty-win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\SillyPutty"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\SillyPutty"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional icons:"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch SillyPutty"; Flags: nowait postinstall skipifsilent
