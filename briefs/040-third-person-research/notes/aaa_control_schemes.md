# Third-person camera and control conventions in AAA action games (PC mouse and keyboard)

Scope: GTA V, Red Dead Redemption 2, The Witcher 3, The Last of Us Part I/II and Uncharted (PC ports), Assassin's Creed (Valhalla, Mirage), Hitman (2016 to World of Assassination), Death Stranding, and Zelda: Breath of the Wild (controller, for camera ideas). Context: a PC tactics game where the player sometimes drops into one agent in third person. The designer wants the camera to respect the direction of travel: if the player turns around, the character runs that way and the camera slowly pans round behind.

Caveat on RDR2 Steam links: several RDR2 Steam-thread claims come from search-result summaries that covered more than one thread. The thread linked for a given quote may not be the exact one; re-check before quoting verbatim.

Source quality: few official documents exist for these behaviours. Most keybind lists come from fan guide sites, and most camera-behaviour claims come from Steam threads. Each one is marked **[community]**. Claims taken from official settings text, patch notes or GDC listings are marked **[official]**. Claims from press reviews are marked **[review]**.

## Movement: camera-relative? What happens on S/back? Does the camera auto-recentre, and after how long?

### Takeaway
Every game here uses camera-relative movement. The best documented example of the designer's request is TLOU Part I's **Camera Assist**: "Automatically reorients the camera in the direction of your movement," with On, Off, Horizontal and Vertical options. Elsewhere, recentring is either always on and hard to disable (GTA vehicles) or a by-product of movement tuning (Witcher 3: the camera "pans slowly in the direction you press" in the default mode, removed in the alternative mode). I found no source giving recentre delays in seconds for any game.

