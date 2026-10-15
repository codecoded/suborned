# How to play the Handover (brief 011 D, first playable)

**Start.** Open Unity on `unity/`. Open `Assets/Scenes/HarbourDistrict.unity` and press Play. The menu **Suborned › Play › Play the Handover** does the same. The clock starts paused at 0:00 (17:24:30) with the **briefing** open: the job, the objective, how it wins and fails, the clock and its 8:30 close, the purse, the two known ways in, and what each agent does. **Space** starts the clock and closes it; **B** brings it back at any time. Councillor Vale is inside the Records Office. Your four agents wait at the getaway car on the west side of the square.

The words (briefing, action cards, refusals, the roster's activity lines, the dialogue) are Fable's `contracts/missions/handover/text.json`, validated by bun and loaded from StreamingAssets.

**The job.** Get Vale alive into your getaway car before the close.
- The front door (route A): learn about Inez, the café waitress at the door. Turn her, then call her so she draws the guard away from the door. Loop the entrance camera, then the Enforcer goes in.
- The pickup (route B): turn Vale's driver, then take Vale at his car.
- The loud route: shoot your way in.

Every order shows its full price before you commit.

## Controls
Sky's scheme of 2026-10-15 ("everything but rotation"). The camera keeps yaw 30. Every key comes from one bindings table (`controls` on the `handover` object in the scene, class `PlayControls`), so remapping means editing that table.

| | |
|---|---|
| **left click an agent** | select it. **Shift**-click adds to the group. |
| **drag a box on the ground** | select every agent inside the box |
| **left click the ground** | the selection walks there. Roads can be crossed anywhere. A click on something solid (the café terrace, a planter, the fountain) snaps to the nearest walkable spot within 3 m; a ring marks where each agent will stop. |
| **double-click the ground** | run (2.8 m/s; nobody runs carrying Vale). Security counts a run it sees as a found fault. The marker shows » while running. |
| **hover a person or thing** | a ring and an action cursor show the cell can act on it; the chip shows the selected agent's price ($ · time · Trail · Alert · outcome, "?" = not known yet). Passers-by show "passer-by". |
| **right click a person or thing** | opens the **action panel**: one card per action your selected agents have on it. Each card: who and the action in plain words ("F2 Fixer · Pay Inez"), one line on what it does and its trade-off, the walk and the full price ($ · time · Trail · Alert · outcome). A locked card is dimmed, says why, and says what unlocks it ("Needs what Inez needs first. Have the Scout Listen."). The panel's head says who or what the target is. Click a card to give it, to that card's agent. Hover a card to see the walk. 1–4 pick the selected agent's cards. Esc, a left click elsewhere or a right click on empty ground closes it. The clock keeps running (`controls.pauseWithPanel` pauses it). |
| **right click with a verb picked (1–4)** | gives that verb at once, no panel (the fast path) |
| **right click empty ground** | closes the panel; else drops a picked verb; else stops the selection's walks (and the act they were walking to). It never cancels an act under way: that is C or an ✕, which say the cost first. |
| **C** | **cancel** what the selected agents are doing (the engine's cancel order). Walking to an act, the walk stops and the act is dropped. An act under way stops at its cost: a Watch or Listen loses its time, a Turn gives the money back but they remember the approach, a held loop ends, an entry or a take may be reported. |
| **✕ on a roster slot, or on the ring over an agent** | the same cancel for that agent. Hovering the ✕ shows what it costs ("Cancel Watch — The time is spent and nothing lands."). |
| **L** (or click the log's header) | open or collapse the **dialogue log** (bottom left). Collapsed, it shows the latest line. |
| **B** | the briefing |
| **every order answers** | a refusal pops what is wrong and what unlocks it, at the cursor and over the target, and goes in the dialogue log; a taken order pops ✓, pulses a ring on the target, and the agent answers in the log. An agent too far away walks to the act's place first (the Tech to the cabinet, the Enforcer to the door), then acts. |
| **1 2 3 4** | pick a different verb. The hotbar above the roster shows them and lights the one a right click will give. |
| **F1 F2 F3 F4** or a **roster portrait** | select Scout, Fixer, Tech or Enforcer (Shift adds). Press twice quickly, or double-click the portrait, to centre the camera on them. |
| **Tab** | next agent |
| **WASD, arrow keys, middle-mouse drag** | pan. Edge scrolling is off by default (`controls.edgeScroll`). |
| **mouse wheel** | zoom 12.5–18.5. People stay 40–60 px tall, as §10 asks. No rotation. |
| **Space** | pause or run. You can give orders while paused. |
| **=  /  −** | speed ×1, ×2, ×4, ×8 |
| **N** | next event: run the clock until something happens |
| **F12** | debug layer: the engine, its cost and the last log lines |
| **Return** | after the end card, play again |

The roster and hotbar can sit at the bottom, in a left column or in the corner: `controls.dock` (Bottom, Left, Corner).

**The verbs** (keys 1–4 for the selected agent):
- **Scout:** 1 Watch (their routine), 2 Listen (their need and temperament: only Listen reveals temperament), 3 False tip (the rivals).
- **Fixer:** 1 Ask, 2 Pay, 3 Help, 4 Pressure. Right click a contact you have already turned to **call** them.
- **Tech:** 1 Loop the entrance camera (stand at the control cabinet first), 2 Reroute the patrol.
- **Enforcer:** 1 acts on whatever you point at: the public door (Enter), Vale at his car (Take), the getaway car (put Vale in). 2 Fire.

If the picked verb doesn't fit the target, a right click walks the agent there.

## What you see
- **Top right:** the mission clock, the next scheduled event, cash, Trail, Alert (three pips), and PAUSE or the speed. Under it, the objective ("Take Vale alive to the getaway car · closes 8:30").
- **Top left:** the last thing that happened.
- **Bottom:** the roster. Each slot: F-key, name, health pips, and **what the agent is doing now**: an icon and a line ("Watching Inez", "Walking to the cabinet", "Holding the loop", "Waiting for orders"), a progress bar with the seconds left, the act queued after a walk ("then: Listen to Inez") or what happens when it lands, and an ✕ to cancel. The 1–4 hotbar sits above it.
- **Bottom left:** the dialogue log. Each order plays its beats in order: the agent's answer when ordered, again on arrival, then what lands (the Scout's read, the person's reply and the Fixer's reaction, the Tech's "Looped. I'm tied here."). The city speaks too: the rivals arriving, the chime, the guard at the café, the win and the fail. Lines are picked deterministically, so a replayed run reads the same.
- **Over each busy agent:** the act's icon in a ring of progress dots; hover it for the ✕. Walking to an act, a line runs on the ground to where the act happens, with the act's icon at its end.
- **Under each agent:** a number on the pavement and what they are doing.
- **Over Inez, the driver, the guard and Vale:** at most two signs. "?" means temperament unknown. "$?" means need suspected (from the briefing), and "$" means need known. ✓ means turned. ◆door marks the guard on the door. ★ marks Vale.
- **On the ground:** red cone outlines are cameras (grey when looped). The amber outline is the guard's sight, and the violet one is the rivals' sight.
- **The city's people** walk their routines (the engine's crowd). Anyone on the street can witness what you do.

A refused order costs nothing. What unlocks it appears at the cursor and in the dialogue log. For example, helping Inez before you know her need: "Needs what Inez needs first. Have the Scout Listen."

## The end card
A win, a fail or the close stops the clock and shows the debrief:
- the time, the money spent, Trail, Alert and shots;
- public security and the investigation;
- the route you took;
- the final fight;
- the contacts kept.

Press Return to play again.

## Your runs are kept
Every order you give, taken or refused, is an engine order. After each one the session is written to `out/play/handover-<date>-<time>.script.json`, with the run's checkpoint hashes. This is the route-script format, so a run can become a route script as it stands. It replays exactly:

    dotnet out/sim-unity/<commit>/sim/cli/bin/Release/net10.0/suborned-sim.dll play out/play/<file>.script.json

At the end, `<file>.end.txt` says whether Unity's own replay of the session matched it.

## Known limits (graybox)
- People are capsules, and the HUD icons are text placeholders for Glyph's v10 art.
- The cell starts under the roofed getaway bay, so pan the camera or press F1–F4 twice to centre on an agent.
- The Fixer's call has no verb key: right click the turned contact.
- If Unity isn't the front app, the clock waits (the project's `runInBackground` is off).
- The final fight is fought with the Fire verb (2 for the Enforcer) on the hostiles, and they hit back. The real-time fight controls (F draw, X cease, V stance, the going-loud card) come next.
- Background people walk on the engine's grid, between the routine stops from Pulse's fix cast. That fixture stands in until a cast passes M-2.
