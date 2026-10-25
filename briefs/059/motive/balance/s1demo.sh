#!/bin/zsh
# Motive 059c: Sky's S1 = no, measured on 0.32 + the balance rulings: A-pressure with the getaway car Driven to the yard before the Enter (apy-1),
# with the gate, without it, and without the late spring. Prints the composed play's line per case.
cd /Users/sky/Projects/suborned
B="PROBE_FIXPOS=1 PROBE_LATE=1 PROBE_FLOOR=3 PROBE_LANE=1 PROBE_SHIELD=1 PROBE_DRAWNBREAK=1"
for c in "gate:PROBE_GATE=1" "nogate:PROBE_GATE=0" "nolate:PROBE_GATE=1 PROBE_LATE=0"; do
  tag=${c%%:*}; ex=${c#*:}
  env ${(s: :)B} ${(s: :)ex} TAG=s1-$tag bun out/scratch/motive/059c/probe.ts apy-1 apy-leak 2>&1 | grep -v '^\$'
done
