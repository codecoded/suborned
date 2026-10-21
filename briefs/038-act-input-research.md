# 038 · Quick vs deliberate acting: what other games do, and what we should do

Author: Glyph. Asked by Sky (2026-10-21, after playing 0.2.19): the hidden quick action on right-click caused mistakes; right-click will always open the ring; "maybe double click to quick action, single to open ring (or vice versa)?" Research only: no GDD, code or data edited. Sources, with what I opened and what I only saw in a search result: `briefs/038/research/sources.md`. Where a source did not open I say so; I found nothing usable on Commandos, and Syndicate's detail is thin.

## 1 · What the games do

| Game | Contextual default? | How a choice among several acts is made | Safety against a wrong click |
|---|---|---|---|
| Shadow Tactics | Left click moves and picks the spot for an act; double-click runs | Verb first: A S D F G pick a shared ability, then click the target | Right click cancels an act before it starts and cancels moves; the act cannot be cancelled once in progress; right click on an enemy shows its cone (a free look) |
| Desperados III | Left click acts (moves, double-click runs); right selects (default reversed) | Verb first (A S D F G Y), then a target; Ctrl held = interact mode for bodies, doors, levers | Showdown (Shift) pauses and queues, Enter executes; quickload is the real undo (review) |
| Satellite Reign | Right click moves; a drawn weapon changes what the same click does | Hotkeys; Ctrl+click forces a shot, Alt+right-click forces an act you lack the skill for (player reports) | None. The review's worst complaint is our bug: a drawn gun turns "select civilian" into "incinerate civilian" |
| Syndicate (1993) | Left click moves, right aims and fires | None (one weapon per agent) | A reviewer notes no feedback on move orders. Not opened |
| Divinity OS2 / BG3 | Left click is the default (interact, move); right click opens a context menu, and does nothing on empty ground | The menu is always a menu; Ctrl+left forces an attack (DOS2) | Alt labels everything clickable; DOS2 colours "may anger people" red; BG3 players still report accidental hostility, no confirm found |
| Disco Elysium | Hover outlines the target and shows the success chance on dialogue options | One click goes and does it | Price before click is the whole defence |
| XCOM 2 | Pick the ability, then the target | Enter or Space confirms | Esc cancels. Hit-chance flow not confirmed |
| Invisible, Inc. | Click to act | Click to act | A limited rewind to "repair those occasional missed clicks" (patch note, not opened) |
| The Sims | None: click always opens the pie menu | Pie menu, always | Interactions queue and can be cancelled |
| GTA V, Mass Effect | The wheel is the choice | Hold to open, point, release. GTA slows time, Mass Effect 3 pauses | The slow-down or pause is the safety |
| Red Dead 2 | Tap and hold on one button mean different acts | Prompts | Players call it inconsistent; Rockstar shipped Tap Assist; a developer says hold is used instead of a confirm pop-up and dislikes it |
| Hitman: Absolution | Prompt names the act | Prompt | A triangle beside a prompt means witnesses will read it as hostile (search result only, unconfirmed) |

## 2 · Patterns that recur

1. **One input, one meaning.** Where a click can act or open a menu, the two are on different buttons or keys (DOS2, BG3: left acts, right menus). Where one button's meaning shifts with hidden state (Satellite Reign's drawn gun, RDR2's tap/hold), players mis-click and complain. This is the 0.2.19 failure.
2. **The default is visible before the click**: outline and chance on hover (Disco), Alt labels (DOS2, BG3), a cone on right-click (Shadow Tactics), a triangle on the prompt (Hitman). Nobody relies on the player remembering.
3. **Verb-first is the deliberate fast path** (Shadow Tactics, Desperados III, XCOM): arm, then click. The armed state is shown, and a cancel is always one click away.
4. **Force or harm sits behind a modifier or a hold**, never the plain click (Ctrl in DOS2, Satellite Reign, Desperados III).
5. **Menus and wheels buy time** by pausing or slowing (Mass Effect, GTA, Showdown), or are always the route (Sims). Pie layouts are fast because direction beats distance, and the same item stays in the same place (Wikipedia, Buxton).
6. **Undo beats a warning** (Raskin: people click through confirmations by habit; Invisible, Inc. rewind; Desperados quickload). We can't undo a kill, but we can cancel until the act starts, as Shadow Tactics does.
7. **Double-click is used for a cheap variant of move (run), not to pick among acts.**

## 3 · What fits us

- A double right-click that acts by waiting is wrong for us: a single click has to wait out the double-click time to tell them apart (Raymond Chen: about half a second on Windows, set by the user). Your 250 ms is optimistic, and it would delay every ring for the sake of the quick path, in a game that is real time.
- The wait is not needed. **Make the ring itself the waiting state**: the first right-click opens the ring at once, with the likely way lit; a **second right-click on the same target takes the lit way**. That is a double-click that costs the single click nothing. A player who clicks twice fast gets the quick act; one who clicks once sees the ring. So the answer to "single or double?" is: single opens, double takes, and no timer is involved.
- That second click is blind if fast (no one reads in 150 ms), so it may take only ways that are safe to take blind. Harmful ways are excluded (section 4).

## 4 · Recommendation

