[CmdletBinding()]
param(
    [switch]$Install
)

$ErrorActionPreference = 'Stop'
$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$packageRoot = Join-Path $root 'artifacts/msix-prototype'
$manifest = Join-Path $root 'packaging/msix-prototype/AppxManifest.xml'
$payload = Join-Path $root 'artifacts/publish/win-x64-aot'
$package = Join-Path $root 'artifacts/publish/AvaPDF-0.1.0.msix'

Remove-Item $packageRoot -Recurse -Force -ErrorAction SilentlyContinue
[System.IO.Directory]::CreateDirectory($packageRoot) | Out-Null
Copy-Item "$payload/*" $packageRoot -Recurse -Force
Remove-Item (Join-Path $packageRoot 'shell') -Recurse -Force -ErrorAction SilentlyContinue
Copy-Item $manifest $packageRoot
Copy-Item (Join-Path $root 'src/AvaPDF.Desktop/Assets/AppIcon.png') $packageRoot

if (Test-Path (Join-Path $packageRoot 'Registry.dat')) {
    throw 'The store package must not contain an MSIX private registry hive.'
}
$manifestText = [System.IO.File]::ReadAllText($manifest)
if ($manifestText -match 'windows\.comServer|windows\.approvedShellExtension') {
    throw 'The store package must not contain COM or classic Shell extension declarations.'
}

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
Write-Host "MSIX package created without the classic Shell extension: $package"
Write-Host "Test certificate: $certificatePath"

if (-not $Install) {
    Write-Host 'Build-only validation complete. Use -Install with sideloading enabled and the test certificate trusted.'
    return
}

Get-AppxPackage -Name 'AvaPDF.MsixPrototype' | Remove-AppxPackage
Add-AppxPackage -Path $package -ForceUpdateFromAnyVersion
$identity = Get-AppxPackage -Name 'AvaPDF.MsixPrototype'
if ($null -eq $identity) {
    throw 'MSIX package did not install.'
}
Write-Host "MSIX install smoke passed: $($identity.PackageFullName)"

Remove-AppxPackage -Package $identity.PackageFullName
Write-Host 'MSIX package removed.'
