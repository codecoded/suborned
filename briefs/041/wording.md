# 041 wording pass: the "Click walks, hold acts" strings (Fable, 2026-10-22)

For Latch (Unity) and Sky. Source: `testbed/controls/*.js` (reference), `HostHoldStatus.cs` and `HudToolkit.Hold.cs` (what Unity says today). Plain-copy rules applied: say what is happening, one word for one thing, no rule words, no key legend, as few words as the HUD budget allows (three or more words in a row on screen count as running text, so every line below is a price line or a cue, never a sentence).

## The four decisions that matter (the rest are small)

1. **The price must not vanish while you hold.** Today the pill reads `Hold · Watch the guard · 20 s`, then switches to `Hold to watch`, then `Let go to watch the guard`: the price disappears at the moment you commit, against "every order shows its full price before you commit". Keep one line the whole way and change only its first word: `Hold · …` while it fills, `Let go · …` when it is full. The fill bar already says "not yet / now". (If Sky likes the sentence, `Let go to watch the guard` is the fallback; keep the price after it.)
2. **One preposition for firing: "at".** text.json said "Fire on", the playground "Fire at". text.json now says "Fire at" everywhere (order, activity, Alt lines). Unity hardcodes `Fire on {label}` in `HandoverHost.cs` (lines ~830, 843): change to `Fire at`.
3. **One word for "can't reach" and one for "can't see".** The game already says `Too far · {m} m` (Watch, Ask, Pay) and `No clear line` (Watch, Fire). Fire said `Out of range` and `No clear shot`. Use `Too far` and `No clear line` everywhere; the plate gets the first two words (`too far`, `no line`). text.json's `out-of-range` lock now reads `Too far to fire. This weapon reaches {m} m.` / `Too far · {m} m`.
4. **"Weapons free" off is "Return fire only", not "Hold fire".** `Hold fire` is the name of the per-agent stance (never fire), and the toggle off does not stop anyone, it only stops them picking targets. On: `Weapons free`. Off: `Return fire only`.

## The table

| # | Where it shows | Current | Proposed | Why |
|---|---|---|---|---|
| 1 | Pill, hover, left-hold act | `Hold · Watch the guard · 26 s` | keep | Says how and what it costs. Drop `Hold ·` for a held-button glyph if the budget ever needs the room. |
| 2 | Pill, hover, fire target | `Right hold · Fire at the officer · down in 4 s` | keep | Plain, Sky's own word ("right hold"), carries the price. |
| 3 | Pill, left hold filling | `Hold to watch` | `Hold · Watch the guard · 26 s` (the offer line, bar filling) | Decision 1. No flicker, the price stays, the target stays named. |
| 4 | Pill, left hold full | `Let go to watch the guard` | `Let go · Watch the guard · 26 s` | Decision 1. Same line, first word swaps; ≤ 6 words with the target. |
| 5 | Pill, right hold filling (fire) | `Hold to fire · bystander · down in 5 s` | `Right hold · Fire at the officer · down in 4 s` | Decision 1. For a civilian see row 7. |
| 6 | Pill, right hold full (fire) | `Let go to fire at the officer` (7 words) | `Let go · Fire at the officer · down in 4 s` | Decision 1; the 7-word sentence is over the line budget, this is a price line. |
| 7 | Pill, firing at a passer-by (range) | `… · bystander` | name them in the line: `Right hold · Fire at a passer-by`; tint warning | "Bystander" is the engine's word; the game says passer-by (`less.pop.passer-by`). The cue is the name and the warning tint, not a tag. Civilians are never the payload: this stays a range/test capability, no encouraging copy. |
| 8 | Pill, chip "seen there" (stand spot in a known cone) | `seen there` | `Watched spot` | "Seen there" reads as a past sighting; it means the spot is watched. |
| 9 | Pill, walking to an act | `Walking there · 9 s` / `Running there · 9 s` | `Walking · 9 s` / `Running · 9 s` | "There" adds nothing: the pill hangs on the target. |
| 10 | Pill, acting | `Watching · 12 s left` | keep | "left" tells a countdown from the price shown before. |
| 11 | Pill, several doers | `[face] walking 9 s · [face] watching 12 s` | keep (lower case, faces carry who) | Reads at a glance. |
| 12 | Pill, just ordered | `Watch the guard · ordered` | keep | Short, confirms the commit. |
| 13 | Pill, finished | `Done · learned their round` | keep | Says what you got. Done words stay the `doneText` set (paid, call made, camera looped, taken…). |
| 14 | Pill, stopped | `Stopped` | keep | One word, same everywhere (also the fire stop; drop the log-only `Cease fire · X`). |
| 15 | Fire pill, drawing | `Drawing…` | `Drawing` | The ellipsis looks like loading. Matches the plate word `drawing`. |
| 16 | Fire pill, aiming | `Aiming · 1 s` | keep | |
| 17 | Fire pill, quiet fight pill | `Firing at the officer · down in 3 s` | keep, and use this one form outside fights too | Outside a fight it still adds `· hit · 1 of 3`; the tracer and the bar already show hits (Sky: keep the Syndicate simplicity). |
| 18 | Fire pill, target falls | `Officer down` | keep (`Guard down`, `Rival down`) | |
| 19 | Fire pill, blocked | `Out of range` / `No clear shot` | `Too far` / `No clear line` | Decision 3. |
| 20 | Plate (two words max) | `9 s`, `aiming`, `firing`, `drawing`, `queued`, `too far`, `no line` | keep all seven | Plain, two words at most. `no line` is the shortest honest form of `No clear line`. |
| 21 | Plate, under fire (siren, no other words) | `Under fire` | keep | Plain, two words. Per engine 0.20 it appears only for the agent's own causes: a holstered agent never seen acting gets no cue, so no copy may say "everyone is a target". |
| 22 | Roster toggle, weapons free on | `Weapons free` | keep | Sky's word; Tech's panic call already shouts it. |
| 23 | Roster toggle, weapons free off | `Hold fire · focus clicks only` | `Return fire only` | Decision 4. The playground file is swapped (`app.js`, `toggleFree`). |
| 24 | Roster toggle tooltip | `Weapons free (G)` | keep; Alt line = text.json `actions.free.does`: `In a fight, drawn agents pick their own targets. Off: they only return fire.` | Short at rest, the rule on Alt only. |
| 25 | Squad, select all | `Squad: all four` / `Squad: Scout, Tech` | keep | |
| 26 | Squad, pause plan waiting | `Paused: the orders start together` | `Paused · orders queue` | Say what happens to the orders you give, not the rule. No key named. |
| 27 | Squad, plan released | `Go: 3 orders start together` | `Started · 3 orders` (`Started · 1 order`) | `Go` is already the getaway order (text.json `go`); two meanings for one word. |
| 28 | Camera follow on / off | `Camera follows` / `Follow off` | `Camera follows` / `Camera free` | `Follow` is also Follow fire and Follow view; "off" says nothing about what you have now. |
| 29 | Portrait dropped on nothing | `Portrait drop: cancelled` | `Nothing there` (existing pop `nothing-there`) | "Portrait drop" is a design word. Reuse the pop. |
| 30 | Ring, held harmful wedge | `Hold to kill` / `Hold to knock out` / `Hold to fire` | `Hold · Kill` / `Hold · Knock out` / `Hold · Fire`, then `Let go · Kill` when full | Decision 1, same two-word grammar as the pill. |
| 31 | Attention clock over an agent | `seen` `noticed` `wary` `report` | `seen` `noticed` `wary` `reported` | The last stage is the report landing; `report` reads as an instruction. |
| 32 | Shift hint | `Shift runs there` | keep | One short line, no key legend beyond the key itself. |
| 33 | Goal-line hint H17 (fire) | `Fire at [{who}]: right click.` | leave for now; see open issues | Right click is correct once a fight is on (focus click), not for the first shot (right hold). |

