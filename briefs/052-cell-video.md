# Brief 052: the cell, a store-style 15 s teaser (concept)

**Objective:** a 15 s teaser of the cell with their props, cut like a console store page trailer. Sky, 2026-10-23: "generate a 15 second video, atmospheric, using our characters, give them props: the enforcer needs his gun, the scout her backpack, fixer her suitcase, tech his neon glasses and pipe lighting"; raised to "something that looks great as game opening / trailer / promo like you see on the Playstation Store".
**Owner:** Forma · **Next:** Sky picks (props, pipe reading, shots to re-roll); Chime scores it (the film is silent); then the props go to the GDD and the cell turnarounds only if Sky approves them.
**Done-check:** `teaser-15s.mp4` 1920x1080, 24 fps, exactly 360 frames (15.000 s), no text or HUD in the generations, title card composited by ffmpeg, every call in `PROVENANCE.json`, faces on-model in all four hero beats, spend under $40.
**Evidence:** `teaser-15s.mp4` (the film) · `teaser_contact.png` (a frame every 0.5 s) · `contact_sheet.png` (every generated clip, 5 frames each, cut shots outlined) · `prop_sheet.png` (the four with their props) · `keyframes/` (Seedream key frames, takes kept) · `shots/` (17 clips) · `PROVENANCE.json` (model, seed, prompt, refs, cost, sha256; rejections and the cut) · `gen.py` (generator), `edit.py` and `title.py` (the cut and the title, re-runnable) · `research/` (Runware docs used, with URLs).
**Cost:** **$14.61** logged (limit $40): 18 Seedream images $1.83 (prop sheet, pipe and neon portraits, shot key frames and re-rolls), 12 Hailuo clips $4.62, 5 Seedance clips $8.15. Two Seedance requests were refused by its moderation before any output and one never returned (worst case +$1.63 if billed; not confirmed on the invoice). The first Hailuo batch rendered at 768p ($0.28 each, the default without a size): those six were previews only; the film uses 1080p clips.

## The props (concept only until Sky approves)
`prop_sheet.png` (Seedream 5.0 Pro, cell lineup plus each person's crop as references; same poses as the turnaround lineup).
| Member | Prop | As drawn |
|---|---|---|
| Enforcer | his gun | a retro-future raygun in polished chrome and cream enamel: rounded body, finned barrel with a ribbed coil, black grip. Not a modern pistol. Holds shape across the film. |
| Scout | her backpack | a compact technical pack in ivory with black webbing, chrome buckles and a rolled top, riding high under the hood; matches her harness. |
| Fixer | her suitcase | a sleek hard case in matte black and ivory, rounded shell, chrome corners, slim chrome handle. |
| Tech | neon glasses, piping lit | slim wraparound frames of thin glowing cyan tube; the cyan piping on his wrap top, sash, trousers and bracers glows and lights his skin. The goggles come off his forehead. |

**Pipe: my reading is the lit piping on his clothes** (his costume already has cyan piping, and the glow is what carries the shot). If Sky meant him smoking a pipe: `keyframes/PIPE_t1.png` (a slim curved chrome pipe, a curl of smoke, neon glasses on) is made so Sky can pick; it is not in the film. `keyframes/NEON_t1.png` is the same portrait without it.

## Shot list (24 fps; one grade; hard cuts, a white flash from the lens flare into the reveal)
| # | Time | Frames | Shot | Source (model, window) | Move |
|---|---|---|---|---|---|
| 1 | 0.00–1.42 | 34 | Cold open hook: a chrome closed-circuit camera (1950s hood) swings its lens onto us, sun flares out | S0 Hailuo t2, 4.30 s on | push to the lens, flare whites the frame |
| 2 | 1.42–4.21 | 67 | World reveal: the cell walks across the civic square toward the Records Office at shift-change light, sky rail trams, harbour, atom-crowned tower | S1 Hailuo t3 (key frame S1b_t1), 0.30 s on | slow push forward and down between them |
| 3 | 4.21–5.79 | 38 | Scout hero beat, pack on her back, glances back | S2 Seedance t2, 0.70 s on | slow arc left |
| 4 | 5.79–7.38 | 38 | Fixer hero beat, case in hand, turns to look into the lens | S3 Hailuo t2, 1.40 s on | slow push in |
| 5 | 7.38–8.96 | 38 | Tech hero beat, glasses and piping blaze on | S4 Seedance t2, 0.40 s on | push in, rack focus to the face |
| 6 | 8.96–10.54 | 38 | Enforcer hero beat, low angle, checks the raygun across his chest | S5 Hailuo t2, 3.60 s on | push in |
| 7 | 10.54–12.54 | 48 | Tension: the Enforcer, gun low, looks across at the Records Office; guard at the door, councillor's car pulling up | S6 Hailuo t2, 0.00 s on | push past his shoulder |
| 8 | 12.54–15.00 | 59 | Title card on a darkened, blurred, slowly pushing frame of shot 7; lockup fades in at 0.35 s, whole card fades out the last 0.35 s | ffmpeg only (S6_t2.png, title.py) | slow zoom |

**Title:** Glyph's lockup from `briefs/047/layout/loading.json` (small tracked THE over SUBORNED, Jost, cream #f7f4ec, one 1 px brass #99784d rule the width of SUBORNED), drawn by `title.py` at 2.2 x the loading-screen size, centred, never generated. **Grade (edit.py):** contrast 1.05, saturation 1.07, teal in the shadows and warm highlights, a light vignette, fine film grain; titles card darker. Not letterboxed (a 2.39:1 version is one ffmpeg line if Sky wants it; it would crop the Tech's hair and the gun).
**Sound:** none. Seedance can make audio but the clips were asked silent; Chime scores it. The film has no audio track.

