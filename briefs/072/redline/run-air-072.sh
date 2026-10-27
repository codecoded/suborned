#!/bin/zsh
# The Suborned on the MacBook Air: the every-face A/B (Redline, brief 072-D, 2026-10-27). SPEC: NEEDS SKY'S OK; NOT RUN ANYWHERE.
# One question: what does DecoKit on every face cost at play zoom, warm, at 1920x1080 (GDD §11's bar), and does the opening fit
# 13.0 ms mean / 16.7 ms p99 (brief 064 A5) with SSAO off for the play camera? Verdict rules: briefs/072/redline/plan.md 4.3 and 5.
# Needs a DEVELOPMENT build (the perf harness runs only there) with every face built in and Latch's switches (perf-072.json's note):
# PerfStop.yaw, PerfVariant.everyFace / moduleShadows, the tplong row `variant`. Base: briefs/064/air-0122/soak-air.sh (controls, cold
# gate, traces) and run-air.sh (rows, sizes). Kit folder beside this script: perf-072.resolved.json (PerfBuild's resolved plan),
# tplong-072.resolved.json (rows with the player's paths), and "The Suborned.app" in ~/Downloads (or $1).
# Copy to the Air by scp (sky@Skys-MacBook-Air.local, -o HostKeyAlias=skys-macbook-air-1), run over SSH; fetch the zip by scp. Not Taildrop.
# Before: plug in, battery full, Low Power Mode off, every app quit, lid open on a hard desk, the Air left alone (~35 min).
# Optional: ROOM_C=22 ./run-air-072.sh   (macmon, if Sky has allowed `brew install macmon`, traces the GPU clock; else thermal state only)
set -u
SIZE=1920x1080
KIT=${0:A:h}
APPDIR=${1:-$HOME/Downloads}
APP="$APPDIR/The Suborned.app"; EXE="$APP/Contents/MacOS/The Suborned"
[[ -x $EXE ]] || { echo "no app at $APP: unzip the development build there first (ditto -x -k <zip> ~/Downloads)"; exit 1; }
for f in perf-072.resolved.json tplong-072.resolved.json; do [[ -f $KIT/$f ]] || { echo "kit file $f missing beside this script"; exit 1; }; done
OUT=$HOME/Downloads/air-072-$(date +%Y%m%d-%H%M); mkdir -p $OUT/perf $OUT/tplong
LOG=$OUT/machine.log
xattr -dr com.apple.quarantine "$APP" 2>/dev/null
thermal() { osascript -l JavaScript -e "ObjC.import('Foundation'); \$.NSProcessInfo.processInfo.thermalState" 2>/dev/null || echo -1; }
gputemp() { [[ -s $OUT/macmon.jsonl ]] && tail -1 $OUT/macmon.jsonl | grep -o '"gpu_temp_avg":[0-9.]*' | cut -d: -f2 || echo ""; }
# the size each pass really ran at, from its own JSON ("screen"): a pass not at $SIZE is logged and labelled, never mixed in silently
atsize() { local s; s=$(grep -ho '"screen": *\(\[[0-9, ]*\]\|"[0-9]*x[0-9]*"\)' $2 2>/dev/null | sed -E 's/.*[["]([0-9]+)[, x]+([0-9]+).*/\1x\2/' | sort -u | tr '\n' ' '); [[ -z $s ]] && s="none "; echo "$1 measured at: $s" | tee -a $LOG; [[ $s == "$SIZE " ]] || echo "WARNING: $1 not at $SIZE: label its numbers with the size above" | tee -a $LOG; }
stamp() { echo "$1 unix $(date +%s) $(date +%T) load $(sysctl -n vm.loadavg) | $(pmset -g batt | sed -n 2p | tr -s ' \t' ' ') | thermal $(thermal) | gpuTemp $(gputemp)" | tee -a $LOG; }
# --- the machine, before anything runs
{ sw_vers; sysctl -n hw.model machdep.cpu.brand_string hw.memsize; pmset -g | grep -i -E 'lowpower|powermode'; echo "room ${ROOM_C:-not given} C"; } > $OUT/machine.txt
system_profiler SPDisplaysDataType > $OUT/display.txt 2>/dev/null
ps -Ao pcpu,pid,comm -r | head -15 > $OUT/top-start.txt
pmset -g batt | grep -q 'AC Power' || echo "WARNING: on battery; plug in for a fair run" | tee -a $LOG
pmset -g batt | grep -q -E '; charging;|finishing charge' && echo "WARNING: the battery is charging; let it finish (charging heats the chassis)" | tee -a $LOG
pmset -g | grep -q -E 'lowpowermode +1' && echo "WARNING: Low Power Mode is on; turn it off" | tee -a $LOG
awk 'NR>1 && $1>20 {print "WARNING: busy before the run: " $3 " at " $1 "% CPU"}' $OUT/top-start.txt | tee -a $LOG
# --- traces for the whole run (killed by PID at the end)
PIDS=()
if command -v macmon >/dev/null 2>&1; then macmon pipe -i 500 > $OUT/macmon.jsonl 2> $OUT/macmon.err & PIDS+=($!)
else echo "WARNING: no macmon: GPU clock not traced, thermal state only (the clock-flat noise gate cannot be read)" | tee -a $LOG; fi
( while true; do echo "$(date +%s) thermal $(thermal) load $(sysctl -n vm.loadavg)"; sleep 5; done ) > $OUT/thermal.log 2>&1 & PIDS+=($!)
trap 'for p in $PIDS; do kill $p 2>/dev/null; done' EXIT INT TERM
# --- cold gate: thermal state 0 and (with macmon) the GPU under 45 C, every 30 s for up to 20 min
stamp idle-start
for i in {1..40}; do
  t=$(thermal); g=$(gputemp)
  if [[ $t == 0 && ( -z $g || ${g%.*} -lt 45 ) ]]; then break; fi
  (( i == 1 )) && echo "Waiting for the Air to cool (thermal $t, GPU ${g:-?} C)..."
  sleep 30
