# Assignment 7 · Complete AI Dev Pipeline

**Sky Rumsey · The Suborned · Unity · branch `assignment-07`**

The Suborned is real-time squad tactics and social infiltration. The Handover asks the player to take Councillor Popovic alive by learning about ordinary people and using their relationships. AI content is made during development; no model runs during play.

The primary example is the **people, relationships and routines pipeline**. World/DecoKit supplies the setting in the gameplay footage. Portraits are optional supporting evidence, not the submission's main example.

## Submission links

- Playable game: https://codecoded.itch.io/the-suborned
- Source: https://github.com/codecoded/suborned/tree/assignment-07
- Primary execution video: **https://youtu.be/725LKDmVNQ8** - **CodeCoded / @codecoded**, Unlisted; 95 seconds, 4K delivery.
- One-page audit: [audit.pdf](audit.pdf), [HTML source](audit.html).
- Original brief: [assignment-brief.pdf](assignment-brief.pdf).

**Release gate remains open.** The anonymously viewed itch page offered macOS `TheSuborned-0.2.231.zip` (521 MB). A compatible stranger's visit-to-control time of at most 120 seconds has not been certified. The video uses the 0.2.232 player binary with freshly staged current-plan people, explicitly labelled as a local replay variant. It does not claim that variant is the public release or that a fresh Unity compilation occurred.

## For markers

From the repository root, with Bun installed:

```sh
bun install --frozen-lockfile
bun courses/a07-complete-ai-dev-pipeline/people-run.ts verify
```

The check uses tracked files only. It needs no model, database, Docker, Unity, paid credentials or retained game build. It checks the accepted AI run, promoted cast, cost arithmetic, critic-gating evidence and the production source hashes. It does not certify the playable-link gate.

| Requirement | Concrete evidence |
| --- | --- |
| Agent generation with design context | [`r016`](../../runs/levels/harbour-district/r016/run.json), its original prompts/retrieved passages/structured outputs in `round-*/*.call.json` and `pulse.json` |
| Real correction | Round 1 fails P-0; Pulse revises; round 2 passes checks and Keel critique |
| Adopted output | [`cast.promotion.json`](../../contracts/people/harbour-district.cast.promotion.json) holds the production cast byte-for-byte to r016 round 2 |
| Generated people and relationships | [`people-run.ts`](people-run.ts) executes the production expander and checker: 130 people, 222 directed ties, 87 places; retained [receipt](evidence/people.json) |
| Automatic engine integration | [`tools/sim/people.ts`](../../tools/sim/people.ts) exports engine data; the proof stages it and its manifest into a scratch copy of the real player; [`SimBundle.Load`](../../unity/Assets/Suborned/Runtime/Play/SimBundle.cs) checks hashes before loading it |
| Functioning content | Actual player capture: café routines, Inez's revealed need/relationship in Intel, Arthur called from his post, third-person people in the built city |
| Production connection | Four mission profiles match the retained shipped data: names, jobs, temperaments, needs, records, scales and ties. The full background projection differs; see below |
| Actual run costs and optimisation | Original per-call usage records and [computed evidence](evidence/people.json); audit sections 03-04 |
| Reproducible evidence | [record-run.py](record-run.py) records a real timed CLI session; [people-video.py](people-video.py) edits that recording and the player captures |

## What ran in this recording

1. Replay archived **r016**, including the original failure, correction, retrieval/citation evidence and verdict. No fresh inference is claimed.
2. Check the promoted production cast against its run, then execute `bun run people` using current rules, plan, needs and name data.
3. Execute the production simulation exporter, preserving the generated file's hash.
4. Automatically stage that file and an updated hash manifest into a scratch copy of the retained **0.2.232** Unity player; sign and verify the replay copy.
5. Launch that copy and execute ordinary Route A orders through the real host. Capture 340 gameplay frames at 20 fps plus one audited Intel still. No relationship or portrait overrides are used.
6. Retain the hashes, source references, counts, profile comparisons, player report and recorded costs in `evidence/people.json`.

