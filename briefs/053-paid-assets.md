# Brief 053: paid assets in one place, with a viewer

**Objective:** every paid-API generation (Runware and the like; real money on Sky's account) lives under one git-ignored `paid/` tree, goes through one helper that enforces a per-pass budget and writes an append-only ledger, and can be reviewed in a web page that shows where the money went and what came out. Sky, 2026-10-23: "make sure anything generated via API calls are in a dedicated paid assets folder structure (gitignored) with an asset viewer web page so I can review where money's being spent and have clear knowledge of the output"; "be economical".
**Owner:** Splice · **Next:** Aster tells Forma the new call form (below) before any further 052 call; Sky opens `paid/index.html` and checks the totals against the Runware dashboard.
**Done-check:** `bun run paid` builds `paid/index.html` (opens nothing); its totals equal the sum of the ledgers; a pass without a budget cannot call; `bun test tools/paid` and `bun run typecheck` pass; no paid call made while building or checking this.
**Evidence:** `bun run paid` printed `paid: $58.95 in 222 calls, 233 ledger rows`; `bun test tools/paid`: 2 tests pass (they run the 19 Python unit tests, and a smoke run of the viewer script on a fixture ledger); `bun run typecheck` clean; refusal runs of `briefs/052/gen.py`, `tools/figures/portraits_gen.py`, `sheets_gen.py` with no budget or a closed pass stop before any request (ledger line counts unchanged).

## Structure
- `paid/` (git-ignored, `/paid/` in `.gitignore`). One folder per job `paid/<brief>-<slug>/`:
  - media by kind (`keyframes/`, `shots/`, `audio/`, `edits/`, `takes/`, `picks/`, `fixes/`, `fix/`, `sheets/`);
  - `ledger.jsonl`, append-only, one line per call: `id, at, at_source, service, model, task (image|video|audio; edit|derived|note are bookkeeping), pass, status (ok|blocked|failed), cost_usd, cost_est, file, sha256, bytes, prompt, refs, seed, size, duration_s, params, used_in, note, task_uuid`; rows indexed in place carry `ref {where, path}` instead of `file`;
  - `passes.json`: the budget of each pass (`budget_usd`; `null` = closed; `prior_budget_usd` keeps an old ceiling for the record).
- `tools/paid/spend.json` (committed, no media): totals per job, pass, model and day, calls, failed and blocked, used assets, cost per used asset. Jobs whose ledger is not on the machine keep their committed summary, so history survives a fresh clone.
- `paid/index.html` and `paid/.thumbs/` (generated, ignored).
- Jobs now: `007-concepts`, `020-people-sheets`, `033-portraits` (indexed in place), `047-loading-art`, `052-teaser` (media moved).

## The helper `tools/paid/paid.py` (Python; the gen scripts are Python)
- `p = paid.Pass(job, pass, budget=USD)`; then `p.image(...)`, `p.video(task, ...)`, `p.audio(task, ...)`. Everything else about a call (submit, poll, fetch, save, ledger line, rollup) is inside.
- Budget: a pass has an explicit budget or it refuses to run (`BudgetError`, a SystemExit). The budget is set once with `--budget USD` (or `python3 -I tools/paid/paid.py open <job> <pass> --budget USD`) and remembered in `passes.json`. Raising it, or reopening a closed pass, needs `--raise` and Sky's OK; a new pass name needs neither. It prints the estimate and the budget left before the first call of a run, and stops when spent + in-flight (other processes' reservations, dead PIDs ignored) + estimate would pass the budget. A call with no reported cost counts its estimate. A fetch failure after the provider charged is logged with the cost and the result URL.
- Key: loaded at runtime by the old repo's client (`workbench/Tools/Concept/runware.py`, `.env.local`); never copied or printed here. Tests use a fake client.
- A repeated take name never overwrites paid output (the new one gets `__r2`). `link=` makes a relative symlink where a consumer reads (033 portraits export, the 020 store sheets) and refuses, before any spend, to replace a real file.
- CLI: `open`, `status [job]`, `refresh`, `view` (= `bun run paid`).
- Calling form now: `python3 -I briefs/052/gen.py img|vid|music ... --pass N [--budget USD]`; `tools/figures/portraits_gen.py <who> [--pass NAME] --budget USD`; `tools/figures/sheets_gen.py turn <who> --pass NAME --budget USD`. 047 is finished: `briefs/047/gen.py` and `fixes/fix*.py` still call the old client directly and must not be run again (data migrated only).