## Method
- **Key frames:** Seedream 5.0 Pro (`bytedance:seedream@5.0-pro`, 2560x1440) through the old store's Runware client, as `briefs/047/gen.py`. References: the cell lineup, one crop per person, the paintings and the Records Office (`refs/`). Prompts follow the style contract (`concept_prompt` city and avoid, the people couture line, no era date on the cell); full text in PROVENANCE.json. Take 2 of each hero frame asks for a near-empty square: it removes the hats and period dress the model puts on a crowd (brief 047's problem) and the smiley patrol robots.
- **Video:** two models, same key frames, kept per shot. Hailuo 2.3 (`minimax:4@1`) at 1080p, 6 s, $0.49, first-frame image to video. Seedance 2.0 (`bytedance:seedance@2.0`) at 1080p, 4 s, $1.63, first-frame mode.
- **Cut:** ffmpeg; windows chosen by looking at every clip.

## What worked and what did not
- **Seedance beat Hailuo on faces and prop continuity** in the two close hero beats. Scout: Hailuo drifted into profile and lost the face; Seedance keeps her facing us, face steady. Tech: Hailuo's neon glasses lit, then went out and vanished by the end of the clip; Seedance's lit up and stayed lit, the piping lights with them (the one real prop failure in all the clips). Seedance is native 1080p and costs 3 x Hailuo.
- **Hailuo beat Seedance on wide shots, cost and moderation.** The reveal, hook, Fixer, Enforcer and tension shots are Hailuo. The Fixer and Enforcer frames could not go to Seedance at all: its moderation rejected both as "may contain real person" (these are painted people; the Scout and Tech pass, the same way). Hailuo made no objection.
- **Hook camera:** the first take showed a modern box CCTV (the model's habit, noted in brief 047). Take 2 asks for a rounded chrome blimp hood with fins and a yoke; that is the film's.
- **Reveal:** the first key frame showed ordinary citizens as the "cell" (coral and navy dresses). Re-made with the lineup as a reference: the four in black and ivory from behind, props visible. The Scout's pack came out olive-grey at that size; fine at distance, not on-model in colour (the ivory pack reads in the hero beats).
- **Enforcer:** Hailuo throws the raygun into a vertical swing in the first second and into a club shape at about 3 s; the cut window (3.6 s on) is the clean "check the gun" moment. The barrel stays on-model.
- **Faces:** no melting in the windows used. Checked frame by frame on contact strips and on stills of every hero beat: Scout undercut, earpiece, hood; Fixer ponytail, halter, long gloves; Tech dreadlocks and beard; Enforcer grey beard and tinted glasses.
- **No cartoon robot eyes in the cut:** the patrol robot shows only tiny and far in two frames; none in a hero beat. S4 t1 and S2 t1 have robots with smiley panels at small size, which is why take 2 empties the background.
- **Crowd:** the reveal frame has many tiny citizens; at that size they read as bareheaded couture, no hats. A few distant ones in the Scout and Fixer shots are in plain skirts and jackets.
- **Stalled call:** one Seedance request (S6) never returned (as the Seedream call did in brief 047); I stopped waiting after 25 min.

## Choices for Sky
1. **Pipe:** lit piping (in the film) or a smoked pipe (`PIPE_t1.png`; would need a Tech hero beat re-rolled from that frame, about $2 with Seedance).
2. **Re-rolls on offer** (any, about $0.50 to $2): a second hook with the camera turning slowly; a cell-wide beat where all four glance up at the Office instead of the reveal's walk-away; a better Scout pack colour in the reveal (re-key-frame, $0.11 plus $0.49); the Enforcer in Seedance if its moderation passes a different crop.
3. **Alternate takes kept:** `shots/` holds every clip; `S4_hailuo_t1` is the Tech's glasses going dark (for the record), `S1_seedanceF_t3` is a Seedance reveal where the camera passes over the cell's heads (very good, but the cell leaves frame in the last second).
4. **Props to the GDD** only after Sky approves them.

## 9:16 crop plan for social (no spend now)
A 9:16 window of the 1920x1080 master is 608 x 1080 (31.7 % of the width), centred on the subject; x is the window's centre as a share of the frame width, from the frames in `teaser_contact.png`. Verify with a real pass when it is wanted.
| Shot | Window centre | Note |
|---|---|---|
| Hook | 0.40 | the lens; the flare fills the frame anyway |
| Reveal | not croppable | the four stand 43 % of the width apart. Either cut from a vertical key frame (Seedance first frame at 1080x1920, about $1.75, same prompt) or hold a 0.25 to 0.68 window at 1.35 x zoom out with a blurred fill |
| Scout | 0.52 | face and pack |
| Fixer | 0.40 | face and case together |
| Tech | 0.45 | glasses and glowing sash |
| Enforcer | 0.51 | the gun and the face |
| Tension | pan 0.22 → 0.58 | Enforcer to the Office and the car over 2 s |
| Title | centred | `python3 -I title.py out.png 1080 1920 1.9` (the same lockup, proportion kept) over the same darkened frame |

## Props reference (Sky, 2026-10-23)
Sky shared the cell's established props: `briefs/052/refs/sky-props-reference.png`, sha256 starting ec1196d00e4f4c84 ("This was the agents props"). This sheet sets the props, and the first-pass props are replaced:
- **Enforcer:** a long chrome rifle with round fittings, carried low at his side; not the cream raygun pistol.
- **Scout:** a black technical backpack with harness straps, worn on one shoulder.
- **Fixer:** a flat brushed-metal attaché case, carried low.
- **Tech:** a visor pushed up on his head (the "neon glasses", lit when worn) and cyan-lit piping on his wrap. That settles "pipe lighting" as the piping, not a smoking pipe.
- In this sheet the Fixer wears a long backless asymmetric dress; the approved turnaround has the short halter dress. Costumes stay on the turnarounds until Sky says otherwise.

---

# Pass 2: Sky's props sheet (2026-10-23)
**Deliverable:** `052/teaser-15s-v2.mp4` (1920x1080, 24 fps, 360 frames = 15.000 s, silent) · `052/teaser_v2_contact.png` (a frame per 0.5 s) · `052/contact_sheet_v2.png` (every pass-2 clip plus the kept hook, cut shots outlined) · `052/prop_compare_v2.png` (Sky's sheet left, the re-render right) · `052/keyframes/PROPS2_t1.png` and `V2S*` key frames · `052/PROVENANCE.json` (pass 2 entries marked `pass: 2` or named V2*) · `052/edit2.py` (the cut). v1 (`teaser-15s.mp4`) stays for comparison.
**Cost, pass 2:** **$8.96** of the $30 ceiling (17 Seedream images $1.78, 8 Hailuo 1080p clips $3.92, 2 Seedance clips $3.26). Running total for brief 052 $23.56. Three Seedance requests were rejected by moderation before output (no cost logged).

## What changed
- **Props from Sky's sheet** (crops in `refs/sky_*.png` fed as a third reference with the cell lineup and the person's crop; costumes from the turnarounds, the Fixer keeps the short halter dress): Enforcer's long chrome rifle with round fittings, low and vertical; Scout's black technical pack on one shoulder; Fixer's flat brushed-silver attache case at her thigh; Tech's visor pushed up on his head, then pulled down to light neon, with the cyan piping glowing.
- **Re-keyed:** the four hero beats, the tension shot and the reveal (`V2S1c_t2`: exactly four people, black pack, case, visor up, rifle). The hook is kept from pass 1: the camera does not clash with the props.
- **Flaws fixed:** the Fixer's case holds one size and shape for the whole shot (prompt pins the camera at a fixed distance, no zoom); the Scout's pack is black in the reveal and in her beat; the title card is now 3.4 x the loading-screen lockup, centred, 60 frames (2.5 s) with a 0.15 s fade in, a slow 3.5 % push and a 0.3 s fade out. Same Glyph lockup, ffmpeg only.
- **Shot list** (frames at 24 fps): hook 34 · reveal 67 · Scout 38 · Fixer 38 · Tech 38 (visor comes down, lights cyan) · Enforcer 38 (rifle held, looks up) · tension 47 (over his shoulder to the Records Office, car, guard, rifle low) · title 60. All Hailuo 2.3 at 1080p; Seedance's Scout take (`V2S2_seedanceF_t1`) is the same shot with more turn of the head and is kept in `shots/` as the alternate.

## Review notes
- Faces on-model in all four hero beats (Scout undercut and earpiece, Fixer ponytail and gloves, Tech dreadlocks and beard, Enforcer grey beard and tinted glasses). Props held shape in the used windows.
- The Tech's visor is pushed up, not on his eyes, as in the sheet; the neon comes when it is lowered. Hailuo lit the lens and piping strongly; the wide sleeve and bracer glow more than on the sheet (the sheet's piping is thin). Seedance moderation refused his frame again, so no comparison.
- The Enforcer clip: Hailuo drifts the rifle into a vertical staff in some takes (`V2S5_hailuo_t2` is the cleanest: it hangs low and long at his side through the used window). `V2S5_hailuo_t1` swings it up to his shoulder; `_t3` zooms into the rifle (a nice detail shot, not in the cut). The rifle's round fittings read in the key frame and the tension close-up; in the wide beat it is a long chrome barrel.
- Reveal: the Hailuo clip has the four walking, the camera pushing forward. The Seedance take (`V2S1_seedanceF_t1`) swoops over their heads and loses them; not used. The Scout's black pack reads clearly; the Tech's visor is small at that size.
- Citizens in the hero beats are in plain couture; a few in the Tech beat's background have conventional skirts and suits (small, defocused). One small robot is visible far behind the Tech at the start of his clip (not in the cut window).
- Grade: v1's, with the vignette corrected (my first pass of this edit darkened the corners; fixed and re-checked on the contact sheet).

