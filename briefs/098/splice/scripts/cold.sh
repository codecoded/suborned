#!/bin/zsh
# usage: cold.sh <name>   one real clean compile of the DecoKit shaders (brief 098, Splice): DK_Wind097.hlsl's `4.0` becomes $LIT (default 4.00; a code
# literal outside every keyword guard: every kit variant's source changes, nothing it computes), build.sh <name>, then the byte back (trapped, also on
# failure). A LIT the cache has already compiled is not cold: pick a new one (4.000, 4.0000, ...). EXTRA passes through to build.sh.
R=/Users/sky/Projects/suborned; F=$R/unity/Assets/Suborned/Shaders/DecoKit/DK_Wind097.hlsl; HERE=${0:A:h}; OUT=${OUT:-$R/out/scratch/splice}; mkdir -p $OUT
if pgrep -f 'Unity.app/Contents/MacOS/Unity.*suborned/unity' >/dev/null; then echo "Unity has the project open; not building"; exit 4; fi
cp -p $F $OUT/DK_Wind097.hlsl.orig; sum0=$(shasum -a 256 < $F)
restore() { cp -p $OUT/DK_Wind097.hlsl.orig $F; [[ "$(shasum -a 256 < $F)" == "$sum0" ]] && echo "DK_Wind097.hlsl restored" || echo "DK_Wind097.hlsl NOT restored: copy $OUT/DK_Wind097.hlsl.orig back"; }
trap restore EXIT INT TERM
python3 -I - $F <<'PY'
import os, sys; p = sys.argv[1]; s = open(p).read(); a = "_DKWind.z : 4.0, speed"; assert s.count(a) == 1, "the literal moved"
lit = os.environ.get("LIT", "4.00"); open(p, "w").write(s.replace(a, f"_DKWind.z : {lit}, speed")); print(f"DK_Wind097.hlsl: 4.0 -> {lit}")
PY
zsh $HERE/build.sh $1
