#!/bin/zsh
# Motive brief 067 (pace and punishment; Keel's 0.33.1 items): rebuild the scratch probe from engine 0.33.1 (commit d4361806) and these files.
# Run from the repo root under the guard:
#   bun run guard zsh briefs/067/motive/mkengine.sh
# Then: cand.sh (the mission check on the candidate route set), ens-all.sh / ens-op.sh (the ensembles), probe.ts (single plans).
# The engine is suborned.sim/0.33.1 plus engine-probe.patch: environment switches only, every switch unset = 0.33.1 byte for byte:
#   PROBE_OPWALK=<cm/s>  the cell's own walk (M067-B1); cell.speed stays the city's pace (the summoned partner, reporters, the crowd, the rounds)
#   PROBE_RUNCLOCK=2     a run in a clock-keeping watcher's sight lifts its clock to the noticed mark, then +1 s a second (M067-B2); 1 = +1 s a
#                        second only, 3 = +2 s a second (not used)
#   PROBE_K3=1           M067-K3: the rivals walk to a car nobody guards from outside, the cell down fails, Go needs a standing op aboard
# The rulebook values (op-run-speed-cm-s, run-alert, carry-untrained-cm-s, mod-running) are set per run by rb.py over rulebook.orig.json.
# The route set is c037's plans with Motive's 059 focus-fire re-compositions (briefs/059/motive/balance/probe-plans) under the reference
# ids, and 059's routes.json (briefs/059/motive/balance/plans): Waymark's c038 list, as 059's cand.sh. Scratch: out/scratch/motive/067/.
set -e
B=briefs/067/motive; S=out/scratch/motive/067
rm -rf $S/tree $S/tree2 $S/routes0 && mkdir -p $S/tree $S/plans $S/routes0
git archive d4361806 sim contracts tools runs/missions/handover GDD package.json bunfig.toml tsconfig.json bun.lock briefs/027/presets briefs/027/keel | tar -x -C $S/tree
git archive d4361806 contracts/missions/handover/routes | tar -x -C $S/routes0
ln -sfn "$PWD/node_modules" $S/tree/node_modules
patch -s -p1 -d $S/tree < $B/engine-probe.patch
cp $S/tree/contracts/rules/rulebook.json $S/tree/contracts/rules/rulebook.orig.json
for f in briefs/059/motive/balance/probe-plans/*.plan.json; do cp $f $S/plans/; done
cp briefs/059/motive/balance/plans/routes.json $S/tree/contracts/missions/handover/routes/routes.json
for f in briefs/059/motive/balance/plans/*.plan.json; do python3 -I -c "import json,sys; p=json.load(open(sys.argv[1])); [s.pop('why',None) for s in p['steps']]; json.dump(p,open(sys.argv[2],'w'),indent=1)" $f $S/tree/contracts/missions/handover/routes/plans/$(basename $f); done
for f in $B/plans/*.plan.json(N); do cp $f $S/plans/; done # 067's variants (ra-h2, ra-h3, rb-st, rbb-st)
(cd $S/tree/sim && dotnet build cli/cli.csproj -c Release -o ../out/missions/engine/tree/bin -v q -nologo)
# tree2: the same engine for single plays and the fight lab, with HEAD's route scripts (c037) and the committed rulebook
rsync -a --exclude 'out/missions/plays' --exclude node_modules $S/tree/ $S/tree2/ && ln -sfn "$PWD/node_modules" $S/tree2/node_modules
for f in $S/routes0/contracts/missions/handover/routes/*.json; do [[ $(basename $f) == routes.json ]] || cp $f $S/tree2/contracts/missions/handover/routes/; done
echo "probe ready in $S (tree: compose and checks; tree2: single plays, fight lab)"
