# Brief 065: the capture, a short clip in a picture-in-picture (concept)

Sky, 2026-10-25: "could we generate a short video of when enforcer captures Vale inside building, and could play in a mini PiP to add atmosphere" · "Using minimax h3 fast".

- **Objective:** one short clip of the Enforcer taking Vale inside the Records Office, made at build time and played in a small picture-in-picture while the Enforcer is inside. The interior stays off-screen for play (exteriors only: Enter is a timed action, 38 s, or 68 s once Alert ≥ 2); the clip is presentation only and never reads or changes the sim, so every run replays exactly.
- **Owner:** Forma (key frame and clip). **Next:** Sky watches the clip; then Glyph (where the PiP sits, its frame, when it plays), Latch (Unity VideoPlayer), Aster (the GDD line, a new version, only once Sky keeps it). Parked for later: Chime (H3 makes audio; the game has none yet, so the PiP is muted and the track is kept).
- **Done-check:** a clip of 6 to 10 s from `minimax:h3@fast` on Runware, first-frame image to video, logged through `tools/paid/paid.py` in job `065-capture-pip` within a $5 pass; the Enforcer and Vale C on-model; no text, HUD or modern tech in frame; a contact sheet; the clip cut to a PiP-sized copy (480 x 270) as well as the full one.
- **Evidence:** `paid/065-capture-pip/` (key frames, clips, ledger) and a short section in this brief with the pick, the cost and a contact sheet path.

## The shot (Forma's starting point; change it if the clip reads better another way)
- **Place:** the Records Office's inside, never designed before. Paper and microfilm, cabinets of drawers, a reading desk, a tall window; 1950s style, centuries-ahead technology (no screens, no computers, no box CCTV); cream stone, seafoam bands, chrome, brass only as a thin line. Concept art first: the key frame decides the room.
- **Beat:** Vale at his desk; the Enforcer comes in quietly; Vale rises; the Enforcer subdues him without a shot (he is taken alive) and lifts him onto his shoulder. Ending on the shoulder carry matches the game: the next thing the player sees is the Enforcer leaving the service door carrying Vale.
- **People:** the Enforcer from his turnaround (`briefs/020/people/sheets/enforcer.png`, the style contract's locked cell look: sharp, space-age, no 1950s tailoring, no long coat); Vale C (`briefs/063/concepts/vale_C.png`). People prompts carry no date.
- **Camera:** a still security-camera-like angle from high in a corner suits the game (the world's cameras are closed-circuit and watched by people) and hides small face drift; a slow push is fine.

## Limits
- Economical: the key frame from Codex first (free); one or two H3 Fast takes, stop at the first good one. A raise of the pass needs Sky.
- Runware wants a minimum balance (about 3 credits) for video even when the clip costs less; if it refuses for credits, stop and say so.
- Nothing goes into Unity, the GDD or the HUD before Sky has watched it.

## Result (Forma, 2026-10-25)
- **Pick:** take 2 of `minimax:h3@fast`, first-frame, 768p (1344 x 768, 24 fps), 10.1 s, from Codex key frame B. On-model, no text or HUD or screens. Beat reads: the Enforcer crosses the room, grabs Vale from behind, Vale goes limp, fireman's carry, the Enforcer leaves by the door at the left.
- **Price:** H3 Fast is $0.046/s at 480p and $0.08/s at 768p (runware.ai/docs/models/minimax-h3-fast, fetched 2026-10-25; modelSearch confirms `minimax:h3@fast`). Two 10 s takes at 768p = $1.60 of the $5 pass (take 1 $0.80, take 2 $0.80); Codex key frames $0. A first attempt with `settings.promptExpansion` was rejected by Runware before any charge (H3 Fast has no `settings`; not allowed with this model).
- **Files** (all in `paid/065-capture-pip/`, git-ignored; ledger `ledger.jsonl`):
  - key frame: `final/capture_keyframe.png` (= `codex/capture_kf_b.png`; kf_a was the first try: too much brass, clutter in the foreground)
  - full clip: `final/capture_full_1344x768.mp4` (= `shots/capture_h3fast_t2.mp4`)
  - PiP copy: `final/capture_pip_480x270.mp4` (h264, aac, 0.64 MB)
  - contact sheet: `final/capture_contact_sheet.png` (one frame per second)
  - script: `briefs/065/gen.py` (prompt of the last take inside)
- **What failed:** take 1 (`shots/capture_h3fast_t1.mp4`, $0.80) read as a hug or a dance: the prompt said "takes him firmly by the arm", and H3 made an embrace and panned the camera. Take 2's prompt named the act ("a silent abduction, not a hug", arm across the chest, hand to the neck, Vale goes limp, fireman's carry) and fixed it. Remaining faults in take 2: the camera is not fully locked (a slow push-in and a small drift, fine as a "slow push"); Vale's black shoulder disc and its gold ring vanish for a few frames while the Enforcer holds him; the Enforcer's coat tails are short and the tunic is simpler than the sheet; the carry is a little rubbery; no clear shot of Vale's face once he is on the shoulder.
- **Audio:** H3 Fast made an AAC track, kept in both copies. It is sparse foley (peak -17.7 dB, mean -49.6 dB, short transients, no speech or music, gaps up to 1 s). I could not hear it; by the levels it is quiet footsteps and thuds, so probably usable as muted-by-default atmosphere. Chime to listen before it is used.
- **Next owner:** Sky watches `final/capture_pip_480x270.mp4`; only if kept, Glyph (PiP place and frame), Latch (VideoPlayer), Aster (GDD line).
