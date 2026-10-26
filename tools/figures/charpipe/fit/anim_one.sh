#!/bin/sh
R=/Users/sky/Projects/suborned; S=$R/out/scratch/forma/char-pipeline/fit-test
$R/out/scratch/forma/char-pipeline/blender.sh -P $R/tools/figures/charpipe/fit/animate.py -- "$1" > $S/logs/anim_$(basename "$1" .json).log 2>&1
