#!/bin/zsh
# Motive brief 067 square probe: a scratch tree of HEAD (71bf4032: engine 0.33.1 code, the data of round 1: A the yard patrol, J the
# robot out, K the planters out and ambush-5 re-sited, L the car points) with engine-probe.patch (my pace switches), the patrol facing that
# Vantage's points need ([0,-100], reroute [-100,0]; tools/sim/site.ts in scratch only: Keel is fixing the shared one), a `trace` command
# (Trace.cs, scratch only), and the c038 candidate route set as briefs/067/motive/cand.sh plays it. Then site -> cover -> site.
# Run from the repo root under the guard:   bun run guard zsh briefs/067/motive/sq/mk.sh
set -e
cd /Users/sky/Projects/suborned
B=briefs/067/motive; S=out/scratch/motive/067sq
rm -rf $S/tree $S/routes0 && mkdir -p $S/tree $S/plans $S/routes0
git archive 71bf4032 sim contracts tools runs/missions/handover GDD package.json bunfig.toml tsconfig.json bun.lock briefs/027/presets briefs/027/keel | tar -x -C $S/tree
git archive 71bf4032 contracts/missions/handover/routes | tar -x -C $S/routes0
ln -sfn "$PWD/node_modules" $S/tree/node_modules
rm -rf $S/tree/sim/Suborned.Sim/bin $S/tree/sim/Suborned.Sim/obj $S/tree/sim/cli/bin $S/tree/sim/cli/obj $S/tree/sim/Suborned.Sim.Tests/bin $S/tree/sim/Suborned.Sim.Tests/obj
patch -s -p1 -d $S/tree < $B/engine-probe.patch
cp $B/sq/Trace.cs $S/tree/sim/cli/Trace.cs
python3 -I - $S/tree <<'PY'
import sys, re
T = sys.argv[1]
p = T + '/sim/cli/Program.cs'; s = open(p).read()
s2 = s.replace('case "probe": Probe.Run(inputs); return 0;', 'case "probe": Probe.Run(inputs); return 0;\n                case "trace": return TraceCli.Run(inputs, root, args); // Motive 067 square probe (scratch)')
assert s2 != s; open(p, 'w').write(s2)
p = T + '/tools/sim/site.ts'; s = open(p).read()
s2 = re.sub(r"patrol: \{ a: 'patrol-a', b: 'patrol-b', facing: \[0, 100\], reroute: 'patrol-reroute', rerouteFacing: \[0, -100\]", "patrol: { a: 'patrol-a', b: 'patrol-b', facing: [0, -100], reroute: 'patrol-reroute', rerouteFacing: [-100, 0]", s)
assert s2 != s; open(p, 'w').write(s2); print('scratch: trace command, patrol facing [0,-100] / [-100,0]')
PY
cp $S/tree/contracts/rules/rulebook.json $S/tree/contracts/rules/rulebook.orig.json
for f in briefs/059/motive/balance/probe-plans/*.plan.json; do cp $f $S/plans/; done
cp briefs/059/motive/balance/plans/routes.json $S/tree/contracts/missions/handover/routes/routes.json
for f in briefs/059/motive/balance/plans/*.plan.json; do python3 -I -c "import json,sys; p=json.load(open(sys.argv[1])); [s.pop('why',None) for s in p['steps']]; json.dump(p,open(sys.argv[2],'w'),indent=1)" $f $S/tree/contracts/missions/handover/routes/plans/$(basename $f); done
for f in $B/plans/*.plan.json(N); do cp $f $S/plans/; done
for f in $B/sq/plans/*.plan.json(N); do cp $f $S/plans/; done
cp $S/tree/contracts/world/civic-block.adjust.json $S/adjust.head.json
mkdir -p $S/tree/out/people && cp out/people/harbour-district.people.json $S/tree/out/people/ # the promoted people the site casts from (bun run people, Splice 06:40)
(cd $S/tree && bun tools/sim/site.ts | tail -1 && bun tools/cover/cli.ts | grep -E "^(ok|FAIL)"; bun tools/sim/site.ts | tail -1)
python3 -I -c "import json,sys; a=json.load(open(sys.argv[1])); b=json.load(open(sys.argv[2])); print('cast as HEAD:', a['cast']==b['cast'], '· patrol', b['patrol']['facing'], b['patrol']['rerouteFacing'], '· cover', len(b['cover']['points']))" sim/data/handover.site.json $S/tree/sim/data/handover.site.json
cp $S/tree/sim/data/handover.site.json $S/site.k.json
(cd $S/tree/sim && dotnet build cli/cli.csproj -c Release -o ../out/missions/engine/tree/bin -v q -nologo | tail -2)
echo "probe ready in $S/tree (site.k.json = round 1's square with the patrol facing fixed)"
