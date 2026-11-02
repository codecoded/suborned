# Assignment 7 · Complete AI Dev Pipeline

**Student:** Sky Rumsey · **Game:** The Suborned · **Branch:** `assignment-07`

**Deadline:** 3 November 2026, 23:59 GMT (4 November, 06:59 Bangkok).

**Status:** reassessment in progress. The portrait walkthrough is supporting evidence only; the primary pipeline-run recording, final audit and playable-link gate are outstanding. See [the reassessment](ASSESSMENT.md).

**Primary demonstration choice:** the people/relationships/routines path, beginning with the accepted `r016` cast and following its checked expansion into simulation input and Unity. World/DecoKit is the visual companion. The portrait clip is optional supporting material; a higher-resolution local draft has been rendered but is not uploaded or approved as primary evidence.

The Suborned is a single-player game of real-time squad tactics and social infiltration in a bright retro-future city. The Handover demo asks the player to take Councillor Popovic alive using a four-person cell. AI content is generated during development; no model runs during play.

## Submission links

- **Playable game:** https://codecoded.itch.io/the-suborned
- **Pipeline source:** https://github.com/codecoded/suborned/tree/assignment-07
- **Primary pipeline-run video:** pending.
- **Supporting portrait walkthrough (CodeCoded, Unlisted):** https://youtu.be/E2I0FdpXLUk (signed-out playback verified; historical AI generation and executed offline export replay are labelled).
- **Provisional one-page portrait audit (not the final wider-pipeline audit):** [audit.pdf](audit.pdf), with [HTML source](audit.html).
- **Original instructions:** [assignment-brief.pdf](assignment-brief.pdf).

The public page was checked anonymously on 3 November (Bangkok). It offered `TheSuborned-0.2.231.zip`, 521 MB, for macOS, and explicitly said its gallery showed 0.2.232. Local evidence below is from build **0.2.232**. Sending that build by Taildrop did not update the public download. No download/first-play timing has been recorded. Apple silicon compatibility is established locally; the marker's machine is unknown. A working landing page alone does not meet the two-minute gate.

## Supporting evidence for markers

The focused pipeline example is **36 live roster portraits**: four agents × nine expressions. The expressions change with the simulation's actual attention, activity, firing and health states. These are production assets already used in the game, not Assignment 6's unintegrated conversation proposals.

From a checkout, with Python 3 (no third-party modules, API calls, Unity or login):

```sh
python3 courses/a07-complete-ai-dev-pipeline/evidence.py verify
```

This verifies the recorded cost arithmetic, all 36 tracked Unity asset hashes and the six production integration source files. It does **not** replay the original model, prove the public download's contents or certify the playable-link gate.

## Automated flow and evidence

1. **Generate:** [`portraits_gen.py`](../../tools/figures/portraits_gen.py) sends reference-conditioned expression-sheet prompts to Seedream via the budget-guarded paid helper. [Fourteen historical receipts](evidence/generation-ledger.json) retain prompts, model, reported cost and raw-output SHA-256. Migrated timestamps retain their `mtime` provenance.
2. **Select:** approved takes are Scout/Fixer/Enforcer t3 and Tech t4. This is a human art-direction step. Four later downed replacements are an explicit second manual exception; see below.
3. **Export:** [`portraits_export.py`](../../tools/figures/portraits_export.py) finds the rings, crops and masks the generated grid, and exports consistently named PNGs. No manual reformatting of the normal grid output is needed.
4. **Integrate:** [`KitImport.LivePortraits`](../../unity/Assets/Suborned/Editor/World/UI/KitImport.cs) copies changed `@2x` files, configures Sprite importers and refreshes the assets. [`GameBuild`](../../unity/Assets/Suborned/Editor/World/Play/GameBuild.cs) calls it before building. This importer requires a retained local `briefs/033/portraits/final/` folder; a source-only clone lacks that ignored folder, though its 36 installed Unity sprites are tracked.
5. **Play:** [`HostPortraits`](../../unity/Assets/Suborned/Runtime/Play/HostPortraits.cs) chooses the expression from simulation state; [`HudToolkit.Live`](../../unity/Assets/Suborned/Runtime/Play/HudToolkit.Live.cs) loads `Kit/portraits/live/<agent>_<state>@2x`. [Integration evidence](evidence/integration.json) records 36 final-to-Unity byte matches and their exact local build-input hashes.

