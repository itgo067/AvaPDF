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
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\.pdf\Shell\AvaPDF"; ValueType: string; ValueName: ""; ValueData: "AvaPDF"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\.pdf\Shell\AvaPDF"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\AvaPDF.Desktop.exe"""; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\.pdf\Shell\AvaPDF"; ValueType: string; ValueName: "SubCommands"; ValueData: "AvaPDF.Merge;AvaPDF.Split;AvaPDF.Extract;AvaPDF.Rotate;AvaPDF.Optimize;AvaPDF.Watermark;AvaPDF.Encrypt;AvaPDF.Decrypt;AvaPDF.ExtractText"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\.pdf\Shell\AvaPDF.Merge"; ValueType: string; ValueName: ""; ValueData: "合并 PDF"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\.pdf\Shell\AvaPDF.Merge\Command"; ValueType: string; ValueName: ""; ValueData: """{app}\AvaPDF.Desktop.exe"" --tool merge --file ""%1"""; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\.pdf\Shell\AvaPDF.Split"; ValueType: string; ValueName: ""; ValueData: "拆分 PDF"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\.pdf\Shell\AvaPDF.Split\Command"; ValueType: string; ValueName: ""; ValueData: """{app}\AvaPDF.Desktop.exe"" --tool split --file ""%1"""; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\.pdf\Shell\AvaPDF.Extract"; ValueType: string; ValueName: ""; ValueData: "提取页面"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\.pdf\Shell\AvaPDF.Extract\Command"; ValueType: string; ValueName: ""; ValueData: """{app}\AvaPDF.Desktop.exe"" --tool extract --file ""%1"""; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\.pdf\Shell\AvaPDF.Rotate"; ValueType: string; ValueName: ""; ValueData: "旋转页面"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\.pdf\Shell\AvaPDF.Rotate\Command"; ValueType: string; ValueName: ""; ValueData: """{app}\AvaPDF.Desktop.exe"" --tool rotate --file ""%1"""; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\.pdf\Shell\AvaPDF.Optimize"; ValueType: string; ValueName: ""; ValueData: "优化 PDF"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\.pdf\Shell\AvaPDF.Optimize\Command"; ValueType: string; ValueName: ""; ValueData: """{app}\AvaPDF.Desktop.exe"" --tool optimize --file ""%1"""; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\.pdf\Shell\AvaPDF.Watermark"; ValueType: string; ValueName: ""; ValueData: "添加水印"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\.pdf\Shell\AvaPDF.Watermark\Command"; ValueType: string; ValueName: ""; ValueData: """{app}\AvaPDF.Desktop.exe"" --tool watermark --file ""%1"""; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\.pdf\Shell\AvaPDF.Encrypt"; ValueType: string; ValueName: ""; ValueData: "加密 PDF"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\.pdf\Shell\AvaPDF.Encrypt\Command"; ValueType: string; ValueName: ""; ValueData: """{app}\AvaPDF.Desktop.exe"" --tool encrypt --file ""%1"""; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\.pdf\Shell\AvaPDF.Decrypt"; ValueType: string; ValueName: ""; ValueData: "解密 PDF"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\.pdf\Shell\AvaPDF.Decrypt\Command"; ValueType: string; ValueName: ""; ValueData: """{app}\AvaPDF.Desktop.exe"" --tool decrypt --file ""%1"""; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\.pdf\Shell\AvaPDF.ExtractText"; ValueType: string; ValueName: ""; ValueData: "提取文本"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\.pdf\Shell\AvaPDF.ExtractText\Command"; ValueType: string; ValueName: ""; ValueData: """{app}\AvaPDF.Desktop.exe"" --tool extracttext --file ""%1"""; Flags: uninsdeletekey

[Run]
Filename: "{app}\AvaPDF.Desktop.exe"; Description: "启动 AvaPDF"; Flags: postinstall nowait skipifsilent unchecked
