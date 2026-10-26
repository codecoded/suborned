#!/bin/sh
# brief 069: every animate spec in <dir>/specs (default fit-test), five at a time; logs in <dir>/logs. Start through `bun run guard`.
R=/Users/sky/Projects/suborned; S=${1:-$R/out/scratch/forma/char-pipeline/fit-test}; mkdir -p $S/logs
ls $S/specs/*.json | xargs -P 5 -n 1 $R/tools/figures/charpipe/fit/anim_one.sh
grep -h -E "DONE|Traceback|Error" $S/logs/anim_*.log
