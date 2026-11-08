#!/bin/bash
# Keel's scratch chain (brief 098, engine 0.50.0): the round's evidence on the final engine, one heavy run at a time through the guard.
W=/Users/sky/.t3/worktrees/suborned/t3-48e18061; K=$W/out/scratch/keel; M=$K/m050f; mkdir -p $M
G() { bun $W/tools/game/playing.ts "$@"; }
AD=$W/contracts/missions/handover/adaptive; SET="unset;people=32;people=64;guards=32;guards=64;cars=8;x4"
REG="office-door:7550,8050,-3820,-2380;cafe-front:-1070,-500,-4100,-1700;cafe-alley:-650,-500,-3700,-3250;office-entrance:6000,6800,-4800,-3900;fountain:0,1300,-3700,-2300"
cd $W
G dotnet $K/bin-050/suborned-sim.dll check > $M/check.txt 2>&1; echo "check: $(tail -1 $M/check.txt)"
SUBORNED_SWEEP_RUNS=$M/sweep-runs-050.tsv G dotnet $K/bin-050/suborned-sim.dll dial sweep $AD --seeds 20 --settings "$SET" --out $M/sweep-050.json > $M/sweep-050.txt 2>&1; echo "sweep: $(grep -E '^dial sweep' $M/sweep-050.txt)"
SUBORNED_LONG=100000 G dotnet $K/bin-050b/suborned-sim.dll dial regions $AD --seeds 10 --settings "unset;people=64;guards=64;x4" --regions "$REG" --out $M/regions-050L.json > $M/regions-050L.txt 2>&1; echo "regions: $?"
G dotnet $K/bin-050/suborned-sim.dll dial bodies --seeds 3 --out $M/bodies-050.json > $M/bodies-050.txt 2>&1; echo "bodies: $?"
G dotnet $K/bin-050/suborned-sim.dll dial bodies $W/contracts/missions/handover/routes --seeds 3 --settings "people=64;guards=64,people=64;x4" --out $M/bodies-routes-050.json > $M/bodies-routes-050.txt 2>&1; echo "bodies routes: $?"
G dotnet $K/bin-050/suborned-sim.dll dial sweep $AD/quiet-g.adaptive.json --seeds 50 --settings unset --out $M/quietg50-050.json > $M/quietg50-050.txt 2>&1; echo "quiet-g 50: $(grep cover: $M/quietg50-050.txt)"
G dotnet $K/bin-050/suborned-sim.dll hashes > $M/hashes.json 2> $M/hashes.err; echo "hashes: $?"
echo done
