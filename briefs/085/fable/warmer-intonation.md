# 085: how the warmer scenes are spoken

Fable, for Sky's phone, then Glyph and Latch. The words are `warmer.json`'s, unchanged. Bold is the word it leans on. `/` is a beat. Flags are proposals; none is applied.

## listen-inez
Milena is a voice through the café door.

- **Milena:** Have you **eaten**? / You've looked at that Office door all shift. *(fussing; second half tossed away)*
- **Inez:** **Eat**? / With the Bureau after me? Waiting tables, I can't keep up. *(half a laugh, then running on, tired)*
- **Milena:** I owe them **too**. / We'll be buried with those cards half stamped. *(dry, comfortable, an old joke)*
- **Inez:** Then bury me somewhere I can **sit down**. *(deadpan, no pause, the laugh under it)*
- **Milena:** You'd get up to look at that door. / Is it so **handsome**? *(sing-song, enjoying herself)*
- **Inez:** Who's been counting? Did **Okafor** say something? Who else has he told? *(laugh gone, too quick, one breath)*
  - **Flag:** nobody counts anything since I cut "Fourth look", so "Who's been counting?" has nothing to answer. Change to: "Who says? Did Okafor say something? Who else has he told?"
- **Milena:** He said nothing, you goose. / You just **did**. *(soft and pleased, then gently)*
- **Inez:** Yes, I'm seeing him. / If the Office hears, it costs us **both**. *(low and flat, she's stopped moving)*
- **Milena:** Not from **me**, they won't. / Stop looking, though. You can't afford it. *(the tease gone, plain, kind)*
- **Scout:** Bureau loan, behind, could be cleared. / Okafor, Office door, could be **used**. *(flat, low, taking notes; the cold after the warm)*

## help-inez-2

- **Fixer:** That's the **rest** paid. / You owe the Bureau nothing, Miss Nikolaou. *(easy, unhurried, good news)*
- **Inez:** Not so **loud**. / I've owed them since my last good shoes. *(a hiss, a glance, then a tired laugh escapes)*
- **Fixer:** Then buy **shoes** first, Miss Nikolaou. / We'll wait our turn. *(smiling; the last four words light, that's the steel)*
- **Inez:** Thank you, I **mean** that. / Your turn. What do I owe? *(the thanks is real; then the smile stops)*
- **Fixer:** A **few** minutes. / When I telephone, call Okafor off the Office door. *(making it small; the name said as if ordinary)*
- **Inez:** **Okafor**. / You know about us, then. / What happens to him? *(the word dropped; then very small)*
- **Fixer:** Easy, now. / Nothing, **if** this goes right. And he never sees us. *(low and gentle; the "if" barely there)*
- **Inez:** You **did** bring the rest. / I'll say it won't go by telephone. *(to herself, slow, then decided)*
- **Inez:** I'm with you. / I watch that square **anyway**. Trouble, I'll warn you. *(small, rueful, offering)*
  - **Flag:** no word change, but two Inez lines in a row read as one breath. This one needs a short hold before it, as if he's waited.

## call-inez

- **Fixer:** It's **time**, Miss Nikolaou. / Call Okafor across now. You'll do fine. *(kind, no hurry, like a doctor)*
- **Inez:** **Now**? / With the terrace looking? Yes. All right. *(breath caught; the yes comes too fast)*
- **Inez:** How's your **shift**, love? *(long beat before; bright, ordinary, her best voice)*
- **Okafor:** **Long**. / This door's said nothing all day. It's good to hear you. *(dry and slack; the last part shy, unpractised)*
- **Inez:** Come **across**. / I've something to tell you, not down a telephone. *(light, a shade too light)*
  - **Flag:** the comma trips when spoken. Change to: "I've something to tell you, and not down a telephone." It's 12 words with "Come across".
- **Okafor:** I've this door till shift's end. / For **you**, it can wait. *(duty said for form, then giving way, smiling)*
- **Inez:** **Walk**, love. / Don't run. Let me keep you a while. *(nearly laughing; she means it, and that's the worst of it)*
- **Okafor:** Keep me as **long** as you like. *(happy, already on his way)*

## Reaching the player without voices

**What each one takes** (from reading the code; nothing has been run or seen on screen):

- **A held pause: the cheapest and the strongest.** `ConvoHud` already hides the bubble until `BeatAt`, and `ReadS` counts from it. So `ConvoStep` sets `BeatAt = now + hold`. The shot holds on the people with no bubble. A click during the hold shows the line at once.
- **A slower reveal: small.** The first part shows, then the rest joins it after a pause, in the same bubble. The bubble must take its full size from the start, or it jumps. A click completes it. The read time grows by the pause.
- **A stressed word: small, but Glyph's call.** The bubble is Barlow Medium, and the kit has no italic Barlow. Glyph picks either a real italic (Barlow is open-licence, so the file is free to add) or SemiBold, which is already in the kit. No HUD code uses rich text yet, so Latch has to try it. Use it sparingly, or stress means nothing: at most one word in a line, and these five only: **handsome**, **did** ("You just did"), **Okafor** (help-inez-2), **did** ("You did bring"), **you** ("For you").
- **A figure gesture: not this round.** My search of the clips by name found `idle`, `idle_alt` and `talk`, with no nod or look. §10 sets both people to idle with feet planted, so even `talk` on the speaker's beats is a §10 change for Sky. Milena has no figure anyway. A new clip is new work, and the pause gets most of the effect.

**What the scene needs:** three optional fields on a beat in `conversations.schema.ts`. They're view only, so the hash can't move. They apply to the cutaway only, not to street lines or fallback bubbles.

```json
{ "speaker": "milena", "line": "He said nothing, you goose. You just did.",
  "lean": "did", "revealAfter": "He said nothing, you goose.", "revealS": 0.9 }
```

- `lean`: the stressed word. It's a field because `Spoken` bans `<` and `>`, and because `line` must stay plain for the word-for-word tests. `lean` needs a rule for which "did".
- `holdBefore`, `holdAfter`: seconds with no bubble, before or after the line.
- `revealAfter`, `revealS`: the part shown first, and the wait before the rest.
- **`holdOn`** (optional, with `holdAfter`): who the camera is on during the hold.
- **Proofs:** `ConvoSaid` reads the bubble, so it reads empty in a hold and partial in a reveal. `Proof085Scenes` has to wait for the whole line.

**The three moments.** The seconds are starting values, for Sky to judge on screen.

| Scene, beat | Line | Use | Why |
|---|---|---|---|
| `call-inez`, 3 | "How's your shift, love?" | `holdBefore: 1.2` | The Fixer's gone, and she finds her ordinary voice. It's the betrayal, and Sky's own line |
| `listen-inez`, 7 | "He said nothing, you goose. / You just did." | `revealAfter`, `revealS: 0.9`, `lean: did` | The whole scene's reveal is three words late, and kindly |
| `call-inez`, 8 | "Keep me as long as you like." | `holdAfter: 1.5`, `holdOn: waitress`, before the cards | He's happy and walking. We stay on her, saying nothing, and then the card says the door's empty |

If those work, the next is `help-inez-2`, beat 6: a hold after "Okafor." using the same reveal.

**Next owner:** Sky picks. Then Glyph picks the stress face and the timing, I add the fields and values, and Latch builds them.
