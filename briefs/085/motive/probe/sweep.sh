#!/bin/zsh
# sweep.sh <name> <on> <stay> <run> [seeds] : the probe engine over plans/, rows in res/<name>.jsonl
cd /Users/sky/Projects/suborned/out/scratch/motive/085p/tree
export DOTNET_ROOT=/opt/homebrew/opt/dotnet/libexec DOTNET_gcServer=1 DOTNET_gcConcurrent=0 DOTNET_TieredPGO=0
MP_NOTE=1 MP_ON=$2 MP_STAY=$3 MP_RUN=$4 dotnet ../bin/suborned-sim.dll batch ../plans/*.adaptive.json --seeds ${5:-200} --seed-from 1 --digest --no-replay --out ../res/$1.jsonl | tail -2
