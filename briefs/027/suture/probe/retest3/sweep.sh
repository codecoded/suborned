#!/bin/zsh
# usage: sweep.sh <policyfile> <preset> <out>
cd /Users/sky/Projects/suborned
S=out/suture027/r3
if grep -q 'BatchCli.Jobs' $S/cli/FightLab.cs 2>/dev/null; then # brief 042: a parallel fight-lab takes the whole list in one call (same rows, same order)
  env DOTNET_gcServer=1 DOTNET_gcConcurrent=0 DOTNET_TieredPGO=0 dotnet $S/cli/bin/Release/net10.0/suborned-sim.dll fight-lab --presets $2 --rulebook $S/rulebook.snapshot.json --runs "$(paste -sd, $1)" > $S/chunk.$2.$(basename $1).aa.out 2>&1
else
split -l 80 $1 $S/chunk.$2.$(basename $1).
for f in $S/chunk.$2.$(basename $1).*; do
  dotnet $S/cli/bin/Release/net10.0/suborned-sim.dll fight-lab --presets $2 --rulebook $S/rulebook.snapshot.json --runs "$(paste -sd, $f)" > $f.out 2>&1 &
done
wait
fi
cat $S/chunk.$2.$(basename $1).*.out | grep -E "^$2 " > $3
grep -h "replay" $S/chunk.$2.$(basename $1).*.out | sort | uniq -c >> $3.replay
rm $S/chunk.$2.$(basename $1).*
