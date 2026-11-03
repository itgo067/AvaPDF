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
UninstallDisplayIcon={app}\AvaPDF.Desktop.exe
CloseApplications=yes
RestartApplications=no
ShowLanguageDialog=no

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "其他任务:"; Flags: unchecked

[Files]
Source: "{#PayloadDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb"

[Icons]
Name: "{group}\AvaPDF"; Filename: "{app}\AvaPDF.Desktop.exe"; WorkingDir: "{app}"
Name: "{userdesktop}\AvaPDF"; Filename: "{app}\AvaPDF.Desktop.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\.pdf\Shell\AvaPDF"; ValueType: string; ValueName: ""; ValueData: "使用 AvaPDF 处理"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\.pdf\Shell\AvaPDF"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\AvaPDF.Desktop.exe"""; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\.pdf\Shell\AvaPDF\Command"; ValueType: string; ValueName: ""; ValueData: """{app}\AvaPDF.Desktop.exe"" --file ""%1"""; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\.pdf\Shell\AvaPDFOptimize"; ValueType: string; ValueName: ""; ValueData: "使用 AvaPDF 优化 PDF"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\.pdf\Shell\AvaPDFOptimize"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\AvaPDF.Desktop.exe"""; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\.pdf\Shell\AvaPDFOptimize\Command"; ValueType: string; ValueName: ""; ValueData: """{app}\AvaPDF.Desktop.exe"" --tool optimize --file ""%1"""; Flags: uninsdeletekey

[Run]
Filename: "{app}\AvaPDF.Desktop.exe"; Description: "启动 AvaPDF"; Flags: postinstall nowait skipifsilent unchecked
