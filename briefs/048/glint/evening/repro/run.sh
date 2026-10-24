#!/bin/zsh
# usage: run.sh <tag>  : sync my files into the scratch clone, build the player there, run the road test into out/scratch/<tag>
set -e
R=/Users/sky/Projects/suborned; S=$R/out/scratch/glint/evening/repo; TAG=$1
A=unity/Assets/Suborned
for f in $A/Runtime/AtmosphereLens.cs $A/Runtime/DistanceHaze.cs $A/Runtime/RoadLight.cs $A/Runtime/Play/LightPhase.cs $A/Shaders/SubornedSky.shader $A/Shaders/DecoKit/DK_GlassSky.shader $A/Resources/Atmosphere/lens.json $A/Resources/Atmosphere/haze.json $A/Resources/Atmosphere/roadlight.json; do rm -f $S/$f; cp -c $R/$f $S/$f; done
cd $R
bun run guard "/Applications/Unity/Hub/Editor/6000.6.2f1/Unity.app/Contents/MacOS/Unity" -batchmode -quit -projectPath $S/unity -executeMethod Suborned.World.Play.GameBuild.Cli -logFile $S/out/build/play/build.log > $S/out/scratch/build-$TAG.txt 2>&1 || true
grep -E '"result"|"errors"' $S/out/build/play/build.json
mkdir -p $S/out/scratch/$TAG
bun -e "
import {resolveTpLong} from './tools/game/tplong.ts';
const raw=JSON.parse(require('fs').readFileSync('$S/out/scratch/rows.json','utf8'));
require('fs').writeFileSync('$S/out/scratch/resolved-$TAG.json', JSON.stringify(resolveTpLong(raw,'$S/out/scratch/$TAG'),null,1));
"
bun run guard "$S/out/build/play/The Suborned.app/Contents/MacOS/The Suborned" -tplong $S/out/scratch/resolved-$TAG.json $S/out/scratch/$TAG -novsync -logFile $S/out/scratch/$TAG/player.log > $S/out/scratch/run-$TAG.txt 2>&1 || true
grep -n "RoadLight\|rejected\|Exception" $S/out/scratch/$TAG/player.log | head
ls $S/out/scratch/$TAG/road-*.jpg | wc -l
