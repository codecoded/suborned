# Brief 096 · Latch's round: Glyph's mocks built (2026-11-04)

Built from `briefs/096/glyph/index.html` (Sky 2026-11-04: "all. recommended"). Mac **0.2.240**, Linux **0.2.241**, both from the same tree (`52c5001e` + working tree). Nothing committed, uploaded or sent.

## What changed

**1 · The start briefing is the book's Job page.** A new run opens the book on Job with the clock stopped at 0:00. `ShowBriefing` is still the flag the host reads, and the book opens on it the next frame (`HostBook.Start.cs`). The page shows:
- the mission title in spaced capitals, the purse and Heat beside "Ways to fail";
- each way's faces: Inez and the guard for Front door, the driver for Pickup. They're derived from the contact and the roles its conditions name;
- the shared end line, with the conditions both ways share (guarded, go, left-safe) listed once under it;
- the clock with the 5:30 times; the rivals' 2:35 shows from the start (call 2);
- the cell strip (four faces, names, job words, one line each), and Begin where Back is.

Hovering a way or a face turns the frozen street to that person, joined by the line; a click keeps it there. Begin closes the book onto the opening view, then the roster and the clock come in and the clock runs. Esc, B or J closes it with the clock still stopped. In play, the Job page is the same page with Back (no Begin) and still frames the door. B opens Job, L opens the Journal. A Menu sign at the end of the rail opens the pause menu (call 6).

One data source: the ways come from brief.json, the conditions from book.job.needs, the clock from book.job.stops and the timeline, the words from text.json.

**Retired:**
- `Briefing.cs`, the old page: deleted. Its two live helpers moved to `HudToolkit.Entrance.cs` (`PgText`, which the book draws with, and `Entrance`, the HUD's entrance after Begin).
- `HostBriefing.cs`: the stills, pins, plan and door-glide parts are gone. It keeps the start sequence and a slim `BriefScreen` (target door, camera yaw, `MissionFile`). The Job page's door facing now comes from the site's vale exit → car, not from `stills.json`.
- `tools/game/briefstills.ts` and its test: deleted. Nothing else read them.
- The `briefStills()` call and `stillsKey` in `tools/game/cli.ts`: removed.
- README's Linux step that placed the stills: removed.
- Kept: `proofdelta.ts`'s `stillsKeyOf` (Splice's, still tested) and ShotRunner's `nocut`/`doorN` shot options (general shot options).

**2 · The Book button and the key-only fixes.**
- Book key, bottom right in the notes key's place: plate, vector stand-in glyph from `make/bookglyph.svg`, "Book", and a turquoise dot while the Journal has unread rows. A click opens the book (on the Journal when something is new). J stays.
- The pill's pause glyph is a button: it shows all the time, lights turquoise while stopped, and has a hover ring.
- Turn buttons sit on the scope's rim (Q/E).
- The fire line's hidden "X stop" keycap is now a Stop button (`book.key.stop`).
- Resting on a price for 0.6 s shows its breakdown, as Alt does. This covers the order sheet, the price chip and the fire odds, not lock sentences (`HostKeys096.cs`).
- No speed or next-event buttons (call 6).

**3 · The slim feed and the Journal.**
- `HostCrucial.cs` queues crucial cards, most urgent first (call 7): seal, police, agent down, rivals for our car, Alert, seen, Popovic out, rival car, turned or refused, body found.
- `HudToolkit.Crucial.cs` shows one card bottom right ("1 of n ›"). A click reads it and shows the next; a click on its line pans there. A card leaves by itself only when it stops being true: the rivals stop going for the car, or the agent is up again (call 4).
- The two story beats and the collapsed history panel are off the screen. L, the old notes key and the history's open path now open the Journal.
- Journal (`HostBook.Journal.cs`): every feed card plus every crucial card, newest first, with time, icon, title and → place. Crucial rows sit on orange discs and unread rows are dotted. Filters: All, Crucial, Learned, Turned, City. A click opens a row's lines and result and turns the street to the place; the wheel scrolls.

**4 · Objective 7's marks** (`HudToolkit.HeldGo.cs`, `HudToolkit.Ring.cs`). While rivals hold the car, Go on its ring is:
- striped, with the lock on the wedge and a violet holder dot;
- its pill reads "Go · lock · held by" with a badge per holder in the rivals' violet ring, the word unstruck;
- the fix ("Step out and draw · or down them") sits on a plate under the pill.

Police holders keep the old struck Go. The rivals-for-the-car card carries its two fix chips.

**Words:** all new words are Glyph's placeholders in `text.json` → `book.{start, journal, key, feed, heldGo}` plus the `menu` sign, each optional in `book.schema.ts`. They're listed in `new-words.md`. I edited none of Fable's lines. The name check bans "Vale", so the card key is `target-out` and its title says "Popovic".

**Files** (lines now): `Book.cs` 89, `BookLayout.cs` 157, `BookLayout.Start.cs` 112, `HostBook.cs` 152, `HostBook.Start.cs` 94, `HostBook.Job.cs` 52, `HostBook.Journal.cs` 114, `HostBook.Words.cs` 53, `HostCrucial.cs` 126, `HostKeys096.cs` 49, `HudToolkit.BookStart.cs` 67, `HudToolkit.BookJournal.cs` 44, `HudToolkit.BookKey.cs` 43, `HudToolkit.Crucial.cs` 64, `HudToolkit.Keys096.cs` 39, `HudToolkit.HeldGo.cs` 51, `HudToolkit.Entrance.cs` 41, `HostBriefing.cs` 111 (was 221), `tools/game/hostlogic/HostLogic.Book096.cs` 109.

