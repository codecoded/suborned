#!/bin/zsh
# Regenerates ../results.txt from the frozen probe trees in out/scratch/motive/067f (see README.txt). Every play through the Sky-is-playing guard.
R=/Users/sky/Projects/suborned; F=$R/out/scratch/motive/067f; E=$R/briefs/067/motive/flips; O=$E/results.txt
g(){ bun $R/tools/game/playing.ts "$@"; }
{
echo "# Motive brief 067 flips: probe results on the frozen engine 0.34.0 (out/scratch/motive/067f), 2026-10-26 (tools/README.txt, tools/results.sh)"
echo; echo "## facts (inputs hash = Keel's tree)"; (cd $F/root && dotnet $F/bin/suborned-sim.dll facts)
echo; echo "## 1. rbb-ff with every lock 3.0 s late (BAL-YARD-FOCUS-LATE's alt, no Go hook), then with the Scout stepping out at 7:34.0"
for s in rbb-ff-late rbb-ff-late-stepout; do (cd $F/root && g dotnet $F/bin/suborned-sim.dll play $E/scripts/$s.script.json) | grep -E '^rbb|board\.in|fixer\.down|rivals\.(for-car|back-to-cover)|car\.held|order\.rejected +cell go|enforcer\.down|fail\.|car\.free|fight\.over|blocker\.out|board\.out'; echo; done
echo "## 2. route B fought out (bal-rb-ff): the Trail ledger"
(cd $F/root && g dotnet $F/bin/suborned-sim.dll play $R/sim/Suborned.Sim/Fixtures/bal-rb-ff.script.json --json) | python3 -I -c "import json,sys; d=json.load(sys.stdin); print(d['outcome'], d['trail'],'T'); [print(' ',e) for e in d['ledger'] if e['what']=='trail']"
echo; echo "## 3. the 41 route scripts, ai.intent.final agent (the rulebook) vs every (only rows that differ; Trail never does)"
python3 -I $F/cmp.py routes-root.jsonl routes-root-every.jsonl agent every
echo; echo "route-a-pressure under agent: rival-5 from his kite step to the break (3:12.7), no shot"; grep -E 'rival-5' $F/logs/rap-0340.txt | grep -v crowd | head -6
echo; echo "## 4. fight lab: the today kite win, traced each 5 s (officer-1, officer-2 at their stands; the Enforcer at the car from 3:18)"
(cd $F && g dotnet trace/bin/trace.dll root "lab/today.L3:tight-enfgo-carfree-runkite.script.json" 3:03.0 3:53.0 officer-1,officer-2,enforcer) | awk 'NR%5==1'
echo; echo "the square's police stands from d:Getaway bay (850,-4820): police-1 17.7 m, police-2 19.4 m, police-3 12.2 m, police-4 18.6 m (pistol eff 15 m, max 16 m)"
echo; echo "today, the three tight enfgo-carfree kite rows by run speed (fight-lab --rulebook)"
for v in run280 run320; do echo "-- $v"; (cd $F/root && g dotnet $F/bin/suborned-sim.dll fight-lab --runs 'L3:tight-enfgo-carfree-runkite,L3:tight-focus-enfgo-carfree-runkite,L3:tight-enfgo-carfree-kite' --presets today --rulebook $F/rb/$v.json) | grep -E '^today +L3' | sed -E 's/  +/ /g' | cut -c1-200; done
echo "-- 360 (the tree)"; grep -E '^today +L3:tight-(focus-)?enfgo-carfree-(run)?kite ' $F/logs/lab-tree.txt | sed -E 's/  +/ /g' | cut -c1-200
echo; echo "lab-kite-off: the six rows that flip to wins, at the old pace (rb/pace140: walk 1.4, run 2.8)"
(cd $F/root && g dotnet $F/bin/suborned-sim.dll fight-lab --runs 'L3:tight-kite,L3:tight-reset-kite,L3:tight-focus-reset-kite,L3:tight-cycle-kite,L3:tight-hold-runkite,L3:tight-step200-focus-norivals-at236-runkite' --presets lab-kite-off --rulebook $F/rb/pace140.json) | grep -E '^lab-kite-off +L3' | sed -E 's/  +/ /g' | cut -c1-120
echo; echo "today and lab matrices (84 rows): police-1 and police-2 moved within reach (root-pp) vs the tree, rows that differ"
python3 -I $F/labcmp.py $F/logs/lab-tree.txt $F/logs/lab-pp.txt
echo "the 41 routes, posts moved vs the tree:"; python3 -I $F/cmp.py routes-root.jsonl routes-root-pp.jsonl tree posts-moved
echo; echo "P-A with the re-composed route-a-board (root-pa; P-A hands over to the lab's player at tick 2490)"
(cd $F/root-pa && g dotnet $F/bin/suborned-sim.dll fight-lab --runs P-A --presets today,lab,lab-cover) | grep -E '^(today|lab|lab-cover) +P-A' | sed -E 's/  +/ /g'
echo "my 1.8 m/s re-compositions on the bare square (sim/Suborned.Sim/Fixtures):"
for s in sq-route-a-board sq-ra-h3 bal-rb-ff bal-rbb-ff; do (cd $F/root && g dotnet $F/bin/suborned-sim.dll play $R/sim/Suborned.Sim/Fixtures/$s.script.json --json) | python3 -I -c "
import json,sys; d=json.load(sys.stdin); c=d['debrief']['cell']; lost=sum(x['max']-x['health'] for x in c); mx=sum(x['max'] for x in c)
dn=[l['code'] for l in d['log'] if l['code'] in ('scout.down','fixer.down','tech.down','enforcer.down')]
t=d['tick']; print(' ',d['id'], d['outcome'], f'{t//600}:{(t%600)/10:04.1f}', d['trail'],'T', f'{round(100*lost/mx)} % lost', 'downs', dn)"; done
echo; echo "## 6. N125"; grep -E 'rivals\.(drawn|coming|drawn-stop)|patrol\.(alarm|breaks)|fail\.' $F/logs/N125-0340.txt
echo; echo "## 8. N185: the blocker's shots on operatives in the car (750 x 750 untrained x 500 beside cover = 281)"; awk 'NR>1 && $1>="5:26.0" && $1<="6:01.0"' $F/logs/N185-0340.txt | grep -E 'board\.in|rival-2 fires on (fixer|scout) at 11 m \(281 ‰\): hit|\.down |order\.rejected' | head -12
echo; echo "## MC-11: loud.json at the loud fight's start (3:45.7)"; grep -E 'ai\.join|car\.held' $F/logs/loud-0340.txt | head -4
(cd $F && g dotnet trace/bin/trace.dll root routes/loud.json 3:46.0 3:46.0 security-1,security-2,scout,fixer,tech)
} > $O 2>&1
