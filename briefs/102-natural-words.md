# 102 · Natural words: agents quiet in play, no mechanics in anyone's mouth

Owner: main session (as Aster), opened 2026-11-08 on `work/098-living-playbook`.

## Sky (2026-11-08)
- "I've noticed we have a lot of poor wording stil throughout. Especially agents talk to player"
- On Fable's sample of the agents' voices (`briefs/099/fable/voices/index.html`): "Maybe even drop them? They will feel repetitive"
- On a proposed line, "He bought it. He'll be back, and he knows my face.": "that's exactly the kind of scripting that bugs me. 'I will remember you.' lines etc - so unnatural"

## Decisions
1. **The agents go quiet in play.** Option 1, from Aster's three, after Sky rejected the rare-lines kind.
   - **Gone:** no line when the player gives an order, when an agent arrives, or when a job lands, and no spoken warnings.
   - **The HUD carries the news:** the portraits, the rings, the cards ("Rivals nearing our car", "Enforcer badly hurt") and the street marks.
   - **The agents keep their voices only in scenes with people:** the Fixer's approaches and calls, the Listen scenes, taking Popovic.
   - **The world's people keep talking** (Inez, the guard, the driver, Popovic, police, rivals, the crowd), so the street stays alive.
   - If a moment feels too silent in play, one line comes back for that moment only.
2. **No line explains a mechanic,** for anyone.
   - **What fails:** a line that tells the player a game rule or a consequence is unnatural scripting: "He'll be back, and he knows my face", "You said no. You'll remember me.", "I will remember you", "A search could find it", "They'll notice".
   - **What passes:** people say what a person in that moment would say. The game shows consequences through the world and the HUD, never through a character explaining them.
   - **The standard is the lines Sky kept:** "You paid my debt? I'm yours. Say when." and "Civic Police! Drop your weapons!"
3. **Test pins on such lines go,** such as text.test.ts's "You said no. You'll remember me.", along with the rules that pinned them.

## Work
- **Latch:** silence the agents' barks in play (acks, arrivals, landings, warnings, reads). Scenes with people keep their lines. Agent speech in the Journal goes too, unless it's a scene's line.
- **Fable:**
  - The loop's guide: the no-mechanics rule, with these failing examples, in NATURAL and TRUE.
  - Sweep every line that remains for natural speech and no exposition: the people's barks, the scenes and conversations, the calls, Popovic, police, rivals, witnesses.
  - Retire the agent rows play no longer uses, or mark them silent.
  - Show Sky a short before-and-after sample from the people's lines before the sweep applies.

## Done-check
- In a played game, the agents speak only in scenes with people.
- No line anywhere explains a rule or a consequence.
- Sky reads the sample and says yes.

## Sky on Fable's sample (2026-11-08)
1. "yes - rewrites are much better": the test and the 22 rewrites (`briefs/102/fable/index.html`) are approved. The sweep runs to the same bar.
2. "we can try - sure": Opus 5.5 judges the sweep, gpt-6.1-sol writes. Fable builds the loop's Claude caller first, so its calls are recorded and replay.
3. "not into those e.g. 'Citizen. Walking is encouraged. Running is noted.' - that noted is AI sounding". None of the four "kept on purpose" lines stays as it is; they go into the sweep. **AI-sounding words fail too:** "noted", "understood", "proceeding", "acknowledged", "affirmative", "rest assured" and their kind, which no person in the street says.

**Defaults Aster took, which Sky can overturn:**
- The rivals following our car out on route B get a news card. The line doesn't come back.
- The Mislead is a scene with a person, so the Scout's tip to the rival comes back as a natural line written to the bar.

## The guard's Call (2026-11-09)
- **Fable's round** (cca05b263): the guard's refused-summons lines were made true wherever Inez stands, and the two dead take-control pop words cut.
- **Keel's proposal** (`briefs/102/keel/call-offer.md`): the turned guard's Call can never succeed, yet it is offered, and its words give away the lover.
- **Aster takes (b)**, which applies the GDD's own rule for a way that would do nothing ("absent, not greyed"): a Call is offered only where it has a use, read from the engine so the offer and the refusal can't drift.
- **Order:** Motive confirms the law's wording first. Keel's engine change follows once Latch's build is out (one heavy agent at a time). Latch swaps the host's test in the next round.
- **Open for Sky:** what turning the guard should buy. Today it buys almost nothing: no call, the door still shut to the cell, and he still radios what he sees. Motive drafts the options.

## Sky's calls on the guard (2026-11-09)
From Motive's options (`briefs/102/motive/turned-guard.md`):
- **"Leaves when called"** (option 1).
  - **The law:** "A call on a turned contact empties a post: their own if they keep one, otherwise their lover's. Whoever leaves walks to the other, stays a minute and walks back."
  - **Shared with Inez's call:** he comes once a shift whichever of them is called, the visit is the short one if either was pressured, and the window is the same as hers (about 2:54, or 1:39).
  - **Prices:** Inez stays the cheapest door ($40,000, 3 Trail). To use the guard instead: pay him $70,000 (4 Trail), help him for $50,000 after a Listen (3 Trail), or pressure him for $0 (5 or more Trail).
- **"Yes, they refuse"** (step 4). Nobody acts on a call while they hold a memory of the cell's gunfire, violence or drawn weapon.
  - Fear alone doesn't stop them, and neither does harm they didn't see or weren't told about.
  - It shows as a shut Call with a plain fact; nothing rings at the click.
  - Her state is checked before his.
- **The offer law** (Aster took Keel's (b), with Motive's wording): what a call *is* decides whether it's offered. What has *happened* decides whether it's shut, with its reason.

**Work, in order:**
1. **Keel:** one engine round in its own worktree: the `GameCall.cs` split, `CallOffered`, `CanCall` as `CallQuery(role).ok`, the step 4 clause, option 1's two-ended visit, and the rulebook sentences, so the pinned hashes move once.
2. **Waymark:** the guard's end as a new variant of route A, across seeds; the careless and loud bots by delta.
3. **Latch:**
   - the `CallOffered` swap;
   - Intel's call line only where a Call is offered;
   - no scene for the caller's own refusal;
   - the call scene between the Fixer and the guard.
4. **Fable:**
   - the shut lock's facts;
   - "he's not answering" when he's down and she didn't see it;
   - the lock form that names nobody before a Listen;
   - option 1's lines, about a dozen.
5. **Glyph:** the shut lock's look, and the Call card's "?" for someone who would leave his own post.
