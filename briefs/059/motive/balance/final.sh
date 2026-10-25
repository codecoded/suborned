#!/bin/zsh
# Motive 059c: the balance rulings as probe switches (FINAL, plus EXTRA from the environment); usage: final.sh compose [ids…] | check
cd /Users/sky/Projects/suborned
FINAL="PROBE_FIXPOS=1 PROBE_GATE=1 PROBE_LATE=1 PROBE_FLOOR=3 PROBE_LANE=1 PROBE_SHIELD=1 PROBE_DRAWNBREAK=1 ${EXTRA:-}"
what=$1; shift
if [[ $what == compose ]]; then env ${(s: :)FINAL} bun out/scratch/motive/059c/tree/tools/missions/check/cli.ts compose handover "$@" --engine tree --inputs tree --jobs 8
else env ${(s: :)FINAL} bun out/scratch/motive/059c/tree/tools/missions/check/cli.ts handover --engine tree --inputs tree; fi
