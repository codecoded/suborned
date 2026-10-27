#!/bin/sh
# brief 069 end-to-end: the whole build chain for an end-to-end set (FIT_SET, default e2e: e2e.json or fixer.json) into <out> (default the set's out):
# basis -> fit -> the hybrid build -> the textures. Used twice for the byte-for-byte check (determinism.py).
R=/Users/sky/Projects/suborned; B=$R/out/scratch/forma/char-pipeline/blender.sh; F=$R/tools/figures/charpipe/fit
export FIT_SET=${FIT_SET:-e2e}; [ -n "$1" ] && export FIT_OUT="$1"
IDS=$(python3 -c "import json;print(' '.join(p['id'] for p in json.load(open('$F/$FIT_SET.json'))['people']))")
$B -P $F/basis.py > /dev/null 2>&1 && $B -P $F/fit_body.py -- $IDS 2>&1 | grep -E "DONE|Error|Traceback" && $B -P $F/build_a.py -- $IDS --hybrid 2>&1 | grep -E "DONE|Error|Traceback" | cut -c1-160 && python3 $F/e2e_textures.py > /dev/null
