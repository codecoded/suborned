#!/bin/zsh
# The Suborned on the MacBook Air: brief 072's one sitting (Redline, 072-D, 2026-10-27; phase 3 2026-10-28). SPEC: NEEDS SKY'S OK; NOT RUN ANYWHERE.
# PRECONDITION (Sky 2026-10-27, "yeah wiat"): ask for this only once ONE development player holds 064 A3's no-look items 1 (SSAO off for the play
# camera), 5 (depth texture by request, the sun flare off at play) and 7 (the signals overlay camera as a pass), every face with the holed L2 shadow
# copies (F1, a5234fdb), 072-F with its surfaceDetail switch (79283d9f), 072-G, and after the Studio CPU check (phase2.md F5) has passed.
# What it measures, warm, uncapped, 1920x1080 (GDD §11's bar), in that player: the as-shipped warm mean and p99 at the opening against 13.0 / 16.7 ms
# (brief 064 A5; the first warm 1080 number, T0 included); every face's price (every-flat - today); 072-F's fetch price (every - every-flat; the
# switch is a global float, so the extra variants' SetPass and interpolators are on both sides and not in this delta); A3 item 1's saving
# (every-ssao - every, only if item 1 lets the perf switch win); the same at street level (tp-street, tp-worst; the holed L2s draw there); and the
# missed-vblank rate as shipped (capped072). Verdict rules: briefs/072/redline/plan.md 4.3, phase2.md 4.3 and 5, phase3.md 4.
# BRIEF 075 (Redline, 2026-10-28; briefs/075/redline.md): the player must also hold 074-H and 075-A/B/C1-2 (old sky, DK Ocean, play bloom, road
# sheet with its detail pair, T-K1, liveries). ab072 gains wide-lamplight-30 and harbour-quay-30 (~+5 min). surfaceDetail now also switches the road's
# detail. The 075 switches (sky, ocean, colourways, playBloom; liveries not recommended) do not exist in PerfVariant / TpLongRun yet: they are
# specified in briefs/075/redline/perf-075-pending.json and merged into the kit only once Latch lands them; the guard below refuses a kit that names
# one when the player's result does not echo it.
# Base: briefs/064/air-0122/soak-air.sh (controls, cold gate, traces) and run-air.sh (rows, sizes). Kit folder beside this script:
# perf-072.resolved.json (PerfBuild's resolved plan), tplong-072.resolved.json (rows with the player's paths), and "The Suborned.app" in ~/Downloads (or $1).
# Copy to the Air by scp (sky@Skys-MacBook-Air.local, -o HostKeyAlias=skys-macbook-air-1), run over SSH; fetch the zip by scp. Not Taildrop.
# Before: plug in, battery full, Low Power Mode off, every app quit, lid open on a hard desk, the Air left alone (~40 min).
# Optional: ROOM_C=22 ./run-air-072.sh   (macmon, if Sky has allowed `brew install macmon`, traces the GPU clock; else thermal state only)
set -u
SIZE=1920x1080
KIT=${0:A:h}
APPDIR=${1:-$HOME/Downloads}
APP="$APPDIR/The Suborned.app"; EXE="$APP/Contents/MacOS/The Suborned"
[[ -x $EXE ]] || { echo "no app at $APP: unzip the development build there first (ditto -x -k <zip> ~/Downloads)"; exit 1; }
for f in perf-072.resolved.json tplong-072.resolved.json; do [[ -f $KIT/$f ]] || { echo "kit file $f missing beside this script"; exit 1; }; done
# JsonUtility drops unknown keys: an editor or player without surfaceDetail would run the texture A/B as a build against itself. Every variant of the
# resolved plan and every tplong row must carry it (at least 4 variants, 8 rows), or nothing runs.
nv=$(grep -c '"surfaceDetail"' $KIT/perf-072.resolved.json); nr=$(grep -c '"surfaceDetail"' $KIT/tplong-072.resolved.json)
(( nv >= 4 && nr >= 8 )) || { echo "resolved kit lacks surfaceDetail (plan $nv of >= 4, tplong $nr of 8): rebuild the kit from a player and editor that have it (79283d9f)"; exit 1; }
OUT=$HOME/Downloads/air-072-$(date +%Y%m%d-%H%M); mkdir -p $OUT/perf $OUT/tplong
LOG=$OUT/machine.log
# brief 075: which pending 075 switches this kit names (none until Latch adds them; the player must then echo each, checked after the perf pass)
want075=(); for k in sky ocean colourways playBloom liveries; do grep -q "\"$k\" *:" $KIT/perf-072.resolved.json $KIT/tplong-072.resolved.json && want075+=($k); done
(( ${#want075} )) && echo "075 switches in this kit: $want075" | tee -a $LOG
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
echo "Running brief 072's sitting (~40 min). The game opens and closes on its own; don't touch the Air."
# 1. the perf harness: its own warm-up to a plateau (600-900 s), then ab072 (8 stops x 4 variants: today, every-flat, every, every-ssao; 3 rotated repeats, +1) and capped072 (today, every)
sed -e "s#\"width\": *[0-9]*#\"width\": ${SIZE%x*}#" -e "s#\"height\": *[0-9]*#\"height\": ${SIZE#*x}#" $KIT/perf-072.resolved.json > $OUT/perf-072.resolved.json
stamp perf-start; "$EXE" -perf $OUT/perf-072.resolved.json -perfOut $OUT/perf/perf-072.result.json -perfShots $OUT/perf/shots -logFile $OUT/perf/player.log >/dev/null 2>&1; echo "perf rc $?" | tee -a $LOG; stamp perf-end; atsize perf $OUT/perf/perf-072.result.json
# the player echoes its PerfVariant whole in the result: no surfaceDetail there = the player dropped it, so the texture rows below would be void too
grep -q '"surfaceDetail": *"off"' $OUT/perf/perf-072.result.json || { echo "STOP: the player's result has no surfaceDetail off: this player predates the switch; the run is void" | tee -a $LOG; exit 1; }
grep -o '"ssaoAsBuilt": *"[^"]*"' $OUT/perf/perf-072.result.json | tee -a $LOG
for k in $want075; do grep -q "\"$k\" *:" $OUT/perf/perf-072.result.json || { echo "STOP: the kit names the 075 switch $k but the player's result does not echo it: the player predates it; the run is void" | tee -a $LOG; exit 1; }; done
# 2. warm straight after: the tp-long rows, today / every / every-flat interleaved, uncapped
AT=(-e "s/\"w\": *[0-9][0-9]*/\"w\": ${SIZE%x*}/" -e "s/\"h\": *[0-9][0-9]*/\"h\": ${SIZE#*x}/")
sed -e "s#/Users/sky/Projects/suborned/out/batch/tplong#$OUT/tplong#" $AT $KIT/tplong-072.resolved.json > $OUT/tplong-072.resolved.json
stamp tplong-start; "$EXE" -tplong $OUT/tplong-072.resolved.json $OUT/tplong -novsync -logFile $OUT/tplong/player.log >/dev/null 2>&1; echo "tplong rc $?" | tee -a $LOG; stamp tplong-end; atsize tplong $OUT/tplong/tplong.json
ps -Ao pcpu,pid,comm -r | head -15 > $OUT/top-end.txt
for p in $PIDS; do kill $p 2>/dev/null; done; PIDS=()
cd ${OUT:h} && ditto -c -k --keepParent ${OUT:t} ${OUT:t}.zip
echo "Done: fetch ~/Downloads/${OUT:t}.zip from the Studio by scp (sky@Skys-MacBook-Air.local, -o HostKeyAlias=skys-macbook-air-1)."
