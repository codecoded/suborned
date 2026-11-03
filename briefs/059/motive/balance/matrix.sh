#!/bin/zsh
# Motive 059c probe matrix: each config is a set of probe switches; plays every plan id given on the scratch engine.
cd /Users/sky/Projects/suborned
ids=("$@")
typeset -A C
B="PROBE_FIXPOS=1 PROBE_GATE=1 PROBE_LATE=1 PROBE_FLOOR=3"
C[s1]="PROBE_GATE=1 PROBE_LATE=1 PROBE_FLOOR=3"
C[fix]="$B"
C[lane]="$B PROBE_LANE=1"
C[shield]="$B PROBE_LANE=1 PROBE_SHIELD=1"
C[cap]="$B PROBE_LANE=1 PROBE_CAP=60"
C[drawn]="$B PROBE_LANE=1 PROBE_DRAWNBREAK=1"
C[yard]="$B PROBE_LANE=1 PROBE_YARDLANE=1"
C[noshield]="$B PROBE_LANE=1 PROBE_CAP=60 PROBE_DRAWNBREAK=1 PROBE_YARDLANE=1"
C[nocap]="$B PROBE_LANE=1 PROBE_SHIELD=1 PROBE_DRAWNBREAK=1 PROBE_YARDLANE=1"
C[all]="$B PROBE_LANE=1 PROBE_SHIELD=1 PROBE_CAP=60 PROBE_DRAWNBREAK=1 PROBE_YARDLANE=1"
C[spot]="$B PROBE_LANE=1 PROBE_SHIELD=1 PROBE_CAP=60 PROBE_DRAWNBREAK=1 PROBE_YARDLANE=1 PROBE_PLACES=svc-ambush-1:7735:-800"
for k in ${(s: :)CONFIGS:-fix lane shield cap drawn all}; do
  env ${(s: :)C[$k]} TAG=$k bun out/scratch/motive/059c/probe.ts $ids 2>&1 | grep -v '^\$'
done
