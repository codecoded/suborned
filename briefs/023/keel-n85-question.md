# Brief 023 · N85 "both doors held, he waits": a rule question for Motive (Keel)

Keel, 2026-10-19. On the tree with the west-face door, engine 0.8.2.

**The rule under test.** rulebook response and brief 013 E3: a cancelled Enter comes out by a door no unit holds. If both doors are held, he waits inside, and that wait can't be cancelled. He comes out the tick a door is free (`Game.ExitWithoutVale`).

**Why N85 can't reach it any more.** A door is held only while a scene unit stands at a found, open body that can see that door (`HoldScene`, `HoldActive`, `DoorsInSightOf`). The public door (west face) and the service door (east face) are now on opposite faces. No single body can see both, so N85 needs two scenes. The probes are in `out/keel-drive/n85*.fsx` and `yard*.fsx`.

- **Public door.** The guard is knocked out at his post at 4:00.1 (N84's orders). Guard 73 finds him at 4:57.1, the report lands at 5:02.1, and the door is held from 5:12.1. An Enter must start before then, because Enter is refused through a held door.
- **Service door.** The yard is quiet by design (Sky 2026-10-19: the quiet back way). Between 4:00 and 5:30 the only person who passes within witness range is guard 74, at 4:20 to 4:31.
  - The Enforcer can run round, knock 74 out at 4:20.9, and leave him unseen.
  - 74's missed check-in at 5:30 sends the north-street response unit. It finds him, and the service door is held from 6:25.7, or 6:24.1 at security level 3 or 4.
- **The gap.** An Enter started before 5:12.1 lasts 38 s, or 68 s at Alert ≥ 2. So it ends by 6:20 at the latest, with Vale, before the second door is held. An Enter can't start after 5:50 either: Vale leaves the Office then (`vale-not-inside`).

So on this block, "both doors held while the Enforcer is inside" can't happen in play. The engine branch is still there, but N85 can't reach it.

**Options (Motive's call):**
1. **Keep the rule and test it with a start fixture.** A test-only start key that places a held scene at each door, like `untie`, which a test uses to cut a tie. Play doesn't change, the branch stays covered, and N85 tests exactly what it tested. Cost: about 40 lines (Keel).
2. **Change the rule: a scene unit holds every door of the building it was sent to.** One body found at either door then holds both. Play changes: any found body at the Office shuts it. The routes need checking (N84, the careless routes, route G).
3. **Retire the wait-inside branch.** With both doors held, the cancel comes out by the door nearer the Enforcer, or the cancel is refused (`door-held`). This simplifies the rule; N85 becomes a test of whichever branch replaces it.

**Keel recommends 1.** The rule still makes sense, and other blocks (one face, two doors) will reach it again. Until Motive rules, N85 is the one scenario still failing, as documented.
