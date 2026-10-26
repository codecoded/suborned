#!/bin/sh
# brief 069 fit test: every animate spec in fit-test/specs, five at a time. Start through `bun run guard`.
R=/Users/sky/Projects/suborned; S=$R/out/scratch/forma/char-pipeline/fit-test
ls $S/specs/*.json | xargs -P 5 -n 1 $R/tools/figures/charpipe/fit/anim_one.sh
grep -h -E "DONE|Traceback|Error" $S/logs/anim_*.log
