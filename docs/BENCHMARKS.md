# 发布体积与启动耗时

## 本机基线：win-x64 Native AOT

记录日期：2026-11-03。配置：Release、.NET 10.0.403 SDK、Windows 10.0.26300、Avalonia 12.1.5、OfficeIMO.Pdf 3.5.3。

| 指标 | 结果 |
| --- | ---: |
| `AvaPDF.Desktop.exe` | 111.76 MiB |
| 完整发布目录（不含 PDB） | 129.73 MiB |
| ZIP 发布包（不含 PDB） | 47.87 MiB |
| 第一次启动 | 589.6 ms |
| 第二次启动（热启动） | 349.4 ms |
| 第三次启动（热启动） | 382.2 ms |
| 三次平均启动耗时 | 440.4 ms |
| 最小 / 最大启动耗时 | 349.4 / 589.6 ms |

启动耗时的测量口径是：**启动进程到 `MainWindowHandle` 变为非零**。它不代表所有异步内容都已完成，也不等同于严格意义上的操作系统冷启动。第一次运行单独记录，后续运行属于热启动。重复测量会受系统负载和文件缓存影响，结果只适合作为同环境回归基线，不应直接用于不同硬件之间的性能承诺。

## CI 记录

`.github/workflows/desktop-aot.yml` 在 Windows CI 中执行单元测试、23 个 PDF 与文档转换验证场景的 AOT 冒烟测试、桌面发布、三次窗口启动测量和 ZIP 打包。

每次运行都会：

- 将体积和启动耗时写入 GitHub Actions Job Summary。
- 生成 `artifacts/metrics/win-x64-aot.json`，保存每次启动数据、平均值、极值、文件明细和运行环境。
- 生成便于查看的 `artifacts/metrics/win-x64-aot.md`。
- 上传不含 PDB 的 `AvaPDF-win-x64-aot.zip` 和指标文件，保留 14 天。

本机执行以下命令会使用相同测量与打包逻辑：

```powershell
powershell -File eng/publish-desktop.ps1 -RequireAot
```
