#!/bin/zsh
# Motive 059 balance pass (2026-10-26): the verdict probes on the scratch probe (after mkengine.sh), from the repo root:
#   bun run guard zsh briefs/059/motive/balance/verdicts.sh > briefs/059/motive/balance/results/verdicts-2026-10-26.txt
cd /Users/sky/Projects/suborned
S=out/scratch/motive/059c; R=(PROBE_FIXPOS=1 PROBE_GATE=1 PROBE_LATE=1 PROBE_FLOOR=3 PROBE_LANE=1); C5=PROBE_PLACES=svc-ambush-1:7735:-800
run() { local tag=$1; shift; local -a ex; while [[ $1 == PROBE_* ]]; do ex+=$1; shift; done; env $R $ex TAG=$tag bun $S/probe.ts "$@" 2>&1 | grep -v '^\$' | sort -k2,2; }
echo "# R = FIXPOS GATE LATE FLOOR=3 LANE (GDD §06/§07 as written); c5 = svc-ambush-1 at (77.35, -8.0)"
echo "## Q1 loud against the carry"; run R loud-x4 loud-yard route-a route-a-pressure rg-ff rb-ff
echo "## Q2 the early quiet routes (square and yard), the leak twins with the gate off"; run R route-a-pressure rgf-ff rgfb-ff apy-1 gfy-1 apy-leak gfy-leak; run nogate PROBE_GATE=0 apy-leak gfy-leak
echo "## Q3 route B at the yard: at will and focus fire, today's svc-ambush-1 and c5"; run R route-b route-b-board rb-ff rbb-ff rbb-ff3; run c5 $C5 route-b route-b-board rb-ff rbb-ff rbb-ff3
echo "## rejected switches, for the record (not rules): SHIELD and the first-down break"; run Rshield PROBE_SHIELD=1 route-g-board route-b; run Rdb1 PROBE_DRAWNBREAK=1 loud
