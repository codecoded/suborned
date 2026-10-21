# Squad, tactics and hybrid games: commander view plus direct control of one unit

Scope: how squad, tactics and hybrid games combine an overview or commander layer with direct control of one unit, how they issue orders to the others, and what players praised or complained about. The target is a PC real-time tactics game (The Suborned: a cell of four, the player can take one agent in a third-person chase camera while the other three act on orders).

Method note: most findings come from search-result summaries of reviews, wikis, guides and forums. Direct page fetches were attempted where useful; StrategyWiki's SWAT 4 controls page returned HTTP 403, so that page could not be read in full. Claims from Steam or EA forums, fan wikis and user reviews are marked **[community]**. Platform is noted where a source describes controller rather than mouse-and-keyboard (M+KB) controls.

## How do these games handle switching, orders from the avatar, synced targets, slow-motion or pause, and context actions?

### Takeaway
Three working patterns exist. (1) **Avatar-first, aim-to-command** (Ghost Recon, Rainbow Six Vegas, Brothers in Arms, SWAT 4, Freedom Fighters): you look at a spot or enemy and one button sends the obvious order, with a menu or wheel for anything else. (2) **Commander-first with a freeze-and-queue mode** (Shadow Tactics, Desperados III, Commandos: Origins): pause, give each unit one action, run them all at once. (3) **Overview RTS with an optional "take control" key** (Satellite Reign: Backspace). Synced marked shots are the best-loved squad feature in the avatar-first group, but when they are the *only* squad order, reviewers say they flatten play. Players complain most about lost or interrupted queued orders, no pause or hold-position, unclear "what is my agent about to do", and too many keybinds.

### Cited Findings

