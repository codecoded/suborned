#!/bin/zsh
R=/Users/sky/Projects/suborned; S=$R/out/scratch/glint/evening/repo; E=$R/out/scratch/glint/evening; TAG=$1
cd $R; O=$S/out/scratch/$TAG-night; rm -rf $O; mkdir -p $O
bun $E/mkshots.ts $E/shots.night.json $O $O/resolved.json
bun run guard "$S/out/build/play/The Suborned.app/Contents/MacOS/The Suborned" -shots $O/resolved.json $O -novsync -night -logFile $O/player.log > $O/run.txt 2>&1 || true
ls $O | grep -E "png|jpg" | grep -v hud | tr '\n' ' '; grep -n "RoadLight\|Exception" $O/player.log | head -5
