# 107 · The briefing's weather and light: one calm control (Glyph, 2026-11-11)

Sky on 0.2.316: "that briefing page looks cluttered where the weather condition selector is". The six loose words on the kicker line ("Day · Night", then "Clear · Overcast · Rain · Mist") are gone.

**Now:** one small pill just left of the clock chip, on the clock's line, above the title. It shows the look as it stands: the sun or the moon, the weather's pictogram (none for clear) and one word ("Clear"), then a chevron.
- **A click** opens a small card under it with two rows: Day · Night, then Clear · Overcast · Rain · Mist. Each choice is a pill with its pictogram; the chosen one is filled deep teal with cream ink.
- **A pick** gives the same orders as before (`ChooseLight`, `ChooseWeather`) and closes the card. A click anywhere else closes it too. There's no key for it.
- **Night is hidden** where the scene can't draw it (`host.CanNight`), as before. Only before the clock runs, only on the start's Job page.
- **Place:** it collides with nothing. The pill sits 12 px left of the clock, and the title starts 16 px below its bottom edge. While it's open, the card lies over the title's end; it is a menu and closes on any click.
- **Style:** book colours only: paper, deep teal #1f6c67 with a 2 px edge, ink #17302f, soft ink for the chevron, pale teal on hover. Type is Jost SemiBold 19.
- **Pictograms:** the kit has no weather singles, so the sun, moon, cloud, rain and mist are painted solid with Painter2D, sized 22 px. They sit in their pills by flex centring inside the border, the shared rule's `Middle()` way (`HudToolkit.Centre.cs`).
- The words are still placeholders for Fable.

## Files
- `unity/Assets/Suborned/Runtime/Play/HudToolkit.Look107.cs`: rewritten (145 lines): the pill, the card, the pictograms, and `LookMenuPin` for stills.
- `unity/Assets/Suborned/Runtime/Play/HudToolkit.Book.cs`: `BookDraw` calls `BkLook(body, pg)` after `BkChrome`, so the pill can find the clock and the card draws on top.
- `unity/Assets/Suborned/Runtime/Play/HudToolkit.BookStart.cs`: `BkKicker` no longer draws the words.
- `unity/Assets/Suborned/Runtime/Play/ShotRunner.cs`: new shot field `lookMenu` (a briefing still with the card open).

## Evidence
- `bun run batch --sky`: **0.2.318** (c064, b9cb3db90+). Compile ok; host logic 203/203, including `Look107`. The smoke was ok: the briefing came up, play reached tick 40, the HUD drew, no exception.
- Stills, 2x crops of the page's top right, made the way Latch made his (the player's `-shots`, a briefing shot at 1920 × 1080, cropped x 960–1920, y 0–260, scaled 2x). The shot spec is `out/scratch/glyph/107/in.json`.
  - `job-look-closed-2x.jpg` / `job-look-closed.jpg`: the pill.
  - `job-look-open-2x.jpg` / `job-look-open.jpg`: the card open.
- **2x check:** nothing clips. The icons and the word sit centred in their pills, and the pill lines up with the clock chip's middle. The card's right edge is on the pill's.

## Open
- **No still of a night or rain pill.** A shot can't set the look yet; the code path is the same, with two pictograms then the word.
- **The open card was pinned by the shot, not clicked.** Click, close-on-outside and hover are untested in the app beyond the smoke.
- **My first `--sky` retry failed on someone else's file mid-edit.** `Runtime/Weather/NightLamps107.cs` (untracked, CS0165) was fixed by its owner within a minute, and the next run passed.