## Migration (what moved)
- 047: `takes/`, `picks/`, `fix/` (directory symlinks) and the 40 pictures of `fixes/` (file symlinks; the three `fix*.py` stay) into `paid/047-loading-art/`.
- 052: `keyframes/`, `shots/`, `audio/` and the four `teaser-*.mp4` into `paid/052-teaser/` (`edits/`), symlinks left at every old path (relative, so a moved repo keeps them).
- Checked first: no `gen.py`/`edit*.py` process was running (read-only `ps`); nothing killed.
- PROVENANCE rows became ledger rows. Dates: the file's modification time (kept by the move), marked `at_source: mtime`; rows with no file (refused, unreturned, notes) inherit the previous row's time, marked `inferred`. 052 passes follow the edit rows (each `edit*.py` closes a pass; explicit `pass` fields win): they reproduce the CAP comments in `gen.py` ($14.60 against "$14.61", pass 5 $4.54 on top of "43.65 before"). All migrated passes are closed (`budget_usd: null`).
- Used: 052 = every clip in an `edit*.py` cut (plus the title keyframe; the keyframe a used clip grew from carries a lighter `via <clip>` badge, not counted as used); 047 = the picks and alts, the fix calls in the `fix/FINAL.json` chains, and the four `fix/*_after.png` plus `A_fixed.png` as derived rows ($0). 020 = the committed sheets `tools/figures` reads. 007 and 033: nothing marked used (no record says which); open for Sky or Forma.
- Indexed in place, not moved: 007 concepts (the PNGs live in the old store `~/Projects/the-suborned-store/concepts/`, only the contact sheets are in the repo), 020 sheets and 033 portraits (the repo copies are git-ignored; `ref.where` says `repo-committed`, `repo-local` or `store`; the viewer labels each).
- `briefs/052/gallery.py` and `gallery.html` are retired (moved to `out/scratch/splice/`); the viewer keeps its ideas (finished films, cards grouped by pass, used outlined with which cut, prompt collapsed, lazy loading, click for full size).
- Left alone: the old `PROVENANCE.json` files (frozen record) and the contact sheets.

## The viewer (`bun run paid` -> `paid/index.html`, opened by hand)
Top: spent all time, calls, failed/blocked, used assets, cost per used asset; tables per job, per pass (with the budget), per model, per day. Then every row by job and pass: thumbnail (640 px JPEG cache, made once), clip with poster, audio player, cost, model, time, status, `used in ...` badge, where it sits, collapsed prompt, refs, sha256; rejects and unused shown (dimmed). Filters by job, model, status, used/unused; sort by time or cost; text search. Media load by relative path, `loading=lazy`, `preload=none`; the page is 0.5 MB and 1.4 GB of media is read only on click. Runware balance: Runware documents no balance or usage endpoint (its help centre: the balance shows at the top right of the dashboard, usage under Usage), so the page says that and fetches nothing.

## Totals the viewer shows
Total **$58.95** in 222 calls (2 failed, 6 blocked, 45 used): 052 teaser $48.19 (111 calls), 047 loading art $6.85 (67), 007 $2.27, 033 $1.16, 020 $0.48 (the three together $3.91). This is $3.32 above the "$55.63" in the request because the 052 ledger sums to $48.19, not $44.87 (pass 1 14.60, 2 8.96, 3 5.54, 4 14.55, 5 4.54); the 111 calls match. Worth comparing with the Runware dashboard.

## Open
- 007 and 033 used-in marks; 007 PNGs are only in the old store.
- Old `briefs/047/gen.py` and `fixes/` still hold direct calls to the old client (finished; not to be run).
- Spend recorded only in the old store (`~/Projects/the-suborned-store/concepts/*`: Citizens, District, Horizon, Nature, Props, Cell's earlier rounds) and the `briefs/008-012` PROVENANCE files (empty or no costs) was not checked or included.

## Free stills through Codex (2026-10-23)
Sky: "should be using chatgpt cli imagegen as that's free". `paid.py codex <job> <pass> --name N.png --prompt ... [--ref IMG]...` runs `codex exec` (image generation, read-only sandbox, stdin closed) and takes only its own session's images from `~/.codex/generated_images/<session id>/`. They go into `paid/<job>/codex/` and are logged at $0 (service "Codex (ChatGPT plan)", model `codex:image_gen`), so free and paid output share one ledger and viewer. Stills go here first, and Runware is for video, music and 3D. Tests: `test_codex_image_*` in `test_paid.py`. First use: the Fixer's shortened original dress (`020-people-sheets/codex/fixer_original_short_codex_*.png`).
