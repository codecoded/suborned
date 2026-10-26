#!/bin/sh
# brief 069 end-to-end: the whole build chain for the e2e people into <out> (relative to the repo; default e2e.json's out):
# basis -> fit -> the hybrid build. Used twice into det folders for the byte-for-byte check (tools/figures/charpipe/fit/determinism.py).
R=/Users/sky/Projects/suborned; B=$R/out/scratch/forma/char-pipeline/blender.sh; F=$R/tools/figures/charpipe/fit
export FIT_SET=e2e; [ -n "$1" ] && export FIT_OUT="$1"
IDS="e2e_coat e2e_dress e2e_guard e2e_flamingo"
$B -P $F/basis.py > /dev/null 2>&1 && $B -P $F/fit_body.py -- $IDS 2>&1 | grep -E "DONE|Error|Traceback" && $B -P $F/build_a.py -- $IDS --hybrid 2>&1 | grep -E "DONE|Error|Traceback" | cut -c1-120 && python3 $F/e2e_textures.py > /dev/null
