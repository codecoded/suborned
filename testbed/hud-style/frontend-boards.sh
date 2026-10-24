#!/bin/sh
# Brief 051 direction A "light line": every board, 1920x1080 and 2880x1864 (the Air), plus the 2x crop checks and the contrast report.
# Needs `bun run hud-style` up (port 5175). Writes to briefs/051/lightline/. About 2 minutes.
cd "$(dirname "$0")/../.." || exit 1
OUT=briefs/051/lightline; S="bun testbed/hud-style/frontend-shot.ts"; mkdir -p $OUT out/scratch/glyph/contrast
shot() { name=$1; hash=$2; for r in "1920 1080" "2880 1864"; do set -- $r; $S "$hash&w=$1&h=$2&static=1" "$OUT/$name-$1x$2.png"; done; }
shot menu "s=menu&f=2"
shot menu-nosave "s=menu-nosave&f=0"
shot options-display "s=options&tab=Display&f=3"
shot options-audio "s=options&tab=Audio&f=1"
shot options-camera "s=options&tab=Camera&f=1"
shot options-gameplay "s=options&tab=Gameplay&f=1"
shot options-from-pause "s=options-pause&tab=Display&f=3"
shot controls-browse "s=controls&mode=browse&f=7"
shot controls-listen "s=controls&mode=listen&f=11"
shot controls-clash "s=controls&mode=clash&f=11"
shot brief "s=brief"
shot pause "s=pause&f=2"
for b in words rowsboard timing; do $S "s=$b&w=1920&h=1080&static=1" "$OUT/board-$b-1920x1080.png"; done
# 2x crops (device px = 2 x ref) of the parts a 1x board hides: clipping, centring, padding
C=$OUT/crop2x
$S "s=menu&f=2&w=1920&h=1080&static=1" $C-menu-stack.png 2 "40,680,420,330"
$S "s=options&tab=Display&f=3&w=1920&h=1080&static=1" $C-options-head-rows.png 2 "480,150,960,380"
$S "s=options&tab=Display&f=3&w=1920&h=1080&static=1" $C-options-foot.png 2 "480,820,960,110"
$S "s=options&tab=Gameplay&f=1&w=1920&h=1080&static=1" $C-options-gameplay.png 2 "480,320,960,360"
$S "s=controls&mode=clash&f=11&w=1920&h=1080&static=1" $C-controls-clash.png 2 "480,690,960,200"
$S "s=controls&mode=listen&f=11&w=1920&h=1080&static=1" $C-controls-keys.png 2 "480,320,960,200"
$S "s=pause&f=2&w=1920&h=1080&static=1" $C-pause-card.png 2 "770,120,380,480"
$S "s=brief&w=1920&h=1080&static=1" $C-brief-continue.png 2 "1560,940,360,110"
# contrast: every text over the busiest 5 % of the pixels under it
for spec in "menu&f=2" "options&tab=Display&f=3" "options&tab=Gameplay&f=1" "controls&mode=clash&f=11" "brief" "pause&f=2"; do
  n=$(echo $spec | tr '&=' '__')
  for r in "1920 1080" "2880 1864"; do set -- $r
    $S "s=$spec&w=$1&h=$2&static=1&bare=1" out/scratch/glyph/contrast/$n-$1.png 1 "" out/scratch/glyph/contrast/$n-$1.json
    echo "== $spec $1x$2"; python3 -I testbed/hud-style/frontend-contrast.py out/scratch/glyph/contrast/$n-$1.png out/scratch/glyph/contrast/$n-$1.json
  done
done > $OUT/contrast-report.txt
echo done
