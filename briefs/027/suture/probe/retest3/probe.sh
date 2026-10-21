#!/bin/zsh
# usage: probe.sh <envname|none> ; runs the matrix + all policies, lab
cd /Users/sky/Projects/suborned; S=out/suture027/r3
if [[ $1 != none ]]; then export $1=1; fi
dotnet $S/cli/bin/Release/net10.0/suborned-sim.dll fight-lab --presets lab,lab-field-on,lab-cover --rulebook $S/rulebook.snapshot.json | grep -E "^lab" | grep -vE "micro" > $S/probe.$1.matrix.txt
$S/sweep.sh $S/policies.all.txt lab $S/probe.$1.lab.txt
