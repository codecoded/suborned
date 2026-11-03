#!/bin/sh
# Brief 075 OldTown foliage trial (Latch, Opus 5.5, 2026-10-29): the exact steps, in order. Review only; -oldtownFoliage trial is mandatory.
# Every Unity step goes through the playing guard via unityrun.ts (holds QualitySettings/GraphicsSettings bytes).
set -e; cd "$(dirname "$0")/../../../.."
E=briefs/075/recovery/oldtown-foliage-unity; S=out/scratch/latch/oldtown-foliage-unity; U=briefs/075/oldbuild/swap/tools/unityrun.ts
W=$PWD/out/scratch/latch/recovery/integration/world.json   # the pinned 0.5.6 world the paving baseline was built from
R=Suborned.World.Review; B=Assets/Scenes/probe-oldtown-ours.unity; T=Assets/Scenes/probe-oldtown-foliage075.unity
python3 -I $S/pin.py $E/inputs-before.json
bun $U $S/logs/audit.log -- -executeMethod $R.OldTownFoliage075.Audit
bun $U $S/logs/apply.log -- -executeMethod $R.OldTownFoliage075.Apply -world $W -oldtownFoliage trial
for t in baseline:$B trial:$T; do n=${t%%:*}; sc=${t#*:}
  bun $U $S/logs/$n-manifest.log -- -executeMethod $R.Recovery075.Manifest -recoveryScene $sc -recoveryTag $n-manifest -recoveryOut $S/manifests
  bun $U $S/logs/$n-restyle.log -- -executeMethod $R.Restyle075.Manifest -restyleManifest "$sc=0=$PWD/$S/manifests/$n-restyle.json"
  bun $U $S/logs/$n-shots.log -- -executeMethod $R.Glint075.Cli -scene $sc -variant today -mode play -out $PWD/$E/$n -spots 'trough-detail:-179.5,-222.5,210,3;trough-pair:-179.5,-220.5,210,3.6;trough-pair:-179.5,-220.5,30,3.6'
  bun $U $S/logs/census-$n.log -- -executeMethod $R.OldTownFoliage075.Census -oldtownScene $sc -oldtownTag census-$n
done
python3 -I $E/compare.py $S/manifests/baseline-manifest.json $S/manifests/trial-manifest.json $S/manifests/baseline-restyle.json $S/manifests/trial-restyle.json briefs/075/recovery/latch-integration/paving.json $E/comparison.json
SP=$(python3 -I -c "print(';'.join(f'm{i:02d}:{-179.5+i*0.01:.3f},{-220.5+i*0.006:.3f},210' for i in range(24)))")
bun $U $S/logs/motion.log -- -executeMethod $R.Glint075.Cli -scene $T -variant today -mode play -nonight -spotsonly -out $PWD/$S/motion -spots "$SP"
python3 -I $S/pin.py $E/inputs-after.json
