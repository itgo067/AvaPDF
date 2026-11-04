[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
$source = @'
using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

public static class MsixRegistryHive
{
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
    private static extern int RegLoadAppKey(string file, out IntPtr key, int access, int options, int reserved);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
    private static extern int RegCreateKeyEx(IntPtr key, string subKey, int reserved, IntPtr type, int options, int access, IntPtr security, out IntPtr result, out int disposition);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
    private static extern int RegSetValueEx(IntPtr key, string name, int reserved, int type, byte[] data, int length);

    [DllImport("advapi32.dll")]
    private static extern int RegCloseKey(IntPtr key);

    private static void Check(int result)
    {
        if (result != 0)
        {
            throw new System.ComponentModel.Win32Exception(result);
        }
    }

    private static void SetString(IntPtr key, string name, string value)
    {
        var bytes = System.Text.Encoding.Unicode.GetBytes(value + "\0");
        Check(RegSetValueEx(key, name, 0, 1, bytes, bytes.Length));
    }

    public static void Create(string path)
    {
        const int access = 0x000F003F;
        IntPtr root;
        Check(RegLoadAppKey(path, out root, access, 0, 0));
        try
        {
            CreateKey(root, @"SOFTWARE\Classes\CLSID\{6F4A17D2-6A4B-4F1E-A6B3-0A9A0DFD0001}\InprocServer32", "shell\\AvaPDF.Shell.dll");
            CreateKey(root, @"SOFTWARE\Classes\SystemFileAssociations\.pdf\ShellEx\ContextMenuHandlers\AvaPDF", "{6F4A17D2-6A4B-4F1E-A6B3-0A9A0DFD0001}");
        }
        finally
        {
            RegCloseKey(root);
        }
    }

    private static void CreateKey(IntPtr root, string path, string value)
    {
        IntPtr key;
        int disposition;
        Check(RegCreateKeyEx(root, path, 0, IntPtr.Zero, 0, 0x000F003F, IntPtr.Zero, out key, out disposition));
        try
        {
            SetString(key, null, value);
            if (path.EndsWith("InprocServer32", StringComparison.OrdinalIgnoreCase))
            {
                SetString(key, "ThreadingModel", "Apartment");
            }
        }
        finally
        {
            RegCloseKey(key);
        }
    }
}
'@
Add-Type -TypeDefinition $source -Language CSharp
Remove-Item $OutputPath -Force -ErrorAction SilentlyContinue
[MsixRegistryHive]::Create([System.IO.Path]::GetFullPath($OutputPath))
Write-Host "Created $OutputPath"
