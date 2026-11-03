#!/bin/zsh
# usage: shotsrun.sh <tag>   (player already built in the scratch clone); day, evening, night shots into repo/out/scratch/<tag>-shots/<phase>
R=/Users/sky/Projects/suborned; S=$R/out/scratch/glint/evening/repo; E=$R/out/scratch/glint/evening; TAG=$1
cd $R
for ph in day evening night; do
  O=$S/out/scratch/$TAG-shots/$ph; rm -rf $O; mkdir -p $O
  bun $E/mkshots.ts $E/shots.in.json $O $O/resolved.json
  extra=(); [ $ph = evening ] && extra=(-evening); [ $ph = night ] && extra=(-night)
  bun run guard "$S/out/build/play/The Suborned.app/Contents/MacOS/The Suborned" -shots $O/resolved.json $O -novsync $extra -logFile $O/player.log > $O/run.txt 2>&1 || true
  ls $O | grep -E "jpg|png" | tr '\n' ' '; echo
done
