# Glyph (2026-10-25), briefs 058 and 059: the board library. Redraws HUD pieces from the game's own kit sprites, fonts and the as-built geometry
# (HudToolkit.Ring / Done / ActMark) over real play-zoom shots (out/batch/shots, briefs/023/latch), at 2 px per game px (the 4K @2x look).
# No colour here lacks a source: kit sprites, kit.uss variables, the style contract's signal colours (cones, guard, rivals) for the world marks only.
# Layers per frame: base (the shot), world (marks on the ground and cones, drawn in the world), hud (plates, pills, chips, rings, markers).
# The budget is measured on the hud layer plus the shot's own HUD (the .hud.png of the same capture).
import sys, os, math
sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/057")
import kitdraw as kd
kd.S = 2.0
from kitdraw import K, R, CREAM, INKDEEP, TURQL, DIM, CHROME, SLOTINK, INK
from PIL import Image, ImageDraw, ImageFont, ImageFilter
import numpy as np, cv2

S = 2.0
AMBER = (240, 160, 32); RED = (224, 58, 47); VIOLET = (138, 79, 208)        # style contract signal_colours: guard, cones, rivals
PLATE = (20, 56, 54); DEEPTEAL = (31, 108, 103); TAILBODY = (12, 52, 52)      # ring wedge, lit wedge (kit.uss --deep-teal), pill tail body (the sprite's)
PPM = 38.6                     # play zoom 14 at 38 deg: game px per metre across the ground; depth is compressed by sin 38 deg
SQ = 0.616
OUT = R + "briefs/"

def G(v): return v * S
def F(name, px): return kd.font(name, px)
def JB(px): return F("Jost-Bold.ttf", px)

# ----------------------------------------------------------------------------- frames
class Frame:
    """a 1920 x 1080 game frame at S px per game px, with a world layer and a hud layer on top of the shot"""
    def __init__(self, shot, crop_game=None, blot=None):
        im = Image.open(shot).convert("RGB")
        if blot:                                                             # boxes (game px) of marks to take out of the shot before redrawing
            a = np.array(im); m = np.zeros(a.shape[:2], "uint8"); k = im.width / 1920.0
            for x0, y0, x1, y1 in blot: m[int(y0 * k):int(y1 * k), int(x0 * k):int(x1 * k)] = 255
            im = Image.fromarray(cv2.inpaint(a, m, 4, cv2.INPAINT_TELEA))
        W, H = int(1920 * S), int(im.height * 1920 / im.width * S)
        self.base = im.resize((W, H), Image.LANCZOS).convert("RGBA")
        self.world = Image.new("RGBA", self.base.size, (0, 0, 0, 0)); self.hud = Image.new("RGBA", self.base.size, (0, 0, 0, 0))
        self.hud_extra = []
    def compose(self, world=True, hud=True):
        o = self.base.copy()
        if world: o.alpha_composite(self.world)
        if hud: o.alpha_composite(self.hud)
        return o
    def crop(self, box, **kw):
        x0, y0, x1, y1 = [int(v * S) for v in box]; return self.compose(**kw).crop((x0, y0, x1, y1))

# ----------------------------------------------------------------------------- strokes (supersampled)
def _mask_paste(layer, mask, box, color, alpha):
    x0, y0 = box[0], box[1]; m = mask.resize(((box[2] - box[0]), (box[3] - box[1])), Image.LANCZOS)
    if alpha < 255: m = m.point(lambda v: int(v * alpha / 255))
    tile = Image.new("RGBA", m.size, tuple(color) + (255,)); tile.putalpha(m); layer.alpha_composite(tile, (x0, y0))

