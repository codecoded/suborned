#!/bin/sh
# play.sh <engine: 0340|0331|tree> <script.json|scenario> [--json]   plays one script on a frozen engine, through the Sky-is-playing guard
R=/Users/sky/Projects/suborned; F=$R/out/scratch/motive/067f
case "$1" in
  0340) DLL=$F/bin/suborned-sim.dll; ROOT=$F/root ;;
  0331) DLL=$R/out/missions/engine/d4361806c675/bin/suborned-sim.dll; ROOT=$R/out/missions/engine/d4361806c675 ;;
  0340e) DLL=$F/bin/suborned-sim.dll; ROOT=$F/root-every ;;
  *) echo "engine?"; exit 2 ;;
esac
shift
cd "$ROOT" && exec bun $R/tools/game/playing.ts dotnet "$DLL" play "$@"
