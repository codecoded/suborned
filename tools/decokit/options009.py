#!/usr/bin/env python3
"""Brief 009 look options (Vela, 15 Oct 2026): the numbers and the one comparison sheet. No Blender.
Reads out/decokit-009/options (render009opts.py: opt.*.png, brass.*.png, frames.json), the candidates' manifest (out/decokit-009/decokit_manifest.json), the world export (read only: the
crossings' light lines) and the style contract (signal colours); writes briefs/009/options/009-crossing-options.png and .json.
Run: python3 tools/decokit/options009.py   (after: bun tools/decokit/run.ts --out out/decokit-009 --only <the candidates>; blender -b -P tools/decokit/render009opts.py)"""

import json
import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[1]
sys.path.insert(0, str(HERE))
from deltae import de, lab  # noqa: E402

OUT = REPO / "out/decokit-009"
R = OUT / "options"
DEST = REPO / "briefs/009/options"
STYLE = json.loads((REPO / "contracts/style/suborned.style.json").read_text())
SIGNALS = {k: v.upper() for k, v in STYLE["signal_colours"].items() if k != "rule"}
MAN = {a["name"]: a for a in json.loads((OUT / "decokit_manifest.json").read_text())["assets"]}
FR = json.loads((R / "frames.json").read_text())
ASPHALT, PAVE = "#8B8080", "#D1BFA2"          # what the Unity capture draws (139,128,128) and (209,191,162): the surfaces a brass line and a stud sit on
tris = lambda n: MAN[n]["tris"]  # noqa: E731

# option -> (letter, short, what, light pieces per line module length, glow pieces, line width m)
OPTS = {
    "a": ("(a)", "as built", "dots 0.22 m on a 0.5 m pitch in brass rings, one row per line", {2: "LightDotRow_2", 1: "LightDotRow_1"}, None, 0.22),
    "b2": ("(b2)", "rows two dots wide", "the same dots, two rows across each line", {2: "LightDotBand2_2", 1: "LightDotBand2_1"}, None, 0.72),
    "b3": ("(b3)", "rows three dots wide", "the same dots, three rows across each line", {2: "LightDotBand3_2", 1: "LightDotBand3_1"}, None, 1.22),
    "c": ("(c)", "light bars along the walk", "LightBar_4 end to end on each line (plus 2 m and 1 m end bars: the plan's [4] alone leaves 3 m of an 11 m line dark)", {4: "LightBar_4", 2: "LightBar_2", 1: "LightBar_1"}, None, 0.34),
    "d1": ("(d1)", "lit strip", "a soft seafoam field the whole 3.9 m of the strip, the as-built dots on top (concepts play, play-avenue)", {2: "LightDotRow_2", 1: "LightDotRow_1"}, ({2: "LightField_2", 1: "LightField_1"}, 1), 3.9),
    "d2": ("(d2)", "light pools", "a soft seafoam pool 1.3 m wide under each dotted line, the as-built dots on top", {2: "LightDotRow_2", 1: "LightDotRow_1"}, ({2: "LightPool_2", 1: "LightPool_1"}, 2), 1.3),
}


def greedy(room, mods):
    out, left = [], room + 1e-9
    for m in sorted(mods, reverse=True):
        while left >= m:
            out.append(m)
            left -= m
    return out


def per_crossing():
    """The reference crossing: 12 m road, lights from 0.5 m off each kerb: an 11 m line on each side."""
    row = {}
    for k, (_, _, _, lines, glow, _) in OPTS.items():
        lm = greedy(11, list(lines))
        m, t = 2 * len(lm), 2 * sum(tris(lines[x]) for x in lm)
        if glow:
            gm = greedy(11, list(glow[0]))
            m += glow[1] * len(gm)
            t += glow[1] * sum(tris(glow[0][x]) for x in gm)
        row[k] = dict(modules=m, tris=t, line_modules=lm)
    return row


