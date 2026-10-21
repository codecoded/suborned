# Research for brief 027: how Syndicate's fights work (Motive, 2026-10-20)

Asked by Aster for Sky: "The fire fights right now is very bad. Need to make that like syndicate." This file is reference only. The GDD and briefs stay the source.

Earlier research I don't repeat here: `briefs/011/research/realtime-fights.md` (Syndicate, Syndicate Wars, Satellite Reign, Desperados, Shadow Tactics, Into the Breach) and Glyph's `briefs/011/research/syndicate-hud.md`.

## Sources and how sure each one is

| # | Source | What it is | How sure |
|---|---|---|---|
| M | Syndicate DOS manual (Bullfrog/EA, 1993): https://www.dosdays.co.uk/media/games/syndicate/synd_manual_dos.pdf | The original manual. I pulled its text out of the PDF's compressed streams with a small zlib script, since no PDF tool was installed. | primary |
| L | lubiki.pl weapon tables: http://syndicate.lubiki.pl/synd/html/synd_weapons.php (the site's TLS certificate has expired, so I read it with `curl -k`) | A fan reference. It gives cost, ammo and range in game units for each weapon. | fan data. It agrees with the manual's text. |
| W | lubiki.pl Syndicate Wars controls: http://syndicate.lubiki.pl/swars/html/swars_controls.php | A fan transcription of the PC and PlayStation controls. | fan data, but detailed |
| F | FreeSynd `data/ref/weapons.dat`: https://github.com/videogamepreservation/freesynd (`freesynd/trunk/data/ref/weapons.dat`) | An open-source reimplementation. Its loader carries the comment "TODO: calibrate weapon time for shot and reload, angle, accuracy". | Indicative only. Its timings are the reimplementers' guesses, not Bullfrog's. |
| H1 | Hardcore Gaming 101, Syndicate: https://www.hardcoregaming101.net/syndicate/ | Retrospective review | review |
| H2 | Hardcore Gaming 101, Syndicate Wars: https://www.hardcoregaming101.net/syndicate-wars/ | Retrospective review | review |
| WP | Wikipedia, Syndicate (1993): https://en.wikipedia.org/wiki/Syndicate_(1993_video_game) | Summary and reception | secondary |

## What the sources say

### Firing and targeting (M)
- **Drawing is its own act.** "left-click a weapon icon … the active agent pulls the weapon from under his/her overcoat." The manual advises selecting the weapon "well before you reach your target, where things might be hectic and swift."
- **Aim is the cursor; range is a colour.** "Aim with the cursor and the targeting icon appears. When this is red the target is within range of your chosen weapon." That one cue is the whole range read.
- **Click fires once; hold sprays.** "Right-click to fire the agent's weapon once. If you are firing an automatic weapon, hold down the right button to spray bullets around the Mission Zone with insane abandon."
- **Holstering hides you.** De-selecting the weapon slips it "back inside the overcoat" to "avoid detection by law enforcement officers."
- **Panic mode.** Press both buttons together: "Your agent automatically selects a weapon and fires a salvo of hot lead. Levels also shoot up to maximum … It ain't pretty, but it is effective."
- **Group mode.** Click the group icon and the agents "respond to commands as a unit"; "plot one course and all the agents follow it." Splitting again takes one click on a single agent's box. In group mode the screen shows only the last activated agent's weapons.

### IPA: how much the agent decides for itself (M)
- The bars "modify the behavior of an agent when left to its own cybernetic devices."
  - Intelligence "controls an agent's reactions to a given situation."
  - Perception "improves precise firing and alerts an agent to danger earlier."
  - Adrenaline "controls speed of reactions" and walking speed.
- High adrenaline with low intelligence "reacts quickly but erratically: he may fire wide or too soon."
- "It's a good tactic to crank up Levels if leaving agents on lookout … They fire to defend themselves while awaiting the next command."
- With everything at maximum, the agent has "a high degree of operational independence." With only Adrenaline at maximum, "you may need to take control of firing."
- Raised levels wear off, and dependency builds: "future injections need to be greater to have the same effect." Resting the levels makes the next boost stronger.
- Read for us: IPA is a dial between autonomy and accuracy/speed that you set ahead of time and pay for later. It is a preparation tool, not something you aim with.

