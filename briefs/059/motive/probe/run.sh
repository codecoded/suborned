#!/bin/zsh
# usage: run.sh <script-name> <variant...>   plays scratch scripts against scratch roots (HEAD inputs + the probe site)
cd "$(git rev-parse --show-toplevel)"
sc=$1; shift
for v in "$@"; do
  (cd out/scratch/motive/059/root-$v && dotnet /Users/sky/Projects/suborned/out/missions/engine/tree/bin/suborned-sim.dll play ../scripts/$sc.json > ../$sc-$v.log 2>&1)
  echo "== $sc $v: $(head -1 out/scratch/motive/059/$sc-$v.log)"
  grep -E "enter.out|fight.loud|ai.join|fight.blocker|car.free|blocker.out|board.in|captured|walked|rivals.reach|success|car-seen|seal" out/scratch/motive/059/$sc-$v.log | grep -v "^  cause\|^  debrief" | head -16 | cut -c1-170
done
