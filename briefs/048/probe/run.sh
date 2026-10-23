#!/bin/zsh
# Brief 048 (Redline): the draw-distance probe in a COPY of the built player. The shipped app (out/build/play) is never touched.
# usage: run.sh setup                      # clone the app to out/redline/048/app, add Redline048.dll, re-sign ad hoc
#        run.sh <name> <flags...>          # run the copy: -redline048 out/redline/048/<name> plus the flags (e.g. -novsync -capture-size 1920x1080 -r48mode measure -r48view 0,90)
set -e
REPO=/Users/sky/Projects/suborned; W=$REPO/out/redline/048; SRC="$REPO/out/build/play/The Suborned.app"; APP="$W/app/The Suborned.app"
if [[ $1 == setup ]]; then
  mkdir -p $W/src $W/app; cp $REPO/briefs/048/probe/{Probe.cs,probe.csproj} $W/src/
  (cd $W/src && dotnet build -c Release -o ../bin | grep -E "error|Build succeeded")
  rm -rf "$APP"; cp -cR "$SRC" "$APP"
  D="$APP/Contents/Resources/Data"; cp $W/bin/Redline048.dll "$D/Managed/"
  python3 -I - "$D" <<'EOF'
import json, sys
d = sys.argv[1]
s = json.load(open(d + '/ScriptingAssemblies.json'))
if 'Redline048.dll' not in s['names']: s['names'].append('Redline048.dll'); s['types'].append(16)
json.dump(s, open(d + '/ScriptingAssemblies.json', 'w'))
r = json.load(open(d + '/RuntimeInitializeOnLoads.json'))
if not any(x['assemblyName'] == 'Redline048' for x in r['root']):
    r['root'].append({'assemblyName': 'Redline048', 'nameSpace': 'Redline048', 'className': 'Probe', 'methodName': 'Boot', 'loadTypes': 0, 'isUnityClass': False})
json.dump(r, open(d + '/RuntimeInitializeOnLoads.json', 'w'))
EOF
  codesign --force --deep -s - "$APP" 2>&1 | tail -1; echo "set up $APP"; exit 0
fi
name=$1; shift; mkdir -p $W/$name
"$APP/Contents/MacOS/The Suborned" -redline048 $W/$name -logFile $W/$name/player.log "$@" >/dev/null 2>&1
echo "exit $? $name"
