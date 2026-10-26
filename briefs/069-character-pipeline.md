# 069 · Character pipeline: one body, one skeleton, a wardrobe, made by rule

**Stage log:** `briefs/069/README.md` records what's proven, what failed, Sky's decisions and where the evidence is. As of 2026-10-27, the intake prompt and the $0 fit test are done, and the end-to-end test is approved (3–4 meshes from the intake stills, about $2.70).
**State:** proof, 2026-10-26. Sky asked for it and will run it in its own dedicated session. Scratch only: no install, no GDD edit, nothing in the game's build until Sky picks from the proof's pictures.
**Asked by:** Sky (2026-10-26): "I'm thinking we regenerate our models - work out a real pipeline that works for all models of every type so can generate lots of different characters easily … What do AAA studios do for diversity?" Then, on main's proposal: "yeah, bring a brief together and I'll start a new dedicated session".
**Owner:** Forma (the pipeline and the proof). Glyph checks the frames at play zoom. Redline checks the frame cost. Latch makes the Unity frames in a scratch scene. Aster routes the work and takes the GDD change to Sky once Sky picks.

## Why
We have 26 figures today (`tools/figures/figures.json`). Each one is a separate image-to-3D mesh with its own rig: almost all have their own Meshy-style 24-bone rig, and the Enforcer has a Mixamo rig. That gives one baked texture per person, recolours by hue window, and nine crowd looks. Every new person is a full generate, rig, clean and LOD job. Every rig has its own bone axes, so we keep fixing the same things one figure at a time:
- mitten hands;
- cape slits (c1 pulled from the pools);
- the torso web tear on the carried councillor;
- tray offsets per waitress (`manifest.json` forLatch 10);
- per-figure seat offsets;
- guard models on people who aren't guards (Sky's 0.2.122 note).

The idle slide (every clip with its root XZ left out of the pose) is being fixed separately in 0.2.123 and is not part of this brief.

