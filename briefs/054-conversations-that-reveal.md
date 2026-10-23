# Brief 054: conversations that reveal

**Objective:** Inez's conversations tell the player what is going on and what they can do about it. Watching her shows her routine; listening overhears her with the guard (the affair) and with a customer (her debt), so the player knows both levers: Help with the debt, or Pressure over the affair. The approaches and the call follow from what was overheard.
**Owner:** Fable writes; Motive reviews (§14 Fable: "Motive reviews"). The style agent (`tools/style-agent`, built for Assignment 6) runs the write → score → revise loop.
**Next:** the writing first. The generated conversations reveal what they should but read flat (Sky, below); Fable finds the human touch of the original lines before anything goes in. Then Fable and Keel raise the scene limits in the text contract, and Latch plays them in the build.
**Done-check:** every conversation in the Inez chain is accepted at 10/10 by the evaluator and passes the code checks; `bun run style:verify` re-checks each saved run without a model call.
**Evidence:** the Assignment 6 submission on branch `assignment-06`: `courses/a06-style-guide/` (page, proposals: 17 conversations accepted at 10/10) and `runs/style-agent/` (24 runs). Not on main.

## Decisions
- Sky, 2026-10-23: the dialogue is too short, doesn't fit the task and doesn't set up the next reveal; conversations with people should be proper dialogues that explain what is going on.
- Sky, 2026-10-23: listening to Inez we hear the affair (with the guard) and, with another customer, that she is struggling for money, so we know we can help with her debts or use the guard to pressure her.
- Sky, 2026-10-23: a conversation fills the act's own seconds, a speech bubble each 3 s (Watch 20 s, Listen 42 s, Help 50 s, Pressure 35 s, the guard's 60 s stay at the café on the call); lines stay short (at most 12 words, the text contract's limit, allowed for now).
- Sky, 2026-10-23: proposals now, the game later: the text contract, the mission text and the build are not changed for Assignment 6.
- Sky, 2026-10-23, after reading them: the generated conversations are boring; the previous lines had a more human touch. To deal with later.

## The chain
Built from the Handover's existing scenes (`contracts/missions/handover/text.json` `scenes.items`), one conversation each, by Inez's temperament (idealist, mercenary, fearful, loyal):
- Watch (scene 0): her routine and the guard's café stop; no debt, no affair, no temperament.
- Listen (scenes 4–7): with the guard, then with a customer; the affair, the debt, her temperament.
- Help (turned: scenes 29, 33, 36, 39) and Pressure (scenes 42, 44, 45, and 46 where a loyal Inez reports it): the Fixer uses what was overheard.
- Call (scenes 90–93): the Fixer calls Inez; she calls the guard over and spills the coffee; the door is open for about two minutes.
