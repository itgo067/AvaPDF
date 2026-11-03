[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ExecutablePath,
    [Parameter(Mandatory = $true)]
    [string]$OutputJsonPath,
    [Parameter(Mandatory = $true)]
    [string]$ZipPath,
    [ValidateRange(1, 10)]
    [int]$Runs = 3,
    [ValidateRange(5, 120)]
    [int]$TimeoutSeconds = 45,
    [ValidateSet('NativeAOT', 'CoreCLR')]
    [string]$Engine = 'NativeAOT'
)

$ErrorActionPreference = 'Stop'
$executable = [System.IO.Path]::GetFullPath($ExecutablePath)
$executableDirectory = [System.IO.Path]::GetDirectoryName($executable)
if (-not (Test-Path $executable -PathType Leaf)) {
    throw "Desktop executable not found: $executable"
}

$measurements = @()
for ($run = 1; $run -le $Runs; $run++) {
    $process = $null
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $process = Start-Process -FilePath $executable -WorkingDirectory $executableDirectory -PassThru
        $windowReady = $false
        while ($stopwatch.Elapsed.TotalSeconds -lt $TimeoutSeconds) {
            $process.Refresh()
            if ($process.HasExited) {
                throw "Desktop process exited during startup with code $($process.ExitCode)."
            }

            if ($process.MainWindowHandle -ne [System.IntPtr]::Zero) {
                $windowReady = $true
                break
            }

            Start-Sleep -Milliseconds 50
        }

        $stopwatch.Stop()
        if (-not $windowReady) {
            throw "Desktop main window was not created within $TimeoutSeconds seconds."
        }

        $kind = 'warm'
        if ($run -eq 1) {
            $kind = 'first'
        }

        $measurements += [pscustomobject]@{
            Run = $run
            Kind = $kind
            Milliseconds = [Math]::Round($stopwatch.Elapsed.TotalMilliseconds, 1)
        }
    }
    finally {
        $stopwatch.Stop()
        if ($null -ne $process) {
            $process.Refresh()
            if (-not $process.HasExited) {
                Stop-Process -Id $process.Id -Force
                $process.WaitForExit()
            }

            $process.Dispose()
        }
    }
}

$payloadFiles = @(Get-ChildItem $executableDirectory -Recurse -File |
    Where-Object { $_.Extension -ne '.pdb' })
$payloadBytes = ($payloadFiles | Measure-Object -Property Length -Sum).Sum
$executableBytes = (Get-Item $executable).Length
$fileVersionInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($executable)
$productVersion = $fileVersionInfo.ProductVersion
$version = $productVersion.Split('+')[0]
$commitSeparator = $productVersion.IndexOf('+')
$commitHash = ''
if ($commitSeparator -ge 0) {
    $commitHash = $productVersion.Substring($commitSeparator + 1)
}
$milliseconds = @($measurements | ForEach-Object { $_.Milliseconds })
$averageMilliseconds = ($milliseconds | Measure-Object -Average).Average
$minimumMilliseconds = ($milliseconds | Measure-Object -Minimum).Minimum
$maximumMilliseconds = ($milliseconds | Measure-Object -Maximum).Maximum

$zipFullPath = [System.IO.Path]::GetFullPath($ZipPath)
$zipDirectory = [System.IO.Path]::GetDirectoryName($zipFullPath)
[System.IO.Directory]::CreateDirectory($zipDirectory) | Out-Null
$stagingDirectory = Join-Path $zipDirectory ('package-staging-' + [Guid]::NewGuid().ToString('N'))
if (Test-Path $stagingDirectory) {
    Remove-Item $stagingDirectory -Recurse -Force
}

[System.IO.Directory]::CreateDirectory($stagingDirectory) | Out-Null
foreach ($file in $payloadFiles) {
    $relative = $file.FullName.Substring($executableDirectory.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar)
    $destination = Join-Path $stagingDirectory $relative
    [System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($destination)) | Out-Null
    Copy-Item $file.FullName $destination
}

Compress-Archive -Path (Join-Path $stagingDirectory '*') -DestinationPath $zipFullPath -Force
Remove-Item $stagingDirectory -Recurse -Force
$zipBytes = (Get-Item $zipFullPath).Length

$jsonFullPath = [System.IO.Path]::GetFullPath($OutputJsonPath)
$jsonDirectory = [System.IO.Path]::GetDirectoryName($jsonFullPath)
[System.IO.Directory]::CreateDirectory($jsonDirectory) | Out-Null
$metrics = [ordered]@{
    Product = 'AvaPDF'
    Version = $version
    CommitHash = $commitHash
    FileVersion = $fileVersionInfo.FileVersion
    Engine = $Engine
    Runtime = 'win-x64'
    Configuration = 'Release'
    RecordedAtUtc = [DateTime]::UtcNow.ToString('o')
    OperatingSystem = [System.Runtime.InteropServices.RuntimeInformation]::OSDescription
    MeasurementMethod = 'Process start to non-zero MainWindowHandle; includes first run and subsequent warm runs'
    ExecutableBytes = $executableBytes
    ExecutableMiB = [Math]::Round($executableBytes / 1MB, 2)
    PayloadBytesWithoutPdb = $payloadBytes
    PayloadMiBWithoutPdb = [Math]::Round($payloadBytes / 1MB, 2)
    ZipBytes = $zipBytes
    ZipMiB = [Math]::Round($zipBytes / 1MB, 2)
    FirstStartupMilliseconds = $measurements[0].Milliseconds
    AverageStartupMilliseconds = [Math]::Round($averageMilliseconds, 1)
    MinimumStartupMilliseconds = [Math]::Round($minimumMilliseconds, 1)
    MaximumStartupMilliseconds = [Math]::Round($maximumMilliseconds, 1)
    StartupMeasurements = $measurements
    PayloadFiles = @($payloadFiles | ForEach-Object {
        [pscustomobject]@{
            Name = $_.FullName.Substring($executableDirectory.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar)
            Bytes = $_.Length
        }
    })
}

$utf8 = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($jsonFullPath, ($metrics | ConvertTo-Json -Depth 6), $utf8)

$markdown = @"
## AvaPDF $Engine release metrics

Measurement: process start to a non-zero ``MainWindowHandle``. The first run is reported separately; subsequent runs are warm starts.

| Metric | Value |
| --- | ---: |
| Version | $version |
| Commit | $commitHash |
| Executable | $([Math]::Round($executableBytes / 1MB, 2)) MiB |
| Payload without PDB | $([Math]::Round($payloadBytes / 1MB, 2)) MiB |
| ZIP package | $([Math]::Round($zipBytes / 1MB, 2)) MiB |
| First startup | $($measurements[0].Milliseconds) ms |
| Average startup ($Runs runs) | $([Math]::Round($averageMilliseconds, 1)) ms |
| Minimum / maximum startup | $([Math]::Round($minimumMilliseconds, 1)) / $([Math]::Round($maximumMilliseconds, 1)) ms |
"@
$markdownPath = [System.IO.Path]::ChangeExtension($jsonFullPath, '.md')
[System.IO.File]::WriteAllText($markdownPath, $markdown, $utf8)
if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_STEP_SUMMARY)) {
    [System.IO.File]::AppendAllText($env:GITHUB_STEP_SUMMARY, $markdown + [Environment]::NewLine, $utf8)
}

Write-Host $markdown
Write-Host "Metrics JSON: $jsonFullPath"
Write-Host "ZIP package: $zipFullPath"
