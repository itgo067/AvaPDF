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
$contextKey = 'HKCU:\Software\Classes\SystemFileAssociations\.pdf\Shell\AvaPDF\Command'
$optimizeKey = 'HKCU:\Software\Classes\SystemFileAssociations\.pdf\Shell\AvaPDFOptimize\Command'

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

    $command = (Get-ItemProperty $contextKey).'(default)'
    $optimizeCommand = (Get-ItemProperty $optimizeKey).'(default)'
    if ($command -notmatch '--file' -or $optimizeCommand -notmatch '--tool optimize') {
        throw '右键菜单命令注册内容不正确。'
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
