#!/bin/zsh
# Motive brief 067 square probe: the mission check on the 067sq scratch tree (mk.sh) with a site variant. From the repo root, under the guard:
#   bun run guard zsh briefs/067/motive/sq/cand.sh <tag> <site.json | -> "<value=n …>" [ENV=… …]
# <site.json>: the site to play (- keeps the tree's). Values over the committed rulebook (rb.py); ENV: the probe switches (engine-probe.patch).
# MAP="id=plan …" puts plans from out/scratch/motive/067sq/plans under the reference ids (default: brief 067's c038 candidates below).
# Output: briefs/067/motive/sq/results/cand-<tag>.txt, and the composed scripts in out/scratch/motive/067sq/scripts-<tag>/.
set -e
cd /Users/sky/Projects/suborned
S=out/scratch/motive/067sq; TR=${TREE:-tree}; T=$S/$TR/contracts/missions/handover/routes; tag=$1; site=$2; vals=$3; shift 3 # TREE=tree2|tree3: the rule-probe engines
mkdir -p briefs/067/motive/sq/results
[[ $site == - ]] || cp $site $S/$TR/sim/data/handover.site.json
python3 -I briefs/067/motive/rb.py $S/$TR/contracts/rules/rulebook.orig.json $S/$TR/contracts/rules/rulebook.json ${=vals}
[[ -d $S/plans-c038 ]] || cp -R $T/plans $S/plans-c038
rm -rf $T/plans && cp -R $S/plans-c038 $T/plans
typeset -A M; M=(route-g rg-ff route-g-board rgb-ff route-g-fast rgf-ff route-g-fast-board rgfb-ff route-b rb-ff route-b-board rbb-ff loud loud-x4 careless-reporter-not-stopped crns-ff careless-scout-idles-pickup csip-ff careless-g-disable cgd-ff)
M+=(route-a ra-h3 route-a-disable rad-h3 route-b rb-rr route-b-board rbb-rr careless-reporter-not-stopped crns-rr careless-scout-idles-pickup csip-rr route-g-kill-board rgkb-h3) # brief 067's (motive.md §5 Waymark)
for f in $S/routes0/contracts/missions/handover/routes/*.json; do [[ $(basename $f) == routes.json ]] || cp $f $T/; done
for kv in ${=MAP}; do M[${kv%%=*}]=${kv#*=}; done
for id src in ${(kv)M}; do cp $S/plans/$src.plan.json $T/plans/$id.plan.json; done
{ echo "# tag $tag · tree: $TR · site: $site · values: ${vals:-as committed} · env: $* · map: ${MAP:-default}"
  env "$@" bun $S/$TR/tools/missions/check/cli.ts compose handover --engine tree --inputs tree --jobs 8 2>&1 | grep -v "^· " | tail -60
  env "$@" bun $S/$TR/tools/missions/check/cli.ts handover --engine tree --inputs tree 2>&1; } > briefs/067/motive/sq/results/cand-$tag.txt || true
rm -rf $S/scripts-$tag; mkdir -p $S/scripts-$tag; cp $T/*.json $S/scripts-$tag/
grep -E '^(ok|FAIL|HOLDS|SHIPS)' briefs/067/motive/sq/results/cand-$tag.txt
