# MSIX COM / Shell 原型验证

## 当前结论

当前只确认 **MSIX 清单、私有注册表和签名打包有效**，尚未确认安装后 Explorer 右键菜单可用，不能据此承诺商店版保留该功能。

原型使用：

- `com4:InProcessServer` 注册现有原生 `AvaPDF.Shell.dll`；
- `desktop7:ApprovedShellExtension` 声明 Shell 扩展；
- `Registry.dat` 写入传统 `InprocServer32` 和 `.pdf` 的 `ShellEx\ContextMenuHandlers\AvaPDF` 关联；
- 继续使用 CLSID `{6F4A17D2-6A4B-4F1E-A6B3-0A9A0DFD0001}`。

需要特别验证：MSIX 的 COM 注册能否被非打包的 Explorer 使用，以及私有注册表中的 ShellEx 关联是否会被 Explorer 枚举。`makeappx` 成功并不证明这两点。

## 已完成

- `makeappx pack` 成功；
- 自签名测试证书签名成功；
- 原型包：`artifacts/publish/AvaPDF-0.1.0-msix-com.msix`；
- 测试证书：`artifacts/msix-prototype-cert.cer`。

构建命令：

```powershell
& eng/build-shell.ps1 -Publish
& eng/test-msix-com.ps1
```

## 本机阻塞

当前非交互环境遇到：

- `0x800B0109`：自签名证书不在受信任根存储；
- `0x80073CFF`：系统未允许旁加载；
- 向受信任根存储导入证书时，系统返回“此操作中不允许使用 UI”。

因此 `CoCreateInstance`、Explorer 菜单和 `InvokeCommand` 的安装后测试还没有执行成功。

## 下一步实测

在允许旁加载的干净 Windows 测试机上，用管理员 PowerShell 导入测试证书，然后执行：

```powershell
Import-Certificate -FilePath artifacts/msix-prototype-cert.cer -CertStoreLocation Cert:\LocalMachine\Root
& eng/test-msix-com.ps1 -Install
```

`-Install` 会清除 AvaPDF 的传统 HKCU COM 测试注册，安装 MSIX，运行 `TestShell.exe` 的 `CoCreateInstance`/`QueryContextMenu` 测试，然后移除 MSIX。

仍需人工在 Explorer 中验证：

1. 选中一个 PDF 后出现 `AvaPDF` 及四个二级菜单；
2. 每个命令启动 MSIX 应用并载入所选文件；
3. 多选 PDF、中文/空格/UNC 路径；
4. 升级和卸载后菜单及 COM 注册无残留；
5. WACK 和 Partner Center 认证是否接受这些声明。

测试证书仅用于本机原型，不得用于正式发布。
