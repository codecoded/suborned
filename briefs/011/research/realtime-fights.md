# Research for brief 011: running and real-time shoot-outs (Motive, 2026-10-15)

Asked by Aster for Sky's two questions: "How do I make players sprint?" and "I want realtime shoot outs like Syndicate. How would we add that to the controls?" I used web search and fetch only. Most sources are reviews and forum posts, not manuals, so each fact below is tagged with how sure it is. The GDD and briefs stay the source. This file is reference only. Glyph's `syndicate-hud.md` (same folder) covers the HUD side and isn't repeated here.

## What the sources say

| Game | Fact | How sure | Source |
|---|---|---|---|
| Syndicate (1993) | Left click moves the agents; the right button aims and fires. | two independent reviews agree | https://www.hardcoregaming101.net/syndicate/ · https://www.vogons.org/viewtopic.php?p=1300123 |
| Syndicate | Clicking both buttons together "will instantly put your agents into an automated state of extreme self defence"; once the threat is gone you carry on with the mission. | a fan article; the name "panic mode" comes from guides, not the manual | https://www.strifestreams.com/articles/2022/syndicate/ · https://www.dosgameclub.com/forums/topic/syndicate-101/ |
| Syndicate | Players "hit the panic button whenever attacked". | review | https://www.hardcoregaming101.net/syndicate/ |
| Syndicate | Police "will shoot your men down if they happen to see them with a weapon out." A drawn weapon is itself the provocation. | review | https://www.hardcoregaming101.net/syndicate/ |
| Syndicate | Agents are picked by their portraits on the left, or by key. Moving all four at once is easier but "significantly reduces the 'strategy' involved". Orders get no confirmation, which is the known flaw. | review | https://www.hardcoregaming101.net/syndicate/ |
| Syndicate | The Persuadertron is short range: the target follows the agent who fired it. Persuaded people "shoot whenever you shoot". | review, design notes | https://www.hardcoregaming101.net/syndicate/ · https://www.dcs.ed.ac.uk/home/scotm/Syndicate.doc |
| Syndicate | Weapons trade range against rate of fire: the Uzi has "same range as pistol but much higher fire rate"; the minigun has a "devastating rate of fire" but is "extremely heavy". | fan design document, Giant Bomb | https://www.dcs.ed.ac.uk/home/scotm/Syndicate.doc · https://giantbomb.com/wiki/Games/Syndicate |
| Syndicate: American Revolt | Only one agent or all four can be controlled, never groups of 2–3. Enemy miniguns can "mow down your entire level-3-modified group within seconds." | review | https://www.ibiblio.org/GameBytes/issue18/greviews/syndicat/synd.html |
| Syndicate Wars (1996) | Agents "run whenever you double click"; the original's agents saunter. Pacing is faster and more action-led. | review | https://www.hardcoregaming101.net/syndicate-wars/ |
| Syndicate Wars | One shared energy pool replaces separate ammunition, so there is no reload. Police now fire on agents carrying a Persuadertron. Killing civilians costs money. | review | https://www.hardcoregaming101.net/syndicate-wars/ |
| Syndicate Wars | Readability failures: skyscrapers hide the action, the view is dark and zoomed in too close, and "agents are often shot before you grasp the situation". The fights read as "overwhelmingly chaotic". The PlayStation version made large structures transparent. | review | https://www.hardcoregaming101.net/syndicate-wars/ |
| Satellite Reign (2015) | Before release, agents stood in the open and fired only when ordered. Players asked for automatic cover and used Shift to queue kill orders. Grenades destroy most cover. | Steam forum, 2015 | https://steamcommunity.com/app/268870/discussions/0/520518688945446783/?ctp=3 · https://steamcommunity.com/app/268870/discussions/0/520518688949533082 |
| Satellite Reign | Pressing G with a group selected throws four grenades at once (group orders). Repeated grenade use raised "the alert levels to even crazier degrees". | Steam forum | https://steamcommunity.com/app/268870/discussions/0/520518688949533082 |
| Satellite Reign | Rewards preparation: setting crossing fields of fire, and "sealing doors to prevent reinforcements". Subversion and silent takedowns "won't expose you to gunfire from all directions." | review | https://www.theskinny.co.uk/tech/gaming/satellite-reign |
| Satellite Reign | Two bars per agent (health, energy). Players asked for a pausable real-time mode. | wiki, forum | https://satellitereign.fandom.com/wiki/Agents?oldid=2916 · https://steamcommunity.com/app/268870/discussions/0/451848854996200673 |
| Desperados III (2020) | Guns need ammunition, and their noise can alarm guards, so players weigh going loud against staying hidden. An alarm brings reserves out of buildings; a guardhouse stops sending them once empty. | developer interview, forum | https://www.pressreader.com/australia/play-uk-1/20191217/281672551823545 · https://steamcommunity.com/app/610370/discussions/0/2527030866855530223 |
| Desperados III | Aiming a gun shows a circle around the character; anyone inside it hears the shot. | beginner guide, via search summary (the page refused fetch) | https://attackofthefanboy.com/guides/desperados-iii-ultimate-beginners-guide-tips-tricks-and-strategies/ |
| Desperados III | Showdown Mode co-ordinates several characters' actions; it pauses time "except on higher difficulties". View cones have zones, and detection fills the cone before the alarm. | Wikipedia | https://en.wikipedia.org/wiki/Desperados_III |
| Shadow Tactics (2016) | Open combat "is also out of question in most cases, as it will raise an alarm". An alarm sends extra patrols out from nearby buildings. A found body raises it too. | reviews, Wikipedia | https://en.wikipedia.org/wiki/Shadow_Tactics:_Blades_of_the_Shogun · https://blogs.tuni.fi/playlab/game-reviews/shadow-tactics-blades-of-the-shogun-game-review/ |
| Hitman: Contracts | NPCs turn toward Agent 47's sprinting footsteps but are much less aware of him walking. Hitman 2 mostly forbids running, by design. | speedrun guide, forum | https://www.speedrun.com/hitman3/guides/a9zd9 · https://steamcommunity.com/app/6850/discussions/0/1743355067088361372 |
| Hitman 3 | Suspicion isn't the same as being spotted: you are "spotted" only after an illegal act is seen. | guide | https://www.redbull.com/gb-en/hitman-3-tips-guide |
| Metal Gear Solid V | Hearing ranges are tiered by pace: about 30 m sprinting, 20 m running, 7 m crouched. | forum | https://steamcommunity.com/app/287700/discussions/0/364039785166894068 |
| Into the Breach (2018) | No hit percentages: every attack is telegraphed. The developers wanted every death to feel like the player's own fault. | developer interview, reviews | https://gamedeveloper.com/game-platforms/road-to-the-igf-subset-games-i-into-the-breach-i- · https://nintendolife.com/reviews/switch-eshop/into_the_breach |