## For Sky
1. **Pass 2 vs pass 1:** `teaser-15s-v2.mp4` is the film to judge. The costumes match the sheet; I did not copy its long Fixer dress.
2. **Visor:** Tech lowers it and it glows; if Sky wants it worn lit the whole shot, `keyframes/V2S4b_t1.png` is the key frame (visor down, lit) and can be re-rolled from for about $0.49.
3. **Still open from pass 1:** 9:16 plan (in the brief above), props to the GDD after approval, a second hook if wanted.

---

# Pass 3: live ending (2026-10-23)
Sky on v2: "all good until the end, as no NPC characters moving so looks odd." Only the ending changed; shots 1 to 6 are identical to v2 (same files, windows and grade).
**Deliverable:** `052/teaser-15s-v3.mp4` (1920x1080, 24 fps, 360 frames = 15.000 s, silent) · `052/teaser_v3_last5s.png` (last 5 s at 4 frames per second, 20 frames) · `052/edit3.py` · provenance in `052/PROVENANCE.json` (`pass: 3`, files `V3S6_*`). **Cost, pass 3: $5.54** of the $8 ceiling (3 Hailuo 1080p $1.47, 2 Seedance 5 s $4.07); brief total $29.10.
- **Tension shot (7, 47 frames):** re-rolled for background life. Five takes, picked frame by frame: `V3S6_seedanceF_t5` (Seedance 2.0 from the same key frame as v2) has the black-and-ivory car actually driving in from the right and stopping at the kerb, pedestrians crossing at several depths, the tram sliding across its arches and the guard at the door, with the Enforcer still in the foreground. It is the one in the film. Alternates in `shots/`: `V3S6_seedanceF_t4` (a back-on crop that cuts his head off: moderation passed, a good close shot, but the car turned into a low coupe), `V3S6_hailuo_t1` and `_t2` (people cross, the car leaves left, fountains appear), `V3S6_hailuo_t3` (a dense crowd in plain pale dresses, car lost).
- **Title (60 frames, 2.5 s):** no longer a still. It runs over the live tail of the same Seedance clip, graded, easing over 0.6 s into a lightly blurred (sigma 6) and darkened plate, so people, car and tram keep moving behind the lockup; same Glyph lockup, ffmpeg only. Motion checked frame by frame on the last-5-s sheet (pedestrians and the car move through the whole card).
- **Seedance moderation** passed both the back-on crop and the full frame this time (it refused the Fixer, Tech and Enforcer frames in pass 2). The Enforcer's face is in profile at the edge in t5.
- Weaker than the rest: the car slows to a stop in the title card's first half, so the title's motion is mostly the pedestrians and the tram; blur at sigma 6 keeps them readable.

