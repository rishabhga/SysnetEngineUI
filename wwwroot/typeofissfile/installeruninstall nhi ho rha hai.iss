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

[Code]
var
  PasswordPage: TInputQueryWizardPage;

function InitializeUninstall(): Boolean;
begin
  PasswordPage := CreateInputQueryPage(
    wpWelcome,
    'Uninstall Password Required',
    'Please enter the password to uninstall SysNet ManageEngine:',
    'Enter password below to continue.'
  );

  PasswordPage.Add('Password:', True); // True = password hidden
  PasswordPage.Values[0] := '';
  //PasswordPage.Show;

  if PasswordPage.Values[0] <> 'admin123' then
  begin
    MsgBox('Incorrect password. Uninstallation cancelled.', mbError, MB_OK);
    Result := False;
  end
  else
  begin
    Result := True;
  end;
end;
