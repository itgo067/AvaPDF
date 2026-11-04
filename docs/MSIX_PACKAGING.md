# MSIX / 微软商店打包

## 功能边界

**MSIX／商店版不包含传统 PDF 右键菜单；Inno Setup 下载版继续保留现有原生 `IContextMenu` 二级菜单。**

商店包明确排除：

- `shell/AvaPDF.Shell.dll`；
- COM `InProcessServer` / `SurrogateServer` 声明；
- `ApprovedShellExtension`；
- `Registry.dat` 中的 ShellEx 注册；
- 安装器使用的 HKCU COM 注册和卸载逻辑。

这不是应用功能的运行时开关，而是独立的打包边界，避免商店包携带未通过验证的 Shell 集成。

## 原 COM 原型结论

原型已实际验证：

- `SurrogateServer` 可以让外部进程成功执行 `CoCreateInstance` 和 `QueryContextMenu`；
- 通过 `IShellFolder::GetUIObjectOf` 获取真实 PDF 菜单时，结果为 `AvaPDF shell association=NOT FOUND`；
- 直接 `InProcessServer` 激活返回 `0x80070005`；
- `Registry.dat` 和临时 HKCU ShellEx 关联均未使 AvaPDF 出现在 PDF 菜单中。

因此不能将当前传统右键扩展作为 MSIX 功能交付。若未来重新提供商店版右键能力，需要单独设计并验证 `IExplorerCommand`，不能恢复上述 COM 原型。

## 当前清单

`packaging/msix-prototype/AppxManifest.xml` 只声明：

- 全信任桌面应用；
- 应用入口和商店图标；
- `runFullTrust`；
- Windows Desktop 目标版本。

它不包含任何 COM 或 Shell 扩展。

## 构建与安装冒烟

前置条件：已经生成 `artifacts/publish/win-x64-aot`。

```powershell
& eng/test-msix.ps1
```

脚本会：

1. 复制桌面发布文件；
2. 删除 `shell` 目录；
3. 检查不存在 `Registry.dat`、COM 和经典 Shell 扩展声明；
4. 使用 `makeappx` 打包；
5. 使用本地测试证书签名。

产物：`artifacts/publish/AvaPDF-0.1.0.msix`。

本地安装测试需要开启开发者模式或允许旁加载，并信任 `artifacts/msix-prototype-cert.cer`，然后执行：

```powershell
& eng/test-msix.ps1 -Install
```

安装冒烟成功后，脚本会移除测试 MSIX，不会删除或修改 Inno Setup 版的 COM/ShellEx 注册。

## 发布前仍需完成

- 使用 Partner Center 的正式 Package Identity、Publisher 和版本号；
- 按商店要求生成完整尺寸图标和截图；
- 使用正式签名流程，禁止使用原型测试证书发布；
- 验证安装、启动、文件处理、升级和卸载；
- 运行 WACK 和干净 Windows 虚拟机测试；
- 准备隐私政策、年龄分级、支持信息和商店资料。
