# 098 · The living playbook: laws, not sums

Owner: main session (as Aster), opened 2026-11-07. **Status: direction approved by Sky (2026-11-07, second quote block below); worked on its own branch, `work/098-living-playbook`.** The GDD changes as a new version once the laws are concrete.

## Sky (2026-11-07)
- "our roads are empty, no reason can't have passing traffic"
- "I understand if we add people that adds witnesses, but don't you think we're being too hard? You start pulling a gun out, then ofc someone probably going to see, and that could happen anytime."
- "The game should be built around guidelines / basic rules of play, not strict maths. How can the game grow if every change needs to be recalculated ad infinitum? It's what's slowing us down and not how I imagined it to be"
- "There should be a general living playbook. Do something in front of a camera, noticed. Pull a gun out in front of someone they run away, maybe call the police based on their nature."
- "It doesn't need to be so 'That's 5 seconds before they get to the police' etc - the player should feel they're in a living world. It's like coding up some natural laws that just work - that's what Suborned in principle should be."

## Sky (2026-11-07, after reading the draft)
- "when it comes to wanting to add more pedestrians, I feel like I'm pushed back because of the work that will entail. It shouldn't be like that. I should say, add 3 more guards, take some away, bring in more cars etc - the game then just does that, no long processing, and I play and get a feel."
- "Ofc adding 100s of guards makes it impossible to win - but then, say I build a prison, we can have fun with that kinda thing - Street Fighter 2 wasn't planned for every possible combination, nor is chess"
- "There's tenets like Inez in love with the bodyguard - scripts in the game that exist, but like GTA, the world has chaos within it you can play with and suffer the spoils of it all"
- "get the banners in, and then we clean up the branch, commit it, and then branch off and work on the rules direction"

## Tenets and chaos
- **Tenets** are the authored story a mission stands on. They stay written:
  - the Handover's cast and their ties: Inez in love with the door guard, the driver, Vale;
  - the mission's clock: Vale out at shift handover, the rivals' car on its timetable.
- **Chaos** is everything else: the city's people, guards, robots, cameras and cars, all running on the laws. A tenet can be played with (turn Inez, call the guard away), and the chaos around it answers by the same laws every time.

## The dial: change the world, play it, feel it
- Sky sets how many of each kind a game has, in one place:
  - guards, patrol robots, cameras, city people, passing cars;
  - later, a whole new place, such as a prison.
- The game builds that world at the start of the next game, from the seed and the laws. There is no generator run, no checker and no rebuild.
  - Guards take posts and beats from the site's doors, gates and corners.
  - People take routines from the site's places.
  - Cars take lanes.
- A dev panel on the start page sets the numbers with clicks: a − and a + per kind, then Play. The counts are saved with the run, so a saved run still replays.
- Any setting runs, including hopeless ones. Hundreds of guards may make the Handover unwinnable, and that's allowed. The bands judge the shipped settings, never every setting.

## Why it is slow now
- The engine already has the right bones:
  - people see in a cone (13 m, ±60°) and hear loud things all round;
  - a person's nature decides whether they report (GDD §05 reports, ten rows);
  - news reaches security only when a person walks to a call box or a staffed door (§01, §05).
- Around those bones, design and tests are written as exact sums.
  - **Tests:** 463 sim scenarios, many pinned to a moment ("5:50 Vale leaves… 6:10 he is at the car").
  - **Numbers per case:** the waitress rests 24–36 s and revisits a table every 75–105 s; the loiter marks are 8/20/45 s; the parked car is reported in 45–60 s; the purse sum is 110,000 > 100,000.
  - **The effect:** a new person, car or bench changes who sees what. Pinned scenarios break, and someone works the sums out again. Brief 080 started moving checks to bands across seeds. The design text and most tests still pin moments.
- The engine keeps old rules alive so pinned scenarios still pass: test-only switches such as `Pre080`, `Pre039`, `OldClock` and `OneInput` (sim `Game.cs` Start) replay the fight, the shots and the clock as they were before a change. Each new rule adds another; laws with their own tests retire them.
- The engine says "every car a player sees is one of them", so every car is a target and solid (GameCars.cs; Sky 2026-10-21, "shootable, as in Syndicate"). Even passing traffic is engine work.

