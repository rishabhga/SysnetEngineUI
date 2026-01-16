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
  // Create a password prompt page
  PasswordPage := CreateInputQueryPage(
    wpWelcome,
    'Uninstall Password Required',
    'Please enter the password to uninstall SysNet ManageEngine:',
    'Enter the correct password to proceed with uninstallation.'
  );

  PasswordPage.Add('Password:', False); // False hides the input
  PasswordPage.Values[0] := '';

  PasswordPage.Show;

  // Check password - change "admin123" to your preferred password
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
