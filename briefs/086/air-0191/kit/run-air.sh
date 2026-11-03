#!/bin/zsh
# The Air sitting of brief 086 (Redline's plan §1.3): the build beside this kit (ROOT, the kit's parent: ~/Downloads/suborned-air-0195 holds 0.2.195). Started over ssh, under caffeinate; stops itself. Closes and kills nothing it did not start.
# Base: briefs/064/air-0122/soak-air.sh. Its Quit-this-app steps are gone: nothing of Sky's is touched. ./run-air.sh [--check]: --check runs the gate and one short launch only.
set -u
HERE=${0:A:h}; ROOT=${HERE:h}; APP="$ROOT/The Suborned.app"; EXE="$APP/Contents/MacOS/The Suborned"; MM=$HOME/.local/suborned-air/macmon/macmon
OUT=$ROOT/run-$(date +%Y%m%d-%H%M); mkdir -p $OUT; LOG=$OUT/machine.log; PRESETS="$ROOT/Suborned Camera Presets"; mkdir -p "$PRESETS"
for z in 8 10 14 18.5; do print -r -- "{\"name\":\"z${z%.*}\",\"overhead\":{\"zoom\":$z}}" > "$PRESETS/z${z%.*}.json"; done
print "$OUT" > $ROOT/last-run.txt
thermal() { osascript -l JavaScript -e "ObjC.import('Foundation'); \$.NSProcessInfo.processInfo.thermalState" 2>/dev/null || echo -1; }
mmlast() { tail -1 $OUT/macmon.jsonl 2>/dev/null | grep -oE "\"$1\":[0-9.]+" | cut -d: -f2; }
others() { ps -Ao pcpu,pid,comm -r | sed 1d | grep -v -E "spotlightknowledged|macmon|sshd|The Suborned" | head -1 | awk '{print $1}'; }
stamp() { print -r -- "$1 unix $(date +%s) $(date +%T) load $(sysctl -n vm.loadavg) | $(pmset -g batt | sed -n 2p | tr -s ' \t' ' ') | thermal $(thermal) | cpuTemp $(mmlast cpu_temp_avg) gpuTemp $(mmlast gpu_temp_avg) gpuMHz $(mmlast gpu_freq_mhz) gpuW $(mmlast gpu_power)" | tee -a $LOG; }
{ sw_vers; sysctl -n hw.model machdep.cpu.brand_string hw.memsize; pmset -g | grep -i -E 'lowpower|powermode'; pmset -g batt; } > $OUT/machine.txt
system_profiler SPDisplaysDataType > $OUT/display.txt 2>/dev/null
ps -Ao pcpu,pmem,pid,comm -r | head -20 > $OUT/top-start.txt
pmset -g batt | grep -q 'AC Power' || { print "STOP: on battery" | tee -a $LOG; exit 3; }
pmset -g | grep -qE 'lowpowermode +1' && { print "STOP: Low Power Mode is on" | tee -a $LOG; exit 3; }
PIDS=()
$MM pipe -i 1000 > $OUT/macmon.jsonl 2> $OUT/macmon.err & PIDS+=($!)
( while true; do print -r -- "$(date +%s) thermal $(thermal) load $(sysctl -n vm.loadavg) top $(ps -Ao pcpu,comm -r | sed 1d | head -3 | sed -E 's#[^ ]*/##' | tr -s ' ' | tr '\n' ';')"; sleep 5; done ) > $OUT/thermal.log 2>&1 & PIDS+=($!)
pmset -g thermlog > $OUT/thermlog.txt 2>&1 & PIDS+=($!)
GAMEPID=""
finish() { [[ -n $GAMEPID ]] && kill $GAMEPID 2>/dev/null; for p in $PIDS; do kill $p 2>/dev/null; done; ps -Ao pcpu,pmem,pid,comm -r | head -20 > $OUT/top-end.txt; stamp end; print "$(date +%s)" > $OUT/DONE; }
trap finish EXIT INT TERM
# the gate: up to 30 min for the machine to settle. Spotlight's indexer alone is logged, not waited out past the gate's load.
gate() { local i ok=0; for i in {1..60}; do local l=$(sysctl -n vm.loadavg | awk '{print $2}') t=$(thermal) o=$(others) c=$(mmlast cpu_temp_avg); print -r -- "gate $i load $l thermal $t top-other ${o:-0}% cpuTemp ${c:-?}" >> $LOG
    if [[ $t == 0 ]] && (( l < 5.0 && ${o:-0} < 50.0 && ${c:-0} < 55.0 )); then (( ok++ )); (( ok >= 2 )) && return 0; else ok=0; fi; sleep 30; done; return 1; }
