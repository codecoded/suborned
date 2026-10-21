# Brief 027 · Second retest (R1–R6) and the next cheapest win (Suture)

**Date:** 2026-10-20. **Engine:** `suborned.sim/0.10.0`, inputs `79455564cfb30029`, rulebook sha256 `0d30561b…` (the tree of Keel's `fight-lab.md` §9).
**Asked by:** Aster, after Keel built the R1–R6 fixes (Pulse `retest-fixes.md`, Motive `tuning.md` §7; Aster: Pulse's one-officer contest stands; units are a note under 6.4.8a).
**Read-only:** nothing under `sim/`, `contracts/` or `unity/` was edited. Probes ran in a scratch copy (`out/suture027/src4`). Its engine is byte-identical to `sim/` except two files:
- the scripted player, `ScenariosFightLabAdv.cs`: new policy flags, and band margins read from the environment;
- one env-gated probe in `GameKite.cs`, off unless `FB=1`.

Every script below also replays on the real tree with `bun run sim play`.

## 0. Short answer

Keel's fixes do what they were built for. R1's rows fail as scripted, R2's stall is gone, and R6 passes. **But loud L3 still has a free win in the plain `lab` preset:**

1. **Wait, then walk.** The three run-kite. The Enforcer holds Vale where he comes out for 6–42 s, then walks to the car.
   - Win at 4:39.3, **0 hits, 0 %**, 13 T, A 2, Standing 2 → 0.
   - 48 of 634 policies.
2. **A tighter band player reopens R1 itself.** Keel's rows step away at the hostile ring + 1.0 m and fire inside max − 0.5 m. With 0.3 m and 0.2 m instead:
   - `L3:focus-enfgo-carfree-runkite` wins at 4:31.4 (5 %, 1 hit);
   - `L3:at196-runkite` wins at 4:21.2 (0 %);
   - 79 of 634 policies.

**Cause:** in the final fight the band (15–18 m) is free.
- Motive's rule 3 sends a police body that is shot from beyond its reach to cover, with no fallback.
- The contesting officer stands in the open 13 m from the car. It is shot down from 16 m without answering, and so are the other three bodies.

**What closes it:**
- **Motive's own fallback, `lab-sidearm-max-m` 18 → 16 (tuning.md §6.6), closes every cheap row I have** (0 of 634 at both tight margins). Matrix L3 stays a heavy win (50 %), and P-A 5 %, P-G 27 % and P-B 16 % all pass.
- An AI fallback alone (no cover → close to the shooter's ring) closes the 1 m player but not the tight runner.

| # | finding | severity | owner |
|---|---|---|---|
| **N1** | **The band is free in the final fight (lab, default preset):** wait-then-walk at 0 hits; at a tight band margin R1's own row wins at 5 % | **critical** | Motive (trigger §6.6's 16 m fallback; rule 3's no-cover fallback) → Keel; Pulse for the AI part |
| N2 | **lab-field-on (Keel's open note):** 122 of 634 policies win and fail the verdict. Under the AI fallback, 26 walked-kite wins stay at 16 % with 7 hits: the field absorbs hits, so "health lost" undercounts | high (preset off by default) | Motive (measure) → Keel |
| N3 | **R5 again:** the matrix has no "hold Vale, then go" row and no tight-margin player, so N1 is invisible to `bun run sim fight-lab` | high (process) | Keel |
| N4 | R3 is still open in the final fight: police-1, shot from 17.4 m, holds still and doesn't fire for 7.6 s and 2 hits | medium | Pulse → Keel |
| N5 | R4 only half closed: the crew's advance never catches a runner (it "walks on" at 16–17 m, takes 3 hits, retreats). Its second answer comes after 50 ticks, not 5 | medium | Pulse |
| N6 | `today` (FYI; Sky: "stay in lab"): 69 of 634 policies win and fail the verdict, 16 of them at 0 hits. The simplest needs only `enfgo-runkite` (4:33.5) | info | Aster (for Sky) |

## 1. Retest of R1–R6

| R | done-check | now (lab) | call |
|---|---|---|---|
| R1 cheap loud win | both run-kite rows fail or win heavy | As scripted (1 m / 0.5 m margins): `focus-enfgo-carfree-runkite` fails enforcer-down 4:02.9, 33 %; `enfgo-carfree-runkite` fails 3:51.5, 33 %. At 0.3 m / 0.2 m the first **wins 4:31.4, 5 %, 1 hit** | **closed only for the scripted margins; reopened by N1** |
| R2 chase budget | no body frozen with a sub-cell remainder | Trace (`TRACE=officer-1,officer-2,security-1,security-2`): the officers walk in free (`walked 0 cm` from 83 m to 30 m); nothing freezes; final-fight bodies use rule 3 cover, not the chase | **closed** |
| R3 brief-013 units | return fire or movement ≤ 1.0 s | In L3, units only appear in the final fight. Band micro (`BAND=police-1@2290@fixer`): police-1 doesn't move (17.4 m throughout), fires no shot, and is hit at 4:10.1 and 4:14.1; the Fixer keeps 40/40 → N4 | **open in the final fight** |
| R4 pre-fight crew | answer in 5 ticks; no lean-out into the shooter | First answer in 5 ticks ✓ ("to their ring of the shooter"). Against a runner it "walks on, 16–17 m out" five times and never closes. The second engagement (lean-out at 3:02.0, shot at 3:03.6) is answered at 3:08.6, 50 ticks → N5 | **half closed** |
| R5 rows + verdict | rows in `fight-lab`; a verdict line for contact | The three rows are in the matrix and fail. Counters reset. 6.4.8a/b are in the verdict and catch every N1 row ("no op down…; 6.4.8b no hostile shot at the cell from Vale coming out") | **closed; new gap N3** |
| R6 lab-cover P-G | ≤ 33 %, no op down | lab-cover P-G 22 %, 0 down (matrix). The old open-loop `P-G.lab-cover` script still fails, because its orders were recorded on the old inputs; use the closed-loop row | **closed** |

**Matrix:** my `fight-lab --rulebook <snapshot>` rerun (writes nothing) matches Keel's `matrix-retest.txt` row for row (`evidence/retest2-matrix.txt`; `diff` shows only the "wrote" line).

## 2. Findings

### N1 · The band is free in the final fight (critical, lab)
**Scripts** (replay on the real tree, `bun run sim play briefs/027/suture/retest2-scripts/<file>`; each prints "replay identical"):

| script | result | T | final |
|---|---|---|---|
| `L3.at208-runkite.lab.script.json` | Success 4:39.3 | 13 | `728e2f340c5c8374` |
| `L3.focus-at208-runkite.lab.script.json` | Success 4:39.2 | 13 | `0c0e4827e7cc06dd` |
| `L3.at236-runkite.lab.script.json` | Success 5:03.5 (the window's late edge) | 13 | `3f7d3deee76a76e2` |
| `L3.at196-runkite.tight.lab.script.json` | Success 4:21.2, 0 hits | 15 | `81267566d9447f51` |
| `L3.focus-enfgo-carfree-runkite.tight.lab.script.json` | Success 4:31.4, 1 hit (R1's row) | 15 | `3b4a3bf12b7f698b` |

**The orders** (harness `probe/retest2/harness-at-hold.diff`):
- L3 as the matrix plays it: route G-fast, the Enforcer shoots the guard, then Enter.
- The three keep out of every armed hostile's effective ring + `KMARGIN` (default 1 m), step away as **runs**, and fire when they stand inside max − `FMARGIN` (default 0.5 m). This is Keel's `runkite`.
- `at<s>`: when Vale comes out (3:14.1), the Enforcer **stands still** until second `s`, then walks Vale to the car without stopping.
  - At the default margins, `s` = 208–236 (3:28–3:56) wins at 0 hits; 200 and 204 win at 16 % and 5 %.
  - At `KMARGIN=30 FMARGIN=20`, `s` = 196 already wins at 0 hits, as does R1's straight walk at 5 %.

| row (lab) | result | end | T | St | down | hp | hits | foes | verdict |
|---|---|---|---|---|---|---|---|---|---|
| matrix L3 | win heavy | 4:50.3 | 22 | 2 → −2 | 1 | 38 % | 7 | 5 | pass |
| `L3:at208-runkite` | **win** | **4:39.3** | 13 | 2 → 0 | **0** | **0 %** | **0** | 7 | **FAIL** (casualty, 6.4.8b) |
| `L3:at208-kite` (walked steps) | fail sealed | 5:05 | 18 | | 1 | 22 % | 4 | 7 | pass |
| `L3:focus-enfgo-carfree-runkite`, tight | **win** | 4:31.4 | 15 | 2 → 0 | 0 | 5 % | 1 | 6 | FAIL |
| `L3:at196-runkite`, tight | **win** | **4:21.2** | 15 | 2 → 0 | 0 | 0 % | 0 | 7 | FAIL |

The Trail, Alert and Standing lines of §7.1's price table are paid (≥ 12 T, A 2, ≤ start − 2). The fight and contact lines aren't. Free/side reads 3/3 to 4/3, against matrix L3's 1/1.

**What happens** (`LOG=L3:at208-runkite`):
1. **2:34–3:28:** the Tech drives the crew back twice from 16–17 m. The crew "walks on toward tech" but never reaches its ring of a runner (N5).
2. **3:14.1:** Vale comes out. Officer-1 "comes for the car: its stand is 13 m from it". Security-1 joins at police-3, and security-2 is the blocker at cv-087.
3. **3:20–3:34:** the Tech kills the blocker from 16 m. The blocker doesn't answer, by rule: "the blocker keeps its lane".
4. **3:50–3:57:** the Scout and Fixer kill security-1 from 16 m. It has been in cover at cv-093 since 3:20.9 and never answers from it.
5. **4:06–4:11:** the three kill officer-1 from 16 m. It logs no kite line at all.
   - The engine trace (`DBG`, scratch only) shows `canAnswer False`, `prot False` and `FightCover` = none on every step.
   - No cover within `ai.seek.police` is nearer the car than its 13 m stand, so `KiteFightCover` returns and the officer stands in the open.
6. **4:11–4:18:** officer-2 at cv-043 dies the same way. `fight.over` comes at 4:18.0, and the Enforcer, walking since 3:28, boards with nobody up.

**Why:**
- Motive §7.2 rule 3 ("shot from out of reach it takes cover against the shooter… It does not turn to chase") replaced the pre-fight chase with cover. Where no cover qualifies, nothing replaces it.
- Pulse's contest pulls one officer *closer to the car*, which is where cover runs out.
- Rule 1 (police go for the Vale holder) wasn't built (Aster's ruling). So a Vale holder who waits out of reach is never approached.
- Motive §7.3's premise, "outrunning them buys nothing at the car", holds only if the police come for Vale. Here the runners clear the street first.
- The deeper cause: a 3 m band (15–18 m) is a safe firing zone for any runner while the hostiles only fire inside 15 m.

**Probes** (lab, the same 634 policies; `evidence/retest2-sweep3-*`, `retest2-sweep4-*`):

| probe | player margins | FAIL wins / 634 | cheapest | matrix L3 | P-A / P-G / P-B |
|---|---|---|---|---|---|
| lab as built | 1 m / 0.5 m | **48** (all `at*-runkite`) | 0 %, 0 hits | win heavy 38 %, 1 down | 0 / 22 / 16 % |
| lab as built | **0.3 / 0.2 m** | **79** (incl. R1's rows) | 0 %, 0 hits | 38 % | 0 / 22 / 16 % |
| `lab-police-contest-n` 2 (rulebook copy) | 1 / 0.5 | at208 still wins | 5 %, 1 hit | 33 % | 0 / 22 / 16 % |
| `lab-police-contest-n` 0 | 1 / 0.5 | at208 still wins | 0 % | 38 % | same |
| **FB**: no rule-3 cover → advance to its ring of the shooter (`probe-fb-nocover-fallback.diff`, `FB=1`) | 1 / 0.5 | **0** (28 wins, all heavy) | — | 38 % | 0 / 22 / 16 % (identical) |
| FB | 0.3 / 0.2 | **26** (`at*-runkite`) | 0 %, 0 hits | 38 % | same |
| **`lab-sidearm-max-m` 16** (§6.6 fallback) | 0.3 / 0.2 | **0** (12 wins, all 61–66 % heavy) | — | **win heavy 50 %, 2 down, 1 captured** | **5 / 27 / 16 %**, no op down |
| `lab-sidearm-max-m` 16 | 0.1 / 0.05 | **0** (12 wins, all heavy) | — | 50 % | same |

At max 16 with tight margins, the run-kite rows fail enforcer-down at 55 %. The band player fires 9–18 shots and downs one hostile. L1c, L2 and L4 still fail sealed.

**Owners:**
- **Motive:** trigger the §6.6 fallback, `lab-sidearm-max-m` 16. Every condition you set for it holds: kite rows fail, matrix L3 stays heavy, P-G is 27 % ≤ 33 % with no op down, and P-A and P-B are ≤ 25 %. Also decide what a final-fight police body does when shot from beyond reach with no qualifying cover: FB, or rule 1's advance on the Vale holder. FB alone is not enough against a tight runner, but without it a contesting officer is a still target. And reread §7.3 ("never a foot race") with N1 in hand.
- **Pulse:** the FB motion (re-plan, stop point, keeping the car in reach while it closes), and the same for units (N4).
- **Keel:** build both lab-gated, then the N3 rows.

### N2 · lab-field-on (high; the preset is off by default)
**Scripts** (all replay identically):
- `L3.at208-kite.lab-field-on.script.json`: win 4:28.9, 18 T, 7 hits, 16 %;
- `L3.kite.lab-field-on.script.json`: win 5:03.8, 16 %, Keel's row;
- `L3.enfgo-carfree-kite.lab-field-on.script.json`: win 4:31.1, 22 %, Keel's row.

**Counts:**
- 122 of 634 policies win and fail the verdict. The cheapest are N1's rows, which the field doesn't change.
- With FB on, 26 still win, every one a **walked** kite (`at*-kite`, `kite`, `enfgo-kite`, `hold*-kite`) at **16 %, 7 hits**.
- With max 16 (tight margins, field on), 8 win and 2 fail the verdict. Both are `L3:carfree` at 66 %, 3 captured, failing only 6.4.8a: officer-2 never had the Vale holder in reach. That is not cheap; it is a note on how strict 6.4.8a reads.

**Why:**
- Pre-fight, the response unit closes on the walking Tech (the kite works) and hits him six times, 3:08–3:13.
- The scripted player raises the field below half health. It absorbs 30, so the Tech reaches the final fight alive.
- §6.4.1 and §3.1 read **health lost**, which the field hides: 7 hits × 10 = 70, recorded as 16 %.

**Owner:** Motive. Should the casualty line count damage taken, including absorbed damage (or hits), when the field is on? Then Keel.

### N3 · The matrix still can't see the cheapest line (process, high)
The adversarial rows either walk the Enforcer at once (`enfgo`) or wait for `fight.over`. None of them:
- plays "hold Vale out of reach, then go", which is the obvious move for anyone watching the police drop;
- stands closer to the ring than 1 m.

**Keel:**
- add `L3:at208-runkite` and `L3:focus-at208-runkite` (both presets), `L3:at208-kite` (`lab-field-on`), and one tight-margin pass over the kite rows (`harness-at-hold.diff`: flags `at<s>`, `hold[30|90]`, `holdcar[20]`; env `KMARGIN`/`FMARGIN`; all off unless named, so existing rows don't change);
- print 6.6.2's free-downs trigger for those rows (they read 3/3 to 4/3).

### N4 · Units in the final fight (medium)
Micro: `BAND=police-1@2290@fixer` on `L3:focus-enfgo-carfree-runkite`, scratch probe. Police-1 is shot from 17.4 m from 4:06.5. Its distance never changes, it fires no shot, and it takes 2 hits by 4:14.1.

`KiteFightCover` takes a unit's limit as "no farther from the car than it stands", which is its own position, so nothing qualifies. The units aren't on the lane, so this costs the cell only Trail, but they are free targets.

**Owner:** Pulse (the same fallback as N1), then Keel.

### N5 · The pre-fight crew against a runner (medium)
- "Advance to the ring" fires in 5 ticks, then "walks on toward tech, 16–17 m out" every 2 s while the Tech runs 4 m steps. It never gets into its ring. It takes hits at 2:41.5 and 3:06.1, and retreats at 3:28.2.
- After its lean-out at 3:02.0, its answer to the 3:03.6 shot comes at 3:08.6 (50 ticks), failing §6.6.2's 1.0 s.
- Max 16 makes this mostly moot (a 1 m band).

**Owner:** Pulse.

### N6 · today (FYI, no change asked: Sky "stay in lab")
- 69 of 634 policies win and fail the verdict, 16 of them at 0 hits.
- The simplest is `L3:enfgo-runkite` (no carfree): 4:33.5, 17 T, 0 hits, 7 down. That is one flag fewer than R1's today line.
- `L3.enfgo-runkite.today.script.json` and `L3.at196-runkite.today.script.json` give the same orders (final `b339c8e7cb450056`), and both replay identically.

## 3. Handoffs (inputs · done-check · next owner)

1. **Motive.**
   - **Inputs:** N1 (trigger §6.6's 16 m; rule 3's no-cover fallback; §7.3), N2 (the casualty measure under the field).
   - **Done-check:** rulings in `tuning.md` §8.
   - **Next owner:** Keel (rulebook value), Pulse (motion).
2. **Pulse.**
   - **Inputs:** the N1 FB motion, N4, N5.
   - **Done-check:** the band micro on police-1 draws movement or fire within 1.0 s. A contesting officer with no cover answers a band shooter.
   - **Next owner:** Keel.
3. **Keel.**
   - **Inputs:** the value and the AI fallback, lab-gated; the N3 rows and the margin switch.
   - **Done-check:**
     - `bun run sim fight-lab` shows no lab FAIL win among the N3 rows at either margin;
     - matrix L3 wins heavy;
     - P-A and P-B ≤ 25 %, P-G ≤ 33 %, no op down;
     - `dotnet test` green;
     - `today` 228 of 228 identical.
   - **Next owner:** Suture, to rerun the 634-policy sweep at both margins.
4. **Aster** (FYI): N6, if Sky asks how cheap today's loud route is. Its pistol band is 15–25 m, so it is cheaper still.

## 4. Evidence and how to repeat
- **Matrix:** `dotnet out/suture027/src4/cli/bin/Release/net10.0/suborned-sim.dll fight-lab --rulebook out/suture027/rulebook.snapshot4.json` gives `evidence/retest2-matrix.txt`.
- **Sweeps:** `evidence/retest2-sweep*.txt`, 0 replay differences in every file.
  - `sweep`: 382 flag combinations (focus/enfgo/carfree/clear/units × kite modes) for L3 and L1c, on lab, `FIELD=1`, `KITE=0` and today.
  - `sweep2`: the hold variants.
  - `sweep3`: all of the above plus `at196–244` × kite/runkite/allkite-runkite × units (634), on lab, `FB=1`, field, field + FB and today.
  - `sweep4`: tight margins, on lab, lab + FB, max 16, max 16 + field, and max 16 at 0.1/0.05.
  - Rulebook copies for the probes: `out/suture027/rb5-*.json`.
- **Repeat:**
  1. Copy `sim/Suborned.Sim` and `sim/cli` to a scratch folder.
  2. Apply `probe/retest2/harness-at-hold.diff`, and optionally `probe-fb-nocover-fallback.diff` (`FB=1`; the `DBG=<id>` lines are trace only).
  3. Put `probe/retest2/Program.cs` and `probe.csproj` in `<scratch>/probe4`, then `dotnet build -c Release`.
  4. From the repo root: `RUNS=L3:at208-runkite PRESET=lab dotnet <probe.dll> contracts/rules/rulebook.json`.

  Switches: `KMARGIN`/`FMARGIN` (cm), `FIELD=1`, `CHARGE=0`, `KITE=0`, `SNAP=0`, `COVER=1`, `LOADOUT=tech=carbine`, `LOG=<id>` (`ALL=1`), `TRACEID/TRACE/FROM`, `BANDID/BAND`, `SCRIPTS=<dir>`.
- **zsh pitfall:** set each variable by itself. In zsh, `set -- $v` doesn't split words, and that voided my first margin and FB loops. Every number above comes from the corrected runs.
- **Scripts:** all 10 in `retest2-scripts/` give Success with "replay identical" under `bun run sim play` on the real tree.
