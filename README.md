# AvaPDF

基于 **.NET 10、Avalonia 12.1.5、OfficeIMO.Pdf 3.5.3** 的本地 PDF 工具台。项目采用分层架构与按功能拆分的处理器，桌面界面不承载 PDF 业务逻辑，为后续 ASP.NET Core / Web 版本复用核心能力。

## 当前功能

| 分类 | 功能 |
| --- | --- |
| 页面工具 | 多文件合并、逐页/按页数/按范围拆分、页面提取、页面删除、页面旋转、页面重排 |
| 优化与转换 | 无损优化、图片转 PDF、原生文本提取为 UTF-8 TXT |
| 桌面体验 | 亮色/暗色/跟随系统三态主题、主题偏好持久化、文件排序、参数面板、输出目录选择、后台执行、取消、进度反馈、结果文件打开 |

所有文件均在本机处理。输出文件遇到同名时自动添加序号，不覆盖已有文件。

## 运行

要求安装 .NET SDK 10.0.403 或兼容的 .NET 10 SDK。

```bash
dotnet restore AvaPDF.slnx
dotnet run --project src/AvaPDF.Desktop/AvaPDF.Desktop.csproj
```

测试：

```bash
dotnet test AvaPDF.slnx
```

发布 Windows x64 版本（**Native AOT 优先，自包含 CoreCLR 兜底**）：

```powershell
powershell -File eng/publish-desktop.ps1
```

脚本会检测 MSVC 工具链，先运行全部 PDF 功能的 AOT 冒烟测试，再发布桌面程序，并记录体积和三次窗口启动耗时、生成 ZIP；条件不足或验证失败时明确提示并回退到 CoreCLR。强制要求 AOT 时使用 `-RequireAot`。详见 [docs/PUBLISHING.md](docs/PUBLISHING.md) 和 [docs/BENCHMARKS.md](docs/BENCHMARKS.md)。

## 项目结构

```text
src/
  AvaPDF.Domain/          工具目录、操作标识、结果与进度模型
  AvaPDF.Application/     流抽象、操作请求、参数解析、统一任务入口
  AvaPDF.Pdf.OfficeIMO/   OfficeIMO 适配器；每项功能一个独立处理器
  AvaPDF.Desktop/         Avalonia 12、MVVM、文件对话框、桌面组合根
tests/
  AvaPDF.Tests/           页码解析测试与真实 PDF 操作测试
  AvaPDF.AotSmoke/        覆盖全部 PDF 功能的 Native AOT 冒烟测试
```

依赖方向：

```text
AvaPDF.Desktop ───────> AvaPDF.Application ───────> AvaPDF.Domain
       │                          ▲
       └── 仅组合根注册 ─> AvaPDF.Pdf.OfficeIMO ─────┘
```

更详细的边界和 Web 复用方式见 [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)，分支与提交规范见 [docs/GIT_WORKFLOW.md](docs/GIT_WORKFLOW.md)。

## 扩展一个 PDF 工具

1. 在 `PdfOperationKind` 中增加稳定标识，并在 `PdfToolCatalog` 添加描述。
2. 在 Application 层定义请求模型。
3. 在 `AvaPDF.Pdf.OfficeIMO/Features` 新增 `PdfOperationHandler<TRequest>`。
4. 在 `AddOfficeImoPdf` 中注册处理器。
5. 添加真实 PDF 测试；桌面端按参数需求增加对应设置面板。

## 品牌资源

Logo 位于 `src/AvaPDF.Desktop/Assets/`：`Logo.svg` 为完整应用图标，`LogoMark.svg` 为透明图形标，同时提供 PNG 和多尺寸 ICO。修改 SVG 或生成脚本后，可运行 `eng/generate-logo.ps1` 重新生成桌面资源。

## 当前边界

- “优化 PDF”是 OfficeIMO 的**无损结构优化**，不是强制缩小文件或有损图片压缩；没有更小结果时保留原始内容。
- “提取文本”只提取 PDF 原生文本，扫描件需要后续接入 `OfficeIMO.Pdf.Ocr` 与 OCR Provider。
- 加密、签名或带特殊权限的 PDF 可能限制部分改写，错误会显示在界面中，不会绕过安全策略。
- 当前版本尚未包含 OCR、水印、页码、元数据编辑、PDF/A 转换、Office 文档转 PDF 和 Web 宿主。
