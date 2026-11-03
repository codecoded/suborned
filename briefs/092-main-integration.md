# Main integration — 4 November 2026

Sky authorised bringing the current game into main and removing superseded local
branches. Assignment 7 is locked: its local and remote branch remain at
`9ab7c14bc686eb378cffbd556691742b09048145`, and its course directory is unchanged.

## What landed

- Merge `ae73a756` brings the Assignment 7 game history into the previous main,
  retaining main's dialogue style-agent tooling and Codex adapter changes.
- Merge `ad98afab` preserves the 23-file pre-cleanup checkpoint from
  `safety/before-main-cleanup-20261103`. It includes build metadata, the missing
  life-contract type, existing reports, performance capture scripts, and saved
  Unity scene, grading, reflection and material state.
- The older style-agent extraction now recognises the current GDD's world,
  cast, approach-price and timing wording. Its focused tests use the current
  30-second Listen, 35-second Help and 25-second Pressure durations. Archived
  sources still reconstruct the original Assignment 6 results.

## Checks and limits

Passed: TypeScript typecheck; crew-card drift check; Assignment 7 people evidence
verification (12 source hashes); 60 style-agent/Codex tests; all 24 archived
Assignment 6 dialogue replays using the integration tooling.

Checkpoint JSON and Unity YAML parse successfully. FightRange retains its
356 serialized documents and 94 GameObject names. Night-grade values match by
component name; the reflection payload is a saved bake. The added inactive grade
component resolves to its tracked script. Both Air capture scripts pass `zsh -n`.

The broad offline suite is **not green**: 2,535 passed, 7 skipped, 29 failed and
one import error, in 554 seconds. The import error was the old style-agent GDD
parser, fixed and covered by the 60 passing focused tests afterwards. Other
failures concern kit/plan fixtures, generated-data drift, transitive code hashes,
skyline checks and recorded routes. Four failures (kit digest, transit code hash,
and two road/pavement expectations) reproduce from an isolated archive of the
locked A7 source. The remaining failures have not all been independently rerun
against that archive. Their source directories and current kit/plan inputs were
not changed by this integration. Do not treat this merge as a passing full suite.

No Unity import, new player build, playtest or itch upload was performed. Saved
Unity files received structural review, not visual verification. Existing
untracked art, source assets and local evidence were preserved. Submission
branches 5, 6 and 7 and all existing worktrees are retained; the safety checkpoint
branch remains as a recovery reference. Raw check logs are local under
`out/scratch/main-integration/`.