def district():
    """The 95 crossings of the live world export (read only): the light lines' spans, from the dot rows placed there, laid again with each option's modules."""
    w = json.loads((REPO / "out/world/harbour-district.world.json").read_text())
    by = {}
    for s in w["street"]:
        if s["kind"] == "crossing-dots":
            by.setdefault(s["crossing"], []).append(s)
    cr = {c["id"]: c for c in w["crossings"]}
    spans = []   # (side, length) of every unbroken light line
    for cid, items in by.items():
        (ax, ay), (bx, by_) = cr[cid]["line"]
        L = math.hypot(bx - ax, by_ - ay)
        u = ((bx - ax) / L, (by_ - ay) / L)
        for side in ("l", "r"):
            mods = sorted(((((i["at"][0] - ax) * u[0] + (i["at"][1] - ay) * u[1]), i["size"][0]) for i in items if i["id"].split("/")[-1].startswith(side)))
            run = None
            for c, size in mods:
                lo, hi = c - size / 2, c + size / 2
                if run and abs(lo - run[1]) < 1e-6:
                    run[1] = hi
                else:
                    if run:
                        spans.append((side, run[1] - run[0]))
                    run = [lo, hi]
            if run:
                spans.append((side, run[1] - run[0]))
    out = {}
    for k, (_, _, _, lines, glow, _) in OPTS.items():
        m = t = 0
        for side, ln in spans:
            for x in greedy(round(ln), list(lines)):
                m, t = m + 1, t + tris(lines[x])
            if glow and (glow[1] == 2 or side == "l"):
                for x in greedy(round(ln), list(glow[0])):
                    m, t = m + 1, t + tris(glow[0][x])
        out[k] = dict(modules=m, tris=t)
    out["_crossings"] = len(cr)
    out["_line_spans"] = len(spans)
    out["_line_metres"] = round(sum(ln for _, ln in spans), 1)
    return out


def Lstar(a):
    """CIE L* of an sRGB uint8 array (..., 3)."""
    c = a.astype(np.float32) / 255
    c = np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)
    y = 0.2126 * c[..., 0] + 0.7152 * c[..., 1] + 0.0722 * c[..., 2]
    f = np.where(y > 0.008856, np.cbrt(y), 7.787 * y + 16 / 116)
    return 116 * f - 16


def lit_pixels():
    """Pixels of the strip (between the kerbs, inside the two edge lines) that an option lights: L* at least 5 above the same frame without the light lines (same people, shadows, orbs)."""
    base = np.asarray(Image.open(R / "opt.none.png").convert("RGB"))
    mask = Image.new("L", (1920, 1080), 0)
    ImageDraw.Draw(mask).polygon([tuple(p) for p in FR["strip"]], fill=255)
    m = np.asarray(mask) > 0
    out = {}
    for k in OPTS:
        a = np.asarray(Image.open(R / f"opt.{k}.png").convert("RGB"))
        d = Lstar(a) - Lstar(base)
        out[k] = dict(lit_px=int(((d >= 5) & m).sum()), strip_px=int(m.sum()), mean_lift=round(float(d[m].mean()), 2))
    return out


def lch(h):
    L, a, b = lab(h)
    return round(L, 1), round(math.hypot(a, b), 1), round(math.degrees(math.atan2(b, a)) % 360, 1)


BRASS = [("#99784D", "the palette's brass (DK_Brass_Civic, BRASS)"), ("#AD866F", "the GDD's light brass (BRASS_LIGHT)"), ("#BF917C", "one lighter step: the lightest at this hue that holds 20 (rose, not brass)")]


def brass_rows():
    rows = []
    for h, why in BRASS:
        d = {k: round(de(h, v), 1) for k, v in SIGNALS.items()}
        rows.append(dict(hex=h, why=why, LCh=lch(h), dE=d, min=min(d.values()), gate="pass" if min(d.values()) >= 20 else "FAIL", dE_asphalt=round(de(h, ASPHALT), 1), dE_pavement=round(de(h, PAVE), 1),
                         dL_asphalt=round(lch(h)[0] - lch(ASPHALT)[0], 1), dL_pavement=round(lch(h)[0] - lch(PAVE)[0], 1)))
    return rows


# ----------------------------------------------------------------------------- the sheet

REC = {"d1": ((40, 140, 120), 5, "recommended"), "c": ((140, 185, 170), 3, "fallback")}
BG, INK, MUTE, RULE = (247, 243, 234), (34, 34, 34), (96, 92, 84), (205, 198, 182)


def font(sz, bold=False):
    for p, idx in (("/System/Library/Fonts/HelveticaNeue.ttc", 1 if bold else 0), ("/System/Library/Fonts/Helvetica.ttc", 1 if bold else 0)):
        try:
            return ImageFont.truetype(p, sz, index=idx)
        except OSError:
            continue
    return ImageFont.load_default()


