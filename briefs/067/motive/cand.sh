#!/bin/zsh
# Motive brief 067 (pace and punishment, Sky playing 0.2.122): the candidate route set and the mission check on the scratch probe at engine
# 0.33.1 (d4361806) with the probe switches in engine-probe.patch. Run mkengine.sh first, then from the repo root under the guard:
#   bun run guard zsh briefs/067/motive/cand.sh <tag> "<value=n …>" [ENV=… …]
# MAP="route-a=ra-h3 route-b=rb-st" puts 067's re-compositions (briefs/067/motive/plans) under the reference ids.
# e.g. cand.sh base ""   ·   cand.sh w180 "op-run-speed-cm-s=360 run-alert=0 carry-untrained-cm-s=90" PROBE_OPWALK=180 PROBE_RUNCLOCK=2
# The route set is c037's plans with Motive's 059 focus-fire re-compositions under the reference ids (balance-rulings.json M059-B7; Waymark's
# c038 list), as 059's cand.sh. Output: briefs/067/motive/results/cand-<tag>.txt
set -e
cd /Users/sky/Projects/suborned
S=out/scratch/motive/067; T=$S/tree/contracts/missions/handover/routes; tag=$1; vals=$2; shift 2
python3 -I briefs/067/motive/rb.py $S/tree/contracts/rules/rulebook.orig.json $S/tree/contracts/rules/rulebook.json ${=vals}
[[ -d $S/plans-c038 ]] || cp -R $T/plans $S/plans-c038 # the tree's plans as mkengine left them (c037 + 059's balance plans), restored before every run
rm -rf $T/plans && cp -R $S/plans-c038 $T/plans
typeset -A M; M=(route-g rg-ff route-g-board rgb-ff route-g-fast rgf-ff route-g-fast-board rgfb-ff route-b rb-ff route-b-board rbb-ff loud loud-x4 careless-reporter-not-stopped crns-ff careless-scout-idles-pickup csip-ff careless-g-disable cgd-ff)
for f in $S/routes0/contracts/missions/handover/routes/*.json; do [[ $(basename $f) == routes.json ]] || cp $f $T/; done # every script from HEAD (c037) first: a plan that won't compose plays HEAD's script, flagged (stale) by table.py
for kv in ${=MAP}; do M[${kv%%=*}]=${kv#*=}; done # MAP="route-a=ra-h3 …": 067's re-compositions under the reference ids
for id src in ${(kv)M}; do cp $S/plans/$src.plan.json $T/plans/$id.plan.json; done
{ echo "# tag $tag · values: ${vals:-as committed} · env: $*"
  env "$@" bun $S/tree/tools/missions/check/cli.ts compose handover --engine tree --inputs tree --jobs 8 2>&1 | grep -v "^· " | tail -60
  env "$@" bun $S/tree/tools/missions/check/cli.ts handover --engine tree --inputs tree 2>&1; } > briefs/067/motive/results/cand-$tag.txt || true
mkdir -p $S/scripts-$tag; cp $T/*.json $S/scripts-$tag/
grep -E '^(ok|FAIL|HOLDS|SHIPS)' briefs/067/motive/results/cand-$tag.txt
