# 085 · A discreet Listen: she comes near the Scout and talks to someone

Motive, 2026-11-01. Reworked around Sky's "We could have Inez walk near scout and talk to another NPC". A report: nothing in `sim/`, `contracts/` or `text.json` changed. Measured on a scratch copy of HEAD (engine 0.38.0) in `out/scratch/motive/085l/`, seeds 1-500 at human pace, through `bun run guard`. The probe and raw tables are in `briefs/085/motive/probe-listen/` (`results-near-scout.txt`).

## The rule

Clicking Listen on someone, the Scout takes a spot her walk will soon bring her near. When she comes within reach she stops and talks to someone she knows. He listens from the edge. **Listening itself is never noticed.**

| Part | Value |
|---|---|
| Reach | **5 m** (`listen-reach-m`, 3 m today) |
| Spot, someone on rounds | 1.5-5 m from where she'll be, reached before she gets there |
| Spot, someone who stays put | 3-5 m from them |
| Time | still 30 s; she stands still for it + 3 s, as today |
| Trail, Alert, witness | none, as today |

**Choosing the spot, for anyone on any mission:**
1. Read ahead along the person's known walk, second by second, and take the earliest moment the Scout can already be in place.
2. At that moment, take the spot nearest the Scout: a free chair if one comes up within 10 s, else free ground beside something solid, on a clear line. Out of every watcher's cone, either way.
3. A Scout already 3-5 m off and beside something solid stays put, so the player can place him and be right.
4. Nothing found: walk in as today and stop at reach. It fired in none of the mission plans; only for the guard at 4 m.

**The talk partner, view only, in this order:**
1. Someone tied to them, on the street and near.
2. Someone of the same job on shift at the same place, stepping out of the door. A mission may name one.
3. Nobody: they talk on a handset, voice only.

## 1. Who's the other person?

**Milena, stepping out. Nobody tied to Inez is on the terrace.**

| Person | Tie to Inez | During the mission |
|---|---|---|
| Milena Rask `p-cafe-12` | none in the file | server, inside the café (`out 0`) the whole mission |
| Freya Antoniou `p-cafe-05` | workmate | `beyond`, off the sheet all mission |
| Layla Sokol `p-cafe-21` | workmate | at home until long after the mission |
| Okafor | lover | at his door, and the subject of the talk |
| Terrace customers | none | seated, strangers to her |

Six more staff work inside. Inez is the only server on the street.

- **It can stay Milena.** Her lines stand as agreed, with no re-attribution. The "Milena, inside" label changes (Fable).
- **No new tie.** A workmate tie would change who Inez tells in the debrief, so the partner reads job and place instead. The mission names Milena so the lines match.
- **Pulse:** nothing, if she stays view only. Putting her in the routine with `out 1` would make her an engine walker: she'd join the crowd, block sight and count as a witness, and everything would change for no gain.

## 2. In the engine or view only?

**The stop already exists, so her walk needs nothing new.** Today a Listen holds her still for its 30 s + 3 s (`RoundHold`, already hashed). The rule uses that hold where she first comes within 5 m. There's no new stop and no detour: she doesn't go out of her way to him.

**What the player sees in the 30 s:** she's walking her round, she stops near the Scout, and Milena comes out to her. They stand and talk, then Milena goes in and Inez walks on. She is paused, not walking on. Letting her walk on would take her out of reach partway, and the talk couldn't be seen.

| Engine (hashed) | View only |
|---|---|
| Reach 5 m | Milena: her figure, walk out, stand, walk back |
| Spot choice and the wait for her | Facing: Inez and Milena to each other, the Scout side on |
| Hold keeps her own facing, no longer turning to him | Sit clips, the slide to a chair |
| Waiting at the spot holds the attention clock | The scene's framing |

**Milena costs one honest limit:** the engine doesn't know her, so she can't be clicked, shot, or block a cone. Latch should make her unselectable and send her in if a fight starts.

**Her walk:** Inez stops a median 7-8 m from the door (14.6 m at worst), so Milena takes about 5-6 s (10 s at worst), well inside the 30 s.

## 3. Numbers

Same plans and seeds as the last table, as written:

| Plan | Win today | **Win, 5 m** | Heard today | **Heard, 5 m** |
|---|---|---|---|---|
| `quiet-a` | 99.0 | **98.8** | 1:14 | **1:13** |
| `thin-quiet` | 86.6 | **86.4** | 1:22 | **1:21** |
| Prepared Pressure | 95.0 | **94.2** | 1:14 | **1:13** |
| Helped, then pressured | 97.8 | **98.2** | 1:14 | **1:13** |
| `quiet-b` (driver) | 91.2 | **91.2** | 1:27 | **1:29** |
| `quiet-g` (no Listen) | 74.2 | **74.2** | none | none |

Differences under about 2 points are noise at 500 seeds.

**The +19 s wait is gone.** Order to heard is about 33 s, as today (longest 47.5 s, in `thin-quiet`). Plans that skip the walk to `beside-inez` and just click Listen also hold: `quiet-a` without it wins 98.8, heard at 1:16.

**Why 5 m:**

| Reach | Result |
|---|---|
| 3 m | can't serve someone who stays put, since the 3 m minimum leaves no room: `quiet-b` falls to 0 % |
| 4 m | wins hold, but plans that skip the walk hear at 1:26 instead of 1:16, and the guard has no spot |
| **5 m** | wins hold, no wait, and the driver and guard both get a spot |

**The trade: he mostly leans.** Few chairs near her path are free. Of the ways I ran:

