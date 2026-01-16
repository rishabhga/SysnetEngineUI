[Setup]
AppName=SysNet ManageEngine
AppVersion=1.0
DefaultDirName={pf}\sysnetManageEngine
OutputDir=Output
OutputBaseFilename=setup
Compression=lzma
SolidCompression=yes

[Files]
Source: "ManageEngineclientApplicationExe.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "config.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "*.dll"; DestDir: "{app}"; Flags: ignoreversion

[Run]
Filename: "{app}\ManageEngineclientApplicationExe.exe"; Description: "Launch after install"; Flags: postinstall nowait skipifsilent
