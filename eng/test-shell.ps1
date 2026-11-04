[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$source = Join-Path $root 'src/AvaPDF.ShellNative/TestShell.cpp'
$outputDirectory = Join-Path $root 'artifacts/shell-tests'
[System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
$output = Join-Path $outputDirectory 'TestShell.exe'
$vsWhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
if (-not (Test-Path $vsWhere -PathType Leaf)) {
    throw '未找到 vswhere.exe。'
}
$installation = & $vsWhere -latest -prerelease -products * `
    -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 `
    -property installationPath | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($installation)) {
    throw '未找到 MSVC x64 工具链。'
}
$vcVars = Join-Path $installation 'VC/Auxiliary/Build/vcvars64.bat'
$command = 'call "{0}" >nul && cl.exe /EHsc /std:c++17 /W4 /utf-8 "{1}" /link /OUT:"{2}" user32.lib shell32.lib ole32.lib' -f $vcVars, $source, $output
& cmd.exe /d /c $command
if ($LASTEXITCODE -ne 0) {
    throw "COM 测试程序编译失败，退出码：$LASTEXITCODE"
}

& $output
if ($LASTEXITCODE -ne 0) {
    throw "COM 创建或 QueryContextMenu 验证失败，退出码：$LASTEXITCODE"
}
Write-Host 'COM 创建和 QueryContextMenu 验证通过。'
