#!/bin/zsh
# The Suborned on the MacBook Air: brief 075 Perf A/B, the old-built town against our district (Redline, 2026-10-29).
# SPEC: NEEDS SKY'S OK (Aster asks); NOT RUN ANYWHERE. Base: briefs/072/redline/run-air-072.sh (controls, cold gate, traces).
# What it measures, warm, uncapped, 1920x1080 windowed (GDD §11's bar), against brief 064 A5 (warm mean <= 13.0 ms, p99 <= 16.7 ms):
#   the as-built warm mean and p99 of both scenes at the play views (perf-075ab.json), and on the old town the measured saving of each cut
#   (crowd off, MSAA off, lean lens, our pipeline). Order A-B-A (ours, old, ours again) so the second "ours" pass gates the machine's drift:
#   the two ours passes must agree within 3 % at cell-open-30, or the old pass is labelled "drift" and not held against the budget.
# Kit folder beside this script (Latch builds and resolves it; briefs/075/notes.md "Perf A/B", item B):
#   perf-075ab.ours.resolved.json, perf-075ab.old.resolved.json (PerfBuild's resolved plans, one per player; old foci at plan y + 206)
#   and in ~/Downloads (or $1): "The Suborned Perf Ours.app" and "The Suborned Perf OldTown.app", development players from ONE commit.
# Copy to the Air by scp (sky@Skys-MacBook-Air.local, -o HostKeyAlias=skys-macbook-air-1), run over SSH; fetch the zip by scp. Not Taildrop.
# Before: plug in, battery full, Low Power Mode off, every app quit, lid open on a hard desk, the Air left alone (~60 min).
set -u
SIZE=1920x1080
KIT=${0:A:h}
APPDIR=${1:-$HOME/Downloads}
OURS="$APPDIR/The Suborned Perf Ours.app"; OLD="$APPDIR/The Suborned Perf OldTown.app"
exe() { local m=("$1"/Contents/MacOS/*(N)); (( ${#m} == 1 )) && [[ -x ${m[1]} ]] && echo ${m[1]}; }  # the one executable (named by productName, not the .app)
for a in $OURS $OLD; do [[ -n $(exe "$a") ]] || { echo "no player at $a (unzip with ditto -x -k <zip> $APPDIR)"; exit 1; }; done
for f in perf-075ab.ours.resolved.json perf-075ab.old.resolved.json; do [[ -f $KIT/$f ]] || { echo "kit file $f missing beside this script"; exit 1; }; done
# JsonUtility drops unknown keys: an old-town player without the 075ab switches would run every cut as a build against itself.
for k in oldCrowd msaa oldLens pipeline; do grep -q "\"$k\" *:" $KIT/perf-075ab.old.resolved.json || { echo "old plan lacks $k: re-resolve it from a player that has the switch"; exit 1; }; done
grep -q '"scene" *: *"Assets/Scenes/OldTown.unity"' $KIT/perf-075ab.old.resolved.json || { echo "old plan was not resolved on OldTown.unity"; exit 1; }
# the old town is measured through its play camera (OldTownPlayLook; Latch 2026-10-29), never the study camera: a plan resolved before the switch is refused
grep -q '"camera" *: *"Play camera (our play look)"' $KIT/perf-075ab.old.resolved.json || { echo "old plan does not use the play camera: re-resolve it (PerfBuild -perfScene Assets/Scenes/OldTown.unity -perfSpec briefs/075/redline/perf-075ab.json -perfResolveOnly)"; exit 1; }
grep -q '"scene" *: *"Assets/Scenes/HarbourDistrict.unity"' $KIT/perf-075ab.ours.resolved.json || { echo "ours plan was not resolved on HarbourDistrict.unity"; exit 1; }
OUT=$HOME/Downloads/air-075ab-$(date +%Y%m%d-%H%M); mkdir -p $OUT/ours1 $OUT/old $OUT/ours2
LOG=$OUT/machine.log
xattr -dr com.apple.quarantine "$OURS" "$OLD" 2>/dev/null
thermal() { osascript -l JavaScript -e "ObjC.import('Foundation'); \$.NSProcessInfo.processInfo.thermalState" 2>/dev/null || echo -1; }
gputemp() { [[ -s $OUT/macmon.jsonl ]] && tail -1 $OUT/macmon.jsonl | grep -o '"gpu_temp_avg":[0-9.]*' | cut -d: -f2 || echo ""; }
atsize() { local s; s=$(grep -ho '"screen": *\(\[[0-9, ]*\]\|"[0-9]*x[0-9]*"\)' $2 2>/dev/null | sed -E 's/.*[["]([0-9]+)[, x]+([0-9]+).*/\1x\2/' | sort -u | tr '\n' ' '); [[ -z $s ]] && s="none "; echo "$1 measured at: $s" | tee -a $LOG; [[ $s == "$SIZE " ]] || echo "WARNING: $1 not at $SIZE: label its numbers with the size above" | tee -a $LOG; }
stamp() { echo "$1 unix $(date +%s) $(date +%T) load $(sysctl -n vm.loadavg) | $(pmset -g batt | sed -n 2p | tr -s ' \t' ' ') | thermal $(thermal) | gpuTemp $(gputemp)" | tee -a $LOG; }
{ sw_vers; sysctl -n hw.model machdep.cpu.brand_string hw.memsize; pmset -g | grep -i -E 'lowpower|powermode'; } > $OUT/machine.txt
system_profiler SPDisplaysDataType > $OUT/display.txt 2>/dev/null
ps -Ao pcpu,pid,comm -r | head -15 > $OUT/top-start.txt
pmset -g batt | grep -q 'AC Power' || echo "WARNING: on battery; plug in for a fair run" | tee -a $LOG
pmset -g batt | grep -q -E '; charging;|finishing charge' && echo "WARNING: the battery is charging; let it finish" | tee -a $LOG
pmset -g | grep -q -E 'lowpowermode +1' && echo "WARNING: Low Power Mode is on; turn it off" | tee -a $LOG
awk 'NR>1 && $1>20 {print "WARNING: busy before the run: " $3 " at " $1 "% CPU"}' $OUT/top-start.txt | tee -a $LOG
PIDS=()
if command -v macmon >/dev/null 2>&1; then macmon pipe -i 500 > $OUT/macmon.jsonl 2> $OUT/macmon.err & PIDS+=($!)
else echo "WARNING: no macmon: thermal state only" | tee -a $LOG; fi
( while true; do echo "$(date +%s) thermal $(thermal) load $(sysctl -n vm.loadavg)"; sleep 5; done ) > $OUT/thermal.log 2>&1 & PIDS+=($!)
trap 'for p in $PIDS; do kill $p 2>/dev/null; done' EXIT INT TERM
stamp idle-start
for i in {1..40}; do t=$(thermal); g=$(gputemp); [[ $t == 0 && ( -z $g || ${g%.*} -lt 45 ) ]] && break; (( i == 1 )) && echo "Waiting for the Air to cool (thermal $t, GPU ${g:-?} C)..."; sleep 30; done
stamp idle-end
echo "Running the 075 A/B (~60 min). The game opens and closes on its own; don't touch the Air."
# one pass: player, plan, folder, warm-up floor (the first pass warms the machine to a plateau, 600-900 s; later passes keep a 120 s floor)
pass() { local app=$1 plan=$2 dir=$3 minw=$4 bin; bin=$(exe "$1")
  sed -e "s#\"width\": *[0-9]*#\"width\": ${SIZE%x*}#" -e "s#\"height\": *[0-9]*#\"height\": ${SIZE#*x}#" -e "s#\"minSeconds\": *[0-9]*#\"minSeconds\": $minw#" $KIT/$plan > $OUT/$dir/plan.json
  stamp $dir-start; "$bin" -perf $OUT/$dir/plan.json -perfOut $OUT/$dir/result.json -perfShots $OUT/$dir/shots -logFile $OUT/$dir/player.log >/dev/null 2>&1
  echo "$dir rc $?" | tee -a $LOG; stamp $dir-end; atsize $dir $OUT/$dir/result.json; }
pass "$OURS" perf-075ab.ours.resolved.json ours1 600
pass "$OLD"  perf-075ab.old.resolved.json  old   120
# the old player echoes each PerfVariant whole: a missing switch = the player dropped it, so its cut rows are void
for k in oldCrowd msaa oldLens pipeline; do grep -q "\"$k\" *:" $OUT/old/result.json || { echo "STOP: the old player's result does not echo $k: its cut rows are void (as-built rows still stand)" | tee -a $LOG; }; done
pass "$OURS" perf-075ab.ours.resolved.json ours2 120
ps -Ao pcpu,pid,comm -r | head -15 > $OUT/top-end.txt
for p in $PIDS; do kill $p 2>/dev/null; done; PIDS=()
cd ${OUT:h} && ditto -c -k --keepParent ${OUT:t} ${OUT:t}.zip
echo "Done: fetch ~/Downloads/${OUT:t}.zip from the Studio by scp (sky@Skys-MacBook-Air.local, -o HostKeyAlias=skys-macbook-air-1)."
