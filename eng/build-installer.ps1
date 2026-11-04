[CmdletBinding()]
param(
    [string]$PayloadDirectory = '',
    [string]$OutputDirectory = '',
    [string]$InnoCompiler = ''
)

$ErrorActionPreference = 'Stop'
$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if ([string]::IsNullOrWhiteSpace($PayloadDirectory)) {
    $PayloadDirectory = Join-Path $root 'artifacts/publish/win-x64-aot'
}
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $root 'artifacts/publish'
}

$PayloadDirectory = [System.IO.Path]::GetFullPath($PayloadDirectory)
$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
$executable = Join-Path $PayloadDirectory 'AvaPDF.exe'
$script = Join-Path $root 'installer/Windows/AvaPDF.iss'
if (-not (Test-Path $executable -PathType Leaf)) {
    throw "找不到安装负载：$executable"
}

if ([string]::IsNullOrWhiteSpace($InnoCompiler)) {
    $candidates = @(
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6/ISCC.exe'),
        (Join-Path $env:ProgramFiles 'Inno Setup 6/ISCC.exe'),
        (Join-Path $env:LOCALAPPDATA 'Programs/Inno Setup 6/ISCC.exe'))
    $InnoCompiler = $candidates | Where-Object { Test-Path $_ -PathType Leaf } | Select-Object -First 1
}
if ([string]::IsNullOrWhiteSpace($InnoCompiler) -or -not (Test-Path $InnoCompiler -PathType Leaf)) {
    throw '未找到 Inno Setup ISCC.exe。请安装 Inno Setup 6。'
}

$productVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($executable).ProductVersion
$version = $productVersion.Split('+')[0]
if ([string]::IsNullOrWhiteSpace($version)) {
    throw '无法从发布程序读取版本。'
}

& (Join-Path $root 'eng/build-shell.ps1') -OutputDirectory (Join-Path $PayloadDirectory 'shell')
if ($LASTEXITCODE -ne 0) {
    throw "Shell Extension 构建失败，退出码：$LASTEXITCODE"
}

$outputBaseName = "AvaPDF-$version-win-x64-setup"
$arguments = @(
    '/Qp',
    "/DAppVersion=$version",
    "/DPayloadDir=$PayloadDirectory",
    "/DOutputDir=$OutputDirectory",
    "/DOutputBaseFilename=$outputBaseName",
    $script)
& $InnoCompiler @arguments
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup 编译失败，退出码：$LASTEXITCODE"
}

$setup = Join-Path $OutputDirectory "$outputBaseName.exe"
if (-not (Test-Path $setup -PathType Leaf)) {
    throw "安装包未生成：$setup"
}

Write-Host "安装包已生成：$setup"
