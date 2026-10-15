# How to play the Handover (brief 011 D, first playable)

**Start.** Open Unity on `unity/`. Open `Assets/Scenes/HarbourDistrict.unity` and press Play. The menu **Suborned › Play › Play the Handover** does the same. The clock starts paused at 0:00 (17:24:30). Councillor Vale is inside the Records Office. Your four agents wait at the getaway car on the west side of the square.

**The job.** Get Vale alive into your getaway car before the close.
- The front door (route A): learn about Inez, the café waitress at the door. Turn her, then call her so she draws the guard away from the door. Loop the entrance camera, then the Enforcer goes in.
- The pickup (route B): turn Vale's driver, then take Vale at his car.
- The loud route: shoot your way in.

Every order shows its full price before you commit.

## Controls
| | |
|---|---|
| **1 2 3 4** or left click | select Scout, Fixer, Tech or Enforcer. Shift adds to the group. |
| **right click the ground** | move there. Roads can be crossed anywhere. |
| **hover a person or thing** | the price chip at the cursor shows what a right click will do: $ · time · Trail · Alert · the outcome. "?" means you don't know yet. |
| **right click a person or thing** | the selected agent does it |
| **Q W E R** | pick a different verb. The hotbar above the roster lights the one a right click will give. |
| **Space** | pause or run. You can give orders while paused. |
| **=  /  −** | speed ×1, ×2, ×4, ×8 |
| **N** | next event: run the clock until something happens |
| **arrow keys, middle mouse** | pan |
| **mouse wheel** | zoom 12.5–18.5. People stay 40–60 px tall, as §10 asks. Pitch and yaw are fixed (38°, 30°). |
| **F1** | debug layer: the engine, its cost and the last log lines |
| **Return** | after the end card, play again |

**The verbs.**
- **Scout:** Q Watch (their routine), W Listen (their need and temperament: only Listen reveals temperament), E False tip (the rivals).
- **Fixer:** Q Ask, W Pay, E Help, R Pressure. Right click a contact you have already turned to **call** them.
- **Tech:** Q Loop the entrance camera (stand at the control cabinet first), W Reroute the patrol.
- **Enforcer:** Q acts on whatever you point at: the public door (Enter), Vale at his car (Take), the getaway car (put Vale in). W Fire.

If a verb doesn't fit the target, a right click walks the agent there.

## What you see
- **Top right:** the mission clock, the next scheduled event, cash, Trail, Alert (three pips), and PAUSE or the speed.
- **Bottom:** the roster (number, name, health pips, what each agent is doing), with the Q/W/E/R hotbar above it.
- **Under each agent:** a number on the pavement and what they are doing.
- **Over Inez, the driver, the guard and Vale:** at most two signs. "?" means temperament unknown. "$?" means need suspected (from the briefing), and "$" means need known. ✓ means turned. ◆door marks the guard on the door. ★ marks Vale.
- **On the ground:** red cone outlines are cameras (grey when looped). The amber outline is the guard's sight, and the violet one is the rivals' sight.
- **The city's people** walk their routines (the engine's crowd). Anyone on the street can witness what you do.

A refused order costs nothing. Its reason appears at the bottom left. For example, paying Inez before you know her: "needs the pay approach's knowledge".

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
- The cell starts under the roofed getaway bay, so pan the camera or select agents with 1–4.
- The Fixer's call has no verb key: right click the turned contact.
- The final fight is fought with W Fire on the hostiles, and they hit back. The scripts move the Enforcer between volleys.
- Background people walk on the engine's grid, between the routine stops from Pulse's fix cast. That fixture stands in until a cast passes M-2.
