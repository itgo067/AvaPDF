# Git 工作流

## 分支约定

- `main`：始终保持可构建、可测试的基线。
- `feature/<name>`：新功能，例如 `feature/pdf-watermark`。
- `fix/<name>`：问题修复，例如 `fix/page-range-validation`。
- `refactor/<name>`：不改变行为的结构调整。
- `docs/<name>`：文档更新。

功能完成后通过 Pull Request 合并；独立本地开发时，也建议先测试再合入 `main`。

## 提交信息

使用简洁的 Conventional Commits：

```text
feat: add PDF watermark operation
fix: validate custom split page ranges
refactor: isolate OfficeIMO stream handling
docs: describe desktop theme preferences
test: cover PDF page reordering
chore: update Avalonia packages
```

常用类型：`feat`、`fix`、`refactor`、`docs`、`test`、`build`、`ci`、`chore`。

## 提交前检查

```bash
dotnet test AvaPDF.slnx
dotnet build AvaPDF.slnx -c Release --nologo
git diff --check
```

不要提交 `bin/`、`obj/`、本地 NuGet 检查包、测试结果、用户配置或密钥。Logo 的 SVG 源文件和生成脚本需要入库，生成的 PNG/ICO 也作为桌面发布资源入库。
