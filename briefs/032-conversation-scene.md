# 032 · The conversation scene

**Status (2026-10-21): final, ready for Latch** (Sky chose timing B and accepted Glyph's camera and default; the choices are closed). First spec 2026-10-20. Author: Glyph. Asked by Sky (via the coordinator, noted in brief 031): when a conversation plays (Listen, Ask, Pay, Help, Pressure, Call, Mislead), the camera cuts to a close third-person shot of the two people and the dialogue plays out with mission time paused; Skip or Back to plan resumes real time. Not first person. No GDD, code or data has been edited.

## 0 · What already exists, so nothing new is written

- **The lines are Fable's.** `text.json` `scenes.items` has 106 scenes: `overheard` (Watch, Listen: the target talks with people around her), `approach` (Ask, Pay, Help, Pressure: keyed by verb, `about`, `outcome` and temperament; beats are the Fixer's `arrive`, the person's own lines, `reply`, `done`), `call` and `city`. Brief 014 plays them today as speech bubbles at the speakers (`less.feed`: 2 bubbles at once, 3 s each; call and city scenes their first 3 beats). The host finds a scene with `lookupScene()` and picks variants and `ref` lines with `pickIndex`/`pickLine` from the run's seed. **The scene is this same data on a stage.** No new prose, no new variants.
- **The outcome is only known when the act lands.** The engine decides a turn when the order lands, after the act's seconds (Ask 10 s, Pay 14 s and so on). Before then only the price quote exists, and its outcome reads "?" until the temperament is known (`BusyQuote.Outcome`). A scene that plays before landing must therefore show only the opening lines, never the reply (timing B, section 1).
- **Pausing is a host state.** The host's `Paused` flag holds the clock; no tick passes, no order is added, nothing is recorded.

## 1 · When it plays: B (Sky, 2026-10-21)

**The scene plays when the agent arrives and begins the act, with the opening lines only.** It never shows an answer, so it cannot contradict the result; the outcome reaches the player as today (the person's bubble, the ring marks, the history) when the act lands.

- **Trigger:** the act's first tick (the agent has walked to its stand spot and the act begins), for Listen, Ask, Pay, Help, Pressure, Call and Mislead. Watch's overheard scene is not a conversation the cell takes part in and stays as bubbles.
- **Lines:** only the opening beats, at most 3: the Fixer's `arrive`, the person's first reply line, and (Listen) the overheard exchange's first two beats, up to its last line before the Scout's `read`. Call and Mislead: `ack`, `arrive`. The `reply`, `done`, `result` and the Scout's `read` are never in the scene; they play at landing as bubbles, exactly as today. A scene with no `arrive` beat does not play.
- **Time:** the clock is held while the scene plays (Sky: "since the player can't move"). When it ends or is skipped the host unpauses and the act's seconds run as priced. The scene is 6 to 9 s of wall time (3 beats, up to 3 s each, less when advanced).
- **Why it is safe:** at the start of the act nothing is decided; the lines are pure functions of the run's seed (`lookupScene`, `pickLine`). The cut changes no engine state, so replays are identical with the scene, skipped, or never played.
- **Rejected for the record:** A (at landing, truthful) and C (whole conversation at the start, which needs a foresee of the outcome).

## 2 · The shot

It is a **cutaway**, a presentation state like the failure camera (`HostFailCam`), not the play camera. Hard cut in and out (0.12 s dip), never a swoop, and **it returns to exactly the view the player left** (map position, yaw and zoom, or the third-person chase, same agent): Sky's "no automatic view switch" holds because the way back is exact.

**Framing (recommended): a side two-shot.** The camera stands on the perpendicular bisector of the two people, so both are in profile and both faces read (the cell's look matters). Over the shoulder hides the cell member's face, so it is the fallback only when walls spoil the side.
- Distance: 3.6 m from the line between them (the two stand about 1.2 m apart), so both fit in the middle 55 % of a 16:9 frame.
- Height 1.9 m (just over eye level), looking 8° down, vertical FOV 35°. It reads as eye level and is still third person, above the street, never first person.
- The side is the one where the boom (HostFollow's indexed solids, 0.4 m margin) is clear and where the **target's face** turns toward the camera; if neither side is clear: over the shoulder behind the cell member, 2.2 m back, 0.5 m to the side, 1.9 m high.
- Slow push-in of 8 % over the scene (its only motion). Letterbox bars 6 % top and bottom tell the player this is a cutaway at a glance and cost no HUD.
- Listen and Watch overheard: the two **speakers of the overheard scene** (the target and the person she talks to) are framed; the Scout stands in the near corner, soft. Call: the Fixer and the contact. Mislead: the Scout and the rival.
- People are held where the engine has them. Idle loops keep playing (breath, a glance); no one moves. Other people and traffic freeze with the clock.

**GDD note (a choice):** the low 8° shot makes characters far bigger than the "40 to 60 px at 1080p" rule for play, and sits under the play camera's 38°. §10 fixes the *play* camera; this cutaway is not play. If Sky wants it inside the play limits, use the alternative: the same two-shot from 38° down at a close zoom (about 7 m), faces smaller, no GDD question.

## 3 · What shows

Everything except the bare minimum hides. Kept: the **clock with its paused mark** (the pill pulses, so the freeze is explained), the **Alert pill**, the bubbles and the controls. HUD at rest under 3 % (the bars are not HUD).
- **Lines:** the opening beats, one bubble at a time at the speaker (the tail says who; no names, no log, no subtitle bar). Each shown whole (no typewriter), held up to `bubbleSeconds` (3 s) then the next. A row of up to 3 dots (the played ones filled) sits under the pair.
- **No stamp, no price chip, no outcome of any kind.** The player has just paid the price on the order; the answer is not known yet. The scene ends on the last opening line with a 0.4 s beat, then cuts back by itself (no stamp to dismiss), so the player's wait is the lines and nothing else.
- Listen and Watch overheard: the two speakers of the overheard scene (the target and the person she talks to) are framed; the Scout stands in the near corner, soft. Call: the Fixer and the contact. Mislead: the Scout and the rival.

## 4 · Controls

- **Click or Space** advances (the next bubble at once). On the last beat it returns.
- **Esc, or the Skip keycap** (bottom right, one word: "Skip"): ends the scene now. Same action, any time.
- No other input is read. The clock resumes at the restored speed and pause state (a player who had paused stays paused). Orders are given on the map afterwards, as always.
- **Setting (Esc menu, "Conversations"): Always / First time with each person / Never.** Never gives today's behaviour exactly: bubbles at the speakers, the map unchanged. Stored in `PlayControls`.
- **Default: First time with each person** (any of the verbs above), so the first Ask of Inez plays and a repeat is bubbles. Call plays the first time. Reason: a run has 4 to 8 conversations; a freeze on every one would cost the real-time feel (Sky: "more realtime than strategy"). **Change from the first spec:** the clause "always on a refusal or suspicious outcome" is dropped, because under B the outcome does not exist when the scene plays.

## 5 · When it does not play

Any of: Never is set; a fight is on or Alert is 2 or more (it falls back to bubbles: nobody reads a scene while being shot at); the mission is over; either person is down, gone or inside; the target is not in the engine's view of the street; **another scene is already playing or queued** (a second act starting at the same tick plays as bubbles, so there is never a cut-spam queue); the player is in freeze or Y target mode. The same rule as `less`: a beat the cell was not present for is never shown.

## 6 · What it changes in the engine: nothing

- No order is issued, no tick steps, no random draw: the scene and its lines come from `lookupScene` and `pickLine` with the seed the host already uses, pure functions of the run.
- The clock is held by the host's pause, so a replay from start state plus orders runs identically with or without the scene; skipping or never playing changes no hash.
- It reads only what the host has already: the act-start event and the lines for it. It writes nothing to the session file or the history beyond what bubbles write today.
- Scene length is wall time. The mission clock does not move while it plays, so it cannot help or hurt a window; it can only cost the player's attention.

## 7 · Reading budget

No sentence appears that is not a character's line. Per scene, text on screen is at most one bubble, the clock, and one keycap word. Nothing is explained.

## 8 · Hand-offs

- **Latch:** the cutaway camera (state, hard cut, exact return, boom reuse), the Skip keycap, the setting; the scene runner reusing `SceneBubbles`' opening beats at one bubble a time, started at the act's first tick; "when it does not play" checks; the 0.12 s dip.
- **Glyph:** nothing more: B has no stamp or chip. The keycap art is one word and a glyph.
- **Fable:** nothing new; check that the chosen `read`, `reply` and `done` lines are one short sentence each (they are today) and the keycap words.
- **Forma:** conversation idle loops for the stand-ins (a nod, a glance), after the first look.
- **Glint:** the cutaway light: the same grade as play, no extra lights.
- **Chime:** a low murmur under the scene, if Sky wants sound.
- **Suture:** play route A with scenes Always, First and Never; the end state and the replay hash are identical in all three.

## Done-check

1. With scenes Always, every verb in the list plays its opening beats at the act's first tick on the Handover; the mission clock is identical before and after the scene and the act's seconds run only after it.
2. The same orders with scenes Always, First and Never give the same final hash and the same history.
3. The return view is bit-identical to the view before the cut (map and third person).
4. A fight or Alert 2 turns the scene into bubbles; a second act starting during a scene does too.
5. The reply, `done` and `result` lines never appear inside the scene.
6. Sky plays one Handover with First and says whether it was too much, too little, or fine; the answer decides the default.

## Choices for Sky
None open. Timing B, the low side two-shot, and the default First time with each person are decided (below).

## Sky (2026-10-21)
Timing: **B**, the scene plays at the start of the act with the opening lines only (no answer, so it can't contradict the result); the outcome shows later as today. Camera and default setting: Glyph's recommendations (low side two-shot; first time with each person plus refusals and suspicious outcomes) unless Sky says otherwise.

## Glyph (2026-10-21): closed for Latch
B removes the stamp table, the price chip and the refusal/suspicious clause of the default; everything else in sections 2, 4, 5 and 6 stands. Section 6 holds as before (the engine changes nothing).
