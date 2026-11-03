# 084 · Fight cues: who's mine, who's firing, who's hit, who can see me

Owner: Aster. Opened 2026-10-31. Page: `briefs/084/cues.html` (sources in `briefs/084/src/`; `eye.html` is stale, ignore it).

## Sky's notes
- "I think the closed eye icon is confusing - It's used in multiple contexts - tbh, not sure what it means"
- "if someone sees you off screen their icon can be on the edge of screen with arrow to indicate some interaction happening?"
- "it was unclear who you were shooting. I thought aiming would be enough but it's not. Does need some visual cue"
- On the plate over the patrol robot: "showing icon over moving robot makes me think it's attacking or homing or something else. As a user I only care about if it can see me or not (which needs to be watched to get it's cone / notifcation) - the route is secondary. Would be same for the guard or anything else."
- On the page's red rings: "which one is player?"
- "I think muzzle flash and hit actions would help"

## Decision (Sky, 2026-10-31)
"Let's try glyphs recommendations. Can setup in the fight lab we have for quick iteration. Looking at the test, the muzzle , flash, and jolt animations on fire / hit did seem to give it more notice"

This is a try, not a ruling: lab switches on the fight range (GDD §11 playground: "each a switch, none a ruling"). No GDD edit, and no change to the Handover's default, until Sky has played it and says so.

## Glyph's pick (the page's preset 2)
| Cue | What |
|---|---|
| Who's mine | A ring on all four agents, also in a fight: thicker than today's unselected ring, on a dark edge. Selected keeps its thick ring and arrow |
| Target | Corners on every target, self-picked ones too, with the shooters' faces above. The focus ring goes, or it keeps saying "target" |
| Incoming fire | The agent keeps the turquoise ring; a red dart on it points at the shooter. Never a red ring: that means "your named target" in the build (`HudToolkit.TargetRings.cs:47`) |
| Shots | Orange means fired, white means hit. Flash orange `#ff7a1a` with core `#fff3c4` and a dark edge, about 2.5× today's star, 0.16 s, along the shot. Hit: bigger white spark, 0.3 s. Miss: dust puff, 0.22 s, no thin line, so it never outlasts a hit |
| Jolt | View only, so replays stay exact. Hit: body flashes white 0.12 s, knocked 15 px along the shot (in over 0.06 s, out over 0.44 s). Fire: a 5 px kick back over 0.14 s. Values in `briefs/084/src/cues.js` `bodyReact` |
| Edge icons | One list: the watcher's face on the screen edge with an arrow and what it's doing (sees you, calling it in, coming to you, firing at you). Known watchers only. Clicking jumps the camera. Replaces the edge marks in `Lock`, `Reroute`, `Rivals` and the off-screen chevron in `Attention` |
| Watchers that can't see you | Dashed, hollow cone; fills back red over the last 10 s. Time left on hover and Alt only ("Can't see you, back in 18 s"). No plate over anything that moves. The closed eye goes. Route lines hover only, never in a fight |
| Never watched | Sky's rule stands: no cone, a hover line only |

Engine facts behind the cones: a rerouted robot and a looped camera can't see; a guard called away still can, his cone walks with him (`GameRerouteWalk.cs:6`, `GameSight.cs:129-143`).

## Weak spots to judge at 1× in play
Shots fall in beats, so a volley can strobe: if it's "way too crowded", drop the jolt first, then shrink the flash. White on real models, and pale on pale. A flash firing up the screen covers his own head. Four rings plus darts is more than the 0.35 fight review wanted. The guard's door window leaves the street, which may hurt timing the Enter.

## Done-check
Sky opens the fight range from the menu, spawns hostiles, and in under a minute can switch each cue between as built and the pick, tune the flash and jolt live without a rebuild, and judge them on real figures on a floor the colour of the paving.

## Owners
Latch (the lab switches and the cues), Glyph (reviews on real figures), Forma (bigger hit reactions, only if Sky keeps the jolt), Fable (hover lines, on adoption), Aster (GDD §10 and §15, art bible and style contract for the orange, on adoption).

## After this round
`HandoverHost.cs` (1,680 lines) is split by topic to under 500 lines each (Sky: "I like max 500 LoC per file usually"), then Splice gives each part its own row in `tools/game/proofmap.json`. Then `HudToolkit.Less.cs` (1,886), `HudToolkit.cs` (1,300), and the other 17 Runtime/Play files over 500, as they're touched.