class Sheet:
    def __init__(self, w, h):
        self.im = Image.new("RGB", (w, h), BG)
        self.d = ImageDraw.Draw(self.im)
        self.w, self.y, self.m = w, 28, 36

    def text(self, x, y, s, sz=16, bold=False, fill=INK):
        self.d.text((x, y), s, font=font(sz, bold), fill=fill)

    def wrap(self, x, y, s, width, sz=16, bold=False, fill=INK, lead=1.38, draw=True):
        """Draw (or, draw=False, only measure) wrapped text; returns the y below it."""
        f = font(sz, bold)
        for para in s.split("\n"):
            line = ""
            for word in para.split(" "):
                t = (line + " " + word).strip()
                if self.d.textlength(t, font=f) > width and line:
                    if draw:
                        self.d.text((x, y), line, font=f, fill=fill)
                    y += int(sz * lead)
                    line = word
                else:
                    line = t
            if draw:
                self.d.text((x, y), line, font=f, fill=fill)
            y += int(sz * lead)
        return y

    def box(self, title, body, sz=15):
        """A tinted recommendation box sized to its text; returns nothing, moves the cursor below it."""
        w = self.w - 2 * self.m - 32
        end = self.wrap(0, 0, body, w, sz, draw=False)
        h = 46 + end + 14
        top = self.y
        self.d.rectangle([self.m, top, self.w - self.m, top + h], fill=(238, 244, 240), outline=(120, 170, 156), width=2)
        self.text(self.m + 16, top + 10, title, 20, True)
        self.wrap(self.m + 16, top + 42, body, w, sz)
        self.y = top + h + 16

    def rule(self, y):
        self.d.line([(self.m, y), (self.w - self.m, y)], fill=RULE, width=1)

    def paste(self, im, x, y):
        self.im.paste(im, (x, y))


def crop(path, box, scale=1):
    im = Image.open(path).convert("RGB").crop(box)
    if scale != 1:
        im = im.resize((im.width * scale, im.height * scale), Image.NEAREST)
    return im


def fit_h(path, h):
    im = Image.open(path).convert("RGB")
    return im.resize((round(im.width * h / im.height), h), Image.LANCZOS)


