#!/bin/sh
# Do the pins bite? (Pulse, brief 067 I, 2026-10-26.) tools/people/test/looks.test.ts pins tools/people/looks.ts to the host's C# source. A pin that cannot fail is no pin, so this reads the
# same test from a copy of the host's source (HostViews*.cs, HandoverHost.cs) with one change each, and says whether the test caught it. The unchanged copy must pass.
#   sh briefs/067/pulse/mutate-looks-test.sh        exit 0 when the control passes and every change is caught
# The copies are in out/scratch/pulse/mut/ (scratch); nothing in unity/ or tools/ is touched. out/ is ignored by `bun test`, so the run names a scratch bunfig.
set -u
cd "$(dirname "$0")/../../.." || exit 1
ROOT=$(pwd); OUT=out/scratch/pulse/mut; mkdir -p "$OUT"; printf '[test]\n' > "$OUT/bunfig.toml"; bad=0

# run <name> <file in Play/> <old text> <new text> <what the change is>  ->  prints "caught" (at least one test fails) or "MISSED"; the control (old = new) must print "passes"
run() {
  D="$OUT/$1"; rm -rf "$D"; mkdir -p "$D/Play"
  cp unity/Assets/Suborned/Runtime/Play/HostViews*.cs unity/Assets/Suborned/Runtime/Play/HandoverHost.cs "$D/Play/"
  python3 -I - "$D/Play/$2" "$3" "$4" <<'PY' || { echo "  $1: the target text is not in $2 (the host moved): update this script"; bad=1; return; }
import sys
p, old, new = sys.argv[1:4]; s = open(p).read()
assert old in s, 'mutation target not found: ' + old
open(p, 'w').write(s.replace(old, new, 1))
PY
  sed -e "s#join(import.meta.dir, '..', '..', '..')#'$ROOT'#" \
      -e "s#const play = join(root, 'unity/Assets/Suborned/Runtime/Play');#const play = '$ROOT/$D/Play';#" \
      -e "s#from '../looks'#from '$ROOT/tools/people/looks'#" \
      -e "s#src('unity/Assets/Suborned/Runtime/Play/HostViews.Rivals.cs')#readFileSync(join(play, 'HostViews.Rivals.cs'), 'utf8')#" \
      tools/people/test/looks.test.ts > "$D/looks.mut.test.ts"
  res=$(bun test --config="$OUT/bunfig.toml" "./$D/looks.mut.test.ts" 2>&1); fails=$(printf '%s\n' "$res" | grep -cE '^\(fail\)'); passes=$(printf '%s\n' "$res" | grep -E '^ *[0-9]+ pass$' | tr -d ' ' | sed 's/pass//')
  if [ "$3" = "$4" ]; then
    if [ "$fails" = 0 ]; then echo "  $1: passes ($passes tests)  - $5"; else echo "  $1: CONTROL FAILS ($fails)  - $5"; bad=1; fi
  elif [ "$fails" -gt 0 ]; then echo "  $1: caught by $fails test(s)  - $5"; printf '%s\n' "$res" | grep -E '^\(fail\)' | sed -e 's/ \[[0-9.]*ms\]$//' -e 's/^(fail) the replay is the host.s choice (each line of it pinned to the source) > /      /'
  else echo "  $1: MISSED  - $5"; bad=1; fi
}

echo "pins against changes to the host's source:"
run m0 HostViews.cs 'using System.Collections.Generic;' 'using System.Collections.Generic;' 'the control: the source unchanged'
run m1 HostViews.cs 'Ring("sel/0");' 'Dress("crowd/0", "guard"); Ring("sel/0");' 'a new Dress call the audit does not replay'
run m2 HostViews.cs 'Dress(key, BodyFigure(p)); }' 'Dress(key, PersonFigure(p)); }' 'bodies back to the plain pick (the 16 walkers change clothes again)'
run m3 HandoverHost.cs 'p.Job == "guard" ? Text?.TargetName("crowd-guard", "a guard") ?? "a guard"' 'p.Job == "guard" ? Text?.TargetName("crowd-guard", "a passer-by") ?? "a passer-by"' 'the crowd guard named a passer-by again'
run m4 HostViews.Figures.cs '_ when IsCrewId(key) || key.StartsWith("body/rival-") => "rival",' '_ when IsCrewId(key) || key.StartsWith("body/rival-") => RivalFigure(key),' "RoleFigure given Forma's RivalFigure (a look by member)"
run m5 HostViews.cs 'Dress(key, "rival"); }' 'Dress(key, RivalFigure(p.Id)); }' "a rival's body given RivalFigure"
run m6 HostViews.cs 'host.figures.Pick("family/passer", b.Id)' 'host.figures.Pick("family/guard", b.Id)' "the range's passers drawn from the guards' pool"
run m7 HostViews.cs 'h.Id.StartsWith("rival") ? "rival" : "security")' 'h.Id.StartsWith("rival") ? "rival" : "guard")' 'a hostile officer dressed as the door guard'
[ "$bad" = 0 ] && echo "every change caught, the control passes" || echo "SOMETHING IS WRONG (above)"
exit $bad
