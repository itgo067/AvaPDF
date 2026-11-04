[CmdletBinding()]
param(
    [string]$OutputDirectory = ''
)

$ErrorActionPreference = 'Stop'
$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $root 'artifacts/publish/shell'
}
$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
$source = Join-Path $root 'src/AvaPDF.ShellNative/AvaPdfContextMenu.cpp'
$vsWhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
if (-not (Test-Path $vsWhere -PathType Leaf)) {
    throw '未找到 vswhere.exe，无法定位 MSVC。'
}
$installation = & $vsWhere -latest -prerelease -products * `
    -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 `
    -property installationPath | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($installation)) {
    throw '未找到 MSVC x64 工具链。'
}
$vcVars = Join-Path $installation 'VC/Auxiliary/Build/vcvars64.bat'
if (-not (Test-Path $vcVars -PathType Leaf)) {
    throw "未找到 vcvars64.bat：$vcVars"
}

[System.IO.Directory]::CreateDirectory($OutputDirectory) | Out-Null
$output = Join-Path $OutputDirectory 'AvaPDF.Shell.dll'
$definition = Join-Path $root 'src/AvaPDF.ShellNative/AvaPDF.Shell.def'
$command = 'call "{0}" >nul && cl.exe /LD /MT /EHsc /std:c++17 /W4 /O2 /utf-8 "{1}" /link /DEF:"{2}" /OUT:"{3}" user32.lib shell32.lib ole32.lib' -f $vcVars, $source, $definition, $output
& cmd.exe /d /c $command
if ($LASTEXITCODE -ne 0) {
    throw "Shell Extension 编译失败，退出码：$LASTEXITCODE"
}
Write-Host "Shell Extension 已生成：$output"
