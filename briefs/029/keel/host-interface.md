# Brief 029 · The fight range: host interface (Keel → Latch)

Engine `suborned.sim/0.10.0`, DLL source `sim/Suborned.Sim` (new: `GameRange.cs`, `ScenariosRange.cs`). The range is the same engine and the same rules. Only the site differs. Nothing in the Handover moved: all 254 Handover scenario and route hashes are byte-identical (`out/keel029/hashes-{before,after}.json`).

## 1. Inputs: a range run never loads the Handover's files

| file | what |
|---|---|
| `sim/data/range.site.json` | layout **open** (18 pieces, 58 cover points) |
| `sim/data/range-dense.site.json` | layout **dense** (31 pieces, 104 cover points) |
| `contracts/rules/rulebook.json`, `contracts/people/people.rules.json` | the same two files the Handover uses |

Both sites are written by `bun tools/sim/range.ts`. `--check` exits 1 if a file is stale, and `tools/sim/range.test.ts` checks this too. Never edit the sites by hand.

```csharp
// headless: Engine.LoadRange(root, "open" | "dense")
var inputs = Engine.FromTexts(rulebookText, peopleRulesText, rangeSiteText);   // Unity: from StreamingAssets; no people/provisional/takedown/rounds
var start  = Start.Parse(presetJson["start"]);                                 // briefs/027/presets/today.json or lab.json, as in the lab
var g      = Engine.New(inputs, start);                                        // g.Range == true; g.FightBranch == "range"; g.FightOn == true
```

- **Restart (R):** build a new `Game` from the same inputs and start.
- **Switch preset:** a new `Game` with the other preset's `start`.
- **Switch layout:** a new `Game` with the other site.
- **The host part of each preset** (fightMode, freeze triggers, targetPlayback) is yours, exactly as in brief 027. Freeze and target mode stay host time only (stop calling `Step`).
- **Slow time ½×:** step at 5 Hz wall time. The ticks are identical.

**Unity copy (yours: I did not touch `tools/sim/unity.ts` or `unity/`):**
- Add both range sites to `unity.ts` INPUTS so they land in `StreamingAssets/sim/`.
- Optionally play `range list` / `range play <id> --json` into `headless.json` as a `range` block. That gives the Unity match a range set too.
- **Caution:** `unity.ts` exports from HEAD by default, and the range files are not committed yet. Use `--tree` until they are.

## 2. The floor and what to draw

**Frame:** integer cm. x runs along the range from the cell's end; y runs across.

**The floor:** x 0–4500, y 0–4000 (45 × 40 m). Draw the 1 m grid on it.

**The mark:** `g.RangeMark`, place `range:mark` at (300, 2000). It is the centre of the cell's line.
- Draw the 5 m distance rings about it.
- Every spawn distance is measured from it.
- Per-agent reach rings stay as in the lab: `op.Gun.W.EffCm` / `MaxCm`.

**The pieces** are `S.Occluders` whose id starts with `range/`. Each has an integer-cm footprint polygon, `Z` 0 and height `H`. Read the kind from the id prefix:

| id prefix | height | the engine's cover (tools/cover/gen.ts rule, not authored) | label |
|---|---|---|---|
| `range/low-wall-` | 100 cm | **partial** (q 429), shoot over it | low wall |
| `range/car-box-` | 145 cm | **full** (q 652 to 720), shoot over it | car-sized box |
| `range/pillar-` | 300 cm | partial (q 350), **blocks sight** | pillar |
| `range/high-wall-` | 250 cm | full behind its long faces, **blocks sight** | high wall |

**"Half / full" mapped to the engine's rules:**
- A low wall is partial cover: 750 ‰ today, −1 pip with the lab.
- A high wall hides you completely: no line, no shot either way.
- A car box is full cover you can shoot over: 500 ‰ today, −1 pip with the lab.
- A pillar hides you like the high wall, and its edge points are partial.

**Cover marks:** `g.S.Cover` (points with `At`, `Faces`, `Cos`, `Q`, `Of`), the same as the Handover's. `CoverMarks(op)` and `PreviewSnap` work unchanged under the lab.

**Do not draw:**
- places `wings:*`, the two cameras `entrance` / `pickup`, the cast (`People`), the guard, patrol, Vale and the car;
- the drive grid.

These are the site format's required Handover keys, parked in the wings (x 49–61 m), where no walk from the floor reaches. The range never reads them.

## 3. Spawning: an order, so it replays

```csharp
g.Issue(new Order { Tick = g.Tick, Do = "spawn", Target = "police", To = "cover", Count = 3 });
// Target: police | rivals | response      To: near | eff | beyond | cover      Count: 1..6 (0 reads as 1)
// script form: {"tick":0,"do":"spawn","target":"police","to":"cover","count":3}   (no "op")
```

**Slots** are site places `spawn:<to>:1..6`, filled first-free in order. `g.RangeSlot(to, k)` gives each stand. Slot 1 is nearest the firing line, then they alternate ±.

| to | distance from the mark (as built, open) |
|---|---|
| `near` | 7.8–8.1 m |
| `eff` | 14.7–15.1 m: the effective ring (hostiles' and today's pistol eff 15 m) |
| `beyond` | 17.8–18.0 m: past every hostile's 15 m and the lab sidearm's 16 m max, inside today's pistol 25 m |
| `cover` | 20.9–24.1 m, on a cover point a body fires over (low walls and boxes, never behind a high wall or pillar). The hostile holds that point. |

**A slot is held** while a hostile up stands within 1 m of it, or holds it as its stand (police, response).

