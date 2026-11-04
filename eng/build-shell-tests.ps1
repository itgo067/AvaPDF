[CmdletBinding()]
param(
    [string]$OutputDirectory = ''
)

$ErrorActionPreference = 'Stop'
$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $root 'artifacts/shell-tests'
}
$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
$vsWhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
$installation = & $vsWhere -latest -prerelease -products * `
    -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 `
    -property installationPath | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($installation)) {
    throw 'MSVC x64 toolchain was not found.'
}
$vcVars = Join-Path $installation 'VC/Auxiliary/Build/vcvars64.bat'
[System.IO.Directory]::CreateDirectory($OutputDirectory) | Out-Null

foreach ($test in @('TestShell', 'TestShellAssociation')) {
    $source = Join-Path $root "src/AvaPDF.ShellNative/$test.cpp"
    $output = Join-Path $OutputDirectory "$test.exe"
    $command = 'call "{0}" >nul && cl.exe /MT /EHsc /std:c++17 /W4 /O2 /utf-8 "{1}" /link /OUT:"{2}" user32.lib shell32.lib ole32.lib' -f $vcVars, $source, $output
    & cmd.exe /d /c $command
    if ($LASTEXITCODE -ne 0) {
        throw "$test compilation failed: $LASTEXITCODE"
    }
}
Write-Host "Shell tests built: $OutputDirectory"
