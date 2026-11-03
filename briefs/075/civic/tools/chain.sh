#!/bin/zsh
# Brief 075 civic restyle (Latch): before | after builds, shots, manifests and the zone regression, offscreen batchmode, each step through
# the playing guard (unityrun.ts holds ProjectSettings/QualitySettings and GraphicsSettings). Before = -civicRestyle off, after = the default
# (Editor/World/civic-restyle.json, Vela's restyle.json verbatim). Steps by name: STEPS="a b c ..." (default all, in this order).
# Order matters: a swap build rewrites the old builders' shared generated meshes (OldTownArt/DecoKit/Generated), so the before swap scene is
# shot, verified and measured before the after swap is built (and goes stale once it is; OldTown.unity is built last and stays valid).
set -u
cd /Users/sky/Projects/suborned
L=out/scratch/latch/075civic
R=briefs/075/oldbuild/swap/tools/unityrun.ts
W=out/scratch/latch/075swap2/harbour.world.json   # the pinned export (plan 0.5.5, sha f469704e), the swap2 build's
P=$L/plan-0.5.5.json                              # its plan, pinned (git show 6c258751:contracts/world/harbour-district.plan.json, sha e168639f): plan 0.5.6 landed mid-run
STEPS=${STEPS:-"a b d f j1 ib e g j2 h ia c ic k"}
step() { local log=$1; shift; echo "$(date +%T) START $log"; bun $R $L/$log.log -- "$@" | tail -1; }
has() { [[ " $STEPS " == *" $1 "* ]]; }
U=Suborned.World; S=Suborned.OldTown.Editor.Swap.OldTownSwap; M=$U.Review.Restyle075.Manifest
for s in ${=STEPS}; do case $s in
  a) step a-ours-before -executeMethod $U.HarbourWorldBuilder.Build -world $W -scene Assets/Scenes/probe-civic-before.unity -civicRestyle off ;;
  b) step b-ours-after -executeMethod $U.HarbourWorldBuilder.Build -world $W -scene Assets/Scenes/probe-oldtown-ours.unity ;;
  c) step c-harbour -executeMethod $U.HarbourWorldBuilder.Build -world out/world/harbour-district.world.json ;;   # the pipeline's scene from the current export
  ic) step ic-manifest-harbour -executeMethod $M -restyleManifest "Assets/Scenes/HarbourDistrict.unity=0=$L/manifest/harbour-after.json" ;;
  d) step d-swap-before -executeMethod $S.Build -swapHeights rule -swapHq ours -swapOurs Assets/Scenes/probe-civic-before.unity -swapScene probe-oldtown-civic-before -swapOut $L/swap-before -swapPlan $P ;;
  f) step f-shoot-before -executeMethod $S.Shoot -swapScene probe-oldtown-civic-before -swapOut $L/shots-before -swapShots golden,play -swapTag before ;;
  j1) step j1-verify-before -executeMethod $S.Verify -swapOurs Assets/Scenes/probe-civic-before.unity -swapScene probe-oldtown-civic-before -swapOut $L/swap-before ;;
  ib) step ib-manifest-before -executeMethod $M -restyleManifest "Assets/Scenes/probe-civic-before.unity=0=$L/manifest/ours-before.json;Assets/Scenes/probe-oldtown-civic-before.unity=206=$L/manifest/swap-before.json" ;;
  e) step e-swap-after -executeMethod $S.Build -swapHeights rule -swapHq ours -swapOurs Assets/Scenes/probe-oldtown-ours.unity -swapScene OldTown -swapOut $L/swap-after -swapPlan $P ;;
  g) step g-shoot-after -executeMethod $S.Shoot -swapScene OldTown -swapOut $L/shots-after -swapShots golden,play -swapTag after ;;
  j2) step j2-verify-after -executeMethod $S.Verify -swapOurs Assets/Scenes/probe-oldtown-ours.unity -swapScene OldTown -swapOut $L/swap-after ;;
  h) step h1-ours-play-before -executeMethod $U.Review.Glint075.Cli -variant today -mode play -nonight -scene Assets/Scenes/probe-civic-before.unity -out $L/ours-before
     step h2-ours-play-after -executeMethod $U.Review.Glint075.Cli -variant today -mode play -nonight -out $L/ours-after
     step h3-ours-golden-before -executeMethod $U.Review.Glint075.Cli -variant today -mode golden -variants $L/golden-variants.json -scene Assets/Scenes/probe-civic-before.unity -out $L/ours-before
     step h4-ours-golden-after -executeMethod $U.Review.Glint075.Cli -variant today -mode golden -variants $L/golden-variants.json -out $L/ours-after ;;
  ia) step ia-manifest-after -executeMethod $M -restyleManifest "Assets/Scenes/probe-oldtown-ours.unity=0=$L/manifest/ours-after.json;Assets/Scenes/OldTown.unity=206=$L/manifest/swap-after.json" ;;
  # Redline's Air A/B kit: both plans re-resolved on the rebuilt scenes (the old one on the play camera); no player is built
  k) step k1-resolve-old -executeMethod $U.Perf.PerfBuild.Cli -perfScene Assets/Scenes/OldTown.unity -perfSpec briefs/075/redline/perf-075ab.json -perfResolveOnly
     step k2-resolve-ours -executeMethod $U.Perf.PerfBuild.Cli -perfScene Assets/Scenes/HarbourDistrict.unity -perfSpec briefs/075/redline/perf-075ab.json -perfResolveOnly ;;
  *) echo "unknown step $s"; exit 2 ;;
esac; done
echo "$(date +%T) DONE"
