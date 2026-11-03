#!/bin/zsh
# Motive 2026-10-26 (yard lane ruling; copy of briefs/059/motive/balance/cand.sh, RBB=<plan> picks route-b-board): the candidate route set and the mission check on the scratch probe (run after mkengine.sh, from the repo root):
#   bun run guard zsh briefs/059/motive/balance/cand.sh <tag> [extra env, e.g. PROBE_PLACES=svc-ambush-1:7735:-800]
# Swaps in the focus-fire re-compositions under the reference ids (scratch tree only), composes every route on the probe with the GDD-as-written
# switches (R: FIXPOS GATE LATE FLOOR=3 LANE), then runs the mission check. Output: out/scratch/motive/059c/cand-<tag>.txt
set -e
cd /Users/sky/Projects/suborned
S=out/scratch/motive/059c; T=$S/tree/contracts/missions/handover/routes/plans; tag=$1; shift
typeset -A M; M=(route-g rg-ff route-g-board rgb-ff route-g-fast rgf-ff route-g-fast-board rgfb-ff route-b rb-ff route-b-board ${RBB:-rbb-ff} loud loud-x4 careless-reporter-not-stopped crns-ff careless-scout-idles-pickup csip-ff careless-g-disable cgd-ff)
for id src in ${(kv)M}; do cp $S/plans/$src.plan.json $T/$id.plan.json; done
R=(PROBE_FIXPOS=1 PROBE_GATE=1 PROBE_LATE=1 PROBE_FLOOR=3 PROBE_LANE=1 "$@")
{ env $R bun $S/tree/tools/missions/check/cli.ts compose handover --engine tree --inputs tree --jobs 8 2>&1 | tail -45
  env $R bun $S/tree/tools/missions/check/cli.ts handover --engine tree --inputs tree 2>&1; } > $S/cand-$tag.txt || true
grep -E '^(ok|FAIL|HOLDS|SHIPS)' $S/cand-$tag.txt
