#!/bin/zsh
# Motive brief 067: the start-delay ensembles for the pace candidates, one after another (each writes the scratch tree's rulebook).
#   bun run guard zsh briefs/067/motive/ens-all.sh [tag …]   (no tags: all five)
cd /Users/sky/Projects/suborned
K=0,0.4,0.8,1.2,1.6,2,3
R=(route-a route-a-board route-a-pressure route-a-pressure-board route-a-disable route-b route-b-board route-g route-g-board route-g-fast route-g-fast-board route-g-kill route-g-kill-board loud)
typeset -A RBV ENV
RBV=(base "" r0c2 "run-alert=0" w160 "op-run-speed-cm-s=320 run-alert=0 carry-untrained-cm-s=80" w180 "op-run-speed-cm-s=360 run-alert=0 carry-untrained-cm-s=90" w200 "op-run-speed-cm-s=400 run-alert=0 carry-untrained-cm-s=100" w180r1 "op-run-speed-cm-s=360 carry-untrained-cm-s=90")
ENV=(base "" r0c2 "PROBE_RUNCLOCK=2" w160 "PROBE_OPWALK=160 PROBE_RUNCLOCK=2" w180 "PROBE_OPWALK=180 PROBE_RUNCLOCK=2" w200 "PROBE_OPWALK=200 PROBE_RUNCLOCK=2" w180r1 "PROBE_OPWALK=180")
tags=("$@"); (( ${#tags} )) || tags=(base r0c2 w160 w180 w200)
for t in $tags; do
  echo "== $t $(date +%H:%M:%S)"
  env ${=ENV[$t]} RB="${RBV[$t]}" TAG=$t bun briefs/067/motive/ens.ts $K $R 2>&1 | tail -1
done
echo "== done $(date +%H:%M:%S)"
