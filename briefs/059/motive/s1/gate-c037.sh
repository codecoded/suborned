#!/bin/zsh
# Motive 059 S1: every c037 base script on the scratch engine with the gate on (PROBE_GATE=1), on c037's own inputs (the repo tree).
# Same final hash, tick and outcome as c037's record = the gate changes no recorded result. Run from the repo root under the guard:
#   bun run guard zsh briefs/059/motive/s1/gate-c037.sh
ENG=out/scratch/motive/059-s1/engine/bin/suborned-sim.dll; same=0; diff=0
for f in runs/missions/handover/c037/routes/*.json; do
  n=${f:t:r}; [[ $n == routes ]] && continue
  got=$(PROBE_GATE=1 dotnet $ENG play $f --json 2>/dev/null | python3 -I -c "import json,sys; d=json.load(sys.stdin); print(d['finalHash'], d['tick'], d['outcome'])")
  want=$(python3 -I -c "import json; p=json.load(open('runs/missions/handover/c037/run.json'))['plays']['$n']; print(p['finalHash'], p['tick'], p['outcome'])")
  if [[ $got == $want ]]; then same=$((same+1)); else diff=$((diff+1)); echo "DIFF $n got=[$got] c037=[$want]"; fi
done
echo "gate on, c037 base scripts: $same same, $diff differ"
