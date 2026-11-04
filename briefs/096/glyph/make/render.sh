#!/bin/sh
# Glyph, brief 096: render each mock's HTML to a PNG at 2x with headless Chrome (no browser pane), then a 1x copy.
cd "$(dirname "$0")"
CH="/Applications/Google Chrome.app/Contents/MacOS/Google Chrome"
for f in "$@"; do n="${f%.html}"
  "$CH" --headless=new --disable-gpu --hide-scrollbars --allow-file-access-from-files --force-device-scale-factor=2 --window-size=1920,1080 --virtual-time-budget=3000 --screenshot="$PWD/out/$n@2x.png" "file://$PWD/$f" >/dev/null 2>&1
  python3 -I -c "from PIL import Image; im=Image.open('out/$n@2x.png'); im.resize((1920,1080), Image.LANCZOS).save('../$n.png')"
  echo "$n done"
done
