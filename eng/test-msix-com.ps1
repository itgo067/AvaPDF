[CmdletBinding()]
param(
    [switch]$Install
)

$ErrorActionPreference = 'Stop'
$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$packageRoot = Join-Path $root 'artifacts/msix-prototype'
$manifest = Join-Path $root 'packaging/msix-prototype/AppxManifest.xml'
$payload = Join-Path $root 'artifacts/publish/win-x64-aot'
$package = Join-Path $root 'artifacts/publish/AvaPDF-0.1.0-msix-com.msix'
$testShell = Join-Path $root 'artifacts/shell-tests/TestShell.exe'
$handlerKey = 'HKCU:\Software\Classes\SystemFileAssociations\.pdf\ShellEx\ContextMenuHandlers\AvaPDF'
$clsidKey = 'HKCU:\Software\Classes\CLSID\{6F4A17D2-6A4B-4F1E-A6B3-0A9A0DFD0001}'

Remove-Item $packageRoot -Recurse -Force -ErrorAction SilentlyContinue
[System.IO.Directory]::CreateDirectory($packageRoot) | Out-Null
Copy-Item "$payload/*" $packageRoot -Recurse -Force
Copy-Item $manifest $packageRoot
Copy-Item (Join-Path $root 'src/AvaPDF.Desktop/Assets/AppIcon.png') $packageRoot
& (Join-Path $PSScriptRoot 'create-msix-registry.ps1') -OutputPath (Join-Path $packageRoot 'Registry.dat')

& (Join-Path ${env:ProgramFiles(x86)} 'Windows Kits/10/bin/10.0.26100.0/x64/makeappx.exe') pack /d $packageRoot /p $package /o
if ($LASTEXITCODE -ne 0) {
    throw "makeappx failed: $LASTEXITCODE"
}

$pfx = Join-Path $root 'artifacts/msix-prototype-cert.pfx'
if (-not (Test-Path $pfx)) {
    $certificate = New-SelfSignedCertificate -Subject 'CN=AvaPDF Test' -Type CodeSigningCert -KeyUsage DigitalSignature -CertStoreLocation 'Cert:\CurrentUser\My'
    $password = ConvertTo-SecureString 'msix-test-only' -AsPlainText -Force
    Export-PfxCertificate -Cert $certificate -FilePath $pfx -Password $password | Out-Null
}
& (Join-Path ${env:ProgramFiles(x86)} 'Windows Kits/10/bin/10.0.26100.0/x64/signtool.exe') sign /fd SHA256 /f $pfx /p msix-test-only $package
if ($LASTEXITCODE -ne 0) {
    throw "signtool failed: $LASTEXITCODE"
}
$certificatePath = Join-Path $root 'artifacts/msix-prototype-cert.cer'
$certificatePassword = ConvertTo-SecureString 'msix-test-only' -AsPlainText -Force
$signingCertificate = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($pfx, $certificatePassword)
Export-Certificate -Cert $signingCertificate -FilePath $certificatePath -Force | Out-Null
Write-Host "MSIX prototype created: $package"
Write-Host "Test certificate: $certificatePath"

if (-not $Install) {
    Write-Host 'Build-only validation complete. Use -Install on a machine with sideloading enabled and the test certificate trusted.'
    return
}

Remove-Item $handlerKey -Recurse -ErrorAction SilentlyContinue
Remove-Item $clsidKey -Recurse -ErrorAction SilentlyContinue
Add-AppxPackage -Path $package -ForceUpdateFromAnyVersion
$identity = Get-AppxPackage -Name 'AvaPDF.MsixPrototype'
if ($null -eq $identity) {
    throw 'MSIX package did not install.'
}

Write-Host 'MSIX installed. Calling CoCreateInstance...'
& $testShell
$exitCode = $LASTEXITCODE
Write-Host "COM test exit code: $exitCode"
if ($exitCode -ne 0) {
    Write-Warning 'COM context-menu handler is not registered by the MSIX package.'
}

Remove-AppxPackage -Package $identity.PackageFullName
Write-Host 'MSIX package removed.'
