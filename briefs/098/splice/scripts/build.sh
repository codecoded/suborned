#!/bin/zsh
# usage: build.sh <name> [margin]  -> $OUT/<name>/ (default OUT=out/scratch/splice): a lean scratch player, the zone and a margin (brief 097's pattern).
# EXTRA adds GameBuild flags: "-keepVariants", "-scratchVariants traced", "-scratchDev". Brief 098 fast loop (Splice); run it through the guard:
#   bun run guard zsh briefs/098/splice/scripts/build.sh after
R=/Users/sky/Projects/suborned; OUT=${OUT:-$R/out/scratch/splice}; mkdir -p $OUT; cd $OUT; name=${1:?name}
if pgrep -f 'Unity.app/Contents/MacOS/Unity.*suborned/unity' >/dev/null; then echo "Unity has the project open; not building"; exit 4; fi
t0=$(date +%s)
/Applications/Unity/Hub/Editor/6000.6.2f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath $R/unity -executeMethod Suborned.World.Play.GameBuild.CliScratch -scratchBuild $OUT/$name -lean ${2:-110} ${=EXTRA} -logFile $OUT/$name.log
code=$?; echo "exit $code after $(( $(date +%s) - t0 )) s"
grep -E "ShaderStrip097:|ShaderTrace098:|Game: build|error CS|BuildFailed" $OUT/$name.log | head -40
exit $code
