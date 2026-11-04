[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$SetupPath
)

$ErrorActionPreference = 'Stop'
$setup = [System.IO.Path]::GetFullPath($SetupPath)
$installDirectory = Join-Path $env:LOCALAPPDATA 'Programs/AvaPDF-Test'
$installedExe = Join-Path $installDirectory 'AvaPDF.Desktop.exe'
$uninstaller = Join-Path $installDirectory 'unins000.exe'
$contextKey = 'HKCU:\Software\Classes\SystemFileAssociations\.pdf\Shell\AvaPDF'
$optimizeKey = 'HKCU:\Software\Classes\SystemFileAssociations\.pdf\Shell\AvaPDF.Optimize\Command'

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

try {
    Invoke-Installer $setup
    if (-not (Test-Path $installedExe -PathType Leaf)) {
        throw '安装后未找到 AvaPDF.Desktop.exe。'
    }

    $subCommands = (Get-ItemProperty $contextKey).SubCommands
    $optimizeCommand = (Get-ItemProperty $optimizeKey).'(default)'
    if ($subCommands -notmatch 'AvaPDF.Merge' -or $subCommands -notmatch 'AvaPDF.ExtractText' -or $optimizeCommand -notmatch '--tool optimize') {
        throw '右键功能子菜单注册内容不正确。'
    }

    foreach ($tool in @('Merge', 'Split', 'Extract', 'Rotate', 'Optimize', 'Watermark', 'Encrypt', 'Decrypt', 'ExtractText')) {
        $key = "HKCU:\Software\Classes\SystemFileAssociations\.pdf\Shell\AvaPDF.$tool\Command"
        if (-not (Test-Path $key)) {
            throw "缺少右键功能命令：$tool"
        }
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
    if (Test-Path $uninstaller -PathType Leaf) {
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
}
