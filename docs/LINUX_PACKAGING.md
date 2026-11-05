# Ubuntu / Kubuntu 发布计划

## 格式选择

优先级为 **DEB + AppImage**，Flatpak 暂缓。

| 格式 | 定位 | 原因 |
| --- | --- | --- |
| DEB | Kubuntu 正式安装版 | 原生应用菜单、文件关联、系统图标、依赖管理及 KDE 右键集成 |
| AppImage | 免安装便携版 | 单文件分发，不依赖系统包管理器，适合试用和便携使用 |
| Flatpak | 后续渠道 | 沙箱下需要重新验证文档门户、输出目录、原生库和文件访问权限，不作为第一版 |

## 目标环境

- 首个架构：`linux-x64`。
- CI 构建：`ubuntu-latest`。
- 用户桌面验证：Kubuntu 26.04。
- 发布仍采用 Native AOT，不要求用户安装 .NET Runtime。
- `.github/workflows/linux-packages.yml` 构建 linux-x64 Native AOT、DEB 和 AppImage，并在 Xvfb 中检查真实 AvaPDF 窗口。
- CI 实际执行 DEB 安装、程序入口与 KDE 文件检查、GUI 启动、卸载清理，以及 AppImage 的 extract-and-run 启动。
- AppImageTool 固定为 1.9.1，并在 CI 中校验官方 SHA-256。
- 中文 PDF 显式嵌入 `DroidSansFallbackFull.ttf`，同时保留 `DejaVu Sans` 等拉丁字体回退；CI 安装 `fonts-droid-fallback`，DEB 同时推荐 `fonts-noto-cjk` 和 `fonts-droid-fallback`。
- KDE 菜单与真实桌面交互仍建议在 Kubuntu 26.04 做最终人工验证。

## DEB 结构

计划安装到：

```text
/opt/avapdf/                                程序和原生依赖
/usr/bin/avapdf                            启动入口或符号链接
/usr/bin/avapdf-job                        右键菜单任务启动器（生成 --job 任务文件）
/usr/share/applications/avapdf.desktop     应用菜单与文件关联
/usr/share/icons/hicolor/512x512/apps/avapdf.png
/usr/share/kio/servicemenus/avapdf-pdf.desktop        KDE：单个 PDF（提取文本）
/usr/share/kio/servicemenus/avapdf-pdf-merge.desktop  KDE：多个 PDF（合并）
/usr/share/kio/servicemenus/avapdf-convert.desktop    KDE：文档/图片（转 PDF）
/usr/share/nautilus-python/extensions/avapdf.py   GNOME Nautilus（需 python3-nautilus，Suggests）
/usr/share/caja-python/extensions/avapdf.py       MATE Caja（需 caja-python，Suggests）
/usr/share/nemo/actions/avapdf-*.nemo_action      Cinnamon Nemo（声明式，无额外依赖）
/usr/share/avapdf/thunar/uca.xml                  XFCE Thunar 模板，需用户合并进自己的 uca.xml
```

右键集成在 KDE/GNOME/MATE/Cinnamon 上随 DEB 自动生效（GNOME/MATE 需装对应 python 扩展包后重启文件管理器）；Thunar 无系统级注册机制，只提供模板。AppImage 不做任何系统级右键注册。

Linux 右键菜单的菜单矩阵与 Windows 版保持一致（见 docs/SHELL_MENU.md）：单个 PDF 提供提取文本、多个 PDF 提供合并、文档/图片提供转 PDF，不提供额外的「更多工具…」入口。各桌面入口统一调用 `avapdf-job [--tool …] [--auto] <文件…>`，由它按 SHELL_MENU.md 的启动任务协议在 `$TMPDIR/AvaPDF/Jobs` 生成一次性 `AvaPDF-*.json` 后以 `avapdf --job` 启动应用；autoRun 仅用于提取文本和转换，合并进入工作区确认。

`avapdf.desktop` 使用 `avapdf --file %F`，支持文件管理器带入文件。KDE 服务菜单通过 `avapdf-job` 提供提取文本、合并与转 PDF 入口。

DEB 构建时需要：

- 使用 `dpkg-deb` 生成标准包。
- 对实际随包原生库计算依赖，不凭空硬编码完整依赖列表。
- 验证安装、程序入口、桌面文件、KDE 服务菜单和卸载。
- 使用 `desktop-file-validate` 校验桌面文件。

## AppImage

AppImage 与 DEB 共用同一 AOT 目录，包含 AppRun、桌面文件和图标。CI 使用固定版本的 AppImage 工具并校验工具来源，生成：

```text
AvaPDF-<version>-linux-x64.deb
AvaPDF-<version>-linux-x64.AppImage
```

AppImage 的 KDE 右键菜单不能仅依靠 AppImage 内部文件完成系统级注册；需要用户选择安装集成，或由桌面集成工具处理。DEB 是需要系统集成功能时的首选。

## Flatpak 暂缓原因

当前工具需要读取用户选择的文件、写入指定输出目录，并可能读取 Markdown/HTML 的相对资源。Flatpak 化前需要明确文档门户和宿主路径权限，不能简单添加 `host` 全权访问来规避审核。待 DEB/AppImage 稳定后再评估 Flatpak。
