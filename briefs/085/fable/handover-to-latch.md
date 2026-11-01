# 085: conversations and the pressed call, Fable to Latch

Inputs: `contracts/missions/handover/text.json`, `conversations.schema.ts` beside it, engine 0.38.0, GDD v0.36.0 §10 (look C). Next owner: Latch, then Fable reads the first shots. Run `bun tools/game/words.ts` to put the words in StreamingAssets; I didn't run it.

The look and placing of the clip, insets and field are Glyph's: `briefs/085/glyph/capture-and-cues.md`. This note covers the words and what triggers them.

## Where scenes live
`scenes.conversations`: `{ rule, names, items[] }`. Each item: `id`, `title`, `status`, `from`, `when`, `beats[]`, `street[]` (optional), `learned[]`, `opens[]`. `scenes.conversations` also has `ui`, below. `scenes.items` is unchanged and still what `PlayScene` reads.

`status` is a record of what Sky has read, and nothing is gated on it. Don't read it in the host. `awaiting-sky` means it plays and he hasn't read it yet.

**New words this round:** `listen-inez`, `help-inez-2` and `call-inez` are Sky's agreed warmer versions, and `help-inez-2` now has 9 beats. A live run for the other four scenes is still to come, so those words will change too. Don't pin any scene's lines in a proof.

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
| Call, Inez, turned by Pressure | `call-inez-pressure` |

The engine's outcome picks the Help visit, not a count of orders.

## A scene's lines, in order
`beats[]` in file order; port `conversationLines`. `speaker` picks the live face, `names[speaker]` is the name it shows (Inez, Okafor, Zhou, Milena, Scout, Fixer). Everywhere else the game still says Guard and Driver. There are no variants, refs or picks, so don't call `SceneUse`.

- **Listen:** `milena` is a voice through the café door with no figure, so she has no live face. Show `ui.voice.milena` ("Milena, inside") in place of her name. Glyph owns what sits in the face's place.
- **Both calls:** three speakers across a square. Today's intercut knows KeyA and KeyB only, so the guard's line would keep Inez's shot. It has to cut to whoever speaks, the guard included. The Fixer has no one to face: frame him alone. He doesn't arrive.
- **Length:** up to 10 beats, each held to read (§10). The 3-line cap goes.

## A line's view-only fields
They apply only to the cutaway. Street lines and fallback bubbles ignore them and draw the plain `line`. Nothing the engine records reads them, so the hash can't move. `PlayText.Conversations` needs to read them onto `ConvLine`.

| Field | How it reads |
|---|---|
| `lean` | That one word in Barlow SemiBold, which is already in the kit, and the rest of the line as usual. The schema holds it to a whole word, once in the line. No HUD code uses rich text yet, so try it there first |
| `holdBefore` | Seconds of the shot with no line. In `ConvoStep`, `BeatAt = now + holdBefore`; `ConvoHud` already waits for `BeatAt` and `ReadS` counts from it. A click shows the line at once |
| `holdAfter` | After the read time or a click, seconds of the shot with no line, before the next beat or the cut out. A click ends it |
| `holdOn` | Only with `holdAfter`: the speaker the camera is on during the hold, framed alone as a call's cut does |
| `revealAfter`, `revealS` | Show `revealAfter`, the line's start, then the whole line `revealS` later. Size the plate to the whole line from the start, or it jumps. The read time grows by `revealS`. The first click completes the line, the second advances |

All seven uses, by scene and 1-based beat:
- `listen-inez`: 5 `lean` handsome; 7 `lean` did, `revealAfter` "He said nothing, you goose.", `revealS` 0.9.
- `help-inez-2`: 6 `lean` Okafor; 8 `lean` did.
- `call-inez`: 3 `holdBefore` 1.2; 6 `lean` you; 8 `holdAfter` 1.5, `holdOn` waitress.

`ConvoSaid` reads empty in a hold and partial in a reveal, so `Proof085Scenes` has to wait for the whole line.

## The cards and look C's words
`learned[]` and `opens[]`, at most 6 words each, are shown as written. GDD v0.36.0: they come after the cut, in play, on the person, not in the scene. Glyph places them. A conversation has no `result`: use `learned` for the feed card's footer and keep the whole scene in the history.

| Where | Words |
|---|---|
| The card's header | the person's `names` entry, as the `when.about` role: Inez, Zhou |
| Beside the `opens` rows | `ui.new`: "New" |
| A voice with no figure | `ui.voice.milena`: "Milena, inside" |
| Skip | the keycap's "Skip", as built |

Glyph's mocks also have "What the Scout heard", "Heard" and "You can now". Look C doesn't use them, and they aren't in the file.

## When a conversation played
It holds the reply and the outcome already, so at that landing silence that act's `reply`, `done`, `read` and `confirm` bubbles, and `guard-lured` for the call. The street's later lines still play.

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

