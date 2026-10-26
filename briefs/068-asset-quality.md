# Brief 068 · Asset quality: the finish of the café and the Records Office (look)

Owner: Aster. **Opens after batch 0.2.123 lands; not part of it.** Sky said yes on 2026-10-26 to the asset-quality review (`briefs/068/review/`: `summary.html`; `review.html` is 8.7 MB with its pictures embedded and stays on disk, git-ignored with `audit.json`; `audit.py` and `report.py` rebuild them). Main's read, which Sky accepted, sets the order below. Sky judges from pictures; then it is measured on the Air.

## The review's findings (built on 0.2.117)
Strong source assets and a healthy Unity pipeline; the biggest gain is a more consistent material finish. 347 kit entries (262 adopted), 288 materials (kit and people), 225 texture imports audited.
1. Civic façades: shared stone and enamel detail, stronger contact definition (CafeBay has 0 textured materials of 9).
2. Hedge bands: leaf treatment and varied outlines to match the richer palms.
3. Character materials: separate cloth, skin, leather and trim (the Scout, Tech and Enforcer drop ORM and use one flat body smoothness).
4. Reflections: sharper baked hero probes and an HDR fallback (the fallback cube is 64 px LDR).
5. Texture fidelity: lossless normals; the UV snap on atlas pieces.
6. (perf) goes to Redline's Air work, brief 064.
7. Stale: it used 0.2.117, before the F cut.

## Order (main's read, Sky's yes)
1. **First pass, the review's own:** one matched comparison at the café (one bay, the planter band, a chrome post, a dressed character), at play zoom and the follow distance, with the lighting fixed. Then day, dusk and night.
2. **#1 café and Records Office surface detail** (Vela) · **#4 hero reflections** (Glint) · **#5 texture-loss fixes**: lossless normals, the atlas UV snap (Splice and Forma).
3. **#2 hedges** (Vela), inside the style contract's "sculpted and tidy, no leaf clutter": try a leafy surface shader on the existing shapes before leaf cards.
4. **#3 character material masks** (Forma), lower: the cell are 40–60 px at play zoom.

## The budget
Redline's Air run on 0.2.122 (briefs/064/air-0122; brief 064 "Air 0.2.122"): warm, the play views run 21–23 ms mean, so the game is already 8–10 ms over its 60 fps line. **Nothing additive ships until the Air baseline exists** (the settings round and the soak, both waiting on Sky). Until then a change must not move the warm frame (≤ +0.3 ms in an Air A/B); after the settings round it may spend up to 1.5 ms of what is left under a 13.0 ms warm mean. Full detail stays (GDD §12: no decimation, triangle caps or texture downscaling without Sky). Look decisions come from pictures shown to Sky, never palette tables.

## Done-check (first pass)
The café comparison frames (today against proposed, same light, play zoom and follow) for Sky, each with its frame cost on the Studio and the piece's triangle and material counts; nothing installed until Sky picks.

Next owner when it opens: Vela with Glint (the café pass), Splice (normals and the UV snap), Latch (frames).
