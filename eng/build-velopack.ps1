[CmdletBinding()]
param(
    [string]$PayloadDirectory = '',
    [string]$OutputDirectory = '',
    [string]$PackageVersion = '',
    [switch]$PreserveReleases
)

$ErrorActionPreference = 'Stop'
$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if ([string]::IsNullOrWhiteSpace($PayloadDirectory)) {
    $PayloadDirectory = Join-Path $root 'artifacts/publish/win-x64-aot'
}
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $root 'artifacts/velopack'
}
$PayloadDirectory = [System.IO.Path]::GetFullPath($PayloadDirectory)
$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
$executable = Join-Path $PayloadDirectory 'AvaPDF.Desktop.exe'
if (-not (Test-Path $executable -PathType Leaf)) {
    throw "Velopack payload executable not found: $executable"
}

$productVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($executable).ProductVersion
$version = $productVersion.Split('+')[0]
if (-not [string]::IsNullOrWhiteSpace($PackageVersion)) {
    $version = $PackageVersion
}
if ([string]::IsNullOrWhiteSpace($version)) {
    throw 'Unable to resolve the AvaPDF package version.'
}

if (-not $PreserveReleases) {
    Remove-Item $OutputDirectory -Recurse -Force -ErrorAction SilentlyContinue
}
[System.IO.Directory]::CreateDirectory($OutputDirectory) | Out-Null
$icon = Join-Path $root 'src/AvaPDF.Desktop/Assets/AppIcon.ico'
& dnx 'vpk@1.2.161' --yes --skip-updates -- pack `
    --packId 'AvaPDF' `
    --packVersion $version `
    --packDir $PayloadDirectory `
    --outputDir $OutputDirectory `
    --runtime 'win-x64' `
    --channel 'win' `
    --mainExe 'AvaPDF.Desktop.exe' `
    --packTitle 'AvaPDF' `
    --packAuthors 'AvaPDF' `
    --icon $icon `
    --shortcuts 'Desktop,StartMenuRoot'
if ($LASTEXITCODE -ne 0) {
    throw "vpk pack failed: $LASTEXITCODE"
}
if (-not (Test-Path (Join-Path $OutputDirectory 'releases.win.json'))) {
    throw "Velopack release feed was not created in $OutputDirectory"
}
Write-Host "Velopack packages created: $OutputDirectory"
