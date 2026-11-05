# Windows 右键菜单设计

## 目标

右键菜单围绕三个高频入口场景设计：**文档/图片转 PDF、多选合并、PDF 提取文本**。它是任务入口，不是完整功能清单。

## 菜单矩阵

| 选中内容 | AvaPDF 子菜单 | 执行方式 |
| --- | --- | --- |
| 一个 PDF | 提取文本 | 直接生成 TXT |
| 多个 PDF | 合并 PDF | 进入合并工作区，确认顺序和输出后执行 |
| 一个支持的文档 | 转换为 PDF | 生成同名 PDF，不覆盖源文件 |
| 多个支持的文档 | 批量转换为 PDF | 每个输入生成独立 PDF |
| 一张图片 | 转换为 PDF | 生成单页 PDF |
| 多张图片 | 合并为 PDF | 进入图片工作区，确认顺序和输出后执行 |
| PDF 与其他格式混合 | 不显示 AvaPDF 菜单 | 不提供隐式混合合并 |
| 不支持的文件或文件夹 | 不显示 AvaPDF 菜单 | 不静默跳过文件 |

支持的文档和图片来源与现有转换器一致：Word、Excel、PowerPoint、Markdown、HTML、RTF、TXT，以及 JPG、PNG、BMP、GIF、WEBP、TIF、SVG 等。

## 安全与交互

- 快速任务只生成新文件，不覆盖源文件；同名输出自动添加序号。
- 默认输出到源文件所在目录；不可写或处理失败时在 AvaPDF 中显示错误。
- 转换警告必须展示，不能静默忽略。
- 合并必须进入工作区确认文件顺序，默认按自然文件名排序，不依赖 Explorer 的选中顺序。
- Shell DLL 不解析或转换文档，实际工作由 AvaPDF 的后台任务执行。
- 任务文件不包含密码；密码功能不会通过快速任务自动执行。

## 启动任务协议

Shell 在 `%TEMP%\AvaPDF\Jobs` 创建一次性 UTF-8 JSON 文件，并通过以下参数启动应用：

```text
AvaPDF.Desktop.exe --job "<job.json>"
```

任务结构：

```json
{
  "tool": "extracttext",
  "autoRun": true,
  "files": ["D:\\Documents\\sample.pdf"]
}
```

- `tool` 为现有稳定工具标识；文档批量转换使用 `null`，由应用按扩展名选择转换器。
- `autoRun=true` 用于提取文本和转换；合并使用 `false`，由用户在工作区确认后执行。
- `files` 保留完整多选列表。
- 应用只删除上述专用目录中符合 `AvaPDF-*.json` 命名的任务文件，不删除其他路径。
- 使用任务文件而不是拼接大量命令行参数，避免长路径和多文件的命令行长度限制。

## 原生实现

- `IContextMenu` 负责动态菜单、命令 ID 和启动应用。
- 安装器通过 CLSID 的 `AppExecutable` 值保存当前安装目录中的启动程序，避免右键启动到其他 AvaPDF 安装实例。
- `IShellExtInit` 从 `CF_HDROP` 接收完整选中文件列表。
- 菜单命令 ID 使用 Explorer 的数字偏移，同时保留测试用的字符串动词。
- 不支持的选中内容不会创建菜单。
- 安装器按支持的扩展名注册 `SystemFileAssociations\<ext>\ShellEx\ContextMenuHandlers\AvaPDF`；MSIX 商店版不包含此扩展。

## 验证

- .NET 测试覆盖任务协议、格式分类、真实快速提取和批量转换。
- `TestShellMenu.exe` 模拟 `CF_HDROP`，覆盖单/多 PDF、文档、图片、混合与不支持文件。
- `eng/test-installer.ps1` 额外验证安装后的 `InvokeCommand`、中文路径和实际 TXT 输出。
