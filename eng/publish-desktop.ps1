[CmdletBinding()]
param(
    [ValidateSet('win-x64')]
    [string]$Runtime = 'win-x64',
    [switch]$RequireAot
)

$ErrorActionPreference = 'Stop'
$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$desktopProject = Join-Path $root 'src/AvaPDF.Desktop/AvaPDF.Desktop.csproj'
$smokeProject = Join-Path $root 'tests/AvaPDF.AotSmoke/AvaPDF.AotSmoke.csproj'
$publishRoot = Join-Path $root 'artifacts/publish'
$metricsRoot = Join-Path $root 'artifacts/metrics'
$measureScript = Join-Path $root 'eng/measure-desktop.ps1'
$aotOutput = Join-Path $publishRoot "$Runtime-aot"
$coreClrOutput = Join-Path $publishRoot "$Runtime-coreclr"
$smokeOutput = Join-Path $publishRoot 'aot-smoke'

function Publish-CoreClr {
    Write-Host '正在发布自包含 CoreCLR 版本…' -ForegroundColor Cyan
    $arguments = @(
        'publish', $desktopProject,
        '--nologo',
        '-c', 'Release',
        '-r', $Runtime,
        '--self-contained', 'true',
        '-p:PublishAot=false',
        '-o', $coreClrOutput)
    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "CoreCLR 发布失败，退出码：$LASTEXITCODE"
    }

    Write-Host "CoreCLR 发布完成：$coreClrOutput" -ForegroundColor Green
    Invoke-ReleaseMetrics 'CoreCLR' $coreClrOutput
}

function Invoke-ReleaseMetrics([string]$engine, [string]$outputDirectory) {
    $suffix = 'coreclr'
    if ($engine -eq 'NativeAOT') {
        $suffix = 'aot'
    }
    $jsonPath = Join-Path $metricsRoot "$Runtime-$suffix.json"
    $executable = Join-Path $outputDirectory 'AvaPDF.Desktop.exe'
    $productVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($executable).ProductVersion
    $version = $productVersion.Split('+')[0]
    if ([string]::IsNullOrWhiteSpace($version)) {
        throw '无法从发布程序读取 MinVer 版本。'
    }
    $zipPath = Join-Path $publishRoot "AvaPDF-$version-$Runtime-$suffix.zip"
    & $measureScript `
        -ExecutablePath $executable `
        -OutputJsonPath $jsonPath `
        -ZipPath $zipPath `
        -Engine $engine `
        -Runs 3
    if ($LASTEXITCODE -ne 0) {
        throw "发布指标测量失败，退出码：$LASTEXITCODE"
    }
}

function Find-VcVars {
    if ($env:OS -ne 'Windows_NT') {
        return $null
    }

    $vsWhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
    if (-not (Test-Path $vsWhere -PathType Leaf)) {
        return $null
    }

    $installation = & $vsWhere -latest -prerelease -products * `
        -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 `
        -property installationPath | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($installation)) {
        return $null
    }

    $vcVars = Join-Path $installation 'VC/Auxiliary/Build/vcvars64.bat'
    if (Test-Path $vcVars -PathType Leaf) {
        return $vcVars
    }

    return $null
}

function Invoke-AotPublish([string]$project, [string]$output, [string]$vcVars) {
    $vsInstaller = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer'
    $arguments = @(
        'publish', ('"{0}"' -f $project),
        '--nologo',
        '-c', 'Release',
        '-r', $Runtime,
        '-p:PublishAot=true',
        '-p:IlcUseEnvironmentalTools=true',
        '-p:TrimmerSingleWarn=false',
        '-p:WarningsNotAsErrors=IL2026%3BIL3050%3BIL2075',
        '-p:StripSymbols=true',
        '-o', ('"{0}"' -f $output)) -join ' '
    $command = 'set "PATH={0};%PATH%" && call "{1}" >nul && dotnet {2}' -f $vsInstaller, $vcVars, $arguments
    & cmd.exe /d /c $command
    if ($LASTEXITCODE -ne 0) {
        throw "Native AOT 发布失败，退出码：$LASTEXITCODE"
    }
}

if (-not (Test-Path $desktopProject -PathType Leaf)) {
    throw "找不到桌面项目：$desktopProject"
}

$vcVars = Find-VcVars
if ($null -eq $vcVars) {
    if ($RequireAot) {
        throw '未找到 MSVC x64 工具链，无法执行 Native AOT 发布。请安装 Visual Studio 的“使用 C++ 的桌面开发”工作负载。'
    }

    Write-Warning '未找到 MSVC x64 工具链，Native AOT 不可用，将改用 CoreCLR。'
    Publish-CoreClr
    exit 0
}

try {
    Write-Host '检测到 MSVC 工具链，优先验证并发布 Native AOT…' -ForegroundColor Cyan
    Write-Host '正在执行全部 PDF 操作的 AOT 冒烟测试…' -ForegroundColor Cyan
    Invoke-AotPublish $smokeProject $smokeOutput $vcVars
    $smokeExe = Join-Path $smokeOutput 'AvaPDF.AotSmoke.exe'
    & $smokeExe
    if ($LASTEXITCODE -ne 0) {
        throw "AOT 冒烟测试失败，退出码：$LASTEXITCODE"
    }

    Write-Host 'AOT 冒烟测试通过，正在发布桌面程序…' -ForegroundColor Cyan
    Invoke-AotPublish $desktopProject $aotOutput $vcVars
    Write-Host "Native AOT 发布完成：$aotOutput" -ForegroundColor Green
    Invoke-ReleaseMetrics 'NativeAOT' $aotOutput
}
catch {
    if ($RequireAot) {
        throw
    }

    Write-Warning "Native AOT 发布或验证失败：$($_.Exception.Message)"
    Write-Warning '将改用自包含 CoreCLR 发布。'
    Publish-CoreClr
}
