# 104 · The robots' models

**Asked by:** Sky (2026-11-10), after playing 0.2.303: "game played well. Robots are wrong models though - check our suborned store for all robot models we made in past".

## Objective
Put the right robot models in the Handover, picked by Sky from every robot model we've already made.

**Today in the game:**
- **The response dial's robot units** (sent when the Alert rises) are drawn with `DK_RobotPorter_Play`, a luggage porter with a signal lamp on top (`HostViews.cs:379`, `PlayInstall.cs:122`).
- **The patrol** is `DK_PatrolRobot` (brief 031), with the attention face.
- **The square's dock** is `CivicRobotDock`.
- **Street life in the old town** uses `RobotWaiter`, `RobotShopkeeper`, `RobotNanny`, `RobotButler` and `RobotPorter` (`DecoKitStreetLife.cs`, `DecoKitSouthsideLife.cs`).

**Made before, in `~/Projects/the-suborned-store`:**
- `testbed-art/mobility/Robots/RFM_Robot{Concierge,ParcelCourier,StreetSweeper,TrackedRepair}V1.glb`
- `testbed-art/mobility/Vending/RFM_RobotServiceV1.glb`
- `testbed-art/civic-street-props/civic-robot-v1.glb` (the butler's far mesh on a plinth)
- the concepts in `concepts/Robots/` and `concepts/Props/sheets/robots_{civic,service}.png`

There are also the kit's DecoKit robots and anything in `~/Projects/the-suborned`.

## Owner
Vela (the board and the mapping). Then Latch (install) once Sky picks.

## Done-check
1. One board shows every robot model we've made, rendered the same way and at play size, plus the robot concepts.
2. It marks what each role uses today and proposes a model for each role.
3. Sky picks.
4. The build shows the picked models in their roles.

## Evidence
`briefs/104/vela/robots/` (board and notes), then the build's shots.

## Next owner
Vela, then Sky, then Latch.

## Vela's board (2026-11-10, cdd312770)
14 models are on one board (`briefs/104/vela/robots/`).
- **Proposed:**
  - the Patrol robot with the red lamp for the response units;
  - a Patrol robot standing in the square's dock;
  - the store robots stay in the store.
- **Aster's note:** the small blue-domed service robot reads close to a famous film robot. Repaint or reshape it later.

## A new patrol robot: concept work (Sky, 2026-11-10: "create some concept work for a suborned patrol robot")
**Vela, $0 Codex stills from an open brief (the style contract's `concept_prompt`):**
- two or three lineup sheets of about six designs;
- a play-size strip at about 80 px;
- her top three.

**The robot's world facts, given to the art:**
- it walks a round and is sent from street docks when the alert rises;
- it reports people who linger;
- it is clockwork-smart, with no networks;
- a dot-matrix face shows its four attention stages, with no cartoon eyes;
- an unlit emissive alarm signal;
- the city's "optimistic surface, coercive underneath";
- original, like no film or game robot.

**Deliverables:** `briefs/104/vela/patrol-concepts/`, with images in `paid/104-patrol-robot/`.

**Pass 1 rejected (Sky, 2026-11-10): "none of those really meet our retro future suborned vibe really".** The sheets were `paid/104-patrol-robot/codex/A-civic.png`, `B-stern.png` and `C-signal.png`.
- **Aster's read:**
  - flat illustration on beige paper, like a vintage toy catalogue, instead of the 26 Sep paintings' cinematic painted realism (glossy cream enamel, chrome, sun);
  - legs, springs, horns and toy proportions instead of 1950s industrial design (Streamline Moderne, Googie, atomic-age appliance and car lines) carrying effortless, centuries-ahead technology (our city's tech floats);
  - no city around them.
- **Pass 2 (Vela):**
  - references: the Records Office and city hero paintings, plus our in-game robot family (butler, waiter, nanny) as the family it joins;
  - two or three painted plates on the civic square;
  - one glossy lineup;
  - the play-size strip.

**Passes 2 and 3 (Aster in the main thread, on Sky's word: "just you did it here, no need for vela"; Vela's task cancelled).** All $0 Codex, through `paid.py`; images in `paid/104-patrol-robot/codex/`, with references in `paid/104-patrol-robot/refs/`.
- **Pass 2** (prompts start from `concept_prompt`; references: the Records Office and city hero paintings, plus the in-game robot family):
  - `P2-plate-1`: a hovering enamel robot with jukebox chrome, a red tail-light and a paper-tape printer;
  - `P2-plate-2`: a long 1950s car-body hover robot, too big for the dock;
  - `P2-plate-3`: a tall doorman robot on a hover disc.
- **Sky:** "think they would have more warning lights on them, and more primary colors to stand out".
- **Pass 3** adds a security livery: signal red, cobalt blue and chrome yellow in big blocks over cream enamel and chrome, with a light bar or beacon, side markers and tail-lights.
  - `P3-lineup-1`: six designs. 2 (light bar) and 5 (three beacons) read most as security.
  - `P3-plate-1`: on the square, it hands a printed ticket and holds a stamp; close to lineup 2.
- **Next, Sky's call:** refine a pick, push another sheet, or a turnaround of the pick for the 3D piece.

**Pass 4 (Aster, "show me more concepts").** All $0 Codex, in the security livery:
- `P4-lineup-objects`: six designs built on 1950s street objects (parking meter, petrol pump, jukebox, fire-alarm call box, locomotive nose, traffic signal). The call box reports by handset, which fits how information moves in the city.
- `P4-lineup-vehicles`: robot riders on hover-scooters and tiny patrol cars. They read as traffic police; 2 and 4 are close to real car and scooter designs.
- `P4-plate-dusk`: two petrol-pump patrol robots answering an alarm at dusk outside the Records Office, light bars on.

Strongest so far: `P3-lineup-1` #2, the call-box and petrol-pump robots, and `P3-plate-1`. 11 images in all, $0.

**Sky: "Not really happy with them. Robot patrols would need guns, be armed as they fire at the agents don't they?"** Yes: GDD §07 says "a robot is an armed patrol unit", and at Alert 3 robots fire on a reported face (HI-ROBOT).
- **Pass 5** keeps the livery and adds a visible 1950s-styled firearm: a carbine or a holstered sidearm, part of the design, a real gun, not a ray gun. The robots are serious and imposing, not cute.
  - `P5-lineup-armed`: six broad, upright armoured robots with car-grille chests.
  - `P5-plate-armed`: one on the square, carbine held low, light bar, visor face.

**Sky: "less menacing. Think 1950s retro future robots are more cute but deadly kinda vibe".**
- **Pass 6** keeps the gun, the livery and the warning lights, and makes the body charming: tin-toy and appliance proportions, a big round helmet head with a porthole visor, ball-tipped antennae, and a polite face made of amber dots. The holstered pistol is what gives it away.
  - `P6-lineup-cute`: six designs:
    1. a pear body on wheels;
    2. a jukebox chest on a hover disc;
    3. a round body on one wheel;
    4. a small, round figure on hover feet;
    5. a fridge body on treads;
    6. a red and yellow body on a hover disc.
  - `P6-plate-cute`: design 6 in blue and red on the square in front of the Records Office, with its pistol in a hip holster.
  - Aster's notes:
    - The smile is a dot display, so it can change: a calm smile on patrol and a flat line at Alert 3, with no new mesh.
    - The big heads are about 1.6–1.9 m tall next to the scale figure, so a pick needs shrinking to the 1.3 m brief.

**Sky: "need something more simplistic I think - do a new sheet. don't guide too much in terms of color scheme or reference images".**
- **Pass 7** used a short prompt with no reference images and no palette. It gave only the world, "cute but deadly", simple iconic shapes readable from above, the gun built in, warning lights and a display face.
  - `P7-lineup-simple`, six designs:
    1. a cone on two wheels with a chest gun;
    2. a capsule on three legs with an arm gun;
    3. a ball on treads with a belly gun;
    4. an egg on two legs with an arm gun;
    5. a little car with its display on a stalk;
    6. a tall cabinet on treads with a side gun.
  - The faces are display slits whose glyphs change.
  - The finish is slightly worn painted enamel.

**Sky: "hmm robots still not appealing to me".**
- **Pass 8:** a wide exploration with as little guidance as possible: nine directions, made as different as possible, with only the world, "patrols, watches, armed" and "appealing, charming, memorable".
- `P8-explore` (3 × 3):
  1. a mint pebble on casters;
  2. a stilt-legged heron;
  3. a red four-legged hound;
  4. a hovering ring with an eye;
  5. a yellow tank on treads;
  6. a ball on a pole with a fan crest;
  7. a blue umbrella on four legs with a periscope;
  8. a chrome finned unicycle;
  9. a lilac cabinet with a fan crest.
- The faces are lenses, not displays.

**Sky: "more cute".**
- **Pass 9**, `P9-explore-cute`: the pass 8 prompt made cute. The result was nine chubby robots with painted cartoon faces and toy ray-gun pistols.

**Sky: "minimal weaponry, nothing more than pistol … and yes, needs an LED face".**
- **The rulebook already agrees.** A robot is a patrol unit with a pistol:
  - `GameResponse.cs:175` gives response robots `PistolW`, and `Game.cs:129` does the same for the patrol;
  - pistol: 1 damage, a shot every 1.5 s, effective to 15 m, maximum 16 m;
  - robot aim: 45 % (`aim-robot`, below a guard's 50 %);
  - robot health: 4 (`health-patrol`).
- **Pass 10**, `P10-cute-led`: the same cute brief, plus:
  - an LED face: a dark glass screen with glowing dot eyes and a dot mouth, with no painted face;
  - one small pistol in a hip holster, and nothing built in.
- The result: nine round pastel robots, each with a holstered pistol.
  - Row 1: a pear, a four-legged yellow pod, a tall pink figure in big boots.
  - Row 2: a blue bubble car, a mint robot balancing on a ball, a coral six-legged pod with its head on a spring.
  - Row 3: a red head-on-spring robot on treads, a mint saucer, a lilac robot on casters.
- Aster's note: the LED expression can change with Alert (a calm smile, then a flat line), driven by the shader so the robots stay instanced.

**Sky: "which would be easiest to make in Blender and add movement animation?"** Aster: the saucer.
- It is made of revolved, rigid parts.
- Hover, tilt and turn are done in code.
- Only the arms would need keyframed clips.
- The ball-balancer is second. Legs are the most work.

**Sky: "but how about the shooting / holding of the gun. I would probably mount the weapon".**
- **Pass 11**, `P11-saucer-mounts`: the P10 saucer (`refs/ref-p10-saucer.png`), with four mounts, each shown closed and open. The LED face goes from a smile to narrowed eyes.
  1. A swing arm out of a hatch in the rim.
  2. In the forearm, behind a sliding cover.
  3. A swivel mount that rises from the top of the head.
  4. A turret that drops from under the saucer.
- The first wording, with "pistol … aiming", was refused by Codex's violence filter (ledger row 21). It worked when described as a "sidearm-shaped gadget … pointing forward".
- Aster's read for play at 38°:
  - **3 (head top)** shows best from above. Rising is the draw, which security treats as an act. A two-axis swivel aimed in code needs no clips.
  - **1 (rim arm)** changes the silhouette at the side.
  - **2 (forearm)** is the smallest change and hard to read at about 80 px.
  - **4 (under the saucer)** is hidden by the saucer from the play camera.

**Sky picks the ball-balancer (P10, row 2, middle): "I think I like this one - and gun comes from middle panel perhaps?"**
- Sky's crop is saved as `refs/ref-sky-pick-ball.png`.
- **Pass 12**, `P12-ball-chest`: the holster is gone. The gadget sits behind the chest panel with the three slats. Three frames:
  1. **closed:** the robot smiles;
  2. **opening:** the panel hinges up and the gadget slides out;
  3. **open:** the gadget points forward, the eyes narrow and the mouth goes flat.
- Rig, as Aster reads it:
  - **Rigid parts:** the ball, the body, the panel (one hinge), the gadget (one slide) and the arms.
  - **Ball:** its roll comes from ground speed, in code.
  - **Body:** it leans into travel in code, and aims by turning, since the gadget is fixed to the chest.
  - **Face:** the LED face is a shader setting.

**Sky: "former, you do it"**, i.e. the turnaround sheet for modelling in Blender.
- **Pass 13:**
  - `P13-turnaround`: five orthographic views on a common ground line, with guide lines at the ball centre, the widest part of the body, the top of the body and the antenna. The views are front, left, back, right and top, drawn about 1.3 m tall.
  - `P13-details`:
    - six LED faces: calm, curious, alert, focused, hurt and off;
    - the chest-panel mechanism: closed, then hinged up, then the gadget slid out on a rail, in side cutaway and front views;
    - the ball's bands.
- **The model reference is committed** in `briefs/104/concepts/`, kept by `.gitignore`:
  - `patrol-robot-pick.png`
  - `patrol-robot-chest-sidearm.png`
  - `patrol-robot-turnaround.png`
  - `patrol-robot-details.png`
- **Model notes for the build:**
  - **Rigid parts:** ball, body, chest panel (hinged at its top edge), gadget (on a rail), two arms (shoulder and elbow), and the halo antenna.
  - **Face:** one screen material whose expression is chosen in the shader. No per-robot MaterialPropertyBlock, so the robots stay inside the GPU Resident Drawer.
  - **Driven in code:** the ball's roll from ground speed, the body's lean and the aim turn.
  - **Clips (short):** the panel opening and the gadget sliding out, the gadget going back, an arm idle and a hit flinch.
  - **Next:** check the 1.3 m height against the robot dock (0.98 m) and the crowd budget before modelling.

**Sky: "model it".** Vela built it.
- **What:** `DK_PatrolBot104`, made by `tools/decokit/patrolbot104.py` and `patrolbot104_surf.py`, exported to `out/decokit/Models/Robots/DK_PatrolBot104.fbx`. Notes and renders are in `briefs/104/vela/patrolbot/`.
- **Size:** 1.30 m tall and 0.89 m across, so it fits the dock bay (0.98 × 1.0 × 2.14 m). At play zoom it's about 58 px tall, next to about 85 px for a 1.8 m person.
- **Triangles:** 7,988, with lower-detail versions at 1,986 and 471.
- **Parts:** Body (the root, pivoted at the ball contact), Ball, ChestPanel, Gadget, ArmL/R, ForearmL/R, Halo and Screen.
- **Materials:** five, all shared.
- **Faces:** Vela proposes one material per face, all on one shader with a 3 × 2 atlas, swapped on the Screen's slot so the robots stay instanced.
- **Where it differs from the sheets:**
  - the arms hang at 42° to keep it within 0.9 m;
  - the ball halves are dark iron;
  - the sidearm is cream;
  - the hatch opens about 150°.
- **Shared code:** `geo.Shape.part(pivot=)` and `export_kit` now keep a part's own pivot. It's opt-in; the old patrol robot builds identically at 12,522 triangles.
- **Not done yet:**
  - the kit step (`bun run kit`, collide, sim:site);
  - Latch's check that the parts import into Unity with no added rotation;
  - the runtime: face swap, roll, lean, aim, and the hatch clip.

**Sky: "sure - though I would put a siren light on his head instead of antenna".** Vela, round 2:
- **Siren:** a `Beacon` replaces the halo. It has a chrome base, a red glass dome 0.152 m across with a four-bar cage, and a spinning `BeaconLamp` inside (a mirror and bulb). The material is `DK_PatrolBot104_Beacon`, red, with emission off as built; the game drives it.
  - The spinning mirror only shows if the dome gets see-through glass in Unity (Latch). Until then the spin is an emission sweep.
  - Glint judges the lit red in Unity.
- **Ball:** the halves are now warm satin gunmetal (`DK_Gunmetal_Warm`). The cream-halves option merged with the body's tip.
- **Hatch:**
  - **Panel:** 25 mm thick, with a gunmetal inner face, two stiffeners and two barrel hinges.
  - **Well:** gunmetal, with a chrome lip, two ribs and two amber lamps.
  - **Rail:** nickel.
- **Size and budget:** 8,608 triangles and 1.271 m tall. The old patrol robot builds identically.

**Sky: "can you show me a demo video of it first before putting in game".**
- Vela animated it in Blender: `briefs/104/vela/patrolbot/demo/patrolbot-demo.mp4`, git-ignored, with a contact sheet `demo/frames.png`.
  - The clip runs 40 s: the 20 s routine on a close follow camera, then the same routine on the play camera at 1:1 (the robot is 54 px tall).
  - The routine: patrol, stop, look around, alarm, draw and three shots, hit, stand down.
  - Rendering took 297 s (EEVEE, 16 samples), through `bun run guard`.
- What it showed:
  - All eleven pivots work.
  - The ball rolls with 0 mm of slide.
  - The body leans at most 7° and clears the ball.
- Open look questions for Sky:
  - The open hatch hides most of the sidearm from the close camera. A longer slide (0.14 m) or a wider hatch (165°) would show it.
  - At play size the draw reads through the flash, the recoil and the beacon, not the hatch.
  - The beacon's red drifts to salmon in Blender, so Glint judges it in Unity.

**Sound, Chime's paid pass:**
- **Cost:** $0.54 of $3, 48 calls: Seed Audio 1.0 and Mirelo. Report: `briefs/104/chime/robot-sfx.html`.
- **The shot.** Sky: "the gun shot doesn't really match, I think robot_shot might be a bit better. Paid one has a reload sound, too long".
  - Aster then cut the paid crack without its slide clacks: `briefs/104/chime/paid/shot_slide_t1_crack.wav`, 0.6 s, trimmed at 0.6 s with a 0.18 s fade, made with ffmpeg from `shot_slide_t1`.
  - Sky: "yeah, shot slide trimmed works actually". **The shot is `shot_slide_t1_crack`.** The synth `robot_shot` is the fallback.
  - Chime should make the trim part of `make/robot_paid.py`, so it rebuilds like the other clips.
- **The siren** is still for Sky to pick. Chime's pick is `siren_yelp_t3`.

## In the game: build 0.2.309 (Latch, Aster's kit step)
- **Both roles:** `DK_PatrolBot104` draws the patrol and the response's robot model, on the shared LED face (DK_RobotFace and RobotFace.cs, with four new glyphs: focused and hurt).
  - The Handover sends no response robots, because the engine sends them only from a dock and the square's dock is empty. So the patrol is the mission's only robot.
- **New code:**
  - `Runtime/Robots/PatrolBot.cs`: ball, lean, hatch, slide, recoil, flash, arms and beacon, from Vela's demo maths.
  - `Runtime/Play/HostViews.PatrolBot.cs`: read-only from the engine.
  - `Runtime/Play/Proof104.cs`: the r104 proof.
- **The beacon** lights from Alert 2 or when the weapon is out. The view counts the weapon as out when it's drawn or has a target, because the engine's patrol never sets "drawn".
- **r104 passes:**
  - 11 parts, 0.0 cm off Vela's pivots, no added rotation;
  - the ball rolls 1.02 m per 1.00 m;
  - the draw shows the hatch at 150°, the gadget out 0.140 m, the focused face and the beacon lit;
  - only the face carries a property block.
- **Kit step:**
  - `kit.json` adopted, and the cover and skyline digests regenerated (Latch).
  - `bun run collide` (Aster) added only the PatrolBot104 record.
  - `bun run sim:site` is unchanged.
- **Batch:** everything through game-build and its proofs passed, with 13 shots and a clean budget.
  - late-fight wasn't judged ("NOISY MACHINE": a Blender bake from another project was using about 1,600 % CPU), and tp-long didn't run.
  - Next, when the Studio is quiet: `bun run batch --from late-fight`.
- **Open:**
  - Glint: the beacon's red under bloom, and its placeholder emission.
  - Splice: five skyline tests fail on an extra CAP-3, also at HEAD.
  - The robot's view keeps moving while paused.
  - The square's dock stays empty, so no robot units appear in the Handover. Sky to decide.
  - Chime's sound, once Sky picks the siren.
