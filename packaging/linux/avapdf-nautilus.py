"""AvaPDF context menu extension for Nautilus (GNOME) and Caja (MATE).

Install targets (handled by eng/build-linux-packages.sh):
  /usr/share/nautilus-python/extensions/avapdf.py   (Nautilus)
  /usr/share/caja-python/extensions/avapdf.py       (Caja)

Requires the nautilus-python (python3-nautilus) or caja-python package.

菜单矩阵与 docs/SHELL_MENU.md 的 Windows 右键设计保持一致：
  单个 PDF        → 提取文本（autoRun）；更多工具…
  多个 PDF        → 合并 PDF（进工作区确认）；更多工具…
  单个文档/图片   → 转换为 PDF（autoRun）；更多工具…
  多个文档/图片   → 批量转换为 PDF（autoRun）；更多工具…
  混合/不支持     → 仅「更多工具…」/ 不显示菜单
所有入口通过 avapdf-job 生成一次性任务文件后以 `avapdf --job` 启动。
"""

import locale
import subprocess

import gi

_PROVIDER_MODULE = None
for _name, _version in (("Nautilus", "4.0"), ("Nautilus", "3.0"), ("Caja", "2.0")):
    try:
        gi.require_version(_name, _version)
        _PROVIDER_MODULE = _name
        break
    except ValueError:
        continue

if _PROVIDER_MODULE is None:
    raise ImportError("neither Nautilus nor Caja typelibs are available")

if _PROVIDER_MODULE == "Nautilus":
    from gi.repository import Nautilus as FileManager
else:
    from gi.repository import Caja as FileManager
from gi.repository import GObject

_ZH = (locale.getlocale()[0] or "").lower().startswith("zh")

_DOCUMENT_EXTENSIONS = {
    ".doc", ".docx", ".odt", ".xls", ".xlsx", ".ods",
    ".ppt", ".pptx", ".odp", ".md", ".markdown", ".html", ".htm",
    ".rtf", ".txt",
}
_IMAGE_EXTENSIONS = {".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp", ".tif", ".tiff", ".svg"}


def _label(zh, en):
    return zh if _ZH else en


class AvaPDFMenuProvider(GObject.GObject, FileManager.MenuProvider):
    def get_file_items(self, *args):
        # Nautilus 4: (files); Nautilus 3 / Caja: (window, files)
        files = args[-1]
        paths = [f.get_location().get_path() for f in files if f.get_location()]
        if not paths or any(not p for p in paths):
            return []

        kinds = {_kind_of(p) for p in paths}
        items = []
        single = len(paths) == 1

        if kinds == {"pdf"}:
            if single:
                items.append(self._make_item(
                    "AvaPDFExtractText", _label("提取文本", "Extract Text"),
                    ["--tool", "extracttext", "--auto"], paths))
            else:
                items.append(self._make_item(
                    "AvaPDFMerge", _label("合并 PDF", "Merge PDFs"),
                    ["--tool", "merge"], paths))
        elif kinds == {"document"} or kinds == {"image"}:
            items.append(self._make_item(
                "AvaPDFConvertToPdf",
                _label("转换为 PDF", "Convert to PDF") if single
                else _label("批量转换为 PDF", "Batch Convert to PDF"),
                ["--auto"], paths))
        elif "unsupported" in kinds:
            return []

        items.append(self._make_item(
            "AvaPDFMoreTools", _label("更多工具…", "More Tools..."), [], paths))
        return items

    def _make_item(self, name, label, extra_args, paths):
        item = FileManager.MenuItem(
            name=name,
            label=label,
            tip=_label("通过 AvaPDF 处理所选文件", "Process the selection with AvaPDF"),
            icon="avapdf",
        )
        item.connect("activate", self._on_activate, extra_args, paths)
        return item

    def _on_activate(self, _item, extra_args, paths):
        subprocess.Popen(["avapdf-job", *extra_args, *paths])


def _kind_of(path):
    lowered = path.lower()
    if lowered.endswith(".pdf"):
        return "pdf"
    for ext in _DOCUMENT_EXTENSIONS:
        if lowered.endswith(ext):
            return "document"
    for ext in _IMAGE_EXTENSIONS:
        if lowered.endswith(ext):
            return "image"
    return "unsupported"