**Ghost Recon: Wildlands (2017)**
- Sync Shot: you tag a target with the drone, binoculars or gun sights; a prompt marks it and a number appears over its head. The squad takes time to line up, and you can't fire until you hear them confirm. Then you either fire yourself (teammates fire with your shot) or order them to. It starts with one teammate and is upgraded to all three — [Twinfinite](https://twinfinite.net/?p=265009)
- A co-op review describes the result as suppressed shots landing "almost simultaneously" — [Co-Optimus](https://co-optimus.com/review/1992/page/2/tom-clancy-s-ghost-recon-wildlands-co-op-review.html)
- PC default binding reported as Mark Enemy = Q (third-party controls guide) — [MagicGameWorld](https://www.magicgameworld.com/?p=3146)
- Sources disagree on whether Sync Shot works in co-op: one guide says single-player only, while a troubleshooting article says co-op partners must be in the same instance — [Twinfinite](https://twinfinite.net/?p=265009); contradicted by [West Games](https://west-games.com/?p=26761)

**Ghost Recon: Breakpoint (2019; AI teammates added July 2020)**
- AI teammates were missing at launch and arrived in a mid-2020 title update (sources give the number as 2.0.1 or 2.1.0). Orders included hold position, regroup, engage, and move to a marked point and take cover. Sync shots were set by aiming at a target and pressing the prompt, and teammate sync shots could combine with the drone for up to seven enemies — [Neowin](https://www.neowin.net/news/ai-teammates-are-finally-coming-to-ghost-recon-breakpoint-on-july-15/); [Xbox Wire](https://news.xbox.com/en-us/2020/07/12/ghost-recon-breakpoint-ai-teammates-update/); [GameChronicles](https://gamechronicles.com/tom-clancys-ghost-recon-breakpoint-adds-tu-2-1-0-including-ai-teammates)
- Design detail: each teammate's second weapon is always a standard rifle, so sync shots still work if the first is an LMG or shotgun. The system guarantees the synced action can always run — [GameChronicles](https://gamechronicles.com/play-tom-clancys-ghost-recon-breakpoint-with-ai-teammates-today)
- TU 4.0.0 (May 2021): teammates use their special abilities on their own when conditions are met, and the player can switch that off — [MP1st](https://mp1st.com/title-updates-and-patches/ghost-recon-breakpoint-update-1-15-may-25-shoots-out-for-tu-4-0-0)
- In Breakpoint, sync shot also exists as a consumable "sync shot drone" item (Use Item key) — [Shacknews](https://shacknews.com/article/114259/how-to-use-sync-shot-in-ghost-recon-breakpoint)

**Ghost Recon: Future Soldier (2012)**
- Marking up to three targets lets you order the squad to fire, or fire yourself to trigger them. It works only while enemies are unaware — [Wikipedia](https://en.wikipedia.org/wiki/Tom_Clancy%27s_Ghost_Recon:_Future_Soldier)
- Sync Shot is described as the only squad command; the squad's commands are "restricted to choosing whom to shoot and when" — [PC Gamer](https://pcgamer.com/ghost-recon-future-soldier-review); [Giant Bomb](https://giantbomb.com/reviews/tom-clancys-ghost-recon-future-soldier-review)
- Reception was split: one review found it empowering, another said it removes player choice and makes the game predictable, and a third said Sync Shot's power makes the campaign feel dull — [The Artifice](https://the-artifice.com/ghost-recon-future-soldier-review/); [The Register](https://www.theregister.com/2012/05/30/review_game_ghost_recon_future_soldier); [Expert Reviews](https://www.expertreviews.co.uk/technology/gaming/ghost-recon-future-soldier-review)
- A review notes limited authority over orders and squad movement, which it sees as the price of better squad AI — [Giant Bomb user review](https://giantbomb.com/user-reviews/uid/5248/ghost-recons-bombastic-shift-falters-in-its-execut) **[community]**

**Rainbow Six Vegas (2006)**
- One context button: you mostly look at something and press A, and the order depends on where you are and what you aim at (a door gives breach options; elsewhere the team moves to that point) — [Giant Bomb user review](https://giantbomb.com/user-reviews/id/1433/infuriatingly-difficult-but-incredibly-intense-and) **[community]**; [GameRevolution FAQ](https://www.gamerevolution.com/?p=38457)
- The D-pad holds secondary orders (hold or regroup), and a bumper toggles rules of engagement between Assault (lethal) and Infiltration (passive). One global stance changes how every order is carried out, so the order list stays short — [GameRevolution FAQ](https://www.gamerevolution.com/?p=38457)
- The fan wiki says the command scheme came from Ghost Recon Advanced Warfighter — [Rainbow Six fandom wiki](https://rainbowsix.fandom.com/wiki/Tom_Clancy%27s_Rainbow_Six:_Vegas?oldid=7889) **[community]**
- Trusted Reviews praised the simplified command system and the squad's initiative; an Obsidian-forum review criticised feedback that came only as teammate audio — [Trusted Reviews](https://www.trustedreviews.com/reviews/tom-clancy-s-rainbow-six-vegas-xbox-360); [Obsidian forums](https://forums.obsidian.net/blogs/entry/50-rainbow-six-vegas-review) **[community]**
- Platform note: these sources describe the console (Xbox 360) layout.

**Brothers in Arms: Road to Hill 30 (2005)**
- Developer description: look at a good firing position and press the command button; your character gives the hand signal and the team moves there, takes cover and fires. Press again on an enemy and the team focuses fire ("unload") on it — [GameSpot Q&A](https://gamespot.com/articles/brothers-in-arms-road-to-hill-30-qanda-final-thoughts/1100-6119621/)
- The command interface opens in real time, without a pause — [GameSpot Q&A](https://gamespot.com/articles/brothers-in-arms-road-to-hill-30-qanda-final-thoughts/1100-6119621/)
- There are four commands (form up, move, suppressing fire, assault). Suppression lowers enemy fire rate and accuracy, which sets up the flank — [GameSpot walkthrough](https://gamespot.com/articles/brothers-in-arms-road-to-hill-30-walkthrough/1100-6120725/)
- A 2005 preview puts the command menu on the left trigger (console). The PC mouse binding was not found — [Daily Star](https://archive.thedailystar.net/rising/2005/04/03/center.htm)

**SWAT 4 (2005)**
- There are two command modes, set in options: Graphical (right-click opens a context menu built from what the cursor is over) and Classic (number keys walk the SWAT 3 command tree) — [TweakTown](https://www.tweaktown.com/gaming/2248/swat_4_single_player/index.html); [StrategyWiki](https://strategywiki.org/wiki/SWAT_4/Controls)
- Pointing at a door and holding right mouse gives "Open and Clear", "Open, Bang and Clear" and so on; a locked door gives pick or breach; a civilian gives zip-cuff — [GameSpot review](https://gamespot.com/reviews/swat-4-review/1900-6121423/)
- The commands on offer also depend on teammates' health — [Giant Bomb user review](https://giantbomb.com/user-reviews/id/340/fun-tactical-experience) **[community]**
- Tab cycles which element (team) receives orders — [GameSpot hands-on](https://www.gamespot.com/articles/swat-4-final-hands-on/1100-6121046); [MagicGameWorld](https://www.magicgameworld.com/swat-4-gold-edition-pc-keyboard-controls/)

**Ready or Not (2023)**
- Most commands run from a single button, and in single-player you can enter wheel menus to command the squad — [Stevivor](https://stevivor.com/reviews/ready-or-not-review-not-ready/)
- No source was found for the exact binding or for specific complaints about the wheel (see Gaps).

**Freedom Fighters (IO Interactive, 2003)**
- Three orders, each on its own button (attack/advance, defend, regroup/recall). Tap sends the order to the nearest squadmate; hold sends it to the whole squad — [Giant Bomb](https://giantbomb.com/wiki/Games/Freedom_Fighters); [GameSpot preview](https://www.gamespot.com/articles/freedom-fighters-preview/1100-6073064/)
- Attack doubles as move: aim at a place to send the squad, aim at an enemy to attack it — [GameSpot preview](https://www.gamespot.com/articles/freedom-fighters-preview/1100-6073064/)
- Recall makes allies fall in behind the player and leave cover — [Giant Bomb](https://giantbomb.com/freedom-fighters/3030-10853/)
- Squad size is gated by Charisma, earned through heroic acts such as healing civilians and wounded fighters, up to 12 members — [Giant Bomb](https://www.giantbomb.com/games/3030-10853/)
- The squad positions itself, so you don't need to micromanage, though you can. It was credited with making squad tactics accessible on console — [GameSpot preview](https://www.gamespot.com/xbox/action/freedomthebattleforli/preview_6071314.html/); [Giant Bomb](https://giantbomb.com/wiki/Games/Freedom_Fighters)

**Mass Effect 2 / 3 (PC)**
- On PC, holding Left Shift opens the squad command interface (the power wheel), which pauses combat. Mapping powers to hotkeys lets you use them in real time without pausing — [Fextralife ME2 wiki](https://masseffect2.wiki.fextralife.com/Combat) **[community]**; [GamesRadar](https://gamesradar.com/mass-effect-2-review-2-2/3)
- Q and E send squadmate 1 or 2 to move to, or attack, the target under the crosshair (one key per squadmate, context-sensitive) — [EA forums](https://forums.ea.com/discussions/mass-effect-franchise-discussion-en/squad-commands-are-broken-for-km/9191739) **[community]**
- ME1 used arrow-key hotkeys for squad orders (Up = Move, Left = Attack) — [Prima eGuide](https://primagames.com/eguides/mass-effect-pc-eguide/essentials/combat)
- Legendary Edition complaint: with M+KB, squad move or attack orders don't work while paused in the tactics HUD, though they work with a controller — [EA forums](https://forums.ea.com/discussions/mass-effect-franchise-discussion-en/squad-commands-are-broken-for-km/9191739); [Steam](https://steamcommunity.com/app/1328670/discussions/0/3117034765002144509) **[community]**
- Note: the "one key per squadmate" reading of Q/E reflects the standard ME2/ME3 PC layout as players describe it. The cited forum thread confirms Q/E give move or attack orders but was not read in full.

**Shadow Tactics (2016) / Desperados III (2020) / Commandos: Origins (2025)** (commander-first, freeze-and-queue)
- Shadow Tactics' Shadow Mode lines up one action per character and fires them all together with a single input, so planning can clear a room in one go — [PressReader / Mac Life review](https://www.pressreader.com/australia/mac-life/20170207/281642484874719)
- Mimimi's creative director: the late-90s and early-2000s tactical stealth games they admired paired "slow planning" with "very fast and precise execution" — [Gamereactor](https://www.gamereactor.eu/?p=30953)
- Desperados III's Showdown Mode freezes time (except on the highest difficulty) so you can queue actions for all characters. Reviewers called it "an excellent quality of life feature" — [Shacknews](https://www.shacknews.com/article/118611/desperados-3-review-quickest-save-in-the-west); [TheSixthAxis](https://www.thesixthaxis.com/2020/06/12/desperados-3-review/)
- Complaint: entering Showdown again before actions finish cancels them, which can leave a teammate exposed — [GameCritics](https://gamecritics.com/brad-gallaway/desperados-iii-review/)
- Complaint: plans break when an enemy spots a body and starts a randomised patrol; trial and error is "the first hurdle" — [TechRaptor](https://Techraptor.net/gaming/reviews/desperados-3-review); [Way Too Many Games](https://waytoomany.games/2020/06/14/review-desperados-iii/)
- Commandos: Origins' Command Mode turns the screen greyscale and lets you give one action per commando, then unpause. One review says it makes the gamepad controls "almost bearable" — [TheSixthAxis](https://thesixthaxis.com/2025/04/09/commandos-origins-review); [Shacknews](https://shacknews.com/article/143819/commandos-origins-review-score)
- Commandos: Origins was criticised for its interface: selecting commandos, picking abilities and starting takedowns were "unintuitive and, worst of all, laggy". Users report Command Mode being "bugged and inconsistent", with characters not doing what they were told — [Shacknews](https://shacknews.com/article/143819/commandos-origins-review-score); [Vaporlens summary of Steam reviews](https://vaporlens.app/app/1479730/commandos_origins) **[community]**

**Satellite Reign (2015)** (closest overview analogue to Syndicate)
- Backspace takes or releases direct control of the selected agent or mech. Pause is tactical (you can still give orders). Ctrl+F1–F4 saves groups and F1–F4 recalls them — [Steam guide/discussion](https://steamcommunity.com/app/268870/discussions/0/520518688941640674) **[community]**
- Some players say there is no full tactical pause in normal play, only the Support agent's Team Stims (time at about 10%) and a pause in the mission-control screen — [Steam](https://steamcommunity.com/app/268870/discussions/0/351660338704778867) **[community]**. Note: this conflicts with the guide above that lists a tactical Pause key; it may depend on version or mode.
- Complaints: no hold-position order, agents hard to keep in cover, agents don't fire unless ordered, slow-motion felt sluggish — [Steam](https://steamcommunity.com/app/268870/discussions/0/350533172686874856) **[community]**
- Reviewer: "a myriad of keybindings" that feel unfamiliar even to RTS veterans; a wish for "something that made it clear to see what agents were about to do" — [The Mancunion](https://mancunion.com/2015/10/14/satellite-reign-review/)

**Syndicate (Starbreeze, 2012)**
- A single-player FPS with no squad orders in the campaign; co-op is the squad mode — [Game Informer](https://gameinformer.com/games/syndicate/b/pc/archive/2012/02/21/the-revered-strategy-title-makes-the-jump-to-fps)
- The DART Overlay slows time and highlights targets, even through cover. "Breach" (hold a button while a progress bar fills over the target) unlocks doors, turns turrets and strips armour. Apps are limited by adrenaline earned from kills and objectives — [Giant Bomb](https://giantbomb.com/wiki/Games/Syndicate_2012); [Game Informer](https://gameinformer.com/games/syndicate/b/pc/archive/2012/02/21/the-revered-strategy-title-makes-the-jump-to-fps)

**XCOM: Chimera Squad (2020)** (turn-based; included for synced openers)
- Each encounter opens with Breach Mode: you pick breach points with different trade-offs, then get a free group turn of shots, abilities or breach items. Stack order sets turn order and where each unit takes cover — [2K FAQ](https://support.2k.com/hc/en-us/articles/360045913454--XCOM-Chimera-Squad-FAQ); [Edge via PressReader](https://www.pressreader.com/australia/edge/20200521/283046941663088)
- Interleaved turns are shown on a HUD timeline and above enemies' heads. Edge says this visibility makes encounters easier than classic XCOM — [Wikipedia](https://en.wikipedia.org/wiki/XCOM:_Chimera_Squad); [Edge via PressReader](https://www.pressreader.com/australia/edge/20200521/283046941663088)

**Jagged Alliance 3 (2023)**
- Real-time exploration until the squad is noticed, then turn-based combat. Outside combat you can move the whole squad or single mercs, set up takedowns and flanks — [Shacknews](https://www.shacknews.com/article/136241/jagged-alliance-3-review-score); [PC Gamer](https://pcgamer.com/jagged-alliance-3-review)
- Complaint: no pause in the real-time phase, so it's hard to give orders quickly before being spotted — [MP1st](https://mp1st.com/reviews/jagged-alliance-3-review-mercenaries-for-hire)

### Inferences
- **Two directions for quick target locks in a firefight.** The Ghost Recon model (mark from the avatar's aim; teammates confirm; fire on your shot) and the Mass Effect model (one key per squadmate sends that member to the target under the crosshair) both work without leaving the avatar. Ghost Recon's numbered markers over enemies' heads and Mass Effect's per-member keys suit a four-agent cell well: three keys for three non-controlled agents, or one "mark" key that adds the target to the next free agent.
- **Synced shots need a guarantee and a "ready" signal.** Wildlands' audible "in position" confirmation and Breakpoint's fixed backup rifle stop a synced order from failing silently. That is the readability problem Satellite Reign players complained about ("what are agents about to do").
- **Sync as the only command is a trap.** Future Soldier shows that one strong synced order, with nothing else, makes play feel predictable. Sync should be one order among a few.
- **Freeze-and-queue is loved, but cancelling must be explicit.** Desperados' accidental cancellation and Commandos: Origins' "laggy and inconsistent" execution are the main complaints. A queued order should stay until the player clears it, and its state should be visible in the world.
- **A global stance shrinks the order set.** Vegas's Assault and Infiltration toggle changes how every order is carried out, so the order list stays short. This could map onto The Suborned's quiet/loud distinction (Trail/Alert cost).
- **Pausing to command is expected on PC.** JA3's and Satellite Reign's lack of a full pause drew complaints. The ME Legendary Edition M+KB bug shows players want orders to work during a pause. Given The Suborned's "preparation > improvisation" pillar, a pause or slow command mode fits.
- **Taking control should be one key.** Satellite Reign's Backspace take/release is the nearest overview-to-avatar precedent. No source assessed how well it played (see Gaps).

### Gaps
- No primary Mimimi talk or interview was found that explains *why* Shadow Mode was built as it is. The GDC 2018 talk by Moritz Wagner is reported to cover the game's design but was not reviewed. Exact PC bindings for Shadow Mode and Showdown were not confirmed.
- No IOI postmortem on Freedom Fighters' command design was found. Only 2003-era reviews and previews are cited.
- Ubisoft developer statements on the design of sync shots were not found.
- Brothers in Arms' and Rainbow Six Vegas' PC mouse bindings were not confirmed. The sources describe console layouts.
- Ready or Not: the wheel's binding and specific player complaints were not found (one search returned nothing relevant).
- SWAT 4's "default command" (middle-mouse fast order) could not be confirmed because the StrategyWiki page returned 403. It is left out of the findings.
- Commandos: Origins' Command Mode bindings and its avatar/switch behaviour are not covered by reliable sources.
- No source evaluates Satellite Reign's direct-control (Backspace) mode.

## What does the evidence say about radial menus versus single contextual prompts, and how much on-screen text is too much?

### Takeaway
No controlled study compares radial menus with single contextual prompts in games. HCI work on marking menus shows radial selection gets much faster with practice (gesture marks were about 3.5× faster than popping up the menu) but is not self-explanatory. In games, the best-received squad schemes use one context button for most of the work, with a menu or wheel for the rest. Complaints cluster around too many keybinds and too little feedback, not around wheels as such.

### Cited Findings
- Kurtenbach and Buxton's field study of marking menus: making the mark was on average 3.5× faster than selecting from the popup menu, and experts still went back to the popup to check the layout — [Autodesk Research: User learning and performance with marking menus](https://www.research.autodesk.com/publications/user-learning-and-performance-with-marking-menus/)
- Kurtenbach's thesis: marks "are not intrinsically self-explanatory" the way buttons and menus are, so speed trades against discoverability — [Autodesk Research: The Design and Evaluation of Marking Menus](https://research.autodesk.com/publications/the-design-and-evaluation-of-marking-menus)
- A 1988 comparison of radial and linear menus is cited for about 15% faster selection and fewer errors (via Wikipedia; the original paper was not checked) — [Pie menu (Wikipedia mirror)](https://demo.azizisearch.com/lite/wikipedia/page/Pie_menu)
- Fitts's-law argument (theoretical, not tested here): radial slices are equidistant and effectively unbounded outward, so they are easy to hit — [IIT Guwahati Virtual Lab](https://iitg.ac.in/cseweb/vlab/creative-design-prototyping/Fitts.html); [300Mind (practitioner opinion)](https://300mind.studio/blog/radial-menus-in-game-design/)
- Rainbow Six Vegas: "mostly just look at something and press A", with secondary orders on the D-pad. The simplified system was praised — [Giant Bomb user review](https://giantbomb.com/user-reviews/id/1433/infuriatingly-difficult-but-incredibly-intense-and) **[community]**; [Trusted Reviews](https://www.trustedreviews.com/reviews/tom-clancy-s-rainbow-six-vegas-xbox-360)
- SWAT 4 lets players pick between a cursor-context menu (Graphical) and a number-key tree (Classic), which serves both new and expert players — [TweakTown](https://www.tweaktown.com/gaming/2248/swat_4_single_player/index.html)
- Ready or Not: most commands on one button, with wheels for the rest — [Stevivor](https://stevivor.com/reviews/ready-or-not-review-not-ready/)
- Overload complaints: Satellite Reign's "myriad of keybindings"; Commandos: Origins' "unintuitive" ability selection — [The Mancunion](https://mancunion.com/2015/10/14/satellite-reign-review/); [Shacknews](https://shacknews.com/article/143819/commandos-origins-review-score)
- Feedback complaints: Vegas's audio-only confirmations; Satellite Reign's unclear upcoming agent actions — [Obsidian forums](https://forums.obsidian.net/blogs/entry/50-rainbow-six-vegas-review) **[community]**; [The Mancunion](https://mancunion.com/2015/10/14/satellite-reign-review/)
- Mass Effect's wheel pauses combat, which turns the wheel's slowness into planning time; hotkeyed powers skip the wheel — [GamesRadar](https://gamesradar.com/mass-effect-2-review-2-2/3); [Fextralife](https://masseffect2.wiki.fextralife.com/Combat) **[community]**

### Inferences
- **A layered model is best supported:** one context key for the obvious action (most uses), a radial or wheel for alternatives that opens on hold (ideally pausing or slowing time), and direct hotkeys for experts. This matches marking-menu evidence (novices browse, experts flick) and the best-received games (Vegas, SWAT 4, ME).
- **Where a wheel opens on hold and pauses, its slowness costs nothing.** Where it runs in real time (Brothers in Arms), it has to stay small (four orders).
- **Text budget:** no source gives a word or element count. The evidence is that feedback should live in the world (numbered markers over targets, hand signals, greyscale command mode) rather than in panels. That fits Sky's "less reading" direction.

### Gaps
- No controlled or CHI PLAY study comparing radial menus with single contextual prompts in games was found.
- No quantitative guidance (word counts, element counts) on how much on-screen text is too much for tactics HUDs was found.

## Which games have the cleanest "one key does the obvious thing" design, and how do they show it?

### Takeaway
The strongest examples are **Rainbow Six Vegas** (one context button reads what you're aiming at: a door gives breach, floor gives move), **Freedom Fighters** (one button per order; aim chooses move or attack; tap means nearest ally, hold means everyone), **Brothers in Arms** (one command button; place means move and cover, enemy means focus fire; shown by the avatar's hand signal) and **Mass Effect 2/3** (Q/E: one key per squadmate, crosshair target chooses move or attack). **Ghost Recon's Sync Shot** is the cleanest synced-target scheme (mark → number above head → "ready" call → your shot fires all).

### Cited Findings
- Vegas: the result of the one button depends on location and aim target — [GameRevolution FAQ](https://www.gamerevolution.com/?p=38457); [Giant Bomb user review](https://giantbomb.com/user-reviews/id/1433/infuriatingly-difficult-but-incredibly-intense-and) **[community]**
- Freedom Fighters: tap for nearest squadmate, hold for whole squad; attack doubles as move — [Giant Bomb](https://giantbomb.com/wiki/Games/Freedom_Fighters); [GameSpot preview](https://www.gamespot.com/articles/freedom-fighters-preview/1100-6073064/)
- Brothers in Arms: look, press, and the avatar gives a visible hand signal. Pressing on an enemy means focus fire — [GameSpot Q&A](https://gamespot.com/articles/brothers-in-arms-road-to-hill-30-qanda-final-thoughts/1100-6119621/)
- Mass Effect 2/3: Q/E per squadmate on the crosshair target — [EA forums](https://forums.ea.com/discussions/mass-effect-franchise-discussion-en/squad-commands-are-broken-for-km/9191739) **[community]**
- Ghost Recon Wildlands: mark, number over head, teammate confirmation, then fire on your shot — [Twinfinite](https://twinfinite.net/?p=265009)
- SWAT 4: the menu content is built from what's under the cursor (door, locked door, civilian) — [GameSpot review](https://gamespot.com/reviews/swat-4-review/1900-6121423/)

### Inferences
- **For The Suborned's three needs:**
  - Quick target locks: aim at an enemy plus a per-agent key (ME style), or a Mark key that numbers targets and assigns them to free agents (Ghost Recon style), firing on the player's shot.
  - Contextual default: one key (E or click) whose result is decided by the object under the reticle (Vegas, SWAT 4), with hold to open alternatives.
  - Showing the default: put a small icon or verb on the object itself, and have the agent react visibly (hand signal, nod, marker number) instead of a text panel.
- **Tap-versus-hold for scope** (Freedom Fighters) adds "one agent or all agents" to a single key at no extra cost.
- **What players will punish:** orders that silently fail or cancel, agents that won't hold position, and no way to see what agents are about to do.

### Gaps
- No source directly compares the readability of how these games show their default action (icon versus verb versus none). This is inferred from the descriptions above.
- No PC-specific usability data was found for any of these schemes; several of the cleanest (Vegas, Freedom Fighters, Brothers in Arms) were designed controller-first.
