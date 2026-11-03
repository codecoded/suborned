#!/bin/zsh
# Motive brief 067 square probe: a site variant on the 067sq scratch tree. Starts from HEAD's adjust file and mission.json (saved by mk.sh as
# adjust.head.json, mission.head.json), applies variant.py's change <name> to them, then block -> site -> cover -> site, as Splice's chain runs.
# Saves site.<name>.json, block.<name>.json, cover.<name>.json in out/scratch/motive/067sq. From the repo root, under the guard:
#   bun run guard zsh briefs/067/motive/sq/site.sh <name>
set -e
cd /Users/sky/Projects/suborned
S=out/scratch/motive/067sq; T=$S/tree; name=$1
cp $S/adjust.head.json $T/contracts/world/civic-block.adjust.json
cp $S/mission.head.json $T/contracts/missions/handover/mission.json
python3 -I briefs/067/motive/sq/variant.py $name $T/contracts/world/civic-block.adjust.json $T/contracts/missions/handover/mission.json
(cd $T && bun tools/plan/block.ts | grep -E "^structures|^buildings|adjustments" | cut -c1-160; bun tools/sim/site.ts | tail -1 | cut -c1-80; bun tools/cover/cli.ts | grep -E "^(ok|FAIL)|ambush|police"; bun tools/sim/site.ts | tail -1 | cut -c1-80)
cp $T/sim/data/handover.site.json $S/site.$name.json; cp $T/contracts/world/civic-block.json $S/block.$name.json; cp $T/contracts/world/civic-block.cover.json $S/cover.$name.json
echo "site.$name.json"