## What AAA studios do (main's summary for Sky, 2026-10-26)
- A few base bodies on **one shared skeleton**. Height, weight and build are blend shapes on the same mesh, so every clip, prop attachment and fix works on everyone.
- **Parts in slots:** head, hair, top, legs, shoes, hat, accessory. Each part is fitted to the base body and takes its skin weights from it.
- **Colour by mask.** Each part has a channel mask for fabric, trim and skin, so one coat gives many colourways at no extra cost.
- **Rules, not hand picks.** A role lists the parts and palettes it may use, and a seed (the person's id) picks the combination, so the same person always looks the same.
- **Shared materials and atlases,** so a crowd costs few draw calls.
- **Heroes hand-finished on the same base.**

Examples: GTA's peds (component slots with texture variants), Assassin's Creed Unity's crowds, Hitman, Cyberpunk 2077. Engine tools: Unreal's Mutable; in Unity, UMA (Unity Multipurpose Avatar, open source).

## The pipeline to prove
Sky, 2026-10-26: "whole point is a super clean pipeline." The pipeline is the deliverable. It runs in stages (body → skeleton → clips → wardrobe → figures → export), each with defined inputs and outputs, from one command. Everything that varies lives in data, with no per-figure, per-part or per-clip special cases in code. Nothing carries over from the old figures, and the build is byte-identical on every run. Bad input fails loudly. The work goes stage by stage, and Sky signs off each stage before the next starts: first the bodies and clothes, shown as every combination they can make (Sky, 2026-10-26: "the first thing should be signing off the models / clothes pipeline so I can see all the combinations we can do"); then the clips, starting with walk and run. Also: "shouldn't need to be adjusting and fine tuning if we can help it." Pick methods that are correct by construction: standard Humanoid retargeting, clips that are clean as sourced, speed from root motion × humanScale, and garments fitted by MPFB's own system. Checks fail the build; they are not dials to tune.

1. **Base bodies:** a man and a woman, with build and height blend shapes. First candidate: MPFB, the MakeHuman add-on for Blender, which is scriptable, gives the same result every time and is believed to give CC0 output. Verify the licence of the add-on's output and of every asset it uses before building on it. Compare at least one other candidate briefly; say which and why you picked the one you did. Faces matter only for the portraits, third person and the conversation close-ups. At play zoom (40–60 px) silhouette and colour carry the read.
2. **One skeleton for everyone:** a Unity Humanoid rig with fingers. Mixamo, the Quaternius Universal Animation Library and any other humanoid library retarget with no per-figure work. ~~Show our current clip set (`unity/Assets/Suborned/People/Clips`) playing unchanged on the proof figures.~~ Changed by Sky (2026-10-26, after round 1 showed the old clips' flaws on the new bodies): "forget existing animations - this is all fresh stuff so nothing from previous carries over." The proof makes a fresh clip set for the new skeleton (sourced, retargeted and cleaned by the pipeline, with licences checked); nothing from today's clips or figures carries over. Today's crowd and clips are the comparison baseline only.
3. **The wardrobe:** garments fitted to the base body that take its weights (data transfer), with shape keys that follow the body sliders. A garment's first shape may come from image-to-3D or Codex art. Stills come from the Codex CLI first (`tools/paid/paid.py codex`, $0). Paid 3D goes through `paid.py` with a budget, kept economical, and needs Sky's OK over $10. Every garment is retopologised, fitted and weighted to the base, never a whole one-off person.
4. **Colour masks and palettes:**
   - Palettes come from the style contract (`contracts/style/`): cream, seafoam, chrome, brass as a thin line only.
   - The faction colours come from the GDD: guards amber, rivals violet. They are world colours on clothes, not the unlit signals.
   - The cell are visitors from a nearby timeline: sharper, with no deco or 1950s tailoring.
   - People prompts carry no date.
5. **Rules → figures:** role rules (extend `tools/figures/roles.json`) list the slots, parts and palettes each role may use. The generator picks by a seed from the person's id, so the same input gives the same figure, and it writes LOD'd FBX plus the manifest rows Latch reads today. It runs at build time only, with no AI at runtime.
6. **Heroes:** the cell, Vale, Inez and the driver are rebuilt on the same base and skeleton, then hand-finished for portraits and close-ups. This is out of the proof's scope; say what it would take.

## Hold
- **Look:** 1950s styling with technology centuries ahead. The concept art leads (`briefs/008/concepts/`, the cell and Vale concepts the GDD names). Job, silhouette and facing readable at 40–60 px at play zoom 14 at 38°.
- **Frame cost:** the game is already 8–10 ms over the Air's 60 fps line (brief 064, `briefs/064/air-0122`). A crowd from the new pipeline must cost no more than today's crowd on the Studio, and should cost less: shared materials and atlases, fewer draw calls. Report triangles, materials, draw calls and skinned-mesh count against today's figures. No Air run without Sky's OK.
- **The shared tree:**
  - work only in `out/scratch/forma/char-pipeline/` and new files under `tools/figures/`;
  - no edits to the installed figures, `People/`, `unity/Assets/Suborned/People/manifest.json` or the game's scenes;
  - Latch alone makes a scratch Unity scene, never the HarbourDistrict build;
  - no stash, checkout, reset or clean; kill processes by PID only;
  - start any load through `bun run guard`.
- **Batch 0.2.123** is building in the same tree. Don't touch `tools/figures/unitymeta.py`, the clip metas or `FigureBuild.cs` until it lands. Forma's idle-slide fix is in those files.

## Direction after round 1 (Sky, 2026-10-26)
Round 1 proved the plumbing but not the look. MPFB bodies with rule-made garments "look awful third person - and lose any real appeal to how we do things now." Sky's aim: "a very dynamic and diverse character building pipeline that can have great animations", where unique characters are easy to generate "without worrying about this every time."
- **The look stays generated from concept art.** Codex stills go to image-to-3D, as today. Each generated mesh is then wrapped onto the MPFB base, so it inherits one skeleton, the base's weights and a standard rest pose. Its texture is transferred across. Variety comes from more generated people plus recolouring by mask. The procedural wardrobe is shelved. The bodies, skeleton, licence work and determinism from round 1 carry over.
- **A precise intake prompt comes first.** The generation prompt and sheet spec fix the pose to the base's rest: an A-pose with the base's arm angle, legs straight and parallel, feet at the base's hip-joint width, forward and flat, and fingers apart. Every generated person then wraps cleanly. Sky signs off the prompt before any paid 3D or wrap test (Sky: "I want to make sure I have a very precise prompt for wrapping"; in one of the Unity videos the feet clash).
- **Animation fix found** (Latch, `out/scratch/latch/char-pipeline/leftfoot.md`): Unity builds each clip's and each figure's Humanoid reference pose from that file's own first frame or rest. The left foot came out toe-up and the legs bowed out. The fix is one shared neutral-leg reference for all clips, plus neutral legs written into each figure's meta. It applies by rule on both sides.
- **Meanwhile:** "we can fix up every figure if it's cheap and easy." Once batch 0.2.123 lands and `unitymeta.py` is free, Forma applies both rules to today's 55 clips and every installed figure, and Latch checks the result in a build.

**Clip requirement added 2026-10-26 (main, Sky's OK):** the clip stage must also cover fight B's cover and pin states. See `briefs/069-fight-b-clips.md`. Today's stand-in clips carry the game until then.

## Done-check (the proof)
1. Two base bodies with build and height sliders, on one Humanoid skeleton with fingers. The licence is checked and written down.
2. About 8 wardrobe parts (2 tops, 2 legs, 1 coat, 2 hair, 1 hat), each fitted, weighted and masked.
3. 50 figures generated from role rules: commuters, guards (amber uniform, varied), rivals (violet crew, varied), café staff. Running the generator twice gives byte-identical output.
4. **Pictures for Sky:**
   - a contact sheet of the 50 at play zoom beside today's crowd;
   - the same 50 in Unity (Latch's scratch scene) playing idle, walk, run and aim_pistol, with no foot slide, no tears, and hands that aren't mittens;
   - one guard group and one rival group, uniform but individual.
5. **Numbers:**
   - Studio frame cost of a 60-person crowd, old against new;
   - per-figure triangles, materials and bones;
   - the time to add one new garment;
   - the time to add one new role.
6. **The read:** what the full rebuild would take (the parts count for the city's roles, the heroes, the swap-over in Latch's figure code), and the open risks.

**Evidence:** `out/scratch/forma/char-pipeline/` with a `KEEP` file: `proof.html` (self-contained: the pictures, the numbers, the licence notes, the read), the generator scripts and the role rules. Commit the scripts and this brief's docs; the pictures stay on disk.

## For Sky after the proof
1. Does the new crowd beat today's at play zoom, measured against the concept art?
2. Rebuild the city's people on it? If yes, this is a GDD version (§14 Forma's delivers, §06 and §15 where they name the stand-ins), then `bun run crew`.
3. The cell and Vale: rebuild them on the base, or keep their current look and only re-rig them?

## Read first
- `CLAUDE.md`
- GDD §01, §14 Forma; §06, §15 for people and style (`GDD/the-suborned-agent-gdd-v0.26.0-2026-10-26.md`, load by section, never the whole human GDD)
- `tools/figures/` (`figures.json`, `roles.json`, `build.py`, `figlib.py`, `clips.json`, `unitymeta.py`)
- `unity/Assets/Suborned/People/manifest.json` (`conventions`, `forLatch`, `playRoles`, `families`, `jobBlocks`)
- `briefs/068-asset-quality.md` (character materials #3, the frame budget)
- the store: `~/Projects/the-suborned-store/source-art/` (`Citizens3D`, `Anim/mixamo_library`, about 2,278 clips)
- the idle measurements: `out/scratch/main/idle-sway/sway.json`

## Starting the dedicated session
From `/Users/sky/Projects/suborned`, start a plain `claude` session (it can spawn crew; Forma's card can't), then: "Run brief 069 (briefs/069-character-pipeline.md), the proof only, in scratch. Spawn Forma (character-artist-animator) for the pipeline, Latch (unity-engineer) for the scratch Unity frames, Redline (code-performance-reviewer) for the cost and Glyph (interface-artist) for the play-zoom read. Batch 0.2.123 is running in another session: share the tree, and keep off the files the brief's Hold names."
