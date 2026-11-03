#!/bin/zsh
# Brief 075 tram bed (Latch, E11 + E12): rebuild the swap scene, verify the zone, shoot the tram frames; offscreen, each step through the playing guard
# (unityrun.ts holds ProjectSettings). STEPS="before build verify shoot" picks steps; "before" audits and shoots the scene as it stands (no rebuild).
set -u
cd /Users/sky/Projects/suborned
L=out/scratch/latch/075tram; mkdir -p $L
R=briefs/075/oldbuild/swap/tools/unityrun.ts
O=briefs/075/oldbuild/tram
M=Suborned.OldTown.Editor.Swap.OldTownSwap
step() { local log=$1; shift; bun $R $L/$log.log -- "$@" | tail -1; }
W=out/scratch/latch/075tram/harbour.world.json   # plan 0.5.6's export (sha 1c38f7e6...), pinned
for s in ${=STEPS:-ours build verify shoot}; do
  case $s in
    ours)   step ours -executeMethod Suborned.World.HarbourWorldBuilder.Build -world $W -scene Assets/Scenes/probe-oldtown-ours.unity ;;
    before) step audit-before -executeMethod $M.TramAudit -swapScene OldTown -swapOut $O -swapTag before
            step shoot-before -executeMethod $M.Shoot -swapScene OldTown -swapOut $O -swapShots tram -swapTag before ;;
    build)  step build -executeMethod $M.Build -swapHeights rule -swapHq ours -swapWorld $W -swapOut $O ;;
    verify) step verify -executeMethod $M.Verify -swapOut $O ;;
    shoot)  step shoot -executeMethod $M.Shoot -swapScene OldTown -swapOut $O -swapShots tram,vista,circus -swapWorld $W -swapTag rule ;;
  esac
done