**Right click always opens the ring, instantly. The lit way is taken by a second right click on the same target, Enter, or release over any wedge. Harmful ways are never lit and need a hold.**

1. **Ring and highlight.** Opens in the first frame (a short fade-in, no delay). The likely way is at 12 o'clock and lit; a second right click on the same target takes it; a right click on another target moves the ring there; on empty ground it closes. Press, flick, release over a wedge takes that wedge (the marking-menu path, already in 030).
2. **Hover marker = lit wedge.** The 24 px glyph at the cursor (from the plate decision) is the same glyph as the lit wedge, from the same ranking (`IntentFor`), so what you saw is what is lit. A target with more ways ends it with the three-dot mark. A harmful way shows the dashed outline and loud mark on that glyph. This is the single most important fix: the 0.2.19 mistakes came from not knowing what the click would do.
3. **Harmful acts: two grades.**
   - *Never lit, never taken blind*: Pressure, Kill, Fire on a non-hostile, and any Fire while the gun is holstered. They sit last before the three dots, dashed, and the second click can never take them.
   - *Hold to take*: Kill, Fire on a non-hostile and anything seen or loud take the **0.5 s hold** the game already uses for the going-loud card (the wedge fills like the existing ring). Pressure takes a plain click on its wedge (its price is on it) but is never lit.
   - A drawn gun fires directly only at a combatant; on a civilian, contact, guard, Vale or anyone not engaged a right click opens the ring even with the gun drawn (rule 3 of 030). Ctrl+right opens the ring with Fire on the wedge, held, not an instant shot. Satellite Reign is the cautionary tale.
4. **Keep the cancel window.** A walk-and-act has the walk before the act: a right click on empty ground or Esc during the walk cancels it, and the act's busy chip shows when it can no longer be cancelled (Shadow Tactics' rule). No new confirmation dialogs (Raskin).
5. **Shift+1 to 4 and Tab are the verb-first path** (Shadow Tactics, Desperados III): the picked verb wears the cursor glyph, and a right click then fires it directly. Rules: it is **one-shot** (spent by the next click, or Esc), and if it cannot apply to the target it shows its lock pop and does nothing. It never turns into another act. A picked Kill, Fire or Pressure still needs its hold or the ring.
6. **Third person.** Same grammar on the focus: E opens the ring on the focus (the focus ring and price chip are the hover marker), a second E takes the lit way, E held takes a hold-way. Right click at the cursor is the same as on the map. The chip names the lit way.
7. **The ring does not pause or slow time** (real time is the default); the cost is that the ring must be readable at a glance, which is what the marker and the fixed wedge order are for. If playtests still show blind mistakes in fights, a setting that slows the world to 25 % while a ring is open (GTA's way) is the next step before any confirm.
8. **The old quick action becomes an opt-in setting, off by default** (as you said): single right click takes the lit way only when the three checks in 030 pass (one clear best, no "?" outcome, not harmful), and the hover marker is always on.

## 5 · Other places with the same problem (review)

- **Double-click run on the ground.** Same class as the Shadow Tactics and Desperados double-click, but ours makes a quiet walk a noticed run (A 1). The second click upgrades the walk, so the move ring must switch to the run ring on the second click in the same frame (it is the price), and G's latch is the deliberate route. Check: if the run's eye state differs from the walk's, keep it a walk unless G is latched. Needs Latch to confirm what the ring shows today.
- **Ctrl+right for a civilian or contact.** Ctrl-click is a right click on a Mac (030 note), so the modifier and the button are the same input there. Moving it to "opens the ring with Fire held" removes the dependence.
- **Both buttons = panic.** A chord, fight-only, so low risk, but it is the one remaining one-input irreversible order: give it the 0.5 s hold ring like the card.
- **Hidden picked verb** (Shift+1 to 4) is the only other mode that changes what a click means; the cursor glyph and one-shot rule above are the fix.
- **Left click on a person.** It selects or moves, never acts, which matches DOS2 and BG3: keep it so.

## 6 · One alternative

**Keep the quick action as the default, but only after the hover marker has proved itself.** Single right click takes the lit way when the three checks pass; the ring opens on hold or Shift+right click; the marker is always on. Cost: it is today's 0.2.19 behaviour and relies on players reading a 24 px glyph in real time. Switch to it only if a playtest (same method as T10: uncoached, 6 players, "what will this click do?") has ≥ 5 of 6 answering right from the marker alone, and the ring-first build still feels slow to the same players. I would not pick the reverse (double-click = ring, single = quick): every quick act pays the double-click wait and the hidden-default problem comes back.

## 7 · For Aster

- Owner of the build: Latch (`ActContext`, `IntentFor`, hold ring on a wedge, second-click rule, one-shot picked verb). Keel: nothing new unless "slow while open" is wanted (it would be an engine speed rule so replays stay exact).
- Fable: one hint, "Right-click to act." stays; no new sentences.
- Done-check for the build: with the gun holstered and drawn, 20 right clicks on a guard, a civilian, Vale and a door by a scripted pointer never produce Kill, Fire or Pressure without a hold; a double right click on a camera, a door and a contact takes the lit way with no extra delay measured over a single click; the marker glyph and the lit wedge match on every target kind.
