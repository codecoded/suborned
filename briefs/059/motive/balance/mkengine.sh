#!/bin/zsh
# Motive 059 balance pass on engine 0.32.0 (2026-10-26; first run 2026-10-25 died at the usage limit). Rebuilds the scratch probe from Keel's 059 round
# (commit 100b5e3a) and these files. Run from the repo root under the guard:
#   bun run guard zsh briefs/059/motive/balance/mkengine.sh
# Then the pass's two checks (the candidate route set on the GDD-as-written switches R, today's svc-ambush-1 and the moved one):
#   bun run guard zsh briefs/059/motive/balance/cand.sh r
#   bun run guard zsh briefs/059/motive/balance/cand.sh c5 PROBE_PLACES=svc-ambush-1:7735:-800
# Probes: bun out/scratch/motive/059c/probe.ts <plan id>… with the switches in the environment (TAG names the log); sweep.ts replays composed scripts.
# The engine is suborned.sim/0.32.0 plus engine-probe.patch: env switches only, every switch off = 0.32.0 (the off check reproduces Keel's
# briefs/059/keel/missions-check-0.32.txt failures). Switches: PROBE_FIXPOS (the JoinFight (0,0) fix), PROBE_GATE, PROBE_LATE, PROBE_FLOOR=n (S1),
# PROBE_LANE (a rival blocker keeps its lane), PROBE_DRAWNBREAK=1|2 (1: a drawn crew breaks at its first man down, rejected; 2: a broken crew never
# presses), PROBE_SHIELD (Vale as cover for his carrier, rejected), PROBE_BLKHOLD (a walking blocker holds fire, GDD §06 text), PROBE_CAP, PROBE_YARDLANE=1|2 (the yard's lane from the car along the drive; 1 at the road's width throughout, 2 the drive part at the drive's 3 m: lane ruling 2026-10-26),
# PROBE_PLACES=id:x:y;… (moves a place), PROBE_TRACE. final.sh, matrix.sh and s1demo.sh are the 2026-10-25 run's (SHIELD and DRAWNBREAK on: superseded).
set -e
B=briefs/059/motive/balance; S=out/scratch/motive/059c
rm -rf $S/tree; mkdir -p $S/tree $S/plans $S/scripts $S/logs
git archive 100b5e3a sim contracts tools runs/missions/handover GDD package.json bunfig.toml tsconfig.json bun.lock | tar -x -C $S/tree
ln -sfn "$PWD/node_modules" $S/tree/node_modules
rm -rf $S/tree/sim/Suborned.Sim/bin $S/tree/sim/Suborned.Sim/obj $S/tree/sim/cli/bin $S/tree/sim/cli/obj
patch -s -p1 -d $S/tree < $B/engine-probe.patch
cp $B/plans/routes.json $S/tree/contracts/missions/handover/routes/routes.json
for f in $B/plans/*.plan.json; do cp $f $S/tree/contracts/missions/handover/routes/plans/; done
for f in $B/probe-plans/*.plan.json; do cp $f $S/plans/; done
for f in $(cd $S/tree/contracts/missions/handover/routes/plans && ls *.plan.json); do python3 -I -c "import json,sys; p=json.load(open(sys.argv[1])); [s.pop('why',None) for s in p['steps']]; json.dump(p,open(sys.argv[2],'w'),indent=1)" $S/tree/contracts/missions/handover/routes/plans/$f $S/plans/$f; done
cp $B/probe.ts $B/matrix.sh $B/final.sh $B/s1demo.sh $S/
(cd $S/tree/sim && dotnet build cli/cli.csproj -c Release -o ../out/missions/engine/tree/bin -v q -nologo)
echo "probe ready in $S"
