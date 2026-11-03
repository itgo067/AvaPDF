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

安装后为 PDF 注册两个当前用户菜单：

- `使用 AvaPDF 处理`：启动程序并载入文件。
- `使用 AvaPDF 优化 PDF`：启动优化工具并载入文件。

它们只打开工具和文件，不会自动执行处理或覆盖源文件。注册位置：

```text
HKCU\Software\Classes\SystemFileAssociations\.pdf\Shell\AvaPDF
HKCU\Software\Classes\SystemFileAssociations\.pdf\Shell\AvaPDFOptimize
```

卸载时删除上述注册项。

## 启动参数

```text
--file "<路径>"       载入文件，可重复使用
--file=<路径>
--tool optimize      打开指定工具
--tool=<工具标识>
```

支持 `--tool merge`、`watermark`、`encrypt`、`decrypt`、`word-to-pdf` 等稳定标识。不会仅凭启动参数自动执行任务。

## 安装验证

```powershell
powershell -File eng/test-installer.ps1 -SetupPath <setup.exe路径>
```

验证脚本执行静默安装，检查程序文件与右键注册，启动安装后的程序，然后静默卸载并检查清理结果。CI Release 工作流执行相同验证。

## Release 流程

`.github/workflows/release-installer.yml` 在 GitHub Release 发布后：

1. 检出对应标签及完整 Git 历史。
2. 运行单元测试和 Native AOT 发布验证。
3. 构建 Setup 安装包。
4. 验证安装、右键菜单、启动和卸载。
5. 上传 Setup、AOT ZIP 和指标文件到工作流产物及原 GitHub Release。

当前安装包**未进行代码签名**，Windows SmartScreen 可能提示未知发布者。正式发布建议配置受信任代码签名证书，并对 Setup 与 EXE 签名。
