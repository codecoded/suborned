#!/bin/zsh
# usage: run.sh <seeds> plan...  (summary per plan)
cd /Users/sky/Projects/suborned/out/scratch/keel/wt080
DLL=$PWD/out/missions/engine/tree/bin/suborned-sim.dll; N=$1; shift
T=$(mktemp); DOTNET_gcServer=1 dotnet $DLL batch "$@" --seeds $N --seed-from 1 --digest --no-replay --out $T >/dev/null 2>&1
python3 -I out/scratch/motive/tune/sum.py $T