sleep 5; stamp gate-start
gate || { print "STOP: the machine did not settle in 30 min" | tee -a $LOG; exit 4; }
stamp gate-passed
# one launch. $1 name, $2 rows file ('' = the frames proof), $3 preset, $4 render scale %, $5 seconds it may take
launch() { local name=$1 rows=$2 preset=$3 scale=$4 cap=$5; local dir=$OUT/$name; mkdir -p $dir; print -r -- "z${preset%.*}" > "$PRESETS/default.txt"; print -r -- "{\"display\":{\"renderScale\":$scale}}" > $dir/settings.json
  pmset -g batt | grep -q 'AC Power' || { print "STOP: on battery" | tee -a $LOG; exit 3; }
  local args=(-screen-fullscreen 0 -screen-width 1920 -screen-height 1080 -novsync -settings $dir/settings.json -logFile $dir/player.log)
  if [[ $rows == shipped ]]; then args=(-novsync -settings $dir/settings.json -logFile $dir/player.log -perf064 $dir -capture-native -perf-warm 30 -perf-seconds 20); rows=''; elif [[ -n $rows ]]; then args+=(-tplong $HERE/$rows.rows.json $dir); else args+=(-gameProof $dir -proofOnly frames -capture-size 1920x1080); fi
  stamp "$name-start"; open -n -a "$APP" --args $args; local t0=$(date +%s) seen=0 last=""; GAMEPID=""
  for i in {1..30}; do GAMEPID=$(pgrep -f "^$EXE" | head -1); [[ -n $GAMEPID ]] && break; sleep 1; done
  [[ -z $GAMEPID ]] && { print "STOP: $name: no process after open (a dialog?)" | tee -a $LOG; exit 5; }
  [[ -n $rows ]] && last=$(grep -E '"name"' $HERE/$rows.rows.json | tail -1 | sed -E 's/.*"name": *"([^"]+)".*/\1/')
  print -r -- "$name pid $GAMEPID unix $t0" >> $LOG
  while kill -0 $GAMEPID 2>/dev/null; do sleep 1; local now=$(date +%s)
    if [[ -n $rows && -f $dir/tplong.json ]]; then local n=$(grep -cE '"name": *"' $dir/tplong.json); (( n > seen )) && { seen=$n; print -r -- "row-done $name $n unix $now" >> $LOG; }
      grep -qE "\"name\": *\"$last\"" $dir/tplong.json && { sleep 1; kill $GAMEPID; print -r -- "$name: all rows written, the player's picture extras cut, pid $GAMEPID stopped" >> $LOG; sleep 2; break; }
    elif [[ -n $rows ]] && (( now - t0 > 420 )); then kill $GAMEPID; print "STOP: $name: no row in 420 s (a dialog? see player.log)" | tee -a $LOG; exit 5; fi
    (( now - t0 > cap + 300 )) && { kill $GAMEPID; print -r -- "$name: over its $cap s + 300 s of load, stopped" | tee -a $LOG; break; }
  done; while kill -0 $GAMEPID 2>/dev/null; do sleep 1; done; GAMEPID=""; stamp "$name-end"; }
if [[ ${1:-} == --check ]]; then launch check cold-z10 10 100 560; exit 0; fi
launch cold-z10 cold-z10 10 100 240
launch soak-z10 soak-z10 10 100 660
launch warm-z10 warm-z10 10 100 300
launch warm-z8 warm-z8 8 100 150
launch warm-z14 warm-z14 14 100 120
launch warm-z18 warm-z18 18.5 100 120
for k in 100-a 65-a 100-b 65-b; do launch scale-$k scale 10 ${k%-*} 150; done
launch ceiling ceiling 10 100 480
launch oldfar oldfar 10 100 180
# no `gpu` launch: this release player has no GPU recorders (supportsGpuRecorder false, 83 samplers, none a render pass; gpu.rows.json stays for a development build)
launch fsr fsr 10 100 180
launch shipped shipped 10 100 240
launch frames '' 10 100 330
