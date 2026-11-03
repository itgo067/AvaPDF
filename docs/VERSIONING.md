# 版本管理

AvaPDF 使用 **MinVer 8.0.0**，以 Git 标签作为正式版本的唯一来源，不在项目文件中重复维护产品版本号。

## 规则

- 正式版本使用带注释标签：`v0.1.0`、`v0.2.0`。
- 预发布版本使用：`v0.2.0-alpha.1`、`v0.2.0-beta.1`、`v0.2.0-rc.1`。
- 标签之后的未发布提交由 MinVer 自动增加修订号、预发布标识和提交高度。
- MinVer 将版本写入程序集、文件信息和包版本，不需要手动修改 `Version`。
- CI 使用 `fetch-depth: 0` 获取完整历史与标签。

## 版本展示与发布产物

- 主窗口左侧“关于 AvaPDF”打开关于窗口。
- 关于窗口读取 `AssemblyInformationalVersionAttribute`，显示语义化版本和短提交哈希。
- `eng/publish-desktop.ps1` 从发布 EXE 的 `ProductVersion` 读取同一版本。
- ZIP 命名为 `AvaPDF-<version>-win-x64-aot.zip`，CoreCLR 回退包使用 `coreclr` 后缀。
- 发布指标 JSON/Markdown 同时记录版本、提交哈希、文件版本、体积和启动耗时。
- CI 上传带版本号的 ZIP，并通过通配符匹配，不另行拼接版本。

## 发布新版本

```bash
dotnet test AvaPDF.slnx -c Release --nologo
git status --short --branch
git tag -a v0.1.0 -m "AvaPDF 0.1.0"
```

将标签和提交推送到同一远程仓库后，GitHub Actions 会按该提交和标签计算相同版本。创建标签前，应先完成测试、文档和提交；不要在标签之后继续追加本应属于该版本的改动。