Mislead at landing: `done` (Scout), then `rivals-misled` (rivals, then Scout's line from the same event if you want a third).

## The pressed call: live on engine 0.38.0
A pressed Inez's call plays `call-inez-pressure`, and today's `guard-at-cafe` lines don't play on that visit. Glyph's section 2 says how the off-screen lines show.

There's no new log code. Both codes below also fire on an unpressed visit, so read `Game.GuardCallPressed` with them.

| Moment | Read | Words |
|---|---|---|
| He reaches her | `guard.at-cafe` while `GuardCallPressed` (`street[0].pressed` says so in the data) | `street[0].beats`: hers on arrival, then his, as bubbles with the clock running. They replace the `guard-at-cafe` dialogue and its city scene, the Scout's line included |
| No agent near | same | also `street[0].beat` ("Inez warned him.") in the feed |
| He turns back, 5 s later | `guard.leaves-cafe` while `GuardCallPressed` | the Scout's `guard-leaves-cafe` line only. Silence the guard's: "Break's over" is wrong here. His street line may run on and follow him |
| The run | `Game.GuardRunningBack` | the run clip. Motive: the phase stays "returning to door" |
| Back | `guard.back` | today's `guard-back` lines |

- **Find the street lines by route, not by whether the cutaway played:** query `verb: call`, `about`, `temperament`, `turnedBy`, so Skip, Never and First time still get them.
- **Always played:** the city scene plays only in sight, but these play in sight or not.
- **Pressed, but no scene matches:** only a lab start with another temperament. It keeps today's lines, whose "Stand with me a while" is wrong on a 5 s visit. Not reachable in the shipped game, as the site casts her fearful.
- **An alarm during the 5 s:** `guard.recalled` fires, `guard.leaves-cafe` doesn't, and `GuardRunningBack` stays false, because he goes at a guard's pace. Drop his street line if it hasn't shown, and play the `guard-recalled` lines.
- **An alarm on the run back:** nothing happens. `guard.recalled` doesn't fire, so no line changes.

## Pressure's price
- **The Alt line** is live in the file: `actions.pressure.onTarget.waitress` holds the text. It's written in, not filled: 1:40 and 3:00 need no read. `price` is gone from the scene and from the schema.
- **The field:** `less.price.fields.door`, text `Door {clock}`, no icon. It sits in `less.price.order` after `trail`, and in `verbs.pressure` and `verbs.call`. Today's `switch` in `HudModel.PriceLine` skips it until you add a case. Add `door` to `HudModel.FieldOrder` after `trail`, or the panel's columns drop it.

How it fills:
```csharp
var w = g.GuardCallWindows(o.Target);                     // null: hide the field
if (o.Do == "call" && !g.PersonByRole(o.Target).Has(g.R.SummonPressedAct)) break; // a Help or Pay route shows nothing
int s = (w.Value.pressed + 50) / 100 * 10;                // ticks at 10 a second, to the nearest 10 s: 989 reads 100 s
var clock = $"{s / 60}:{s % 60:00}";                      // 1:40
```
- **Null covers the hiding:** any role with no lover, the guard down or busy, a call already made, Alert above the call's limit. Help shows no field, as it isn't on `verbs.help`.
- **It moves with the walk:** it reads the route from where he stands, so read it for the card and don't cache it across a mission. It hashes nothing.
- **Check against pill width:** Glyph's done-check, the 2x crop of the Inez ring.

## The alarm's walk back
On other routes, `guard.recalled` still fires and he walks at 1.4 m/s. The Scout's two lines no longer say he's running: "The guard's going back to his door." and "Alarm called him back. He's on his way."

## The capture clip's plate
`scenes.clip`: `service` "Service door", `public` "Public door" when the service door is held (`g.DoorHeld(g.ExitDoor)`), `both` "Doors held". Nothing else goes on the clip.

## The lock reason
`locks.target-unknown.short` is now "Can't act on that". I searched `unity/Assets/Suborned/Runtime` and `tools/game`: no proof holds "Not targetable". It's still in `unity/Assets/StreamingAssets/sim/text.json` until `words.ts` runs, and `tools/levels/dialogue/drift.json` names it in an old cause.

## Proofs
`Proof067` (lines 252-254) waits for a scene on the act's first tick (`ConvoPending`), so it moves with this. Proofs that set conversations to Never don't notice. A `text.json` change selects sections `done` and `r083` (`tools/game/proofdelta.test.ts`).

## Done-check
- Route A shots show `help-inez-1` then `help-inez-2` word for word, and the cards after the cut on Inez, with New.
- The three held moments read on screen, at `call-inez` 3 and 8 and `listen-inez` 7, and a click during each behaves as the table says.
- A Pressure route shows `pressure-inez`, then `call-inez-pressure`, then the two arrival lines at `guard.at-cafe`, and the guard's bubble follows him on the run.
- A Help route at `guard.at-cafe` still shows today's lines.
- The Inez ring reads `Pressure · 25 s · +3 · Door 1:40`, with the eye and mark after. The Call way shows the field only after a Pressure.
- A start with `temperament.waitress: mercenary` plays `scenes.items`.
- The same seed and orders give the same hash with scenes Always, First or Never.
