# 更新与网络代理

## 更新方式

- 应用启动时只检查一次 GitHub 最新 Release，发现更高版本时显示提醒。
- “查看更新”打开 Release 页面；“忽略此版本”只忽略当前版本，不影响后续版本。
- “下载并重启更新”使用 Velopack 的 GitHub Releases 源下载、校验并应用更新，然后自动重启。
- Windows 安装包由 `eng/build-velopack.ps1` 生成，Release 同时包含 `releases.win.json` 和 Velopack 更新包。

## 安装边界

Velopack 的一键更新只对 Velopack 安装实例有效。首次使用 Velopack Setup 安装后，后续版本即可一键更新。Inno Setup 安装的用户可以先打开 Release 完成一次 Velopack 安装。

安装、更新和卸载钩子会维护稳定 Shell DLL 路径和 `AppExecutable`，COM 与 PDF 右键菜单会指向 Velopack 启动器。Shell DLL 内容变化或卸载时使用 Restart Manager 释放宿主进程，避免文件锁和 `.previous` 残留。

## 网络代理

主窗口左侧“网络代理”提供：

- **跟随系统代理**：使用操作系统默认代理；
- **不使用代理**：直连 GitHub；
- **自定义 HTTP 代理**：输入 `http://host:port`，例如 `http://127.0.0.1:7890`。

代理设置同时作用于 Release 检查和 Velopack 下载。自定义地址仅支持 HTTP/HTTPS，不能包含账号或密码；应用不会保存代理凭据。设置保存在当前用户的 AvaPDF 配置目录，网络异常不会阻断其他 PDF 功能。
