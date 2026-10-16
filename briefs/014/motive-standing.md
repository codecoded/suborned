# 014 · Standing: Hero by default, kindness spreads (Motive, 2026-10-16)

**Sky's rulings (2026-10-16), via Aster:**
- Turning conversations are private (Keel's `privacy` section). That left nothing moving the city toward Hero.
- **"If you help in a good way then can move to hero":** the person you helped tells their ties.
- **"Hero is kinda the default, if you're not seen then everyone assumes you're good":** only bad acts that are seen or found move the city toward Menace.

This is Keel-ready. All values are proposed, tag S. Nothing in the rulebook is edited.

## Rules

| # | rule | values |
|---|---|---|
| 1 | **Every district starts leaning Hero**: one step short of the Hero band. | `standing-start` **2** (was 0). `hero-at` 3 and `menace-at` −3 are unchanged. |
| 2 | **Kindness spreads.** Between missions, each person the cell **Helped** this mission, and did not Pressure or take down, tells their ties that a stranger helped them. The district moves **+1 per helped person**, not per tie. | `word-standing-step` 1; `word-standing-cap` **±2** per district per mission |
| 3 | **Harm spreads, the same way** (yes, symmetric). Each person the cell **Pressured**, or **knocked out** and who came home, tells their ties: **−1 per person**. A person both helped and pressured counts as pressured: a threat outweighs a kindness. | the same step and cap |
| 4 | **Seen or found bad acts move Standing at once**, as built: Pressure seen, violence seen, a drawn weapon seen and gunfire seen are −1 per incident. A found killing is −1 (rev 4). A missing person is 0. | unchanged, uncapped |
| 5 | **Standing drifts back to the default.** A district where Standing didn't move this mission (no act seen or found, no word) moves **1 step toward `standing-start`**, from either side. | `standing-drift` **1** per mission, never past the start |
| 6 | **Heat is unchanged:** Trail + max(0, −Standing). Starting at +2, it takes 3 net Menace steps before Standing adds any Heat. Hero never cancels Trail, because evidence is evidence. | — |

**Who tells.** The word goes to anyone on the tie, whatever their loyalty. It is **a feeling, not a fact**: no face, no place or time, nothing an investigator can use, and nothing about the cell is passed on. So the obliged-keeps-quiet rule (`word-quiet-loyalty`), which hides facts, doesn't stop it. The facts themselves still travel only as `word.passes` and the Talker test say. A helped person who is a Talker can still give the Fixer's face to investigators, as now (Sky's "could later reach investigators").

**Who doesn't.** The dead, a missing person, a person who has no family, lover or workmate tie, and a contact who was sealed by the Fixer's Quiet.

## The Handover, debrief only

The word moves between missions, so no in-mission price changes. The capstone's tested prices hold.

| route | Standing after (start +2) | next mission |
|---|---|---|
| A, Help ×2 | Inez tells: **+3, Hero band** | Hero prices (Mercenaries overcharge, Idealists volunteer) |
| A, Pressure | the threat seen −1 (if witnessed) + Inez's word −1 = **0** | neutral |
| B, Pay | Pay is private, so nothing moves; it drifts at **+2** | neutral, leaning Hero |
| G, knockout | the guard comes home and tells: **+1** | neutral |
| G, kill (hidden) | missing: **+2** | neutral; the case is open |
| loud | 2 to 3 seen incidents at −1 each (gunfire, drawn weapons): about **0 to −1** | neutral; Standing starts adding to Heat |

**Debrief: one line per district.** For example: "Harbour: Hero +3 ▲1 (Inez told her family a stranger cleared her debt)."

## Rulebook entries (proposed)
- **`values`:**
  - `standing-start` 2 (src: Sky 2026-10-16, "Hero is kinda the default");
  - `word-standing-step` 1 and `word-standing-cap` 2 (src: Sky 2026-10-16, "if you help in a good way then can move to hero");
  - `standing-drift` 1 (src: the same ruling, as "the benefit of the doubt"; reason: what the city stops seeing, it forgives).
- **New event `standing.word`:**
  - phase between missions, after `word.passes`;
  - needs: a simulated person, the subject of a Help (+) or of a Pressure or knockout (−) this mission, alive and not missing, with at least one family, lover or workmate tie, not sealed;
  - changes: Standing ±step for the act's district, capped per district per mission.
- **New event `standing.drift`:** phase between missions, after `standing.word`. It acts only on a district with no Standing change this mission.
- **`standing.rule`:** replace "news along ties never does (Sky Q6)" with "the word of a helped, pressured or assaulted person does, once per person (`standing.word`); facts along ties still never do".

## GDD changes (§04, §08)
- **§04 standing:**
  - starts leaning Hero;
  - kindness and harm spread one hop through the person they were done to;
  - the city forgives over time.
  - Replace "unwitnessed threat: no change (victim still knows)" with "an unwitnessed threat moves Standing only through the victim's word".
- **§08 debrief:** the Standing line with its cause.

## For Sky
1. **Start at +2, not inside the Hero band.** It reads "Hero is the default" as "the city leans Hero, and one known kindness earns the band". Starting at +3 would put the Handover in Hero prices from the first second and change its tested costs.
2. **Drift is symmetric.** Unkept Hero fades back to +2, as Menace does. Hero has to be kept by doing good, and a Menace cell isn't stuck.

## A public fight (Sky 2026-10-16: "it should be per fight, and depends who you're fighting")

This replaces the per-target `seen-gunfire` moves that engine 0.6.1 builds.

| # | rule | values |
|---|---|---|
| 1 | **A fight is one episode.** The final fight is one fight, from its start to its end. Outside it, a fight starts at the first shot by or at the cell and ends `crowd-quiet-s` 30 s after the last one: the crowd's own "it's over". If gunfire runs into the final fight with no 30 s gap, the two are one fight. | reuses `crowd-quiet-s` 30 |
| 2 | **It is public** if at least one simulated person who isn't in the cell is within `crowd-scatter-m` 30 m of any shot of the fight, whether they hear it or see it. A public fight moves Standing **once, when it ends.** One that nobody heard moves nothing. | reuses `crowd-scatter-m` 30 |
| 3 | **The sign depends on who the cell fired at:** see the outcomes table below. | `fight-standing-step` 1 |
| 4 | **The rivals' standing in the public eye is data, per mission:** the rival role in `mission.json` carries `public: "menace" \| "neutral"`. The default is `"menace"`, because a crew that takes people off the street is feared. A district-wide value can come later. | the Handover: `"menace"` |
| 5 | **This is a seen act, not word of mouth.** It sits outside the ±2 word cap. It counts as a move for drift, so that district doesn't drift that mission. Shooting a civilian is unchanged (`seen-violence`), on top of the fight's move. | — |

**Outcomes, at most one per fight.** The first row that applies wins:

| cause id | when | move |
|---|---|---|
| `seen-fight-security` | the cell fired on the guard, the patrol, officers or robots: authority, whoever else was there | **−1** |
| `seen-fight-first` | the cell fired first on rivals the public doesn't fear | **−1** |
| `seen-fight-rivals` | the cell fired on rivals the district fears, and they broke or were all down when the fight ended (the cell drove off a menace) | **+1** |
| — | anything else: rivals not feared who fired first, a fight with feared rivals that the cell didn't win, a withdrawal, a fight nobody heard | **0** |

**src:** Sky 2026-10-16 (via Aster). **reason:** the public judges a fight by its enemy, once per fight. Authority outranks everything else, because a firefight with the police never reads as good.

**The Handover** (start +2; the rivals `menace`; the square is within 30 m of the getaway car, so every final fight is public):

| route | the fight's move | Standing after, with the word |
|---|---|---|
| A, Help ×2 (safe fight, the rivals break) | +1 `seen-fight-rivals` | 2 + 1 Inez's word + 1 = **4, Hero** |
| A, Pressure | +1 | 2 − 1 the threat seen − 1 Inez's word + 1 = **1** |
| B, Pay | +1 | **3, Hero** |
| G, knockout | +1 | 2 − 1 the guard's word + 1 = **2** |
| loud (the guard shot, then police at the car) | −1 per fight: the guard episode, and the police fight if a 30 s gap separates them | **0 to +1** |
| fight-safe-rivals-reach-vale, fight-loud-enforcer-down | a failed mission still moves Standing: rivals not broken = 0 / police = −1 | 2 / 1 |