### Who shoots when (M, H1)
- **Police** "cannot fire on Syndicate agents until weapons are drawn." They give a spoken warning first: "when you hear the warning, 'Police! Put down your weapons!', it's a good idea to take note and get that uzi back in your overcoat." Police "recognize your cyborgs … as soon as a weapon is drawn, and they come straight for you."
- **Enemy agents** "don't stand on ceremony … If you're not careful they are firing before you can even arm a cyborg, so move quickly."
- **Civilians** go about their business until you attack them (H1). Players "hit the panic button whenever attacked" (H1).

### Weapons (M text; L ranges and ammo, in game units where one tile is 256; F timings indicative)
| weapon | manual's line | range (L) | ammo (L) | F: time per shot + reload (ms) | F: damage per shot |
|---|---|---|---|---|---|
| Pistol | "Medium range … easily outclassed but useful as a backup weapon" | 1280 (5 tiles) | 13 | 200 + 600 | 2 |
| Shotgun | "large spread … let down by its lack of effective range. At close range … real hurt" | 1024 (4) | 12 | 250 + 200 | 2 × 6 pellets |
| Uzi | "Fast rate of fire and medium range … the mainstay of any offensive force" | 1792 (7) | 50 | 150 + 100 | 2 |
| Minigun | "Devastating rate of fire and a decent range … huge weight" | 2304 (9) | 500 | 100 + 75 | 2 |
| Long range rifle | "Very long range and extremely accurate … picking off individual targets" | 6144 (24) | 30 | 900 + 400 | 2 |
| Flamer | "stream of ignited jelly that sticks to targets and burns. Very short range but devastating … close crowd clearance" | 512 (2) | 1000 | 70 + 50 | 8 |
| Laser | "cuts through its targets and anyone … foolish enough to get in the way" | 4096 (16) | 5 | 300 + 200 | 32 |
| Gauss gun | "three rockets … destroying tanks or eliminating large groups of people" | 5120 (20) | 3 | 350 + 1500 | 64, area |
| Persuadertron | short range: the target "lose[s] all free will and follow[s] the agent who fired." Civilians always convert; guards, police and agents need enough persuaded followers first (manual chart: at brain version 0, 4 civilians for a guard, 8 for a policeman, 32 for an agent) | 256 (1) | unlimited | n/a | n/a |
| Time bomb | "Drop the time bomb. Then leg it!" | — | 1 | — | area |
| Energy shield | "withstand all projectile weapons … a very short life span" | — | — | — | — |

- **Weight is a cost.** "Carrying a Flamer slows down an agent much more than does a Pistol or an Uzi." Loadout is decided at the team screen before the mission, inside a budget.
- **Weapons are taken from the dead.** Point at a body and the Grab icon appears.
- **Persuaded people** "shoot whenever you shoot" (briefs/011 research, H1). The crowd you have converted joins the fight.

### Syndicate Wars (1996): what it changed (W, H2)
- **Double-click runs.** Agents "run whenever you double click." Pacing is "more action-packed" (H2).
- **Fewer weapon bookkeeping tasks.** Shared energy instead of ammunition removes "arsenal juggling." Shields regenerate. Pistol and shotgun are gone, and the Uzi is the default weapon (H2).
- **Controls (W):**
  - "Auto-Defend mode: Left click and right click together (temporarily ungroups your agents)."
  - "Sel/Des Weapon for group: Right click on weapon tab."
  - "Switch Grouped Agents To Same Weapon" (PlayStation).
  - Two drugs, injected with Alt up/down: "RED MIST/BLUE FUNK."
  - A "Danger track" swaps the music when a fight starts.
- **What went wrong (H2):** the fights read as "overwhelmingly chaotic."
  - Agents are "riddled with bullets by the time you get a grasp on the situation."
  - Buildings hide the action, and the view is dark.
  - Killing civilians costs money, which the review calls "quite annoying."

### Health, energy and drugs (rev. 2, for Sky's question "Did syndicate have energy levels?")

