#!/usr/bin/env python3
"""Self-contained sheet for brief 009 A (Vela). No Blender. Reads out/decokit-009 (catalogue.proposal.json, crossing-colours.json, measured.json), the renders in
out/review/009-renders and the concepts in briefs/009/concepts; writes out/review/009-crossing-pieces.html. Run proposal009.py first.
The scene renders get a stand-in bloom (key on the seafoam and red lights, blur, screen): Unity adds the real one; the PNGs in 009-renders are raw."""

import base64
import html
import io
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter

REPO = Path(__file__).resolve().parents[2]
OUT, R, CON = REPO / "out/decokit-009", REPO / "out/review/009-renders", REPO / "briefs/009/concepts"
CAT = json.loads((OUT / "catalogue.proposal.json").read_text())["assets"]
COL = json.loads((OUT / "crossing-colours.json").read_text())
MEAS = json.loads((OUT / "measured.json").read_text())
e = html.escape
PX = 1080 / 28


def bloom(im, gain=0.8, radius=5):
    a = np.asarray(im.convert("RGB"), np.float32) / 255
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    lum = a.max(-1)
    key = (((g - r) > 0.045) & (lum > 0.74)) | ((r > 0.8) & (g < 0.62) & (b < 0.62))
    lit = (a * key[..., None]).astype(np.float32)
    blur = np.asarray(Image.fromarray((lit * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(radius)), np.float32) / 255
    out = 1 - (1 - a) * (1 - np.clip(gain * blur * 2.2, 0, 1))
    return Image.fromarray((np.clip(out, 0, 1) * 255).astype(np.uint8))


def img(path, w=560, crop=None, scale=None, lit=False, caption=""):
    im = Image.open(path).convert("RGB")
    if lit:
        im = bloom(im)
    if crop:
        im = im.crop(crop)
    if scale:
        im = im.resize((im.width * scale, im.height * scale), Image.NEAREST if scale > 1 else Image.LANCZOS)
    elif im.width > w:
        im = im.resize((w, round(im.height * w / im.width)), Image.LANCZOS)
    b = io.BytesIO()
    im.save(b, "JPEG", quality=86)
    return f'<figure><img src="data:image/jpeg;base64,{base64.b64encode(b.getvalue()).decode()}" alt="{e(Path(path).name)}"><figcaption>{caption}</figcaption></figure>'


def table(head, rows):
    return "<table><tr>" + "".join(f"<th>{h}</th>" for h in head) + "</tr>" + "".join("<tr>" + "".join(f"<td>{c}</td>" for c in r) + "</tr>" for r in rows) + "</table>"


sw = lambda h: f'<span class="sw" style="background:{h}"></span>{e(h)}'  # noqa: E731


def main():
    s = []
    s.append("<h1>009 A: the crossing's kit pieces</h1><p class='meta'>Vela, 14 Oct 2026. Candidates only: <code>contracts/world/kit.json</code>, <code>unity/</code> and the GDD are untouched; nothing adopted. Pieces in <code>out/decokit-009/</code>, "
             "generator <code>tools/decokit/crossing_*.py</code>. Renders: Cycles, studio and courtyard HDRI; scene images carry a stand-in bloom (raw PNGs in <code>out/review/009-renders/</code>).</p>")
    # ---- answer
    s.append("<h2>1. The orb is a sphere</h2><div class='box'><p><b>Sphere, not saucer.</b> Of the six concepts five are spheres (eye, eye-cafe, play, play-2, play-avenue) and one a saucer (eye-2). Reasons, in order of weight:</p><ol>"
             "<li><b>The play camera sees the display.</b> It looks down at 38 degrees. A saucer's display sits in the underside of its rim (eye-2), so its upper hull hides it from above (inferred: no concept shows a saucer from above). A sphere cut flat for the display shows it from any height.</li>"
             "<li><b>Any yaw.</b> The silhouette is a circle whatever the street's direction, so one piece serves every crossing; a saucer reads as a thin ellipse edge-on.</li>"
             "<li><b>Cheapest read per pixel.</b> At play zoom (38.6 px per metre) the orb is 32 px across: a circle with a dark disc and a coloured figure is what survives; a saucer's rim light and curve are sub-pixel.</li>"
             "<li><b>It speaks the kit's language.</b> Chrome spheres already carry the lamps, bollards and the Records globe; the orb joins them, while a saucer would be a second family.</li></ol>"
             "<p>The saucer survives as an option for a later pass at the skyline's eye (it is Googie), not for the street.</p></div>")
    # ---- beside the concepts
    sc = {k: R / f"scene.{k}.png" for k in ("bars.camera.play", "bars.walk.play", "dots.camera.play", "dots.walk.play", "bars.walk.eye", "dots.walk.eye")}
    s.append("<h2>2. Beside the concepts</h2><p>An 8 m crossing built from the candidates only (8 bars or three dot rows, four orbs, 12 kerb studs, four arcs) on pale paving, kerb and lilac-grey asphalt; mannequins are 1.7 m. "
             "No Glint lighting, no skyline: this judges the shapes and the read, not the mood.</p><h3>Eye level (1.7 m, 35 mm)</h3><div class='row2'>"
             + img(CON / "crossing-future-eye.jpg", 700, caption="concept: crossing-future-eye (spheres, red display, bars in brass frames)")
             + img(sc["bars.walk.eye"], 700, lit=True, caption="candidates: bars, orbs looking along the walk")
             + img(CON / "crossing-future-eye-2.jpg", 700, caption="concept: crossing-future-eye-2 (saucers, walk)")
             + img(sc["dots.walk.eye"], 700, lit=True, caption="candidates: dot rows")
             + "</div><h3>Play zoom (orthographic size 14, pitch 38, yaw 30, 1920 x 1080 frame, shown at 50%)</h3><div class='row2'>"
             + img(CON / "crossing-future-play.jpg", 700, caption="concept: crossing-future-play")
             + img(sc["bars.camera.play"], 700, lit=True, caption="candidates: bars, orbs turned to the camera")
             + img(CON / "crossing-future-play-2.jpg", 700, caption="concept: crossing-future-play-2 (dot rows)")
             + img(sc["dots.camera.play"], 700, lit=True, caption="candidates: dot rows, orbs turned to the camera")
             + "</div>")
    s.append("<h3>The same frame at 1:1 pixels (what the game draws) and at 3x</h3><div class='row2'>"
             + img(sc["bars.camera.play"], crop=(1020, 150, 1480, 460), lit=True, caption="east kerb end, 1:1 (460 x 310 px): orb 32 px, figure about 16 px, halo 43 px, stud 15 px")
             + img(sc["bars.camera.play"], crop=(1120, 170, 1280, 270), scale=3, lit=True, caption="the same orb at 3x: dark plate, red figure, brass bezel, beam, halo ring")
             + img(sc["bars.walk.play"], crop=(1020, 150, 1480, 460), lit=True, caption="faces left along the walking direction: the plates are edge-on to this camera (see open issue 1)")
             + img(sc["bars.camera.play"], crop=(420, 380, 880, 690), lit=True, caption="west kerb end, 1:1: the walk aspect (seafoam figure) on the lower orb")
             + "</div>")
    # ---- pieces
    rows = []
    for a in CAT:
        M = MEAS[a["id"]]["levels"]
        o0 = M["L0"]["objects"]
        base = o0[f"DK_{a['id']}"]
        rows.append([f"<b>{e(a['id'])}</b><br><small>{e(a['category'])}</small>", f"{a['size'][0]} x {a['size'][2]} m footprint, {a['heightAbovePivot']} m above the pivot<br><small>{base['px_at_play_zoom'][0]} x {base['px_at_play_zoom'][1]} px at play zoom (body)</small>",
                     f"<b>{a['tris']}</b> / {a['lods'][0]['tris']}", e(a["pivot"] + (f" +{a['pivotOffset']}" if "pivotOffset" in a else "")), ", ".join(m.replace("DK_", "") for m in a["materials"]), e(a["note"])])
    s.append("<h2>3. The pieces</h2><div class='row4'>"
             + "".join(img(R / f, 330, caption=c) for f, c in (("SignalEye.walk.thumb.png", "SignalEye, walk"), ("SignalEye.stop.thumb.png", "SignalEye, stop"), ("SignalEye.walk.L1.thumb.png", "SignalEye L1 (walk)"), ("SignalEye.walk.eye.png", "SignalEye at 1.7 m"),
                                                                ("KerbStud.thumb.png", "KerbStud"), ("LightBar_4.thumb.png", "LightBar_4"), ("LightDotRow_2.thumb.png", "LightDotRow_2"), ("CrossInlayArc_R24_W08.thumb.png", "CrossInlayArc_R24_W08")))
             + "</div><div class='row4'>" + "".join(img(R / f, 330, caption=c) for f, c in (("KerbStud.L1.thumb.png", "KerbStud L1"), ("LightBar_4.L1.thumb.png", "LightBar_4 L1"), ("LightDotRow_2.L1.thumb.png", "LightDotRow_2 L1"), ("CrossInlayArc_R24_W08.L1.thumb.png", "Arc L1")))
             + "</div><p><small>Studio thumbnails: the DecoKit studio HDRI, whose zenith is dark, so flat brass mirrors black here; in the courtyard-lit scene above it reads as dark brass. That is the brass of the palette (#99784D, 21.3 from the guard colour): it is dark by rule.</small></p>"
             + table(["piece", "measured size", "tris L0 / L1", "pivot", "materials", "what it is"], rows))
    ed = {k: MEAS[k]["levels"]["L0"]["objects"][f"DK_{k}"] for k in ("CrossEdgeLine_2", "CrossEdgeLine_1")}
    s.append("<h2>3b. The edge frame line (Aster's follow-up)</h2><p><b>CrossEdgeLine_2</b> (and its 1 m end module <b>CrossEdgeLine_1</b>): a brass line 2.00 x 0.04 m, 6 mm proud, one quad (2 triangles; L1 the same), on each long edge of the strip as in crossing-future-play-2. "
             f"Measured: {ed['CrossEdgeLine_2']['size']} m and {ed['CrossEdgeLine_1']['size']} m. SF-21: area 0.08 m2 per module (limit 1 m2) and 0.04 m wide (limit 0.6 m); a whole 8 m crossing carries 2 x 8 m x 0.04 m = 0.64 m2 of it in two separate lines. Colour DK_Brass_Civic #99784D, 21.3 from the guard colour (cones 25.9, rivals 47.8): passes the 20 gate.</p>"
             "<p><b>Separate tiling piece, not part of the dot row.</b> (1) The bar variant needs it too and has no dot row to carry it. (2) It runs the strip's full length at the strip's edge line, whatever the row layout; a dot module is 2 m with hairlines 0.4 m apart around its own dots, so the edge line cannot ride on it without doubling the hairline. "
             "(3) One quad per 2 m: a 12 m strip is 12 triangles for both edges. Place at y = +-2.0; move the dot rows in to y = +-1.75, because the row's outer hairline would otherwise sit on the line at the same height (z-fight).</p><div class='row2'>"
             + img(sc["dots.camera.play"], crop=(560, 330, 1360, 780), lit=True, caption="candidates: dot rows with the edge line, play zoom, crop 1:1")
             + img(CON / "crossing-future-play-2.jpg", 700, caption="concept: crossing-future-play-2")
             + img(R / "CrossEdgeLine_2.thumb.png", 330, caption="CrossEdgeLine_2 (studio)") + "</div>")
    # ---- budgets and shadows
    prow = []
    for a in CAT:
        for p in a.get("parts", []):
            prow.append([e(p["object"]), e(p["role"]), f"{p['tris']['L0']} / {p['tris']['L1']}", e(p["castShadows"]), e(p["default"]), e(p["note"])])
    s.append("<h2>4. Budgets, parts and shadows</h2>"
             + table(["piece", "budget (LOD0, all parts)", "built L0", "L1", "L1 / L0"], [[e(a["id"]), a["budget"]["tris"], f"<b>{a['tris']}</b>", a["lods"][0]["tris"], f"{100 * a['lods'][0]['tris'] / a['tris']:.0f}%"] for a in CAT])
             + "<p><b>The LODs are for the flyby, the skyline and the zoomed-out view only.</b> Under an orthographic camera Unity picks the level from the piece's size against the frame height, never from distance (out/review/lod-play-zoom.html): at play zoom every one of these is drawn at LOD0. "
               "So LOD0 is the budget and it is kept lean on purpose; each piece still has a hand-built L1 (kit.asset <code>near=</code>, because a collapse decimate wrecks a 34-triangle bar and the floor skips pieces under 400 triangles).</p>"
             + "<h3>SignalEye parts</h3>" + table(["object", "what", "tris L0 / L1", "castShadows", "default", "note"], prow)
             + "<p><b>Do not cast shadows:</b> the beam, the halo, the bars, the dot rows, the arcs, the pictograms and the stud (84 mm). <b>Casts:</b> the orb body only: its shadow, thrown 2 to 3 m from the beam's foot, is the one depth cue that says it hovers at play zoom. "
             f"It is {MEAS['SignalEye']['levels']['L0']['objects']['DK_SignalEye']['materials']['DK_Chrome']} triangles of chrome in {MEAS['SignalEye']['levels']['L0']['objects']['DK_SignalEye']['tris']} for the body; if Glint's caster audit (pieces under 2 m stop casting) takes it out, nothing else changes, and its L1 body ({MEAS['SignalEye']['levels']['L1']['objects']['DK_SignalEye_L1']['tris']} triangles) is the cheap shadows-only proxy. "
             "The glow parts are emissive: receive shadows off.</p>")
    d = COL["district"]
    eye = MEAS["SignalEye"]["levels"]["L0"]["objects"]
    body, drawn = eye["DK_SignalEye"]["tris"], sum(o["tris"] for k, o in eye.items() if not k.endswith("_Stop"))  # one aspect drawn: the other pictogram part is off
    chrome = eye["DK_SignalEye"]["materials"]["DK_Chrome"]
    per = 4 * drawn + 8 * 34 + 12 * 90 + 4 * 60
    s.append(f"<p>Cost of one 8 m crossing as rendered above (4 orbs showing one aspect, 8 bars, 12 studs, 4 arcs): {per:,} LOD0 triangles, of which {4 * body:,} cast (the four orb bodies); the Air draws 1.07 to 1.71 million per frame. "
             f"District, from the world export: {d['crossings']} crossings, {d['metres']} m of strip: {d['bars']} bars (x 34 = {d['bars'] * 34:,} triangles) or {d['dot_rows_2m']} + {d['dot_rows_1m']} dot modules in three rows ({d['dot_rows_2m'] * 92 + d['dot_rows_1m'] * 48:,}); "
             f"{d['orbs_2_per_crossing']} orbs at two per crossing ({d['orbs_2_per_crossing'] * drawn:,}) or {d['orbs_4_per_crossing']} at four ({d['orbs_4_per_crossing'] * drawn:,}), against 18 million kit triangles in the scene. Dots cost about four times the bars; crossing-future-play combines the two (bars with a dot row on each edge: 8 x 34 + 2 x 4 x 92 = 1,008 triangles for the 8 m crossing), which is a Splice option, not a requirement.</p>")
    # ---- colours
    crow = [[e(c["material"]), sw(c["unlit"]), e(c["kind"]) + (f", emission {c['emission']}" if c["emission"] else "") + (f", alpha {c['alpha']}" if c["alpha"] else ""), f"{c['dE2000']['cones']} / {c['dE2000']['guard']} / {c['dE2000']['rivals']}", f"<b>{c['min']}</b>",
             e(c["gate"]), e(c["role"])] for c in COL["materials"]]
    s.append("<h2>5. Colours (unlit palette, dE2000)</h2>" + table(["material", "unlit", "kind", "dE cones / guard / rivals", "min", "gate", "role"], crow)
             + f"<p>Signals: cones {COL['signals']['cones']}, guard {COL['signals']['guard']}, rivals {COL['signals']['rivals']}. Everything is at least 20 except the stop pictogram, which is Sky's exception of 14 Oct (small emissive display pictograms only, never a surface; "
               "mats.SIGNAL_EXEMPT names it, the gate reports nothing for it). The red is #E8402F, a plain signal red (2.4 from the cones colour: a 0.4 m figure on a dark plate, 2.7 m up, switching with the walk and stop state; the cones keep their shape and motion cue). "
               "If a red further from the cones colour is wanted: crimson #F2305A is 11.8 from it (still an exception), and the palette's flamingo #E8609A already passes the gate at 24.6 but reads pink, which is why the kit's old Signal_Red alias used it. "
               "Brass appears only as frame, bezel, hairline and the dome stud (SF-21). Owed to Aster: the exception in GDD s10 and s15 and the style contract.</p>")
    # ---- catalogue proposal
    crow = [[f"<b>{e(a['id'])}</b>", f"{a['size'][0]} x {a['heightAbovePivot']} x {a['size'][2]}", e(a["pivot"]) + (f"<br><small>offset {a['pivotOffset']} (kit axes)</small>" if "pivotOffset" in a else ""),
             ", ".join(e(p["object"].replace("DK_", "")) for p in a.get("parts", [])) or "one object", f"{a['tris']} / {a['lods'][0]['tris']}", e(", ".join(a["tags"]["use"]))] for a in CAT]
    s.append("<h2>6. Catalogue proposal for Splice</h2>" + table(["id", "size x y z (m)", "pivot", "separate objects (FBX siblings)", "tris L0 / L1", "tags.use"], crow)
             + "<p>Full KitAsset rows (files with sha256, LODs, parts with castShadows and default visibility, notes with placement conventions): <code>out/decokit-009/catalogue.proposal.json</code>; colours: <code>crossing-colours.json</code>; measured per object: <code>measured.json</code>. "
               "To adopt: <code>bun tools/decokit/run.ts --out out/decokit-009 --only SignalEye,KerbStud,LightBar_4,LightDotRow_2,LightDotRow_1,CrossInlayArc_R24_W08,CrossEdgeLine_2,CrossEdgeLine_1</code> reproduces the files byte for byte.</p>"
               "<h3>Generator changes (tools/decokit; the old pieces are byte-identical, checked on OceanLiner and BollardSphere against HEAD)</h3><ul>"
               "<li><code>kit.py</code>: <code>asset(near=)</code>, a hand-built L1; <code>build_lod(name, tag)</code> (<code>build_far</code> kept); family <code>crossing009</code> registered.</li>"
               "<li><code>export_kit.py</code>: a hand-built L1 is exported whatever the piece's weight, instead of the 25% collapse and the 400-triangle floor.</li>"
               "<li><code>mats.py</code>: kind <code>glow</code> (translucent light volume: strength and alpha in the row; no .mat written, Latch draws it additive and double-sided); <code>SIGNAL_EXEMPT = {DK_Cross_Stop}</code>; the <code>DK_Cross_</code> prefix is gated.</li>"
               "<li><code>render008.py</code>: <code>main()</code> behind <code>__main__</code> so <code>render009.py</code> can reuse its studio and views.</li></ul>")
    # ---- open
    s.append("<h2>7. Open issues</h2><ol>"
             "<li><b>The displays face the walk, the camera may not.</b> A display looks along the crossing; for a crossing that runs across the camera's view it is edge-on (3rd image of the 1:1 row). The signal is a gameplay cue (a window the player can see coming), so it must read from the play camera. "
             "Recommendation for Latch: turn Body, Walk and Stop together about the vertical axis to face the camera (they share the origin; Beam and Halo are round); the orb then looks at you, which suits the city. If the camera can swing freely this is the only robust answer; "
             "if you want a fixed yaw instead, ask for a four-face orb (about +260 triangles for the second pair of faces; the body would no longer be a single lathe).</li>"
             "<li><b>Brass reads dark.</b> #99784D is dark by the palette gate, and the chrome and brass flats mirror whatever the reflection probe holds: Glint should check the probe above a crossing (a dark zenith turns the frames black).</li>"
             "<li><b>Glow needs Latch and Glint.</b> Blender cannot show the bloom: the seafoam is an emissive base (#72B5A7, strength 2.5) and the walk figure the pale seafoam (strength 2.4); the beam and halo are kind glow (alpha 0.28 and 0.40) for an additive double-sided material. "
             "Whether the bars go dark on stop and whether the beam fades by v is theirs to try.</li>"
             "<li><b>Overhead and keep-clear.</b> An orb is a canopy-class item: a disc of radius 0.42 m between 2.21 and 3.23 m over the pivot; its ground footprint is the halo, radius 0.56 m (CD-1 keep-clear and SF-20 are Splice's rewrite).</li>"
             "<li><b>Flat pieces have no thickness.</b> Lens 4 mm and frame 6 mm above the surface, faces up, nothing below the pivot plane; the manifest's size[1] for them is 2 mm (the box starts at 4 mm), so the proposal carries heightAbovePivot. They sit on the carriageway's own surface: if Splice cuts the road mesh, they need that surface at z = 0.</li>"
             "<li><b>Arc placement.</b> The arc is a quarter ring; the proposal gives the convention (outer edge tangent to the kerb line and the strip's edge, centre = corner + 2.83 into the pavement, quarter-turn yaw). If the plan's landing geometry differs, say so and I will change the radius or add a second one.</li></ol>")
    css = ("body{font:14px/1.45 system-ui;max-width:1500px;margin:2em auto;color:#222;padding:0 1em}h1{margin-bottom:0}.meta{color:#555}table{border-collapse:collapse;margin:.6em 0;width:100%}td,th{border:1px solid #ccc;padding:3px 6px;vertical-align:top;text-align:left}th{background:#eee}"
           ".sw{display:inline-block;width:14px;height:14px;border:1px solid #888;margin-right:4px;vertical-align:-2px}.row2{display:grid;grid-template-columns:1fr 1fr;gap:10px}.row4{display:grid;grid-template-columns:repeat(4,1fr);gap:8px}figure{margin:0}img{max-width:100%;height:auto;display:block}"
           "figcaption{font-size:12px;color:#444;margin:2px 0 8px}code{background:#f2f2f2;padding:0 3px}small{color:#555}.box{background:#f6f3ea;border:1px solid #d8d0b8;padding:.2em 1em}")
    out = REPO / "out/review/009-crossing-pieces.html"
    out.write_text(f"<!doctype html><meta charset=utf-8><title>009 A crossing pieces</title><style>{css}</style>" + "\n".join(s))
    print(out, out.stat().st_size // 1024, "KB")


if __name__ == "__main__":
    main()
