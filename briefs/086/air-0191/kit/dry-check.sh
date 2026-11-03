#!/bin/zsh
# Brief 086: run-air.sh's syntax and paths, launching nothing and writing nothing. Exit 1 on a fault. Run in the kit folder's copy on the Air.
HERE=${0:A:h}; ROOT=${HERE:h}; S=$HERE/run-air.sh; bad=0; say() { print -r -- "$1"; }; no() { say "FAULT $1"; bad=1; }
zsh -n $S && say "syntax ok" || no "run-air.sh syntax"
APP="$ROOT/The Suborned.app"; EXE="$APP/Contents/MacOS/The Suborned"; MM=$HOME/.local/suborned-air/macmon/macmon
[[ -x $EXE ]] && say "player $(/usr/bin/plutil -extract CFBundleShortVersionString raw "$APP/Contents/Info.plist")" || no "no player at $EXE"
xattr "$APP" 2>/dev/null | grep -q quarantine && no "the app is quarantined" || say "no quarantine on the app"
[[ -x $MM ]] && say "macmon $($MM --version 2>&1 | head -1)" || no "no macmon at $MM"
[[ -w $ROOT ]] && say "writable $ROOT" || no "$ROOT is not writable"
grep -qE '^HERE=.*ROOT=\$\{HERE:h\}' $S && say "ROOT is the kit's parent" || no "ROOT is not the kit's parent"
grep -q 'suborned-air-019[0-4]' $S && no "run-air.sh names an old folder"
for r in $(grep -E '^ *(for .*; do )?launch ' $S | sed -E 's/.*launch ([^ ]+) ([^ ]+) .*/\2/' | sort -u); do
  case $r in "''"|shipped) continue;; esac
  f=$HERE/$r.rows.json; [[ -f $f ]] || { no "no rows file $r"; continue; }
  /usr/bin/plutil -lint -s $f >/dev/null 2>&1 || grep -q '"rows"' $f || no "$r is not a rows file"
  grep -q '"name"' $f && say "rows $r: $(grep -c '"name"' $f)" || no "$r has no rows"
done
pgrep -f "^$EXE" >/dev/null && no "a player of this kit is running"
pgrep -x macmon >/dev/null && no "a macmon is running"
say "AC: $(pmset -g batt | head -1)"; say "low power: $(pmset -g | grep -i lowpowermode | tr -s ' ')"; say "disk free: $(df -h $ROOT | tail -1 | awk '{print $4}')"
exit $bad
