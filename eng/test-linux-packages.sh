#!/usr/bin/env bash
set -Eeuo pipefail

if (( $# != 2 )); then
  echo "Usage: $0 <package.deb> <package.AppImage>" >&2
  exit 2
fi

DEB=$(realpath "$1")
APPIMAGE=$(realpath "$2")
SUDO=()
if (( EUID != 0 )); then
  SUDO=(sudo)
fi

cleanup() {
  if command -v avapdf >/dev/null 2>&1; then
    "${SUDO[@]}" apt-get remove -y avapdf >/dev/null 2>&1 || true
  fi
}
trap cleanup EXIT

verify_gui_window() {
  local log=$1
  shift
  xvfb-run -a bash -c '
    log=$1
    shift
    "$@" >"$log" 2>&1 &
    app=$!
    for attempt in {1..40}; do
      if xwininfo -root -tree 2>/dev/null | grep -q "AvaPDF"; then
        kill "$app" 2>/dev/null || true
        wait "$app" 2>/dev/null || true
        exit 0
      fi
      if ! kill -0 "$app" 2>/dev/null; then
        cat "$log" >&2
        wait "$app" 2>/dev/null || true
        exit 1
      fi
      sleep 0.5
    done
    cat "$log" >&2
    kill "$app" 2>/dev/null || true
    wait "$app" 2>/dev/null || true
    exit 1
  ' _ "$log" "$@"
}

"${SUDO[@]}" apt-get install -y "$DEB"
hash -r
command -v avapdf >/dev/null
[[ -x /opt/avapdf/AvaPDF.Desktop ]]
[[ -f /usr/share/applications/avapdf.desktop ]]
[[ -f /usr/share/kio/servicemenus/avapdf-pdf.desktop ]]
[[ -f /usr/share/kio/servicemenus/avapdf-pdf-merge.desktop ]]
[[ -f /usr/share/kio/servicemenus/avapdf-convert.desktop ]]
[[ -x /usr/bin/avapdf-job ]]
[[ -f /usr/share/nautilus-python/extensions/avapdf.py ]]
[[ -f /usr/share/caja-python/extensions/avapdf.py ]]
[[ -f /usr/share/nemo/actions/avapdf-extracttext.nemo_action ]]
[[ -f /usr/share/nemo/actions/avapdf-merge.nemo_action ]]
[[ -f /usr/share/nemo/actions/avapdf-convert.nemo_action ]]
[[ -f /usr/share/avapdf/thunar/uca.xml ]]
python3 -m py_compile /usr/share/nautilus-python/extensions/avapdf.py /usr/bin/avapdf-job
desktop-file-validate /usr/share/applications/avapdf.desktop
for SERVICE_MENU in avapdf-pdf.desktop avapdf-pdf-merge.desktop avapdf-convert.desktop; do
  grep -Fqx 'Type=Service' "/usr/share/kio/servicemenus/$SERVICE_MENU"
  grep -Fqx 'ServiceTypes=KonqPopupMenu/Plugin' "/usr/share/kio/servicemenus/$SERVICE_MENU"
done
grep -Fqx 'MimeType=application/pdf;' /usr/share/kio/servicemenus/avapdf-pdf.desktop
grep -Fqx 'Actions=AvaPDFExtractText' /usr/share/kio/servicemenus/avapdf-pdf.desktop
grep -Fqx '[Desktop Action AvaPDFExtractText]' /usr/share/kio/servicemenus/avapdf-pdf.desktop
grep -Fqx 'Actions=AvaPDFMerge' /usr/share/kio/servicemenus/avapdf-pdf-merge.desktop
grep -Fqx '[Desktop Action AvaPDFConvertToPdf]' /usr/share/kio/servicemenus/avapdf-convert.desktop

# avapdf-job 任务协议：生成的任务文件必须位于 <TMPDIR>/AvaPDF/Jobs 且名为 AvaPDF-*.json。
# 用 avapdf 替身避免真实应用立即消费并删除任务文件。
SHIM_DIR=$(mktemp -d)
printf '#!/bin/sh\nexit 0\n' > "$SHIM_DIR/avapdf"
chmod +x "$SHIM_DIR/avapdf"
rm -rf "${TMPDIR:-/tmp}/AvaPDF/Jobs"
PATH="$SHIM_DIR:$PATH" avapdf-job --tool extracttext --auto /tmp/sample.pdf
JOB=$(find "${TMPDIR:-/tmp}/AvaPDF/Jobs" -name 'AvaPDF-*.json' -print -quit 2>/dev/null)
[[ -n "$JOB" ]]
python3 - "$JOB" <<'PYEOF'
import json, sys
job = json.load(open(sys.argv[1], encoding="utf-8"))
assert job["tool"] == "extracttext", job
assert job["autoRun"] is True, job
assert job["files"] == ["/tmp/sample.pdf"], job
PYEOF
verify_gui_window "$(mktemp)" /usr/bin/avapdf --tool optimize

echo 'DEB install, desktop integration and GUI startup verified.'
"${SUDO[@]}" apt-get remove -y avapdf
hash -r
[[ ! -e /opt/avapdf/AvaPDF.Desktop ]]
[[ ! -e /usr/share/applications/avapdf.desktop ]]
[[ ! -e /usr/share/kio/servicemenus/avapdf-pdf.desktop ]]
[[ ! -e /usr/share/kio/servicemenus/avapdf-pdf-merge.desktop ]]
[[ ! -e /usr/share/kio/servicemenus/avapdf-convert.desktop ]]
[[ ! -e /usr/bin/avapdf-job ]]
[[ ! -e /usr/share/nautilus-python/extensions/avapdf.py ]]
[[ ! -e /usr/share/caja-python/extensions/avapdf.py ]]
[[ ! -e /usr/share/nemo/actions/avapdf-extracttext.nemo_action ]]
[[ ! -e /usr/share/nemo/actions/avapdf-merge.nemo_action ]]
[[ ! -e /usr/share/nemo/actions/avapdf-convert.nemo_action ]]
[[ ! -e /usr/share/avapdf/thunar/uca.xml ]]
echo 'DEB uninstall cleanup verified.'

chmod +x "$APPIMAGE"
verify_gui_window "$(mktemp)" "$APPIMAGE" --appimage-extract-and-run --tool optimize
echo 'AppImage GUI startup verified.'
