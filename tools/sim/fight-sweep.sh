#!/bin/zsh
# fight-sweep.sh <tree> <preset> <out-prefix> [rulebook] [policyfile]   (brief 042, Splice; env passes through, e.g. WF=1)
# The retest3 policies (3,792 by default) played on <tree>'s fight-lab; writes <out-prefix>.txt (the rows) and <out-prefix>.replay.
# One `fight-lab` call: the fight-lab of a current sim/cli runs every (preset, run) on all cores itself and prints the rows in run order, so
# the rows are the bytes the 16 chunked processes gave, in the same order. A tree whose fight-lab is still serial (a pinned older CLI: no
# BatchCli.Jobs in its FightLab.cs) keeps the old 16 chunks, or one call would be 16x slower. FIGHT_SWEEP_CHUNKS=1 forces the chunks.
# Output line: "<n> replay lines" in .replay is the number of calls (1 here, 16 chunked): the text after it is the same.
T=${1:A}; P=$2; O=${3:A}; RB=${4:-contracts/rules/rulebook.json}; PF=${5:-/Users/sky/Projects/suborned/briefs/027/suture/probe/retest3/policies.all.txt}
D=$T/sim/cli/bin/Release/net10.0/suborned-sim.dll; export DOTNET_ROOT=${DOTNET_ROOT:-/opt/homebrew/opt/dotnet/libexec}
[[ ${RB:0:1} == / ]] || RB=$T/$RB
mkdir -p ${O:h}; tmp=$(mktemp -d ${O}.tmp.XXXXXX)
cd $T
if grep -q 'BatchCli.Jobs' sim/cli/FightLab.cs && [[ -z $FIGHT_SWEEP_CHUNKS ]]; then
  # server GC, no concurrent GC, no PGO: measured 25-35% faster than defaults for one process on every core (with defaults, 16 chunked processes beat it)
  env DOTNET_gcServer=1 DOTNET_gcConcurrent=0 DOTNET_TieredPGO=0 dotnet $D fight-lab --presets $P --rulebook $RB --runs "$(paste -sd, $PF)" > $tmp/all.out 2>&1
  outs=($tmp/all.out)
else
  split -l 240 $PF $tmp/c.
  for f in $tmp/c.??; do dotnet $D fight-lab --presets $P --rulebook $RB --runs "$(paste -sd, $f)" > $f.out 2>&1 & done; wait
  outs=($tmp/c.??.out)
fi
cat $outs | grep -E "^$P +L" > $O.txt; grep -h "replay" $outs | sort | uniq -c > $O.replay; rm -r $tmp