def stroke(layer, pts, color, w, dash=None, alpha=255, closed=False, fill=None, fill_alpha=255, round_caps=False):
    """polyline in game px; w in game px; dash = (on, off) in game px"""
    Q = 3; P = [(x * S, y * S) for x, y in pts]
    if closed: P = P + [P[0]]
    pad = int(w * S * 2 + 4); xs = [p[0] for p in P]; ys = [p[1] for p in P]
    x0, y0 = int(min(xs)) - pad, int(min(ys)) - pad; x1, y1 = int(max(xs)) + pad, int(max(ys)) + pad
    x0, y0 = max(0, x0), max(0, y0); x1, y1 = min(layer.width, x1), min(layer.height, y1)
    if x1 <= x0 or y1 <= y0: return
    m = Image.new("L", ((x1 - x0) * Q, (y1 - y0) * Q), 0); d = ImageDraw.Draw(m)
    Pq = [((p[0] - x0) * Q, (p[1] - y0) * Q) for p in P]; wq = max(1, int(round(w * S * Q)))
    if fill is not None:
        fm = Image.new("L", m.size, 0); ImageDraw.Draw(fm).polygon(Pq, fill=255); _mask_paste(layer, fm, (x0, y0, x1, y1), fill, fill_alpha)
    if dash is None:
        d.line(Pq, fill=255, width=wq, joint="curve")
        if round_caps:
            r = wq / 2
            for p in (Pq[0], Pq[-1]): d.ellipse((p[0] - r, p[1] - r, p[0] + r, p[1] + r), fill=255)
    else:
        on, off = dash[0] * S * Q, dash[1] * S * Q; t = 0.0; draw_on = True; rem = on
        for a, b in zip(Pq[:-1], Pq[1:]):
            L = math.dist(a, b)
            if L == 0: continue
            pos = 0.0
            while pos < L:
                step = min(rem, L - pos); p0 = (a[0] + (b[0] - a[0]) * pos / L, a[1] + (b[1] - a[1]) * pos / L); p1 = (a[0] + (b[0] - a[0]) * (pos + step) / L, a[1] + (b[1] - a[1]) * (pos + step) / L)
                if draw_on: d.line([p0, p1], fill=255, width=wq)
                pos += step; rem -= step
                if rem <= 1e-6: draw_on = not draw_on; rem = on if draw_on else off
    _mask_paste(layer, m, (x0, y0, x1, y1), color, alpha)

def gline(fr, pts, color=CREAM, w=2.4, dash=None, alpha=255, under=True, closed=False):
    """a world mark: cream (or a signal colour) over an ink-deep underlay at 60 % (the cones' and the route marks' language, brief 057)"""
    if under: stroke(fr.world, pts, INKDEEP, w + 2.6, dash, int(0.6 * alpha), closed)
    stroke(fr.world, pts, color, w, dash, alpha, closed)

def ground_pt(c, d_m, phi_deg):
    """a ground point d_m metres from c at ground angle phi (0 = screen right); the ground is foreshortened by sin 38"""
    a = math.radians(phi_deg); return (c[0] + math.cos(a) * d_m * PPM, c[1] + math.sin(a) * d_m * PPM * SQ)

def ellipse_pts(c, r_m, a0=0, a1=360, n=96):
    return [ground_pt(c, r_m, a0 + (a1 - a0) * i / n) for i in range(n + 1)]

def gring(fr, c, r_m, color=CREAM, w=2.4, dash=None, alpha=255, under=True, a0=0, a1=360):
    gline(fr, ellipse_pts(c, r_m, a0, a1), color, w, dash, alpha, under, closed=(a1 - a0 == 360))

def chevrons(fr, pts, every_m=3.0, color=CREAM, size=6.0, alpha=255):
    """filled chevrons along a path, pointing the way it goes (the route marks' language)"""
    acc = 0.0; step = every_m * PPM
    for a, b in zip(pts[:-1], pts[1:]):
        L = math.dist(a, b); ux, uy = (b[0] - a[0]) / L, (b[1] - a[1]) / L; pos = step / 2 - acc
        while pos < L:
            cx, cy = a[0] + ux * pos, a[1] + uy * pos; nx, ny = -uy, ux
            tri = [(cx + ux * size, cy + uy * size), (cx - ux * size * 0.6 + nx * size * 0.75, cy - uy * size * 0.6 + ny * size * 0.75), (cx - ux * size * 0.6 - nx * size * 0.75, cy - uy * size * 0.6 - ny * size * 0.75)]
            stroke(fr.world, tri, INKDEEP, 2.6, alpha=int(.6 * alpha), closed=True, fill=INKDEEP, fill_alpha=int(.6 * alpha))
            stroke(fr.world, tri, color, 0.8, alpha=alpha, closed=True, fill=color, fill_alpha=alpha)
            pos += step
        acc = (acc + L) % step

def diamond_ticks(fr, pts, every_m=2.2, color=VIOLET, size=3.4):
    acc = 0.0; step = every_m * PPM
    for a, b in zip(pts[:-1], pts[1:]):
        L = math.dist(a, b); ux, uy = (b[0] - a[0]) / L, (b[1] - a[1]) / L; pos = step / 2 - acc
        while pos < L:
            cx, cy = a[0] + ux * pos, a[1] + uy * pos; dm = [(cx, cy - size), (cx + size, cy), (cx, cy + size), (cx - size, cy)]
            stroke(fr.world, dm, INKDEEP, 2.2, alpha=170, closed=True, fill=INKDEEP, fill_alpha=170); stroke(fr.world, dm, color, 0.8, closed=True, fill=color)
            pos += step
        acc = (acc + L) % step