def build():
    pc, dist, lit, brass = per_crossing(), district(), lit_pixels(), brass_rows()
    W = 2316
    s = Sheet(W, 5200)
    m = s.m
    s.text(m, s.y, "Brief 009: the crossings' presence at play zoom, and the brass", 34, True)
    s.y += 50
    y = s.wrap(m, s.y, "Vela, 15 Oct 2026. Candidates only: nothing is adopted; the plan, the world export, Unity and the kit catalogue are untouched. Pieces in out/decokit-009/, generator tools/decokit/crossing_opts.py, renders tools/decokit/render009opts.py, this sheet tools/decokit/options009.py.\n"
               "Camera = the game's: orthographic, 28 m of street tall (zoom 14), pitch 38, yaw 30, 1920 x 1080, 38.6 px per metre; every image below is 1:1 pixels unless it says 3x. One 12 m road (the district's commonest: 42 of 95 crossings), the 4 m strip, lights from 0.5 m off each kerb. "
               "The stage is lit to the Unity capture of the cafe crossing: asphalt (139,128,128), sunlit pavement (209,191,162), a seafoam dot (154,219,198), no bloom. Everything else on the crossing is the adopted kit (orbs, studs, arcs, brass edge line) and identical in every frame.", W - 2 * m, 16, fill=MUTE)
    s.y = y + 8
    s.rule(s.y)
    s.y += 16
    # ---- section 1
    s.text(m, s.y, "1  Presence: two thin dotted lines against a lit band", 24, True)
    s.y += 38
    CW, CH = 720, 400
    box = (620, 340, 1340, 740)
    refs = [(crop(REPO / "briefs/009/shots/009c-cafe-crossing-play.png", (560, 250, 1280, 650)), "Unity, as built (009c, the cafe crossing; 1:1)"),
            (fit_h(REPO / "briefs/009/concepts/crossing-future-play-2.jpg", CH), "concept: crossing-future-play-2 (dots in bands)"),
            (fit_h(REPO / "briefs/009/concepts/crossing-future-play-avenue.jpg", CH), "concept: crossing-future-play-avenue (a lit strip)")]
    x = m
    for im, cap in refs:
        s.paste(im, x, s.y)
        s.text(x, s.y + CH + 4, cap, 15, fill=MUTE)
        x += im.width + 14
    s.y += CH + 34
    gap = (W - 2 * m - 3 * CW) // 2
    for i, k in enumerate(OPTS):
        col, row = i % 3, i // 3
        x0, y0 = m + col * (CW + gap), s.y + row * (CH + 112)
        s.paste(crop(R / f"opt.{k}.png", box), x0, y0)
        if k in REC:
            s.d.rectangle([x0 - 3, y0 - 3, x0 + CW + 2, y0 + CH + 2], outline=REC[k][0], width=REC[k][1])
        L, short, what, _, _, wd = OPTS[k]
        s.text(x0, y0 + CH + 5, f"{L} {short}" + (f"   {REC[k][2]}" if k in REC else ""), 19, True)
        s.wrap(x0, y0 + CH + 30, what, CW, 14, fill=MUTE, lead=1.3)
        c = pc[k]
        s.text(x0, y0 + CH + 76, f"{c['modules']} modules, {c['tris']:,} tris per crossing   lit {lit[k]['lit_px']:,} px ({100 * lit[k]['lit_px'] / lit[k]['strip_px']:.1f}% of the strip)", 14)
    s.y += 2 * (CH + 112) + 8
    # table
    heads = ["option", "light per line", "pieces (tris L0)", "per 12 m crossing\nmodules / tris", "district, 95 crossings\nmodules / tris", "lit px at play zoom", "needs"]
    needs = {"a": "nothing (in the world)", "b2": "2 pieces, then Splice re-places (same 2 m / 1 m grid)", "b3": "2 pieces, then Splice re-places (same grid)", "c": "plan form 'bars' + 2 end pieces; LightBar_4 alone leaves gaps",
             "d1": "glow shader: fade by |v| (UV across the line); a new quad row", "d2": "glow shader: fade by |v|; a new quad row"}
    pieces = {k: ", ".join(f"{n.replace('Light', '')} {tris(n)}" for n in list(v[3].values()) + (list(v[4][0].values()) if v[4] else [])) for k, v in OPTS.items()}
    cols = [m, m + 250, m + 520, m + 960, m + 1230, m + 1520, m + 1710]
    s.d.rectangle([m, s.y, W - m, s.y + 44], fill=(232, 226, 211))
    for cx, h in zip(cols, heads):
        s.wrap(cx + 6, s.y + 4, h, 280, 13, True, lead=1.15)
    ty = s.y + 50
    for k, (L, short, _, _, _, wd) in OPTS.items():
        s.text(cols[0] + 6, ty, f"{L} {short}", 14, True)
        s.text(cols[1] + 6, ty, {"d1": "3.9 m: the strip", "d2": "1.3 m, soft edge"}.get(k, f"{wd:g} m"), 14)
        s.wrap(cols[2] + 6, ty, pieces[k], 430, 13, lead=1.15)
        s.text(cols[3] + 6, ty, f"{pc[k]['modules']} / {pc[k]['tris']:,}", 14)
        s.text(cols[4] + 6, ty, f"{dist[k]['modules']:,} / {dist[k]['tris']:,}", 14)
        s.text(cols[5] + 6, ty, f"{lit[k]['lit_px']:,}  (x{lit[k]['lit_px'] / lit['a']['lit_px']:.1f})", 14)
        s.wrap(cols[6] + 6, ty, needs[k], W - m - cols[6] - 10, 13, lead=1.15)
        ty += 40
        s.d.line([(m, ty - 6), (W - m, ty - 6)], fill=RULE)
    s.y = ty + 8
    s.y = s.wrap(m, s.y, f"Counts exclude what every option shares: the brass edge line (12 modules, 24 tris on a 12 m crossing), four orbs, twelve studs, four arcs. District = the live export's {dist['_crossings']} crossings, {dist['_line_spans']} unbroken light lines, {dist['_line_metres']} m; "
                         "today's dots are 980 modules and 80,832 tris. 'Lit px' = strip pixels whose L* is 5 or more above the same frame without light lines (same people and shadows), 1920 x 1080.", W - 2 * m, 13, fill=MUTE)
    s.y += 6
    # recommendation (presence)
    ratio = lambda k: lit[k]["lit_px"] / lit["a"]["lit_px"]  # noqa: E731
    s.box("Recommendation (presence): (d1) the lit strip; (c) the light bars if Sky wants lines and no soft light",
          f"1. (d1) is the only option that reads as a band at play zoom: {ratio('d1'):.0f}x the lit pixels of (a), against {ratio('b3'):.1f}x for three-wide dots and {ratio('c'):.1f}x for bars.\n"
          f"2. It costs {pc['d1']['tris'] - pc['a']['tris']} triangles a crossing (one quad per 2 m) and keeps today's dots, studs, arcs and brass edge line: the dots become the rungs of a lit lane, as in play-avenue and play.\n"
          "3. It is the cue the game needs: a strip Latch can light on walk and darken on stop reads across the whole screen; two thin lines cannot (pillar: a window the player can see coming).\n"
          "4. Owed, not done: Latch's glow shader needs a fade by |v| (the UV across the line; the beam's halo already fades by radius), and Glint sets the strength (here 0.34 of the dot colour, additive). "
          "It is the nearest thing to the landing slab Sky ruled out, but it is light, not paving: no surface, soft edges, between the edge lines only. Sky's call.\n"
          f"If not: (c) is {pc['c']['tris'] / pc['a']['tris'] * 100:.0f}% of today's triangles ({dist['c']['tris']:,} against {dist['a']['tris']:,} in the district) for {ratio('c'):.1f}x the light, continuous, no new shader; it needs the plan's form 'bars' and the two end bars made here "
          f"(LightBar_4 alone leaves 3 m of an 11 m line dark). (b3) reads as a band of lights but is the busiest and costs {pc['b3']['tris'] / pc['a']['tris']:.1f}x; (b2) buys little; (d2) glows but loses the dots in it.")
    s.rule(s.y)
    s.y += 16
    # ---- section 2: brass
    s.text(m, s.y, "2  Brass: studs and the edge line in three browns, two shadings", 24, True)
    s.y += 38
    s.y = s.wrap(m, s.y, "Left pair: the kit material as built (kind metal: metallic 1.0, smoothness 0.7, which is what the Unity .mat holds). Right pair: a satin brass (metallic 0.3, smoothness 0.5). Same crossing, same light, the east kerb end: three studs, the brass edge line, the dot line with its hairlines, the orb's bezel. "
                         "1:1 is what the game draws; 3x is the same pixels enlarged. The studio's reflection environment has a dark zenith, as a Unity probe with no sky above a street does: a flat metal facing up mirrors it.", W - 2 * m, 15, fill=MUTE)
    s.y += 6
    BOX1, BOX3 = (1110, 460, 1330, 620), (1180, 500, 1290, 580)
    colx = [m + 330, m + 330 + 230, m + 330 + 460, m + 330 + 460 + 340]
    for i, (h, b) in enumerate(zip(("as built, metal", "satin", "as built, metal, 3x", "satin, 3x"), colx)):
        s.text(b, s.y, h, 15, True, MUTE)
    s.y += 24
    for r in brass:
        hx = r["hex"][1:]
        top = s.y
        s.d.rectangle([m, top, m + 64, top + 64], fill=tuple(int(hx[j:j + 2], 16) for j in (0, 2, 4)), outline=(90, 90, 90))
        s.text(m + 76, top - 2, r["hex"], 22, True)
        s.text(m + 76, top + 26, f"L* {r['LCh'][0]}  C {r['LCh'][1]}  h {r['LCh'][2]}", 13, fill=MUTE)
        s.wrap(m, top + 74, r["why"], 310, 13, fill=MUTE, lead=1.25)
        s.text(m, top + 120, f"dE2000 from cones {r['dE']['cones']}", 14)
        s.text(m, top + 140, f"guard {r['dE']['guard']}   rivals {r['dE']['rivals']}", 14)
        s.text(m, top + 160, f"gate (>= 20): {r['gate']}, min {r['min']}", 14, True)
        s.text(m, top + 184, f"vs asphalt dE {r['dE_asphalt']} (dL {r['dL_asphalt']:+g})", 13, fill=MUTE)
        s.text(m, top + 202, f"vs pavement dE {r['dE_pavement']} (dL {r['dL_pavement']:+g})", 13, fill=MUTE)
        for shade, bx in (("metal", colx[0]), ("satin", colx[1])):
            s.paste(crop(R / f"brass.{hx}.{shade}.png", BOX1), bx, top)
        for shade, bx in (("metal", colx[2]), ("satin", colx[3])):
            s.paste(crop(R / f"brass.{hx}.{shade}.png", BOX3, 3), bx, top)
        s.y = top + 250
    s.y = s.wrap(m, s.y, "dE2000 is the unlit base colour against the signal colours (contracts/style/suborned.style.json: cones #E03A2F, guard #F0A020, rivals #8A4FD0), the gate the palette keeps at 20. "
                         "'vs asphalt' and 'vs pavement' are the same measure against what a brass line and a stud sit on in the Unity capture, (139,128,128) and (209,191,162): how far the brown stands off the street.", W - 2 * m, 13, fill=MUTE)
    s.y += 6
    b0, b1, b2 = brass
    s.box("Recommendation (brass): keep #99784D; change the shading, not the colour",
          "1. The black is the material. DK_Brass_Civic is kind metal: metallic 1.0, smoothness 0.7 (out/decokit/Materials/DK_Brass_Civic.mat). A metal has no diffuse colour; it shows what it reflects, and facing up under a dark zenith that is black. The same hex drawn satin (metallic 0.3) reads as brass.\n"
          f"2. A lighter hex does not fix it and reads worse: #AD866F passes the gate ({b1['min']}) but is rose, and stands off the asphalt {b1['dE_asphalt']} against {b0['dE_asphalt']} for #99784D, and off the pavement {b1['dE_pavement']} against {b0['dE_pavement']}. "
          f"The lighter step that holds, #BF917C ({b2['min']} from the guard colour), is copper-pink and weaker still ({b2['dE_asphalt']} and {b2['dE_pavement']}). The gate forces brass-hued colours dark: the guard amber is at hue 74, brass at 76.\n"
          "3. The change is one row: a satin kind in mats.py for DK_Brass_Civic, DK_Gold and DK_Accent_Gold (every street-level brass piece; the crowns keep their gold and their metal), then Splice re-hashes and adopts. Not done here.\n"
          "4. Known cost: satin brass on asphalt is a warm line, not a dark one, so the 4 cm edge line is fainter at 38.6 px per metre (1.5 px). If Sky wants it firmer it is one number (EW 0.04 to 0.06 in crossing_flat.py; SF-21 allows 0.6).\n"
          "5. Blender is not Unity: the near-black there is the same effect, but Latch or Glint should confirm it with the probe above a crossing before the row changes.")
    s.rule(s.y)
    s.y += 12
    s.wrap(m, s.y, "Corrections: (1) the 009 A play-zoom frames (render009.py) set the orthographic scale on the frame's width, which draws 68.6 px per metre: they were 1.78x too close and their '1:1' captions were wrong. This sheet is at 38.6 px per metre (checked from the projection: the strip's x axis runs 33.4 / -11.9 px per metre on screen, 35.5 in all). "
                    "(2) Candidates exported to out/decokit-009: LightDotBand2_2/_1, LightDotBand3_2/_1, LightBar_2/_1, LightPool_2/_1, LightField_2/_1 (tris in the table); nothing else in that folder changed. Glow strengths in the renders (pool 0.42, field 0.34 of the dot colour) are mine; Latch and Glint set the real ones.", W - 2 * m, 13, fill=MUTE)
    s.y += 74
    return s, pc, dist, lit, brass


def main():
    s, pc, dist, lit, brass = build()
    DEST.mkdir(parents=True, exist_ok=True)
    sheet = s.im.crop((0, 0, s.w, s.y))
    sheet.save(DEST / "009-crossing-options.png", optimize=True)
    (DEST / "009-crossing-options.json").write_text(json.dumps(dict(
        schema="suborned.crossing-options/1", brief="009", date="2026-10-15", camera="orthographic 28 m tall (zoom 14), pitch 38, yaw 30, 1920x1080, 38.6 px/m", road="12 m, lights 0.5 m off each kerb (11 m lines)",
        per_crossing=pc, district=dist, lit_px=lit, brass=brass, signals=SIGNALS,
        pieces={n: dict(tris=MAN[n]["tris"], l1=MAN[n]["lods"][0]["tris"], size=MAN[n]["size"]) for n in sorted({p for v in OPTS.values() for d in (v[3], v[4][0] if v[4] else {}) for p in d.values()})}), indent=1) + "\n")
    print(DEST / "009-crossing-options.png", sheet.size)


if __name__ == "__main__":
    main()
