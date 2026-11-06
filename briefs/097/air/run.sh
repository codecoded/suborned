#!/bin/zsh
# brief 097 on the MacBook Air: 0.2.253 (the round's start) vs the 097 final build, 1920x1080 windowed, GPU and frame ms at play zoom and in third person,
# then the final build with every 097 switch off (the look's own cost). Run from this folder; results in ./res-<run>/.
set -u; K=${0:A:h}; cd $K
state() { echo "$1 $(date +%T) load $(sysctl -n vm.loadavg) | $(pmset -g batt | head -1 | sed 's/Now drawing from //') | $(pmset -g therm 2>/dev/null | grep -i speed_limit | tr -s ' ')" | tee -a machine.log; }
run() { local id=$1 app=$2; shift 2; mkdir -p $K/res-$id; sed "s#AIROUT#$K/res-$id#g" $K/perf-air.resolved.json > $K/res-$id/rows.json
  state $id-start; "$app/Contents/MacOS/The Suborned" -screen-fullscreen 0 -screen-width 1920 -screen-height 1080 -shots $K/res-$id/rows.json $K/res-$id -novsync -fastExit "$@" -logFile $K/res-$id/player.log >/dev/null 2>&1; echo "$id rc $?" | tee -a machine.log; state $id-end; }
xattr -dr com.apple.quarantine base/"The Suborned.app" final/"The Suborned.app" 2>/dev/null
run warm   final/"The Suborned.app"   # first launch: the shader warm-up, not counted
run base1  base/"The Suborned.app"
run final1 final/"The Suborned.app"
run off1   final/"The Suborned.app" -nofx -noclouds -norooms -nowind -novariation -noao
run base2  base/"The Suborned.app"
run final2 final/"The Suborned.app"
echo done | tee -a machine.log
