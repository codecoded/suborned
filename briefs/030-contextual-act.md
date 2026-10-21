# 030 · Contextual right click: one act, a ring when it matters

**Status (2026-10-20): final, ready for Latch** (Sky's answers and key decisions are at the end). Author: Glyph. Asked by Sky: "right click should be kinda contextual. If the gun is drawn, then it's shoot, but otherwise, if it's a door, it's open door, but when more than one option then shows a menu... how do we make it easy to draw / holster gun?" Builds on Aster's proposal. Applies on the map and in third person (brief 024), which now has a chase camera at constant distance and angle, no automatic view switch, and right click on an out of reach target walks there and acts. Mockup: `briefs/030/radial.html`. No GDD, code or data has been edited.

## 0 · Two corrections to the proposal, and what is already built

- **G is taken.** `PlayControls.cs` has G = Gait (the run latch, Sky 2026-10-16) and **F = Draw already**: `HostFight.DrawOrHolster` draws the selection's weapons, or holsters them if any is drawn. So the draw key exists and needs no new binding. Recommendation: keep **F**. (Free letters if Sky wants another: U, J, O, Z.)
- **Today's right click:** `PointerRight` fires on a combatant (drawn, or always on a final-fight hostile), boards the car for the selection, gives the picked verb at once (keys 1 to 4), else `OpenPanel` shows the full panel. Out of reach already walks first on the map (`ActPlace`, "walks 12 m (9 s), then", one time). What is new is the **default choice per target** and **the ring replacing the panel's first appearance**.

## 1 · The rule

Right click does **one act, chosen by the target under the cursor**. It is a *click*, so it acts on release (under 0.22 s, under 6 px of movement), with one exception: **Fire acts on press**, because the going-loud card's 0.5 s hold and the sweep (hold and drag onto the next hostile) both belong to that press.

Order of precedence, first match wins:

1. **An action picked on the plate (Shift+1 to 4, or Tab)** gives that verb at once, as today. The default rule is only for "none picked".
2. **Ctrl** forces fire on a civilian or contact, as today (Vale never; the pop "never fire on Vale" stays).
3. **Fire:** a hostile of a fight, or anything hostile *while an armed selected agent has a gun drawn* (`ShotAt`), fires until down. No ring. A holstered click on a final-fight hostile (a fight is on) also fires: the draw is part of the order and its time is in the price.
4. **The default act** for the target (table below).
5. **Ring** when the default is not safe to take on a quick click (rule 3 below) or the player holds.
6. **Ground:** stop (drop the picked verb, else stop walks), as today.

### The three checks for a quick click

A quick click takes the pre-highlighted way **only if all three hold**; otherwise the ring opens and nothing is taken until the player chooses:

1. **One clearly best way.** The rows left after removing locked ones, ranked (below), leave one at the top. Locked rows never count: a finished Watch (`already-watched`), a loop that already runs, a body already carried drop out, so what remains is the next sensible step. This is what lets a camera go Watch, then Loop, with no setting.
2. **No gamble.** If the preview's outcome is "?" (temperament unknown, `sight.unknown`) the click does not guess: it opens the ring so the three approaches are seen side by side with their "?" outcomes. Once the temperament is known the fitting approach is the default. (Suborn many ways: the default is a hint; every way stays one hold away; Pressure is never a default.)
3. **Not violent while holstered.** Knock out, Kill, and Fire on a non-hostile (guard, patrol, rivals, response unit with no fight on) are never taken by a quick holstered click. The ring opens with "Fire" shown with the draw mark. A gun already drawn is the player's explicit choice, so rule 3 above fires.

If every way is locked, the click shows the shortest lock pop (`ShortOf`, as the panel's locked row does today) and opens nothing. The full panel (with every locked row and its reason) is the ring's last segment (⋯).

### Rank, when several ways are open

1. The mission's finishing acts that are ready: Take, Enter, Board, Go.
2. A free learn the cell has not done: Watch, then Listen (Sky 2026-10-20: Watch stays first, as today; Listen reveals temperament). Both are free learns with no "?" outcome, so a Scout's quick click takes them.
3. A turn, the approach the rulebook fits to the known temperament, then cheapest, then fewest Trail. Never Pressure.
4. A machine act: Loop, Reroute, False tip.
5. Body work: Carry, Hide, Tie, Put down.
6. Takedown (the player arms it with key 3 or picks it in the ring; never a default).
7. Fire (on a non-hostile only through the ring, with the draw mark).

The default goes to the **primary selected agent** (the first selected). With a group selected, a way that two agents can give is one segment with both marks; a way only another selected agent can give shows that agent's mark on its segment. Agents not selected are not listed (Sky 2026-10-18: only the selected).

## 2 · The default per target kind (every kind and order the engine has)

"Default" is what a quick click does when the three checks pass. "Ring" lists what the ring shows (rows that are not locked, in rank order, then ⋯).

| Target | Default | Ring also has | Notes |
|---|---|---|---|
| **Ground** | Stop: drop the picked verb, else stop walks | none | A cover mark within `coverSnap.marks` of a drawn selected agent: Take cover (the engine's move snap), same as a left click there. Holstered never snaps. |
| **A hostile** (fight on) | Fire | none | Always Fire; no ring. Hold sweeps to the next hostile. A holstered selection draws as part of the order (price shows "draws 0.6 s"). |
| **Response unit / officer** (`unit`) | Fire when drawn or a fight is on; silent otherwise | none | Gap: `ShotAt` has no case for `unit` today; Latch maps it as `security`. |
| **Patrol / security / rivals** (no fight) | Scout and Tech: Watch while the cone is unknown; then Reroute (Tech), False tip (rivals, Scout) | Fire (draw mark) | Gun drawn: Fire. Enforcer alone: ring with Fire only (violent rule). |
| **Guard / Inez / the driver / any cast person** | Scout: Watch, then Listen · Fixer: the fitting approach when the temperament is known, else ring · Fixer on a turned contact: Call · Enforcer: ring (Take at the pickup) | Ask, Pay, Help, Pressure (Fixer); Knock out, Kill (Enforcer); Fire (draw mark, guard only) | Locked rows hide: needs-need, refused-before, no cash. A person with nothing for the selection: the one-line hint "the Fixer can" as today. |
| **Vale** (or the official car) | Take (Enforcer) | none | Not the Enforcer: the lock pop "Enforcer only". Too early: "Vale is not at his car yet". Never Fire. |
| **A passer-by** (crowd) | Nothing, silent | Enforcer: Knock out, Kill | Never fired on by a plain click, drawn or not (Ctrl forces). |
| **Door** | Enter (Enforcer) | none | Seen = fail: the price carries the eye; a seen Enter asks for the 0.5 s hold (`SeenGate`). |
| **Control cabinet** | Loop (Tech), then Reroute once the loop runs | Loop, Reroute | Both open at once: ring; the highlighted one is the first open. |
| **Camera** | Tech or Scout: Watch until its cone is learned, then Loop (Tech, if loopable) | Loop | |
| **The getaway car** | Board for the whole selection; Go once aboard | Board / Go | Go commits with the hold when anyone stays (`GoGive`). |
| **A body (found or carried)** | Carry (Put down if you carry it) | Tie, Put down | Never Hide: it needs a stash. |
| **A stash (bin, door)** | Hide, if a body is carried; silent otherwise | none | |
| **A downed agent** | Carry | none | As `CarryDowned` today. |
| **An own agent (standing)** | Falls to Ground | none | Left click selects. |

Orders with no target (draw, holster, cease, stance, panic) stay on keys: F, X, V, both buttons. The volley and the field belong to freeze and target mode (below).

## 3 · The ring

**Opening.** Hold the right button **0.22 s** without moving more than 6 px: the ring opens centred on the press point (it never opens off screen: it slides in to fit), with the likely way at 12 o'clock and pre-highlighted. Release over a segment (move the pointer toward it; dead zone 18 px) takes it. Release in the middle: if the ring is under 0.5 s old and the pointer has not moved it stays open (**sticky**, for the trackpad, where hold and slide is awkward); then a click on a segment takes it. Esc, a click on empty space or a right click on the middle closes it. **Shift + right click** opens the ring at once on any target (to look before choosing). Nothing is ever committed by opening it.

**In third person**: the ring opens at the cursor (the cursor is free) on the thing under it. **E** is the quick click on the focus (the nearest valid target in front within 8 m, with its price chip); **holding E 0.22 s** opens the ring on the focus, centred on it. Same rules, same segments.

**Segments.** At most 5 ways plus ⋯ (6 wedges, 60° each; fewer ways, wider wedges). Order clockwise from the top is the rank above, so the same target kind always has the same shape (muscle memory); violent ways are last before ⋯, drawn with a dashed outline and the loud mark (a shape cue, not a colour).

Each wedge shows an **icon** in the wedge and, beside it, one **pill**: **one word**, then the price in the panel's order, one line: time (walk plus act as one time), `$`, Trail, Alert, and the eye or "found" glyph where the panel shows them. Words: Listen, Watch, Ask, Pay, Help, Pressure, Loop, Reroute, Take, Enter, Board, Go, Carry, Tie, Hide, Drop, Knockout, Kill, Fire. A way that needs the draw first carries the **holstered-gun mark** before its word and "draws 0.6 s" in its price, so the seen-gun cost (`drawn.seen`: Alert to the draw-seen level, with the police warning window in the lab) is printed before anyone commits. Key caps 1 to 4 sit on the wedges that are the agent's knobs (the same keys as the panel). The pill of the highlighted wedge is the strongest; Alt gives the breakdown for it, as today.

A way that needs a hold (a seen takedown, a going-loud fire) shows the hold ring on its wedge; taking it opens the existing card and the same 0.5 s hold (`SeenGate`, `GateHoldByCard`).

**Budget.** A ring with five pills is about 480 × 300 px at 1080p: 7 % of the screen, only while open, inside the "card open" budget (22 %). At rest nothing is added except the glyph below.

**Seeing that a ring exists.** The hover chip (already there) ends with a three-dot mark when other ways are open ("⋯"), and the cursor takes the ring shape on such a target. No words. Two icons over a person stay the limit.

## 4 · How it replaces the panel

- `PointerRight` (the press) becomes `ActContext` (press for Fire, release otherwise). The `OpenPanel(t, screen)` call at its end becomes "build the ring from the same rows": `OpenPanel` already builds every way per selected agent, priced, locked or not. The ring is a second view of that list (available rows only, ranked), so **no price or lock logic is rewritten**. The panel is the ⋯ segment and Shift+right while the setting `controls.panelOnRight` (default off) lets Sky bring it back.
- `IntentFor` (the hover price) uses the same ranking, so the chip shows exactly what the click will do. The default order for a Scout on a person is unchanged (Watch, then Listen); what changes is the "?" check (a Fixer's turn with an unknown temperament opens the ring) and the violent check.
- Left click on a row of an open ring = today's `PanelDo`. Shift+1 to 4 inside it = today's `PanelKey` (plain 1 to 4 are ignored while it is open, so a stray digit never swaps agents under a ring).
- Pops, Alt, the "found" glyph, and the move ring on the ground (the walk drawn on hover of a wedge: `SetPanelHover`'s path) are unchanged.

## 5 · Draw and holster

Drawing is a visible, priced act (a watcher who sees a drawn weapon raises Alert; in the lab a first sighting gives a short warning window to holster). So it is **deliberate and always one key**, and it is also part of the click when you mean to shoot.

- **F toggles** for the selection (existing). Draw takes `draw` ticks; the F key's hover on the portrait shows it.
- **The portrait sidearm mark** (new, on each armed agent's portrait, near the weapon corner): hollow = holstered, filled = drawn, a pulsing ring with a seconds count = a watcher has shouted and you have this long to holster. **Clicking it toggles that one agent**; F toggles the selection. It is the second way, and it is how a trackpad player learns the key (the mark's tooltip is the F cap, no sentence).
- **Fire includes the draw.** Right click on a hostile while holstered fires (draw time in the price). On anything that is not hostile, "Fire" with the holstered-gun mark is in the ring: that is Sky's "Draw and fire" in one word plus a mark.
- **Auto-holster (proposal, needs Keel):** a gun *drawn by a click or by panic* is holstered by the engine **4 s after the fight is over** (`fight.over`), unless a hostile is still in sight. A gun the player drew with **F stays drawn until F**. This keeps "you chose it" separate from "the fight made you". It must be an engine rule (a holster order at a known tick) so replays are exact. Not read when a stance or hold order is active.
- **Drawn is also on the person:** the drawn-gun icon is one of the two icons allowed over a person; the cursor over a hostile becomes a crosshair, over a non-hostile the ring cursor, so the player always sees what a right click will do before pressing.

## 6 · Conflicts and how they resolve

| With | Resolution |
|---|---|
| **Right-drag orbit (brief 024)** | Not needed: the third-person camera now chases at constant distance and angle, so **the right button has no camera duty**. If an orbit comes back, it lives on the middle button, and for a mouse a right press that moves more than 6 px before 0.22 s is an orbit and acts on nothing. |
| **Going-loud 0.5 s hold** | Fire still acts on press, so the hold continues the same press. The ring's hold (0.22 s) never starts on a Fire target, so they cannot collide. A loud way chosen from the ring uses the card's hold (`GateHoldByCard`). |
| **Sweep (hold right, drag onto the next hostile)** | Unchanged and Fire-only; it needs the press to have started a Fire. |
| **Both buttons = panic** | Unchanged; ignored while a ring is open (so choosing with the left button never panics). Off in third person (the wheel's Fall back). |
| **Target mode (Y)** | The ring is off: left adds a shot, **right removes one** (`VolleyClick`) exactly as built; contextual right click returns when target mode ends. |
| **Freeze mode** | The ring works (it is UI time). The chosen order goes to the freeze queue (one action each, the ladder shows it) instead of the engine; Fire's hold card shows inside the freeze as today. |
| **Action picked (Shift+1 to 4, Tab)** | Wins, so a player who wants a fixed verb keeps today's fast path. Right click on empty ground drops it, as today. |
| **Pause / real time** | The ring does not stop the clock (real time is the default; the panel today does not either, `pauseWithPanel` off). It opens in 0.22 s so a quick order is not punished; Sky may want it to slow time in fights, a Motive question. |
| **Left-drag orbit / box select** | Not touched: the ring belongs to the right button (and E in third person). |
| **Mac Ctrl-click** | Ctrl-click is a right click at the OS: check on the Air that Ctrl + right (force fire) still arrives as the key plus the button. |
| **Out of reach** | Map: unchanged ("walks 12 m (9 s), then", one time). Third person: same order, same line; the move ring shows on the ground; any WASD input cancels the walk and the act (nothing is paid until the act starts). A fire out of range walks into the weapon's effective ring: today only the lab's `close` does that; outside the lab `FireSelected` refuses with "out of range". Keel/Latch: use `close` for a click-fire in every mode (Sky's request). |

## 7 · Hand-offs

- **Latch:** `ActContext` with the press/release split and the 0.22 s hold, the ring (UI Toolkit, from the rows `OpenPanel` builds), ranking in `IntentFor`, the sticky ring, E and hold-E, the portrait sidearm mark and click, the cursor shapes, the `unit` fire case, `controls.panelOnRight`.
- **Keel:** auto-holster after the fight (drawn-by-click only), exposing the "fits the temperament" read for the default approach (or Latch ranks by `Preview` outcome, then `$`, then Trail), fire walk-in outside the lab.
- **Fable:** the one-word labels above and the lock pops that remain; check against the plain-copy principles (what, why, fix; the same word for the same thing).
- **Motive:** the 0.22 s and 4 s numbers as tunables (`tune`), and whether the ring slows time in a fight.
- **Suture:** an uncoached check: ask five people to give an order to Inez, open a door, shoot a hostile, take Vale, and holster, with no instruction; at least 4 of 5 finish each on the first try.
- **Glyph (me):** the ring and mark art are in the mockup; I will redo the pill art from Latch's first build.

## Done-check

1. Every target kind in the table gives the stated default in a scripted test (a unit test over the ranking, no engine step), and a quick click never gambles, never takes a violent holstered way, never fires on a civilian without Ctrl.
2. The hover chip and the click agree for all of them.
3. Fire on a hostile with a holstered gun draws, fires, and the price showed the draw before the press.
4. F, the portrait mark and the engine's auto-holster leave the same state; a replay is hash-identical.
5. The ring never opens in target mode; the going-loud and seen holds still commit; panic still works in a fight and not while a ring is open.
6. HUD budget at rest unchanged; with a ring open 22 % or less (`bun run verify`).
7. Sky plays one Handover on the map and one in third person using only right click, E, F, and says whether he reached for the panel.

## 8 · Keys: agents on 1 to 4, actions on Shift+1 to 4 and Tab (Sky's decision, 2026-10-20)

Sky: "1-4 then Shift+1-4 for the action, maybe Tab goes through the player's actions as well, as Tab through players would zap the camera about all over the place." No F row is used (it needs fn). F1 to F4 stay bound as hidden aliases for an external keyboard; nothing shows them.

**One-page key table** (only the keys this brief changes; everything else is as built)

| Key | Map | Third person |
|---|---|---|
| **1 2 3 4** | Select Scout, Fixer, Tech, Enforcer (Shift+click or Shift+box adds to the group) | Take control of that agent; the camera blends 0.45 s to them |
| **Shift+1 to 4** | Pick that slot of the **selected agent's** action plate (what 1 to 4 did) | Same, **only while standing** (no move key held, speed under 0.3 m/s; an engine-driven walk counts as standing). While moving it does nothing, silently; Tab still works |
| **Tab / Shift+Tab** | Next / previous action of the selected agent: none, slot 1, 2, 3, back to none (none = the contextual default). Never moves the camera or the selection | Same |
| **Z / Shift+Z** | Step the **target**: overlapping things under the cursor, else the next target of the picked action, nearest first | Step the focus (the thing E acts on) |
| **Portrait click** | Select (Shift adds); a double click centres the camera | Take control |
| **Right click / E** | Contextual act (section 1); picked action wins | Same; hold 0.22 s for the ring |
| **F, X, V, C** | Draw or holster, cease, stance, cancel | Same |
| **G** | Walk or run latch for the moving agent | Same latch; Shift held also runs |
| **Shift** | Modifier: add to group, second layer of 1 to 4, Tab and Z | **Hold to run**, and the second layer of Tab and Z; of 1 to 4 only while standing |
| **Q, E** | Turn the view a quarter | Q: squad wheel. E: act on the focus |
| **W A S D** | Pan | Move |

Labels: portraits carry **1 to 4**; the action plate's four slots carry **⇧1 to ⇧4** (Mac glyph); ring wedges for an agent's action carry the same ⇧ caps; the plate shows a small Tab glyph at its edge. Panel text that said "select the Enforcer (F4)" says "Select the Enforcer" and lights the portrait.

**Tab's other uses today** (`HandoverHost` lines 336 to 338): (1) when things overlap under the cursor it steps through them; (2) with an action picked it steps through that action's targets; (3) otherwise it cycled agents. Use 3 is gone (Sky). Uses 1 and 2 would fight the new job, so they **move to Z**, which is free: one key, "step the target", with the mouse wheel still stepping an overlap stack on the map. After this Tab means one thing everywhere, and 1 to 4 / Shift+1 to 4 / Tab / Z are four plain jobs. Target mode (Y) is unchanged: it uses left and right click only, and Tab does nothing in it.

**Third-person clash: Shift is run on the ground (Sky's decision: Shift stays run).** So Shift+1 to 4 picks an action only while the agent stands. The rule is checked on the digit's press: move input held or speed at or above 0.3 m/s means the press is ignored, with no pop and no pick (a 0.15 s grace after the last move key, so letting go and tapping works). The cost is that you cannot change action mid-stride with a digit; Tab and Shift+Tab do (they are not Shift-gated), and a right click with an action picked is the same standing or moving. The key strip word stays "Shift run", and the plate's ⇧ caps dim while the agent moves, so the ignored press is never a mystery.

Carrying Vale: nobody runs, unchanged. Brief 024's table is otherwise as written.

## Choices for Sky

1. **Draw key.** G is the run latch; F already draws and holsters. Keep F (recommended), or move run elsewhere and give G to draw.
2. **Auto-holster.** Guns drawn by a click or panic are holstered 4 s after the fight; guns drawn with F stay until F. Yes (recommended), no, or always auto.
3. **Quick click on a Fixer turn with an unknown temperament** opens the ring instead of guessing. Watch stays first for the Scout.

4. **Agent keys: decided** (section 8): 1 to 4 agents, Shift+1 to 4 (standing only in third person) and Tab actions, Z steps targets, Shift stays run. Nothing open for Sky.

## Sky's answers (2026-10-20)
1. **Draw key:** the letter F stays for draw/holster (not a function key). Sky uses F1–F12 as Mac function keys (they need fn), so **agent selection must not depend on F1–F4**: Glyph proposes a replacement (portrait clicks always work; candidates: Shift+1–4, or `~`/Tab-style cycling), Sky picks.
2. **Auto-holster:** yes, try it (gun drawn by a click or panic holsters 4 s after the fight ends; drawn with F stays until F). Keel adds the rule.
3. **Unread person default:** **Watch** stays the default before Listen (Sky), not Glyph's Listen-first; a quick click on a person whose temperament is unknown still opens the ring.

## Sky's selection decision (2026-10-20)
"1-4 then Shift+1-4 for the action, maybe Tab goes through the player's actions as well, as Tab through players would zap the camera about all over the place." So: **1–4 select agents** (Scout, Fixer, Tech, Enforcer, as the roster order), **Shift+1–4 pick that agent's action** (today's 1–4 verbs), **Tab cycles the selected agent's actions** (Shift+Tab back), not agents, so the camera never jumps. Glyph resolves the third-person clash (Shift is run on the ground: run moves to the existing G latch, or Shift+digit only picks when not moving).

## Run key (Sky, 2026-10-20)
**Shift stays run** in third person. So Shift+1–4 picks an action only while the agent is standing still (Glyph's alternative); while running, the plate's actions are picked with Tab or a click.

## The action plate (Sky, 2026-10-21)
Asked whether the Fixer's Watch should be a fifth plate knob: "No, I don't really like the plate - it's too disconnected from the context of the action for me." So Watch stays in the ring; and Glyph proposes how far the plate can shrink or go, with acting moving to the contextual right-click and ring at the target.

## Glyph (2026-10-21): how far the plate can shrink
Board: `briefs/030/plate.html`. The plate has no job of its own left once acting lives on the ring and the target: prices were never on it (they are the ring wedge, hover chip and move ring); the armed verb moves to the cursor (24 px glyph) and the selected portrait's corner, with the ring opening on that wedge; "what can this agent do" becomes a 4-glyph strip under the selected portrait for 2.5 s on Tab or Shift; target mode becomes the crosshair cursor and a pip pill by the clock; the freeze ladder docks beside the selected portrait; stance goes on the sidearm mark's hover fan; the gun mark is already on the portrait. Keys (1 to 4, Shift+1 to 4, Tab, Z) are unchanged.
Measured on the half-scale mocks: today 7.2 % of the screen at rest, no plate 5.7 %, minimal quiver (four 30 px glyph chips beside the selected portrait) 5.9 %.
**Sky to choose:** A, no plate (recommended), or B, the minimal quiver. Latch keeps the keys and removes the drawing; Fable adds one hint, "Right-click to act." (4 words).

## Plate decision (Sky, 2026-10-21)
**A, plate removed for now** ("easiest option to then iterate upwards"), with its jobs moved as in `briefs/030/plate.html`.

## Right-click opens the ring (Sky, 2026-10-21, after playing 0.2.19)
"When I right clicked it was always the quick action, wasn't clear to me I had to hold down, and without being clear what that quick action was, I made a lot of mistakes. I think quick action maybe some player config via plate in future - or it will go so it's always the ring (I didn't mind the callout / panel design too much - was clear)."
So: a right-click always opens the ring at the target, with the likely way highlighted (a second click or Enter takes it; release over a wedge takes that wedge). The quick action becomes a setting, off by default. The hover marker still shows the likely action. The callout and panel stay as they are.
