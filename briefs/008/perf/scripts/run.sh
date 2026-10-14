#!/bin/zsh
# B6 (Latch): one perf run on the Air. Usage (repo root): briefs/008/perf/scripts/run.sh <name> [player args, e.g. -perfStrict -perfPass ab]
# Needs out/build/perf/SubornedPerf.app and out/build/perf/plan.json (PerfBuild, Suborned/Perf/Build Perf Player). Writes out/perf/<name>/.
set -e
R=out/perf/$1; shift
mkdir -p $R/shots
snap() { date -u; pmset -g batt; pmset -g therm; ps -Ao %cpu,comm -r | head -8; }
snap > $R/before.txt
( while true; do { date -u +%H:%M:%S; ps -Ao pcpu,comm -r | sed -n 2,5p | cut -c1-90; } >> $R/load.txt; sleep 60; done ) &
LOAD=$!
caffeinate -di open -W -n out/build/perf/SubornedPerf.app --args -perf "$PWD/out/build/perf/plan.json" -perfOut "$PWD/$R/run.json" -perfShots "$PWD/$R/shots" -logFile "$PWD/$R/player.log" "$@"
kill $LOAD
snap > $R/after.txt
python3 briefs/008/perf/scripts/analyse.py $R/run.json $R/load.txt | tee $R/table.txt