**Syndicate (1993), from the manual (M):**
- **Health is a bar.** "The Health Bar next to each Status Box indicates how much life is left in the cyborg. When the white hits the floor so does the agent." Then: "These cyborgs can take some punishment, but when there's more black than white showing it's time to take evasive action. When the Health Bar is completely black, the Cyborg is dead."
- **Healing.** "The Health Bar can be restored using a MediKit." The MediKit "restores a single agent's health but may only be used once." The manual names no regeneration. A fan search found none either; regenerating health belongs to the 2012 reboot's chips, not to 1993.
- **Durability comes from mods.**
  - Chest: "a metallic chest to help it survive direct hits"; it also holds the self-destruct charge.
  - Heart: "improves overall physical strength and durability."
  - Eyes: "better an agent's accuracy when firing."
  - Legs: speed.
  - Each comes in versions V1 to V3, bought and researched.
- **Energy Shield.** It "can withstand all projectile weapons. Due to the immense power drain, this item has a very short life span."
- **IPA.** The drug levels are the 1993 game's only "levels": intelligence, perception, adrenaline, with dependency (see the IPA section above). They aren't an energy pool.

**Syndicate Wars (1996):**
- **Everything recharges.** "The health, shields and weapon energy (which functions in the place of specific ammunition) of the player's agents recharge over time" (Wikipedia, https://en.wikipedia.org/wiki/Syndicate_Wars).
- **One shared pool.** "All of your energy comes from the same source, which means if you wore out your minigun you don't have ammo in your rocket launcher. Furthermore that's the same energy source your agents draw from to activate heat vision mode or their personal force fields." (lubiki.pl Syndicate Wars tips, http://syndicate.lubiki.pl/swars/html/swars_tactics_tips_common.php)
- **What changes the refill.** "The better the cybernetics in your agents, the faster your energy will replenish. You can also increase the replenish speed by running." And: "Upon firing the Long Range Rifle (LRR), de-equip it. This will eliminate the pause before your energy recharges." (same page)
- **Drugs and healing.** "While pumped full of psychotropic drugs at ANY amount, your agents will recover hit points extremely slowly." (same page). The drugs are Red Mist and Blue Funk, injected with Alt up/down (W). HG101 says the drugs "just send your agents into varying states of panic" (H2).
- **The shield sits on top of health.** A search summary of the Syndicate Wars manual says the shield bar must be emptied before health takes damage, and that it needs minutes to recharge. I couldn't read the scanned manual myself (https://db.hfsplay.fr/files/2020/06/19/Syndicate_Wars_-_1997_-_Electronic_Arts_n2ujpdz.pdf), so treat this as secondary.
- **Medikits remain** ("Always have healing kits with you", the tips page).

**Read for us:**
- In 1993, health is a bar, healing is a one-use item, and toughness is bought before the mission.
- In Wars, there is one recharging energy pool for weapons and the shield, a shield over health, and health regeneration.
- Our nearest equivalents:
  - health bar: §06 health;
  - Medikit: the turned doctor;
  - mods: the quartermaster's armour.
- Wars' energy pool has no counterpart yet. That is option A in brief 027.

## What I take from it (used in brief 027)
1. **The verbs are few:**
   - draw (select a weapon);
   - right click on someone (once, or held to keep firing);
   - both buttons for panic;
   - holster to hide.
   Range is one colour on the cursor. Everything else is preparation: loadout, IPA, the drugs.
2. **Autonomy is a setting you prepare, not a fight-time menu.** With high levels, the agents "fire to defend themselves while awaiting the next command." Our three-way stance (V) is the in-fight version of a setting Syndicate made before the fight.
3. **Fights are fast because weapons are fast**, not because hits are certain. In F's indicative data the Uzi and the minigun fire about 4 to 6 times a second. The weapon set trades range against rate of fire against weight.
4. **The police warning is a readable beat with a choice**: put the gun away or fight.
5. **The Persuadertron is Syndicate's version of turning people.** For us that is the Fixer's Turn and crowd joiners (§06 "join"), never a gun: people > powers.
6. **The chaos has a ceiling** (Syndicate Wars): if agents are shot before the player has grasped the situation, the fight is no longer readable. Keep few hostiles, a big read on who is hurt, and sound for every shot.