The terminal section is **real stdout captured while the command executed**, rendered legibly at 3840×2160. It is not a GUI desktop screen recording or simulated typing. The edited video removes engine loading/saving waits, keeps the command output at its recorded speed and inserts explanatory cards. Gameplay is genuine 1920×1080 capture from the same run, upscaled for the 4K delivery. Original AI generation is historical. No new Unity compilation occurred: the existing binary/scene loads newly generated data.

Author-machine reproduction (retained player, Bun, Python/Pillow and ffmpeg):

```sh
bun run guard python3 courses/a07-complete-ai-dev-pipeline/record-run.py
bun run guard python3 courses/a07-complete-ai-dev-pipeline/people-video.py
```

Artifacts live under `out/scratch/a07/people-run/`: raw timed `people-run.cast`, uncut terminal video, full command output, freshly exported engine data, replay app, player captures, edited video and receipts. The recording scripts make no model calls. Live model generation is a separate, explicitly approved and budgeted operation.

## Production boundary and the issue the replay found

A fresh expansion exposed a retained pre-remap background projection in the shipped game. Exporting the existing cached population reproduced the production input, but regenerating against today's city plan changed some background names, ties and tram stops. Trying the archived pre-remap plan recovered the places but did not reproduce every resident. No inferred input was presented as an original production pin.

The recording therefore declares a **current-plan replay variant**. The four mission characters' complete compared profiles match production; the entire background population does not. The existing r016 cast itself still matches its promotion record exactly. This is a concrete downstream reproducibility problem, documented in the audit, not concealed as a perfect byte-identical rebuild.

The proposed fix is a complete dependency manifest and stale-output gate: cast + world plan + block + life/routine/need/name inputs + generator versions + output hashes. Human cast approval remains intentional; propagation after approval should be automatic. The current proof stages data into an existing binary; it does not claim a new release has been built or published.

## Cost boundary

Original **r016** recorded usage is **$0.6905 / three calls** (unrounded $0.6904733), including its failed proposal. The largest call is Keel's critic, **$0.2800659**. Claude CLI reports these usage valuations on a subscription; they are not additional API debits or an allocated subscription bill. The offline recording makes zero model calls. Other asset jobs, labour, hardware and hosting are outside this total.

The actual mid-project change is **checks before critique**, introduced in brief 007 F / loop 2. Under the old framework, r008's two checker-failed rounds still incurred two critic calls costing $0.4135186. In r016's failed first round, the critic was skipped: zero critic calls/tokens/cost. These are different objectives/candidates under the same framework; no equal-work percentage or hypothetical saved bill is claimed.

## Remaining release checks

- [x] Accepted cast, provenance and recorded usage verified.
- [x] Real expansion, engine-data staging and player execution recorded.
- [x] One-page people-pipeline audit rendered and visually checked.
- [x] Publish the primary video on CodeCoded, Unlisted; signed-out playback verified; 2160p, 1440p and 1080p renditions available.
- [x] Verify source-repository access while signed out (Public; assignment-07 branch visible).
- [ ] Confirm a compatible marker platform and record a visit-to-control test within 120 seconds.
- [ ] Resolve public-release/replay-variant differences before claiming an identical full release reproduction.

[submission.json](submission.json) retains the open gates. Deadline: **3 November 2026, 23:59 GMT** (4 November, 06:59 Bangkok).

## Optional portrait evidence

The earlier 41-second portrait walkthrough remains [supporting material on CodeCoded](https://youtu.be/E2I0FdpXLUk). Its [receipt](evidence/video.json), original [generation receipts](evidence/generation-ledger.json) and [verifier](evidence.py) remain for transparency. The Gamelovr upload is Private. A higher-resolution local portrait draft was held when the stronger people pipeline was selected.