| Way | He sits | Heard, `quiet-a` | Win, `quiet-a` |
|---|---|---|---|
| **5 m, earliest spot (the rule)** | 0-3 % | 1:13 | 98.8 |
| 5 m, waits up to 30 s for a chair | 1-23 % | 1:13, longest wait 62 s | 98.8 |
| 5 m, chair first, up to 60 s | 19-79 % | 2:09 | 98.6 |
| 7 m from her door, last spec | nearly always | 1:33 | 97.8 |

**People who stay put:** the same rule with no read-ahead. All 500 heard, from a spot 4.7 m away. The partner steps out to them.
- **Guard:** order to heard 36.5 s to 40.3 s. His one street tie is Inez, so the partner is a clerk from inside.
- **Driver:** 76.2 s to 75.9 s. His workmates are Vale and a guard, so a guard steps out or he uses a handset; Fable's call.
- **The scene:** only `listen-inez` has a written conversation today. The others need one before this reads as a talk.

**Nothing here is one right way.** The spot follows when the player clicks and where the Scout stands. The player can also place him by a wall first.

**Official bands (200 seeds):** the same 6 misses and the same R-7 failure with the rule off and on. `thin-quiet` stays in band (86.5 %). The new B-11 miss from the 7 m version is gone.

## 4. "Listening shouldn't get noticed"

Kept from the last version, and still true at 5 m.

**Today a Listen already costs nothing and draws nothing:**
- `scout.listen` in the rulebook: `private: true`, price is time only, no witnesses, no `also`, and its one outcome only teaches.
- Attention: an act in sight holds the clock (`AttnInAct`, `GameAttention.cs:64`).
- Played: on seed 3 the ledger had no entry at the Listen; the first Trail came with the turn.

**The "+4" isn't the Listen's and isn't a Trail total.** I haven't seen that shot. The feed's small "+N" counts the extra lines in a conversation card (`HudToolkit.cs:916`). A Trail "+N" appears only on a price above 0 (`HudModel.Less.cs:94-96`), and a Listen's is 0. Glyph can confirm.

**The one gap the rule opens:** waiting at the spot would count as idling, so the rule holds the clock instead.

**Still seen, unchanged:** idling or Watching in a cone before or after the Listen; a run; a drawn weapon, carry, takedown or gunfire; a clock already at noticed or wary; a report already on its way.

**To know:** the cone test reads true cones, even ones the cell hasn't learned. It fits "a scout is a scout", but the Scout knows more than the player.

## 5. Seats and art

- **A walker can already hold a chair**, and often does, so the choice checks each walker's schedule across the whole Listen.
- **Sitting costs no time:** Forma's 2.3 s clips play inside the 30 s.
- **The engine must know:** each chair's name and walkable point (baked from `handover.seats.json`), and the chosen spot. Chair points are in blocked cells, so the Scout's engine position is about 1 m off and the view slides him on.
- **Art:** ships with `sit` and `idle`. Leaning is `idle` side on, and a lean clip would be nicer, not necessary. Latch must confirm the Scout's figure has the sit clips. Milena needs a figure in the café's look (Forma, Latch).
- **Vantage:** no new points. `beside-inez` is no longer needed, and Waymark can drop it.

## 6. What re-records

With the rule on, against 49 saved scripts, 385 scenarios and the sim tests:

| What | Count | Detail |
|---|---|---|
| Hashes | 49 of 49 | any rulebook edit |
| Outcome changes | 6 | `route-a`, `route-a-board`, `route-a-disable`, `route-a-pressure`, `route-a-pressure-board` go from win to `entry-seen-guard`; `careless-loop-runs-out` changes fail code. Re-compose from plans. My reading is that their fixed ticks now run ahead of the Listen; I didn't trace it |
| Scenarios | 3 | N131 and PACE-SPLIT to rewrite; DP1 re-records with `drive-session` |
| Sim tests | 3 of 2,765 fail | all `ScenarioTests.Passes`, 0 with the rule off |

Not run: the Unity proofs (`ProofDone` and `ProofHold` watch Listen's walk in), and the card's walk time, which must read the walk to the spot.

## 7. Decisions for Sky

1. **Milena steps out, drawn by the view only?** Recommend yes: the agreed lines stand and no saved run changes because of her.
2. **Inez pauses for the talk, as she does today?** Recommend yes: walking on would take her out of reach.
3. **Wall now, or wait for a chair?** Recommend wall now, sitting when a chair happens to be free: no wait, but he rarely sits. If he should sit more, the 7 m version does it at +19 s, or Pulse could keep one chair near the door free (not measured).
4. **Someone who never walks, such as the guard?** Recommend the same rule: the Scout takes a wall 3-5 m off and a partner steps out to them.

## What I'm unsure of

- **The look:** nobody has seen any of this. Milena walking out may look staged, and 5 m may look far.
- **Fairness:** the plans are bots at measured human pace, and none of them picks its moment for a chair.
- **Facing:** the view's facing for Inez differs from her engine facing during the talk. No witness rule should notice at Alert below 2, but I didn't test the view.
- **Which chair:** the probe logs the kind of spot and its distance, not the chair's name.
- **Spot search cost:** a grid scan for each second ahead, up to 2 minutes, when the spot is picked. Not timed; Keel should check.

## Next owners

- **Keel:** the rule, after Sky's answers. Done when `bands` reads as above and scenarios pass.
- **Waymark:** re-compose the 6 scripts; drop `beside-inez`; name Milena as the mission's partner.
- **Fable:** Milena's label, the last line as the Scout's report, and scenes for other Listens.
- **Latch, Forma:** Milena's figure, the sit and side-on looks.
- **Glyph:** the Scout's "waiting" label; the "+4" shot.
