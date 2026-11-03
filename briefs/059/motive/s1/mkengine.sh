#!/bin/zsh
# Motive 059 S1: the scratch probe engine = engine 0.31.0 (sim/) + engine-probe.patch (two env switches, off = 0.31.0 byte for byte).
# Run from the repo root: bun run guard zsh briefs/059/motive/s1/mkengine.sh
set -e; E=out/scratch/motive/059-s1/engine
rm -rf $E; mkdir -p $E; rsync -a --exclude bin --exclude obj sim/Suborned.Sim sim/cli $E/
sed -e 's#^--- sim/#--- #' -e 's#^+++ out/scratch/motive/059-s1/engine/#+++ #' briefs/059/motive/s1/engine-probe.patch | (cd $E && patch -p0)
(cd $E && dotnet build cli/cli.csproj -c Release -o bin -v q)
