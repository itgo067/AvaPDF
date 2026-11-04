# Velopack Windows 原型

## 范围

分支：`feature/velopack-prototype`。

该原型用于验证 Velopack 的安装、启动和卸载，不替换现有 Inno Setup、MSIX 或 Linux 发布流程。

- 使用 `vpk` `1.2.161`；
- 主程序仅增加 `VelopackApp.Build().Run()`，用于 Velopack 生命周期和未来更新能力；
- 不自动检查更新，不配置 GitHub Releases 更新源；
- 不注册 COM / PDF 右键菜单；
- 原型包标识为 `AvaPDF.Velopack`，快捷方式名称为 `AvaPDF Velopack`，避免与正式 Inno 安装冲突；
- 默认使用自包含 CoreCLR 缩短原型构建时间，Native AOT 兼容性仍需单独验证。

## 构建

```powershell
& eng/build-velopack.ps1
```

脚本发布 `AvaPDF.exe` 到 `artifacts/publish/win-x64-velopack`，再通过 `dnx vpk@1.2.161` 生成安装包、更新包和便携包。

主要产物：

```text
artifacts/velopack/AvaPDF.Velopack-win-Setup.exe
artifacts/velopack/AvaPDF.Velopack-win-Portable.zip
artifacts/velopack/releases.win.json
```

若要打包已有的 Native AOT 目录：

```powershell
& eng/build-velopack.ps1 `
  -PayloadDirectory artifacts/publish/win-x64-aot `
  -SkipPublish
```

该目录必须已经包含重命名后的 `AvaPDF.exe`。

## 安装与卸载测试

```powershell
& eng/test-velopack.ps1 `
  -SetupPath artifacts/velopack/AvaPDF.Velopack-win-Setup.exe
```

测试覆盖：

1. 静默安装到 `%LOCALAPPDATA%\AvaPDF.Velopack`；
2. 检查根目录 `AvaPDF Velopack.exe` 启动器、`current\AvaPDF.exe` 和 `Update.exe`；
3. 检查当前用户卸载注册项；
4. 通过根目录启动器打开真实 Avalonia 主窗口；
5. 调用 `Update.exe --uninstall --silent`；
6. 检查安装目录和卸载注册项均被删除。

根目录 `AvaPDF Velopack.exe` 是 Velopack 的稳定启动器，它启动 `current` 中的实际程序后退出，测试会以实际子进程的主窗口作为启动成功依据。

## 尚未验证

- 自动检查、下载和应用更新；
- Native AOT 发布及 Velopack SDK 的 AOT 兼容性；
- 正式代码签名；
- COM Shell DLL 被 Explorer 加载后的更新与卸载；
- 从 Inno Setup 版本迁移到 Velopack；
- 正式包标识、快捷方式名称和 CI 发布。

在这些问题验证前，不删除现有 Inno Setup 安装器。
