#!/bin/zsh
# run.sh <app> <outdir> <cold|warm> [extra player args...]: one player run; cold moves the app's Metal cache aside first.
set -e
APP="$1"; OUT="$2"; MODE="$3"; shift 3
C="$(getconf DARWIN_USER_CACHE_DIR)com.Unity-Technologies.com.unity.template.urp-blank"
BAK=/Users/sky/Projects/suborned/out/scratch/latch/prewarm/metal-bak
mkdir -p "$OUT" "$BAK"
if [[ "$MODE" == cold ]]; then
  for d in com.apple.metal com.apple.gpuarchiver com.apple.metalfe; do [[ -e "$C/$d" ]] && { rm -rf "$BAK/$d"; mv "$C/$d" "$BAK/$d"; }; done
fi
echo "cache: $(du -sh "$C" 2>/dev/null | cut -f1)"
"$APP/Contents/MacOS/The Suborned" -screen-fullscreen 0 -screen-width 1920 -screen-height 1080 -novsync "$@" -logFile "$OUT/player.log" &
PID=$!; echo "pid $PID"
for i in {1..240}; do kill -0 $PID 2>/dev/null || break; sleep 1; done
kill -0 $PID 2>/dev/null && { echo "timeout; killing $PID"; kill $PID; }
echo "cache after: $(du -sh "$C" 2>/dev/null | cut -f1)"
