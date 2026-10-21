# Third-person camera accessibility, comfort and motion sickness: guidance, defaults and expected PC options

Scope: what causes discomfort with third-person (non-VR) cameras; what the accessibility guidelines recommend; any numbers for auto-rotation speed and recentre delay; which camera and control options PC players expect; and whether a camera that stays where the player left it beats auto-recentre.

Source quality key: **[Primary guideline]** = platform or industry accessibility standard. **[Peer-reviewed]** = journal or conference study. **[Developer data]** = shipped game settings or engine documentation. **[Practitioner]** = developer or accessibility-writer opinion. **[Anecdotal]** = a single player's or reviewer's experience.

---

## 1. What causes simulator / motion sickness in third-person games?

### Takeaway
The best-supported causes are: (a) visual self-motion (vection) that the body does not feel; (b) camera motion the player did not cause, such as auto-rotation, forced camera swings, shake and bob; and (c) *changes* in rotation velocity (acceleration), which are worse than constant rotation. FOV mismatch and bob or smoothing (any gap between input and camera motion) are recognised triggers. Peer-reviewed work on *third-person, flat-screen* games specifically is thin. Most of the mechanism evidence comes from optokinetic drums, desktop simulations and console-game postural-sway studies.

### Cited Findings

**Mechanism: sensory mismatch and vection**
- XAG 117 explains motion sickness as a mismatch between the visual sense of self-motion and the lack of body motion felt by the vestibular system. It names "camera field of view, camera bobbing, motion blur, weapon sway, automatic changes in camera angle" as the decisions that matter. [Primary guideline] — [Xbox Accessibility Guideline 117](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/117)
- Nooij et al. (PLoS ONE, 2017) rotated a visual scene about the vertical axis at a constant **60°/s**, after a 30°/s training trial, with n=15 analysed. 12 of 15 reported sickness in at least one session. **Vection gain** was the only significant within-participant predictor of VIMS, explaining 39% of variance (partial R² 48% with a random slope). Eye movements (OKN) and head movements were not significant predictors. A fixation target reduced VIMS, but it also reduced vection. [Peer-reviewed] — [Nooij et al., "Vection is the main contributor to motion sickness induced by visual yaw rotation", PLoS ONE](https://journals.plos.org/plosone/article?id=10.1371/journal.pone.0175305)
- Visual stimuli that induce strong vection produce more VIMS than stimuli inducing weak vection (summary of related work, from a search result snippet). [Peer-reviewed, secondary] — [PMC6223881, "More vection means more velocity storage activity"](https://www.ncbi.nlm.nih.gov/pmc/articles/PMC6223881/)

**Acceleration and changes in rotation speed**
- Bonato et al. found that *changing* the rotation velocity of an optokinetic drum increased sensory conflict and motion sickness more than constant velocity, comparing 30°/s and 60°/s (from abstract snippets via search; full text not read). [Peer-reviewed] — [Rotation velocity change and motion sickness in an optokinetic drum (PubMed 16909874)](https://pubmed.ncbi.nlm.nih.gov/16909874/); [ResearchGate copy](https://www.researchgate.net/publication/6877409_Rotation_velocity_change_and_motion_sickness_in_an_optokinetic_drum)
- Nooij et al. argue that a classic vestibular-conflict signal is expected mainly during *angular acceleration*, not during constant-velocity rotation. [Peer-reviewed] — [Nooij et al. 2017](https://journals.plos.org/plosone/article?id=10.1371/journal.pone.0175305)

**Camera motion the player did not cause (auto-rotation, forced cuts, shake)**
- The Can I Play That? editorial (Stacey Jenkins, 8 Dec 2020) names loss of camera control as the main issue: "we need to give the control back to players." Its examples are kill-cam swings where "the camera whips round", surprise screen shake on heavy hits or enemy roars, and scripted character wobble. It says options to turn these off "can be the difference between playable and unplayable." [Practitioner / accessibility press] — [Can I Play That?, "We Need to Talk About Motion Sickness"](https://caniplaythat.com/2020/12/08/we-need-to-talk-about-motion-sickness/)
- The same article cites Keshavarz & Hecht (2012): **active gaming causes less VIMS than passively watching**. It also cites Chang et al. (2012): more than half of participants felt motion sickness after an hour of play. Both are cited second-hand; the original papers were not read. [Peer-reviewed, cited second-hand] — [Can I Play That?](https://caniplaythat.com/2020/12/08/we-need-to-talk-about-motion-sickness/)
- Stoffregen's group (driving game in an HMD): participants who *drove* were less likely to report motion sickness than "passengers" who watched recordings of the game. The authors read this as evidence that control matters. This comes from a search-result summary; the full text was not read. [Peer-reviewed, secondary] — [Frontiers in Virtual Reality 10.3389/frvir.2020.581132](https://www.frontiersin.org/journals/virtual-reality/articles/10.3389/frvir.2020.581132/text)
- Stoffregen et al. (2008), "Motion sickness and postural sway in console video games", *Human Factors* 50:322–331, is the main console-game study. Its findings were not retrieved. A 2011 follow-up with a commercial console game reported sickness in **67% of adults and 56% of 10-year-olds** (no significant difference), with postural changes appearing *before* symptoms. [Peer-reviewed] — [UCSB CDGR entry](https://cdgr.ucsb.edu/database/motion-sickness-and-postural-sway-console-video-games); [Experimental Brain Research 2011](https://link.springer.com/doi/10.1007/s00221-011-2993-4)
- Final Fantasy VII Remake review (Elizabeth Garcia): "the camera reorients by itself as if it is fixed… When Cloud turns, the camera turns whether you touched it or not". Also: "the jumping back and forth between wide and narrow angles" made it "physically ill". She was limited to about 45 minutes per session, and toggling the camera settings did not fix it. [Anecdotal, single reviewer] — [Can I Play That?, "Final Fantasy VII Remake Broke My Heart"](https://caniplaythat.com/2020/05/05/final-fantasy-vii-remake-broke-my-heart/)

**Bob, sway, smoothing (any difference between input and camera motion)**
- Game Accessibility Guidelines (Intermediate): "Avoid (or provide option to disable) any difference between controller movement and camera movement". Common culprits include asynchronous weapon or walk bob, motion blur, chromatic aberration, **mouse smoothing**, and camera shake or tilt. "The more differences implemented the greater the chance of issues." [Primary guideline] — [GAG: difference between controller movement and camera movement](https://gameaccessibilityguidelines.com/avoid-or-provide-option-to-disable-any-difference-between-controller-movement-and-camera-movement/)
- XAG 117: "Avoid any repetitive side-to-side or up-and-down on-screen movement, except that which is core to game play", including weapon sway and camera bob. [Primary guideline] — [XAG 117](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/117)
- Maddy Miller (developer blog, Dec 2024, updated Jun 2025) names FOV ranges, screen shake and bob, motion blur and large repeated textures as triggers, with head bob as her most common one. She reports 30–40 fps as her personal "sweet spot" and sickness from Starfield at 60 fps because of bob. [Anecdotal] — [Motion Sickness Accessibility in Video Games](https://madelinemiller.dev/blog/motion-sickness-accessibility/)

**FOV**
- GAG: "If the field of view is significantly different to what the eye/brain expects to see, it can result in motion sickness", causing "nausea and disorientation" severe enough that play beyond a few minutes can be impossible. [Primary guideline] — [GAG: set an appropriate FOV default](https://gameaccessibilityguidelines.com/if-the-game-uses-field-of-view-3d-engine-only-set-an-appropriate-default-for-the-expected-viewing-environment/)
- Desktop study (de Vries, Bos, van Emmerik, Toet; Toet et al. 2008 SPIE; Bos et al. 2010 *Applied Ergonomics*/*Displays*) compared the internal FOV the game renders with the external FOV the screen fills. Counter-intuitively, **congruent** iFOV/eFOV produced *more* cybersickness. The authors suggest the incongruent conditions were extreme enough to reduce vection. Cybersick participants rated the environment less pleasant and more arousing. Only abstracts were available; the full text was not read. [Peer-reviewed] — [Toet et al. 2008, TNO PDF](https://publications.tno.nl/publication/34606587/yP0Sml/toet-2008-cybersickness.pdf); [Bos et al., Displays 2010](https://www.sciencedirect.com/science/article/abs/pii/S0141938210000934); [HKUST VIMS 2007 proceedings](https://www.ieda.ust.hk/dfaculty/so/pdf/Pages89-95-VIMS2007.pdf)

**Speed and stability of follow**
- Andreas Buehler (Game Developer blog, Nov 2019, citing Isbister & Schaffer 2011): "A camera movement that is too fast or too slow can also lead to nausea". Also: "The player gets sick if the camera moves too precisely with the movements of the main character". A camera that holds height until the character passes a threshold, so it does not follow every jump, is described as "the best solution to prevent nausea". No numbers are given. [Practitioner] — [Game Developer: Third Person Camera View in Games](https://www.gamedeveloper.com/design/third-person-camera-view-in-games-a-record-of-the-most-common-problems-in-modern-games-solutions-taken-from-new-and-retro-games)
- John Nesky (*Journey*), GDC 2014 "50 Camera Mistakes": the abstract lists "triggering simulation sickness" among the main camera failures. The talk's specific items were not retrieved. [Practitioner] — [GDC Vault: 50 Camera Mistakes](https://gdcvault.com/play/1020460/50-Camera)

**Third person versus first person**
- Third-person views with the character on screen are reported as easier than first person by some players: "seeing the player character on screen helps the brain register that you are not moving", and first-person games are unplayable after 15 minutes while third-person ones are "hit-or-miss". [Anecdotal] — [itch.io comment](https://itch.io/post/8572580); [personal blog](https://git.32bit.cafe/yequari/yequari.com/src/branch/master/content/blog/2025/motion-sickness-accessibility.md)
- XAG 117 recommends letting players choose between first- and third-person views as a motion-sickness measure. [Primary guideline] — [XAG 117](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/117)

### Inferences
- Several independent strands point the same way: an auto-rotation the player did not ask for is the riskiest kind of camera motion. The drivers-vs-passengers result, Keshavarz & Hecht's active-vs-passive result, CIPT's "give control back" and the FF7R testimony all agree. It is uncommanded vection, it often starts and stops abruptly (acceleration), and its timing cannot be predicted.
- Velocity *changes* matter more than steady motion. So if a camera must rotate automatically, eased start and stop (bounded angular acceleration) is likely more important than the peak speed.
- The desktop FOV result is not a simple "wider is better". Less vection means less sickness, and a high, steady, distant camera produces relatively little vection. An overhead tactics camera at 38° pitch is in the low-risk class, while a close over-the-shoulder camera is in a higher one.

### Gaps
- No peer-reviewed study was found that measures motion sickness *specifically for third-person camera behaviours* (auto-follow, recentre, orbit lag) on flat screens.
- The Stoffregen 2008 console findings, the full Bos/Toet FOV numbers, Keshavarz & Hecht 2012 and Chang 2012 were not read in full; the PDFs could not be extracted. Their figures above come from abstracts or secondary citations.
- The content of Nesky's GDC talk (specific rules on auto-rotation) could not be retrieved.

---

## 2. What do guidelines recommend? (FOV, auto-centre optional, speed sliders, shake, toggle vs hold, remapping, sensitivity and smoothing)

### Takeaway
The guidelines agree on: an adjustable FOV, a way to turn off automatic camera movement, separate horizontal and vertical sensitivity (XAG: at least ±50% of default), sliders or toggles to remove shake, bob and blur, full remapping including Esc and per-axis invert, and toggle alternatives for every hold. The only numeric defaults found are GAG's FOV guide (about 60° for TV, about 90° for a monitor) and XAG's ±50% sensitivity range.

### Cited Findings

**FOV**
- XAG 117: "Provide adjustable field of view settings", so players can pick the least nauseating angle "based on their sitting distance from their screen and other factors". Examples: Halo Infinite (FOV slider with explanatory text: lower FOV reduces "fishbowl" and helps motion sickness), Minecraft (up to 110°), and Assassin's Creed Valhalla (close/far third-person camera choice). [Primary guideline] — [XAG 117](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/117)
- GAG: "An appropriate viewing angle is usually 60 degrees for TV, 90 degrees for monitor." A companion guideline says FOV should be adjustable because no single default fits every screen size and distance. The page quotes a player: "If your game is locking at a low FOV, I am not buying it… many of us seem to get sick at low FOV" (anecdotal). [Primary guideline + anecdote] — [GAG: FOV default](https://gameaccessibilityguidelines.com/if-the-game-uses-field-of-view-3d-engine-only-set-an-appropriate-default-for-the-expected-viewing-environment/); [GAG: allow FOV adjustment](https://gameaccessibilityguidelines.com/if-the-game-uses-field-of-view-3d-engine-only-allow-a-means-for-it-to-be-adjusted/)
- Note on terms: GAG's 60°/90° figures are not labelled horizontal or vertical. In context they are the horizontal FOV typical of console and PC shooters. Unity's camera FOV is *vertical* by default, and 90° horizontal at 16:9 is about 59° vertical. This is my conversion, not from the source.
- World of Warcraft added an FOV slider as a motion-sickness measure, then capped it at 90° in a June 2022 hotfix (secondary news source, not verified against Blizzard's notes). — [Warcraft Tavern](https://warcrafttavern.com/wow/news/wow-hotfixes-june-22nd-fov-adjustment)

**Auto-centre / automatic camera movement optional**
- XAG 117: "Provide camera movement settings like the ability to adjust horizontal and vertical camera movement sensitivity and the ability to **disable automatic camera movement**." Its scoping questions list "camera auto-centering" explicitly. [Primary guideline] — [XAG 117](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/117)
- XAG's example of best practice is Sea of Thieves, which offers Auto Centre Camera on/off, **Auto Centre Delay** (shown at 2.0) and **Auto Centre Speed** (shown at 180). [Primary guideline / Developer data] — [XAG 117](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/117)

**Camera speed sliders**
- XAG 117 asks for separate horizontal and vertical camera sensitivity (example: Halo Infinite look sensitivity, horizontal and vertical, both default 5.0). [Primary guideline] — [XAG 117](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/117)
- XAG 107: "An option to adjust sensitivity of analog controls individually should be provided at the game level… thumb sticks, joysticks, triggers, racing wheels, and **mouse movement**… Players should be able to increase or decrease sensitivity by **at least 50% of the default**." [Primary guideline] — [XAG 107](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/107)
- GAG (Motor, Basic): "Allow players to set it at game level, and offer a wide range of sensitivity… There is no one-size-fits-all sensitivity level"; some players with restricted movement need very high sensitivity, others with poor precision very low. "The option to reduce sensitivity can also reduce the impact of simulation sickness." Examples cited: CS:GO, Destiny 2, Elder Scrolls Online camera settings, Splatoon 2. [Primary guideline] — [GAG: adjust sensitivity](https://gameaccessibilityguidelines.com/include-an-option-to-adjust-the-sensitivity-of-controls/)

**Reduced camera shake, bob and blur**
- XAG 117: "Avoid the use of camera shake, camera bobbing effects, motion blur, mouse blur, and more **or provide an option to turn off** these behaviors." Examples: Halo Infinite (radial blur, screen shake, full-screen effects and speed lines each 0–100%), Cyberpunk 2077 ("additive camera motions" setting), Elder Scrolls Online (head-bob intensity slider). [Primary guideline] — [XAG 117](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/117)
- The Last of Us Part II "Motion Sickness" group: **Camera Shake 1–10**, **Motion Blur 1–10**, **Dolly Zoom Effect on/off**, **Field of View −5 to +5**, **Camera Distance −5 to +5** (third person), **Full Screen Effects on/off**, **Persistent Center Dot on/off** (a steady reticle as a fixation point). There is also **Weapon Sway on/off**. [Developer data, official] — [PlayStation: TLOU Part II accessibility](https://www.playstation.com/en-us/games/the-last-of-us-part-ii/accessibility/)
- A fixation point is supported by Nooij et al.: a fixation target reduced VIMS, mainly by reducing vection. [Peer-reviewed] — [Nooij et al. 2017](https://journals.plos.org/plosone/article?id=10.1371/journal.pone.0175305)
- God of War Ragnarök has camera rotation speed and a "Motion Reduction" section for ambient camera sway and shake; one MMO offers "Keep Character Centered / Reduce Camera Motion / Allow Dynamic Camera Movement" plus Camera Shake Full/Reduced/Off. Both come from secondary guides found in search and were not verified against the games. — [SeekingTech GoW Ragnarök guide](https://seekingtech.com/how-to-prevent-motion-sickness-when-playing-god-of-war-ragnarok/)

**Toggle versus hold**
- XAG 107: "Avoid introducing mechanics where a key or button should be held down for an extended period before the input is registered." For actions that are normally held (sprinting, firing, accelerating), "consider allowing players to toggle this action on permanently". Examples: The Long Dark's "accessible interactions" converts all press-and-hold into press, and Gears 5 lets players choose "press & hold" or "quick taps". Its scoping question treats holds of "2–3 seconds or more" as a barrier, and its gesture guidance treats a "long press (less than 3 seconds)" as acceptable. [Primary guideline] — [XAG 107](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/107)
- TLOU Part II has Hold/Toggle for aiming, listen mode, sprint, crafting and backpack swap; Tap/Hold for repeated presses and melee combos; Hold/Taps for bow. [Developer data] — [PlayStation: TLOU Part II accessibility](https://www.playstation.com/en-us/games/the-last-of-us-part-ii/accessibility/)

**Remappable keys**
- XAG 107: "Players should be given the option to remap all of the controls within the game itself, regardless of platform-level remapping… including remapping analog and digital controls, **inverting both the x-axis and y-axis individually** for each individual control stick, and the **Esc key on PC games**." It prefers remapping *actions* rather than swapping buttons, and remapped labels must update in every hint and tutorial. [Primary guideline] — [XAG 107](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/107)
- XAG 107: on PC, everything must be possible with keyboard alone. Camera look should be available on digital keys (Grounded maps look to the arrow keys) so a mouse is not required. [Primary guideline] — [XAG 107](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/107)
- AbleGamers' *Includification* (2012) lists "Camera Controls", "Remappable Keys" and "Sensitivity Sliders" in its Mobility section, according to the W3C list reproduction of its topic list. The PDF itself (over 10 MB) could not be read. [Primary guideline, secondary access] — [W3C RQTF list post](https://lists.w3.org/Archives/Public/public-rqtf/2017Sep/0004.html); [AbleGamers Includification PDF](https://accessible.games/wp-content/uploads/2018/11/AbleGamers_Includification.pdf)

**Mouse sensitivity and smoothing**
- GAG: mouse smoothing is a named culprit in input-to-camera mismatch and should be avoided or made optional. [Primary guideline] — [GAG: difference between controller and camera movement](https://gameaccessibilityguidelines.com/avoid-or-provide-option-to-disable-any-difference-between-controller-movement-and-camera-movement/)
- Smoothing and bob are separate settings. A flight-sim forum thread found that a "camera smoothing" variable only smooths input and does not remove bob, so both need their own controls. [Anecdotal] — [IL-2 Sturmovik forum](https://forum.il2sturmovik.com/topic/92743-head-bobbing-controls-2d-without-head-tracking)

### Inferences
- A defensible minimum for a PC third-person camera that passes XAG 107/117 and GAG would be:
  - FOV slider
  - camera distance
  - separate X and Y sensitivity with at least ±50% range
  - per-axis invert
  - auto-centre on/off, ideally with delay and speed
  - camera shake slider down to 0
  - head bob or sway off
  - motion blur off
  - mouse smoothing off (default off)
  - full action remapping including Esc
  - keyboard-only camera control (rotate keys)
  - toggle alternatives for every hold.
- Q/E quarter-step rotation is already a digital (keyboard) camera control, which XAG 107 favours. It is also discrete and predictable, so it causes far less vection than continuous orbit.

### Gaps
- No guideline gives a numeric default or limit for third-person FOV specifically. GAG's 60/90 figures are general guidance for 3D games.
- No guideline gives a numeric limit on camera shake amplitude or rotation speed.

---

## 3. Data on how fast automatic camera rotation can be before it is uncomfortable (°/s, easing, recentre delay)

### Takeaway
No guideline or game-specific study gives a comfort threshold for third-person auto-rotation. The closest data are: lab drum studies (sickness rises with yaw speed up to about 60°/s, and velocity *changes* are worse than constant speed); Sea of Thieves' shipped auto-centre settings (delay 2.0, speed 180, cited by XAG); and Unity Cinemachine's model (wait time after the last input, then a recentre duration). Easing and delay values in practice are tuning choices, not research findings.

### Cited Findings
- Optokinetic drum (VR) study: symptoms increased as drum speed rose **up to 60°/s and decreased at 90°/s**. The relationship is non-monotonic; this is one study, from a search-result summary. [Peer-reviewed, secondary] — [ResearchGate: Motion Sickness Induced by Optokinetic Drums](https://www.researchgate.net/publication/8695039_Motion_Sickness_Induced_by_Optokinetic_Drums)
- Changing yaw velocity (30↔60°/s) caused more nausea than constant velocity. [Peer-reviewed] — [PubMed 16909874](https://pubmed.ncbi.nlm.nih.gov/16909874/)
- Standard VIMS lab stimulus: **60°/s** constant yaw is used because it reliably induces sickness (12/15 participants sick in at least one session). [Peer-reviewed] — [Nooij et al. 2017](https://journals.plos.org/plosone/article?id=10.1371/journal.pone.0175305)
- Sea of Thieves' shipped settings, as shown in XAG 117: Auto Centre Camera (on/off), Auto Centre Delay **2.0** (presumably seconds), Auto Centre Speed **180** (unit not stated; possibly °/s). [Developer data] — [XAG 117](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/117)
- Unity Cinemachine 3.x Orbital Follow recentring has two settings: a **Wait** (seconds after the last user input before recentring begins) and a **Time** (how long recentring takes once started). In the older 2.x FreeLook, "Recentering Time" meant something different. [Developer data] — [Cinemachine 3.0 Orbital Follow manual](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.0/manual/CinemachineOrbitalFollow.html); [Cinemachine 2.4 FreeLook manual](https://docs.unity3d.com/Packages/com.unity.cinemachine@2.4/manual/CinemachineFreeLook.html)
- Implementation pitfall: the Cinemachine recentre wait starts when the axis value stops changing, so input acceleration and deceleration delayed recentring by about 4 s for one user. [Anecdotal developer report] — [Unity Discussions: Recentering delay with joystick](https://discussions.unity.com/t/recentering-delay-with-joystick-cinemachine-3-0-1/940478)
- Common practitioner patterns, all unquantified:
  - auto-centre after the player has been still "for a few seconds";
  - tie camera turn rate to a fraction (0–1) of the character's turn rate;
  - make velocity proportional to the angle error, which gives an elastic ease. [Practitioner / forum] — [GameDev.net: lag for a 3rd-person camera](https://gamedev.net/forums/topic/500641-implementing-lag-for-a-3rd-person-camera/); [TV Tropes: Camera Centering](https://tvtropes.org/pmwiki/pmwiki.php/Main/CameraCentering)
- "A camera movement that is too fast or too slow can also lead to nausea"; finding the right speed takes "frequent quality testing and constant trial and error" (no values). [Practitioner] — [Game Developer: Third Person Camera View in Games](https://www.gamedeveloper.com/design/third-person-camera-view-in-games-a-record-of-the-most-common-problems-in-modern-games-solutions-taken-from-new-and-retro-games)

### Inferences
- Lab drums fill the whole visual field with stripes, which is a stronger vection stimulus than a game camera on a monitor. So 60°/s is an *upper-bound worry*, not a safe line. A recentre that sweeps 90° in about 0.5 s averages 180°/s with a higher peak, which is well above the speeds that sicken people in drums, though it is brief. The 180 in Sea of Thieves (if °/s) sits in that range and is player-adjustable.
- Because acceleration matters more than steady speed, the safest automatic camera motion is: rare, started only after a clear delay (Sea of Thieves uses about 2 s; Cinemachine exposes Wait), eased in and out (critically damped or ease-in-out, with no snap), and cancelled immediately by any player input.
- For a quarter-step yaw (Q/E, 90°), the same logic suggests a short eased turn or a cut. A cut produces no vection at all, at some cost to orientation. Note this is reasoning, not evidence.

### Gaps
- No peer-reviewed comfort threshold (°/s or °/s²) was found for flat-screen third-person camera auto-rotation.
- No published default recentre delays from AAA studios beyond Sea of Thieves (via XAG). The units of SoT's "180" are not stated.
- No study comparing an eased turn with an instant cut for discrete camera rotation in games was found.

---

## 4. Which camera options do PC players expect in third-person games?

### Takeaway
Based on guideline examples and shipped AAA menus (TLOU Part II, Sea of Thieves, Halo Infinite, AC Valhalla, ESO, Cyberpunk 2077), the expected set is:
- FOV
- camera distance
- separate X and Y sensitivity
- per-axis invert
- auto-centre on/off (plus delay and speed)
- shake, blur and bob intensity
- a camera-assist mode
- stick or axis swap
- shoulder swap
- full remapping

There is no survey that ranks these by player expectation.

### Cited Findings
- **Auto-centre on/off + delay + speed**: Sea of Thieves. [Developer data] — [XAG 117](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/117)
- **Camera distance / close camera**: AC Valhalla (close-camera toggle); TLOU Part II (Camera Distance −5..+5). [Developer data] — [XAG 117](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/117); [PlayStation TLOU II](https://www.playstation.com/en-us/games/the-last-of-us-part-ii/accessibility/)
- **FOV slider**: Halo Infinite, Minecraft (to 110°), TLOU Part II (−5..+5, third person). ESO has *separate first- and third-person FOV*. [Developer data] — [XAG 117](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/117); [PlayStation TLOU II](https://www.playstation.com/en-us/games/the-last-of-us-part-ii/accessibility/); ESO detail from search summary of [GAG mouse-smoothing page](https://gameaccessibilityguidelines.com/avoid-or-provide-option-to-disable-any-difference-between-controller-movement-and-camera-movement/)
- **Horizontal and vertical sensitivity**: Halo Infinite. [Developer data] — [XAG 117](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/117)
- **Invert X and Y individually**: Gears 5 (XAG requires both axes). [Primary guideline] — [XAG 107](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/107)
- **Camera assist (camera follows movement direction)**: TLOU Part II "Camera Assist" (On / Off / Horizontal / Vertical) "Automatically reorients the camera in the direction of your movement", for newcomers or players who struggle to use both sticks. It is switched on by the Motor accessibility preset, which implies it is off by default, though the page does not say so explicitly. Lock-on aim is separate. [Developer data] — [PlayStation TLOU II](https://www.playstation.com/en-us/games/the-last-of-us-part-ii/accessibility/); [Can I Play That? TLOU2 features](https://caniplaythat.com/2020/06/09/the-last-of-us-2-accessibility-features-revealed-naughty-dog/); [Destructoid Remastered list](https://www.destructoid.com/all-accessibility-options-in-the-last-of-us-part-2-remastered/)
- **Input relative to character or camera**: TLOU Part II "Rope/ladder movement: Character or Camera". [Developer data] — [PlayStation TLOU II](https://www.playstation.com/en-us/games/the-last-of-us-part-ii/accessibility/)
- **Shoulder swap**: present in TLOU Part II (referenced by the "Melee while aiming" option, which "disables Shoulder Swap on that button"). The Buehler article notes cameras are usually "slightly offset to the left or right" but does not discuss a swap option. [Developer data] — [PlayStation TLOU II](https://www.playstation.com/en-us/games/the-last-of-us-part-ii/accessibility/); [Game Developer: Buehler](https://www.gamedeveloper.com/design/third-person-camera-view-in-games-a-record-of-the-most-common-problems-in-modern-games-solutions-taken-from-new-and-retro-games)
- **Additive or secondary camera motion intensity**: Cyberpunk 2077; Halo Infinite shake/blur 0–100%. [Developer data] — [XAG 117](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/117)
- **First/third-person choice**: recommended by XAG 117. [Primary guideline] — [XAG 117](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/117)
- **Vehicle / mount camera control**: forum posts ask for a fixed camera during mount transitions, wider zoom and a toggle for camera control in vehicles. [Anecdotal] — [Guild Wars 2 forum: mounts and motion sickness](https://en-forum.guildwars2.com/discussion/7195/mounts-and-motion-sickness); [Blizzard forum: Dragonriding motion-sickness settings](https://us.forums.blizzard.com/en/wow/t/dragonriding-tips-and-settings-for-reducing-motion-sickness/1427636)
- **Vignette when turning, fixed crosshair as a focal point**: offered in at least one shipped game's accessibility patch. [Developer data, single game] — [SteamDB patch notes](https://steamdb.info/patchnotes/8125621)

### Inferences
- The "camera follows vehicle or character direction" option exists in two forms: TLOU Part II's Camera Assist (camera reorients to movement direction, apparently off by default) and the generic auto-centre with delay (Sea of Thieves). Both are exposed as player choices, never forced.
- For a commander camera (fixed pitch, quarter-step yaw), the relevant subset is: rotation keys remappable; rotation speed or "instant vs eased turn"; follow on/off ("camera stays put" vs "camera follows selected unit"); follow smoothing; zoom/distance range; shake off; and any third-person try-out carrying FOV, distance, sensitivity, invert, auto-centre and shoulder swap.

### Gaps
- No PC-player survey quantifying which camera options players *expect* was found. PCGamingWiki's FOV and camera pages returned 403 and could not be read.
- AbleGamers' Includification camera section could not be read (the PDF is too large to fetch).

---

## 5. Camera stays where the player left it vs auto-recentre: evidence and for whom

### Takeaway
No direct comparison study was found for games. The evidence is indirect but consistent:
- Accessibility guidance requires auto-centre to be *optional*.
- Motion-sensitive players and accessibility reviewers report uncommanded reorientation as a trigger.
- Lab evidence says self-controlled motion causes less sickness than passive motion.
- Players who struggle to manage a second camera input (newcomers, some motor disabilities) benefit from assistive recentre. TLOU Part II puts it in its Motor preset, apparently off by default.
- Academic camera work suggests players split into "likes control" and "doesn't" groups, and adaptive or hybrid cameras do best for non-experts.

### Cited Findings
- XAG 117 requires "the ability to disable automatic camera movement", and gives Sea of Thieves (auto-centre toggle, delay, speed) as the example. [Primary guideline] — [XAG 117](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/117)
- TLOU Part II: Camera Assist is an *opt-in* assistance setting aimed at "newcomers to action games or players who find it hard to use both sticks at once". It is enabled by the Motor accessibility preset (with lock-on aim, auto weapon swap and others). This implies the default for most players is a camera that stays where it is left, while recentre is an assist for motor or novice needs. [Developer data] — [PlayStation TLOU II](https://www.playstation.com/en-us/games/the-last-of-us-part-ii/accessibility/); [Can I Play That? TLOU2](https://caniplaythat.com/2020/06/09/the-last-of-us-2-accessibility-features-revealed-naughty-dog/)
- Motion-sensitive players: FF7R's camera "turns whether you touched it or not", making a reviewer "physically ill". The CIPT editorial argues for giving camera control back to players. [Anecdotal / practitioner] — [CIPT FF7R](https://caniplaythat.com/2020/05/05/final-fantasy-vii-remake-broke-my-heart/); [CIPT motion sickness](https://caniplaythat.com/2020/12/08/we-need-to-talk-about-motion-sickness/)
- Active control reduces sickness: drivers had less sickness than passengers (Stoffregen group), and active play caused less VIMS than passive viewing (Keshavarz & Hecht 2012, cited second-hand). [Peer-reviewed, secondary] — [Frontiers VR 2020](https://www.frontiersin.org/journals/virtual-reality/articles/10.3389/frvir.2020.581132/text); [CIPT citing Keshavarz & Hecht](https://caniplaythat.com/2020/12/08/we-need-to-talk-about-motion-sickness/)
- Burelli & Yannakakis (player-modelling camera; IEEE Transactions on Computational Intelligence and AI in Games line of work): instead of a fully automatic or fully manual camera, a camera *adapted to the individual player* improved performance and "was perceived extremely positively by non-expert participants". This is from a search summary; the full paper could not be extracted, so sample size and design are unknown. [Peer-reviewed, secondary] — [Burelli & Yannakakis, "Adapting Virtual Camera Behaviour through Player Modelling"](https://yannakakis.net/wp-content/uploads/2015/02/J30.pdf)
- In a non-game user study (automated meeting-camera system, from a patent), viewers fell into two groups: those who like to control the camera and those who do not. The authors concluded systems should offer both automatic and manual control. [Non-game, patent] — [USPTO patent 6937266](https://image-ppubs.uspto.gov/dirsearch-public/print/downloadPdf/6937266)
- Practitioner view: hybrid cameras (automation does most work, player can override "to a limited extent") are the common third-person pattern. Temporary auto-centre is reasonable at doors or fight start, after which the camera "returns to its previous position". Automated cameras risk making the player feel impaired. [Practitioner] — [Game Developer: Buehler 2019](https://www.gamedeveloper.com/design/third-person-camera-view-in-games-a-record-of-the-most-common-problems-in-modern-games-solutions-taken-from-new-and-retro-games)
- Forum split: "automatic follow very annoying, because it does not do what I want most of the time", versus requests to switch between automatic and player-controlled cameras on the fly. [Anecdotal] — [GameDev.net: Third person cameras](https://gamedev.net/forums/topic/521182-third-person-cameras/)

### Inferences
- Default "camera stays where the player put it", with auto-centre or follow as an option, matches XAG, the TLOU Part II default and the motion-sickness testimony. It also fits a tactics or commander game where the player sets up a view on purpose (e.g. the Records Office front at yaw 210).
- Who benefits from recentre: novices and players with motor impairments who cannot easily operate a second axis. Who is harmed by it: motion-sensitive players and expert players who place the camera deliberately. Making it optional, with a delay and speed setting and immediate cancel on input, serves both groups.

### Gaps
- No controlled study was found comparing auto-recentre with a persistent camera in games for comfort, performance or preference.
- No data was found on what share of players turn auto-centre off when given the option (e.g. Sea of Thieves or TLOU II telemetry).
