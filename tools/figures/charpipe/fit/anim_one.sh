#!/bin/sh
# one animate spec; its log goes to the spec folder's sibling logs/
R=/Users/sky/Projects/suborned; D=$(dirname $(dirname "$1"))
$R/out/scratch/forma/char-pipeline/blender.sh -P $R/tools/figures/charpipe/fit/animate.py -- "$1" > $D/logs/anim_$(basename "$1" .json).log 2>&1
