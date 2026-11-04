[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$SetupPath
)

$ErrorActionPreference = 'Stop'
$setup = [System.IO.Path]::GetFullPath($SetupPath)
$installDirectory = Join-Path $env:LOCALAPPDATA 'AvaPDF.Velopack'
$rootExecutable = Join-Path $installDirectory 'AvaPDF Velopack.exe'
$currentExecutable = Join-Path $installDirectory 'current/AvaPDF.exe'
$updateExecutable = Join-Path $installDirectory 'Update.exe'
$uninstallKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\AvaPDF.Velopack'

function Invoke-PrototypeUninstall {
    if (Test-Path $updateExecutable -PathType Leaf) {
        $process = Start-Process -FilePath $updateExecutable -ArgumentList '--uninstall', '--silent' -Wait -PassThru
        if ($process.ExitCode -ne 0) {
            throw "Velopack uninstall failed: $($process.ExitCode)"
        }
    }
}

if (-not (Test-Path $setup -PathType Leaf)) {
    throw "Velopack Setup not found: $setup"
}
Invoke-PrototypeUninstall
if (Test-Path $installDirectory) {
    Remove-Item $installDirectory -Recurse -Force
}

$startedProcesses = @()
try {
    $setupProcess = Start-Process -FilePath $setup -ArgumentList '--silent' -Wait -PassThru
    if ($setupProcess.ExitCode -ne 0) {
        throw "Velopack Setup failed: $($setupProcess.ExitCode)"
    }
    foreach ($required in @($rootExecutable, $currentExecutable, $updateExecutable)) {
        if (-not (Test-Path $required -PathType Leaf)) {
            throw "Expected installed file not found: $required"
        }
    }
    if (-not (Test-Path $uninstallKey)) {
        throw 'Velopack uninstall registration was not created.'
    }
    Write-Host 'Velopack install layout verified.'

    $existingIds = @(Get-Process -Name 'AvaPDF' -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Id)
    $launcher = Start-Process -FilePath $rootExecutable -ArgumentList '--tool', 'optimize' -PassThru
    $startedProcesses += $launcher
    $deadline = [DateTime]::UtcNow.AddSeconds(30)
    $app = $null
    while ([DateTime]::UtcNow -lt $deadline) {
        $app = Get-Process -Name 'AvaPDF' -ErrorAction SilentlyContinue |
            Where-Object { $_.Id -notin $existingIds -and $_.Path -eq $currentExecutable } |
            Select-Object -First 1
        if ($null -ne $app) {
            $app.Refresh()
            if ($app.MainWindowHandle -ne [System.IntPtr]::Zero) {
                break
            }
        }
        Start-Sleep -Milliseconds 100
    }
    if ($null -eq $app -or $app.MainWindowHandle -eq [System.IntPtr]::Zero) {
        throw 'The Velopack-installed Avalonia main window was not created.'
    }
    $startedProcesses += $app
    Write-Host 'Velopack installed application startup verified.'
}
finally {
    foreach ($process in $startedProcesses) {
        if ($null -ne $process) {
            $process.Refresh()
            if (-not $process.HasExited) {
                Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
            }
        }
    }
    Invoke-PrototypeUninstall
}

if (Test-Path $installDirectory) {
    throw 'Velopack install directory remains after uninstall.'
}
if (Test-Path $uninstallKey) {
    throw 'Velopack uninstall registration remains after uninstall.'
}
Write-Host 'Velopack install, GUI startup, uninstall and cleanup verified.'
