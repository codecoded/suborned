#!/bin/zsh
# brief 106 on the MacBook Air (Sky's OK 2026-11-11, this measurement only): one -tplong launch, full screen as it comes, vsync off, every row
# sets GPU occlusion, Mesh LOD and STP (rows.json). Run from this folder; results in ./res/. The machine's state goes to machine.log at the start and end.
set -u; K=${0:A:h}; cd $K
state() { echo "$1 $(date +%T) load $(sysctl -n vm.loadavg) | $(pmset -g batt | head -1 | sed 's/Now drawing from //') | therm $(pmset -g therm 2>/dev/null | grep -i -E 'speed_limit|thermal' | tr -s ' ' | tr '\n' ' ')" | tee -a machine.log; }
APP="$K/The Suborned.app"; xattr -dr com.apple.quarantine "$APP" 2>/dev/null
rm -rf res; mkdir -p res; sed "s#AIROUT#$K/res#g" rows.template.json > res/rows.json
state start
"$APP/Contents/MacOS/The Suborned" -screen-fullscreen 1 -capture-native -tplong res/rows.json res -novsync -logFile res/player.log >/dev/null 2>&1
echo "rc $?" | tee -a machine.log
state end
