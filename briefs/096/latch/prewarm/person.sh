#!/bin/zsh
# person.sh <app> <outlog> <cold|warm> <wait-pattern>: a person's launch (screen flags only: the front end, the loading bar, the menu); stopped by PID once the log shows the pattern.
APP="$1"; OUT="$2"; MODE="$3"; PAT="$4"; shift 4
C="$(getconf DARWIN_USER_CACHE_DIR)com.Unity-Technologies.com.unity.template.urp-blank"; BAK=/Users/sky/Projects/suborned/out/scratch/latch/prewarm/metal-bak
LOG="$HOME/Library/Logs/DefaultCompany/The Suborned/Player.log"
if [[ "$MODE" == cold ]]; then for d in com.apple.metal com.apple.gpuarchiver com.apple.metalfe; do [[ -e "$C/$d" ]] && { rm -rf "$BAK/$d"; mv "$C/$d" "$BAK/$d"; }; done; fi
rm -f "$LOG"; T0=$(date +%s.%N 2>/dev/null || python3 -c 'import time;print(time.time())')
"$APP/Contents/MacOS/The Suborned" -screen-fullscreen 0 -screen-width 1920 -screen-height 1080 "$@" &
PID=$!; echo "pid $PID"
for i in {1..90}; do sleep 1; rg -q "$PAT" "$LOG" 2>/dev/null && break; done
sleep 3; kill $PID; sleep 2; cp "$LOG" "$OUT"
