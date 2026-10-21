# Brief 027 · Third retest (N1, N3, N4) and an adversarial pass (Suture)

**Date:** 2026-10-20. **Engine:** `suborned.sim/0.10.0`, inputs `f869a690f20e18c1`, rulebook sha256 `6a0b05ba…` (the tree of Keel's `fight-lab.md` §10).
**Asked by:** Aster, after Keel built N1/N3/N4 lab-only.
**Read-only:** nothing under `sim/`, `contracts/` or `unity/` was edited. The sweeps ran on a byte-identical copy of `sim/` (`out/suture027/r3`; `diff -rq` clean before my edits). The adversarial pass adds:
- two scripted-player flags to that copy;
- four env-gated engine probes, all off by default.

With every flag and probe off, the copy reproduces the real tree row for row (checked on 1,268 + 640 policies). Every script below replays on the real tree with `bun run sim play`.

## 0. Short answer

1. **Independent check: passes.** I reran my own 634 policies (L3 443, L1c 191) at Keel's two margins (default 1.0 / 0.5 m and `tight` 0.3 / 0.2 m) with `fight-lab`.
   - **lab: 0 FAIL wins at either margin.** Each margin has 12 wins, all heavy (61–66 % health lost, 1–3 down).
   - The matrix matches Keel's `matrix-retest2.txt` row for row. L3 wins heavy at 50 %; P-A 5 %, P-G 27 %, P-B 16 %.
   - 0 replay differences.
2. **Adversarial: N1 is not closed.** A band player still wins L3 in the plain `lab` preset at **0 hits, 0 %** (4:21.5).
   - It differs from the old one in two ways: it **ignores the ambush crew**, and it steps back **2–3 m, not 4 m**.
   - **58 of 3,792 policies** win cheap (< 40 %). 10 of them are at 0 %.
   - Two still win at Keel's own `tight` margins (5 % and 16 %).
3. **Cause (N7):** in the final fight, rule-3 cover is not an answer. A police body shot from beyond its reach takes cover against where the shooter stood. The shooter moves 2 m, the cover stops shielding it, and the body waits out the full `cover.stepGap` (4 s) before it re-plans. Then it takes another cover rather than closing in. It never fires. This is Keel's noted cadence weakness, and it is the one that matters.
4. **Fix candidate (probed):** when the body's position no longer shields it from the shooter, re-plan at `kite.react` (5 ticks), not at `cover.stepGap`.
   - **Effect: cheap lab wins 58 → 0. Matrix unchanged** (L3 50 %, P-A 5 %, P-G 27 %, P-B 16 %, lab-cover L3 66 %). 0 replay differences.
   - The other three probes don't close N7, and one breaks lab-cover L3. Releasing the held cover changes almost nothing (§2).

| # | finding | severity | owner |
|---|---|---|---|
| **N7** | Final-fight police take cover against a band shooter's *old* position and wait 4 s; the band (15–16 m) is still free. L3 wins at 0 hits | **critical** (lab default preset) | Pulse (cadence rule) → Motive (rule 3 text) → Keel |
| N8 | The matrix has no player that ignores the ambush crew or steps less than 4 m, so N7 is invisible to `fight-lab` (the N3 gap again) | high (process) | Keel |
| N9 | A held cover point is not released when the body closes in (Keel's note). It only blocks the partner's choice. Effect: 58 → 55 cheap wins | low | Pulse |
| N2 | `lab-field-on` (open from retest2): with the N7 fix, 5 wins stay at 27–33 % with 7–15 hits; the field hides the damage | medium (preset off) | Motive (measure) |
| N10 | 6.6.2 free-downs triggers fire on failing rows: `clear`+`runkite` (the Enforcer downs 2 police before they fire) and `enfgo-units` (the Tech downs 2–4 units). None of these rows wins | info | Motive (read only) |

## 1. Independent check (the 634 policies at both margins)
Command: `dotnet <copy>/suborned-sim.dll fight-lab --presets <p> --rulebook <snapshot> --runs <80 policies>`, in 8 parallel chunks. With `--runs` it writes nothing. The policies are my retest2 sweep3 set; for `tight` I put the prefix after the run, as in `L3:tight-…`.

| preset | margin | wins | FAIL wins | cheapest win | replay |
|---|---|---|---|---|---|
| lab | 1.0 / 0.5 m | 12 | **0** | `L3:enfgo` 61 %, 2 down | 0 differ |
| lab | 0.3 / 0.2 m | 12 | **0** | `L3:tight-enfgo` 61 % | 0 differ |
| lab-field-on | 1.0 / 0.5 m | 8 | 2 | `L3:carfree` 66 %, 3 captured (6.4.8a only) | 0 differ |
| lab-field-on | 0.3 / 0.2 m | 16 | 10 | 66 % (`L3:tight-carfree`); 8 × `L1c:tight-focus-carfree-*runkite` at 94 % (6.4.8a only) | 0 differ |

Every FAIL win under field-on is heavy and fails only 6.4.8a, which is retest2's note on how strictly 6.4.8a reads. My count agrees with Keel's 0 lab wins. Field-on differs from Keel's "0 wins" only by these heavy 6.4.8a rows, which come from my L1c set and flag orders.

- **Old N1 rows:** `L3:at208-runkite` now fails enforcer-down (4:05.4, 33 %) and `L3:tight-at208-runkite` fails at 55 %. The blocker (security-2 at cv-087) downs the Enforcer as he walks.
- **N4:** units move 5 ticks after the first shot (Keel's band micro). I didn't repeat it.

## 2. Adversarial pass

### New player flags (scratch harness, `probe/retest3/harness-step-norivals.diff`; all off unless named)
- `step<cm>`: the kite step size (default 400).
- `norivals`: fire only on police and security. The ambush crew is still a threat to step away from, but never a target.
- `blk`: the blocker first.
- `aware`: units count as threats even when they aren't targets.

### Sweep
3,792 L3 and L1c policies: margins (`tight`, `km5–30` × `fm5–20`) × step 60–400 × targeting × Enforcer (`enfgo`, `carfree`, `at196–236`, `hold`) × `runkite`/`kite`. List: `probe/retest3/policies.all.txt`.

**Result (lab, the real engine):** 192 wins, 106 FAIL, **58 cheap (< 40 %)**, 10 at 0 hits.
- 51 of the 58 need `norivals`.
- 41 use step 200.
- At Keel's exact `tight` margins: `L3:tight-step200-focus-norivals-at236-runkite` wins at 5 % (1 hit), and two walked `tight-step60-blk-at208-kite` rows win at 22 % with 1 down.

### Scripts
Ranked. They are in `briefs/027/suture/retest3-scripts/`, and each prints "replay identical" under `bun run sim play <file>` on the real tree.

| rank | script | result | T | hp / hits | final |
|---|---|---|---|---|---|
| 1 | `L3.km10-fm5-step200-norivals-at208-runkite.lab.script.json` | Success 4:21.5 | 12 | 0 % / 0 | `3ed775e567f92577` |
| 2 | `L3.km15-fm5-step300-norivals-enfgo-carfree-runkite.lab.script.json` (no hold: the Enforcer walks at once) | Success 4:37.4 | 14 | 0 % / 0 | `4fa4ffeefc89921c` |
| 3 | `L3.km20-fm10-step200-norivals-at220-runkite.lab.script.json` | Success 4:40.8 | 11 | 0 % / 0 | `cd2ee1b66d3150fb` |
| 4 | `L3.km30-fm15-step250-norivals-at220-runkite.lab.script.json` (0.3 m keep-out, as `tight`) | Success 4:49.0 | 9 | 5 % / 1 | `082db54e62481a63` |
| 5 | `L3.tight-step200-focus-norivals-at236-runkite.lab.script.json` (Keel's `tight` exactly) | Success 4:58.7 | 9 | 5 % / 1 | `e4bae57a4bce9e3b` |

The verdict catches every one of them: FAIL on casualty, and 6.4.1 fires ("wins with 0 hits taken"). The rows just aren't in the matrix (N8).

### What happens (rank 1)
Logs: `bun run sim play`. Engine trace from the env-gated `DBG=<id>` line in the scratch copy.

1. **3:14.1:** Vale comes out. The Enforcer stands until 3:28.
2. **3:14.3–3:18.5:** security-1 (joined at police-3) is shot from 15.0–16.1 m and goes down in 4 s. It has no cover and never fires.
3. **3:20.9–3:28.7:** the blocker, security-2 (cv-087), is shot from 15 m and never answers ("keeps its lane").
   - Then "car.free" at 3:28.7. That is all the police near the car, and it took no hits.
4. **3:40.7–4:03.2:** the officers arrive.
   - Each "is fired on … out of its reach: takes cover at cv-001/cv-097" (the car and a pier), then cv-003 4–5 s later, and so on.
   - Trace for officer-2: shooter distance 15.1–16.7 m, `prot False` for most ticks, gap counting to 40 before each re-plan. Each re-plan picks cover again (FightCover finds one), so it never closes and never fires.
   - Both officers go down. `fight.over` comes at 4:03.3, and the crew boards at 4:21.5 with full health.

### Why
- §7.2 rule 3 ranks cover above closing. `FightCover` nearly always finds a point within 8 m near the car.
- A cover point shields against one shooter position. The runner's 2 m step breaks the shield.
- The body may not re-plan for `cover.stepGap` (4 s). For about 4 of every 4.5 s it stands unshielded, can't answer and doesn't move.
- The old 4 m step overshot the 1 m band (15–16 m), so the player had to walk back; 2–3 m keeps it in band. `norivals` stops the player chasing the hidden ambush crew into the officers' reach.

### Probes
Env-gated, scratch only (`probe/retest3/probe-replan-close-answermax.diff`). Run on the same 3,792 policies plus the matrix (lab, lab-cover, lab-field-on).

| probe | cheap lab wins (< 40 %) | FAIL wins | matrix (lab L3 · P-A/P-G/P-B · lab-cover L3) |
|---|---|---|---|
| as built | **58** (10 at 0 %) | 106 | 50 % heavy · 5/27/16 % · 66 % heavy |
| **`PROBE_REPLAN`**: unshielded → re-plan at kite.react (5 ticks) | **0** | 40, all ≥ 61 %, all 6.4.8a-only | **identical** |
| `PROBE_CLOSE`: skip cover, always close (retest2 FB) | 6 (min 5 %) | 62 | L3 66 %; **lab-cover L3 fails enforcer-down 100 %** |
| `PROBE_ANSWERMAX`: a body answers its shooter out to its own max | 33 (min 11 %) | 97 | identical |
| `PROBE_RELEASE`: release the held cover on close-in (N9) | 55 | 103 | identical |

- **lab-field-on:** with `PROBE_REPLAN`, cheap wins fall from 96 to 5 (27–33 %, 7–15 hits). That is N2's measurement question, not a free line.
- **today:** not run (Sky: stay in lab). The probes sit behind the lab kite switch.

## 3. Handoffs (inputs · done-check · next owner)
1. **Pulse.**
   - **Inputs:** N7 (rank-1 log and trace, `PROBE_REPLAN` as one answer: re-plan as soon as the position stops shielding, rate-limited by kite.react), N9.
   - **Done-check:** a ruling on the cadence in `pulse/`.
   - **Next owner:** Motive, if rule 3's wording changes ("cover against the shooter" means against where he stands now). Then Keel.
2. **Keel.**
   - **Inputs:** build the cadence (lab-gated) and port the harness flags `step<cm>`, `norivals`, `blk`, `aware` (diff above). Add rows `L3:km10-fm5-step200-norivals-at208-runkite`, `L3:km15-fm5-step300-norivals-enfgo-carfree-runkite`, `L3:tight-step200-focus-norivals-at236-runkite` (N8).
   - **Done-check:**
     - `fight-lab` shows no lab FAIL win;
     - the matrix is unchanged;
     - today 228 of 228 identical;
     - `dotnet test` green.
   - **Next owner:** Suture, to rerun `policies.all.txt` (3,792) on lab and field-on.
3. **Motive:** N2 (field measure, still open), N10 (read only).

## 4. Evidence and how to repeat
- **Matrix:** `evidence/retest3-matrix-lab.txt`. Its `lab` rows are identical to `keel/matrix-retest2.txt`.
- **634 at both margins:** `evidence/retest3-sweep-{lab,field}-{default,tight}.txt`.
- **Adversarial and probes:** `evidence/retest3-adv-<probe>.{lab.wins,matrix}.txt` (success rows only, headed by policy count and replay line) and `retest3-adv-{none,PROBE_REPLAN}.field.wins.txt`.
- **Repeat:**
  1. Copy `sim/Suborned.Sim` and `sim/cli` to `out/suture027/r3`.
  2. Apply both diffs in `probe/retest3/`, then `dotnet build -c Release` in `cli`.
  3. Run `probe/retest3/sweep.sh <policies> <preset> <out>` or `probe.sh <none|PROBE_…>`. Keep the rulebook snapshot at `out/suture027/r3/rulebook.snapshot.json` (sha256 `6a0b05ba…`).
- **Replays:** all 5 scripts give Success with "replay identical" under `bun run sim play` on the real tree. Every sweep chunk printed "every run replays identically from its order log" (48 of 48 chunks per probe).
