#!/usr/bin/env bash
set -Eeuo pipefail

ROOT=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
ARTIFACT_ROOT=${AVAPDF_ARTIFACT_ROOT:-"$ROOT/artifacts"}
PROJECT="$ROOT/src/AvaPDF.Desktop/AvaPDF.Desktop.csproj"
PUBLISH_DIR="$ARTIFACT_ROOT/publish/linux-x64-aot"
PACKAGE_DIR="$ARTIFACT_ROOT/publish"
DEB_ROOT="$ARTIFACT_ROOT/linux/deb-root"
APPDIR="$ARTIFACT_ROOT/linux/AppDir"

rm -rf "$PUBLISH_DIR" "$DEB_ROOT" "$APPDIR"
mkdir -p "$PUBLISH_DIR" "$PACKAGE_DIR" "$DEB_ROOT" "$APPDIR"

dotnet publish "$PROJECT" \
  --nologo \
  -c Release \
  -r linux-x64 \
  -p:PublishAot=true \
  -p:IlcUseEnvironmentalTools=true \
  -p:TrimmerSingleWarn=false \
  -p:WarningsNotAsErrors=IL2026%3BIL3050%3BIL2075 \
  -p:StripSymbols=true \
  -o "$PUBLISH_DIR"

VERSION=$(dotnet msbuild "$PROJECT" -nologo -target:MinVer -getProperty:MinVerVersion | tail -n 1 | tr -d '\r' | xargs)
VERSION=${VERSION%%+*}
if [[ ! "$VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+ ]]; then
  echo "Unable to resolve MinVer version, got: $VERSION" >&2
  exit 1
fi

desktop-file-validate "$ROOT/packaging/linux/avapdf.desktop"
# KDE service menus use Type=Service and KDE-specific keys, which the generic
# desktop-file-validate utility incorrectly rejects as an Application entry.
# 菜单矩阵与 docs/SHELL_MENU.md 保持一致：提取文本 / 合并 / 转 PDF / 更多工具。
for SERVICE_MENU in avapdf-pdf.desktop avapdf-pdf-merge.desktop avapdf-convert.desktop; do
  grep -Fqx 'Type=Service' "$ROOT/packaging/linux/$SERVICE_MENU"
  grep -Fqx 'ServiceTypes=KonqPopupMenu/Plugin' "$ROOT/packaging/linux/$SERVICE_MENU"
done
grep -Fqx 'MimeType=application/pdf;' "$ROOT/packaging/linux/avapdf-pdf.desktop"
grep -Fqx 'Actions=AvaPDFExtractText;AvaPDFMoreTools' "$ROOT/packaging/linux/avapdf-pdf.desktop"
grep -Fqx '[Desktop Action AvaPDFExtractText]' "$ROOT/packaging/linux/avapdf-pdf.desktop"
grep -Fqx 'Actions=AvaPDFMerge;AvaPDFMoreTools' "$ROOT/packaging/linux/avapdf-pdf-merge.desktop"
grep -Fqx '[Desktop Action AvaPDFConvertToPdf]' "$ROOT/packaging/linux/avapdf-convert.desktop"

# DEB payload
mkdir -p "$DEB_ROOT/opt/avapdf" "$DEB_ROOT/usr/bin" "$DEB_ROOT/DEBIAN"
mkdir -p "$DEB_ROOT/usr/share/applications" "$DEB_ROOT/usr/share/kio/servicemenus"
mkdir -p "$DEB_ROOT/usr/share/icons/hicolor/512x512/apps"
cp -a "$PUBLISH_DIR"/. "$DEB_ROOT/opt/avapdf/"
find "$DEB_ROOT/opt/avapdf" -type f \( -name '*.pdb' -o -name '*.dbg' \) -delete
cat > "$DEB_ROOT/usr/bin/avapdf" <<'EOF'
#!/bin/sh
exec /opt/avapdf/AvaPDF.Desktop "$@"
EOF
chmod 755 "$DEB_ROOT/usr/bin/avapdf"
cp "$ROOT/packaging/linux/avapdf-job" "$DEB_ROOT/usr/bin/avapdf-job"
chmod 755 "$DEB_ROOT/usr/bin/avapdf-job"
cp "$ROOT/packaging/linux/avapdf.desktop" "$DEB_ROOT/usr/share/applications/avapdf.desktop"
cp "$ROOT/packaging/linux/avapdf-pdf.desktop" "$DEB_ROOT/usr/share/kio/servicemenus/avapdf-pdf.desktop"
cp "$ROOT/packaging/linux/avapdf-pdf-merge.desktop" "$DEB_ROOT/usr/share/kio/servicemenus/avapdf-pdf-merge.desktop"
cp "$ROOT/packaging/linux/avapdf-convert.desktop" "$DEB_ROOT/usr/share/kio/servicemenus/avapdf-convert.desktop"
cp "$ROOT/src/AvaPDF.Desktop/Assets/AppIcon.png" "$DEB_ROOT/usr/share/icons/hicolor/512x512/apps/avapdf.png"

# 其他主流文件管理器的右键集成（均为可选增强，缺依赖时菜单不出现但不影响主程序）
mkdir -p "$DEB_ROOT/usr/share/nautilus-python/extensions"   # GNOME / Nautilus
mkdir -p "$DEB_ROOT/usr/share/caja-python/extensions"       # MATE / Caja
mkdir -p "$DEB_ROOT/usr/share/nemo/actions"                 # Cinnamon / Nemo
mkdir -p "$DEB_ROOT/usr/share/avapdf/thunar"                # XFCE / Thunar（模板，需用户合并）
cp "$ROOT/packaging/linux/avapdf-nautilus.py" "$DEB_ROOT/usr/share/nautilus-python/extensions/avapdf.py"
cp "$ROOT/packaging/linux/avapdf-nautilus.py" "$DEB_ROOT/usr/share/caja-python/extensions/avapdf.py"
cp "$ROOT"/packaging/linux/avapdf-*.nemo_action "$DEB_ROOT/usr/share/nemo/actions/"
cp "$ROOT/packaging/linux/thunar-uca.xml" "$DEB_ROOT/usr/share/avapdf/thunar/uca.xml"
chmod 644 "$DEB_ROOT/usr/share/nautilus-python/extensions/avapdf.py" \
          "$DEB_ROOT/usr/share/caja-python/extensions/avapdf.py" \
          "$DEB_ROOT"/usr/share/nemo/actions/avapdf-*.nemo_action \
          "$DEB_ROOT/usr/share/avapdf/thunar/uca.xml"

mapfile -t NATIVE_FILES < <(find "$DEB_ROOT/opt/avapdf" -type f \( -name '*.so' -o -name '*.so.*' \))
DEPENDENCIES='libc6'
if (( ${#NATIVE_FILES[@]} > 0 )); then
  DETECTED=$(dpkg-shlibdeps -O "${NATIVE_FILES[@]}" 2>/dev/null | sed -n 's/^shlibs:Depends=//p' | tail -n 1 || true)
  if [[ -n "$DETECTED" ]]; then
    DEPENDENCIES="$DETECTED"
  fi
fi
INSTALLED_SIZE=$(du -sk "$DEB_ROOT" | cut -f1)
cat > "$DEB_ROOT/DEBIAN/control" <<EOF
Package: avapdf
Version: $VERSION
Architecture: amd64
Maintainer: AvaPDF <dev@avatools.local>
Installed-Size: $INSTALLED_SIZE
Depends: $DEPENDENCIES
Recommends: fonts-noto-cjk, fonts-droid-fallback
Suggests: python3, python3-nautilus, caja-python
Priority: optional
Section: utils
Description: Local PDF workspace
 AvaPDF provides local PDF page tools, document conversion, watermarks,
 metadata editing and password protection without Microsoft Office.
EOF

DEB_PATH="$PACKAGE_DIR/AvaPDF-$VERSION-linux-x64.deb"
dpkg-deb --build --root-owner-group "$DEB_ROOT" "$DEB_PATH"

# AppImage payload
mkdir -p "$APPDIR/usr/bin"
cp -a "$PUBLISH_DIR"/. "$APPDIR/usr/bin/"
find "$APPDIR/usr/bin" -type f \( -name '*.pdb' -o -name '*.dbg' \) -delete
cat > "$APPDIR/AppRun" <<'EOF'
#!/bin/sh
HERE=$(dirname "$(readlink -f "$0")")
exec "$HERE/usr/bin/AvaPDF.Desktop" "$@"
EOF
chmod 755 "$APPDIR/AppRun"
cp "$ROOT/packaging/linux/avapdf.desktop" "$APPDIR/avapdf.desktop"
cp "$ROOT/src/AvaPDF.Desktop/Assets/AppIcon.png" "$APPDIR/avapdf.png"
APPIMAGE_PATH="$PACKAGE_DIR/AvaPDF-$VERSION-linux-x64.AppImage"
appimagetool --appimage-extract-and-run "$APPDIR" "$APPIMAGE_PATH"
chmod 755 "$APPIMAGE_PATH"

for PACKAGE in "$DEB_PATH" "$APPIMAGE_PATH"; do
  BYTES=$(stat -c '%s' "$PACKAGE")
  MIB=$(awk "BEGIN { printf \"%.2f\", $BYTES / 1048576 }")
  printf '%s\t%s bytes\t%s MiB\n' "$PACKAGE" "$BYTES" "$MIB"
done

if [[ -n "${GITHUB_STEP_SUMMARY:-}" ]]; then
  {
    echo "## AvaPDF Linux $VERSION packages"
    echo
    echo '| Package | MiB |'
    echo '| --- | ---: |'
    for PACKAGE in "$DEB_PATH" "$APPIMAGE_PATH"; do
      BYTES=$(stat -c '%s' "$PACKAGE")
      MIB=$(awk "BEGIN { printf \"%.2f\", $BYTES / 1048576 }")
      echo "| $(basename "$PACKAGE") | $MIB |"
    done
  } >> "$GITHUB_STEP_SUMMARY"
fi

printf 'AVAPDF_VERSION=%s\n' "$VERSION"
printf 'AVAPDF_DEB=%s\n' "$DEB_PATH"
printf 'AVAPDF_APPIMAGE=%s\n' "$APPIMAGE_PATH"