## The playbook (draft laws)
Each law is one plain rule that holds everywhere, for everyone, in every mission. Its few settings live in the rulebook (Keel's), not in the GDD text.

| # | Law | Its settings |
|---|-----|--------------|
| L1 | **Senses.** People see what is in front of them and hear loud things all round. Security also feel someone at their elbow. Walls, cars, crowds and dark block sight. A camera only matters if a person is watching its screen. | sight reach and cone; hearing reach per sound; the screen-watcher's scan |
| L2 | **Alarm.** Every act has a weight: odd (loitering, running, being where you shouldn't), threatening (a drawn gun, a threat, a fight), violent (a shot, a body, a takedown). Ordinary people shrug off odd things unless they're already on edge; nobody shrugs off violence. | the act list and each act's weight; how on edge Alert makes people |
| L3 | **Nature picks the reaction.** The same alarm gets different answers by person. The timid run and hide; the dutiful head for a call box or a guard; the brave shout or step in; the greedy look away unless it pays; the nosy come to look. Each person's nature comes from their own seed, so the same plan plays out differently. | the natures and their share of the crowd; each nature's answer per weight |
| L4 | **News travels on foot.** Ordinary people have no radio. A witness has to reach a call box, a guard or a staffed door, then call. Stop them, scare them or block their way, and the news never lands. How long it takes is just how far they are. | none: walking pace and the call boxes' places do it |
| L5 | **Security answers what reaches it.** Reports raise Alert; gunfire is heard at once. The response comes from where it is, at its pace. | the response dial (already data per district) |
| L6 | **Crowds.** People flee danger, take cover, gather to look once it's quiet, and drift back to their routines. | how far a shot clears a street; how long until people return |
| L7 | **Memory.** People remember faces and what they saw. Questioning later works through memory and nature (the Talker test). | what a witness keeps; the Talker test (exists) |
| L8 | **Traffic.** Drivers keep to their lanes, stop for people in the road and for cars ahead, pull over for police, and brake or flee at gunfire. Roads close in a lockdown. A car is a car: it can be shot, hidden behind or blown up. | lane headways from the plan; how traffic reacts to shots and Alert |
| L9 | **Bodies.** People have real bodies: no two ever overlap, they keep a little space and step round each other, a place holds one body and the next takes the spot beside it, and armed groups stand apart covering different angles (Sky 2026-11-07: "ensuring NPC's don't overlap each, they have real boundaries"; "we need to think about real tactics from this"). | body radius; personal space at rest and walking; a crew's spacing |

What it replaces: per-person and per-mission timings in the design text (a waitress's seconds, a report's seconds, "reported in 45–60 s"). Those become what the laws produce in a place, not what we write down.

## How we would work
- **Laws have tests.** Small tests check that each law holds anywhere: "a timid person who sees a drawn gun moves away from it"; "a report needs a person to reach a report point"; "a car stops for a person in its lane".
- **Missions have fairness bands, not timetables.** The Handover check plays a few hundred seeds and asks:
  - can both routes be won, within a band;
  - does loud play cost;
  - is nothing impossible or trivial?

  A change that keeps the bands passes. Nobody re-derives a timing.
- **Replays still replay.** A saved run (seed + orders) replays exactly on its engine version. When a law changes, pinned replays are re-recorded with one command instead of being hand-fixed.
- **The design text holds laws, the rulebook holds numbers.** Each system's section becomes its laws in plain words plus its list of settings. Changing a number is tuning, not a new design version.
- **The order card reads the scene.** "Every order shows its full price before you commit" stays, as a reading from the same laws: "Seen by 3: 2 will run, 1 will call it in". It stays honest because the card and the street run the same rules. It is never a stopwatch.

## What stays
The pillars and tiebreakers; Keel's rulebook as the one source (unknown = reject); seed + orders replays exactly; the Handover as the reference mission; the price shown before you commit (as a reading); brief 080's seeded starts and hit rolls.

## First moves (in order)
0. **The dial** for guards and city people: counts in one place (the mission's start data plus the dev panel), placed by the engine at the start from the seed. Today the 130 people (16 of them guards) are baked timetables in `sim/data/handover.people.json`, made by Pulse's generator. Under the dial the engine places and routes them itself, so a count is a setting, not a regeneration. Keel (engine, in its own worktree), Pulse (the placing and routine rules, ported from tools/life and tools/routines), Latch (the panel), Waymark (the shipped settings' bands).
1. **Traffic, the first system built this way (L8).** Cars on the plan's lanes as real sim cars: shootable, solid, cover, like every car. The Handover is judged by its bands, with no timings re-derived. Keel (engine, in its own worktree), Pulse (lanes and headways from the plan), Latch (the cars in Unity), Redline (cost on the Air).
2. **Witnesses and reactions rebuilt on L1–L6.** The ten-row reports table becomes natures × alarm. The scatter, the walk to a call box and the response stay but lose their per-case numbers. Motive (natures, weights), Pulse (reactions), Keel (engine).
3. **Checks move to bands.** Suture and Waymark move the Handover checks to seed bands. Scenarios pinned to moments retire as the laws replace them; the law tests and a handful of whole-mission replays stay.
4. **More people.** Once 1–3 land, adding people to the zone is content, not a balance job.

## Decisions
1. **The direction:** laws plus fairness bands, instead of pinned sums. **Sky: yes** ("work on the rules direction").
2. **Traffic:** real sim cars under L8, read from Sky's "bring in more cars etc - the game then just does that". Scenery-only cars are dropped.
3. **The first laws:** L1–L8 as the starting list. Sky can add or cut any as they meet play.
4. **The dial's people count** (Sky 2026-11-07: "people counter is for street"): it counts people on the street at a time, on average (today about 22, not the 114 who mostly work indoors).
5. **Who leaves when a count is lowered** ("same for now"): the same people every game, in a fixed order; the cast never leaves.
6. **Lockdown** ("Drive out"): the cars in the zone drive out, and no new ones come in.
7. **How busy the roads get** ("its fine, even just a few passing helps bring to life"): the plan's roads and junctions stay; above about 25 cars the extra ones queue at the edges.
8. **Jams** ("should be opposite - shot cars can get past, others block"): ordinary traffic blocks a mission car's way, so a jam can box the cell's car in. A car that's been shot can be shoved aside or driven round, so shooting the car in your way clears it.

## Sky's calls on L9 Bodies (2026-11-07)
Keel's law (engine 0.46.0, branch `t3/simulation-body-spacing`, `briefs/098/keel/bodies.html`). Sky: "Do all but the jumping back to spot should be better to dealt with".
- **Kept:**
  - bodies 60 cm apart at the closest (radius 30 cm, the fight spacing);
  - the city's personal space 80 cm, 70 cm in a queue;
  - the cell steps round people or waits, never through them.
- **Not kept: the last resort.** A body stuck 10 s rejoining its place in one tick (about seven a game at 64 people) goes. So does hiding it with a fade or an off-camera cut. A stuck person resolves it as a person would:
  - the one in the way steps aside;
  - two in a one-body gap squeeze past, shoulders turned;
  - a city person who still can't get through changes plans: another free spot that serves, or the next step of their routine.

  Tenets never change plans. Nobody ever jumps.
- **The rest goes ahead in order:**
  1. The merge, after Latch's debrief fix.
  2. Latch's host side: the host's own crowd spreading off.
  3. The cleanup round: Popovic's car stand about 0.6 m out (Vantage), the routes that no longer compose (Waymark), the Q4 Help grid (Motive), Fable's text gaps, and the rest of the failing tests.

## Sky's calls on walking and listening (2026-11-08)
- **"Sit them down if they're listening, they're just regular people."**
  - A listening agent sits at a free seat in listening reach, as any café customer would. Engine 0.39.0's discreet Listen (brief 085: "sit down at cafe, or stand by a wall") becomes the strong default, not a chair that happens to come up within a grace period.
  - With no seat, the agent stands off every walking lane, by a wall, and never across a passage. In quiet-a, the Scout and the Fixer stood across the café's only lane and held people up to 117 s.
  - **Aster's reading of "regular people", for Sky to confirm:** an idle agent (no order) steps aside for someone stuck behind them and back again, as anyone does. Never while acting, watching or listening.
- **"Gap between planter and fountain should be walkable, don't be one of those game engines where you can't get past even though visibly it's clear you have a lot of space either side. We can do better."**
  - **The cause:** the fountain's four planter gaps are 2.05 m in the block, but they're walls in the engine.
    - Every solid is grown by `BODY` 0.65 m (`tools/plan/walk-solids.ts`). That value was calibrated so two measured walks came out the right length: a sum, not a law.
    - The result is then cut into 50 cm cells (`sim/data/handover.site.json` grid), and the engine's A* never cuts a corner.
    - So the 0.75 m left between planter and basin becomes one diagonal line of cells that nobody can cross.
  - **The law:** what is walkable is what you see.
    - A walker keeps its body (L9's radius, 30 cm) and a small margin from a solid: one value, read by the engine, the site tools and the plan checker alike.
    - A gap a body fits through is walkable in any direction.
    - The solids are the pieces as built; 097's volumes pinned to older pieces go.
    - A check over the whole zone proves there are no invisible walls.

## GDD v0.40.0: text to catch up
These are rules Sky already approved that the GDD text still lags, gathered as they're found. They go in with the laws (the done-check below), then `bun run crew`. The human GDD wins on conflict.
- **§04 approaches table:** since engine 0.45.0, a Mercenary or Idealist for whom Help is the wrong approach pays $50,000 once; only a Loyal person still takes two Helps. Sky 2026-11-05: "As you recommend", Motive's Q1 (b). The decision log has no entry for it yet.
- **§07 route A:** since 0.44.0, a fearful Inez takes Help once, $40,000, after the Listen. It's no longer two visits at $20,000.
- **§14 Motive's hold line, and the human GDD's purse sentence:** helping Inez and the Mercenary driver costs $90,000, not $80,000. It still fits the $100,000 purse.
- **Bubble length and count** (Glyph 2026-11-09, `briefs/101/glyph/r5-review.md`):
  - agent §10 says 10 words, while §12, the human GDD and text.schema say 12;
  - conversation lines now run to 25 words (Sky: "Full sentences");
  - "at most two" means two bubbles on screen at once, not two rows;
  - the §10 text becomes: "Speech bubbles at the speaker: at most two on screen at once, one per speaker (the newest), 3 s, twelve words or fewer. A conversation's street line keeps the cutaway's caps."
- **§01 and §10, an order's full price:** money, Trail, Alert risk and what the street will do, no longer time. There's no hit chance in play, and time is shown only for key events (Sky 2026-11-08, brief 101).
- **L9 Bodies and its give and take** (Sky's calls above), as a law with its settings: radius 30 cm, space 80 and 70 cm, crews 2.5 m, a squeeze at 40 cm and 0.5 m/s, 5 s before changing plans.
- **The walk law and seated listeners** (Sky's calls above): a gap a body visibly fits through is walkable, the solids are the pieces as built, and listeners sit.
- **§10 and §11, the ground view leaves [lab]** (Sky 2026-11-09, brief 101: "Button + eye"):
  - A round tab beside the selected agent's face takes control, as T does.
  - In control, the same slot is "Back to map", which T also does.
  - The eye over the driven agent (§10's "who sees you") ships with it.
  - Watcher pips, edge markers and the goal beam wait until the Handover is solid.
  - T14 loses its [lab] mark when it runs.
- **§10 controls:** Esc is always the pause menu (Sky 2026-11-09: "esc is always pause menu"). It never leaves a view.
- **§04, §05 and §07, calls** (Sky 2026-11-09, brief 102):
  - A call is offered only where it has a use under the rules.
  - Nobody acts on a call while they hold a memory of the cell's violence.
  - A call on a turned contact empties a post: their own if they keep one, otherwise their lover's.
  - §07's capstone line "the call that pulls the guard off his door is Inez's" gains "or his own, once the cell has turned him".
  - §07 route B: the driver's Help is $50,000.
- **§05, a turned staff witness:** the witness line says a turned witness "makes no report", but the reports list puts staff first, and the engine follows the reports list (Motive, `briefs/102/motive/turned-guard.md` §2.4). Pick one with Pulse.
- **§10 and §12, the agents' voices** (Sky 2026-11-08, brief 102):
  - The cell is quiet in play: no acks, arrivals, landings or warnings.
  - The agents speak only in scenes with people.
  - No line explains a rule or a consequence, and AI-sounding words fail.
- **§10, the hover outline** (Sky 2026-11-09, brief 101: "B"): whatever a left click would act on wears a thin ink outline with a fine cream edge (1.5 + 1 px at 1080p), the cell included, never a passer-by. It replaces the cream ring at a person's feet and the dashed stack rings; the stack dots and the pill stay.

## Done-check
Sky can set the guards, people and cars in the dev panel, press Play and get that world, with no rebuild. The GDD goes to v0.40.0 with laws in place of per-case sums, and the crew is regenerated (`bun run crew`). Traffic runs in play under L8, with the Handover's bands holding across seeds and 60 fps on the Air. The witness rules run on L1–L6.

## Evidence
This brief; the GDD diff; the band report (seeds, routes, win rates) before and after; an Air frame-time row with traffic; a play-zoom clip of traffic reacting to a drawn gun and a shot.

## Next owner
Keel, Pulse, Latch and Waymark for step 0 (the dial); Motive joins at step 2.
