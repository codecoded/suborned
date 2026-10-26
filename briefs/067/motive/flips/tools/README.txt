Motive, brief 067 flips (2026-10-26). Probes ran on a frozen copy of Keel's engine 0.34.0 build (out/scratch/keel/bin, built 11:31/11:39)
and the tree's inputs at 11:55 (inputs 12390659450729014507, the same hash as briefs/067/keel/facts-0.34.0.txt), under out/scratch/motive/067f/:
  root/       the tree's inputs (rulebook, people rules, sim/data, briefs/027/presets, the 41 route scripts)
  root-every/ root with ai.intent.final "every" (only that word changed; the inputs hash only labels the start state: Game.StartHash)
  root-pp/    root with police-1 at (-5.0, -44.0) and police-2 at (-6.0, -45.0) in sim/data/handover.site.json (14.1 m and 14.8 m from
              d:Getaway bay, a line to it, outside fight.hold and the exit lane)
  root-pa/    root with route-a-board.json replaced by sim/Suborned.Sim/Fixtures/sq-route-a-board.script.json
  root0331/   engine d4361806 (0.33.1) inputs, presets and routes (git archive)
  rb/         rulebook probes for fight-lab --rulebook: run280, run320, pace140 (walk 1.4 + run 2.8), mr250
Tools: play.sh <0340|0340e|0331> <script|scenario>; trace.cs (positions each second; built against the frozen Suborned.Sim.dll);
free.cs (nearest free cell and line to the car); cmp.py (two batch outputs); labcmp.py (two fight-lab outputs).
Every command went through bun tools/game/playing.ts (the Sky-is-playing guard). Results: ../results.txt.
