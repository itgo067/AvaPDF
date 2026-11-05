# Windows 安装包与右键菜单

## 安装包

Windows 使用 Inno Setup 6，安装包由已验证的 Native AOT 目录生成，不需要安装 .NET Runtime。

本机构建：

```powershell
powershell -File eng/publish-desktop.ps1 -RequireAot
powershell -File eng/build-installer.ps1
```

产物：

```text
artifacts/publish/AvaPDF-<version>-win-x64-setup.exe
```

安装器默认当前用户安装到 `%LOCALAPPDATA%\Programs\AvaPDF`，不需要管理员权限。提供开始菜单快捷方式和可选桌面快捷方式，支持静默安装与卸载。

## 右键菜单

此功能仅属于 Inno Setup 下载版；MSIX／商店版不打包或注册该扩展，详见 [MSIX_PACKAGING.md](MSIX_PACKAGING.md)。

安装后为 PDF、支持的文档和图片注册一个动态 **AvaPDF 功能子菜单**：

- 单个 PDF：提取文本；
- 多个 PDF：合并 PDF，并进入界面确认顺序；
- 文档：转换或批量转换为 PDF；
- 单张图片：转换为 PDF；
- 多张图片：合并为 PDF，并进入界面确认顺序。

提取文本和转换会生成新文件，并在应用中显示进度、结果和警告；不会覆盖源文件。合并不自动执行。完整菜单矩阵和任务协议见 [SHELL_MENU.md](SHELL_MENU.md)。

主要注册位置：

```text
HKCU\Software\Classes\SystemFileAssociations\<支持的扩展名>\ShellEx\ContextMenuHandlers\AvaPDF
HKCU\Software\Classes\CLSID\{6F4A17D2-6A4B-4F1E-A6B3-0A9A0DFD0001}\InprocServer32
```

右键菜单由原生 C++ COM `IContextMenu + IShellExtInit` 处理器创建，模型与 7-Zip 一致，不依赖 `SubCommands`，也不需要安装 .NET Runtime。安装新版本时会先检测并提示卸载旧版本；静默升级则自动执行旧卸载器，并等待旧卸载注册项被删除后才继续安装，避免仅启动卸载器就直接覆盖。卸载时删除上述注册项。

## 启动参数

```text
--file "<路径>"       载入文件，可重复使用
--file=<路径>
--tool optimize      打开指定工具
--tool=<工具标识>
```

支持 `--tool merge`、`watermark`、`encrypt`、`decrypt`、`word-to-pdf` 等稳定标识。普通启动参数不会自动执行任务。

Shell 快速任务使用 `--job "<任务文件>"`，任务文件中的 `autoRun` 决定是否直接执行；合并始终要求用户在界面确认。

## 安装验证

```powershell
powershell -File eng/test-installer.ps1 -SetupPath <setup.exe路径>
```

验证脚本执行静默安装，检查程序文件与右键注册，运行动态菜单场景，启动安装后的程序，验证右键提取文本及中文路径输出，然后静默卸载并检查清理结果。CI Release 工作流执行相同验证。

## Release 流程

`.github/workflows/release-installer.yml` 在 GitHub Release 发布后：

1. 检出对应标签及完整 Git 历史。
2. 运行单元测试和 Native AOT 发布验证。
3. 构建 Setup 安装包。
4. 验证安装、右键菜单、启动和卸载。
5. 上传 Setup、AOT ZIP 和指标文件到工作流产物及原 GitHub Release。

当前安装包**未进行代码签名**，Windows SmartScreen 可能提示未知发布者。正式发布建议配置受信任代码签名证书，并对 Setup 与 EXE 签名。
