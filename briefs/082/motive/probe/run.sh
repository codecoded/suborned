#!/bin/zsh
cd /Users/sky/Projects/suborned; N=$1; shift; F=$(mktemp); DOTNET_gcServer=1 dotnet out/missions/engine/tree/bin/suborned-sim.dll batch "$@" --seeds $N --seed-from 1 --digest --no-replay --out $F >/dev/null 2>&1; python3 -I out/scratch/motive/082/sum.py $F