Files over 500 lines didn't grow: `HudToolkit.Less.cs` 1886 → 1853, `HudToolkit.cs` 1300 → 1300, `HandoverHost.cs` 1681 → 1681.

Proofs updated for the new start:
- `FrontEnd.Proof` briefing checks;
- `ProofBook` (bookModal, and bookNeeds for the three getaway conditions);
- `Proof083` (tUnderBriefing);
- `ProofInput` (closes the book after its Book key click);
- `Proof067` (people: the shift guard at 5:08, the 5:30 shift; this one had been red since the shift).

`proofmap.json` has a row for the new files; `shots.json`'s briefing-day is now picture-only.

## Evidence

- `bun run hostlogic`: **103/103**. 5 new cases cover:
  - the start page's data and layout on the grid;
  - the queue's order and staleness, and that it leaves the closing hash unchanged;
  - the Journal;
  - the new words.
- Batch on 0.2.239: sim 394/394 headless, unity-match 491/491, words ok.
- Full proof on 0.2.239: every section ok except r083's `r085clipTopSpeed`/`FindsHim`. That part passed on 0.2.236, failed three different ways across reruns under load 7–24, and passed in the delta proof that followed. It's real-time video preparation at Speed 8: flaky under load, nothing in this change. 0.2.240 is a delta on top: nothing the proof reads changed.
- `bun run typecheck` clean. 300 tool and contract tests pass.
- `proofdelta.test.ts` 52/57. 4 of the 5 CLI-gate tests fail only because the tree's uncommitted deletion of `Briefing.cs` reads as a changed input; they pass once it's committed. I fixed the host-row case.
- Shots 13/13, audit 11/11 clean. Coverage within limits: HUD at rest 6–10 % against 15 %; +0.14 % for the Book key, as Glyph estimated.

**Budget still red (3 pictures), for Glyph and Splice:**
- `fight-loud-body`, `r123-fight-loud`: "2 card panels on screen". The slim feed's seal card shows beside the existing seal card under the clock. The budget's one-card rule was written for a conversation's cards. Tagging the feed card as a beat would break the beat rules (two lines, more than six words), so I left it honest. Needs a ruling: a feed-card kind in the budget, or drop the seal from the slim feed since the clock card already says it.
- `r067-official-car`: "1 line of running text": the clock bar's "5:20 ▶▶ 5:30" read as text. Not tied to a change of mine as far as I can tell; unverified.
- The book (and the start page) has no budget state. Book captures are now picture-only, with no HUD mask. Splice: add a `book` state.

## Builds

- **Mac 0.2.240:** `out/build/play/The Suborned.app` (not packed or sent).
- **Linux 0.2.241:** `GameBuild.CliLinux`, 0 errors, 67 warnings (none in the new files). Repackaged as brief 094 did (`out/scratch/latch/package.py`, README updated in `out/scratch/latch/README-Linux.txt`, stills left out):
  - `out/build/releases/TheSuborned-0.2.241-linux-x86_64.zip`: 678,505,012 bytes, SHA-256 `301ba2bb44e282e547c13d735653a918f4430e303f95dd238b8aa68df08318a5`
  - `out/build/releases/TheSuborned-0.2.241-linux-x86_64.tar.gz`: 678,366,257 bytes, SHA-256 `e4971b9728d220642da23d6f1571116f835077766b525b3da6f46b58cdd73f7f`
  - Checksums verified. The new code is in `Suborned.Runtime.dll`. Native Linux play is still the tester's.

## Pictures (play zoom, 1920×1080; `briefs/096/latch/shots/`)
- `1-start-job-0.2.240.jpg`: the start page
- `2-hud-rest-0.2.240.png`: the HUD at rest
- `3-feed-card-0.2.240.png`: the slim card in a fight
- 2x crops: `2c-book-key-2x.png`, `2d-pill-pause-2x.png`, `2e-scope-turn-2x.png`, `3d-feed-card-2x.png`

## Left
- **Held Go in a picture:** no proof picture shows rivals holding the car, so the striped Go, "held by" and the fix line aren't seen in a capture yet. Suture or a proof row with rivals on the car (K3-PRESS) should take one.
- **The "seen" card** says "One of ours seen" without who or by what. The engine's ledger cause doesn't name the agent; Glyph's "Hasti was seen · by the entrance camera" needs Keel to log it.
- **The fix chips** on the rivals card are labels, not buttons.
- **Old code that never shows now:** the history drawer's code in `HudToolkit.cs` (never opens), the story beats' host side (`PushBeat`) and the IMGUI view's history. Remove when those files are next split.
- **For Aster at commit:** Unity's world rebuild renumbered IDs in `FightRange.unity`, `HarbourDistrict-grade-night.asset` and six `Generated/Range/*.mat`. I checked each: the diffs are ID renumbering only. Leave them out of the commit.
- **Next owners:**
  - Fable: the words (`new-words.md`).
  - Glyph: a 2x review of the built start page, card and ring; the seal-card ruling.
  - Splice: the budget's book state and feed-card kind.
  - Aster: GDD v0.39.0 §10.