---

# Pass 4: 30 s cut, v4 (2026-10-23)
Sky: replace the camera hook with a city flyby sweep; make it 30 s; add the "turning ordinary people" middle.
**Deliverable:** `052/teaser-30s-v4.mp4` (1920x1080, 24 fps, 720 frames = 30.000 s, silent) · `052/teaser_v4_contact_2fps.png` (every 0.5 s, 60 frames) · `052/edit4.py` · `052/PROVENANCE.json` (pass 4 and V4*/A*/M* entries) · key frames `keyframes/A1_t*, A2_t*, M1..M5_t*`.
**Cost, pass 4 (to date):** about $14.6 of the $35 ceiling, running total for the brief $43.65. **Runware balance is now $2.61 and video requests need 3 credits minimum, so no more clips can be made until the account is topped up** (needs Sky; I have not and will not top up).

## Shot list (24 fps, frames at 24)
| # | Time | Frames | Shot | Source |
|---|---|---|---|---|
| 1 | 0.0–3.0 | 72 | Flyby 1: high sweep over harbour, sky rail and tram, SpaceHQ's atom crown | V4A1 Seedance 2.0 t1, 0.2 s on |
| 2 | 3.0–6.25 | 78 | Flyby 2: lower view over the plaza, tram on the arches, Records Office, people crossing | V4A2 Hailuo 2.3 t1, 0.5 s on |
| 3 | 6.25–9.04 | 67 | Reveal: the cell walks toward the Office (white flash in) | V2S1 Hailuo 2.3 (as v3) |
| 4 | 9.04–11.13 | 50 | Scout beat, black pack | V2S2 Hailuo 2.3 (as v3, longer) |
| 5 | 11.13–13.21 | 50 | Fixer beat, case | V2S3 Hailuo 2.3 |
| 6 | 13.21–15.29 | 50 | Tech beat, visor lowers, neon | V2S4 Hailuo 2.3 |
| 7 | 15.29–17.38 | 50 | Enforcer beat, rifle | V2S5 Hailuo 2.3 |
| 8 | 17.38–19.29 | 46 | Inez the waitress glances at the Office door; the guard crosses behind her | V4M1 Hailuo 2.3 |
| 9 | 19.29–21.21 | 46 | The Fixer slides an envelope across the table to the driver; traffic and a tram pass | V4M2 Hailuo 2.3 |
| 10 | 21.21–23.13 | 46 | The Tech's gloved hand in a camera's junction box, cyan light, camera lens goes dark | V4M3 Seedance 2.0 t1 |
| 11 | 23.13–25.0 | 45 | The bored watcher at the closed-circuit monitor wall (steam, chin on fist, people crossing outside) | V4M4 Hailuo 2.3 |
| 12 | 25.0–27.17 | 52 | Tension: Enforcer back-on, the car rolls in, people cross | V3S6 Seedance 2.0 t5 (as v3) |
| 13 | 27.17–30.0 | 68 | Title over the live tail of the same clip, lockup as v3, fade out | ffmpeg |
Not used: the Scout-on-the-balcony beat (`V4M5_seedanceF_t1` is good: she touches her earpiece and speaks while the plaza moves below; the cut was full), the Seedance monitor and Hailuo junction alternates, Vale stepping out (`V4S6_*`: both takes lose the car's arrival or never show a clear Vale; v3's shot stays).

