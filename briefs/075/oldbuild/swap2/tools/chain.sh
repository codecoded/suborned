#!/bin/zsh
# Brief 075 swap, second build (Latch): ours scene is built separately (HarbourWorldBuilder from out/scratch/latch/075swap2/harbour.world.json);
# this builds the swap and shoots it, offscreen, each step through the playing guard (unityrun.ts holds ProjectSettings).
set -u
cd /Users/sky/Projects/suborned
L=out/scratch/latch/075swap2
R=briefs/075/oldbuild/swap/tools/unityrun.ts
step() { local log=$1; shift; bun $R $L/$log.log -- "$@" | tail -1; }
[[ "${SKIP_BUILD:-}" == 1 ]] || step build -executeMethod Suborned.OldTown.Editor.Swap.OldTownSwap.Build -swapHeights rule -swapHq ours
step shoot -executeMethod Suborned.OldTown.Editor.Swap.OldTownSwap.Shoot -swapScene OldTown -swapOut briefs/075/oldbuild/swap2 -swapShots ${SHOTS:-circus,golden,vista,play,playold,district}
