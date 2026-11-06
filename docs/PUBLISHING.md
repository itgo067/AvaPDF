# 桌面发布策略

AvaPDF 采用 **Native AOT 优先、自包含 CoreCLR 兜底** 的 Windows 发布策略。日常开发、普通构建和普通 `dotnet publish` 不强制启用 AOT。

## 一键发布

```powershell
powershell -File eng/publish-desktop.ps1
```

脚本执行以下步骤：

1. 通过 `vswhere` 检测 Visual Studio C++ x64 工具链。
2. 条件满足时，以 Native AOT 发布 `AvaPDF.AotSmoke`。
3. 在 AOT 环境运行 23 个验证场景，覆盖 20 个工具：既有 PDF 页面与内容工具、现代与旧版 Office 转换、水印、加解密和元数据。
4. 冒烟测试通过后发布 Native AOT 桌面程序。
5. 三次启动桌面程序并测量主窗口创建耗时，统计 EXE、完整目录和 ZIP 体积，生成 JSON、Markdown 和不含 PDB 的 ZIP 包。
6. 工具链缺失、AOT 发布失败或任何验证失败时，明确警告并发布自包含 CoreCLR 版本，同样记录体积和启动指标。

输出目录：

```text
artifacts/publish/win-x64-aot/       Native AOT 桌面程序
artifacts/publish/win-x64-coreclr/   自包含 CoreCLR 桌面程序
artifacts/publish/aot-smoke/         AOT 验证程序
artifacts/publish/AvaPDF-win-x64-aot.zip
artifacts/metrics/win-x64-aot.json   完整机器可读指标
artifacts/metrics/win-x64-aot.md     可读指标摘要
```

启动耗时采用“进程启动到 `MainWindowHandle` 非零”的统一口径，连续测量三次。ZIP 不包含 PDB 调试符号。历史数据和本机基线见 [BENCHMARKS.md](BENCHMARKS.md)。

## CI 验证

工作流 `.github/workflows/desktop-aot.yml` 在 `main` 推送、相关 Pull Request 和手动触发时运行于 `windows-latest`。日常 CI 使用 `-RequireAot -SkipAotSmoke`，执行轻量 Desktop AOT 构建和三次窗口启动验证，不再重复编译独立 AotSmoke。工具链缺失、AOT 编译失败或窗口启动超时都会使工作流失败，不会静默回退。

只有 Release 构建保留完整 AotSmoke，覆盖 23 个场景和全部 20 个工具；日常 PR 不重复执行这部分全量操作验证。

每次成功运行会将指标写入 Job Summary，并上传 ZIP、JSON 和 Markdown 指标，产物保留 14 天。

## CI 耗时优化

- `desktop-aot.yml` 只负责 `main` 和 Pull Request 的日常 AOT 验证，不再对 `v*` 标签重复发布；Release 的 Windows AOT 由 `release-installer.yml` 统一构建。
- 三个构建工作流使用 `setup-dotnet` 的 NuGet 缓存，依赖版本文件作为缓存键。
- Linux 的 AppImageTool 和 Windows 的 Inno Setup 使用 Actions Cache，同时保留 AppImageTool 的 SHA-256 校验。
- 同一工作流、同一分支出现新提交时自动取消过期运行，减少重复 AOT 编译。
- CI 使用 `-DisableNativeDebugSymbols`，避免为发布包不需要的原生 PDB 生成和写入调试信息；本地需要原生调试时不加此参数。

本机诊断构建中，关闭原生符号将总发布时间从约 503 秒降至 414 秒，其中 ILC 编译从约 462 秒降至 394 秒。实际 CI 收益取决于 runner 和缓存。

这些优化不减少单元测试、AOT 冒烟、GUI 启动、安装或卸载验证。

## 自动创建 GitHub Release

`.github/workflows/create-release.yml` 在推送 `v*` 标签或手动输入版本标签时运行：

1. 校验标签格式；
2. 使用 `GITHUB_TOKEN` 创建 Release 并生成发布说明；
3. 预发布标签自动设置 prerelease；
4. 已存在的 Release 不覆盖描述和备注，支持安全重跑。

Release 创建后，`release-installer.yml` 和 `linux-packages.yml` 的 `release: published` 触发器接管构建、安装验证和产物上传。自动发布不需要手动在 GitHub 页面先建 Release。

## 强制要求 AOT

CI 或发布环境不允许回退时：

```powershell
powershell -File eng/publish-desktop.ps1 -RequireAot
```

此模式在缺少工具链或任何 AOT 验证失败时返回失败，不生成 CoreCLR 替代包。

## 环境要求

- .NET 10 SDK
- Windows PowerShell 5.1+ 或 PowerShell 7+
- Windows x64
- Visual Studio 2022 或兼容版本
- “使用 C++ 的桌面开发”工作负载及 MSVC x64 工具链

脚本通过 `vcvars64.bat` 建立原生链接环境，并设置 `IlcUseEnvironmentalTools=true`，避免依赖终端预先配置 MSVC PATH。

Ursa 2.3.0 的主题反射绑定和 `EnumSelector` 会产生已知的 `IL2026`、`IL3050`、`IL2075` 警告。AOT 发布仅将这三类警告设为不作为错误，并通过 `TrimmerSingleWarn=false` 保留完整警告明细；其他裁剪或 AOT 警告仍会阻断发布。

## 当前边界

Native AOT 不等于单一 EXE。Avalonia 的 Skia、HarfBuzz 和 OpenGL ES 原生库仍需随程序发布。AOT 冒烟测试通过 23 个场景覆盖当前 20 个工具，但新加入 OCR、更多 Office 格式、PDF/A、签名或第三方 Provider 时，必须扩展验证。普通 CoreCLR 单元测试仍是功能正确性的主要测试入口。
