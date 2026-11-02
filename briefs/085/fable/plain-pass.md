# Plain pass: lines that needed decoding

Fable, 2026-11-02. Sky asked about Milena's line ("wtf is this line 'Is it so handsome?'"), and I read all seven scenes and the overheard items for lines a player would have to decode. Sky read the list and said: **"plain version is better"**. Every rewrite below is now in `contracts/missions/handover/text.json`.

**Scene status after the pass:**
- **Agreed, with "plain pass, Sky 2026-11-02" in `from`:** help-inez-2, pay-driver, call-inez, call-inez-pressure.
- **Still awaiting Sky:** listen-inez. Its new line 5 wasn't on his list.
- `briefs/085/fable/approved.json` holds the agreed words. For listen-inez it keeps the last version Sky agreed.

## Not on Sky's list (awaiting his read)
- **listen-inez, line 5.**
  - Before: "You'd get up to look at that door. Is it so handsome?" (`lean: handsome`)
  - After: "You'd only get up to watch the guard on that door." (`lean: guard`)

## The seven scenes
1. **listen-inez, line 3.**
   - Before: "I owe them too. We'll be buried with those cards half stamped."
   - After: "I owe them too. We'll still be paying when we're old."
2. **listen-inez, line 4.**
   - Before: "Then bury me somewhere I can sit down."
   - After: "I'd settle for five minutes sitting down."
   - It still mentions sitting down, so line 5 ("You'd only get up…") still answers it.
3. **listen-inez, line 9.**
   - Before: "Not from me, they won't. Stop looking, though. You can't afford it."
   - After: "Not from me. Stop looking at him, though. People will notice."
   - "they won't" was cut so the line stays within 12 words.
4. **listen-inez, line 7.**
   - Before: "He said nothing, you goose. You just did."
   - After: "He said nothing. You just told me yourself."
   - The leaned-on word moved from `did` to `yourself`. The reveal now shows "He said nothing." first, with the same 0.9 s pause.
5. **help-inez-2, line 2.**
   - Before: "Not so loud. I've owed them since my last good shoes."
   - After: "Not so loud. I've owed them for years."
   - The Fixer's reply changed with it. Before: "Then buy shoes first, Miss Nikolaou. We'll wait our turn." After: "Then take a breath first, Miss Nikolaou. We'll wait our turn."
6. **help-inez-2, line 8.**
   - Before: "You did bring the rest. I'll say it won't go by telephone."
   - After: "You kept your word. I'll tell him to come over in person."
   - The leaned-on word moved from `did` to `word`.
7. **pay-driver, line 3.**
   - Before: "Easy. I won't ask who's we. I'd only have to remember it."
   - After: "Easy. I won't ask who you are. Better I don't know."
8. **pay-driver, line 7.**
   - Before: "Been paid for 'not long' before. Couple of minutes, then I drive."
   - After: "I've heard 'not long' before. A couple of minutes, then I drive."
9. **call-inez, line 4.**
   - Before: "Long. This door's said nothing all day. It's good to hear you."
   - After: "Long. Nobody's come to the door all day. Good to hear you."
   - Trimmed to stay within 12 words.
10. **call-inez-pressure, line 4.**
    - Before: "It's me, love. How long till the change?"
    - After: "It's me, love. How long till your shift ends?"
11. **call-inez-pressure, line 7.**
    - Before: "Can it wait till I'm off? I've this door till the change."
    - After: "Can it wait till I'm off? I've this door till then."
    - The listed rewrite, "…till my shift ends", came to 13 words and repeated "till I'm off", so the line ends "till then".

## Overheard lines (`scenes.items`)
11. **The guard's coffee** (watch guard and listen guard, loyal).
    - Before: "Coffee before the shift ends. Regulations." / "Regulations. Coffee before the shift ends." / "Regulations. Of course. Sit." / "Regulations. Of course."
    - After: "Coffee before the shift ends. Same as always." / "Every day. Sit."
12. **Councillor, loyal.**
    - Before: "And the strangers in the stills?"
    - After: "And the strangers in the camera photographs?"
13. **Councillor, mercenary.**
    - Before: "Which is why I'm still here." / "And why I'm still here, sir."
    - After: "My price keeps me in this office." / "And mine keeps me quiet, sir."
14. **Every opening to Inez** (10 lines).
    - Before: "Hello. Is this about the files?"
    - After: "Hello. What can I get you?"
15. **Ask lines** (7 of each).
    - "I have a small favour. Nothing in return." → "Could you do me a small favour? I can't pay."
    - "Could you do us a kindness? Just ask." → "Could you do us a small kindness? I'm only asking."
    - "I'd like a hand with the car. Just ask." → "I'd like a hand with the car, if you're willing."
    - "Could you do us a favour? No reason." → "Could you do us a favour? I'm only asking."
16. **The idealist driver** (10 lines).
    - Before: "Yes? I'm listening. Fairly."
    - After: "Yes? Ask me straight and I'll listen."
17. **Help lines to the guard.**
    - "I can ease something for you, officer." → "I can help with what you owe, officer."
    - "A kindness, officer." → "Something toward what you owe."
    - "Another kindness, officer." → "A little more toward what you owe."
18. **Calls with a spill that never happens.**
    - "Oh! Your coat! Forgive me." and "Oh dear, coffee. Let me wipe that." → "Thank you. Stay with me."
    - "Clumsy of me. Sit, I'll fetch a cloth." and "Forgive me. Café rules: I clean it." → "Sit with me a moment."
    - "Whoops. The cloth is on the house." and "A spill. Sit, I'll see to it." → "You came. Stay a little."
    - My draft said "Stay a minute". The tests reject "a minute" in reachable lines, because the guard stays longer than that, so it's "Stay with me".
19. **Rivals.**
    - Before: "That's the Office. Same door as the file."
    - After: "That's the Office. The file comes out that door."
20. **The guard at handover.**
    - Before: "It minds me, more like."
    - After: "I know. It sweeps the yard."
21. **Crowd at the chime.**
    - Before: "Walk, please. Walking is encouraged."
    - After: "Walk, please. No running."

## Checks
- `bun test contracts/missions/handover`: 194 pass.
- `bun test tools/dialogue`: 75 pass.
- `bun tools/names/check.ts`: pass.
- `bun tools/game/words.ts`: pass.
