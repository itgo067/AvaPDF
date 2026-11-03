# 架构说明

## 设计目标

1. PDF 用例不依赖 Avalonia、Windows 路径或桌面文件对话框。
2. OfficeIMO 仅出现在基础设施适配项目中，业务协议不泄露第三方类型。
3. 每项操作独立注册、独立测试，后续可继续按业务模块拆成程序集。
4. 桌面端与未来 Web 端共享同一套请求、校验、调度和 PDF 处理器。

## 各层职责

### Domain

包含稳定的 `PdfOperationKind`、工具元数据、操作结果和进度模型。无第三方依赖，不执行 I/O。

### Application

`PdfJobService` 是统一应用入口，根据操作标识分派到 `IPdfOperationHandler`。请求对象携带该操作需要的全部参数。

输入输出通过以下抽象解耦宿主：

- `IPdfSource`：按需提供只读流，可来自本地文件、HTTP 上传、对象存储。
- `IPdfOutputTarget`：按文件名创建输出流，宿主决定覆盖、临时文件、下载或存储策略。

`PageRangeExpression` 在引擎外完成基础格式、页码边界、空范围和重复页策略校验。

### PDF / OfficeIMO

每个 Handler 只负责一项 PDF 操作。`OfficePdf` 集中处理流加载、保存、进度和通用文件名逻辑。依赖注入通过 `AddOfficeImoPdf()` 一次注册。

OfficeIMO 的加载方法会快照输入，因此输入流在加载后即可释放；输出流由 Handler 和宿主共同确定生命周期。

### Document converters / OfficeIMO

`AvaPDF.Converters.OfficeIMO` 独立承载 Word、Excel、PowerPoint、Markdown、HTML、RTF 和 TXT 转 PDF，避免这些格式依赖进入 PDF 页面处理模块。每个 Handler 使用统一的流式转换管线，返回输出文件及结构化警告，并通过 `AddOfficeImoDocumentConverters()` 注册。

`DocumentToPdfRequest.ResourceBaseDirectory` 是宿主明确授权的资源根目录。桌面端只传入所选 Markdown/HTML 文件所在目录，用于解析相对图片和资源；Web 宿主需要自行设置隔离目录，不能直接信任上传路径。远程资源解析默认不开启。

### Desktop

- `MainWindowViewModel`：工具目录和当前工具。
- `ToolWorkspaceViewModel`：文件列表、参数、任务生命周期和结果。
- `FileDialogService`：Avalonia StorageProvider 对话框。
- `LocalPdfSource` / `LocalPdfOutputTarget`：Application 流抽象的本地文件实现。
- `App`：唯一的依赖注入组合根。

PDF 工作通过 `Task.Run` 移出 UI 线程，并传递取消令牌与进度。ViewModel 不引用 OfficeIMO 类型。转换结果中的警告单独展示，避免把成功生成文件等同于完全无损转换。

## 后续 Web 宿主

建议新增 `AvaPDF.Web`，只引用 Application 与 OfficeIMO 适配项目：

1. 将 `IFormFile.OpenReadStream()` 包装为 `IPdfSource`，并在 Web 边界设置上传大小和请求超时限制。
2. 单文件下载可用临时文件实现 `IPdfOutputTarget`，完成后以 `FileStreamResult` 返回；多文件结果可写入 ZIP。
3. Controller / Minimal API 只负责 HTTP 模型映射，构造现有请求并调用 `PdfJobService`。
4. 若需要异步任务，将请求参数持久化到任务表，由 Worker 调用同一应用入口；不要在大文件场景把 PDF 转成 Base64 放入队列。

Web 边界还应增加身份认证、上传配额、临时文件清理、并发限制、病毒扫描和可观测性，这些不属于桌面 MVP。

## 大型文件与后续优化

当前抽象避免核心层依赖路径，但 OfficeIMO 的部分操作会缓冲文档，图片转 PDF 也会暂存单张图片编码数据。首个版本对单张图片设置 128 MiB 限制。处理超大文档时，应增加统一的输入/输出预算、临时文件策略和后台任务队列，而不是在 UI 中自行绕过引擎限制。
