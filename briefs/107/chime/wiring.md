# 107 · Weather and night sounds: wiring (Chime → Latch)

Sky, 2026-11-11: "go with your recommendations, though rain needs to be light background noise". The clips are in the game's files. You only wire playback. Listening page: `briefs/107/chime/weather-sfx.html`.

## What's already done
- Clips with their `.meta` (import settings as in the 087 pack): `unity/Assets/Suborned/Resources/Audio/107/`. Load them with `Resources.Load<AudioClip>("Audio/107/<id>")`.
- Rows: `contracts/audio/weather.json` (schema `contracts/audio/weather.schema.ts`, checked by `tools/audio/weather.test.ts`). They use the same fields as `briefs/087/cues.json`, plus `when`, `role` and `place`.
- `tools/sim/unity.ts` now appends these rows to `StreamingAssets/sim/sound.json`, so `PackInit` loads them with the pack (47 cues). `sound.json` is unhashed and view only, so the engine and replays don't move.
- Weather and light come from the start state (engine 0.54.0: `Game.Weather` is clear, overcast, rain or mist; `Game.Light` is day or night). They're fixed for the mission, so every sound starts at Begin and stops at the end screen, as `street_bed` does.

## Which clip plays when
| Clip | Plays when | 2D / 3D | Where | Level (per clip, gain 1) |
|---|---|---|---|---|
| `night_bed` | light = night | 2D stereo loop | on `bedSource`, **in place of** `street_bed` | −40 LUFS (2 dB under the day bed) |
| `rain_bed` | weather = rain | 2D stereo loop | a second 2D source beside `bedSource`, **on top of** whichever bed plays | −48 LUFS |
| `rain_awning` | weather = rain | 3D loop, `3d-near` (8–45 m) | each café awning and parasol cluster; use the 4 nearest the camera's ground focus | −48 LUFS |
| `drips` | weather = rain | 3D loop, `3d-near` | eaves and awning edges, a few per block; the 4 nearest | −46 LUFS at the loudest drip (−53 overall) |
| `wet_step_1`–`4` | weather = rain, *optional* | 3D one-shot, `3d-near` | at the walker | −37 LUFS each, the same as `foot_concrete` |
| `night_tram` | light = night, *optional* | 2D one-shot, about 11 s | listener | −40 LUFS |

All of them are on the **ambience** bus. They follow its Options slider, its −14 dB duck in a conversation and the bed's pause halving, the same way `street_bed` does in `PackLoops`.

Rain at night means `night_bed` plus `rain_bed`. Mist and overcast add no sound.

## Details
- **Levels are final.** Keep `volume` at the bus gain and move nothing per clip. With the street bed unchanged, rain near a café reads −37.3 LUFS against the dry day's −38.0, and −36.6 standing under an awning. The siren's margin over the rain is 16.9 dB (the 087 bar was 21 dB over the dry bed).
- **Awning and drip loops:** give each its own sources (4 + 4), not the 16-voice pool, as the patrol loops have. When the nearest set changes, move a source with a crossfade of about 1 s. Set each loop's start time from a hash, `Streams.Fnv1a32($"rain_awning|{siteId}") % samples`, so neighbours never play in step. As in the 087 pack, no `UnityEngine.Random`.
- **Where the sites come from:** awnings and parasols are kit pieces on the café frontages (the civic block's café and Vela's terrace props). Eaves are the building edges over the pavement. If the world doesn't list them yet, start with the awnings only and add drips at the same sites. That's fine for a first build.
- **Wet steps:** the dry footsteps (`foot_concrete_1`/`_2`) aren't played anywhere today. Wire the wet ones only if footsteps get wired: in rain, play `wet_step_1`–`4` in their place, chosen by hash, with ±3 % pitch from the hash.
- **Far tram:** a 2D one-shot at night every 40–90 s, the gap taken from `Fnv1a32($"night_tram|{n}")`. Its row's 40 s cooldown is the floor.
- **Seams:** every loop measures clean as WAV (the test checks it). As in the 087 pack, the loops are Vorbis and nobody has listened at a Vorbis loop point yet. If one clicks in a build, switch that clip to ADPCM.

## Done-check
- `bun test tools/audio/weather.test.ts` passes (clips, hashes, `.meta`, seams, rain at least 8 dB under the street bed).
- The r087clips proof still shows every clip loaded (it now counts 47).
- In a build:
  - With rain, the second bed source plays `rain_bed`.
  - At night, `bedSource.clip` is `night_bed`.
  - With clear weather by day, nothing has changed.
- A 30 s capture by day in rain and one dry, at the same Options, for Chime to measure: the rain capture should come out within about 1 LU of the dry one.

Next owner: Latch → Suture checks it in play.
