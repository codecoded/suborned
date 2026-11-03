#!/bin/zsh
# Motive brief 067: the per-operative jitter ensembles (JMODE=op: each operative's own start delay, 0-2 s, from sample k's stream; k=0 is
# the plan as composed). One candidate after another (each writes the scratch tree's rulebook).
#   bun run guard zsh briefs/067/motive/ens-op.sh [tag …]   (no tags: base w160 w180)
cd /Users/sky/Projects/suborned
K=0,1,2,3,4,5,6,7
R=(route-a route-a-board route-a-pressure route-a-pressure-board route-a-disable route-b route-b-board route-g route-g-board route-g-fast route-g-fast-board route-g-kill loud ${=EXTRA})
typeset -A RBV ENV
RBV=(base "" w160 "op-run-speed-cm-s=320 run-alert=0 carry-untrained-cm-s=80" w170 "op-run-speed-cm-s=340 run-alert=0 carry-untrained-cm-s=85" w180 "op-run-speed-cm-s=360 run-alert=0 carry-untrained-cm-s=90" w200 "op-run-speed-cm-s=400 run-alert=0 carry-untrained-cm-s=100" w180m "op-run-speed-cm-s=360 run-alert=0 carry-untrained-cm-s=90 mod-running=375")
ENV=(base "" w160 "PROBE_OPWALK=160 PROBE_RUNCLOCK=2" w170 "PROBE_OPWALK=170 PROBE_RUNCLOCK=2" w180 "PROBE_OPWALK=180 PROBE_RUNCLOCK=2" w200 "PROBE_OPWALK=200 PROBE_RUNCLOCK=2" w180m "PROBE_OPWALK=180 PROBE_RUNCLOCK=2")
tags=("$@"); (( ${#tags} )) || tags=(base w160 w180)
for t in $tags; do
  echo "== op-$t $(date +%H:%M:%S)"
  env ${=ENV[$t]} JMODE=op RB="${RBV[$t]}" TAG=op-$t bun briefs/067/motive/ens.ts $K $R 2>&1 | tail -1
done
echo "== done $(date +%H:%M:%S)"
