# 102 · The turned guard: when a Call is offered, the caller's own state, and what turning him should buy

Motive, 2026-11-09. A proposal. Nothing is edited but this file: no `sim/`, `contracts/rules/`, GDD, `text.json` or `unity/Assets`. Read at a4e047cff and Fable's 5792f2ede, with `rg` and `sed` only. Nothing was run, since Latch is building.

## Summary
1. **The offer law stands, with one wording change.** What the call *is* decides whether it is offered; what has *happened* decides whether it is shut. Keel's test, "lasts the whole mission", doesn't sort his own list: "came once" and "saw violence" also last the mission once they happen, and he rightly keeps them as shut ways.
2. **Before a Listen: confirmed.** One gap: today's lock words for the lover's state name the guard. That holds in the Handover only because its briefing already says it.
3. **Step 4: yes, as one law for whoever acts on a call.** Anyone who holds a memory of the cell's gunfire, violence or a drawn weapon won't act: the one called won't make the call, and the one who'd leave his post won't come. The way is shut, not absent, and silent: no phone scene and no line. That settles Fable's case where it starts.
4. **What turning the guard buys: four options for Sky. I recommend option 1, "he leaves his door when you call him".** It is the same visit to Inez, made from his end.

## 1. The offer law

### 1.1 The law, and where its line falls
**The law (Aster took Keel's (b)): a Call is offered only where it has a use under the rules.**
- Confirmed. It is the GDD's own rule for a way that would do nothing ("absent, not greyed": §03 Watch, §04 rival crew).
- Read from the same helper as the refusal, the offer can't drift from it.

**Change the test that sorts absent from shut:**
- **Absent: what the call *is*.** It is true from the first tick, and nothing the player or the clock does can change it:
  - no lover;
  - a retired role job (the driver's camera block, brief 059);
  - a visit the engine doesn't play. Today that is any pair where the posted guard isn't the one sent for; under option 1 below, any pair where neither keeps a post.
- **Shut, with its reason: what has *happened*.** Play or the clock made it: busy (down, held, fighting, already away), alarm, saw violence, came once, too far, off the street, and step 4's caller state (§1.4).
  - A shut way is news the player can use: what their acts, or the clock, did to these people.
  - It stays shut for the rest of the mission where its cause does: came once, a lover dead, a memory of violence. One shut row is the record of what they did.
- **An absent way has no news to give, and showing it would leak the secret.** A shut "Nobody comes" on the turned guard would tell a player who never listened that his lover keeps no post.
- **Why the wording matters:** "Only reasons that last the whole mission remove the offer" would also remove "came once" and "saw violence", which nobody wants.
- **The Watch is no exception.** A used Watch goes absent (§03) because a shut "Already watched" tells the player nothing they don't know.
- **The refusal order stays as Keel has it,** and it follows the same line: what the call is first (absent), then what has happened (shut). Among the shut reasons, the one called comes before the one sent for (§1.4).

### 1.2 The rulebook sentence (Keel, the summon rule: one sentence, one clause, one fix)
**After the refusal list in `summon.rule`:**
> "The Call is offered on a turned contact only where the rules can play its use (a lover the engine sends on a visit, or a role job); where they can't (no lover, a retired job, a visit the engine doesn't play) it is absent, never refused, and each refusal above comes from play or the clock and shows as a shut way with its reason (Motive, brief 102; GDD §03 'absent, not greyed')."

**Into the refusal list, for step 4 (§1.4):**
> "…or the contact called holds a memory of the cell's gunfire, violence or drawn weapon (the lover's test, read from the one called), checked before the lover's state."

**The fix:** replace the stale last sentence, "A contact's role job (the driver's camera block) is the call's other use.", with "No role job takes a call today: the driver's camera block is retired (brief 059)." The engine already refuses that call (`nothing-to-call`, `GameOrders.cs:672`).

**Hashes:** the rulebook's text is part of the inputs hash (`Debrief.cs:189`), and that hash is the default start text for scenario and script checkpoints (`sim/cli/Program.cs:120`). So the words alone probably move every pinned checkpoint, even though no behaviour changes. Land them in the same round as the engine change, so the hashes move once. Keel confirms.

### 1.3 Before a Listen (confirmed, with one gap)
**What shows until the tie is known** (a Listen on either lover, or a briefing that says it):
- the Call with its price (+1 Trail, and who would see it);
- its outcome as "?";
- no badge, portrait or name of who would come, or (under option 1) of where he'd go;
- in every shut way, words that name only the one called, never the one they'd send for. Fable's `summon-no-visit` ("Calling {who} brings nobody.") already does this.

**The gap: the other lock lines name the guard.**
- They are `summon-busy` ("The guard is down, busy or away from the door."), `summon-came-once`, `summon-alert`, `summon-saw-violence`, `summon-too-far` ("…the guard's walk to Inez…") and `summon-off-street` ("Inez or the guard…").
- They hold in the Handover by Keel's exception, because the briefing says "Front door: Inez and the door guard are close" and "Turn her to call him off and loop his camera".
- A generated mission's lock words must not name the lover until the tie is known. Before that, a shut way reads only as "not now", which is the price of not listening.
- That's Fable's general form, plus Latch's choice of which words show. The Handover doesn't need it now.

**Intel's "Call them when it counts"** shows only where a Call is offered (Keel's Latch item), and says nothing of who would come.

### 1.4 Step 4: the caller's own state (with Fable's input)
**Yes, the law should exist. One law covers whoever acts on a call:**
> **Nobody acts on a call while they hold a memory of the cell's gunfire, violence or drawn weapon.** The one called won't make the call, and the one who'd leave his post won't come.

**Why (people > powers):** someone who has just watched the cell hurt a person doesn't do it favours that day.
- The lover already refuses for exactly this ("he saw what you did", rulebook summon). This is the same test, read from the one called, so the game learns no new idea.
- It holds for every contact in every mission. It lasts the mission, as memories do (no decay, rulebook ladders).
- Under option 1 the person visited isn't acting, so her memory doesn't stop his visit.

**What it is not:**
- **Not fear.** A pressured Inez is afraid and still calls: the short visit stays (brief 085). Pressure is the cell's lever; violence is what turns people away.
- **Not harm she doesn't know about.** A person knows only what she saw or was told (§04 knowledge). If the guard went down out of her sight and nobody told her, she tries the call. A told memory reads exactly as it does in his test today.
- **Not the end of the relationship.** She stays turned. Between missions the existing rules carry it: the saw-violence ladder (bravery down; fear and resentment up, toward Informer and Talker, §06) and grief (§04).

**Shut, not absent:** play made it, so it is news (§1.1).

**Order:** it is checked before the lover's state. When both hold (she saw him killed, and he is down), hers is the one that shows. So the busy line ("You've picked your moment, dear.") can't play over a death she watched.

**What the player sees** (no line explains a rule):
- **Before the click:** the Call row on her stays, shut.
  - Its short lock is a fact in HUD words, such as what she saw ("Saw violence"). It is never a rule, such as "won't help you after that". Fable writes it.
- **At the click: nothing rings.**
  - No phone scene, no line from her and none from him. Silence is what a person in her state gives the cell.
  - Latch: the refused-call feed (`FeedSummonRefused`) plays nothing for this code, and the pop repeats the lock.
  - Optional, for Glyph and Chime: if the pop wants a sound, a phone ringing out with nobody answering is the world's answer, not a rule.
- **In the street:**
  - she reads scared: her head cue, and a Watch shows it (§04 mood);
  - where the one hurt was her lover, she runs to the call box to report it (kin, §05 reports row 3). That tells the player more than any line could.
- **In the debrief:** what changed for her, in plain words, by the end-card rule.

**The case next to Fable's: the guard is down, and she didn't see it.**
- She tries, and nobody answers.
- The host already knows he is down: he gives no answer then (TieHears). So Fable can give that case its own lines, true to what she knows (something like "He's not answering."), with no new engine code.
- Today's "You've picked your moment, dear." implies she knows the moment is wrong. This is Fable's and Latch's call; I only flag it.

**Cost:**
- **Keel:**
  - one clause in `SummonRefusal`, reading the caller's `Memory` with the same acts and the same `By` test as the lover's, and one new refusal code;
  - three scenarios: she saw a takedown, then is called; she saw her lover killed, then is called; she was only told of it.
- **Waymark:** the reference routes do no violence before the call, so route A's bands don't move. Check only the careless and loud bots, by delta.

**Out of this law:** a role job that isn't a call. Whether a turned driver who saw the cell's violence still brings Popovic round is a separate question. I'd leave it: route B's bands rest on it, and he does the job in his own car.

## 2. What turning the guard should buy (for Sky)

### 2.1 Today
**Arthur Reilly, the door guard,** is a Mercenary with a debt (a bureau loan, like Inez's), and Inez's lover (`sim/data/handover.people.json`, p-records-office-01). He can be turned three ways today:
- **Pay $70,000** after a Watch (+2 Trail). His door is in the entrance camera's view, so a Pay there is taped unless the camera is looped.
- **Help $50,000** after a Watch and a Listen on him (+1 Trail). It clears his loan; a Mercenary's Help costs $10,000 more (`worse-help-once-usd`).
- **Pressure, free,** once a Listen on either of them has found the affair (+3 Trail). He is afraid, resents it, and could name the Fixer.

**What it buys: nothing a player can use.**
- Calling him brings nobody.
- He still stands his door, and the Enforcer can't go in past him (`GameOrders.cs:736`; `Game.cs:340` has no turned test).
- He still radios what he sees (`GameTakedown.cs:369`).
- He still watches the cell for loiterers.
- The debrief doesn't count him as a contact. It counts only Inez and the driver (`Debrief.cs:49`).
- The one change is that a takedown he sees adds no witness Trail (`GameTakedown.cs:360`), which the player can't see.

So $70,000 goes and nothing happens. That's a trap, and every option below removes it.

### 2.2 Option 1: he leaves his door when you call him (recommended)
**The law:**
> "A call on a turned contact empties a post: their own if they keep one, otherwise their lover's. Whoever leaves walks to the other, stays a minute and walks back."
- Today only the second half exists: Inez is called, and he comes. This reads the same visit from either end.
- He comes once a shift, whichever of them is called. The visit is the short one if either of them was pressured. Every refusal stays as it is, read from either end.

**What the player gets:**
- Turn the guard instead of Inez and call him. He walks to Inez at the café, stays a minute and walks back.
- It is the same window route A uses: about 2:54, or about 1:39 if either of them was pressured.
- Inez needn't be turned.

**Price of the front door, by who is turned** (the loop's +1 and the call's +1 included):

| who is turned | needs first | $ | Trail |
|---|---|---|---|
| Inez, Help (today's route A) | Watch, Listen | 40,000 | 3 |
| the guard, Pay | Watch; loop first, or the camera tapes it | 70,000 | 4 |
| the guard, Help | Watch, Listen on him | 50,000 | 3 |
| the guard, Pressure | a Listen on either (the affair); loop first | 0 | 5 or more; the short window; he could name the Fixer |

**The two ways (§07) and the purse:**
- Route A keeps its shape, its window and its timings, and gains a second person to turn. Inez stays the cheapest door, so the mission's tenet stays at its centre.
- Route B is untouched.
- The purse rule holds at his end too, so learning still buys the second route:
  - paying the guard ($70,000) plus helping the driver ($50,000) doesn't fit in $100,000;
  - helping both ($100,000) fits exactly.

**Re-measure:**
- Route A's window doesn't change (same walk, same stay), so its bands stand.
- Waymark measures the guard's end once, across seeds, as a new variant of A.
- B and G are untouched.

**Build: small,** about one short engine round. It adds no new system: the summons is read from both ends.
- **Keel:**
  - The visit reads the pair. The one who walks is whoever keeps the post, and the meeting point stays her post (brief 022). The short visit and the once-a-shift count read either end.
  - The visit code already walks the guard to Inez, so it is reused. It goes in the planned `GameCall.cs` split, about 100–200 lines with a new scenario file.
  - The debrief counts a turned guard as a contact. His trip to the café is already traced as staged (`Debrief.cs:46`), so his line sits beside Inez's "may be questioned". A pressured guard would talk if questioned (the Talker test).
  - Rulebook delta: "the one who leaves" replaces "the lover" in the visit, and `perShift` counts the one who leaves.
- **Latch:** nothing beyond the planned `CallOffered` swap (his Call row appears because the engine offers it), plus the call scene between the Fixer and the guard.
- **Fable:** about a dozen lines, to brief 102's bar: the Fixer's ask, his yes, and his refusals spoken to the Fixer rather than to Inez.
- **Glyph:** the Call card's "?" for someone who would leave his own post.

**Against the pillars:**
- **Subvert people:** knowing more makes him cheaper: $70,000 with a Watch, $50,000 with a Listen, free with the affair (paid in his fear).
- **Orchestrate the city:** the same window, one the player sees coming.
- **Live with it:** every generated mission with a posted lover has two ends to work.
- **Systems > scripts:** one law, read both ways.
- **Preparation > improvisation:** the loop and the timing still decide it.

### 2.3 Option 2: he lets you in
**The law:**
> "A turned person who keeps a door lets the cell through it, and doesn't report it."

**What the player gets:** turn him and loop the camera, and the Enforcer walks in whenever he likes until 5:10, when the guard's shift ends (the relief isn't yours). No call, no window, no Inez.

**Price** (loop included): paid, $70,000 and 3 Trail; helped, $50,000 and 2 Trail; pressured, free and 4 Trail.

**The two ways and the purse:**
- It adds a third front door, quicker and simpler than route A. With a Listen it is also cheaper in Trail (2 against 3).
- Route A becomes the cheap but timed way, and many players will skip Inez, so the mission's tenet becomes optional.
- Route B is untouched, and the purse works out as in option 1.

**The fiction:** a guard who waves a stranger into the Records Office in plain view is the first man questioned. A man taking money would rather be on his break, which is option 1.

**Re-measure:**
- every front-door band, since there'd be a new fastest quiet route; route A's place in the reference results becomes Sky's to decide again;
- in generated missions, every guarded door becomes a price the mission generator must set, so that the bribe isn't always the answer.

**Build: medium,** one to two engine rounds.
- **Keel:** the entry check, plus his reports and his loiter watch once he is turned; the debrief's line for who let them in; scenarios across the front-door rows.
- **Latch:** small to medium: the Enter card stops warning of him, and he steps aside.
- **Fable and Glyph:** small.

**Against the pillars:**
- The most readable: it's the first thing a player guesses, and it puts people first.
- It trades away orchestration and preparation at the capstone, and money opens doors everywhere.

### 2.4 Option 3: he keeps quiet about you
**The law:**
> "A turned person keeps quiet about what the cell does, whatever their job."

**Today the GDD disagrees with itself here.**
- §05 witness says a turned witness "makes no report".
- §05 reports puts staff (row 2) before turned (row 4), and the engine follows that list (`GameTakedown.cs:369`).
- The engine reads it both ways already: its crowd-guard run check skips a turned guard, but the door guard's doesn't (`GameCombat.cs:685`, `:1022`).

**What the player gets:** he stops radioing a knockout, a carry or a body he sees, and stops reporting the cell hanging about near his door. He still holds the door, and still can't be called.

**Price:** today's turn. No route changes; it gives improvised play by the front door a little help.

**Re-measure:** none for the routes. Check the careless and loud bots by delta.

**Build: small.** Keel changes the report order and the loiter watch for turned staff, with scenarios. Latch and Fable have nothing to do.

**Honest view:**
- It buys little on its own, and it can go with option 1.
- My lean is against it: a man on duty radios what he sees in the open, paid or not, and the city fears disorder.
- Either way, the GDD's two lines must agree. Aster settles which in the catch-up.

### 2.5 Option 4: you can't turn a guard on duty
**The law:**
> "A person on a security post answers to it while they hold it: no approach is offered on them there."

This is "absent, not greyed", like the Call.

**What the player gets:** no Pay, Help or Pressure on him or on any guard at a post, so no money is lost to nothing. He can still be watched, listened to, knocked out or shot.

**Price, routes, purse and bands:** none change.

**Build: small.**
- **Keel:** an offer law on approaches; the scenarios that turn or press the guard retire.
- **Latch:** the rows go.
- **Fable:** nothing.

**Honest view:**
- It is true to today's rules, and it removes the trap.
- It takes away the most natural bribe in the game, and cuts across §04: guards are Loyal or Mercenary so that they can be approached, and "a Mercenary sells you to the highest bidder".
- It goes against the living world too (brief 098: a tenet can be played with).
- It is the right stopgap only if Sky wants none of 1–3 yet.

### 2.6 Recommendation: option 1
- **It makes the purchase real without moving the capstone.** Inez's call stays the centre, her door stays the cheapest, and the window and its timings don't move. So the routes' bands stand, and of the three options that buy something, it is the smallest build.
- **It passes all three pillars.** Knowing more makes him cheaper, the window is one the player sees coming, and the same choice comes back wherever lovers keep posts.
- **It keeps the tiebreakers:** readable (pay him, call him, he walks off), people > powers, systems > scripts (one law, both ends), preparation > improvisation (the loop and the timing still decide it).
- **It fits the Handover focus:** no new system, just the existing visit read from both ends.
- **It is true to people:** a bought guard arranges to be away rather than wave a stranger in, and the investigators still find the trip staged.
- **Option 2 is the honest alternative** if Sky wants the most direct reading. The choice then is a quicker front door that makes Inez optional.

### 2.7 For Aster to put to Sky (plain words)
> Turning the door guard, Arthur, costs real money today ($70,000 to pay him) and buys nothing. Calling him brings nobody, he still blocks the door, and he still radios what he sees. Four ways to fix it:
> 1. **He leaves his door when you call him.** It's the same visit to Inez as when she calls him, just from his end, so the window and its timings stay the same. Paying him costs more than helping Inez, and listening first makes him cheaper. *My pick: Inez and the timing stay at the heart of the mission.*
> 2. **He lets you walk in.** No call and no window while his shift lasts. It's what a player guesses first, but Inez becomes optional, and every guarded door in the game becomes a price tag.
> 3. **He keeps quiet.** He stops radioing what he sees, but still blocks the door. It's small, and can go with 1.
> 4. **You can't turn a guard on duty.** The option disappears. That's honest, but it takes away a natural bribe.

## 3. Mismatches found on the way (for Aster's GDD catch-up)
- **§05 witness against §05 reports,** for a turned staff witness (§2.4). The engine follows the reports list.
- **The rulebook summon rule's last sentence** still names the retired camera block (§1.2).
- **Prices in §04 and §07 lag the engine since 0.44.0 and 0.45.0** (brief 096), unless the catch-up already carries them:
  - §07 route A's "a fearful Inez takes two visits at $20,000" is gone: she takes Help once, at $40,000;
  - the driver's Help is $50,000, not §07 route B's $40,000;
  - so §04's purse line, "help + driver = 80,000", is now $90,000, and so is my card's hold.
- **§07 capstone:** "the call that pulls the guard off his door is Inez's" gains "or his own, once the cell has turned him" if Sky takes option 1.

## 4. Next owners
| who | inputs | done-check |
|---|---|---|
| Aster | §2.7 to Sky; §1 to Keel | Sky's pick recorded in brief 102 |
| Keel | §1.2's sentence, clause and fix, and §1.4's refusal, in one engine round after Latch's build is out, in its own worktree; option 1's pair reading if Sky takes it | sim suite green; §1.4's three scenarios and option 1's pass; pinned hashes moved once |
| Fable | §1.4's lock words; the "he's down" lines; the general no-name lock form (§1.3); option 1's lines if taken | brief 102's bar; Sky reads the sample |
| Latch | no scene for the caller's own refusal; the planned `CallOffered` swap | `HostLogic.Summons` assertions |
| Glyph | confirms "absent"; the shut lock's look; option 1's "?" | Glyph's note |
| Waymark | option 1: the guard's end, once across seeds; option 2: every front-door band | bands across seeds, delta only |
| Pulse | option 3, or the GDD fix, touches Pulse's reports list | agrees the reading with Aster |

## Evidence (read only)
- Keel's facts hold at a4e047cff:
  - `SummonRefusal`'s first test refuses any lover but the guard (`GameOrders.cs:628`);
  - `CanCall`'s driver branch says ready (`:619`), while `CallRefusal` refuses it (`:672`);
  - Enter fails on `GuardAtDoor` (`:736`), which has no turned test (`Game.cs:340`);
  - staff radio before the turned row (`GameTakedown.cs:369`; `IsStaff` and `IsCellPerson` at `:97–98`).
- Cast (`sim/data/handover.people.json`):
  - p-records-office-01, Arthur Reilly: Mercenary, need debt, record bureau-loan, lover p-cafe-14;
  - p-cafe-14, Inez Nikolaou: Fearful, need debt, record bureau-loan.
- Prices and values:
  - `people.rules.json` approaches: Pay $70,000 and +2; Help $40,000 and +1; Pressure +3;
  - rulebook `worse-help-once-usd` 10,000 (`rulebook.json:71`), `lover-stay-s` 60, `summon-pressed-stay-s` 5, `summons-per-shift` 1, `summon-alert-max` 1, `call-trail` 1;
  - window times from brief 085 (2:54.4 and 1:38.9).
- Briefing: `text.json` `briefing.hints.front-door` and `briefing.page.ways.front-door.line`.
- Debrief: the guard's trip is traced as staged (`Debrief.cs:36`, `:46`); only Inez and the driver count as contacts (`:49–50`).
- Hash: the rulebook's text is in the inputs hash (`Debrief.cs:189`), which is the default start text for checkpoints (`sim/cli/Program.cs:120`).