def fan(fr, hub, reach_m, a0, a1, a_hub=0.13, a_far=0.01, rim_alpha=0.55, rim_w=1.6, dashed_far=True, fill=True, rim_all_dashed=False):
    """a camera's quiet cone (brief 057 §3): red, distance fade, dashed far edge, drawn unlit"""
    n = 22
    if fill:
        for i in range(n):
            r0, r1 = reach_m * i / n, reach_m * (i + 1) / n; al = a_hub + (a_far - a_hub) * (i + .5) / n
            p = ellipse_pts(hub, r1, a0, a1, 40); q = ellipse_pts(hub, max(r0, 1e-3), a0, a1, 40)[::-1]
            stroke(fr.world, p + q, RED, 0.1, alpha=0, closed=True, fill=RED, fill_alpha=int(al * 255))
    edge = ellipse_pts(hub, reach_m, a0, a1, 40)
    if rim_all_dashed:
        stroke(fr.world, [hub] + edge + [hub], RED, rim_w, dash=(5, 4), alpha=int(rim_alpha * 255))
    else:
        stroke(fr.world, [hub, edge[0]], RED, rim_w, alpha=int(rim_alpha * 255)); stroke(fr.world, [edge[-1], hub], RED, rim_w, alpha=int(rim_alpha * 255))
        stroke(fr.world, edge, RED, rim_w, dash=(5, 4) if dashed_far else None, alpha=int(rim_alpha * 255))

# ----------------------------------------------------------------------------- kit pieces
def nine_h(rel, w, h, sl):
    """horizontal nine-slice of an @2x sprite: left and right caps (sl px of the file) at the scale that makes the sprite h px tall"""
    im = kd.load(K + rel + "@2x.png"); f = h / im.height; c = int(round(sl * f)); sw, sh = im.size
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    L = im.crop((0, 0, sl, sh)).resize((c, h), Image.LANCZOS); Rr = im.crop((sw - sl, 0, sw, sh)).resize((c, h), Image.LANCZOS)
    M = im.crop((sl, 0, sw - sl, sh)).resize((max(1, w - 2 * c), h), Image.LANCZOS)
    out.paste(L, (0, 0)); out.paste(M, (c, 0)); out.paste(Rr, (w - c, 0)); return out

def plate_dark(layer, x, y, w, h, tint=None):
    """kit plate_dark at (x, y) with w x h game px (nine-slice, 24 file px, scaled to the height)"""
    pw, ph = int(round(w * S)), int(round(h * S)); im = kd.nine("markers/plate_dark", pw, ph, 24, ph / 60.0)
    if tint: a = np.array(im).astype(float); a[..., :3] = np.clip(a[..., :3] * np.array(tint), 0, 255); im = Image.fromarray(a.astype("uint8"), "RGBA")
    layer.alpha_composite(im, (int(round(x * S)), int(round(y * S)))); return im

def text(layer, xy, s, font, fill=CREAM, anchor="lm"):
    ImageDraw.Draw(layer).text((xy[0] * S, xy[1] * S), s, font=font, fill=fill, anchor=anchor)

def tw(s, font): return font.getlength(s) / S

def icon(layer, name, cx, cy, size, rgb=CREAM, alpha=255):
    im = kd.icon(name, size, rgb)
    if alpha < 255: im.putalpha(im.split()[3].point(lambda v: int(v * alpha / 255)))
    layer.alpha_composite(im, (int(round(cx * S - im.width / 2)), int(round(cy * S - im.height / 2))))

def disc(layer, cx, cy, size, ic, ring=True, light=False, icon_size=None, rgb=None):
    """a kit disc (disc_dark_24 / disc_light_24) with its icon, the size of a cameo, the icon bright and filling most of it"""
    rel = "markers/disc_light_24" if light else "markers/disc_dark_24"; d = kd.sprite(rel, game_h=size)
    layer.alpha_composite(d, (int(round(cx * S - d.width / 2)), int(round(cy * S - d.height / 2))))
    icon(layer, ic, cx, cy, icon_size or size * 0.62, rgb or (INK if light else CREAM))

