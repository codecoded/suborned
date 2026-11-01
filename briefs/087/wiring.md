# 087 · Wiring spec for Latch (Chime, 2026-11-01)

Inputs: `briefs/087/cues.json` (28 cues: trigger, file, levels, group, voices, spatial), `Resources/Audio/087/*.wav` + `.meta`. Nothing here is wired yet, and `contracts/audio/cues.json` is unchanged, so the build you have keeps playing what it played.

Load with `Resources.Load<AudioClip>("Audio/087/<id>")`.

## 1. Rules that keep replays exact
- Sound reads and never writes. Everything hangs off `Drain()` (`HandoverHost.cs:351`) and view frames. No audio code calls an order or a step, or reads a sim random stream.
- No `UnityEngine.Random`. Any variation (pitch ±3 %, stagger, loop start) comes from `Streams.Fnv1a32($"{id}|{l.Tick}|{by}")`, as the engine loop already does.
- Cooldowns may use `Time.unscaledTime` (as `AudioLog` does): they change only what's heard.
- `CuesPlayed` keeps growing for the proofs. It records what the log asked for, so it must not depend on voice caps or on whether a clip loaded.
- Batch or `-nographics` runs: skip the clip loads and sources, keep `CuesPlayed`.
- Done-check for Suture: the closing-hash harness gives the same hash with sound on, sound off, and scenes Always/Never.

## 2. Bugs to fix on the way in
1. **The listener never turns.** `RivalAudioFrame` sets `listener.position = focus` and leaves its rotation alone, so 3D left and right ignore the camera. The Records Office front plays at yaw 210, where they come out reversed. Each frame, set the listener's rotation to the camera's yaw (level, no pitch).
2. `AudioLog` returns early on `Game.Range`. Let the **fight group** through so Sky can judge the voices in the fight range next to the flash and jolt (brief 084). The rest stays off there.
3. `PlayOneShot` on the single `cueSource` can't place or pan a voice. Use a pool (section 6).

## 3. What fires each cue

