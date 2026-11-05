# 096 · The rivals' timeline in mission.json, after engine 0.41.0–0.43.0 (Waymark, 2026-11-05)

The rival car no longer drops three at 2:46 and parks at 3:10. Since engine 0.41.0 (Motive's rivals page, Sky "all recommended": the block on
arrival on every route), it turns in and drives to our car's stand, then stops across its way out. The timeline now says so.

## What the engine does (seed 0, engine 0.43.0, `dotnet run --project sim/cli -c Release -- play <route>`)

| beat | log code | square (route-a, route-b) | service drive (fight-loud-wait-yard) | data or engine |
|---|---|---|---|---|
| turns in from Orbit Walk | rivals.coming | 2:35.6 | 2:35.6 | data: computed back from the anchor 3:10 (now `rivals.anchor`), ±15 s by seed |
| stops across our way out (boxed in) | rivals.block | 2:52.8 | 2:48.4 (the drive's mouth) | engine: the drive to the stand's block point |
| three step out (5 s after, 2 s apart) | car.held.post (route-a) | 2:57.8, 2:59.8, 3:01.8 | – | engine: rulebook values |
| the three posted | rivals.post | 3:15.2, 3:17.9, 3:40.0 (route-b) | – | engine: walks |
| the lookout posted, watching Popovic's car | rivals.lookout-posted | 3:58.6 (route-b) | 3:09.6 | engine: walk |

Every beat moves with the seed's rivals band (all of them together).

## mission.json (contracts/missions/handover/mission.json)
- timeline: `2:35` reworded to "the rival car turns in from Orbit Walk and drives to the cell's car" (Keel's §07 line), with a new src and note.
- timeline: `2:46` (the drop) and `3:10` (the park) become one mark, `2:53`: "it stops across the cell's car's way out, boxing it in; three get out and post round it, the lookout walks to watch Popovic's car, the wheelman stays in". The note says 2:53 is the engine's time, not data, rounded. It also says the posts and the lookout's post are the engine's walks, so they get no mark (their seed-0 times are in the note).
- `rivals.anchor` (new): `3:10`, the reference the engine times the crew from (site `schedule.rivalsArrive`, `Game.RivalAt`; the turn-in is computed back from it). It used to be read from the "rival car parks" entry, which is gone. The value is unchanged (190 s), so `sim/data/handover.site.json` builds the same.
- Two code lines follow: `contracts/missions/mission.schema.ts` (rivals.anchor, optional, `{ at, src, note }`) and `tools/sim/site.ts` (`rivalsArrive: sec(mission.rivals.anchor.at)`, and it throws if the anchor is missing).

## text.json (Fable's words): only what had to follow
- `briefing.timeline`: the `2:46` mark moves to `2:53`. Its label ("Three rivals take posts round our car") and short ("Rivals post") are unchanged.
- `briefing.timeline`: the `3:10` mark ("The rival car parks. Its lookout watches the official door", "Rivals park") is removed. The pill must not show a park that never happens.
- `contracts/missions/handover/text.test.ts`: the Alt words list drops 'Rivals park'.
- Already right: `conversations.rivals-arrive` clock `2:53` (now a real mark).

## For Fable (words, not done by me)
- The 2:53 label could name the box-in, the real news at that mark (e.g. "The rival car boxes ours in; three post round it"). The lookout lost his pill line with the 3:10 mark (his crucial card, rivals.lookout-posted, still tells it).
- `hints.H8.trigger`: "3:10 has passed (the rivals have parked)…" is stale. Unity's H8 waits for the lookout posted at his post (HostHints.cs).
- `conversations.rivals-coming.note`: "The 2:46 drop and the 3:10 park stay in sight" is stale.

## For Aster (GDD v0.39.0, §07 schedule, line 229 of v0.38.0)
`tools/missions/handover/mission.test.ts` ties the timeline to §07's `schedule:` line word for word, so it fails until the GDD says this. Use these two items in place of the old three rival items, kept as they are: each must begin with its m:ss, so no "about" in front of 2:53.

`2:35 the rival car turns in from Orbit Walk and drives to the cell's car · 2:53 it stops across the cell's car's way out, boxing it in; three get out and post round it, the lookout walks to watch Popovic's car, the wheelman stays in`

The human GDD's three `hot` rows (2:35, 2:46, 3:10) and the §07 rivals paragraph ("parks across from Popovic's car at 3:10") follow. So does §14 Pulse's hold line (Keel's gdd-text-rivals.md has it).

## Left stale on purpose (each needs someone else's step)
- Point src/reason of rival-park, rival-drop, rival-lookout and rival-car-entry still say 2:46.5, 3:10, 3:15 and "parks". They are copied into `sim/data/handover.site.json`, so changing them makes the site stale and needs `bun run sim:site` (sim/data, off limits to me) and maybe the recorded runs' inputs. rival-park is no longer where the car goes. That is for Vantage (the points) with Keel (site and records).
- The plan route `rivals` still ends at "the rival car's park on Apron Road". The routes are under mission.test's byte-for-byte lock (declared moves only). Re-ending it at the block point is a declared change for a later round.
- Latch: HostHints H8 still gates on `190 * Game.Hz` (the old 3:10). It is harmless at the square, where the lookout posts about 3:58. From the drive's mouth he can post as early as 2:55 on an early seed, and the hint then waits until 3:10. Proof123.cs's comment names the 3:10 park.
- Keel (optional): the engine could read the turn-in itself instead of computing it back from a retired park's time. Then `rivals.anchor` would go and the 2:35 mark would be the data.

## Evidence
- `bun test contracts/missions/handover`: 200 pass, 0 fail.
- `bun test tools/missions/handover`: 37 pass, 1 fail. The one is "the timeline is GDD §07's schedule, entry for entry" (waits for Aster above).
- `bun tools/plan/cli.ts --overlay tools/missions/handover/overlay.ts`: exit 0.
- `bun run typecheck`: exit 0.
- `bun test tools/sim/sim.test.ts -t handover.site.json`: the site builds identical to the committed one except `cast.sha256`, and the people test fails too. Both come from `out/people/harbour-district.people.json` on this machine (3 Nov), not from this change. `schedule.rivalsArrive` stays 190.
- Logs: out/scratch/waymark/route-b.log, overlay.txt, typecheck.txt.