def cameo(layer, cx, cy, size, rel_badge):
    im = kd.cameo_img(rel_badge, size); layer.alpha_composite(im, (int(round(cx * S - im.width / 2)), int(round(cy * S - im.height / 2))))

def cameo_png(layer, cx, cy, size, path):
    """a cameo of any square face art, in the kit's chrome ring (2 px #cbc6c3 over 1 px ink-deep)"""
    n = int(round(size * S)); Q = 4; ring = Image.new("RGBA", (n * Q, n * Q), (0, 0, 0, 0)); d = ImageDraw.Draw(ring)
    d.ellipse((0, 0, n * Q - 1, n * Q - 1), fill=INKDEEP + (255,)); r1 = int(round(1 * S / 2 * Q)); r2 = int(round(2 * S / 2 * Q)) + r1
    d.ellipse((r1, r1, n * Q - 1 - r1, n * Q - 1 - r1), fill=CHROME + (255,)); d.ellipse((r2, r2, n * Q - 1 - r2, n * Q - 1 - r2), fill=(11, 22, 21, 255))
    ring = ring.resize((n, n), Image.LANCZOS); inner = int(round((size - 2 * 2.2) * S)); f = Image.open(path).convert("RGBA").resize((inner, inner), Image.LANCZOS)
    m = Image.new("L", (inner, inner), 0); ImageDraw.Draw(m).ellipse((0, 0, inner - 1, inner - 1), fill=255); f.putalpha(m); ring.alpha_composite(f, ((n - inner) // 2, (n - inner) // 2))
    layer.alpha_composite(ring, (int(round(cx * S - n / 2)), int(round(cy * S - n / 2))))

# ----------------------------------------------------------------------------- the window marker (as built: plate_dark 38 px, icon + seconds, a 2.5 px draining underline, amber in the last 10 s)
def clock(s): return "%d:%02d" % (s // 60, s % 60)

def window_marker(fr, cx, bottom, ic, label, frac=1.0, low=False, face=None, dim=1.0):
    WH = 38.0; f = JB(14.5); lw = tw(label, f) if label else 0; w = 11 + 15 + 6 + lw + 12
    if face is not None: w = 11 + 15 + 6 + 16 + 12
    x = cx - w / 2; y = bottom - WH; lay = Image.new("RGBA", fr.hud.size, (0, 0, 0, 0))
    plate_dark(lay, x, y, w, WH); icon(lay, ic, x + 11 + 7.5, y + WH / 2 - 1, 15)
    if face is None: text(lay, (x + 11 + 15 + 6, y + WH / 2 - 0.5), label, f)
    else: cameo(lay, x + 11 + 15 + 6 + 8, y + WH / 2 - 1, 16, face)
    tx0, tx1, ty = x + 11, x + w - 11, y + WH - 9
    stroke(lay, [(tx0, ty + 1.25), (tx1, ty + 1.25)], INKDEEP, 2.5, alpha=153, round_caps=True)
    stroke(lay, [(tx0, ty + 1.25), (tx0 + (tx1 - tx0) * frac, ty + 1.25)], AMBER if low else TURQL, 2.5, round_caps=True)
    if dim < 1: lay.putalpha(lay.split()[3].point(lambda v: int(v * dim)))
    fr.hud.alpha_composite(lay); return (x, y, w, WH)

# ----------------------------------------------------------------------------- the done chip (panel_dark at slice 0.5, the doer's face with the outcome badge, icon-and-word rows, an unlock row)
def chip(fr, cx, bottom, face_badge, tone, rows, unlocks=(), rise=0):
    fw = JB(15); rw = max(16 + 8 + tw(r[1], fw) for r in rows)
    uw = 0
    if unlocks: uw = 12 + sum(5 + 9 + 12 + 5 + tw(u[1], JB(12.5)) + 10 for u in unlocks)
    w = 17 + 32 + 11 + max(rw, uw) + 2 + 17; h = 13 + 22 * len(rows) + 12 + ((4 + 6 + 30) if unlocks else 0)
    x = cx - w / 2; y = bottom - h - rise
    pw, ph = int(round(w * S)), int(round(h * S)); im = kd.nine("panels/panel_dark", pw, ph, 35, S / 2)
    lay = Image.new("RGBA", fr.hud.size, (0, 0, 0, 0)); lay.alpha_composite(im, (int(round(x * S)), int(round(y * S))))
    fcx, fcy = x + 17 + 16, y + 13 + 16 - 2; cameo(lay, fcx, fcy, 32, face_badge)
    mk = kd.sprite("badges/mark_" + tone, game_h=19); lay.alpha_composite(mk, (int(round((fcx + 16 + 6 - 19) * S)), int(round((fcy + 16 + 5 - 19) * S))))
    for i, (ic, wd) in enumerate(rows):
        cy = y + 13 + 22 * i + 11; icon(lay, ic, x + 17 + 32 + 11 + 8, cy, 16); text(lay, (x + 17 + 32 + 11 + 16 + 8, cy + 0.5), wd, fw)
    if unlocks:
        uy = y + 13 + 22 * len(rows) + 4 + 6 + 15; ux = x + 17 + 32 + 11
        stroke(lay, [(ux, uy - 21), (x + w - 17, uy - 21)], (153, 120, 77), 1.0, alpha=255)           # brass hairline (kit --brass), 1 px
        icon(lay, "chev_up", ux + 6, uy, 12, TURQL); ux += 12
        for ic, wd, shut in unlocks:
            ww = 9 + 12 + 5 + tw(wd, JB(12.5)) + 10; ux += 5
            plate_dark(lay, ux, uy - 15, ww, 30); icon(lay, ic, ux + 9 + 6, uy, 12); text(lay, (ux + 9 + 12 + 5, uy + 0.5), wd, JB(12.5))
            if shut: stroke(lay, [(ux + 6, uy), (ux + ww - 6, uy)], CREAM, 1.5)
            ux += ww
    fr.hud.alpha_composite(lay); return (x, y, w, h)

# ----------------------------------------------------------------------------- the verb pill (pill_status 38 px; offer, hold, ready) and its chrome tail
def vpill(fr, q, head, body, ic, faces=(), state="offer", frac=0.0, harm=False):
    """q = the point the tail's tip touches (game px); head = the dim first word ("Hold"/"Let go"), body = the price line"""
    H = 38.0; fh = JB(15.5); dsc = 30; padl = 19; padr = 20
    hw = tw(head, fh) if head else 0; dotw = tw("·", fh) + 10 if head else 0; bw = tw(body, fh)
    w = padl + dsc + 9 + (hw + dotw if head else 0) + bw + (10 + 24 + (len(faces) - 1) * 17 if faces else 0) + padr
    w = max(w, 2 * 40.2 + 20); x = q[0] - w / 2; y = q[1] - 10 - H - 11
    lay = Image.new("RGBA", fr.hud.size, (0, 0, 0, 0))
    if harm:
        im = kd.nine("panels/panel_strip", int(w * S), int(H * S), 25, 0.3 * S); lay.alpha_composite(im, (int(x * S), int(y * S)))
    else:
        im = nine_h("panels/pill_status", int(round(w * S)), int(round(H * S)), 93); lay.alpha_composite(im, (int(round(x * S)), int(round(y * S))))
    if frac > 0:
        cl = Image.new("L", lay.size, 0); cd = ImageDraw.Draw(cl); x0, y0, x1, y1 = (x + 15) * S, (y + 6) * S, (x + w - 15) * S, (y + H - 6) * S
        cd.rounded_rectangle((x0, y0, x0 + (x1 - x0) * frac, y1), radius=13 * S, fill=int(0.4 * 255)); fillimg = Image.new("RGBA", lay.size, TURQL + (255,)); fillimg.putalpha(cl); lay.alpha_composite(fillimg)
    cy = y + H / 2 - 0.5; cx = x + padl + dsc / 2
    disc(lay, cx, cy, dsc, ic, light=harm)
    tx = x + padl + dsc + 9
    if head:
        text(lay, (tx, cy + 0.6), head, fh, TURQL if state == "ready" else DIM); tx += hw + 5
        text(lay, (tx, cy + 0.6), "·", fh, DIM); tx += tw("·", fh) + 5
    text(lay, (tx, cy + 0.6), body, fh); tx += bw + 10
    for i, fb in enumerate(faces): cameo(lay, tx + 12 + i * 17, cy, 24, fb)
    if state == "ready":
        for k in range(10, 0, -1):
            stroke(lay, [(x - 2 - k / 2, y - 2 - k / 2), (x + w + 2 + k / 2, y - 2 - k / 2), (x + w + 2 + k / 2, y + H + 2 + k / 2), (x - 2 - k / 2, y + H + 2 + k / 2)], TURQL, k, alpha=int(.13 * .42 * 255), closed=True)
        rr = ImageDraw.Draw(lay); rr.rounded_rectangle(((x - 1) * S, (y - 1) * S, (x + w + 1) * S, (y + H + 1) * S), radius=20 * S, outline=TURQL + (255,), width=int(2 * S))
    fr.hud.alpha_composite(lay)
    # the tail: a chrome triangle with the pill's body inside it, its tip at q
    tl = Image.new("RGBA", fr.hud.size, (0, 0, 0, 0)); stroke(tl, [(q[0] - 9, q[1] - 13 + 2), (q[0] + 9, q[1] - 13 + 2), (q[0], q[1])], CHROME, 0.1, closed=True, fill=CHROME)
    stroke(tl, [(q[0] - 6, q[1] - 13), (q[0] + 6, q[1] - 13), (q[0], q[1] - 13 + 7.5)], TAILBODY, 0.1, closed=True, fill=TAILBODY); fr.hud.alpha_composite(tl)
    return (x, y, w, H)

# ----------------------------------------------------------------------------- the ring (as built: HudToolkit.Ring), n wedges, the pill outside each wedge on its side
RO, RI = 84.0, 30.0
def Dir(deg): a = math.radians(deg); return (math.sin(a), -math.cos(a))

def ring_wheel(fr, C, segs, lit=0, hold=None):
    """segs: list of dict(icon, violent, shut). hold = (index, progress): the 0.5 s hold of a harmful wedge, filling from the middle out"""
    n = len(segs); w = 360.0 / n; gap = 2.2 if n > 1 else 0.0; Q = 3
    pad = 14; x0, y0 = int((C[0] - RO - pad) * S), int((C[1] - RO - pad) * S); size = int((RO + pad) * 2 * S)
    lay = Image.new("RGBA", (size * Q // Q, size), (0, 0, 0, 0))
    def sector(r_out, r_in, a0, a1, steps=40):
        pts = [];
        for i in range(steps + 1): a = a0 + (a1 - a0) * i / steps; d = Dir(a); pts.append((C[0] + d[0] * r_out, C[1] + d[1] * r_out))
        for i in range(steps, -1, -1): a = a0 + (a1 - a0) * i / steps; d = Dir(a); pts.append((C[0] + d[0] * r_in, C[1] + d[1] * r_in))
        return pts
    L = fr.hud
    for i, s in enumerate(segs):
        a0 = i * w - w / 2 + gap; a1 = i * w + w / 2 - gap; isl = i == lit
        if n == 1: a0, a1 = 0.0, 359.99
        stroke(L, sector(RO, RI, a0, a1), PLATE, 0.1, closed=True, fill=DEEPTEAL if isl else PLATE, fill_alpha=255)
        if hold and hold[0] == i:
            hr = RI + (RO - RI) * hold[1]; stroke(L, sector(hr, RI, a0, a1), TURQL, 0.1, closed=True, fill=TURQL, fill_alpha=int(.75 * 255))
        edge = [(C[0] + Dir(a0 + (a1 - a0) * k / 40)[0] * (RO - 1), C[1] + Dir(a0 + (a1 - a0) * k / 40)[1] * (RO - 1)) for k in range(41)]
        if s.get("violent"): stroke(L, edge, TURQL if isl else CREAM, 1.6, dash=(((a1 - a0) / math.ceil((a1 - a0) / 9)) * math.pi / 180 * RO * 0.55, ((a1 - a0) / math.ceil((a1 - a0) / 9)) * math.pi / 180 * RO * 0.45))
        elif isl: stroke(L, edge, TURQL, 2.0)
    ring = [(C[0] + Dir(a)[0] * (RO + 3), C[1] + Dir(a)[1] * (RO + 3)) for a in range(0, 361, 4)]
    stroke(L, [(C[0] + Dir(a)[0] * (RI - 5), C[1] + Dir(a)[1] * (RI - 5)) for a in range(0, 361, 6)], PLATE, 0.1, closed=True, fill=PLATE, fill_alpha=int(.6 * 255))
    stroke(L, ring, INKDEEP, 4.0, closed=True); stroke(L, ring, CHROME, 2.0, closed=True)
    for i, s in enumerate(segs):
        d = Dir(i * w) if n > 1 else (0, -1); mid = ((RO + RI) / 2 * d[0], (RO + RI) / 2 * d[1]); isl = i == lit
        icon(L, s["icon"], C[0] + mid[0], C[1] + mid[1], 22, TURQL if isl else CREAM, alpha=int(255 * (.45 if s.get("shut") else 1)))

def ring_pill(fr, C, deg, tokens, lit=False):
    """the pill outside a wedge: placed as RingFrame does (right side: left edge at the wedge's outer tip; left side: right edge; top and bottom centred)"""
    d = Dir(deg); at = (d[0] * (RO + 10), d[1] * (RO + 10)); pw = kd.pill_width(tokens) / S      # plate width, game px
    right, left = d[0] > 0.2, d[0] < -0.2
    px = at[0] if right else at[0] - pw if left else at[0] - pw / 2
    py = at[1] - 15 + (0 if abs(d[0]) > 0.2 else (-14 if d[1] < 0 else 14))
    x_vis = (C[0] + px) * S + kd.G(4); yc = (C[1] + py + 15) * S
    box, _ = kd.draw_pill(fr.hud, tokens, x_vis, yc, lit=lit, width=int(pw * S))
    return (C[0] + px, C[1] + py, pw, 30)

# price tokens in the ring pill's own grammar (brief 057 kitdraw): word, gap, icon + number fields
def tokens(word, fields, loud=False):
    t = [("word", word)]
    for f in fields:
        t.append(("gap", 11))
        kind = f[0]
        if kind == "time": t += [("icon", "p_time"), ("gap", 1), ("num", f[1])]
        elif kind == "trail": t += [("icon", "p_trail"), ("gap", 1), ("num", f[1])]
        elif kind == "alert": t += [("icon", "p_alert"), ("gap", 1), ("num", f[1])]
        elif kind == "usd": t += [("icon", "p_cash"), ("gap", 1), ("num", f[1])]
        elif kind == "lasts": t += [("icon", "loop"), ("gap", 1), ("num", f[1])]
        elif kind == "in": t += [("gap", -5), ("icon", "nextevent", 11, DIM), ("gap", 2), ("num", f[1], DIM)]
        elif kind == "eye": t += [("icon", "eye_" + f[1], 17, CREAM if f[1] != "shut" else INK)]
    if loud: t += [("gap", 8), ("icon", "bang", 14)]
    return t

# ----------------------------------------------------------------------------- sheets
def board_font(px): return ImageFont.truetype(K + "fonts/Jost-Bold.ttf", px)
def body_font(px): return ImageFont.truetype(K + "fonts/Barlow-Medium.ttf", px)
BG = (28, 36, 36); BCREAM = (247, 244, 236)
def sheet(panels, cols, path, title, sub, cap_h=96, pad=28, max_w=2400, min_w=1500):
    """panels: list of (image, caption, small-caption). Writes a board: dark ground, cream captions (Jost Bold / Barlow Medium), crops at native 2 px per game px"""
    ims = [p[0].convert("RGB") for p in panels]; rows = (len(ims) + cols - 1) // cols
    colw = [max(im.width for k, im in enumerate(ims) if k % cols == c) for c in range(cols)]
    rowh = [max(im.height for k, im in enumerate(ims) if k // cols == r) + cap_h for r in range(rows)]
    W = max(min_w, sum(colw) + pad * (cols + 1)); H = 150 + sum(rowh) + pad * (rows + 1)
    out = Image.new("RGB", (W, H), BG); d = ImageDraw.Draw(out)
    d.text((pad, 28), title, font=board_font(46), fill=BCREAM); d.text((pad, 92), sub, font=body_font(26), fill=(176, 205, 199))
    y = 150
    for r in range(rows):
        x = pad
        for c in range(cols):
            k = r * cols + c
            if k >= len(ims): break
            out.paste(ims[k], (x, y + pad)); cap, small = panels[k][1], panels[k][2]
            d.text((x, y + pad + ims[k].height + 14), cap, font=board_font(30), fill=BCREAM); d.text((x, y + pad + ims[k].height + 54), small, font=body_font(23), fill=(176, 205, 199))
            x += colw[c] + pad
        y += rowh[r] + pad
    if out.width > max_w:
        k = max_w / out.width; out = out.resize((max_w, int(out.height * k)), Image.LANCZOS)
    out.save(path); return out.size

# ----------------------------------------------------------------------------- the budget: the shot's own HUD plus this frame's hud layer, as a share of the 1920 x 1080 screen
def budget(fr, hud_png, extra_world=False):
    base = np.array(Image.open(hud_png).convert("RGBA").resize(fr.hud.size, Image.NEAREST))[..., 3] > 8
    mine = np.array(fr.hud)[..., 3] > 8; both = base | mine; tot = fr.hud.size[0] * 1080 * S
    res = {"rest_baseline_pct": round(100 * base.sum() / (fr.hud.size[0] * fr.hud.size[1]), 2), "with_pct": round(100 * both.sum() / (fr.hud.size[0] * fr.hud.size[1]), 2)}
    if extra_world: wm = np.array(fr.world)[..., 3] > 8; res["with_world_pct"] = round(100 * (both | wm).sum() / (fr.hud.size[0] * fr.hud.size[1]), 2)
    return res

# ----------------------------------------------------------------------------- brief 059 additions: cars, stands, edge chips, route plates
CAR_L, CAR_W = 4.6, 1.9      # metres (the sedan's footprint)
def ground_xy(c, u, v, psi_deg):
    """a ground point u metres along heading psi and v metres to its left, from screen point c (the ground is foreshortened by sin 38)"""
    a = math.radians(psi_deg); gx = u * math.cos(a) - v * math.sin(a); gy = u * math.sin(a) + v * math.cos(a)
    return (c[0] + gx * PPM, c[1] + gy * PPM * SQ)

def car_footprint(c, psi, grow=0.0, n=8):
    L, W, r = CAR_L / 2 + grow, CAR_W / 2 + grow, 0.5 + grow; pts = []
    for (cx, cy, a0) in ((L - r, W - r, 0), (-L + r, W - r, 90), (-L + r, -W + r, 180), (L - r, -W + r, 270)):
        for i in range(n + 1): a = math.radians(a0 + 90 * i / n); pts.append(ground_xy(c, cx + r * math.cos(a), cy + r * math.sin(a), psi))
    return pts

def car_ghost(fr, c, psi, color=CREAM, dash=(6, 5), alpha=255, w=2.2, fill_alpha=0, icon_name="car", disc_size=24, with_disc=True, nose=True):
    pts = car_footprint(c, psi)
    stroke(fr.world, pts, INKDEEP, w + 2.6, dash, int(.6 * alpha), closed=True)
    stroke(fr.world, pts, color, w, dash, alpha, closed=True, fill=color if fill_alpha else None, fill_alpha=fill_alpha)
    if nose:                                                      # a chevron at the nose: which way the car faces
        n0 = ground_xy(c, CAR_L / 2 - 0.2, 0, psi); n1 = ground_xy(c, CAR_L / 2 - 1.1, 0.7, psi); n2 = ground_xy(c, CAR_L / 2 - 1.1, -0.7, psi)
        stroke(fr.world, [n1, n0, n2], INKDEEP, w + 2.6, alpha=int(.6 * alpha)); stroke(fr.world, [n1, n0, n2], color, w, alpha=alpha)
    if with_disc: disc(fr.hud, c[0], c[1] - 8, disc_size, icon_name)

def paste_sprite(layer, path, cx, cy, flip=False, scale=1.0):
    im = Image.open(path).convert("RGBA")
    if flip: im = im.transpose(Image.FLIP_LEFT_RIGHT)
    n = (int(im.width * S * scale), int(im.height * S * scale)); im = im.resize(n, Image.LANCZOS)
    layer.alpha_composite(im, (int(cx * S - n[0] / 2), int(cy * S - n[1] / 2)))

def edge_chip(fr, x, y, ic, rot_deg=0, face_badge=None):
    """the off-screen pointer: plate_dark 22 px at the screen edge with the thing's cameo (or icon) and the pointer icon turned toward it"""
    w = 52 if face_badge or ic else 30; plate_dark(fr.hud, x - w / 2, y - 11, w, 22)
    if face_badge: cameo(fr.hud, x - 11, y, 16, face_badge)
    elif ic: icon(fr.hud, ic, x - 10, y, 14)
    im = kd.icon("pointer", 14, CREAM).rotate(-rot_deg, resample=Image.BICUBIC, expand=True); fr.hud.alpha_composite(im, (int((x + 12) * S - im.width / 2), int(y * S - im.height / 2)))

def route_plate(fr, cx, cy, parts):
    """a small plate on a route: parts = [("icon", name) | ("txt", "6:13")]; plate_dark 28 px, Jost-Bold 13.5"""
    f = JB(13.5); w = 10 + sum((14 + 4) if p[0] == "icon" else tw(p[1], f) + 4 for p in parts) + 6; x = cx - w / 2; y = cy - 14
    plate_dark(fr.hud, x, y, w, 28); px = x + 10
    for p in parts:
        if p[0] == "icon": icon(fr.hud, p[1], px + 7, cy, 14 if p[1] != "pointer" else 12); px += 18
        else: text(fr.hud, (px, cy + 0.5), p[1], f); px += tw(p[1], f) + 4
    return (x, y, w, 28)