| Cue | Fires on | Plays as |
|---|---|---|
| `cafe_bell` | log `guard.at-cafe` | 2D. The bell means the door's open for you |
| `cafe_door` | log `guard.leaves-cafe` | 2D. The door means the window's closing |
| `handover_chime` | `Game.Tick` crosses `Game.T(S.Chime)` (`Site.cs:131`; there's no log line), once a run | 2D, over everything but a cutaway |
| `notice_1` / `_2` / `_3` | logs `attention.noticed` / `attention.wary` / `attention.reporting` | 2D. Several on one tick: play only the highest. Cooldown 0.5 s per stage |
| `camera_click` | the patrol's `turn-at-a` / `turn-at-b` from `Game.Routine("patrol", horizon)`: call once a second, keep the next tick, play when it passes | 3D at the patrol. `Routine` returns null until the routine is watched, so an unwatched patrol stays silent |
| `patrol_loop` | the patrol's view, while it's up and not chasing | 3D loop on the view, as `rival-car-engine`. Start offset by hash |
| `patrol_chase` | `PatrolChasing` or `patrol.alarm` until `patrol.sweep`, `patrol.returns`, `patrol.resumes` or `patrol.stands-down` | Same source, 0.3 s crossfade. A patrol fooled by `patrol.rerouted` keeps `patrol_loop` |
| `patrol_break` | logs `patrol.breaks`, `patrol.alarm` | 3D-far at the patrol, on the tick the loop changes |
| `police_siren` | log `alarm.dispatches` (cooldown 20 s) | 2D with pan from the first new `Unit.From`: `clamp(dot(cameraRight, from - focus) / 40 m, -0.8, 0.8)`, recomputed each frame while it rings, so turning the camera moves it |
| `fire_*` | every `shot.hit`, `shot.miss`, `shot.covering`, at the shooter (voice map below) | 3D-fight, with the flash in `Shot()`. Covering fire's words differ, so parse the shooter from `lays covering fire on` too |
| `hit_body` / `hit_metal` | `shot.hit`, by target: `patrol`, `robot-*`, `camera:*` or a car takes metal, others body. Also `car.hit` (metal) | 3D-fight at the target, with the spark |
| `miss` | `shot.miss` | 3D-fight at the puff |
| `down_body` | `fight.down`, `guard.down`, `security.officer-down`, `rivals.down`, `civilian.down`, `enforcer.down`, and `response.down` when the id is `police-*` | 3D-fight at the body |
| `down_robot` | `patrol.down`, and `response.down` when the id is `robot-*` | 3D-fight. `security.down` (a whole unit) plays nothing |
| `cut_in` / `cut_out` | `ConvoCut()` / `ConvoRestore()`, on the 0.12 s dip | 2D, unscaled time |
| `room_tone` | while `ConvoOn` | 2D loop, fade in over the dip, out over 0.4 s |
| `card_new` | a `PersonCard` with `Opens.Count > 0` first shows (`PersonCardNow` changes) | 2D |
| `street_bed` | from Begin to the end screen | 2D stereo loop. Keeps playing when paused, down 6 dB |
| `foot_concrete_*` | optional, **off by default**: left on one step, right on the next, for the selected agent only | 3D-near. Try only after Sky has heard the rest |

Please check the `down_*` list against the code. I read it from `GameCombat.cs` and the fight-lab list, not from a run.

**Weapon voices.** Read the shooter's `Gun.W` where it exists rather than the name, because the range lets loadouts change.

| Shooter | Voice |
|---|---|
| Carbine in hand | `fire_carbine` |
| `scout`, `fixer`, `tech`, or the Enforcer with a sidearm | `fire_pistol` |
| `guard`, `police-*`, armed civilian | `fire_police` |
| `rival*` | `fire_rival` |
| `patrol`, `robot-*` | `fire_robot` |

**Volleys.** Shots land in beats (the engine runs 10 ticks a second), so a volley would stack on one sample. Delay each voice 0 to 40 ms by hash and pitch it ±3 % by hash. Past 6 shots on one tick, drop the farthest.

## 4. Groups and Options
The buses are in `cues.json`. They stay named gains (`Bus()`), with the additions below.

| Bus | dB | Options slider | In a cutaway |
|---|---|---|---|
| clock | 0 | Effects | 0 |
| danger | 0 | Effects | -18 |
| fight | -3 | Effects | -18 |
| convo | 0 | Effects | 0 |
| ambience | 0 | **Ambience (new)** | -14 |
| vehicles | -3 | Effects | -18 |

Options today: `Settings.cs:148` applies Master only. Effects, Music and Voices are saved and do nothing.
- Apply Effects to the buses above.
- Add `ambience` (default 60) to `AudioS` and a slider to `FrontEnd.Panel.cs`.
- Leave Music and Voices alone until they have content; dim them if you think an inert slider misleads.
- Keep `rivals-coming` muted (Sky, 2026-10-26). It isn't in this pack.

**The cap on the sum.** If every voice cap hit its peak on one sample at nearest distance, the sum would reach +22 dBFS, and a named-gain mix has nothing to stop that. The scripted minute peaks at -2.6 dBFS, but it's only one script. Choose one, and tell me which:
- **Preferred:** an AudioMixer asset with a Compressor on Master (threshold -6 dB, ratio 4:1, attack about 5 ms, release about 100 ms), with the bus gains exposed on its groups.
- Otherwise: limit the fight group to 6 live gunshots and turn the fight bus down.

## 5. Ducking
All view-only, moved with `Mathf.MoveTowards` on unscaled time.
- **Conversation.** The clock is held, so no new sim cues arrive. Over the 0.12 s dip, take each bus to its cutaway value and bring in `room_tone`. On the cut back, return over 0.4 s. Tails already ringing duck with their bus. Moving loops stop tracking speed while the clock's held; hold their pitch.
- **Danger over the fight.** For the length of `police_siren`, and for 0.5 s after any `notice_*`, take the fight bus down 4 dB. Without this the siren's strongest bands only tie with steady pistol fire (0 dB, see `mix.json`).
- **Pause.** Sim loops pause with the clock, as the engine loop does. The bed stays, down 6 dB.
- **Capture clip.** Leave its foley muted. Its level is -44.7 LUFS with a -17.6 dB peak, below the bed's -38, so it would need about +15 dB and a duck to be heard. Sky can ask if he wants that.

## 6. Performance (60 fps limit)
- Pool 16 2D and 3D `AudioSource`s, made in `AudioInit`, never `AddComponent` per shot. Group caps are `voices_max` in `cues.json`; when a cap is hit, steal the oldest.
- Load every one-shot in `AudioInit` so the first shot doesn't hitch. Don't call `Resources.Load` per shot as `AudioLog` does now.
- `.meta` settings are in the files, all Vorbis:
  - one-shots: Decompress On Load, q 0.7
  - chime, siren, patrol loops: Compressed In Memory, q 0.6
  - bed and room tone: Streaming, q 0.45, 32 kHz
  - all mono except the bed
- Listen at the loop points of `street_bed` and `room_tone` in a build. The seams measure clean as WAV; Vorbis decoding is untested. If one clicks, set that clip to ADPCM.

## 7. Moving the data
When your build is done, tell me and I'll move the cues from `briefs/087/cues.json` into `contracts/audio/cues.json` with a new schema, `clips.json`, `tools/audio/check.ts` (it requires PCM today) and the `HostAudio` defaults you need. Doing it now would start cues firing in your build.

## Done-check
Sky hears each cue in the running game, and the 087 done-check is met:
- peak and loudness measured from the running mix
- the hash test in section 1
- frame time unchanged at play zoom

Next owner: Latch, then Suture, then Sky's ears.