I didn't find a manual-level source for Syndicate's weapon numbers or Satellite Reign's cover rules. I didn't find Shadow Tactics' running-noise radius either: searches returned other games. Nothing below relies on those.

## What I took from it (used in briefs/011/motive-fights.md)
1. **Left click moves, right click fires, both together is the panic button.** This matches Sky's approved scheme (left selects and moves, right acts), so shoot-outs fit it with no new mouse buttons.
2. **Double click to run** is Syndicate Wars' own control and Sky's approved one.
3. **A drawn weapon is an act.** Syndicate's police fire on a visible weapon. Desperados and Shadow Tactics make noise and alarms the price of guns. Our city already prices acts by who sees them, so drawing is priced the same way.
4. **Running is noticed, walking is not** (Hitman, MGS V). Running's cost is attention, not stamina.
5. **No dice the player can't read** (Into the Breach). Hits come from a deterministic rate the card shows as "down in 4 s", never as a roll.
6. **Short and lethal, but readable** (Desperados, Shadow Tactics against Syndicate Wars' chaos): few hostiles, a visible health pip per hit, a tracer per shot, and a distinct miss.
7. **Group orders and a fire-at-will stance answer Satellite Reign's complaint** that agents stood in the open and fired only when ordered. The panic order is the one-input version.
8. **Alarms bring reserves** (Desperados, Shadow Tactics). Our loud branch already sends police and starts the seal clock, and the response unit already on the street joins the fight.
