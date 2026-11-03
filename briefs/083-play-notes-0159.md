# 083 · Sky's play notes on 0.2.159

Owner: Aster. Opened 2026-10-30, Sky's first hand play of the hit-roll build, on the Air.

Sky: "so - few things. I think change the drag LMB to select squads, to move map. When moving to third person it cancels any action the player was doing. Sometimes moving to third person, the WASD controls were disabled and was the point to click movement. It's way too easy to cancel a player's action, but also very unclear when a player is performing an action - need an icon above their head. Needs to be more clear which character you're controlling".

| # | Note | Done-check |
|---|---|---|
| 1 | Drag LMB moves the map, not squad select | drag pans; a click still selects and orders; selecting a squad has a way that isn't drag |
| 2 | Third person cancels the agent's action | entering and leaving third person leaves the order running |
| 3 | Third person sometimes leaves WASD dead and click-to-move on | cause named, reproduced, fixed; test |
| 4 | Too easy to cancel; unclear when acting | an icon above an acting agent's head, readable at play zoom; cancelling takes a deliberate act |
| 5 | Clearer which character you control | selected or driven agent unmistakable at play zoom |

Owner: Latch, Glyph for the look. Delta rule: fix loop by section, one delta batch at the end. Engine untouched.
