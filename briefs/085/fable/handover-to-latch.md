# 085: conversations, Fable to Latch

Inputs: `contracts/missions/handover/text.json`, `conversations.schema.ts` beside it. Next owner: Latch, then Fable reads the first shots.

**First, a build blocker (not mine to edit):** `text.schema.ts` now imports `./conversations.schema.ts`. `tools/sim/unity.ts` line 148 copies the schema's imports by a fixed list, so add `contracts/missions/handover/conversations.schema.ts` to it, or the engine build stops at the import. Aster has this too. Then `bun tools/game/words.ts` puts the words in StreamingAssets; I didn't run it.

## Where scenes live
`scenes.conversations`: `{ rule, names, items[] }`. Each item: `id`, `title`, `status`, `from`, `when`, `beats[]`, `learned[]`, `opens[]`. `scenes.items` is unchanged and still what `PlayScene` reads. `PlayText` doesn't load conversations yet.

## Picking a scene: at landing, never at the act's start
Port `lookupConversation(c, query)`. Every field a `when` names must equal the query's. No match means no conversation: the act uses its `scenes.items` scene.

| Query field | Read it from |
|---|---|
| `verb` | `listen`, `help`, `pressure`, `pay`, `call`, ... (the approach, not `turn`) |
| `about` | the role |
| `temperament` | `Person.Temperament` |
| `outcome` | `TurnOutcome(role)` |
| `turned` | `Person.Turned` when the act lands |
| `helped` | any `turn.help.*` log line for this role from an earlier tick (not this landing's own line) |
| `turnedBy` | the approach in the role's `turn.<approach>.turned` log line; scan the whole `Game.Log`, since `TurnOutcome` only looks back 40 lines |

| Act that landed | Scene |
|---|---|
| Listen, Inez, not turned | `listen-inez` |
| Help, Inez, `not-yet`, not helped | `help-inez-1` |
| Help, Inez, `turned`, helped | `help-inez-2` |
| Pressure, Inez, `turned`, not helped | `pressure-inez` |
| Pay, driver, `turned` | `pay-driver` |
| Call, Inez, turned by Help | `call-inez` |
| Call, Inez, turned by Pressure | `call-inez-pressure` (status `awaiting-sky`) |

The engine's outcome picks the Help visit, not a count of orders.

`lookupConversation` doesn't filter on `status`. Don't play `call-inez-pressure` before Keel lands the rule: its words say he runs back, and today's engine keeps him at her side. Aster decides when it goes on.

## A scene's lines, in order
`beats[]` in file order; port `conversationLines`. `speaker` is where the bubble goes, `names[speaker]` is the name it shows (Inez, Okafor, Zhou, Milena, Scout, Fixer). Everywhere else the game still says Guard and Driver. There are no variants, refs or picks, so don't call `SceneUse`.

- **Listen:** frame the Scout and Inez. `milena` is a voice through the café door with no figure, so `ConvoHead` gives null: draw her bubble at the café door. Glyph owns how it looks.
- **Both calls:** three speakers across a square. Today's intercut knows KeyA and KeyB only, so the guard's line would keep Inez's shot. It has to cut to whoever speaks, the guard included. The Fixer has no one to face: frame him alone. He doesn't arrive.
- **Length:** up to 10 beats, each held to read (§10). The 3-line cap goes.

## The cards
`learned[]` and `opens[]`, at most 6 words each, are shown as written, on or after the last line. Glyph places them. A conversation has no `result`: use `learned` for the feed card's footer and keep the whole scene in the history.

## When a conversation played
It holds the reply and the outcome already, so at that landing silence that act's `reply`, `done`, `read` and `confirm` bubbles, and `guard-lured` for the call. The street's later lines still play; see open issues.

First time for each scene counts by conversation `id`, not by person (`convoMet` keys on target today). An act with no conversation counts by `sceneKey`, or by verb and target for Mislead.

## Falls back to today's lines
Same words as today, played whole at landing:
- Ask on anyone. Pay on Inez, Call after Pay or Ask.
- Listen on a turned Inez, or on anyone but Inez.
- Help `kept`. Pressure after a Help visit.
- Help or Pressure on the driver or guard. Pay on the guard.
- Mislead: there's no scene, and its ack and arrive are future tense ("A wrong turn, coming up."). At landing play its `done` line, then `rivals-misled`, both already in `dialogue`.

Today's `scenes.items` lines stay as bubbles, with no cutaway, for every act above and for the seven scenes too when §10 says no scene plays: Never, a repeat under First time, a fight or Alert 2+, a speaker down, gone or inside, another scene playing, freeze or target mode. A covered act's fallback carries Sky's old complaints until those lines are rewritten; Aster has the list.

An approach's fallback now runs past the reply: `ConvoStart` stops at `l.Reply` and needs to play all of them, or the answer goes missing.

## Second pass: arrival lines, Mislead, price
- **`street` on `call-inez-pressure`:** when the engine logs `guard.at-cafe` on a visit where Inez was turned by Pressure, play `street[0].beats` as ordinary speech bubbles at the two people, clock running, in place of the `guard-at-cafe` dialogue and city scene. Keel is adding the rule and his report isn't in yet: `guard.at-cafe` is today's code for him reaching her, and if Keel gives this visit its own code, tell me and I'll rename `on`. The stay is 5 s, so hers shows on arrival and his before he leaves; let his bubble follow him as he runs.
- **Look up the street lines by route, not by whether the cutaway played:** query with `verb: call`, `turnedBy`, so Skip, Never and First time still get them. Motive's "she remembers a Pressure" and `turnedBy: pressure` are the same set: Pressure is refused once she's turned.
- **Heard off screen:** the city scene plays only in sight, but these should always play. With no agent near, also show `street[0].beat` ("Inez warned him.") in the feed.
- **Hold it back:** `call-inez-pressure`, its `street` and its `price` stay off until Keel lands the rule and Aster says so. Today's `guard-at-cafe` lines cover every route until then.
- **Pressure's price line:** the item's `price.line` then replaces `price.on` (`actions.pressure.onTarget.waitress`). The times are written in; Motive wants them from the engine, rounded to 10 s, so add a read if Keel exposes one.
- **Mislead at landing:** `done` (Scout), then `rivals-misled` (rivals, then Scout's line from the same event if you want a third). Both are in `dialogue`, and no line changed.

## Proofs
`Proof067` (lines 252-254) waits for a scene on the act's first tick (`ConvoPending`), so it moves with this. Proofs that set conversations to Never don't notice. A `text.json` change selects sections `done` and `r083` (`tools/game/proofdelta.test.ts`).

## Done-check
Route A shots show `help-inez-1` then `help-inez-2` word for word, cards included. A Pressure route shows `pressure-inez`. A start with `temperament.waitress: mercenary` plays `scenes.items`. The same seed and orders give the same hash with scenes Always, First or Never.