**Refusals** (existing codes; Fable needs no new lock line):

| code | when |
|---|---|
| `target-unknown` | the side is unknown |
| `no-place` | the placement is unknown, the count is out of 1..6, or fewer free slots than asked |
| `unknown-verb` | spawn in a Handover run |

**What each side does** (the final fight's machine, `GameAi.FightBody`, plus `GameKite` with the lab kite):

| side | ids | at spawn | then |
|---|---|---|---|
| police | `officer-n` | on the slot; state `hold`; trained; health-officer | Engages any agent inside its 15 m effective ring on a line, and holds the slot as its stand. With the lab kite, if shot from beyond its reach and *unshielded*, it takes cover toward the shooter or closes to its ring. |
| rivals | `rival-n` | on the slot; `alerted` for weapons.aimNew; untrained; health-rival | Engages, or **advances on the mark** by the site's cover. It **breaks** when more than half of *its own spawn* is down, runs to `range:retreat`, and is gone. |
| response | `response-n` | at `range:entry` (the far end, 40 m out); `advance` | Walks to the slot as its stand, stops to engage the first agent inside its ring, holds. |

**The car-contest rule** (`fight.lab.policeContest`) belongs to the loud final fight's car, and the range has no car, so it is **not applied**: police stay on the slot you put them on. Where the AI reads "the car" (rivals' objective, the police kite cover limit), the range gives it the mark.

**There is no hold, no blocker and no Board/Go.**

## 4. Orders the range takes

- **Taken:** `move`, `drive` (third person), `cancel`, `draw`, `holster`, `cease`, `stance`, `fire`, `volley`, `field`, `panic`, `carry` (a downed agent), `spawn`, `withdraw`.
- **Refused `unknown-verb`:** everything else.
- **A fire or volley** names only a spawned hostile (else `target-unknown`). Every lab rule applies as in the Handover: close, then, charge, volley, field, snap, pips, kite.
- **Nothing is traced:** Trail, Alert, Gunfire and Standing stay at their start, and nothing reports.
- **The range never fails:** the Enforcer down is any agent down, and nothing ends at the close. `g.Result` stays `None` until `withdraw`.

## 5. Reads (read-only, never hashed)

```csharp
List<RangeHostile> hs = g.RangeHostiles();
// every spawned hostile in spawn order: Id, Side (police|rivals|response), Pos, Facing, Health/Max, Wave, State (alerted, advance, engage,
// hold, break, gone, down), Target, Cover, Moving, Down, Gone        → draw all but Gone; health bars as the lab (Health/Max, ×10 with lab)

List<FireLine> lines = g.RangeLines();            // "show the numbers": every line of fire now
// agents (up, armed) → each hostile up, then each hostile → each agent:
// Shooter, Target, From, To, DistanceCm, Band (point|eff|long|out), Line (within max on a clear line), InReach (would fire now:
// agent = Line; hostile = inside its eff on a line), Aimed (its aim is on this target), Acc ‰, Pips (lab), Damage,
// Cover (none|partial|full as the shot counts it now), CoverId, CoverLost (a point shields but the target walks), Moving, Untrained
```

- **`Acc` is the engine's chance** for the next shot on that line: the same function the shot uses. R03 and R04 check that the logged shot lands at exactly `Acc`.
- **For the overlay:** draw `InReach || Aimed` lines (or all lines from the selected agent). Label them pips (lab) or ‰ (today), plus band and a cover glyph. Keep it to one line each.
- **The lab's reads all work unchanged:** `PreviewFire`, `PreviewVolley`, `PreviewAnswer`, `Combatants()`, `CoverMarks`, `ChargeOf`, `FieldOf`.

**New log codes:**
- `range.spawn`: n of a side at a placement, with their ids.
- `range.clear`: every hostile is down, broken or gone.
- The freeze triggers from 027 still come from `shot.*`, `<op>.down` and so on.

## 6. Replay and records

- `Script.Write(g, inputs.Hash)` writes `"mission": "range"` and each spawn's `count`.
- `bun run sim range play <script.json> [--layout dense]` replays it and verifies the record.
- **CLI:** `bun run sim range list | play <R01…|script> [--json] | script <id> | hashes`.

## 7. Behaviour worth knowing before Sky plays (for Suture and Motive)

1. **Today, police behind cover at 20 m never fire** while the cell stays more than 15 m away (R08). Today's police hold their stand, and their effective reach is 15 m.
2. **With the lab, the reach matters.** A sidearm agent's fire order at 20 m walks him into his 15 m ring, which is inside theirs, and they answer at once (R07: answered at 4.8 s).
3. **Probe (`out/keel029/probe-*.script.json`): the long-range kill.** The Enforcer's carbine, firing from the start line at 20 m (lab eff 20 / max 30; today max 30), downs officers in cover in both presets, and they never fire back.
   - Kite rule 3 (Motive tuning.md §7.2) keeps a *shielded* final-fight police body where it is.
   - That is the rule as written, not a bug, but it is exactly the "cheap win from outside reach" the kite was meant to close.
   - Question for Motive and Pulse: should range police (or final-fight police generally) answer a shooter beyond reach even when shielded?

## Evidence (2026-10-20)

- `dotnet test sim/Suborned.Sim.Tests -c Release`: 1009/1009 pass. That includes R01–R10 ×3 (passes, same checksums twice, replay) and 5 range facts.
- `hashes` before and after: identical, 254/254 Handover runs.
- `bun test tools/sim/range.test.ts`: 4 pass.
- `bun run typecheck`: clean.
