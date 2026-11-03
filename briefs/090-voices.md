# 090 · Spoken voices: a first test

Owner: Aster. Opened 2026-11-02. Sky: "Do you think spoken vocices would work?", then "Yes" to a small paid test of up to $5.

## Objective
Find out whether build-time synthetic voices sound good enough for the Handover, by ear, before anything is wired.

## Done-check
- Two or three voice services, each with commercial-game terms read from the service's own pages and kept with the test.
- A cast for Inez, Arthur, Popovic, Lukac and the four of the cell (Larthi, Hasti, Sethre, Arruns), each voice suited to the person's portrait, age and background.
- The Listen scene (`listen-inez`, Milena and Inez) in full, and about ten short shouts ("Hold it!" and the like, from the game's own text), from each service.
- A recording for Sky's phone with the services side by side and unnamed, the key kept apart. His ears decide; measured levels only rule things out.
- Names said the same way every time (Larthi, Popovic, Lukac, Nikolaou).
- Spend within $5, every call through `tools/paid/paid.py` with its ledger.
- Nothing wired into the game in this round.

## Rules that hold
No AI at runtime: lines are voiced at build time and the game plays recordings. Sound never changes the run. No celebrity or real-person voice clones. Steam's AI-content disclosure will apply.

## Owners
Chime (picks, makes, measures) · Fable (lines, if any need a spoken pass) · Sky (his ears).

## Sky's pick (2026-11-02)
"Service B": Qwen3-TTS VoiceDesign (`alibaba:qwen@3-tts-1.7b-voicedesign`, Apache-2.0), voices built from written descriptions, no clones. Its known faults from the test: some word slips ("Yo, stop right there", "A car's", "Got on camera"), some male parts measured high in pitch (Sethre about 400 Hz, the rival about 310 Hz), 24 kHz output, and no pronunciation control (names respelled).

## Parked (Sky, 2026-11-02)
"hmm, let's park this. Not happy with consistency or range". Three rounds on Qwen3-TTS VoiceDesign, run locally after round one: design once then clone held a voice steadier than designing per line, but Sky heard it as not consistent enough, and the young women's voices crowded together (Inez, Milena, Larthi measured 0.97 to 0.99 alike); Milena never landed and the robot sat between options. What carries over when it opens: the line list (`briefs/090/chime/lines-handover.json`, 1,701 lines, about 100–140 heard in one run; a 794-line one-wording set in `chime/voiced-set.json`), the name respellings (`chime/say.json`), the take-picking by machine listener, the shout levels (-14 LUFS with the fight dipped 4 dB), and the terms read for Inworld, Qwen and Fish (`terms.md`). Spend: $0.27 of $5. The local model (about 10 GB) is in `out/scratch/chime/models/`.
