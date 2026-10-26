#!/bin/sh
# The look matrix of briefs/067/pulse.md section 7.3 (Pulse, brief 067 I): every figure the host can put on the street, at true size through Forma's proof tools, over both play yaws (30 the opening,
# 210 the Office front), sun and shade, two headings, measured against the guard and the police by look-matrix.py. Part A is Forma's build (the Velvet Set, not installed), part B the install (the old rival).
#   bun run guard sh briefs/067/pulse/run-look-matrix.sh [out.txt]        (default: briefs/067/pulse/look-matrix.txt; about a minute; it needs Blender, as tools/figures/proof_run.py does)
cd "$(dirname "$0")/../../.." || exit 1
OUT=${1:-briefs/067/pulse/look-matrix.txt}
ALL=guard,security,d4,d4b,d5c,c6,d6,el1,sh2,workers_d5,cafe_w2,cafe_w3,cafe_w5,rival,rival_b,rival_c,rival_d,waitress,driver,councillor,scout,fixer,tech,enforcer
{
echo "# Pulse, brief 067 I, re-run 2026-10-26: who reads as the guard (and as the police) at true size. python3 -I briefs/067/pulse/look-matrix.py <People dir> <yaw> <light> <heading> <ids>"
echo "# part A: Forma's build (out/people-models/People: the Velvet Set, not installed yet). Part B: the install (unity/Assets/Suborned/People: the rival the game draws today)."
echo
echo "## A. the build"
for yaw in 30 210; do for light in sun shade; do for hd in cam 75; do
  python3 -I briefs/067/pulse/look-matrix.py out/people-models/People $yaw $light $hd $ALL || exit 1
  echo
done; done; done
echo "## B. the install (old rival), against the guard and the police"
for yaw in 30 210; do for light in sun shade; do for hd in cam 75; do
  python3 -I briefs/067/pulse/look-matrix.py unity/Assets/Suborned/People $yaw $light $hd guard,security,rival,d4,d4b,d5c,c6 || exit 1
  echo
done; done; done
} > "$OUT"
echo "wrote $OUT"
