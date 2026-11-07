#!/bin/zsh
# usage: trace.sh <dev-build-dir> <out.graphicsstate>   brief 098 traced variants (Splice): a -scratchDev player (build.sh with EXTRA=-scratchDev) run
# through HitchTrace096 in both orders (map at four yaws, third person and the drive, a conversation cutaway), each run's GraphicsStateCollection
# appended to <out> (ShaderPrewarm.TraceSave). Logs under $OUT/trace/. The default trace the traced mode reads: briefs/098/splice/traced/*.graphicsstate.
R=/Users/sky/Projects/suborned; OUTFILE=${2:A}; OUT=${OUT:-$R/out/scratch/splice}; APP="${1:A}/The Suborned.app"; mkdir -p $OUT/trace
for order in third convo; do
  rm -rf $OUT/trace/$order; mkdir -p $OUT/trace/$order
  "$APP/Contents/MacOS/The Suborned" -screen-fullscreen 0 -screen-width 1920 -screen-height 1080 -novsync -hitchTrace $OUT/trace/$order -hitchOrder $order -gscTrace $OUTFILE -logFile $OUT/trace/$order/player.log &
  pid=$!; for i in {1..300}; do kill -0 $pid 2>/dev/null || break; sleep 1; done
  kill -0 $pid 2>/dev/null && { echo "$order: still running after 300 s; stopping pid $pid"; kill $pid; }
  grep -E "ShaderPrewarm: trace|HitchTrace096: written" $OUT/trace/$order/player.log | cut -c1-220
done
