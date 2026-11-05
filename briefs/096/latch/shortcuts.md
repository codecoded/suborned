# Brief 096 · hidden shortcuts out of the player's keys (Latch, 2026-11-05)

Sky, 2026-11-05: "need to remove N and P and other odd shortcuts that are hidden to the user, and not of high relevance."
Rule (memory "no key-only features"): every feature has a clickable control; a key is only a second way.

## What changed

- `PlayControls.Defaults()` (the player's table and the Controls tab) loses N, `=`, `−`, Return, F12, F11, `` ` ``, K, L, B, Y, Z and P.
- Dev keys: F12 debug, F11 HUD view, P camera lab (and the fight lab's panel), Y target mode and Return (the lab freeze's "go") now live in a separate table, `PlayControls.dev`. It is read only when `PlayControls.DevKeys` is on, which means the Editor or a player started with `-debug` (the flag that already shows the lab's options and the Fight Range). A development build alone doesn't turn it on, because Sky plays those. These keys are never in the Controls tab: the tab's "Debug" rows are gone.
- Handlers removed: next event, faster/slower, Return to replay on the end card (the card's Replay button and the pause menu's Restart stay), B (open Job, close the start page, Job inside the book), L (Journal, in play and inside the book), `` ` `` all strips, K acknowledge, Z step target. Dead host methods were deleted: `NextEvent`, `AcknowledgeNewest`, `StepTarget`, `FocusNext`.
- `ShowAllStrips` is no longer remembered in PlayerPrefs. With no key left to turn it off, a saved "on" would have stuck for good.
- `ModalGate`: Briefing and Restart are no longer allowed actions (the briefing allows Space, the end card only the camera keys). `ModalGateTests` are updated to match.
- Key text on screen: the IMGUI placeholder's city line (`HudModel.City`) lost "B briefing · L history · N next"; its old briefing footer lost "B: this briefing"; its feed bar and tick lost the K and L keycaps. The host's paused status lost "B shows the briefing again". The kit HUD already hid these (`KeyLegends = false`).
- Enum members stay (scenes store `PlayAction` as numbers); only the table, handlers and rows changed.
- Saved settings: `ApplyBindings` skips an action that is no longer in the table, an unknown action name and a binding with no keys. It never throws, so an old settings.json with `NextEvent` still loads.
- Proofs:
  - FrontEnd.Proof's rebinding now uses Turn left: rebind Q → N (N is free now), clash Cancel → N, Swap, Reset back to C / Q. It also checks that a removed action has no binding. The clash screenshot offers Q (Turn left).
  - ProofBook's "street keys don't reach the world under the book" check presses I (Intel stays shut) where it used to press `=` (speed).
  - ProofFire, Proof085, Proof085Squad and BotRun.Human press only E, W, T, F, V, X, Q, 1–5, F1–F4 and Esc. All of those stay, so they need no change.

## Audit: every binding before this change

Visible = a button or icon does the same job, or an on-screen legend names the key. Kit HUD (`KeyLegends = false`) shows no keycaps except the portraits' 1–4.

| Key | Action | Visible control | Dev / test | Matters in play | Decision |
|---|---|---|---|---|---|
| WASD / arrows | pan | middle-drag, [D] left drag | – | high | keep |
| Shift+1–4 | pick action slot | the ring and its wedges ⇧1–⇧4 | – | high | keep (ring keys) |
| 1–4 (+F1–F4 hidden aliases) | select agent | portraits carry 1–4 | – | high | keep |
| Tab | step the agent's actions / overlap stack (hold preset) / book sections | ring, wheel steps the stack, book rail | – | medium | **borderline** |
| Space | pause | the pill's pause button | – | high | keep |
| = / − | faster / slower | none (Options › Default game speed sets the speed for the next run) | – | low (no skip offer, §01) | **removed** |
| N | next event | none | – | low | **removed** |
| Return | replay on the end card; lab freeze go | end card Replay, pause menu Restart | lab | low | **removed** (lab go → dev keys) |
| F12 | debug overlay | none | dev | none | **dev keys only** |
| Shift (with mouse) | add to group / run | modifier | – | high | keep |
| C | cancel (classic) / camera follow (hold preset) | busy chip's ✕ cancels; follow has none | – | medium | **borderline** |
| B | open Job | the Book button, the book's Job tab | – | low | **removed** |
| L | open Journal | the Book button (opens Journal when unread), Journal tab | – | low | **removed** |
| F | draw / holster | the portrait's sidearm mark (click) | – | high | **borderline** (has a click; hints teach "press F") |
| X | cease (classic, retired) / stop all orders (hold preset) | per-agent ✕ on the busy chip | – | medium | **borderline** |
| V | stance (retired by engine 0.35, one input) | stance fan (off under one input) | – | none today | **borderline** (dead key) |
| Ctrl (with mouse) | force fire | modifier | – | medium | keep |
| H | hints on/off, reopen a folded hint; Shift+H relearn | hint "?" reopens; Options › Hints on/off | – | low | **borderline** |
| K | acknowledge newest card | click the card | – | low | **removed** |
| `` ` `` | all four strips | none | test | low | **removed** |
| F11 | HUD view Kit/IMGUI | none | dev | none | **dev keys only** |
| I | Intel | the Intel strip (click) | – | medium | **borderline** |
| Alt | detail layer | rest on a price 0.6 s | – | medium | keep (modifier) |
| R | response list | the response pill (click) | – | medium | **borderline** |
| G | walk / run | the walk/run glyph on a moving agent's chip (click); double click runs | – | medium | **borderline** |
| Q / E | quarter turn | the turn buttons on the plot scope | – | high | keep |
| Alt + middle-drag | free turn | modifier | – | low | keep |
| T | follow / ground view [lab] | none to enter; Squad button reads "Back to map · T" to leave | lab | low | **borderline** |
| P | camera lab / fight-lab panel | none | dev / lab | none for the player | **dev keys only** |
| Y | target mode | none (lab preset only) | lab | none for the player | **dev keys only** |
| Z | step target / driven focus | target markers and edge pins are clickable; the wheel steps the stack | – | low | **removed** |
| E (third person) | act | click acts (Left click acts, on by default) | lab view | low | keep (lab view's key) |
| J | the book | the Book button | – | high | keep |
| 5 (and Ctrl/Cmd+A) | select all | the Squad button | – | medium | **borderline** |
| Esc | close / pause menu | Menu on the book's rail | – | high | keep (not in the table) |

## Borderline: hidden or half-hidden but gameplay-relevant (for Aster to bring to Sky)

Not changed. Each line says whether there's a click today, then my recommendation.

- **T, follow / ground view [lab]:** there's no click to enter it; the Squad button shows "Back to map · T" to leave. *Recommend:* while the ground view stays [lab], move T to the dev keys. If Sky wants it in the game, add a button first (the portrait's corner, or beside Squad).
- **G, walk / run:** has a click (the walk/run glyph on a moving agent's chip; a double click runs). *Recommend:* keep as a second way. The hint teaches it ("Press G").
- **F, draw / holster:** has a click (the portrait's sidearm mark). *Recommend:* keep. Hint H21 teaches "press F", so if F goes, Fable rewrites that hint.
- **V, stance:** dead under engine 0.35's one input (`!OneInput` guards it), and the Controls tab already had no row for it. *Recommend:* remove from the table (BotRun.Human presses V only for stance steps that no longer happen; it would need a one-line guard).
- **X, stop (hold preset) / cease (retired):** the busy chip's ✕ stops one agent; there's no "stop all selected" click. *Recommend:* keep X if Sky wants a stop-all. Otherwise remove it, since ✕ per agent covers it. BotRun.Human's `Stop` presses X and would switch to `HoldStop()`.
- **C, cancel (classic) / camera follow (hold preset):** in the default (hold) preset C toggles the camera following the selection. That has no click and isn't in the GDD's hold line. *Recommend:* remove the camera-follow toggle, or give it a click. Cancel already has the ✕.
- **H, hints:** the "?" reopens a folded hint, and Options › Hints turns them on and off. Shift+H "relearn" has no click. *Recommend:* remove the key. Options covers on/off, and a "Relearn hints" button could go on the Options page if Sky wants it.
- **I, Intel:** has a click (the Intel strip). *Recommend:* keep as a second way.
- **R, response list:** has a click (the response pill). *Recommend:* keep as a second way.
- **Tab, step actions / overlap / book sections:** clicks exist (the ring's wedges, the wheel on a stack, the book's rail). *Recommend:* keep. It's the game-standard cycle key and appears in hints.
- **5 / Ctrl+A, select all:** has a click (the Squad button). *Recommend:* keep as a second way.

## GDD replacement lines (v0.39.0 §10; for Aster: Latch doesn't edit the GDD)

Line "Shift+1–4 …" (agent GDD line 312), replace with:

> - Shift+1–4 pick a slot of the selected agent's actions · Tab / Shift+Tab step through them (none = the contextual default), never moving the camera or the selection; the wheel steps an overlap stack on the map, and a picked action's targets are clicked on their markers or edge pins.

Line "view: …" (line 315), replace the tail "· T the ground view [lab] · Y target mode [lab] · P the camera lab [lab]." with:

> · T the ground view [lab] · target mode and the camera lab are lab tools on dev keys (Y, P), read only in the Editor or with -debug, never in the player's Controls.

Line "time: …" (line 316), replace with:

> - time: Space or the pill's pause button pauses (tactical pause, a tool; T1). The clock runs at the speed set in Options (default 1×); there's no speed key and no next-event jump in play (no skip offer, §01).

Line "panels: …" (line 317), replace "· I opens Intel; the Book button or J the book, B on its Job page, L on its Journal; R the response list; H hides hints;" with:

> · I opens Intel; the Book button or J the book (its rail opens Job and Journal); R the response list; H hides hints;

and "lab and dev keys stay key-only." with:

> lab and dev keys stay key-only and live outside the player's table (F12 debug, F11 HUD view, P camera lab, Y target mode: Editor or -debug only).

Latch's §14 role line ("hold:"), replace "Shift+1–4 and Tab the selected agent's actions, Z steps the target," with "Shift+1–4 and Tab the selected agent's actions,", "=/− speed and N next event (keys only)," with nothing, and "the Book button or J the book (B Job, L Journal)" with "the Book button or J the book".

## text.json

No keys deleted. The Controls tab's row names are in C# (`FrontEnd.ControlRows`), not text.json. For Fable's words pass (contracts/missions/handover/text.json):

- `locks.mission-over.line`: "The mission is over. Press Return to play again." Return no longer replays. Suggest: "The mission is over. Replay from the end card."
- The `_doc` note near line 1499 says "the history key L": the history is the book's Journal now (no key).

## Checks run

- `bun run hostlogic`: 124 pass, 0 fail.
- `bun run typecheck`: clean.
- `bun run batch --only words,compile,hostlogic,unity-match`: BATCH OK. words ok; compile ok (1 warning, as before); hostlogic 124/124; unity-match 556/556 scenarios match headless. Its Unity batchmode compile built every assembly, Suborned.Play.Tests included, with no errors; the two CS0414 warnings are older and not in these files.
- Unity EditMode `Suborned.Play.Tests.ModalGateTests`: 5/5 pass.
- Not run: FrontEnd.Proof and ProofBook (player proofs, need a build; no build in this round).
