# Illustrated art bible

Run `bun run art:bible` from the repository root. Open the printed HTML path in a browser. No database, model, Unity, external image store or Python installation is needed. The page embeds the v0.15 boards and the eight street paintings; the art approved since v0.15 (skyline, crossings, HUD, the cell) is linked by relative path into `briefs/`, so open the page from a checkout.

Run `bun run art:bible --check` to compare the checked-in page with a fresh deterministic rendering. It exits 1 if the page is absent, changed or stale. Input hashes are recorded inside the HTML. After an approved GDD version change, regenerate and update the human GDD and root README links to the new output filename.

Inputs:

- The latest matched agent/human GDD pair, selected by the same numeric-version selector as RAG. Art rules come from agent §15; the human GDD is also fingerprinted so its changes invalidate the check.
- `contracts/style/suborned.style.json`: configured models, dimensions, fields, reference limits, palettes and prompts.
- The archived v0.15.0 HTML: embedded image bytes only. Its prose is never imported. Keep it as the portable image source; its banner identifies it as historical.
- `briefs/008/concepts/PROVENANCE.json` and its eight paintings, verified against their recorded hashes.
- The approved-since-v0.15 images listed in `generate.ts` (`approved`), each cited by a §15 rule and hashed into the check. They must stay committed (the `.gitignore` keep list).

The HTML explicitly records existing source contradictions and their editorial resolution. In particular, the later no-dates rule overrides dated people framing/finish/guide text; current cell and 3D presentation rules override legacy gold/painted-background instructions. The exact model-sheet model comes from the style contract rather than the GDD's inaccurate “All Seedream” shorthand. These are presentation corrections, not new design decisions or changes to runtime generation. The underlying legacy style fields remain unchanged.

Two reference thumbnails were already missing from the archived bible (`commuters__spaceage_bold.png` and `corporation__spaceage_bold.png`). Their slots are labelled, rather than silently dropped. Historical probe examples are kept as history, not evidence of a newly validated model route.

The drift check detects changed inputs and generated output; it does not prove that future prose changes are semantically consistent. Review the reconciliation notes when changing either design source.
