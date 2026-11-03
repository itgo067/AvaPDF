# 桌面发布策略

AvaPDF 采用 **Native AOT 优先、自包含 CoreCLR 兜底** 的 Windows 发布策略。日常开发、普通构建和普通 `dotnet publish` 不强制启用 AOT。

## 一键发布

```powershell
powershell -File eng/publish-desktop.ps1
```

脚本执行以下步骤：

1. 通过 `vswhere` 检测 Visual Studio C++ x64 工具链。
2. 条件满足时，以 Native AOT 发布 `AvaPDF.AotSmoke`。
3. 在 AOT 环境实际运行合并、拆分、提取、删除、旋转、重排、优化、图片转 PDF 和文本提取。
4. 冒烟测试通过后发布 Native AOT 桌面程序。
5. 工具链缺失、AOT 发布失败或冒烟测试失败时，明确警告并发布自包含 CoreCLR 版本。

输出目录：

```text
artifacts/publish/win-x64-aot/       Native AOT 桌面程序
artifacts/publish/win-x64-coreclr/   自包含 CoreCLR 桌面程序
artifacts/publish/aot-smoke/         AOT 验证程序
```

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

## 当前边界

Native AOT 不等于单一 EXE。Avalonia 的 Skia、HarfBuzz 和 OpenGL ES 原生库仍需随程序发布。AOT 冒烟测试覆盖当前 9 项 PDF 功能，但新加入 OCR、Office 转换、PDF/A、签名或第三方 Provider 时，必须扩展验证。普通 CoreCLR 单元测试仍是功能正确性的主要测试入口。
