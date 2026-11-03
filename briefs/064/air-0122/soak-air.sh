#!/bin/zsh
# The Suborned 0.2.122 on the MacBook Air: the controlled soak (Redline, brief 064, 2026-10-26). Base: run-air.sh, same player, kit and rows.
# One question: is the cold -> warm rise (15 -> 21-23 ms mean at 2880x1864 in the 0.2.122 run, counts identical) the fanless Air throttling its GPU?
# Measured at 1920x1080 (Sky, 2026-10-26: "1080p probably ok" is the 60 fps bar; GDD §11): the tp-long and late rows are rewritten to 1920x1080 here,
# a windowed drawable of 1920x1080 pixels (CaptureSize.Apply), never fullscreen; render scale stays as the build ships it (Sky's look call).
# 0.2.122's -perf064 ignores -capture-size and sets 2880x1864 itself (Perf064.cs): every pass's real size is read back from its JSON and logged.
# Put this file in the kit folder beside run-air.sh (tplong.resolved.json, late.resolved.json, loud-*.json). Needs "The Suborned.app" in ~/Downloads (or $1).
# Trace, no sudo: macmon (GPU clock, GPU power, GPU and CPU temperature, every 0.5 s; `brew install macmon`) and NSProcessInfo.thermalState (0-3) every 5 s.
# Without macmon it still runs, with the thermal state only (coarse). Before: plug in, battery full (charging heats the chassis), Low Power Mode off,
# quit every app (T3 Code, Outlook, Chrome, the VM, Unity Hub), lid open on a hard desk, leave the Air alone for the whole run (~25 min of load).
# Optional: ROOM_C=22 ./soak-air.sh  (the room temperature, logged).
set -u
SIZE=1920x1080
KIT=${0:A:h}
APPDIR=${1:-$HOME/Downloads}
APP="$APPDIR/The Suborned.app"; EXE="$APP/Contents/MacOS/The Suborned"
[[ -x $EXE ]] || { echo "no app at $APP: unzip TheSuborned-0.2.122.zip there first (ditto -x -k <zip> ~/Downloads)"; exit 1; }
for f in tplong.resolved.json late.resolved.json loud-fight.json loud-left-downed.json; do [[ -f $KIT/$f ]] || { echo "kit file $f missing beside this script"; exit 1; }; done
OUT=$HOME/Downloads/air-soak-0122-$(date +%Y%m%d-%H%M); mkdir -p $OUT
LOG=$OUT/machine.log
xattr -dr com.apple.quarantine "$APP" 2>/dev/null

thermal() { osascript -l JavaScript -e "ObjC.import('Foundation'); \$.NSProcessInfo.processInfo.thermalState" 2>/dev/null || echo -1; }
gputemp() { [[ -s $OUT/macmon.jsonl ]] && tail -1 $OUT/macmon.jsonl | grep -o '"gpu_temp_avg":[0-9.]*' | cut -d: -f2 || echo ""; }
# the size each pass really ran at, from its own JSON ("screen"): a pass not at $SIZE is logged, never silently mixed in
atsize() { local s; s=$(grep -ho '"screen": *\(\[[0-9, ]*\]\|"[0-9]*x[0-9]*"\)' $2 2>/dev/null | sed -E 's/.*[["]([0-9]+)[, x]+([0-9]+).*/\1x\2/' | sort -u | tr '\n' ' '); [[ -z $s ]] && s="none "; echo "$1 measured at: $s" | tee -a $LOG; [[ $s == "$SIZE " ]] || echo "WARNING: $1 not at $SIZE: label its numbers with the size above" | tee -a $LOG; }
AT=(-e "s/\"w\": *[0-9][0-9]*/\"w\": ${SIZE%x*}/" -e "s/\"h\": *[0-9][0-9]*/\"h\": ${SIZE#*x}/")  # every row's size -> $SIZE
stamp() { echo "$1 unix $(date +%s) $(date +%T) load $(sysctl -n vm.loadavg) | $(pmset -g batt | sed -n 2p | tr -s ' \t' ' ') | thermal $(thermal) | gpuTemp $(gputemp)" | tee -a $LOG; }

# --- the machine, before anything runs
{ sw_vers; sysctl -n hw.model machdep.cpu.brand_string hw.memsize; pmset -g | grep -i -E 'lowpower|powermode'; echo "room ${ROOM_C:-not given} C"; } > $OUT/machine.txt
system_profiler SPDisplaysDataType > $OUT/display.txt 2>/dev/null   # includes "UI Looks like": the scaled mode decides the surface the game gets
ps -Ao pcpu,pid,comm -r | head -15 > $OUT/top-start.txt
find "$HOME/Library/Application Support" -maxdepth 3 -iname '*suborned*' -type d 2>/dev/null | while read -r d; do find "$d" -maxdepth 1 -iname '*.json' -exec cp {} $OUT/ \; ; done  # the player's own settings (vsync, render scale)
pmset -g batt | grep -q 'AC Power' || echo "WARNING: on battery; plug in for a fair run" | tee -a $LOG
pmset -g batt | grep -q -E '; charging;|finishing charge' && echo "WARNING: the battery is charging; let it finish (charging heats the chassis)" | tee -a $LOG
pmset -g | grep -q -E 'lowpowermode +1' && echo "WARNING: Low Power Mode is on; turn it off" | tee -a $LOG
awk 'NR>1 && $1>20 {print "WARNING: busy before the run: " $3 " at " $1 "% CPU"}' $OUT/top-start.txt | tee -a $LOG

