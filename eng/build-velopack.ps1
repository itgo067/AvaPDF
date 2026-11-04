[CmdletBinding()]
param(
    [string]$PayloadDirectory = '',
    [string]$OutputDirectory = '',
    [switch]$SkipPublish
)

$ErrorActionPreference = 'Stop'
$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$project = Join-Path $root 'src/AvaPDF.Desktop/AvaPDF.Desktop.csproj'
if ([string]::IsNullOrWhiteSpace($PayloadDirectory)) {
    $PayloadDirectory = Join-Path $root 'artifacts/publish/win-x64-velopack'
}
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $root 'artifacts/velopack'
}
$PayloadDirectory = [System.IO.Path]::GetFullPath($PayloadDirectory)
$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)

if (-not $SkipPublish) {
    & dotnet publish $project --nologo -c Release -r win-x64 --self-contained true `
        -p:PublishAot=false -p:DebugType=None -p:DebugSymbols=false -o $PayloadDirectory
    if ($LASTEXITCODE -ne 0) {
        throw "CoreCLR prototype publish failed: $LASTEXITCODE"
    }
}

$executable = Join-Path $PayloadDirectory 'AvaPDF.exe'
if (-not (Test-Path $executable -PathType Leaf)) {
    throw "Velopack payload executable not found: $executable"
}
$productVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($executable).ProductVersion
$version = $productVersion.Split('+')[0]
if ([string]::IsNullOrWhiteSpace($version)) {
    throw 'Unable to resolve the AvaPDF package version.'
}

Remove-Item $OutputDirectory -Recurse -Force -ErrorAction SilentlyContinue
[System.IO.Directory]::CreateDirectory($OutputDirectory) | Out-Null
$icon = Join-Path $root 'src/AvaPDF.Desktop/Assets/AppIcon.ico'
& dnx 'vpk@1.2.161' --yes --skip-updates -- pack `
    --packId 'AvaPDF.Velopack' `
    --packVersion $version `
    --packDir $PayloadDirectory `
    --outputDir $OutputDirectory `
    --runtime 'win-x64' `
    --channel 'win' `
    --mainExe 'AvaPDF.exe' `
    --packTitle 'AvaPDF Velopack' `
    --packAuthors 'AvaPDF' `
    --icon $icon `
    --shortcuts 'Desktop,StartMenuRoot'
if ($LASTEXITCODE -ne 0) {
    throw "vpk pack failed: $LASTEXITCODE"
}

$setup = Get-ChildItem $OutputDirectory -Filter '*-Setup.exe' |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1
if ($null -eq $setup) {
    throw "Velopack Setup was not created in $OutputDirectory"
}
Write-Host "Velopack prototype created: $($setup.FullName)"
