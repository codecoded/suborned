# Brief 098 step 0: unset counts change nothing (Keel, 2026-11-07)

`suborned-sim hashes` prints every scenario's and recorded route script's checkpoint hashes (518 runs: the scenarios, contracts/missions/handover/routes, briefs/011/motive-fights) with their results.

- Base (29b3452b, before the dial): built to out/scratch/keel/base-cli, `suborned-sim hashes` → sha256 `0be53a7d2cc9b6b25c27e3386827ca0a98e2fe65afb1687323456843cee153f4` (inputs hash 10378814492870903911).
- With the dial (this branch), no counts set: the same command → sha256 `0be53a7d2cc9b6b25c27e3386827ca0a98e2fe65afb1687323456843cee153f4`; `cmp` reports the two outputs byte-identical.

The inputs hash does not move because the dial's rules are their own file (contracts/rules/population.json), read and hashed only by a start that sets counts.
