# Velopack Windows 原型

## 范围

分支：`feature/velopack-prototype`。

该原型用于验证 Velopack 的 Native AOT 安装、真实版本升级、PDF 右键菜单和卸载，不替换现有 Inno Setup、MSIX 或 Linux 发布流程。

- 使用 `vpk` / Velopack SDK `1.2.161`；
- 默认发布 Native AOT，`-CoreClr` 可显式使用 CoreCLR 对照；
- 主程序调用 `VelopackApp.Build().Run()` 并注册安装、更新、卸载钩子；
- 不在应用内自动检查更新；真实升级通过本地更新目录和 Velopack CLI 执行；
- 原型包标识为 `AvaPDF.Velopack`，快捷方式及稳定启动器名称为 `AvaPDF Velopack`，避免与正式 Inno 安装冲突。

## 构建 Native AOT 包

```powershell
& eng/build-velopack.ps1
```

脚本调用现有 `eng/publish-desktop.ps1 -RequireAot`，执行全部 PDF 操作的 AOT 冒烟和 GUI 启动指标测试，构建原生 Shell DLL，再通过 `dnx vpk@1.2.161` 生成安装包、更新包和便携包。

主要产物：

```text
artifacts/velopack/AvaPDF.Velopack-win-Setup.exe
artifacts/velopack/AvaPDF.Velopack-win-Portable.zip
artifacts/velopack/releases.win.json
```

使用已有 AOT 输出、覆盖包版本或保留旧版本以生成增量包：

```powershell
& eng/build-velopack.ps1 -SkipPublish -PackageVersion 0.2.100 -OutputDirectory artifacts/velopack-updates
& eng/build-velopack.ps1 -SkipPublish -SkipShellBuild -PackageVersion 0.2.101 -OutputDirectory artifacts/velopack-updates -PreserveReleases
```

## 安装与卸载冒烟

```powershell
& eng/test-velopack.ps1 -SetupPath artifacts/velopack/AvaPDF.Velopack-win-Setup.exe
```

覆盖静默安装、目录结构、当前用户卸载注册、真实 Avalonia 窗口启动、静默卸载和清理。

根目录 `AvaPDF Velopack.exe` 是 Velopack 的稳定启动器，它启动 `current\AvaPDF.exe` 后退出，测试以实际子进程的主窗口作为启动成功依据。

## 右键扩展设计

Velopack 版本也保留传统 `IContextMenu` 二级菜单，但不把 COM 注册直接指向会被替换的 `current`：

- 包内 DLL：`current\shell\AvaPDF.Shell.dll`；
- 稳定 DLL：`<install>\shell\AvaPDF.Shell.dll`；
- `AppExecutable` 注册值指向 `<install>\AvaPDF Velopack.exe`；
- 安装和更新钩子复制 DLL 并写入 HKCU COM/ShellEx 注册；
- 卸载钩子删除 COM 和 ShellEx 注册；
- 若 DLL 内容变化，旧文件先改名为 `.previous`，避免被加载的旧 DLL 阻止更新；
- 后续安装/更新会尝试清理已释放的 `.previous` 文件。

原生处理器同时实现 `IContextMenu` 和 `IShellExtInit`，接收 Shell 选中的文件；`InvokeCommand` 支持 Explorer 的数字命令 ID。Inno 版也通过 `AppExecutable` 指向其 `AvaPDF.exe`。

## 真实版本升级与 COM 占用测试

先确保 `artifacts/publish/win-x64-aot` 是当前 Native AOT 输出，然后执行：

```powershell
& eng/test-velopack-upgrade.ps1
```

脚本自动：

1. 生成 `0.2.100` 和 `0.2.101` 两个包及增量更新；
2. 修改 v2 Shell DLL 的文件内容，确保更新必须替换稳定 DLL；
3. 安装 `0.2.100`；
4. 通过 `IShellFolder::GetUIObjectOf` 检查真实 PDF 菜单；
5. 调用 `InvokeCommand`，检查启动程序、工具参数和 PDF 文件路径；
6. 用 `TestShell.exe --hold` 持续占用旧 COM DLL；
7. 从本地更新源下载并应用 `0.2.101`；
8. 再次检查菜单、命令调用和版本注册；
9. 在 COM 仍被占用时卸载并检查注册表清理；
10. 释放 COM 后清理残留文件。

### 当前实测结果

- Native AOT 全功能冒烟通过；
- Native AOT Velopack 安装、GUI 启动和普通卸载通过；
- `0.2.100 -> 0.2.101` 真实更新通过，增量包生成成功；
- COM 被占用时更新成功，旧 DLL 保留为 `.previous`，新 DLL 正常工作；
- 更新前后真实 PDF 菜单均为 `AvaPDF shell association=FOUND`；
- `InvokeCommand=0x00000000`，启动命令包含 `--tool optimize --file "<pdf>"`；
- 卸载后 COM、ShellEx 和卸载注册均删除。

### 已知边界

如果卸载时仍有进程加载旧 Shell DLL，目录中可能暂时留下 `shell\AvaPDF.Shell.dll.previous`。这不影响注册清理，但不能称为文件完全无残留。释放 DLL 后测试会清理；后续安装/更新也会重试清理。

正式版本若要求立即删除该文件，需要另行设计 Explorer 重启或延迟删除策略，不能在无提示情况下强制关闭用户的 Explorer。

## 尚未完成

- 应用内更新界面和 GitHub Releases 正式更新源；
- 正式代码签名；
- 从 Inno Setup 版本迁移到 Velopack；
- 正式包标识、快捷方式名称和 CI 发布；
- 多选 PDF、UNC 路径和 Explorer 进程实际加载时的完整交互回归。

在这些问题确定前，不删除现有 Inno Setup 安装器。
