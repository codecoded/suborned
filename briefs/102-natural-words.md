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
