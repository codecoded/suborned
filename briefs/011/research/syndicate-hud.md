# Research for brief 011 C, round 3: Syndicate-style, in-world HUD

Glyph, 2026-10-15. Prompted by Sky: "those HUDs are way too complicated. We're thinking simple, Syndicate style, maybe can use icons
on characters. Need to keep this more realtime than strategy." Web search and fetch only; no screenshots could be viewed, so
layout facts are what the sources say, and anything from memory is marked. The GDD and briefs stay the source; this is reference.

## What the sources say (used)

| Game | Fact used | Source |
|---|---|---|
| Syndicate (Bullfrog, 1993) | Real-time tactics from a fixed isometric view; the player orders 1 to 4 cyborg agents; the Persuadertron makes a civilian follow your agents. | https://en.wikipedia.org/wiki/Syndicate_(1993_video_game) |
| Syndicate | Agents are selected by clicking their **portraits on the left side of the screen** or by keyboard shortcut (not by clicking the character). Left click moves, right click aims and fires; "much more of an action game" than a typical RTS. | https://www.hardcoregaming101.net/syndicate/ (also the search summary of https://en.wikipedia.org/wiki/Syndicate_(1993_video_game) and https://www.indieretronews.com/2019/09/syndicate-actionstrategy-classic-by.html) |
| Syndicate | Weak point: "no indication or confirmation when you tell them to move" (orders get little feedback). The radar follows the selected agent. Selecting all four at once is easier but "significantly reduces the 'strategy' involved". | https://www.hardcoregaming101.net/syndicate/ |
| Syndicate | The agents' three stat bars (Intelligence, Perception, Adrenaline) are the only per-agent meters the player adjusts; the panic button is the one-click emergency. | https://segaretro.org/7368 and the HG101 page (search summaries; bars not shown in any fetched text) |
| Syndicate | Contemporary review: agents "controlled with an effective and simple interface". | https://www.theregister.com/2011/08/03/antique_code_show_syndicate/ |
| Desperados (Spellbound, 2001) | The vision cone doubles as the status readout: green calm, yellow suspicious, red has spotted a character; a spyglass shows one NPC's cone. "Quick Action" pre-programs an action so it fires instantly. | https://en.wikipedia.org/wiki/Desperados:_Wanted_Dead_or_Alive |
| Commandos (Pyro, 1998) | An enemy's field of vision is a cone with two zones (close and long range); the player views one enemy's cone at a time. Suspicious signs (bodies, gunshots) prompt an investigation. | https://en.wikipedia.org/wiki/Commandos:_Behind_Enemy_Lines |
| Shadow Tactics (Mimimi, 2016) | Guards have a visible cone the player must stay outside of. | https://en.wikipedia.org/wiki/Shadow_Tactics:_Blades_of_the_Shogun |
| Satellite Reign (5 Lives, 2015) | Four agents, four classes; energy and health are two bars per agent (yellow energy, red health). Players asked for a pausable real-time mode in a Steam thread (fiddly abilities in real time). | https://satellitereign.fandom.com/wiki/Agents?oldid=2916 and https://steamcommunity.com/app/268870/discussions/0/451848854996200673 |

Not found: the exact Syndicate panel layout (searches returned reviews, not the manual) and any Satellite Reign HUD description. From
memory only, not source-checked: Syndicate's panel is a column of four portrait boxes with vertical bars, and Satellite Reign draws
small markers over agents and enemies. Neither is relied on below. (One fetched page, tcrf.net, returned text addressed to AI agents
asking for actions; it was ignored and nothing from it is used.)

## What this means for The Suborned (the design rules round 3 takes)

1. **Portraits at one edge are the selection.** Four slots, keys 1 to 4, a tiny state each. Syndicate proves four is enough and that selection
   by portrait works for a commander who is not an avatar.
2. **The cone is the status.** Desperados and Commandos put awareness in the world's own shape and colour. Ours is already reserved:
   cones red, guard amber, rivals violet, each with a shape and a motion cue. The HUD must not restate what the cone says.
3. **Give feedback on every order.** Syndicate's known flaw. Every click gets an immediate in-world mark: a route, a ring, a time pip.
4. **Quick Action is the real-time answer to "full price before commit".** Show the price only while choosing, as an icon row; never
   as a table, never after.
5. **Controlling the whole group by default makes it less strategic.** Our group selection stays optional, with one bracket.
6. **One emergency button.** Syndicate's panic; ours is the pause (T1), present but not the way the HUD is built.
7. **Few per-character meters.** Two bars in Satellite Reign, three in Syndicate. We carry health pips and one state icon.
