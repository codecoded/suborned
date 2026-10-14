# Handoff · The Suborned · 2026-10-14 (Air → Mac Studio)

Work moves to the Mac Studio (M5 Max, 128 GB). The 15-inch MacBook Air M4 stays the performance reference and a preview screen. Read this file, then `CLAUDE.md` (the game's core concepts are at the top), then the briefs it names. It replaces the 2026-10-13 handoff.

## 0 · Starting a session on the Studio
1. Open it in `/Users/sky/Projects/suborned`. That exact path is what links the project's Claude memory.
2. Run `git pull`, then `bun install`.
3. Open `unity/` in Unity 6000.6.2f1, so the Unity MCP can connect.
4. Lead (Aster): `claude --agent lead-producer`. In a tool that can't pick an agent, start with: "Read briefs/HANDOFF.md and CLAUDE.md, act as Aster (lead), and continue §4 in order."
5. The session's first checks: `bun run doctor` (the stack and the index; run `bun run ingest` if the index is missing), `bun run typecheck`, `bun run plan`, and Unity's console showing 0 errors.
6. Only one machine works on the branch at a time. Commit and push before switching, and pull when you arrive.

## 1 · Where things are
| | |
|---|---|
| Branch | `world/space-age`, pushed; the Studio is on the same commit. Not merged into `main` (`de1a3db`). |
| `assignment-04` | **Frozen** (the submitted course assignment, tip `a0d05ff`). Never change it, commit to it or check it out. To inspect it, clone it into a separate folder outside `/tmp`. |
| GDD | v0.20.1 (`GDD/the-suborned-agent-gdd-v0.20.1-2026-10-14.md` + human `.html`, which wins on conflict). Style contract `contracts/style/suborned.style.json` v0.20.1. |
| Health at handoff | `bun run typecheck` clean · `bun test tools/world tools/plan` 247 pass · `tools/gen tools/levels` 148 pass · `tools/rag` 33 pass · `bun run plan` and the Handover overlay exit 0 · `bun run verify` OK. |
| Live export | `out/world/harbour-district.world.json` sha256 `9b2b451d…`. It is **before** brief 009 B: crossings are still zebra, with landing slabs, until Splice regenerates it (§4). |

## 2 · Two machines
- **The Studio does the work:** Unity 6000.6.2f1, Blender 5.2.2, crew agents and builds. It has no fan limit, so jobs can run side by side, but agents still share one working tree.
- **The Air measures and previews.** The 60 fps target is set on the 15-inch Air (GDD §11), so perf runs happen only on the Air, with the machine quiet: build the player on the Studio and copy it over (§3). Numbers taken on the Studio are not evidence.
- **Retrieval (SurrealDB + embeddings) runs on the Studio** for both machines, over Tailscale (`skys-mac-studio`, 100.81.57.43). Each machine's git-ignored `.env` holds the endpoints and the password (`.env.example`; never print the password). There is one database per corpus hash, so neither machine's ingest wipes the other's. `bun run doctor` checks the stack and the index.
  - After a Studio reboot, Tailscale must be up before Docker starts the containers.
  - Over SSH, use `docker compose up -d --wait`: `bun run up` rebuilds the images, which needs the Keychain.
- **SSH from the Air:** `ssh studio` (key `~/.ssh/studio`). Screen Sharing is on.
- `out/` and the Unity kit and scene are git-ignored. They were copied from the Air at handoff, so the Studio opens the same scene. Rebuild them with `bun run world`, `bun run decokit`, `bun run kit` and then `Suborned/World/Build Harbour District (Export)`.

## 3 · Previewing from the Air
1. **Screen Sharing** for the Editor and Play Mode. In Finder, ⌘K → `vnc://skys-mac-studio`, and pick High Performance when asked (both Macs are Apple silicon). On the home network it is close to local. Good for looking; frame rates seen through it mean nothing.
2. **A native player on the Air** for walking the world at real speed, and for perf. Build the macOS player on the Studio (the perf player: `Suborned/Perf` menu → `out/build/perf/SubornedPerf.app`), then on the Air run `rsync -a --delete studio:Projects/suborned/out/build/perf/SubornedPerf.app/ ~/Projects/suborned/out/build/perf/SubornedPerf.app/` and open it. Latch's harness takes `-perf` for measured runs.
3. **Shots** from the agents (Unity MCP `screenshot`, `out/shots/`): ask the session to send them, or pull them with `rsync`.

## 4 · Open work, in order
1. **Brief 008 B6, perf (Latch).** Evidence is in `briefs/008/perf/` (report `b6-2026-10-14.html`).
   - **The player glow is fixed** (`VertexChannelCompressionMask` 4054 → 0; NaN pixels are 0 at every stop). It looks like a Unity bug worth reporting.
   - **No clean numbers yet.** Run ab-1 overlapped other work, so read it as ratios only; r2 is invalid.
   - At the heaviest stop, fixes 1 (SSAO from depth) and 3 (2 cascades) together cut the frame about 23%. Turning **the architecture fade off halves it**: its discard probably disables the GPU's hidden-surface removal. SSAO costs 24–35%.
   - Proposals, none applied: keep fixes 1 and 3 (Glint signs off); hold the proxies (fix 2), whose gain is unmeasured and which Vela and Glint should judge; measure a fade that discards only inside its reach (Redline item 9); Glint decides whether SSAO stays.
   - **To run the reference stops:**
     - On the Studio, open and save HarbourDistrict as built from export `9b2b451d`, then run `Suborned/Perf/Build Perf Player`. The first build compiles shaders for a long time (85 minutes on the Air).
     - Copy `out/build/perf/SubornedPerf.app` and `plan.json` to the Air.
     - On the Air: AC power; Editor, VM and heavy apps closed; player kept in front. Run `briefs/008/perf/scripts/run.sh b6-ref -perfStrict` (three passes, about 65 minutes, including the fixes 1+3 variant that decides the proxies).
   - Then: Glint signs off fixes 1 and 3 and decides on SSAO; Redline's budget proposal (world ≤ 9 ms GPU, people and vehicles ≤ 4.5 ms) goes to Forma and Keel; the game ships with vSync 0 in every quality level.
   - Run the perf stops before brief 009 changes the export, or rebuild the scene from `9b2b451d` for them, so runs stay comparable.
2. **Brief 009, crossings** (`briefs/009-crossings.md`). A and B are done; the floating signal eyes are designed and placed.
   - **Splice:**
     - adopt the six pieces into `kit.json`; their files are under `out/decokit-009` (use `DECOKIT_OUT`);
     - remove the beacons wherever an eye stands (bollards stay);
     - draw crossings on the CAD sheet as two light lines along the walk;
     - run `bun run world` (the new live export);
     - declare the replay drift.
   - **Vela:** the thin brass frame line on the strip's long edges (`briefs/009/concepts/crossing-future-play-2.jpg`).
   - **Latch, brief 009 C:** CrossingKit reads the new export shape (`contracts/world/world.schema.ts`, commit `3039f53`):
     - the eye's display turns to face the play camera (yaw 30);
     - only the orb body casts a shadow;
     - confirm the arc yaw in a shot (`ARC_BULGE` in `tools/world/gen/street/partkit.ts`);
     - shots beside the concepts, and frame cost on the Air.
3. **Facade third pass (Latch):**
   - `sand_p` → stone #A2937E with a brass bead on the ledge (`ProcShop.cs:37`);
   - `t_hotel` → `e_seafoam`;
   - the jazz-club and pawnbroker stripes gold → chrome;
   - then rebuild.
4. **After perf (Splice):**
   - trees and lamps at play zoom;
   - street pieces at LOD1;
   - a baked camera stand-off grid for panning.
5. **Questions for Sky** from the world premise (no networks), with Aster's recommendation for each:
   - How does a robot's sighting reach security? Recommended: it phones in from a street call box to a staffed dispatch desk, with no change to timing.
   - What is the Tech's tether, and what is Reroute? Recommended: the Tech works at a junction box on the camera's line, and Reroute is a false radio dispatch.
   - Can a robot ever be turned? Recommended: no; it can only be timed around or blinded.
   - Stealing or burning records? Recommended: later.
6. **Backlog:**
   - walking body clearance as a plan standard (Datum);
   - a walking pace for the Handover (brief 003);
   - the Official entrance surface (Waymark);
   - move the waitress loop to x −5.35 (Pulse);
   - review crossing coverage (Datum);
   - the station kit bakes the name HARBOUR (Vela);
   - OvalPlanter soil;
   - `DK_Enamel_Coral` and `DK_Neon_Gold` ΔE from the signals;
   - an entrance camera site at the Office door;
   - a far shot to decide crown gold or brass;
   - define "adjacent" in grammar §4 (Vela);
   - a third-person try-out.
7. **Brief 003, missions** (on hold until Sky signs off the world) and **brief 005, city life** (built, not started in the sim). Merge `world/space-age` → `main` when Sky is happy with the look.

## 5 · How to work
- **The crew:** `claude --agent lead-producer` (Aster), or spawn crew members by job name. Cards are generated from the agent GDD by `bun run crew`. Edit the GDD, never a card.
- **Shared tree:** never `git stash`, `checkout`, `reset` or `clean`. Before every commit, run `git diff --cached --name-only`: agents leave things staged, and a GDD rename once slipped into an unrelated commit. Commit and push only when Sky asks; on this branch Sky has said to commit as work lands.
- **Look decisions come from concept art** made from an open brief (style contract `concept_prompt`), never from palette tables; show Sky pictures.
  - The era: an alternate 1950s with centuries of atomic power. People prompts carry no date.
  - Tools: the Codex CLI image tool (`~/Projects/the-suborned/workbench/Tools/Concept/codex_image.py`, no per-image cost) and Runware Qwen Image 2.1 (key in that workbench's `.env.local`).
- **Content goes through the pipeline:** world big moves and missions are generated through `tools/gen` + `tools/levels` + `tools/rag`, as recorded runs that replay. Model calls are opt-in (`SUBORNED_LIVE=1`, `--approved-by`, `--max-usd`), never in tests.
- **Unity:** only Latch edits scenes. Check `editor_status` first, and leave the Editor alone while Sky is in Play Mode. The scene is generated from the export: never edit it by hand.
- **Evidence** that must ship goes in a committed folder (`briefs/<n>/`), never only in `out/`.

## 6 · Where to look
- Briefs `briefs/001…009`, each with a status line; 008 and 009 are live.
- The reference art:
  - `briefs/008/concepts/`: the 26 Sep paintings, street level;
  - `briefs/009/concepts/`: crossings;
  - `~/Projects/the-suborned-store/concepts/`: skyline.
- The look:
  - `contracts/world/harbour-district.grammar.md`;
  - `unity/Assets/Suborned/Editor/World/{look.json, looks/harbour.json, light.json}`.
- Reviews in `out/review/` (copied, git-ignored): `009-crossing-pieces.html`, `008-b6-perf.html`, `008-civic-parts.html`, `008-pieces.html`.
