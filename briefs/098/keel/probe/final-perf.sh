#!/bin/bash
# Keel's scratch chain (brief 098, engine 0.50.0): the tick's cost, 0.49.0 and 0.50.0 alternating, two rounds, one thread each.
W=/Users/sky/.t3/worktrees/suborned/t3-48e18061; K=$W/out/scratch/keel; M=$K/m050f
G() { bun $W/tools/game/playing.ts "$@"; }
for round in 1 2 3; do for e in e049 050; do
  B=$K/bin-$e; R=$W; [ $e = e049 ] && R=$K/e049
  for sc in $K/m050/route-a-410.json $W/contracts/missions/handover/routes/loud-fight.json; do
    n=$(basename $sc .json); (cd $R && G dotnet $B/suborned-sim.dll dial perf $sc --settings "unset;x4;people=64;guards=64" --runs 10 > $M/perf-$e-$n-$round.json 2> $M/perf-$e-$n-$round.txt)
  done
done; done
echo done
