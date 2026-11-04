#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif
#ifndef PayloadDir
  #define PayloadDir "..\..\artifacts\publish\win-x64-aot"
#endif
#ifndef OutputDir
  #define OutputDir "..\..\artifacts\publish"
#endif
#ifndef OutputBaseFilename
  #define OutputBaseFilename "AvaPDF-" + AppVersion + "-win-x64-setup"
#endif

[Setup]
AppId={{D8F93822-2B84-4F4A-9E4B-5BDB49CDB56F}
AppName=AvaPDF
AppVersion={#AppVersion}
AppVerName=AvaPDF {#AppVersion}
AppPublisher=AvaPDF
DefaultDirName={localappdata}\Programs\AvaPDF
DefaultGroupName=AvaPDF
OutputDir={#OutputDir}
OutputBaseFilename={#OutputBaseFilename}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\AvaPDF.exe
CloseApplications=yes
RestartApplications=no
ShowLanguageDialog=no

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "其他任务:"; Flags: unchecked

[Files]
Source: "{#PayloadDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb"

[Icons]
Name: "{group}\AvaPDF"; Filename: "{app}\AvaPDF.exe"; WorkingDir: "{app}"
Name: "{userdesktop}\AvaPDF"; Filename: "{app}\AvaPDF.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Classes\CLSID\{{6F4A17D2-6A4B-4F1E-A6B3-0A9A0DFD0001}\InprocServer32"; ValueType: string; ValueName: ""; ValueData: "{app}\shell\AvaPDF.Shell.dll"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\CLSID\{{6F4A17D2-6A4B-4F1E-A6B3-0A9A0DFD0001}\InprocServer32"; ValueType: string; ValueName: "ThreadingModel"; ValueData: "Both"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\.pdf\ShellEx\ContextMenuHandlers\AvaPDF"; ValueType: string; ValueName: ""; ValueData: "{{6F4A17D2-6A4B-4F1E-A6B3-0A9A0DFD0001}"; Flags: uninsdeletekey

[Code]
function GetPreviousUninstallString: String;
var
  UninstallString: String;
begin
  Result := '';
  if not RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{D8F93822-2B84-4F4A-9E4B-5BDB49CDB56F}_is1', 'UninstallString', UninstallString) then
    UninstallString := '';
  Result := UninstallString;
end;

function InitializeSetup(): Boolean;
var
  PreviousUninstaller: String;
  ResultCode: Integer;
  Timeout: Integer;
begin
  Result := True;
  PreviousUninstaller := GetPreviousUninstallString;
  if PreviousUninstaller <> '' then
  begin
    if not WizardSilent then
    begin
      if MsgBox('检测到已安装的 AvaPDF 旧版本。安装程序将先卸载旧版本，是否继续？', mbConfirmation, MB_YESNO) = IDNO then
      begin
        Result := False;
        exit;
      end;
    end;
    if not Exec(RemoveQuotes(PreviousUninstaller), '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
    begin
      MsgBox('无法启动旧版本卸载程序，请先手动卸载后重试。', mbError, MB_OK);
      Result := False;
      exit;
    end;
    if ResultCode <> 0 then
    begin
      MsgBox('旧版本卸载未完成，请先手动卸载后重试。', mbError, MB_OK);
      Result := False;
      exit;
    end;
  end;
  RegDeleteKeyIncludingSubkeys(HKCU, 'Software\Classes\SystemFileAssociations\.pdf\Shell\AvaPDF');
  RegDeleteKeyIncludingSubkeys(HKCU, 'Software\Classes\SystemFileAssociations\.pdf\Shell\AvaPDFOptimize');
  RegDeleteValue(HKCU, 'Software\Classes\SystemFileAssociations\.pdf\Shell\AvaPDF', '');

  Timeout := 600;
  while (Timeout > 0) and RegKeyExists(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{D8F93822-2B84-4F4A-9E4B-5BDB49CDB56F}_is1') do
  begin
    Sleep(100);
    Dec(Timeout);
  end;
  if RegKeyExists(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{D8F93822-2B84-4F4A-9E4B-5BDB49CDB56F}_is1') then
  begin
    MsgBox('旧版本卸载超时，请先手动卸载后重试。', mbError, MB_OK);
    Result := False;
    exit;
  end;
end;

[Run]
Filename: "{app}\AvaPDF.exe"; Description: "启动 AvaPDF"; Flags: postinstall nowait skipifsilent unchecked
