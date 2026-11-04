[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$output = Join-Path $root 'artifacts/velopack-aot-updates'
$payload = Join-Path $root 'artifacts/publish/win-x64-aot'
$v1Setup = Join-Path $root 'artifacts/velopack-aot-v1-setup.exe'
$install = Join-Path $env:LOCALAPPDATA ('AvaPDF.' + 'Velopack')
$update = Join-Path $install 'Update.exe'
$packages = Join-Path $install 'packages'
$shellDirectory = Join-Path $install 'shell'
$uninstallKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\AvaPDF.' + 'Velopack'
$clsidKey = 'HKCU:\Software\Classes\CLSID\{6F4A17D2-6A4B-4F1E-A6B3-0A9A0DFD0001}'
$handlerKey = 'HKCU:\Software\Classes\SystemFileAssociations\.pdf\ShellEx\ContextMenuHandlers\AvaPDF'
$testShell = Join-Path $root 'artifacts/shell-tests/TestShell.exe'
$associationTest = Join-Path $root 'artifacts/shell-tests/TestShellAssociation.exe'
$samplePdf = Join-Path $root 'artifacts/startup sample.pdf'
$trackedProcesses = @()

function Stop-TrackedProcesses {
    foreach ($process in $script:trackedProcesses) {
        if ($null -ne $process) {
            $process.Refresh()
            if (-not $process.HasExited) {
                Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
                $process.WaitForExit()
            }
        }
    }
}

function Remove-PrototypeInstallation {
    Stop-TrackedProcesses
    if (Test-Path $update -PathType Leaf) {
        $process = Start-Process -FilePath $update -ArgumentList '--uninstall', '--silent' -Wait -PassThru
        if ($process.ExitCode -ne 0) {
            throw "Velopack uninstall failed: $($process.ExitCode)"
        }
    }
    if (Test-Path $install) {
        Remove-Item $install -Recurse -Force
    }
}

function Assert-ShellMenu {
    & $associationTest $samplePdf
    if ($LASTEXITCODE -ne 0) {
        throw "AvaPDF was not found in the real PDF context menu: $LASTEXITCODE"
    }
}

function Invoke-ShellCommandAndVerify {
    $existing = @(Get-Process -Name 'AvaPDF' -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Id)
    & $testShell --invoke $samplePdf
    if ($LASTEXITCODE -ne 0) {
        throw "Shell InvokeCommand failed: $LASTEXITCODE"
    }
    $deadline = [DateTime]::UtcNow.AddSeconds(30)
    while ([DateTime]::UtcNow -lt $deadline) {
        $app = Get-Process -Name 'AvaPDF' -ErrorAction SilentlyContinue |
            Where-Object { $_.Id -notin $existing -and $_.Path -eq (Join-Path $install 'current/AvaPDF.exe') } |
            Select-Object -First 1
        if ($null -ne $app) {
            $app.Refresh()
            if ($app.MainWindowHandle -ne [System.IntPtr]::Zero) {
                $script:trackedProcesses += $app
                return
            }
        }
        Start-Sleep -Milliseconds 100
    }
    throw 'The shell command did not open the Velopack Avalonia application.'
}

Remove-PrototypeInstallation
Remove-Item $output -Recurse -Force -ErrorAction SilentlyContinue
& (Join-Path $PSScriptRoot 'build-shell-tests.ps1')

try {
    & (Join-Path $PSScriptRoot 'build-velopack.ps1') -SkipPublish -PackageVersion '0.2.100' -OutputDirectory $output
    Copy-Item (Join-Path $output ('AvaPDF.' + 'Velopack-win-Setup.exe')) $v1Setup -Force

    # Change only the packaged shell DLL overlay so v2 must replace the stable DLL.
    $shellDll = Join-Path $payload 'shell/AvaPDF.Shell.dll'
    $stream = [System.IO.File]::OpenWrite($shellDll)
    $stream.Seek(0, [System.IO.SeekOrigin]::End) | Out-Null
    $stream.WriteByte(0x56)
    $stream.Dispose()
    & (Join-Path $PSScriptRoot 'build-velopack.ps1') -SkipPublish -SkipShellBuild `
        -PackageVersion '0.2.101' -OutputDirectory $output -PreserveReleases

    $setupProcess = Start-Process -FilePath $v1Setup -ArgumentList '--silent' -Wait -PassThru
    if ($setupProcess.ExitCode -ne 0) {
        throw "v1 Setup failed: $($setupProcess.ExitCode)"
    }
    if ((Get-ItemProperty $uninstallKey).DisplayVersion -ne '0.2.100') {
        throw 'Velopack v1 version registration is incorrect.'
    }
    Assert-ShellMenu
    Invoke-ShellCommandAndVerify
    Stop-TrackedProcesses
    $script:trackedProcesses = @()
    Write-Host 'v0.2.100 Native AOT shell integration verified.'

    $holder = Start-Process -FilePath $testShell -ArgumentList '--hold' -PassThru
    $trackedProcesses += $holder

    & dnx 'vpk@1.2.161' --yes --skip-updates -- download local `
        --path $output --outputDir $packages --channel 'win'
    $package = Get-ChildItem $packages -Filter '*0.2.101*-full.nupkg' | Select-Object -First 1
    if ($null -eq $package) {
        throw 'The downloaded v0.2.101 package was not found.'
    }
    $updateProcess = Start-Process -FilePath $update `
        -ArgumentList @('apply', '--silent', '--norestart', '--package', $package.FullName) -Wait -PassThru
    if ($updateProcess.ExitCode -ne 0) {
        throw "Velopack update failed while COM was loaded: $($updateProcess.ExitCode)"
    }
    if ((Get-ItemProperty $uninstallKey).DisplayVersion -ne '0.2.101') {
        throw 'Velopack did not apply v0.2.101.'
    }
    Assert-ShellMenu
    Invoke-ShellCommandAndVerify
    Write-Host 'Native AOT v0.2.100 -> v0.2.101 update with COM loaded verified.'

    $uninstallProcess = Start-Process -FilePath $update -ArgumentList '--uninstall', '--silent' -Wait -PassThru
    if ($uninstallProcess.ExitCode -ne 0) {
        throw "Velopack uninstall while COM was loaded failed: $($uninstallProcess.ExitCode)"
    }
    if (Test-Path $uninstallKey) {
        throw 'The uninstall registration remains after uninstall.'
    }
    if (Test-Path $clsidKey) {
        throw 'The COM registration remains after uninstall.'
    }
    if (Test-Path $handlerKey) {
        throw 'The PDF shell handler remains after uninstall.'
    }
    if (Test-Path $install) {
        Write-Warning 'A loaded shell DLL left files in the install directory; they will be removed after releasing COM.'
    }
}
finally {
    Remove-PrototypeInstallation
}

Write-Host 'Native AOT upgrade, real shell menu, InvokeCommand and uninstall registration verified.'