## Not done, and why
- **H3:** Runware does have `minimax:h3@0` (up to 15 s, 1440p, $0.13/s, first and last frame). My flyby (one 6 s first-to-last-frame glide from `A1_t2` to `A2_t2`) and the hero-beat comparison were refused for insufficient credits (balance $2.61; minimum 3). The flyby is therefore two cuts (Seedance then Hailuo) rather than one take. After a top-up: `LASTFRAME=keyframes/A2_t2.png python3 -I briefs/052/gen.py vid V4A1 keyframes/A1_t2.png --model h3 --mode fl --dur 8 --take 3 --prompt ...` is about $1.04.
- Audio: none; the film is silent (no H3 audio exists).
- The Runware key: the old store's client reads it from `~/Projects/the-suborned/.env.local`, the same key as `/Users/sky/Projects/Tools/.env` (Aster's correction); `gen.py` uses that client unchanged and nothing is copied.

## Weak spots
The flyby cut at 3 s is a jump (high to lower view) rather than a glide; the Seedance flyby loses the trams in its last second. The Fixer's face in the envelope shot is a little broader than the turnaround; her case is off-frame there. The driver is bald and in a light-blue jacket (the model's choice; not the art bible's slate blue).

---

# Back to v4 (2026-10-23)
Sky on v6: "not into intro, skytrain still weird, techs glasses not good either - let's go back to previous version". v5 and v6 (the H3 long-take flyby, H3 Tech, Scout and envelope takes, scratch score) are set aside; their clips stay in `paid/052-teaser/` as rejects. **v4 is the current cut.** `edit4b.py` → `teaser-30s-v4b.mp4` is v4 unchanged except that the Tech (V2S4 Hailuo, same clip) holds 74 frames instead of 50, so about 1.3 s on his face with the visor down (Sky's earlier ask), paid for by 6 frames from each of the four middle shots. It's an ffmpeg re-edit at $0. The sky train still jumps at v4's 3 s flyby cut.
