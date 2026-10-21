# 031 · Everyone watches, and watchers notice loiterers

**Status: approved by Sky 2026-10-20** (D1–D3 and L1–L3; Sky's answers are at the end). Ready to build. Author: Motive. No rulebook, sim, text or GDD file has been edited. The measurements are in `briefs/031/`:
- `loiter-probe.cs`: scratch code that reads the sim library and changes nothing.
- `loiter-probe.txt`: the first thresholds (wary 15 s, report 30 s), reference routes.
- `loiter-probe-heat0.txt`: the approved Heat 0 band, every script.

Asked by Sky (2026-10-20):
- "all characters should be able to watch … I'd like to have all my players do basic recce at the beginning, so can formulate a strategy - also means it causes the player's cell to split up."
- "Scout can watch from further away, enforcer is right close to them, tech also far, fixer a little nearer … so scout can be outside camera / guard ranges."
- "Standing in an observed area for a while without moving causes alert. So standing behind a camera's LoS is fine … same for a robot (hence the digital face that changes expression on them)."
- "as the cell becomes infamous, then it draws attention quicker."

**Objective:** any agent can Watch; the reach is the agent's skill. Security watchers notice an agent standing still in their sight, faster as the cell's Heat grows, and show their attention on the street.
**Owner:** Motive → Keel.
**Done-check:**
- Keel accepts §1 to §3.
- The scenarios in §7 pass.
- After Waymark's re-siting, `bun run missions:check handover` SHIPS with MC-1 and MC-2 met.
- The probe finds no report on any quiet route.

**Next owner:** Aster (GDD v0.25, §6), then Keel.

## How each rule shows on the street (the readable test)

- **Watch:** picking Watch draws the selected agent's ring (Scout 30, Tech 24, Fixer 12, Enforcer 3 m). An order out of reach walks to the ring's edge first.
- **What watchers see:** you read it from the watchers themselves. A camera or a robot looks only the way its lens or face points, so behind it is safe. A person also senses anyone standing beside him.
- **Attention:** once an agent stands still in a watcher's sight, the watcher shows it. Its head or lens turns to the agent (noticed), then the lens pulses or the robot's face narrows (wary), then the lens flashes or the face shows its alarm pictogram (reporting). Walking on or stepping out of sight lets it cool.
- **Heat:** the cell's notoriety appears as one word on the briefing card ("unknown", "noticed", "wanted", "infamous"). In play it shows as how quickly the faces turn.
- **Talking:** a conversation never looks like loitering, so the faces stay calm while the Fixer talks.
- **What the player reads:** no rule in this brief puts a sentence on screen in play. The player reads rings, lenses, faces and heads.

## 0 · What is true today (engine 2026-10-20)

| | today |
|---|---|
| Watch | person: the Scout only, 6 m, with no line check. Camera and patrol: the Scout or the Tech, 18 m on a clear line. Container: the Scout. 20 s, $0, 0 T |
| standing in sight | logged as a sighting (investigators' paper; within 4 m of a contact it exposes the contact's job). No suspicion, no Alert |
| all-round sense (`near-m` 3 m) | walking guard, officers, response robots, the chasing patrol. Not cameras, the sweeping patrol or the posted guard |
| Heat | GDD §05/§06: Heat = campaign Trail + Menace standing. No campaign layer yet; no Heat in the rulebook or the start state |

## 1 · Watch: any agent, four reaches

- **`cell.watch`, `by` all four agents:** 20 s for everyone, $0, 0 T, no Alert of its own. Every watcher teaches the same facts: routine, a watcher's sight and check-in marks, a container's round, an affair seen together. The temperament stays Listen's.
- **The reach is the agent's**, one ring for people and machines alike, on a clear line at eye height (people included). Crowds block lines.

| agent | reach | can watch unseen |
|---|---|---|
| Scout | **30 m** | everything, from beyond every cone (the posted guard's is 26 m) |
| Tech | **24 m** | the cameras (18, 22) and a walking guard (18) from anywhere; the patrol (24) off its axis; the posted guard only from well to his side |
| Fixer | **12 m** | anything only from its side or behind |
| Enforcer | **3 m** | a camera or a robot from behind it (machines have no all-round sense). A person never: he senses anyone within 3 m |

- **The patrol's routine** (40 s) counts the union of watched time: two agents watching at once count once.
- **Listen (42 s, 3 m) and Mislead (44 s) stay the Scout's.** The Tech still can't Watch while his loop holds.
- **The stand spot:** a Watch out of reach walks to the nearest spot in range with a line. It never chooses by cones the cell hasn't learned.

## 2 · The attention clock

**Who has a clock:** every security watcher on the street: cameras (live, not looped), the door guard, the patrol robot, and the response's officers and robots. Each keeps one clock per agent. Civilians and rivals have none.

**What moves it:** each tick, for each agent:

| the agent | the clock |
|---|---|
| in the watcher's sight, standing still, idle or Watching | **+1 s per s** |
| in sight, in any other act (Listen, a turn, Call, Loop, Reroute, Enter, Take, a takedown, a carry: Sky L1) | **paused**; the act's own seen-rules apply |
| moving, or out of its sight | **−1 s per s**, down to 0, or down to the wary mark once wary |

**What "in sight" means (Sky D3, L2):**
- **Machines** (cameras, the patrol, the response's robots) see only through their cone.
- **People** (the guard, officers) also sense anyone within 3 m on a clear line, whatever their facing. That includes the posted guard, for the clock only. The takedown's instant awareness at the order stays today's cone, so a blow from his blind side still works; lingering beside him first does not.

**Stages:**
- **Noticed:** a warning only.
- **Wary:** for the rest of the mission; the clock never drains below the wary mark again. A wary person is aware of that agent for a takedown: unarmed, he struggles and screams; the armed guard beats it and draws (Alert at least 2). This is Sky's D1.
- **Report:** a loiterer reported. It counts as a found fault: Alert at least 1, through the report path (a camera at its desk's next scan, 20 s, plus 3 s; a person or a robot by radio, 3 s). The dial sends its Alert-1 response (S2: 2 robots). It is logged as a sighting, adds **no Trail**, and happens once per agent per watcher per mission.

### 2.1 The marks scale with Heat (Sky L3)

**Heat** = campaign Trail + Menace, where Menace = max(0, −Standing). It is read once, at the mission's start, and fixed for the mission (§05's definition). There is **no campaign layer yet**, so the start state carries it:
- a new start field, `heat`;
- mission data, `contracts/missions/handover/mission.json` `start.heat` (owner Waymark): the **Handover's is 0**. It is the first mission: campaign Trail 0, and Standing +2 leaning Hero gives Menace 0.
- a scenario may set it.

Once a campaign layer exists, it writes `heat` from the debrief.

| Heat | band (the briefing's word) | noticed | wary | report |
|---|---|---|---|---|
| 0 to 2 | unknown | 8 s | 20 s | 45 s |
| 3 to 5 | noticed | 6 s | 18 s | 35 s |
| 6 to 9 | wanted | 5 s | 15 s | 25 s |
| 10 or more | infamous | 3 s | 12 s | 18 s |

The rulebook block `attention.bands` holds `{ heatFrom, notice, wary, report }`, all [S], tuned by play.

- **Generous at the start.** In the unknown band a 20 s Watch from inside a sight line just reaches wary, and only a 45 s stand reports.
- **Wary at or under 20 s in every band**, so a whole Watch in sight always makes the watcher wary (Sky D1 holds at any Heat).
- **Only when infamous is the report (18 s) shorter than a Watch**, so a famous cell can no longer Watch from inside a sight line at all.
- **Live with it.** Using today's reference results (Trail, then Standing after word of mouth) as the next mission's Heat:
  - Help: 5 T, +4, so noticed.
  - B: 3 T, +3, so noticed.
  - G-kill: 4 T, +3, so noticed.
  - Pressure: 7 T, +1, so wanted.
  - G: 6 T, +2, so wanted.
  - Loud: 10 T, 0 → −1, so 11, infamous.

  How the Handover is won decides how fast the next mission's faces turn.

## 3 · The cues

The cues are world signals, drawn unlit and emissive, each with its own motion cue. They own no colours, and red is allowed on a small emissive display pictogram (§10, §15). A cue shows only once an agent is already in sight, so it never draws a cone the cell hasn't learned. It shows the watcher's highest stage towards any agent.

| stage | camera | robot (patrol, response) | person (guard, officers) |
|---|---|---|---|
| calm | lens dim | face display: open eyes, slow blink | idle |
| noticed | the iris glows and the head nods a small arc towards the agent (the cone does not move) | eyes turn to the agent and widen | head turns to the agent and holds |
| wary | the iris pulses slowly | eyes narrow, brows down; the head tracks | the head tracks; a small world mark over him (within ≤ 2 icons over a person) |
| reporting (until the report lands) | the lens flashes | the alarm pictogram | hand to the radio; one line of speech |

## 4 · L2: robots lose the all-round sense

The change applies to detection as well as to the clock: one rule for every machine.
- The response robots (`UnitSees`) and the chasing patrol drop `near`.
- Officers and the guard keep it.

**What it does to the routes: nothing measurable.** In every recorded script (36: quiet, loud, fail, careless, withdraw), no agent ever stands within 3 m of a response robot outside its cone (`loiter-probe-heat0.txt`, "robot-near-only ticks 0").
- Where it does show: a fight or a hunt after an alarm. An agent can now slip behind a robot at arm's length; an officer would still sense him.
- It makes the fight range's robots easier to flank, which Suture should look at in T2.

## 5 · Consequences per route (Heat 0 band, measured)

Today's engine, every reference route, the §2 clock at 8/20/45 s:

| route (c024) | what the clock finds | fix (Waymark) |
|---|---|---|
| **A, Help ×2** (4:30.2, 5 T, A 1) | at Inez's door, the Scout beside Inez for the Listen, and the Fixer's turns: **nothing** (no security watcher sees the courtyard or the terrace). The Enforcer waits 29 s in the entrance camera's view before the loop lands: wary, no report | wait out of the camera's cone; keeps him unknown to it |
| **A, Pressure** (3:41.8, 7 T) | as A, plus 23.8 s in the patrol's sweep: wary | as A; wait outside the sweep |
| **B, driver** (7:48.2, 3 T, A 0) | the Scout waits after the Listen at (82.5, −31.0) in the pickup camera's view: **report at 2:32.5**, so Alert 1 and 2 robots. The Fixer's Pay at the car is paused (L1) | the Scout waits out of the pickup cone; the route keeps A 0 |
| **G-fast** (3:47.8, 6 T, A 1) | the Enforcer stands 39 s within 3 m of the posted guard before the 2:03.6 knockout: **wary, so aware, so the knockout fails** (he draws). Also 37.9 s in the sweep (wary) | wait more than 3 m away, out of the sweep, and walk in for the blow (walking drains, the 6 s knockout pauses) |
| **G, G-kill** (4:15.2 · 5:42.2) | the entrance camera 29 s (wary). The Scout's Watch of the guard from inside his cone makes him wary of the **Scout** (harmless) | as A; put the Scout's Watch beyond 26 m |
| **loud** (4:34.6, 10 T, A 2) | the Enforcer waits 53.7 s in the entrance camera's view: **report at 1:09.8**, so Alert 1 and 2 robots before the shot at 2:31 | as A |
| fail and careless scripts | early reports in 13 of them (`loiter-probe-heat0.txt`); each still has to end with its expected fail code | Waymark re-checks each expected code |

- **Quiet still beats loud.** The clock adds no Trail (loud 10 T against quiet 2 to 7), and the re-sited quiet routes keep their Alert.
- **Nothing becomes trivial.** The turning still runs through the Scout's Listen (A, B) and Mislead (B). The faster recce clears the cards' "?" and changes no outcome: the cameras, the guard and Inez in about 20 s plus walking; the patrol's routine in 40 s, where today it takes about two minutes. Now each agent must stand where it isn't watched.
- **The Scout matters more.** It is the only one who can watch everything unseen.
- **A split cell is riskier through the clock itself.** Four agents spread out means four clocks to manage, on top of sightings, nobody to cover or carry a lone agent, the walk back to the car, and known faces.

## 6 · The GDD as v0.25 (for Aster)

v0.25.0 (Sky, 2026-10-20; brief 031): every agent Watches people, cameras, the patrol and containers, 20 s, $0, the same facts; reach Scout 30, Tech 24, Fixer 12, Enforcer 3 m on a clear line; a patrol's routine counts watched time once · watchers notice loiterers: each security watcher keeps a clock per agent, running while the agent stands still idle or watching in its sight, paused during any act, draining while moving or out of sight; noticed, wary (for the mission; a wary person is aware at a takedown), report (found fault, Alert ≥ 1, no Trail) · the marks scale with Heat at the mission's start (unknown 0–2: 8/20/45 s · noticed 3–5: 6/18/35 · wanted 6–9: 5/15/25 · infamous 10+: 3/12/18); the Handover starts at Heat 0 · machines (cameras, robots) see by their cone only; people also sense 3 m all round · cameras, robots and people show the stage on the street (lens, face, head) (§01 terms, §03, §04, §05, §06 Heat, §07, §10, §13, §15) · log in §12 decisions.

Edits by section:
- **§01 terms:** Heat also sets how fast watchers notice.
- **§03:**
  - Scout row: "Listen, Mislead; watches from furthest".
  - All ops: "Watch, 20 s, reach per agent".
  - The Tech's tether covers Watch.
- **§04:** the Pay row reads "routine known (Watch, any agent)".
- **§05:**
  - "once the Scout has watched it" becomes "once the cell has watched it".
  - The attention clock and its bands.
  - Machines have no all-round sense.
  - The counter: "keep moving, or stand out of sight".
  - The patrol union rule.
- **§06:** Heat also scales attention.
- **§07:** "an agent watches"; the re-sited routes per §5; refreshed reference results.
- **§10:** the Watch ring; the Intel chip names the agent; attention cues are world signals; the briefing's Heat word.
- **§15:** the robot face display, four expressions; the camera lens states.
- **§13:** §7's scenarios.

## 7 · Build list

**Keel (rulebook + engine).** Inputs: §1 to §4.
1. **Rulebook:**
   - `scout.watch` becomes `cell.watch`, with `by` all four.
   - New values: `watch-reach-{scout,tech,fixer,enforcer}-m` (30, 24, 12, 3).
   - Retired: `watch-sight-reach-m`, and the use of `act-reach-m` for a person's Watch.
   - `sight.kinds.*.by` becomes all four. Drop the "a person by the Scout alone" refine (`rulebook.schema.ts` 198/204, `Rules.cs` 479, `rulebook.test.ts` 404).
2. **Watch:**
   - The container Watch is open to all (`GameStashWatch.cs` 29).
   - A person's Watch needs a clear line.
   - The patrol's routine counts the union of watched time.
   - `only-scout` applies only to Listen and Mislead (`GameOrders.cs` 155, `GameSight.cs` 536).
3. **The `attention` block:**
   - `bands` from §2.1.
   - A clock per (watcher, agent), as in §2, with a wary set for the mission.
   - The report as a found fault through the report path and the dial.
   - Hashed state; the sight tests detection already runs, once a tick.
4. **Start state:** `heat` (int); read from the mission's `start.heat`, else 0.
5. **L2:** robot units and the chasing patrol drop `near`. The posted guard gets `near` for the clock only.
6. **Host API:** `Attention(watcher)` gives the highest stage and its target; `HeatBand` gives the briefing word.
7. **Scenarios:**
   - The N107 Fixer and Enforcer refusals flip to accepted.
   - Each reach: accepted inside it, too-far 1 m past it.
   - The Enforcer 3 m behind the entrance camera: no clock. 3 m in front: noticed at 8 s.
   - Heat 0: idle 45 s in a live camera's view → Alert 1 at its next scan + 3 s. Looped: nothing.
   - Heat 10: the same at 18 s.
   - Walking for as long as you stood drains the clock to 0; once wary, never below wary.
   - A Pay at the car in the camera's view is paused.
   - The Enforcer 20 s within 3 m of the posted guard, then the knockout: aware, it fails, he draws.
   - A robot doesn't sense an agent 2 m behind it.
   - Two agents on the patrol at once still need 40 s.
   - The Fixer's Watch on Inez → Pay shows "?".
8. **Done-check:** `bun test contracts/rules`, the sim scenarios, `missions:check handover` (after Waymark).

Next owner: Waymark.

**Waymark (mission + routes).**
1. Add `start.heat: 0` to `contracts/missions/handover/mission.json`, with its source: the first mission, campaign T 0, Standing +2.
2. Re-site the waits:
   - the Enforcer's before the loop lands (A, A-Pressure, G, G-kill, loud);
   - out of the sweep and more than 3 m from the guard (A-Pressure, G-fast);
   - the Scout's out of the pickup cone (B, B-block);
   - the Scout's Watch of the guard beyond 26 m (G).
3. Re-check the 13 fail and careless scripts' expected codes.
4. Add careless twins:
   - "the Enforcer watches the guard from 3 m, then the knockout" (fails, loud);
   - "B with the Scout idling in the pickup camera's view" (Alert 1).
5. Optional: a four-agent recce opener with A's chain unchanged.
6. **Done-check:** MC-1 and MC-2 hold, and the probe finds no report on any quiet route.

Next owner: Suture (T2, T3: does the recce cost something a player feels, and are the cues read in time?).

**Fable (text, `contracts/missions/handover/text.json`).**
1. `only-scout` (line 396): "Only the Scout can listen.", short "Scout only", verbs `listen` and `mislead`. Drop the machine variant.
2. Hint (line 892): "The door has a guard. Watch the people on the café terrace."
3. Watch `ack` lines for the fixer and the enforcer: 3 each, ≤ 4 words.
4. "Too far · n m" carries each agent's reach.
5. A first-noticed hint (≤ 6 words, retired once seen): "Seen. Keep moving or step out."
6. The guard's speech at reporting (≤ 4 words): "Desk, I've got one."
7. The four Heat words for the briefing: unknown, noticed, wanted, infamous. These are a proposal; plain copy (brief 019) decides.
8. **Done-check:** `bun test contracts/missions/handover`.

Next owner: Latch.

**Glyph (interface).**
1. A board of the four stages for each kind: camera lens, robot face pictograms, the person's mark.
2. The four Watch rings.
3. The briefing's Heat word.
4. The rule: a cue never draws an unlearned cone.
5. **Separate, Sky's L1 idea, not this brief's rule:** the conversation close-up camera (third person, skippable). Glyph and Latch own it; Aster takes the open question to Sky. Rules view: mission time keeps running during the scene, because the act's time is its price and the clock is paused by the act anyway.
6. **Done-check:** Sky picks from pictures.

Next owner: Vela, Latch.

**Vela (environment kit), with Glint (emissive levels) and Forma (heads).**
1. The robot face as a kit piece: one emissive display, a material with four states (calm, noticed, wary, reporting) and a short blink loop at rest. Status candidate in `contracts/world/kit.json`.
2. The camera lens material: dim, glow, pulse, flash.
3. Forma: a head-turn and track clip for the guard and officers, and the robot head's yaw.
4. **Done-check:** `bun run kit` hash-checked, plus renders for Sky.

Next owner: Latch.

**Latch (Unity), after the above.**
1. `HudToolkit.Intel.cs` `IntelNext` (147–154): offers the selected agent if it can Watch, else the idle agent with the shortest time to learn (walk to its ring + 20 s), ties to the Scout. Listen stays the Scout's. Ranks at 181 and 204 follow `by`.
2. The Watch ring.
3. Drive each watcher's cue from `Attention`.
4. The briefing's Heat word.
5. **Done-check:** Unity compile, console clean, and a capture of an agent idling in a camera's view through all four stages.

**Also informed:**
- Vantage: 30 m clear-line spots onto the posted guard and the patrol, and a wait for the Enforcer out of the entrance cone and the sweep.
- Redline: the clock's cost.
- Suture: the playtest; robots flanked at arm's length on the fight range.

## Sky's answers (2026-10-20)
1. **Being seen watching makes the target wary: yes.** And wider: "Standing in an observed area for a while without moving causes alert. So standing behind a camera's LoS is fine - they would have to look for visual cues on that, same for a robot (hence the digital face that changes expression on them)." So: loitering still inside a watcher's sight for a while raises suspicion then Alert; out of its line of sight (behind a camera, outside its cone) is safe; the player reads each watcher's attention from cues on the street (a camera's cone and lens, a robot's digital face changing expression as it grows suspicious).
2. **Watch time 20 s for everyone:** yes ("they save that time by not walking closer").
3. **Enforcer's reach:** follows from 1: machines see by their cone only, so an agent behind a camera's line of sight is unseen even close up; the "seen within 3 m whatever way it faces" sense applies to people, not cameras or robots.

## Sky's answers on L1–L3 (2026-10-20)
- **L1: yes,** a conversation (any act) pauses the loiter clock. Plus a new idea: during a conversation the camera goes close (third person; first person is outside the fixed camera limits and needs a GDD discussion first) to show the dialogue play out; the player can skip or return to realtime. Open: does mission time keep running during the scene (Aster's recommendation: yes, the act's time is its price; the scene is only a view), or stop?
- **L2: no all-round sense for robots** (cone only): "it's retro future, so robots look futuristic, mindset is still quite old school, forward facing cameras not 360". One rule for all machines.
- **L3: thresholds scale with the cell's Heat:** "if the heat is low, then it's not really an issue, but as the cell becomes infamous, then it draws attention quicker". Start generous for a low-Heat cell (the Handover is the first mission); tune by playing. Sky: "hard to know without playing".
- Sky's worry: rule complexity. Answer: every rule must show on the street (cues, not text); the fight range and play sessions are the test.
- **Conversation scene timing (Sky, 2026-10-20):** mission time is **paused** while the close-up conversation plays ("since the player can't move"); going back to plan mode or continuing resumes the game, and the act's time then runs as priced. The pause changes nothing the engine records, so replays are unaffected. Glyph specs the scene, Latch builds it.

## Robot faces and the attention callout (Sky, 2026-10-21)
- **Old robots keep their own faces:** the old city's waiter, porter, shopkeeper and butler robots show the four attention states on their own face displays (the old glyph table), not a new head.
- **The patrol robot:** there isn't one yet, so Vela's new `robot_face_display` head becomes the patrol robot.
- **Legibility:** rather than enlarging heads, "robots we can zoom in, or do a sort of PiP kinda feature showing when something changes state, a callout or in corner": when a watcher (robot, camera, guard) changes attention stage, a small picture-in-picture shows its face/lens close up, either as a callout beside it or in a screen corner. Glyph designs it within the HUD budget.
- **Inset placement (Sky, 2026-10-21): try A,** the callout above the watcher on a short stem with a mini portrait naming the agent (Glyph recommended B; A for now, revisit after play). Off-screen watchers: no inset for now; the portrait's live face covers them.
- **Robot face colours (Sky, 2026-10-21): yes,** each old robot keeps its own LED colour (amber on butler and porter, teal on waiter and shopkeeper) for stages 0–2; seafoam on the patrol robot; red only for reporting.
