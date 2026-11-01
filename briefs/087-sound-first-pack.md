# 087 · Sound: the first pack

Owner: Aster. Opened 2026-11-01. Sky: "What can we for sound effects?", then yes to a first pack of about 20 sounds that help play, from free libraries plus a small generated batch, with up to $10 on generated sounds.

## Objective
The game sounds like this world and the sound helps you play: timing, danger, fight feedback and the conversations, per GDD §10 "audio = clock" and Chime's §14.

## Done-check
- About 20 cues in the game behind one mix, each with its source, licence and cost on record. Spend within the $10 pass, every call through `tools/paid/paid.py`.
- No licence that blocks a commercial game. The licence read from the source's own page, kept with the sound.
- Peak and loudness measured per cue and for the mix in the running game. Nothing masks a danger cue; ambience stays under speech-free play.
- A short recording Sky can watch on his phone, then his ears decide. Chime can't hear, so measured levels only rule things out.
- Replays stay exact: sound reads the sim and changes nothing it records.
- 60 fps limit holds: voices capped, streaming or compressed clips where it matters.

## Owners
Chime (picks, makes, mixes) · Latch (`HostAudio.cs` and hooks, after his current build) · Sky (his ears).
