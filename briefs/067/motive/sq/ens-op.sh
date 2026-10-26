#!/bin/zsh
# Motive brief 067 square probe: the per-operative jitter ensemble (JMODE=op: each operative's first step and clock-pinned steps 0-2 s late,
# 8 samples, sample 0 as composed) for the square's fight plans, on one site variant, at the new pace with K3. From the repo root:
#   bun run guard zsh briefs/067/motive/sq/ens-op.sh <site name> [plan ...]       (site name: out/scratch/motive/067sq/site.<name>.json)
# Rule probes: TREE=tree2 K3=2 ENVX="PROBE_SQ=7" TAGX=-r7 (tree2 carries the PROBE_SQ switches and PROBE_K3=2, the press on a held car only).
set -e
cd /Users/sky/Projects/suborned
name=$1; shift; plans=($@); (( ${#plans} )) || plans=(ra-h3 route-a-board route-a-pressure-board route-g route-g-fast-board rgk-h3 rgkb-h3 loud-x4)
env RB="op-run-speed-cm-s=360 run-alert=0 carry-untrained-cm-s=90 mod-running=375" PROBE_OPWALK=180 PROBE_RUNCLOCK=2 PROBE_K3=${K3:-1} ${=ENVX} \
  SITE=out/scratch/motive/067sq/site.$name.json JMODE=op TAG=op-$name${TAGX} bun briefs/067/motive/sq/ens.ts 0,1,2,3,4,5,6,7 $plans