On the author machine, with the retained raw AI sheets and Pillow/OpenCV/NumPy available, the deterministic stage can run again without an API:

```sh
python3 courses/a07-complete-ai-dev-pipeline/evidence.py replay
```

It writes only to `out/scratch/a07/portrait-replay/`. The author-machine run reproduced the **32 unmodified grid states** exactly, and reported the four downed replacements separately. [Its result](evidence/replay.json) is retained. A replay is not a fresh AI-generation run. Paid originals stay in the existing asset storage and are not duplicated into the course folder.

## Costs and audit boundary

[Cost evidence](evidence/costs.json) is calculated from actual receipts: **$1.15560 / 14 calls**, including rejected/superseded takes. Full nine-state sheets cost $0.0963 each (10 calls, $0.9630); targeted downed-option strips cost $0.04815 each (4 calls, $0.19260). The **50% reduction is per repair call**, not per output portrait or a controlled equal-work benchmark. Subscriptions, labour, hardware, hosting and other asset jobs are excluded. This is the portrait campaign's cost, not the entire game's cost.

The four selected downed options were ring-refined and desaturated to 18% after human selection ([brief 033](../../briefs/033-live-portraits.md)). The original grid exporter does not reproduce that treatment. We document this rather than claim 100% automation. A versioned selection/override manifest, one deterministic override exporter, and a source-asset archive would remove the implementation friction; human aesthetic approval would remain a deliberate gate.

## Supporting video prepared for review

The author-machine artifact is `out/scratch/a07/video/TheSuborned-A07-pipeline-walkthrough.mp4` (41 seconds). Its [receipt](evidence/video.json) records the SHA-256. It shows a historical generated sheet and actual cost receipts, stdout from a newly executed deterministic export, the production sprite set, the automatic import/build hooks and live gameplay frames from 0.2.232.

Every frame labels this as an **offline replay of historical generation**. It is an annotated evidence walkthrough, not a screen recording of a fresh model call or a new Unity build. It makes no claim about the two-minute first-play gate. Hosted on **CodeCoded / @codecoded** as **Unlisted**: https://youtu.be/E2I0FdpXLUk. The previous Gamelovr copy was made Private after the channel mistake was identified. The brief does not explicitly require newly purchased model calls, but this annotated replay is not sufficient primary run evidence. Record one complete production path executing into the engine. New live calls require explicit opt-in and budget guards.

To reproduce on the author machine (retained production sheets, captured gameplay frames, Pillow/OpenCV/NumPy and ffmpeg required):

```sh
python3 courses/a07-complete-ai-dev-pipeline/walkthrough.py
```

## Remaining submission checks

- [x] Branch and focused pipeline evidence prepared.
- [x] Public itch.io project exists and offers a downloadable build.
- [x] Provisional portrait audit PDF rendered and checked; supporting walkthrough prepared.
- [ ] Final one-page audit aligned with the demonstrated primary pipeline.
- [ ] Confirm marker's supported platform or provide a compatible build.
- [ ] Choose the exact submission build and upload it; page, download and evidence must agree.
- [ ] Record a fresh, timed visit → download → launch → control of the game on another machine, with no development tools or command-line workaround. Pass only at ≤120 seconds.
- [x] Publish the labelled evidence walkthrough: historical generation, newly executed deterministic exporter, production sprites, automatic Unity import/build hook source, and recorded gameplay state changes. It does not record a fresh model call or a new Unity build.
- [ ] Record a complete production path running into Unity/the player; label archived model responses if used. Obtain explicit opt-in before any new model call.
- [ ] Insert and verify the primary pipeline-run video URL. The supporting clip has a separate link.
- [ ] Check repository access while signed out.
- [ ] Review the one-page audit against the final video and public build, then submit.

There is no claim that these remaining gates have passed. [Submission status](submission.json) keeps the missing links and checks explicit.