done
stamp idle-end
echo "Running the every-face A/B (~35 min). The game opens and closes on its own; don't touch the Air."
# 1. the perf harness: its own warm-up to a plateau (600-900 s), then ab072 (6 stops x 4 variants x 3 rotated repeats, +1) and capped072
sed -e "s#\"width\": *[0-9]*#\"width\": ${SIZE%x*}#" -e "s#\"height\": *[0-9]*#\"height\": ${SIZE#*x}#" $KIT/perf-072.resolved.json > $OUT/perf-072.resolved.json
stamp perf-start; "$EXE" -perf $OUT/perf-072.resolved.json -perfOut $OUT/perf/perf-072.result.json -perfShots $OUT/perf/shots -logFile $OUT/perf/player.log >/dev/null 2>&1; echo "perf rc $?" | tee -a $LOG; stamp perf-end; atsize perf $OUT/perf/perf-072.result.json
# 2. warm straight after: the tp-long rows, today and every interleaved, uncapped
AT=(-e "s/\"w\": *[0-9][0-9]*/\"w\": ${SIZE%x*}/" -e "s/\"h\": *[0-9][0-9]*/\"h\": ${SIZE#*x}/")
sed -e "s#/Users/sky/Projects/suborned/out/batch/tplong#$OUT/tplong#" $AT $KIT/tplong-072.resolved.json > $OUT/tplong-072.resolved.json
stamp tplong-start; "$EXE" -tplong $OUT/tplong-072.resolved.json $OUT/tplong -novsync -logFile $OUT/tplong/player.log >/dev/null 2>&1; echo "tplong rc $?" | tee -a $LOG; stamp tplong-end; atsize tplong $OUT/tplong/tplong.json
ps -Ao pcpu,pid,comm -r | head -15 > $OUT/top-end.txt
for p in $PIDS; do kill $p 2>/dev/null; done; PIDS=()
cd ${OUT:h} && ditto -c -k --keepParent ${OUT:t} ${OUT:t}.zip
echo "Done: fetch ~/Downloads/${OUT:t}.zip from the Studio by scp (sky@Skys-MacBook-Air.local, -o HostKeyAlias=skys-macbook-air-1)."
