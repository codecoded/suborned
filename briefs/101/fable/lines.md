# 101 · Fable: the why block's titles and lines, the take-control pop, Inez's slip

Fable, 2026-11-09, on `work/098-living-playbook`. Uncommitted; Aster commits. No Unity runtime code, scene, GDD or engine was touched. The hand edits, with what the judge read and said: `briefs/101/fable/hand-whys-2.json`.

**In short:**
- **`alert.warned`'s line** no longer reads out a rule: "{who} told {agent} to drop the gun."
- **`alert.confirmed`** has no "confirmed" left in it, and no longer says "police": the one who looks can be a robot sent to look, the patrol, an officer or the guard (`GameResponse.Confirm` from `FoundBySecurity` and a unit's `Verify`), and the host doesn't say which. Its title is now "It was a killing", its short "the body was a killing" and its line "On a closer look, {about} had been killed."
- **The sweep:** three more lines stated a rule (`police.gang`, `police.cordon`, `onWay.ways.checkin`). No other AI-sounding word is in the block.
- **The judge read the titles and lines** for the first time since guide /19, so 19 more faults were fixed from its findings. "Called the police" was false when {who} is a camera. Two causes were both titled "Weapon seen". Six lines were two sentences. "Escort" was an engine word. The post and split lines were unclear.
- **The take-control pop** can't name a click, because no click takes control: from the map, only T does. The pop should go. That needs Aster's yes (round 3 asked for the click) and one line from Latch.
- **Inez's slip:** yes. Debt and Secret can show as icons, and every row reads right as written. No words changed.

## The lines, before and after

Each cause and way below carries `hand` in `text.json`, so no run of the style loop replaces it.

| where | before | now | why |
|---|---|---|---|
| `alert.confirmed` title | Killing confirmed | **It was a killing** | AI-sounding word; true whoever looked |
| `alert.confirmed` short | police call it a killing | **the body was a killing** | "police" not always true; 277.4 px of 296 |
| `alert.confirmed` line | The police checked {about} and confirmed a killing. | **On a closer look, {about} had been killed.** | AI-sounding word; true whoever looked |
| `alert.warned` line | {who} told {agent} to drop it. Holster, or it's called in. | **{who} told {agent} to drop the gun.** | a rule read out |
| `police.gang` line | Nobody saw who fired. Police come, but don't seal the district yet. | **Nobody saw who fired, but the shots brought the police.** | a rule, and "yet" foretold it |
| `police.cordon` line | At Alert 3 the doors lock and police close the district's exits. | **The district is in lockdown: the doors lock and police close its exits.** | a law of the game |
| `onWay.ways.checkin` line | {about} is due to check in. When they don't, the desk knows. | **The desk misses {about}'s check-in in {s} s.** | a rule |
| `alert.carry` (Popovic) title | Popovic's escort seen | **Popovic seen led away** | judge: "escort" is the engine's word, and reads as his bodyguard |
| `alert.drawn` title | Weapon seen | **Gun seen** | judge: two causes with one title; "gun" everywhere else |
| `alert.drawn-people` title | Weapon seen | **People saw a gun** | as above |
| `alert.fought` line | {about} saw the Enforcer coming, drew and called it in. | **{about} saw the Enforcer coming, drew a gun and called it in.** | judge: "drew" alone is shorthand |
| `police.witness` line | {who} saw the shooting and called the police. | **{who} saw the shooting, and the police were called.** | judge: a camera calls nobody |
| `police.drawn` line | {who} saw {agent} with a gun out and called the police. | **{who} saw {agent} with a gun out, and the police were called.** | as above (80 characters at its widest) |
| `police.shot-at` line | {about} was shot at, and people called the police. | **{about} was fired on, and people called the police.** | judge: one phrase, as its short |
| `police.patrol` line | The patrol was shot at {where} and called it in. | **The patrol was fired on {where} and called it in.** | as above |
| `police.post` (routine unknown) line | The guard was missed at his post, so the police are coming. | **Nobody heard from the guard, so police come to his post.** | judge: "was missed" reads two ways |
| `police.split` line | Popovic is gone, so police close in on whoever is left. | **Popovic is gone, so police close in on the agents still here.** | judge: "whoever" could be anyone |
| `onWay.ways.radio` line | {who} is radioing it in. It lands in {s} s. | **{who} is radioing it in, and it lands in {s} s.** | judge: a line is one sentence |
| `onWay.ways.walk` line | {who} walks to {to} to report it. It lands in {s} s. | **{who} walks to {to} and reports it in {s} s.** | as above |
| `onWay.ways.run` line | {who} runs to {to} to report it. It lands in {s} s. | **{who} runs to {to} and reports it in {s} s.** | as above |
| `onWay.ways.camera` line | {who} has it on tape. The desk sees it in {s} s. | **{who} has it on tape, and the desk sees it in {s} s.** | as above |
| `onWay.ways.robot` line | {who} is ringing the desk. It lands in {s} s. | **{who} is ringing the desk, and it lands in {s} s.** | as above |
| `onWay.ways.screen` line | The desk sees {about} is dead in {s} s. | **{about} is dead, and the desk sees it in {s} s.** | judge: read as if the camera dies in {s} s |
| `onWay.ways.look` plate | Going to look | **Going to look at the body** | judge: at what? |
| `onWay.ways.look` line | {who} is going to look first, then report it. | **{who} is going to look at the body, then report it.** | as above |

Where I didn't take the judge's own words, this is why:
- **"…, and it lands in {s} s"** went over 80 characters for walk and run at the schema's widest fills (82 and 81). The judge's "…, landing in {s} s" left "landing" on the person walking. So these two say "and reports it in {s} s".
- **The judge's "The guard wasn't at his post"** may be false: he can be lying at it. "Nobody heard from the guard" is true either way, and never says check-in, which the cell doesn't know of yet.
- **For the screen line**, "{about} is dead, and the desk sees it in {s} s" keeps the family's shape, in place of "the dead screen from {about}".
- **For the warning**, the judge wanted one phrase across title, short and line. The title and short (Aster's last round) say "drop it", and the engine calls it the drop-it warning. So the line says "drop the gun", and "the gun" stops it reading as dropping someone carried.

`onWay.ways.checkin` never shows today: the host makes no check-in mark (`HostWhy.Reports.cs`). Its words are ready for when it does.

## How it was judged (guide /23)

From guide /19, the why families held the shorts only, so no judge had read the titles and lines. Guide /23 changes three things:
- A hand run of a why cause family now scores each cause's title, short and line as they play.
- PLAIN holds titles and lines to "what happened, never a rule of the game", as it held shorts. The code check fails an AI-sounding word in them; today's confirmed title and line both fail it.
- The facts follow the engine: who confirms a killing, and that only the guard or an officer gives the gun warning (`GameIntent.TodayWarns`).

Two label families exist in hand runs only (`short-slip23.ts`): Inez's slip, and the take-control pop. A writer run's families read exactly as at /22, except the ways marked `hand`.

Each family was scored once by Motive on Claude Opus 5.5, approved as Sky, as in brief 102's hand rounds, with my draft in place:

| family | score | findings | run |
|---|---:|---:|---|
| `why/witnessed` | 6 | 8 | `runs/style-agent/short/2026-11-08T20-08-31.473Z-why-witnessed` |
| `why/gunfire` | 7 | 10 | `2026-11-08T20-08-33.427Z-why-gunfire` |
| `why/police` | 8 | 3 | `2026-11-08T20-08-35.446Z-why-police` |
| `why/on-the-way` | 5 | 11 | `2026-11-08T20-08-37.447Z-why-on-the-way` |
| `label/inez-slip` | 6 | 6 | `2026-11-08T20-08-39.458Z-label-inez-slip` |
| `label/take-control` | 8 | 2 | `2026-11-08T20-08-41.491Z-label-take-control` |

That is 123.5k tokens on the subscription, and no paid API. Each line the judge faulted was rewritten from its finding (`judged` in the hand file) and not scored again, as in the earlier hand rounds. The judge had no finding on the gang line.

**Declined** (10 findings):
- **`alert.down` "knocked-out body found"** (TRUE: "body" reads as dead): this was Aster's pick last round. "Body" is the game's word for anyone down (Carry, "a body seen carried", the stash's "Body found."), and "knocked-out" says alive.
- **`alert.woken` "someone woke and told" and its Popovic variant "Popovic told when let go"**: last round's hand shorts, and shorts weren't this pass. The judge would rather have "someone came round" and "Popovic reported it". Aster's call.
- **`alert.noise`, `alert.gunfire` and `police.rivals`**: shorts the /19 loop accepted at 10, outside this pass. The judge offers "a hidden person heard", "{agent} fired" and "gunfight with the rivals".
- **`onWay.nofix` "Too late to stop it"**: true as it stands. For a tape or a dead screen, the chance to stop it came earlier (a loop, or not cutting the camera).
- **`onWay.ways.camera`'s plate "On tape · desk in {s} s"**: the judge's "On the desk's tape" is untrue, since the tape is the camera's. The countdown is to the desk's scan, as on the dead screen's plate.
- **Inez's slip, the full stops**: the judge applied the label kind's "a fragment, no full stop". But the card's rows are sentences, as Sky agreed them and as Glyph's mock draws them. My family's context should have said so.

## The take-control pop ("Take control: T")

The pop shows over the agent for 2 s as control ends (`HostDrive.DriveExit`, `HostDrive.cs:168`). That is either just after the player lets go (T, or the roster's Back to map tab), or when the game ends the drive and the follow view takes the agent back by itself within half a second.

No click takes control today:
- From the map, only T does it (`HostFollow.FollowKey` → `DriveToggle`).
- A click on a face selects and frames the agent.
- Once in the ground view, a click on a face swaps the agent you hold.
- GDD §10 has the ground view as [lab]: "T drops into the selected agent".

So the pop can't name a click. At the moment it shows, it tells the player nothing they can use, so it should go. The judge agreed (8). Its two findings:
- The cut is only real once the host stops drawing the pop. A pop the host has no word for prints its key (text.test.ts's `door-seen` note), so the word must stay until then.
- Round 3 asked for a pop naming the click, and Sky approved round 3. That conflict goes to Aster rather than being settled in a cut note.

`text.json` keeps "Take control: T" for now. `hand-whys-2.test.ts` ties the word to the host: it holds the word while `HostDrive.cs` pops it, and fails once the host stops, so the word goes then. `less.pop.let-go` ("Map view: T") names a key too, but no code pops it. It can go with the first.

The real gap: taking control is key-only (T), against Sky's rule. Once Glyph gives it a click and Latch builds it, the click is the feature's own icon, and no pop is needed.

## Inez's slip

Yes: on Glyph's slip, Debt and Secret show as their icons and the rest of each row as written. Help and Pressure stay as words on their capsules.
- "Bureau loan, and she's behind." reads right after the debt icon.
- I first wrote the Secret row as "Secret: she's seeing Arthur, the guard.", because without its word "seeing Arthur" has no subject. The judge (6) found that needless, and it was right:
  - Her name and face head the slip.
  - "Door" says which guard, and where Pressure leads.
  - They are Sky's agreed words (briefs/085/fable/approved.json).
  - Sky saw the row without its word on Glyph's board and said yes to round 3.

  So no row changes.
- The judge's note on "Pressure: threaten to expose the affair." (the slip calls it seeing Arthur, never "the affair") is Sky's to raise, if ever. The words are Sky's.

For Latch, the label is the part before ": " (Debt, Secret; Help, Pressure). It stays in the text: today's card, the book and the Journal show it as a word (`hand-whys-2.test.ts` holds that shape). One more card has the same shape: after the small Help offer, "Listen first: learn what she owes". Its label is "Listen first", and its act is Listen. If the slip makes every way a capsule, map it to Listen, or ask me for a reword.

## Open, and next owners

- **Aster:** the take-control pop goes. Round 3 asked for the click, but there is none to name. If you agree, **Latch** drops `PopKey("take-control", …)` in `HostDrive.DriveExit` (`HostDrive.cs:168`). Done-check: letting go of an agent, or the game ending the drive, shows no pop and no bare key. Then I remove `less.pop.take-control` and `less.pop.let-go`.
- **Glyph:** taking control has no click. It needs one (Sky's rule), for Latch to build.
- **Aster:** the judge's views on the declined shorts (`alert.down`, `alert.woken` and its Popovic variant, `alert.noise`, `alert.gunfire`, `police.rivals`). These are your call.
- **Latch:** for the slip, the label is the part before ": "; and the "Listen first" note above.

## Code (the style agent; no runtime code)

- **`short-types.ts`:** guide /23, now `SHORT_NOW`.
- **`short-whys19.ts`:** the /23 hand families (title, short and line), the engine's facts for confirmed and warned, PLAIN at /23, and the held ways.
- **`short-slip23.ts`** (new): the two hand-only label families, and the scenes' names a card row may say.
- **`short-labels.ts`:** a card row's path.
- **`short-briefs.ts`:** `hand` reaches the label families.
- **`short-checks.ts`:** a card row may say the scenes' names.
- **`short-cli.ts`:** a hand edit names its `field` and may be a cut; label families take hand edits.
- **`why.schema.ts`:** a way may carry `hand`. The host reads only its plate and line.
- **Tests:**
  - `hand-whys-2.test.ts` (new, 8 tests).
  - `hand-whys.test.ts` follows round 1's confirmed short to where it ended up, and checks round 1 at /22, the guide it was scored at.
  - `hand.test.ts`: three non-null marks.
- **README:** the /23 paragraph.

## Checks

- **Export** (`bun run guard bun tools/sim/unity.ts --tree`): headless 472/472 and range 99/99 replay identically. `text.json` 99d317bf7172 parses, and the player's copy (`unity/Assets/StreamingAssets/sim/text.json`) is the same file.
- **`bun run hostlogic`:** 186 pass, 0 fail, on the exported words. The first try didn't compile: Latch's round-3 edits in the same tree were mid-way (`HudToolkit.Lock.cs`: `LockMark.EdgeTab`). Two minutes later, with no build running, they compiled, and all passed. No host test reads a line this round changed.
- **`bun run test tools/style-agent contracts/missions/handover tools/dialogue`:** 376/376, the eight new ones in `hand-whys-2.test.ts` among them.
- **`bun run typecheck`:** clean.
- **Every saved style-agent run rebuilds exactly:** 187 (24 conversations; short lines /1–/22 157, /23 6).
- **The lozenge:** 48 shorts at their widest, none over the 296 px box. The widest is still 295.3 px ("Popovic told when let go at the service drive · 4:44"). Confirmed's new short takes 277.4 px.
- **`apply`** with Aster's restore list, run for real on a copy: `text.json` byte for byte. The why units it holds are the four causes from round 1 and the eight hand ways.
- **A side effect to know:** a marked cause is left out of writer runs whole, so the shorts of the 14 causes marked this round are frozen too. A later lozenge round writes them by hand, or clears a mark first.
- **Scratch:** `out/scratch/fable/why-lines/` (the edits, the checks, the logs).

## 2026-11-09, after 0.2.290: the dead pops cut, the guard's no-visit lines

- **Cut from `text.json`:** `less.pop.take-control` ("Take control: T"), now that Latch has dropped its `PopKey` (0.2.290, 53a24e5b3). Also `less.pop.let-go` ("Map view: T"), which nothing had popped since brief 083 (50cfe9fa8). Sky's take-control button uses `actions.drive.label` "Take control" and `actions.drive.ways.release` "Back to map", which stay as they are.
- **The `label/take-control` family is retired:** `short-slip23.ts` builds it only from a text that still has the word, so its one run (`2026-11-08T20-08-41.491Z-label-take-control`) still rebuilds from its archive. `hand-whys-2.json` says the cut landed. `hand-whys-2.test.ts` now checks the word is gone and that no file in `Runtime/Play` pops either key, since a pop with no word prints its key.
- **The guard's no-visit lines**, now true wherever Inez stands (the record and every case are in `briefs/102/fable/hand-barks-3.json`):
  - "Fetch Inez? What good would that do you?" → "Inez? What good would that do you?"
  - "Inez has her tables, and I have my door." → "Inez can't get away. Not on a working day."
- **For Keel (the rules, not changed):** the turned guard's Call can never be taken in the Handover, yet the panel and the Fixer's click still offer it. `SummonRefusal` reads his tie before anything else, so it answers no-visit wherever Inez is, even beside him, and also with an alarm up or after he saw the cell's violence.
