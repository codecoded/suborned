# Geometry/walk recovery for plan 0.5.6

The existing generated world/walk pair is fresh and was preserved. The walk was reconstructed in memory and matches byte-for-byte. All eight simulation-data files match HEAD and remained unchanged. No source, Unity, perf, plan or simulation changes were made.

The geometry gate is **blocked**: G-3a counts 59 objects against a cap of 58; the other nine rules pass. The focused tests pass 14/14. See [the handoff](../../oldbuild/READY-geometry-056) for exact commands, hashes, results and next owners.

Evidence: `before.json` and `after.json` record hashes and preservation; `walk-verification.json` records generator equality; `geometry-blocker.json` records rule counts and missing-occluder groups. The three check logs retain actual output. `check-walk.ts` is the rerunnable verification script and writes only its evidence JSON (the geometry loader may maintain its normal cache under out/).
