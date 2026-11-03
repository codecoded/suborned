#!/bin/zsh
# The Suborned 0.2.122 on the MacBook Air: the perf rows the Studio can't prove (briefs 048, 064).
# Needs "The Suborned.app" in ~/Downloads (or pass its folder as $1). Plug in power, close other apps, leave the Air alone ~10 min.
# The bar is 60 fps at 1920x1080 (Sky, 2026-10-26: "1080p probably ok"; GDD §11). Every pass asks for a 1920x1080 window: the player's harnesses
# size a windowed 1920x1080 drawable in pixels (CaptureSize.Apply; a Retina window of 960x540 points), never fullscreen. Render scale is left as the
# build ships it (Sky's look call). 0.2.122's -perf064 ignores -capture-size and sets 2880x1864 itself (Perf064.cs): its row is checked and labelled below.
set -u
SIZE=1920x1080
KIT=${0:A:h}
APPDIR=${1:-$HOME/Downloads}
APP="$APPDIR/The Suborned.app"; EXE="$APP/Contents/MacOS/The Suborned"
[[ -x $EXE ]] || { echo "no app at $APP: unzip TheSuborned-0.2.122.zip there first (ditto -x -k <zip> ~/Downloads)"; exit 1; }
OUT=$HOME/Downloads/air-perf-0122-$(date +%Y%m%d-%H%M); mkdir -p $OUT/tplong $OUT/perf064 $OUT/late
xattr -dr com.apple.quarantine "$APP" 2>/dev/null
# the resolved rows point at the Studio's paths: point them here
# and every row at 1920x1080 (the 0.2.122 kit's tp-long rows say 2880x1864; late's already say 1920x1080)
AT=(-e "s/\"w\": *[0-9][0-9]*/\"w\": ${SIZE%x*}/" -e "s/\"h\": *[0-9][0-9]*/\"h\": ${SIZE#*x}/")
sed -e "s#/Users/sky/Projects/suborned/out/batch/tplong#$OUT/tplong#" $AT $KIT/tplong.resolved.json > $OUT/tplong.resolved.json
sed -e "s#/Users/sky/Projects/suborned/out/batch/late#$OUT/late#" -e "s#/Users/sky/Projects/suborned/contracts/missions/handover/routes#$KIT#" $AT $KIT/late.resolved.json > $OUT/late.resolved.json
# the size each pass really ran at, from its own JSON ("screen"): a pass not at $SIZE is logged, never silently mixed in
atsize() { local s; s=$(grep -ho '"screen": *\(\[[0-9, ]*\]\|"[0-9]*x[0-9]*"\)' $2 2>/dev/null | sed -E 's/.*[["]([0-9]+)[, x]+([0-9]+).*/\1x\2/' | sort -u | tr '\n' ' '); [[ -z $s ]] && s="none "; echo "$1 measured at: $s" | tee -a $OUT/machine.log; [[ $s == "$SIZE " ]] || echo "WARNING: $1 not at $SIZE: label its numbers with the size above" | tee -a $OUT/machine.log; }
state() { echo "$1 $(date +%T) load $(sysctl -n vm.loadavg) | $(pmset -g batt | head -1 | sed 's/Now drawing from //') | therm $(pmset -g therm 2>/dev/null | grep -i speed_limit | tr -s ' ')" | tee -a $OUT/machine.log; }
{ sw_vers; sysctl -n hw.model machdep.cpu.brand_string hw.memsize; system_profiler SPDisplaysDataType 2>/dev/null | grep -E 'Resolution|Chipset|Cores'; pmset -g | grep -i -E 'lowpower|powermode'; } > $OUT/machine.txt
pmset -g batt | grep -q 'AC Power' || echo "WARNING: on battery; plug in for a fair run" | tee -a $OUT/machine.log
pmset -g | grep -q -E 'lowpowermode +1' && echo "WARNING: Low Power Mode is on; turn it off" | tee -a $OUT/machine.log
echo "Running three passes (~10 min). The game will open and close on its own; don't touch the Air."
state tplong-start;  "$EXE" -tplong $OUT/tplong.resolved.json $OUT/tplong -novsync -logFile $OUT/tplong/player.log >/dev/null 2>&1; echo "tplong rc $?" | tee -a $OUT/machine.log; state tplong-end; atsize tplong $OUT/tplong/tplong.json
state perf064-start; "$EXE" -perf064 $OUT/perf064 -capture-size $SIZE -novsync -perf-warm 30 -perf-seconds 20 -logFile $OUT/perf064/player.log >/dev/null 2>&1; echo "perf064 rc $?" | tee -a $OUT/machine.log; state perf064-end; atsize perf064 $OUT/perf064/perf064.json
state late-start;    "$EXE" -late $OUT/late.resolved.json $OUT/late -novsync -logFile $OUT/late/player.log >/dev/null 2>&1; echo "late rc $?" | tee -a $OUT/machine.log; state late-end; atsize late $OUT/late/late.json
# a second tp-long, warm, to see the spread
mkdir -p $OUT/tplong-2; sed -e "s#$OUT/tplong\"#$OUT/tplong-2\"#" $OUT/tplong.resolved.json > $OUT/tplong-2.resolved.json
state tplong2-start; "$EXE" -tplong $OUT/tplong-2.resolved.json $OUT/tplong-2 -novsync -logFile $OUT/tplong-2/player.log >/dev/null 2>&1; echo "tplong-2 rc $?" | tee -a $OUT/machine.log; state tplong2-end; atsize tplong-2 $OUT/tplong-2/tplong.json
cd ${OUT:h} && ditto -c -k --keepParent ${OUT:t} ${OUT:t}.zip
TS=/Applications/Tailscale.app/Contents/MacOS/Tailscale; [[ -x $TS ]] || TS=tailscale
if $TS file cp ${OUT:t}.zip skys-mac-studio: ; then echo "Done: results sent to the Studio by Taildrop (${OUT:t}.zip)."
else echo "Done, but Taildrop failed: send ~/Downloads/${OUT:t}.zip to skys-mac-studio by hand."; fi
