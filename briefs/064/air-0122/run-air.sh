#!/bin/zsh
# The Suborned 0.2.122 on the MacBook Air: the perf rows the Studio can't prove (briefs 048, 064).
# Needs "The Suborned.app" in ~/Downloads (or pass its folder as $1). Plug in power, close other apps, leave the Air alone ~10 min.
set -u
KIT=${0:A:h}
APPDIR=${1:-$HOME/Downloads}
APP="$APPDIR/The Suborned.app"; EXE="$APP/Contents/MacOS/The Suborned"
[[ -x $EXE ]] || { echo "no app at $APP: unzip TheSuborned-0.2.122.zip there first (ditto -x -k <zip> ~/Downloads)"; exit 1; }
OUT=$HOME/Downloads/air-perf-0122-$(date +%Y%m%d-%H%M); mkdir -p $OUT/tplong $OUT/perf064 $OUT/late
xattr -dr com.apple.quarantine "$APP" 2>/dev/null
# the resolved rows point at the Studio's paths: point them here
sed -e "s#/Users/sky/Projects/suborned/out/batch/tplong#$OUT/tplong#" $KIT/tplong.resolved.json > $OUT/tplong.resolved.json
sed -e "s#/Users/sky/Projects/suborned/out/batch/late#$OUT/late#" -e "s#/Users/sky/Projects/suborned/contracts/missions/handover/routes#$KIT#" $KIT/late.resolved.json > $OUT/late.resolved.json
state() { echo "$1 $(date +%T) load $(sysctl -n vm.loadavg) | $(pmset -g batt | head -1 | sed 's/Now drawing from //') | therm $(pmset -g therm 2>/dev/null | grep -i speed_limit | tr -s ' ')" | tee -a $OUT/machine.log; }
{ sw_vers; sysctl -n hw.model machdep.cpu.brand_string hw.memsize; system_profiler SPDisplaysDataType 2>/dev/null | grep -E 'Resolution|Chipset|Cores'; pmset -g | grep -i -E 'lowpower|powermode'; } > $OUT/machine.txt
pmset -g batt | grep -q 'AC Power' || echo "WARNING: on battery; plug in for a fair run" | tee -a $OUT/machine.log
pmset -g | grep -q -E 'lowpowermode +1' && echo "WARNING: Low Power Mode is on; turn it off" | tee -a $OUT/machine.log
echo "Running three passes (~10 min). The game will open and close on its own; don't touch the Air."
state tplong-start;  "$EXE" -tplong $OUT/tplong.resolved.json $OUT/tplong -novsync -logFile $OUT/tplong/player.log >/dev/null 2>&1; echo "tplong rc $?" | tee -a $OUT/machine.log; state tplong-end
state perf064-start; "$EXE" -perf064 $OUT/perf064 -capture-size 2880x1864 -novsync -perf-warm 30 -perf-seconds 20 -logFile $OUT/perf064/player.log >/dev/null 2>&1; echo "perf064 rc $?" | tee -a $OUT/machine.log; state perf064-end
state late-start;    "$EXE" -late $OUT/late.resolved.json $OUT/late -novsync -logFile $OUT/late/player.log >/dev/null 2>&1; echo "late rc $?" | tee -a $OUT/machine.log; state late-end
# a second tp-long, warm, to see the spread
mkdir -p $OUT/tplong-2; sed -e "s#$OUT/tplong\"#$OUT/tplong-2\"#" $OUT/tplong.resolved.json > $OUT/tplong-2.resolved.json
state tplong2-start; "$EXE" -tplong $OUT/tplong-2.resolved.json $OUT/tplong-2 -novsync -logFile $OUT/tplong-2/player.log >/dev/null 2>&1; echo "tplong-2 rc $?" | tee -a $OUT/machine.log; state tplong2-end
cd ${OUT:h} && ditto -c -k --keepParent ${OUT:t} ${OUT:t}.zip
TS=/Applications/Tailscale.app/Contents/MacOS/Tailscale; [[ -x $TS ]] || TS=tailscale
if $TS file cp ${OUT:t}.zip skys-mac-studio: ; then echo "Done: results sent to the Studio by Taildrop (${OUT:t}.zip)."
else echo "Done, but Taildrop failed: send ~/Downloads/${OUT:t}.zip to skys-mac-studio by hand."; fi
