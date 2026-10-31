# 085 · Sky's play notes on 0.2.177

Owner: Aster. Opened 2026-10-31, Sky's hand play of the Handover on the Air, third-person view in his shot (`/Users/sky/.t3/userdata/attachments/c5c3e6a1-50ff-47a1-a9a8-4be60485dd6f-4a170c49-e39b-4f9b-a518-2b2c789cd7e6.png`). Fight cues (brief 084) are separate and still waiting on his verdict.

| # | Sky | Done-check | Owner |
|---|---|---|---|
| 1 | "why so bright?" | Cause named with evidence; the paving keeps its detail and shadows read in both views, day, on the Air's screen; Sky has seen before and after | Glint |
| 2 | "when I was controlling the squad, it would sometimes just action one agent sometimes" | Cause reproduced; every selected agent gets the order, or the one that can't says why | Latch |
| 3 | "When shooting as a squad, they don't always all shoot, only one agent would start to shoot at a target unless I pressed F" | Cause reproduced; a squad told to fire all fire with no F, or the one that can't says why | Latch |
| 4 | "The rivals walk away too easily from a fight" | Cause named; rivals stay and contest, in bands across many seeds, without making the fight unfair | Pulse, Motive, Keel |
| 5 | "No visual cue to selecting the guard at the door" | Selecting or hovering the door guard is as clear as any other person | Latch, Glyph |
| 6 | "I think we should show what the agent is doing above their if they're perfoming an action or on their way to do one" | An agent acting or on the way to act says what, over the head, readable at play zoom, in both views; extends 083 note 4 | Glyph, Latch |
| 7 | "Conversations are too short and the whole speech is bad. We have our RAG surreal db for that to make better conversations. In the cutscene should clearly explain the full conversaiton so player knows whats going on" | Generated at build time through `tools/rag`; a player who only reads the cutscene knows who these people are, what was said, and what it opens up. Sky reads samples first | Fable |
| 8 | "When you listen or watch someone and learned, it should say clearly a new action is available" | Learning something that opens an action says so at that moment, in plain words, and names the action | Glyph, Fable, Latch |
| 9 | "The log in bottom right is way too busy, and not helpful enough" | Calmer, and each line tells the player something to act on; Sky picks from a mock | Glyph |
| 10 | "The game should start showing key NPC's in the area to help learn the game with some tips on what to do"; then "The quick info panel about important NPC's was actually useful, but it's so understated - I think we need to make that bigger feature to help players know what to do for early missions" | The top-right people panel leads the opening: a new player can tell who matters, what they know, what's hidden and what to try, without a key legend or a wall of text; fed by the mission's people, never one right way | Glyph, Fable, Waymark |

Standing rules: `less-reading`, `plain-copy`, `present-and-adaptive` in the lead's notes: one-line prices, no design jargon, what + why + fix, and a key legend means the controls are wrong. Conversations reveal and set up the next lever, and nothing is hand-authored as a script: everyone can be turned more than one way.

Note 7, Sky's calls on `briefs/085/reports/Writing good game dialogue.md` (2026-10-31): "clearly explain" binds the whole scene, the Scout's line and the cards included. No human writes the voices: "you just need to review famous works of public domain literature for classic conversations". A small live test, two scenes, current brief against the new one, read blind by Sky: yes. That go-ahead covers this test only; the full generator run still needs his yes.

Blind result (Sky, 2026-10-31): he picked Y on both scenes, which the key shows is today's brief, over the brief built on the classics: "that spell out the story, and coherently. starting with a short sentences as well feels more natural". He faulted "I'm too scared to say no. I won't forget your face." / "No, you won't. If you're ever questioned, you could describe it.": it "doesn't make sense - and not something Ines would say". The new brief changed many things at once, so the test doesn't say which part lost. Next run: today's brief, with a fuller Inez, game facts on the cards, a check on named fear and review faults sent back; the excerpts stay out.

Rule idea, not built (Sky, 2026-10-31, on the call): "if you get inez to call the guard, he runs back to the door because she said someone told him about them" "and said they looked supsicious (if she hasn't favoured the cell)". Today every turned Inez calls him and he stays, on all routes. Until Sky decides, the scenes keep to that: she gives him a reason to come and never tells him anyone knows. Owners if yes: Motive (the rule and its price), Keel (the sim), Fable (the scene).

Sky's further notes (2026-10-31): "one thing I really liked was the characters bubble that appears when an action occurs off screen....e.g. the guard on a call I think": keep it, don't lose it in this round. "Not seen the vale being taken by the enforcer PIP video play out. That should signifiy Enforcer is about to exit the building so player knows to check": the clip from brief 065 (`paid/065-capture-pip/final/capture_pip_480x270.mp4`, take 3, 10.1 s) was never wired in; it plays as the Enforcer comes to the end of Enter, ending as he comes out. "Would be good keep them walking": anyone not in a conversation keeps walking in the shot.

After this round: the `HandoverHost.cs` split (brief 084), with one full batch.