# --- the traces, in the background for the whole run (killed by PID at the end)
PIDS=()
if command -v macmon >/dev/null 2>&1; then macmon pipe -i 500 > $OUT/macmon.jsonl 2> $OUT/macmon.err & PIDS+=($!)
else echo "WARNING: no macmon (brew install macmon): GPU clock and temperature not traced, thermal state only" | tee -a $LOG; fi
( while true; do echo "$(date +%s) thermal $(thermal) load $(sysctl -n vm.loadavg)"; sleep 5; done ) > $OUT/thermal.log 2>&1 & PIDS+=($!)
pmset -g thermlog > $OUT/thermlog.txt 2>&1 & PIDS+=($!)
trap 'for p in $PIDS; do kill $p 2>/dev/null; done' EXIT INT TERM

# --- cold gate: thermal state 0 and (with macmon) the GPU under 45 C, checked every 30 s for up to 20 min
stamp idle-start
for i in {1..40}; do
  t=$(thermal); g=$(gputemp)
  if [[ $t == 0 && ( -z $g || ${g%.*} -lt 45 ) ]]; then break; fi
  (( i == 1 )) && echo "Waiting for the Air to cool (thermal $t, GPU ${g:-?} C)..."
  sleep 30
done
stamp idle-end

tplong() { # $1 name, $2 extra flags ("-novsync" or "" for the shipped vsync)
  mkdir -p $OUT/$1
  sed -e "s#/Users/sky/Projects/suborned/out/batch/tplong#$OUT/$1#" $AT $KIT/tplong.resolved.json > $OUT/$1.resolved.json
  stamp "$1-start"; "$EXE" -tplong $OUT/$1.resolved.json $OUT/$1 ${=2} -logFile $OUT/$1/player.log >/dev/null 2>&1; echo "$1 rc $?" | tee -a $LOG; stamp "$1-end"; atsize $1 $OUT/$1/tplong.json
}

echo "Running the soak (~25 min of load). The game opens and closes on its own; don't touch the Air."
# 1. cold, then 2. the soak: the same three rows (play-opening, tp-street, tp-worst) every ~110 s, back to back, for ~13 min
for n in 01 02 03 04 05 06 07; do tplong tplong-$n -novsync; done
# 3. warm: the other two passes as run-air.sh runs them
mkdir -p $OUT/perf064 $OUT/late
sed -e "s#/Users/sky/Projects/suborned/out/batch/late#$OUT/late#" -e "s#/Users/sky/Projects/suborned/contracts/missions/handover/routes#$KIT#" $AT $KIT/late.resolved.json > $OUT/late.resolved.json
stamp perf064-start; "$EXE" -perf064 $OUT/perf064 -capture-size $SIZE -novsync -perf-warm 30 -perf-seconds 20 -logFile $OUT/perf064/player.log >/dev/null 2>&1; echo "perf064 rc $?" | tee -a $LOG; stamp perf064-end; atsize perf064 $OUT/perf064/perf064.json
stamp late-start; "$EXE" -late $OUT/late.resolved.json $OUT/late -novsync -logFile $OUT/late/player.log >/dev/null 2>&1; echo "late rc $?" | tee -a $LOG; stamp late-end; atsize late $OUT/late/late.json
# 4. warm again (the number players live with, uncapped), then 5. as shipped: vsync on (the player's own settings), and perf064 at the display's own size
#    (-capture-native: not the bar, the display-mode check of review A4; its size is whatever the desktop gives, logged)
tplong tplong-08 -novsync
tplong tplong-09-vsync ""
mkdir -p $OUT/perf064-native
stamp perf064-native-start; "$EXE" -perf064 $OUT/perf064-native -capture-native -perf-warm 30 -perf-seconds 20 -logFile $OUT/perf064-native/player.log >/dev/null 2>&1; echo "perf064-native rc $?" | tee -a $LOG; stamp perf064-native-end; atsize perf064-native $OUT/perf064-native/perf064.json
ps -Ao pcpu,pid,comm -r | head -15 > $OUT/top-end.txt

for p in $PIDS; do kill $p 2>/dev/null; done; PIDS=()
cd ${OUT:h} && ditto -c -k --keepParent ${OUT:t} ${OUT:t}.zip
TS=/Applications/Tailscale.app/Contents/MacOS/Tailscale; [[ -x $TS ]] || TS=tailscale
if $TS file cp ${OUT:t}.zip skys-mac-studio: ; then echo "Done: results sent to the Studio by Taildrop (${OUT:t}.zip)."
else echo "Done, but Taildrop failed: send ~/Downloads/${OUT:t}.zip to skys-mac-studio by hand."; fi