## Not player text (leave as is)
The playground's on-screen log (`Right hold · nothing happens`, `Box · nobody in it`, `Portrait drop · cancelled`, `Scene reset`, `Preset · …`) is a developer log of what a click did. Latch should not port it as player text. `Hold to / Let go to` lines in `testbed/controls/shoot.ts` checks and the "seen there" / "Walking there" / "Out of range" / "No clear shot" assertions there are left alone so the playground checks still pass; the playground keeps those words, only `toggleFree` was swapped.

## text.json changes made in this pass (for Latch's lookups)
- `actions.free` (new control verb, `Weapons free`) and lock `free-not-cell`: engine 0.19.0; `free` added to `CONTROL_VERBS` in `text.schema.ts` (no price line, no exchange, like `drive`, `trigger`, `follow`).
- New event `camera-shot` (log `fire.camera-dark`): Scout and Tech speak when a camera is shot out. `fire.camera-hit` stays silent (the pop `Camera hit` shows it).
- "Fire on" became "Fire at" in `actions.fire.withTarget`, `.then`, `activity.firing`, `actions.follow.does`, and the Alt-only lines (`already-drawn`, `fight-over`, `actions.fire.onTarget.councillor`).
- Lock `out-of-range`: see decision 3.
- Engine 0.20.0 (final fight per agent) adds no code, field or refusal, so no new text.

## Open issues
- **H17** (`hints.items.H17`, trigger "a fight is on, no one but the Enforcer has fired"): its short line says right click. Under the hold controls the first shot is a right hold and the focus click (plain right click) works only once the fight is on, which is what Keel's `FreeFightOn` says, not the hint's older "fight is on". Latch/Motive: confirm which flag the hint reads before the line is touched.
- **Ctrl lock** `civilian-target` ("Ctrl fires anyway") and the range hint "bystander: hold Ctrl to fire" (Keel, engine) are stale under "hold acts" (Sky: bystanders need only the hold). The text stays for the 0.2.34 preset; Keel's range line needs the same change when it moves.
- The pill carries faces for the doers; any proposed line above omits the doer's name on purpose (the face replaces it).
