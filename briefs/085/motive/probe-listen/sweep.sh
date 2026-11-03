#!/bin/zsh
# sweep.sh <name> <on> <reach m> [min cm] [seeds] [seats]
S=/Users/sky/Projects/suborned/out/scratch/motive/085l
cd $S/tree
export DOTNET_ROOT=/opt/homebrew/opt/dotnet/libexec DOTNET_gcServer=1 DOTNET_gcConcurrent=0 DOTNET_TieredPGO=0
perl -pi -e 's/("listen-reach-m": \{ "v": )\d+/${1}'$3'/' contracts/rules/rulebook.json
ML_NOTE=1 ML_ON=$2 ML_MIN=${4:-300} ML_SEATS=${6:-1} dotnet ../bin/suborned-sim.dll batch ../plans/*.adaptive.json --seeds ${5:-200} --seed-from 1 --digest --no-replay --out ../res/$1.jsonl | tail -1