### Cited Findings
- **TLOU Part I, Camera Assist [official settings text via press]:** options are On, Off, Horizontal and Vertical. The description reads "Automatically reorients the camera in the direction of your movement." Horizontal and Vertical limit it to one axis. A related option, Navigation Assistance, turns the camera toward the story path when L3 is pressed. In the Motor accessibility preset, Camera Assist is On. — [MP1st, TLOU Part I accessibility list](https://mp1st.com/news/full-list-of-the-last-of-us-part-i-accessibility-features-revealed); preset: [Inverse PC accessibility review](https://www.inverse.com/gaming/last-of-us-tlou-pc-accessibility-review)
- **TLOU Part I, other camera options [official settings text via press]:** Camera Distance (-5 to +5), Field of View (-5 to +5), Camera Shake (1–10), Weapon Sway On/Off, and Persistent Center Dot. — [MP1st](https://mp1st.com/news/full-list-of-the-last-of-us-part-i-accessibility-features-revealed)
- **Witcher 3, patch 1.07 [official changelog]:** added "an alternative movement style for Geralt," optional, under Gameplay > Movement Response. — [GameBanshee changelog](https://gamebanshee.com/kwax8); [DSOGaming](https://www.dsogaming.com/news/the-witcher-3-wild-hunt-patch-1-07-coming-later-this-week-key-features-revealed/)
- **Witcher 3 [review]:** PC Gamer's analysis found the biggest difference was turning sharpness. It noted that in the new mode **the camera no longer pans slowly in the direction you press**, and recommended the mode to newcomers. Complaints before the patch were about how long Geralt takes to start moving from standing, and how fast he goes once moving. This comes from a search-result summary of PC Gamer; a full-page fetch was paywalled, so I could not quote it directly. — [PC Gamer, patch 1.07 movement analysed](https://www.pcgamer.com/au/the-witcher-3-patch-107-geralts-new-movement-mode-analyzed); [Fox News / patch coverage](https://www.foxnews.com/tech/no-free-dlc-this-week-but-the-upcoming-witcher-3-patch-is-a-very-large-update)
- **Witcher 3 next-gen (4.0) [official patch notes via press]:** added a "Close" camera, closer and over the shoulder, which "responds more dynamically to combat and movement." It is set separately for Exploration, Combat and Horseback under Options > Gameplay, alongside the original far camera. — [GameBanshee next-gen changelog](https://www.gamebanshee.com/9aky8); [Vulkk guide](https://vulkk.com/2022/12/14/how-to-change-the-camera-in-the-witcher-3-next-gen/)
- **GTA V vehicle recentre [community, 2015]:** after a mouse look, the camera returns "to the normal position directly behind the car." Players report that the Settings > Camera auto-centre toggle works **only for the first-person vehicle camera**: "nothing for other views or vehicles." It happens even as a taxi passenger. One poster called it "one of the most annoying things in the GTA games." — [Steam: Disable camera auto-center?](https://steamcommunity.com/app/271590/discussions/0/611703999972682423/)
- **GTA V [community]:** "there's no way to do this in third person views, it will always snap back after a few seconds"; another poster says the camera "will auto-center if you start moving." Mods ("Extended Camera Settings", "Lock Camera on vehicle") exist only to turn it off, and an AutoHotkey script nudges the mouse to stop it. — [Steam: Auto center vehicle camera third person](https://steamcommunity.com/app/271590/discussions/0/611703999964185246/); [GTAForums](https://gtaforums.com/topic/788137-disable-camera-center-lock-controller/); [Extended Camera Settings mod](https://pl.gta5-mods.com/scripts/extended-camera-settings); [Lock Camera on vehicle mod](https://www.gta5-mods.com/ca/scripts/lock-camera-reboot)
- **RDR2 [community]:** Controls settings include an option to keep the camera locked or following the horse, set separately for 3rd and 1st person. One poster says there are no such settings for first person. — [Steam RDR2 discussion](https://steamcommunity.com/app/1174180/discussions/0/3153076242548019437)
- **BotW (as a model) [academic or student write-up]:** a Uppsala game-design blog took BotW's camera as its reference: "one joystick to steer the player, and one joystick to rotate the camera round the player," with "the players forward is the same as the cameras." The team lerped the camera's rotation toward the player's heading for "a smooth camera follow." — [Uppsala speldesign blog](https://babel.speldesign.uu.se/?p=1085896)
- **Camera theory [GDC]:** John Nesky's GDC 2014 talk, "50 Game Camera Mistakes" (Journey), includes mistakes about taking camera control away from the player and applying a camera correction right after the player finishes turning the camera. The item numbers (34, 35) come from a fan list, not the slides. — [GDC Vault](https://gdcvault.com/play/1020460/50-Camera); [Game Developer video post](https://gamedeveloper.com/design/video-50-common-game-camera-mistakes----and-how-to-fix-them)
- **Uncharted 3 camera system [GDC 2012, abstract only]:** the player cameras are follow, aim, melee and cover cameras, plus fixed, spline and animated cameras, with an additive shake layer driven by animation. — [GDC Vault, The Cameras of Uncharted 3](https://gdcvault.com/play/1015514/The-Cameras-of-Uncharted)

### Inferences
- The designer's request ("let the player move in that direction, then slow pan around") is TLOU's Camera Assist, or the Witcher 3 default behaviour (camera pans slowly toward the input direction). Both games ship it as an **option**. Witcher 3's alternative mode removed it because some players disliked it, and TLOU exposes per-axis control. This suggests shipping it with an off switch, or a horizontal-only mode.
- GTA's vehicle recentre is the cautionary case. A recentre that fires a few seconds after mouse input, and that the player cannot disable, generated years of complaints and mods. Nesky's "correction right after the player finishes turning the camera" names the same failure. The safe rule: never recentre while the mouse is moving or soon after; recentre only while the character is moving, and smoothly.
- Mouse players tolerate auto-follow less than gamepad players, because the mouse is a position device: the camera stays where the hand put it. Most of these games tune their recentring for the gamepad.

### Gaps
- No source gives a numeric recentre delay or pan speed for any of these games.
- I found no sourced description of what pressing S/back does in each game. From general knowledge, unverified: in GTA V, RDR2, Witcher 3, TLOU, Uncharted and AC the character turns and runs toward the camera, and the camera does not flip behind them immediately. Treat this as unconfirmed.
- I could not confirm whether GTA V's on-foot third-person camera auto-centres; the threads I found cover vehicles.
- RDR2's full camera settings list is not confirmed. Generic guides list "Auto-Center" and "Lock Camera" toggles ([GameWiki.blog](https://gamewiki.blog/how-to-adjust-camera-rdr2)), but they look unreliable and I could not check them against Rockstar.
- The Witcher 3 alternative-mode camera detail comes from a search summary, because the PC Gamer page was paywalled.
- BotW: I found no developer source on its on-foot auto-follow behaviour.

## Mouse look: does the camera auto-follow or stay where the mouse left it? Which settings exist?

### Takeaway
On PC, players expect the camera to stay where the mouse left it. Where a game forces recentring (GTA vehicles), or the framing drifts (AC Mirage's off-centre framing), the Steam threads complain, and the official settings rarely cover third person. Games that handle it well put follow behaviour in a named, scoped toggle: TLOU Camera Assist (per axis), Witcher 3 camera distance per context, RDR2 horse-follow per view.

### Cited Findings
- **GTA V [community]:** the camera auto-centre toggle only covers the first-person vehicle camera, and setting "all options to off under settings/camera" did not stop third-person snapback. — [Steam thread](https://steamcommunity.com/app/271590/discussions/0/611703999972682423/)
- **AC Mirage [community, Dec 2024]:** players say the camera "keeps shifting back and forth during gameplay," that "Basim is always towards the left of the screen and i want it to be in the middle like in the older games," and that no option fixes it. A reply says Ubisoft promised a fix on Discord for 12 months: "This essential feature has been abandoned." — [Steam: Any way to Center the Camera?](https://steamcommunity.com/app/3035570/discussions/0/4632611455209884705/)
- **AC Valhalla and Mirage [community keybind guides]:** both have a Focus Camera input (Valhalla: F, shared with secondary interaction; Mirage: middle mouse click), plus Cinematic Camera on Enter and camera on the arrow keys. — [Shacknews Valhalla keybinds](https://www.shacknews.com/article/121397/pc-keybindings-and-controls-assassins-creed-valhalla); [Frondtech Mirage keybinds](https://frondtech.com/assassins-creed-ac-mirage-pc-keyboard-controls-and-key-bindings/)
- **RDR2 [community]:** V cycles four camera positions: three third-person distances (close, medium, far) and first person. Players also set first person on foot and third person on horseback. — [Steam RDR2](https://steamcommunity.com/app/1174180/discussions/0/2143091644400133682); [Push Square](https://www.pushsquare.com/guides/red-dead-redemption-2-how-do-you-play-in-first-person); [MeinMMO](https://mein-mmo.de/en/red-dead-redemption-2-experte-steuerung-traege,288771)
- **Witcher 3:** camera distance is chosen per context: Exploration, Combat, Horseback. — [Vulkk](https://vulkk.com/2022/12/14/how-to-change-the-camera-in-the-witcher-3-next-gen/)
- **Hitman (Feral manual) [official port manual]:** the mouse moves the view, including peeking from hiding places; the mouse wheel zooms only while aiming a sniper rifle. The manual describes no follow or recentre settings. — [Feral HITMAN manual](https://www.feralinteractive.com/en/manual/hitman/latest/linux/)

### Inferences
- The PC norm: free mouse look that stays where it is left, an explicit "recentre" or "focus camera" key (AC's Focus Camera; TLOU's L3 navigation assist on pad), and any auto-follow as an option, never forced.
- Per-context camera profiles (Witcher 3's exploration, combat and horseback) suit this game's case: one profile when dropped into an agent and walking, another when aiming or fighting.

### Gaps
- I did not obtain full camera-settings menus for GTA V (beyond the first-person vehicle toggle), RDR2, AC Valhalla or Mirage, Hitman 3, or Death Stranding. PCGamingWiki was not fetched within the tool budget.
- Death Stranding: no source on its camera follow behaviour on mouse.

## Aiming: over-the-shoulder offset on RMB, shoulder switch, lock-on versus free aim on PC

### Takeaway
RMB-to-aim with an over-the-shoulder camera is universal. Most games bind a shoulder swap: Hitman V, TLOU Tab while aiming, RDR2 X while aiming. Lock-on is a menu option (RDR2 Normal, Narrow, Wide or Free Aim; TLOU Lock-On Aim Off, On or Auto-Target, with a strength slider). On mouse it is usually off by default, or treated as an accessibility option. Tears of the Kingdom puts its bow camera directly behind Link rather than over the shoulder, for clarity.

### Cited Findings
- **Hitman [official port manual]:** hold RMB to aim, LMB to fire, V to change camera shoulder. — [Feral HITMAN manual](https://www.feralinteractive.com/en/manual/hitman/latest/linux/); [Gfinity, Hitman 3 shoulders](https://www.gfinityesports.com/article/hitman-3-how-to-change-shoulders-xbox-ps4-ps5-pc-stadia)
- **TLOU Part I [community keybind guide]:** RMB aim, LMB fire, Tab shoulder swap while aiming. The same guide also lists Tab for Backpack (a context conflict). — [Destructoid TLOU Part I PC controls](https://www.destructoid.com/the-last-of-us-part-1-all-controls-list-for-pc/)
- **TLOU Part I [official settings text]:** Lock-On Aim Off/On/Auto-Target. It locks the body centre, and the stick picks head or legs; Auto-Target moves to the next enemy "even if they're offscreen." There is a Lock-On Strength slider, Aiming Mode Hold/Toggle, and "Melee While Aiming", which "will disable Shoulder Swap." — [MP1st](https://mp1st.com/news/full-list-of-the-last-of-us-part-i-accessibility-features-revealed)
- **TLOU Part II [GDC abstract]:** the talk "A Second Shot" (Derek Mattson) covers the conflict between the camera's and the character's aim in third-person shooters: camera versus character perception, hit detection, input delay. — [GDC Vault](https://www.gdcvault.com/play/1034215/A-Second-Shot-Improving-Ranged); [GDC schedule](https://schedule.gdconf.com/session/a-second-shot-improving-ranged-combat-in-the-last-of-us-part-ii/899304)
- **RDR2 [community]:** RMB "both aims and locks." X while aiming swaps the shoulder; outside aim there is no shoulder swap. Lock-on modes Normal, Narrow, Wide and Free Aim. Players report the lock on horses dropping after about 3 s. — [Steam RDR2 shoulder thread](https://steamcommunity.com/app/1174180/discussions/0/5788831477026480773); [Steam RDR2 lock-on](https://steamcommunity.com/app/1174180/discussions/0/3826410862732819099); [ProGameGuides](https://progameguides.com/red-dead-redemption/red-dead-redemption-2-controls-xbox-and-playstation/)
- **GTA V [community]:** RMB = "Aim Weapon / Lock-On." — [BisectHosting GTA V controls](https://www.bisecthosting.com/es-es/blog/gta-v-controls-guide-pc-playstation-xbox-keyboard-mouse-gamepad)
- **Zelda TotK bow camera [developer analysis]:** the camera zooms in directly behind Link, not behind his shoulder: "chose clarity over cinematic impact." — [80.lv](https://80.lv/articles/tears-of-the-kingdom-s-bow-aiming-camera-explained)
- **Naughty Dog framing [interview, secondary]:** Uncharted uses a wider camera for exploration; TLOU uses a tighter one that makes the player look over their shoulder for threats. — [Wccftech](https://wccftech.com/naughty-dog-dont-person-uncharted-camera/amp/)
- **Uncharted PC [review]:** reviews are mixed. "Surprisingly intuitive" ([The AU Review](https://www.theaureview.com/games/uncharted-legacy-of-thieves-collection-pc-review/) also says it "feels wonky and awkward"); aim is "floaty" without assist ([Twinfinite](https://twinfinite.net/pc/uncharted-legacy-of-thieves-collection-review-pc)); "accurate and responsive" ([GameRevolution](https://www.gamerevolution.com/review/910970-uncharted-legacy-of-thieves-collection-pc-review-worth-buying-playing)); one reviewer switched to a gamepad for movement and climbing ([Senses](https://www.senses.se/en/review-uncharted-legacy-of-thieves-collection-pc/)).

### Inferences
- For a commander dropping into an agent, RMB aim with a shoulder offset and a single shoulder-swap key (V in Hitman is the closest genre match) is the expected baseline. A lock-on or soft target option fits the game's "commander > avatar" rule and the readable, deterministic play the GDD wants.
- TotK's centred aim camera is an option worth testing for a readable top-down-leaning game: it keeps the aim line on the character's facing.

### Gaps
- No source gives the shoulder offset distances or FOV changes on aim for any title.
- I found no sources for the AC, Death Stranding or Witcher 3 aim and lock-on schemes on PC.

## Interaction: context prompts, hold versus tap, target choice, wheels

### Takeaway
The documented pattern is a single context key (E in GTA V, RDR2, Hitman, TLOU, AC) with the key shown on the prompt. Hitman and AC add a second interaction key. Weapon or item wheels are held on Tab (GTA V, RDR2). The sources barely cover how a game picks one target from several nearby, or how many prompts it shows at once.

### Cited Findings
- **GTA V [community]:** E = "Context Action" (entering places, using objects); F = enter or exit a vehicle; Tab = weapon wheel, mouse wheel cycles weapons. — [BisectHosting](https://www.bisecthosting.com/es-es/blog/gta-v-controls-guide-pc-playstation-xbox-keyboard-mouse-gamepad); [Samurai Gamers](https://samurai-gamers.com/grand-theft-auto-v-gta5/controls-pc/)
- **RDR2 [community]:** E = interact, mount horse, enter vehicle, though guides conflict on E and F. Tab = weapon wheel (tap to holster), number keys select directly. Players complain about "too many systems and mechanics bound to one button or key." — [ComputerBase forum keybind PDF](https://www.computerbase.de/forum/attachments/red-dead-redemption-2-keyboard-commands-pc-pdf.887755/); [MagicGameWorld](https://www.magicgameworld.com/red-dead-redemption-2-pc-controls/); [Steam RDR2](https://steamcommunity.com/app/1174180/discussions/0/4331977787522221714/?ctp=3)
- **RDR2 [community, anecdotal]:** players hold a button on a bystander to focus them, then hold RMB to fill a "Draw" prompt. This illustrates hold-to-fill prompts after targeting a person. — [Steam RDR2](https://steamcommunity.com/app/1174180/discussions/0/1742268065070583609)
- **Hitman [official port manual]:** E = interact, Ctrl (hold) = Instinct, which shows targets and items, I = inventory with arrow keys and Return, Caps Lock = holster. — [Feral HITMAN manual](https://www.feralinteractive.com/en/manual/hitman/latest/linux/); [Gamepressure Hitman 3 keybinds](https://www.gamepressure.com/hitman-iii/controls-keybinds/zfe109)
- **AC Valhalla and Mirage [community]:** E = Primary Interaction, F = Secondary Interaction in Valhalla; Valhalla also has Z for auto-travel along roads. — [Shacknews](https://www.shacknews.com/article/121397/pc-keybindings-and-controls-assassins-creed-valhalla); [DefKey](https://defkey.com/assassin-s-creed-valhalla-shortcuts); [Frondtech Mirage](https://frondtech.com/assassins-creed-ac-mirage-pc-keyboard-controls-and-key-bindings/)
- **TLOU Part I [community]:** E = interact or grab; 1–9 and the mouse wheel pick weapons; Q is listen mode, shared with weapon swap. — [Destructoid](https://www.destructoid.com/the-last-of-us-part-1-all-controls-list-for-pc/)
- **TLOU Part I [official]:** Bow Firing Hold or Taps, Aiming Mode Hold or Toggle. Naughty Dog exposes hold versus tap per action. — [MP1st](https://mp1st.com/news/full-list-of-the-last-of-us-part-i-accessibility-features-revealed)

### Inferences
- One context key (E), with the key and one verb on the prompt, is the PC convention. A second key for a second verb (Hitman's and AC's two-key pattern) covers most people-interactions without a menu. Hold-to-confirm is used for heavy or irreversible acts (RDR2 draw). That matches the GDD's "show the full price before you commit."
- RDR2's "too many things on one key" complaint argues against overloading E across mount, talk and pick up when targets overlap.

### Gaps
- Target selection when several interactables are near (nearest, camera centre, or facing) is not documented in any source I found for these games.
- I found no sources on the number of simultaneous prompts, or on RDR2's person-interaction options (greet, antagonize); those come from general knowledge only.
- Hitman's and Death Stranding's interaction prompt rules are unsourced.

## Default PC keybinds (interact, aim, fire, sprint, cover, weapon wheel)

### Takeaway
There is a de facto standard: WASD, Shift sprint, RMB aim, LMB fire, E interact, C crouch, Tab weapon wheel (Rockstar). Cover is the outlier: Q in Rockstar games, Space in Hitman; TLOU uses automatic cover.

### Cited Findings
| Game | Interact | Aim | Fire | Sprint | Cover | Wheel / weapons | Shoulder | Other | Source |
|---|---|---|---|---|---|---|---|---|---|
| GTA V | E (context) | RMB (aim/lock-on) | LMB | L Shift | Q | Tab wheel; mouse wheel cycle | – | F vehicle, R reload, G grenade | [BisectHosting](https://www.bisecthosting.com/es-es/blog/gta-v-controls-guide-pc-playstation-xbox-keyboard-mouse-gamepad), [Samurai Gamers](https://samurai-gamers.com/grand-theft-auto-v-gta5/controls-pc/) [community] |
| RDR2 | E (guides conflict E/F) | RMB (aim + lock) | LMB | Shift | Q | Tab wheel; mouse wheel; number keys | X while aiming | V camera distance, F6 photo mode | [ComputerBase PDF](https://www.computerbase.de/forum/attachments/red-dead-redemption-2-keyboard-commands-pc-pdf.887755/), [MagicGameWorld](https://www.magicgameworld.com/red-dead-redemption-2-pc-controls/), [Shacknews photo mode](https://www.shacknews.com/article/114795/how-to-use-photo-mode-in-red-dead-redemption-2-on-pc) [community] |
| Hitman (WoA) | E | RMB hold | LMB | L Shift | Space | I inventory; Caps Lock holster | V | Ctrl hold Instinct, C crouch, R reload | [Feral manual](https://www.feralinteractive.com/en/manual/hitman/latest/linux/) [official port manual] |
| TLOU Part I | E | RMB | LMB | L Shift | (contextual) | 1–9, mouse wheel | Tab while aiming | Q listen, F melee, C crouch | [Destructoid](https://www.destructoid.com/the-last-of-us-part-1-all-controls-list-for-pc/) [community] |
| AC Valhalla | E primary / F secondary | – | – | – | – | – | – | F focus camera, Enter cinematic camera, Z auto-travel | [Shacknews](https://www.shacknews.com/article/121397/pc-keybindings-and-controls-assassins-creed-valhalla) [community] |
| AC Mirage | E primary | – | – | – | – | – | – | MMB focus camera, Enter cinematic camera | [Frondtech](https://frondtech.com/assassins-creed-ac-mirage-pc-keyboard-controls-and-key-bindings/) [community] |

- RDR2 keys are remapped under Pause > Settings > Controls > Keyboard & Mouse > Key Mapping, which is only available from the pause menu. — [Twinfinite](https://twinfinite.net/guides/red-dead-redemption-2-pc-controls-remap-keybinds-controls-how/)
- TLOU Part I has three custom control profiles. — [Destructoid](https://www.destructoid.com/the-last-of-us-part-1-all-controls-list-for-pc/)

### Inferences
- If the game adopts WASD, Shift, RMB, LMB, E and Tab-hold for a wheel, PC players will need no teaching. Q/E are already the camera rotate keys in the tactics view (GDD fixed limits). That clashes with E = interact in third person, so the binding needs a deliberate decision per mode.

### Gaps
- No verified defaults for Witcher 3, Uncharted, Death Stranding, or the AC aim, fire, sprint and cover keys.
- Fan guides disagree for RDR2 (E/F interact) and TLOU (Tab double-listed). Check the in-game menus before relying on them.

## Player complaints and praise about each game's camera and controls on PC

### Takeaway
The recurring complaints are forced recentring (GTA vehicles), heavy or laggy movement (RDR2, Witcher 3 before 1.07), and uncorrectable off-centre framing (AC Mirage). Praise goes to options: Witcher 3's movement-response toggle and close camera, and TLOU's granular camera and aim settings. Naughty Dog and Kojima ports are workable on mouse and keyboard, but reviewers still recommend a pad.

### Cited Findings
- **RDR2 [review]:** "Movement is heavy and clunky" is listed as a con ([Windows Central PC review](https://www.windowscentral.com/red-dead-redemption-2-pc-review)), along with "a slight delay between a press of a button and the subsequent action" ([Gamereactor](https://www.gamereactor.eu/red-dead-redemption-2-on-pc/)). An input-lag comparison quotes a tester saying Arthur "feels like a tank"; the suggested fixes are toggle-run and the FPS control scheme ([MeinMMO](https://mein-mmo.de/en/red-dead-redemption-2-experte-steuerung-traege,288771)). Players call it "bad controls, clunky movement" [community] ([Steam](https://steamcommunity.com/app/1174180/discussions/0/4515505180235562943)).
- **GTA V [community]:** players called vehicle camera snapback "one of the most annoying things in the GTA games" and "Needs a fix." The camera fights the player while reversing. — [Steam](https://steamcommunity.com/app/271590/discussions/0/611703999972682423/)
- **Witcher 3 [review/official]:** before 1.07, players complained about start-up delay and turning; CDPR answered with an optional mode. The 4.0 Close camera had existed only as a PC mod before. — [Fox News](https://www.foxnews.com/tech/no-free-dlc-this-week-but-the-upcoming-witcher-3-patch-is-a-very-large-update); [Vulkk](https://vulkk.com/2022/12/14/how-to-change-the-camera-in-the-witcher-3-next-gen/)
- **AC Mirage [community]:** players report off-centre framing they cannot correct, and a fix promised but never shipped. — [Steam](https://steamcommunity.com/app/3035570/discussions/0/4632611455209884705/)
- **Death Stranding PC [review]:** one review calls the controls "simple and intuitive." Another says the keyboard gives Sam only two speeds (jog and sprint), and that the balance controls rely on analogue triggers, so it recommends a pad. A third calls the controls "functional, but not very fun." — [TheSixthAxis](https://thesixthaxis.com/2020/07/13/death-stranding-pc-review); [TechRaptor](https://Techraptor.net/gaming/previews/death-stranding-pc-impressions); [DualShockers](https://www.dualshockers.com/death-stranding-pc-review-kojima/); [GameRevolution](https://www.gamerevolution.com/?p=652629)
- **Uncharted PC [review]:** reviews are split (see the Aiming section). — [The AU Review](https://www.theaureview.com/games/uncharted-legacy-of-thieves-collection-pc-review/); [Senses](https://www.senses.se/en/review-uncharted-legacy-of-thieves-collection-pc/)
- **TLOU Part I PC [review]:** praised for accessibility depth, including three control profiles and Camera Assist in presets. — [Inverse](https://www.inverse.com/gaming/last-of-us-tlou-pc-accessibility-review)

### Inferences
- Two lessons for the drop-in agent. (1) Responsiveness beats animation weight: RDR2's realism-first movement is the most-cited complaint, and Witcher 3 had to patch in a snappier mode. A commander who drops in briefly needs instant turns, not momentum. (2) Any automatic camera behaviour must be gentle, delayed until the mouse is idle, tied to movement, and switchable off. GTA and Mirage show what happens otherwise.
- The designer's "slow pan around" is a known, liked behaviour when it is an option (TLOU Camera Assist, Witcher default). Suggested parameters, unsourced and to be tuned by playtest: only while moving, only after the mouse has been idle briefly, yaw only (matching TLOU's "Horizontal"), with ease-in.

### Gaps
- I did not search Reddit directly; the community evidence is Steam and GTAForums.
- No Digital Foundry analysis of camera or input latency was found within budget.
- BotW: no player-reception or developer sources on its camera beyond the Uppsala write-up and the TotK bow analysis.
- Hitman: no complaints or praise found about its camera.
