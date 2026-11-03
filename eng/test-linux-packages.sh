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
desktop-file-validate /usr/share/applications/avapdf.desktop
desktop-file-validate /usr/share/kio/servicemenus/avapdf-pdf.desktop
verify_gui_window "$(mktemp)" /usr/bin/avapdf --tool optimize

echo 'DEB install, desktop integration and GUI startup verified.'
"${SUDO[@]}" apt-get remove -y avapdf
hash -r
[[ ! -e /opt/avapdf/AvaPDF.Desktop ]]
[[ ! -e /usr/share/applications/avapdf.desktop ]]
[[ ! -e /usr/share/kio/servicemenus/avapdf-pdf.desktop ]]
echo 'DEB uninstall cleanup verified.'

chmod +x "$APPIMAGE"
verify_gui_window "$(mktemp)" "$APPIMAGE" --appimage-extract-and-run --tool optimize
echo 'AppImage GUI startup verified.'
