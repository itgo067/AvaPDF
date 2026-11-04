# MSIX COM / Shell 原型验证

## 结论

**当前原生 `IContextMenu` 右键扩展经 MSIX 打包后，不能作为可用的 Explorer PDF 菜单交付。**

最佳结果是：通过 `SurrogateServer` 可以从外部进程创建 COM 对象并生成菜单；但 Shell 没有加载 AvaPDF 关联。直接进程内激活则被拒绝。因此“COM 创建成功”不能等同于“Explorer 右键可用”。

## 实测结果

| 配置 | 结果 |
| --- | --- |
| `com4:InProcessServer`，默认虚拟化 | `CoCreateInstance=0x80040154`，类未注册 |
| `com4:InProcessServer`，`Virtualization="disabled"` | `CoCreateInstance=0x80070005`，拒绝访问 |
| `com4:SurrogateServer`，禁用虚拟化并声明激活权限 | `CoCreateInstance=0x00000000`，`QueryContextMenu=0x00000000` |
| `Registry.dat` 中的 PDF ShellEx 关联 | `AvaPDF shell association=NOT FOUND` |
| 临时 HKCU ShellEx 关联 + MSIX SurrogateServer | `AvaPDF shell association=NOT FOUND` |
| 临时 HKCU ShellEx 关联 + MSIX InProcessServer | `AvaPDF shell association=NOT FOUND` |

Shell 关联测试通过 `IShellFolder::GetUIObjectOf` 获取真实 PDF 的合并上下文菜单，并枚举菜单项；不是仅检查注册表或调用打包应用。

## 当前原型

`packaging/msix-prototype/AppxManifest.xml` 保留最佳 COM 结果：

- `com4:SurrogateServer`；
- `Virtualization="disabled"`；
- 测试用 `LaunchAndActivationPermission`；
- `desktop7:ApprovedShellExtension`；
- CLSID `{6F4A17D2-6A4B-4F1E-A6B3-0A9A0DFD0001}`。

`Registry.dat` 只保留 PDF ShellEx 关联，用于证明该关联不会被当前 Shell 枚举。测试用激活权限不得直接作为正式商店配置。

## 构建与复测

前置条件：

- 开启开发者模式或允许旁加载；
- 将 `artifacts/msix-prototype-cert.cer` 导入受信任根存储；
- 已构建 `artifacts/publish/win-x64-aot` 和其中的 `shell/AvaPDF.Shell.dll`。

```powershell
& eng/build-shell-tests.ps1
& eng/test-msix-com.ps1 -Install
```

脚本安装 MSIX，依次执行直接 COM 测试和真实 PDF Shell 关联测试，然后移除 MSIX。当前预期是：直接 COM 成功，Shell 关联失败。

## 商店版决策

不要为商店版承诺保留当前传统 `IContextMenu` 二级菜单。

后续选项：

1. 商店版移除传统右键扩展，Inno Setup 下载版继续保留；
2. 单独实现 `IExplorerCommand` + `desktop4:FileExplorerContextMenus`，重新验证 Windows 版本、级联菜单、文件路径和商店认证；
3. 若必须保留完整传统菜单，则继续通过 Inno Setup 分发，不把 MSIX 作为等价替代。

仍需在采用新方案后验证 `InvokeCommand`、多选、中文/空格/UNC 路径、升级卸载、WACK 和 Partner Center 认证。
