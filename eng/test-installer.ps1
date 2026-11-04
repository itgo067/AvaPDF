[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$SetupPath
)

$ErrorActionPreference = 'Stop'
$setup = [System.IO.Path]::GetFullPath($SetupPath)
$installDirectory = Join-Path $env:LOCALAPPDATA 'Programs/AvaPDF-Test'
$installedExe = Join-Path $installDirectory 'AvaPDF.Desktop.exe'
$uninstaller = Get-ChildItem $installDirectory -Filter 'unins*.exe' -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1 -ExpandProperty FullName
$contextKey = 'HKCU:\Software\Classes\SystemFileAssociations\.pdf\ShellEx\ContextMenuHandlers\AvaPDF'
$optimizeKey = $contextKey

function Invoke-Installer([string]$path) {
    $process = Start-Process -FilePath $path -Wait -PassThru -ArgumentList @(
        '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', '/CURRENTUSER', "/DIR=$installDirectory")
    if ($process.ExitCode -ne 0) {
        throw "安装器退出码：$($process.ExitCode)"
    }
}

if (-not (Test-Path $setup -PathType Leaf)) {
    throw "安装包不存在：$setup"
}

if ($uninstaller -and (Test-Path $uninstaller -PathType Leaf)) {
    $previousCleanup = Start-Process -FilePath $uninstaller -Wait -PassThru -ArgumentList @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART')
    if ($previousCleanup.ExitCode -ne 0) {
        throw "测试前清理旧安装失败，退出码：$($previousCleanup.ExitCode)"
    }
}
if (Test-Path $installDirectory) {
    Remove-Item $installDirectory -Recurse -Force
}
Remove-Item 'HKCU:\Software\Classes\SystemFileAssociations\.pdf\Shell\AvaPDF' -Recurse -ErrorAction SilentlyContinue
Remove-Item 'HKCU:\Software\Classes\SystemFileAssociations\.pdf\Shell\AvaPDFOptimize' -Recurse -ErrorAction SilentlyContinue
Remove-Item 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{D8F93822-2B84-4F4A-9E4B-5BDB49CDB56F}_is1' -Recurse -ErrorAction SilentlyContinue

try {
    Invoke-Installer $setup
    $uninstaller = Get-ChildItem $installDirectory -Filter 'unins*.exe' |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1 -ExpandProperty FullName
    $uninstallKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{D8F93822-2B84-4F4A-9E4B-5BDB49CDB56F}_is1'
    if (-not (Test-Path $uninstallKey)) {
        throw '未找到旧版本卸载注册项，无法验证升级卸载。'
    }
    $previousUninstallString = (Get-ItemProperty $uninstallKey).UninstallString
    if ([string]::IsNullOrWhiteSpace($previousUninstallString)) {
        throw '旧版本卸载命令为空，无法验证升级卸载。'
    }

    Invoke-Installer $setup
    $uninstaller = Get-ChildItem $installDirectory -Filter 'unins*.exe' |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1 -ExpandProperty FullName
    if (-not (Test-Path $installedExe -PathType Leaf)) {
        throw '安装后未找到 AvaPDF.Desktop.exe。'
    }

    $handlerClsid = (Get-ItemProperty $contextKey).'(default)'
    if ($handlerClsid -ne '{6F4A17D2-6A4B-4F1E-A6B3-0A9A0DFD0001}') {
        throw 'COM 右键处理器 CLSID 不正确。'
    }

    $inprocKey = 'HKCU:\Software\Classes\CLSID\{6F4A17D2-6A4B-4F1E-A6B3-0A9A0DFD0001}\InprocServer32'
    $shellDll = (Get-ItemProperty $inprocKey).'(default)'
    Write-Host "Shell DLL path: $shellDll"
    if (-not [System.IO.File]::Exists($shellDll)) {
        throw 'COM Shell Extension DLL 未正确安装。'
    }

    & (Join-Path $PSScriptRoot 'test-shell.ps1')
    if ($LASTEXITCODE -ne 0) {
        throw 'COM Shell Extension 实际创建验证失败。'
    }

    $app = Start-Process -FilePath $installedExe -ArgumentList '--tool', 'optimize' -PassThru
    Start-Sleep -Seconds 8
    $app.Refresh()
    if ($app.HasExited) {
        throw "安装后的程序启动失败，退出码：$($app.ExitCode)"
    }
    Stop-Process -Id $app.Id -Force
    $app.WaitForExit()
    Write-Host '安装、右键注册和安装后启动验证通过。'
}
finally {
    if ($uninstaller -and (Test-Path $uninstaller -PathType Leaf)) {
        $uninstall = Start-Process -FilePath $uninstaller -Wait -PassThru -ArgumentList @(
            '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART')
        if ($uninstall.ExitCode -ne 0) {
            Write-Warning "卸载器退出码：$($uninstall.ExitCode)"
        }
    }

    if (Test-Path $installedExe) {
        Write-Warning '卸载后仍存在程序文件，请检查安装目录。'
    }
    if (Test-Path $contextKey) {
        Write-Warning '卸载后仍存在右键菜单注册。'
    }
    if (Test-Path $optimizeKey) {
        Write-Warning '卸载后仍存在优化功能注册。'
    }
}
