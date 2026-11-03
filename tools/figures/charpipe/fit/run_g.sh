#!/bin/sh
# brief 069 fit test: G for every input in parallel (one Blender each), logs in fit-test/logs. Start through `bun run guard`.
R=/Users/sky/Projects/suborned; B=$R/out/scratch/forma/char-pipeline/blender.sh; LOG=$R/out/scratch/forma/char-pipeline/fit-test/logs
for id in "$@"; do $B -P $R/tools/figures/charpipe/fit/build_g.py -- $id > $LOG/g_$id.log 2>&1 & done; wait
grep -h -E "DONE G|Traceback|Error" $LOG/g_*.log
