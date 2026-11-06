# Changelog

## [0.2.4] - 2026-11-06

包含 [0.2.3](#023---2026-11-06) 的全部功能与修复。

### 新增

- 应用启动后仅检查一次 GitHub 最新 Release，发现更高版本时在窗口顶部显示更新提醒。
- 更新提醒显示当前版本和最新版本，可直接打开 Release 页面，也可忽略该版本，后续启动不再重复提醒。
- 版本比较支持正式版、alpha、beta、rc 和 MinVer 提交高度；离线、超时或 API 限流不会阻断应用启动。

### 修复

- 更新检查固定使用 `https://api.github.com/repos/itgo067/AvaPDF/releases/latest`，避免错误的仓库 API 地址。

## [0.2.3] - 2026-11-06

包含 [0.2.2](#022---2026-11-06) 的全部功能与修复。

### 修复

- Windows 安装验证改用纳入版本控制的 `tests/Fixtures/StartupSample.pdf`，避免 CI 因缺少本地 `artifacts` 测试文件而失败。

## [0.2.2] - 2026-11-06

包含 [0.2.1](#021---2026-11-06) 的全部功能与修复。

### 修复

- 为 Release 编排工作流补充 `actions: write` 权限，确保创建 Release 后能够调用 Windows 和 Linux 构建工作流上传产物。

## [0.2.1] - 2026-11-06

包含 [0.2.0](#020---2026-11-06) 的全部功能与改进。

### 修复

- 修复由 `GITHUB_TOKEN` 创建 Release 时不会自动触发后续工作流的问题；创建 Release 后现在会显式调用 Windows 和 Linux 构建，并传入版本标签上传安装包。

## [0.2.0] - 2026-11-06

### 新增

- Windows Inno Setup 安装包，支持当前用户安装、旧版本卸载升级、开始菜单和桌面快捷方式。
- 原生 COM 右键菜单，支持 PDF、Word、Excel、PowerPoint、Markdown、HTML、RTF、TXT 和常见图片格式。
- 单个 PDF 可快速提取文本；多个 PDF 可进入合并工作区确认顺序；文档和图片可转换或批量转换为 PDF。
- 右键任务支持多选、中文路径、长路径和一次性 JSON 任务协议；所有输出均使用新文件，不覆盖源文件。
- Linux DEB 和 AppImage 发布，包含 KDE、GNOME、MATE、Cinnamon 和 XFCE 的任务入口。
- 系统托盘支持显示/隐藏主窗口、打开输出目录和退出；关闭主窗口时可隐藏到托盘并继续后台任务。
- 关于窗口显示 MinVer 版本和提交信息，并链接到 GitHub 仓库。

### 改进

- Linux 内置 CJK 字体回退，改善中文 PDF 创建和转换的兼容性。
- 安装、升级、卸载及 Shell 集成都加入自动化验证，覆盖真实 `InvokeCommand` 和中文路径输出。
- MSIX 原型明确商店版不包含传统 COM 右键扩展，避免未验证的 Shell 集成进入商店包。
- CI 支持推送 `v*` 标签后自动创建 GitHub Release，并上传 Windows、Linux 发布产物。
- 日常 CI 使用轻量 Desktop AOT 构建和启动验证，完整 AotSmoke 保留为 Release 必检项。
- 发布构建不再生成发布包不需要的原生调试符号，并缓存 NuGet、AppImageTool 和 Inno Setup，降低构建时间和网络依赖。

### 修复

- 修复旧版本卸载器查找、升级等待和卸载清理问题。
- 修复 Linux KDE 服务菜单校验及非 root 环境的 Python 缓存写入失败。
- 修复 Avalonia 启动文件载入时机，确保外部调用在窗口初始化后载入文件。
- 修复已知 Ursa AOT 警告的发布策略，仅允许指定警告，不错误屏蔽其他裁剪问题。
