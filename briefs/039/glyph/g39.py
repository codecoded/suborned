# Glyph, brief 039 (2026-10-26): the marks the three fight options are drawn with, on briefs/067/g67.py and briefs/058/glib.py.
# Everything is a kit sprite, a kit icon, a kit font or a kit.uss colour (cream, ink-deep, dim, chrome, turquoise-light). The signal colours (red threat ring,
# violet rivals, amber police) appear only on the things that already wear them (049's rings, the rivals' routes, a cover point a rival holds).
# Pictograms the kit lacks are icons39.py's (shield, shield_half, mark, steam, burst; the crosshair is the kit overlay as an icon).
import sys, math, os
sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/067"); sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/039/glyph")
from g67 import *
import icons39; icons39.install()
from scene39 import *

OUT39 = R + "briefs/039/glyph/"
BRASS = (153, 120, 77)

def ico(layer, name, cx, cy, size, rgb=CREAM, alpha=255): icon(layer, name, cx, cy, size, rgb, alpha)       # glib.icon is patched: new icons resolve by name
def disc39(layer, cx, cy, size, ic, light=False, rgb=None, ring=None, ring_w=1.8):
    disc(layer, cx, cy, size, ic, light=light, rgb=rgb)
    if ring:
        r = size / 2 - 0.5; stroke(layer, [(cx + r * math.cos(math.radians(a)), cy + r * math.sin(math.radians(a))) for a in range(0, 361, 10)], ring, ring_w, closed=True)
def circ(c, r, n=36, a0=0, a1=360): return [(c[0] + r * math.cos(math.radians(a0 + (a1 - a0) * i / n)), c[1] + r * math.sin(math.radians(a0 + (a1 - a0) * i / n))) for i in range(n + 1)]

# ----------------------------------------------------------------------------- over a head: pips, the firing mark
def pips(fr, cx, top, n, left, hostile=True, flash=0, pitch=7.0, dia=5.6):
    """health pips over a head, as built (a hostile's red, the cell's cream, a lost one grey); flash = the pips just lost, ringed for 0.25 s"""
    x0 = cx - (n - 1) * pitch / 2.0
    for i in range(n):
        c = (x0 + i * pitch, top); lost = i >= left; fl = lost and i < left + flash; col = (150, 150, 150) if lost else (RED if hostile else CREAM)
        stroke(fr.hud, circ(c, dia / 2 + 0.6, 24), INKDEEP, 0.1, closed=True, fill=INKDEEP, fill_alpha=100)
        stroke(fr.hud, circ(c, dia / 2 - 0.2, 24), col, 0.1, closed=True, fill=col)
        if fl:
            stroke(fr.hud, circ(c, dia / 2 + 3.0, 24), CREAM, 1.8, closed=True); stroke(fr.hud, circ(c, dia / 2 - 0.2, 24), CREAM, 0.1, closed=True, fill=CREAM)

def firing_mark(fr, c):
    """the pistol with rays over a firing agent (as built, HudToolkit.FireMarks)"""
    lay = layer(fr); d = kd.sprite("markers/disc_dark_24", game_h=24); lay.alpha_composite(d, (int(round(c[0] * S - d.width / 2)), int(round(c[1] * S - d.height / 2))))
    icon(lay, "fire", c[0] - 1.5, c[1] + 0.5, 14)
    for dy, dx in ((-5.5, 3.5), (-0.5, 4.5), (4.5, 3.5)): stroke(lay, [(c[0] + 6.5, c[1] + dy), (c[0] + 6.5 + dx, c[1] + dy + dy * 0.3)], CREAM, 1.5, round_caps=True)
    fr.hud.alpha_composite(lay)

# ----------------------------------------------------------------------------- shots: tracers, streaks, flash, hit, miss, cover
def tracer(fr, a, b, thin=False, alpha=255):
    """the cell's tracer (as built): cream dashes (9 on, 5 off) over an ink-deep underlay at 35 %, 1.5 px (1 px when returning fire); the host marches the dashes"""
    stroke(fr.hud, [a, b], INKDEEP, 3.4, dash=(9, 5), alpha=int(90 * alpha / 255)); stroke(fr.hud, [a, b], CREAM, 1.0 if thin else 1.5, dash=(9, 5), alpha=alpha)
def streak(fr, a, b, colour=VIOLET, ts=(0.2, 0.58), L=20):
    """a hostile's rounds in flight (as built, ShotMarks): short solid streaks in the shooter's colour, an ink underlay"""
    ux, uy = (b[0] - a[0]), (b[1] - a[1]); n = math.hypot(ux, uy); ux, uy = ux / n, uy / n
    for t in ts:
        p = (a[0] + ux * n * t, a[1] + uy * n * t); q = (p[0] + ux * L, p[1] + uy * L)
        stroke(fr.hud, [p, q], INKDEEP, 4.4, alpha=150, round_caps=True); stroke(fr.hud, [p, q], colour, 2.2, round_caps=True)
def muzzle(fr, c, ang=0.0, r=5.5):
    pts = []
    for i in range(8):
        a = math.radians(ang + i * 45); rr = r * 1.7 if i % 2 == 0 else r * 0.45; pts.append((c[0] + rr * math.cos(a), c[1] + rr * math.sin(a)))
    stroke(fr.hud, pts, INKDEEP, 2.6, alpha=150, closed=True, fill=INKDEEP, fill_alpha=150); stroke(fr.hud, pts, (255, 247, 220), 0.1, closed=True, fill=(255, 247, 220))
def spark(fr, c, t=0.2, size=1.0):
    """a hit: six rays bursting out (as built, ShotFx)"""
    for i in range(6):
        a = math.radians(i * 60 + 15); r0, r1 = (3 + 6 * t) * size, (9 + 10 * t) * size
        p = (c[0] + math.cos(a) * r0, c[1] + math.sin(a) * r0); q = (c[0] + math.cos(a) * r1, c[1] + math.sin(a) * r1)
        stroke(fr.hud, [p, q], INKDEEP, 4.0, alpha=120, round_caps=True); stroke(fr.hud, [p, q], (255, 242, 190), 2.2, round_caps=True)
def puff(fr, c, r=7.0, alpha=210):
    """a miss: a small cream ring where the round ends, past the target (as built)"""
    stroke(fr.hud, circ(c, r, 24), CREAM, 1.6, alpha=alpha, closed=True)
def chip_puff(fr, c):
    """cover took the round: a stone chip puff on the prop (four cream flecks and a ring), so a shot that lands on cover is seen landing on the cover"""
    for dx, dy, r in ((-5, -3, 2.2), (4, -6, 1.8), (5, 3, 2.0), (-2, 5, 1.6)): stroke(fr.hud, circ((c[0] + dx, c[1] + dy), r, 12), CREAM, 0.1, closed=True, fill=CREAM)
    stroke(fr.hud, circ(c, 8.5, 24), CREAM, 1.5, alpha=190, closed=True)

# ----------------------------------------------------------------------------- the rings (049): colour says whose, red says it threatens the cell, shape says reach, four ticks say a shot is in the exchange
def ring39(fr, feet, side="rival", threat=False, ours=False, reach="in", hover=False, r_m=0.54):
    col = RED if threat else {"rival": VIOLET, "police": AMBER, "civ": CREAM}[side]; rad = 0.68 if hover and not threat else r_m
    w = 3.4 if threat or hover else 2.8 if reach == "in" else 2.0
    pts = ellipse_pts(feet, rad)
    gline(fr, pts, col, w, dash=(2.2, 3.0) if reach == "out" else None, closed=True)
    if threat or ours:
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):                                   # square to the screen: along the ring's own axes
            if dx: a = (feet[0] + dx * (rad + 0.07) * PPM, feet[1]); b = (feet[0] + dx * (rad + 0.24) * PPM, feet[1])
            else: a = (feet[0], feet[1] + dy * (rad + 0.07) * PPM * SQ); b = (feet[0], feet[1] + dy * (rad + 0.24) * PPM * SQ)
            stroke(fr.world, [a, b], INKDEEP, 4.4, alpha=210); stroke(fr.world, [a, b], CREAM, 2.4)

def under_fire_arc(fr, feet, toward, r_m=0.62):
    """as built (HudToolkit.UnderFireMarks): a 70 degree red arc just outside the agent's ring, centred on whoever shoots him"""
    phi = math.degrees(math.atan2((toward[1] - feet[1]) / SQ, toward[0] - feet[0])); pts = ellipse_pts(feet, r_m, phi - 35, phi + 35, 20)
    stroke(fr.world, pts, INKDEEP, 5.4, alpha=150, round_caps=True); stroke(fr.world, pts, RED, 3.4, round_caps=True)

# ----------------------------------------------------------------------------- the lock reticle (067 D, built), with the reason it waits in its disc
REASON_ICON = {"aim": ("crosshair", 16), "walk": ("s_moving", 14), "noline": ("close_x", 12)}
def reticle39(fr, c, state="locked", cams=(), reason="aim", turn=0.0, scale=1.0, alpha=1.0, flash=0.0):
    """state: locked | held | set | released. reason (held only): aim = drawing / aiming 1 s, walk = walking in, noline = no line. Disc 26 px, the shooters' cameos 16 px under it."""
    lay = layer(fr); d = 26 * scale; a_disc = (0.7 if state == "held" else 1.0) * alpha
    dd = kd.sprite("markers/disc_dark_24", game_h=d); lay.alpha_composite(dd, (int(round(c[0] * S - dd.width / 2)), int(round(c[1] * S - dd.height / 2))))
    ic, isz = REASON_ICON[reason if state == "held" else "aim"]; im = kd.icon(ic, isz * scale, CREAM)
    if state == "held": im.putalpha(im.split()[3].point(lambda v: int(v * 0.6)))
    lay.alpha_composite(im, (int(round(c[0] * S - im.width / 2)), int(round(c[1] * S - im.height / 2))))
    if state == "held":
        r = 15.5; n = 12
        for k in range(n):
            a0 = turn + k * 360 / n; arc = circ(c, r, 8, a0, a0 + 360 / n / 2); stroke(lay, arc, INKDEEP, 3.6, alpha=150); stroke(lay, arc, CREAM, 1.5)
    if flash > 0: stroke(lay, circ(c, 14 + 9 * (1 - flash), 36), CREAM, 3.0, alpha=int(255 * flash), closed=True)
    n = len(cams)
    for i, rel in enumerate(cams): cameo(lay, c[0] + (i - (n - 1) / 2) * 12, c[1] + 13 * scale + 8, 16, rel)
    fr.hud.alpha_composite(mul_alpha(lay, a_disc if state != "held" else alpha))

_STRIPE = {}
def stripes_block(layer, x, y, w, h):
    """a block of the kit's warning stripes (overlays/stripes_warn, the 22 x 15 tile scaled to the block's height, one tile tall: HudToolkit.Stripes), corners rounded 2 px"""
    if "t" not in _STRIPE: _STRIPE["t"] = Image.open(K + "overlays/stripes_warn@2x.png").convert("RGBA")
    t = _STRIPE["t"].resize((int(round(22 * h / 15 * S)), int(round(h * S))), Image.LANCZOS).crop((0, 0, int(round(w * S)), int(round(h * S))))
    m = Image.new("L", t.size, 0); ImageDraw.Draw(m).rounded_rectangle((0, 0, t.width - 1, t.height - 1), radius=int(2 * S), fill=255)
    a = np.minimum(np.array(t.split()[3]), np.array(m)); t.putalpha(Image.fromarray(a)); layer.alpha_composite(t, (int(round(x * S)), int(round(y * S))))

# ----------------------------------------------------------------------------- the pill (pill_status 38 px / panel_strip for harm), with icon tokens in its price
def pill39(fr, q, head, toks, ic, faces=(), harm=False, frac=0.0, ready=False, dim=1.0):
    """q = the tail's tip; head = the dim first word; toks = [("txt", s) | ("icon", name) | ("dot",)] the price line; ic = the disc's icon; faces = the doers' cameos (24 px, overlapped 7)"""
    H = 38.0; fh = JB(15.5); dsc = 30; padl = 19 + (19 if harm else 0); padr = 20 + (19 if harm else 0)      # a harm pill carries the kit's stripes block at each end (10 x 24 and a 9 px gap)
    def tokw(t): return 16 if t[0] == "icon" else tw(t[1], fh) if t[0] == "txt" else tw("·", fh) + 10
    bw = sum(tokw(t) for t in toks) + 4 * (len(toks) - 1); hw = tw(head, fh) if head else 0; dotw = tw("·", fh) + 10
    w = padl + dsc + 9 + ((hw + dotw) if head else 0) + bw + (10 + 24 + (len(faces) - 1) * 17 if faces else 0) + padr; w = max(w, 2 * 40.2 + 20)
    x = q[0] - w / 2; y = q[1] - 10 - H - 11; lay = layer(fr)
    if harm: lay.alpha_composite(kd.nine("panels/panel_strip", int(w * S), int(H * S), 25, 0.3 * S), (int(x * S), int(y * S)))
    else: lay.alpha_composite(nine_h("panels/pill_status", int(round(w * S)), int(round(H * S)), 93), (int(round(x * S)), int(round(y * S))))
    if frac > 0:
        cl = Image.new("L", lay.size, 0); cd = ImageDraw.Draw(cl); x0, y0, x1, y1 = (x + 15) * S, (y + 6) * S, (x + w - 15) * S, (y + H - 6) * S
        cd.rounded_rectangle((x0, y0, x0 + (x1 - x0) * frac, y1), radius=13 * S, fill=int(0.4 * 255)); f = Image.new("RGBA", lay.size, TURQL + (255,)); f.putalpha(cl); lay.alpha_composite(f)
    cy = y + H / 2 - 0.5; cx = x + padl + dsc / 2; disc(lay, cx, cy, dsc, ic, light=harm); tx = x + padl + dsc + 9
    if harm: stripes_block(lay, x + 15, y + (H - 24) / 2 - 0.5, 10, 24); stripes_block(lay, x + w - 15 - 10, y + (H - 24) / 2 - 0.5, 10, 24)
    if head:
        text(lay, (tx, cy + 0.6), head, fh, TURQL if ready else DIM); tx += hw + 5; text(lay, (tx, cy + 0.6), "·", fh, DIM); tx += tw("·", fh) + 5
    for t in toks:
        if t[0] == "icon": icon(lay, t[1], tx + 8, cy, 15); tx += 16 + 4
        elif t[0] == "txt": text(lay, (tx, cy + 0.6), t[1], fh, t[2] if len(t) > 2 else CREAM); tx += tw(t[1], fh) + 4
        else: tx += 5; text(lay, (tx, cy + 0.6), "·", fh, DIM); tx += tw("·", fh) + 5
    tx += 6
    for i, fb in enumerate(faces): cameo(lay, tx + 12 + i * 17, cy, 24, fb)
    if ready:
        for k in range(10, 0, -1): stroke(lay, [(x - 2 - k / 2, y - 2 - k / 2), (x + w + 2 + k / 2, y - 2 - k / 2), (x + w + 2 + k / 2, y + H + 2 + k / 2), (x - 2 - k / 2, y + H + 2 + k / 2)], TURQL, k, alpha=int(.13 * .42 * 255), closed=True)
        ImageDraw.Draw(lay).rounded_rectangle(((x - 1) * S, (y - 1) * S, (x + w + 1) * S, (y + H + 1) * S), radius=20 * S, outline=TURQL + (255,), width=int(2 * S))
    fr.hud.alpha_composite(mul_alpha(lay, dim))
    tl = layer(fr); stroke(tl, [(q[0] - 9, q[1] - 13 + 2), (q[0] + 9, q[1] - 13 + 2), (q[0], q[1])], CHROME, 0.1, closed=True, fill=CHROME)
    stroke(tl, [(q[0] - 6, q[1] - 13), (q[0] + 6, q[1] - 13), (q[0], q[1] - 13 + 7.5)], TAILBODY, 0.1, closed=True, fill=TAILBODY); fr.hud.alpha_composite(mul_alpha(tl, dim))
    return (x, y, w, H)

# ----------------------------------------------------------------------------- the cursor over a hostile (as built), with the cover badge
def aim_cursor(fr, c, odds=0.75, hold=0.0, badge=None):
    """crosshair cursor: a ring (r 13), four band ticks wider apart as the odds drop, a centre dot; hold = the 0.2 s hold's arc; badge = None | 'counts' | 'half' | 'struck' (his cover, at the cursor's lower right)"""
    spread = 16 - 11 * odds; lay = layer(fr)
    for pas in range(2):
        col = INKDEEP if pas == 0 else CREAM; w = 4.2 if pas == 0 else 2.0; al = 150 if pas == 0 else 255
        stroke(lay, circ(c, 13, 36), col, w, alpha=al, closed=True)
        for dx, dy in ((0, -1), (0, 1), (-1, 0), (1, 0)):
            stroke(lay, [(c[0] + dx * (13 + spread * 0.25), c[1] + dy * (13 + spread * 0.25)), (c[0] + dx * (18 + spread * 0.25), c[1] + dy * (18 + spread * 0.25))], col, w, alpha=al, round_caps=True)
    stroke(lay, circ(c, 1.8, 8), CREAM, 0.1, closed=True, fill=CREAM)
    if hold > 0: stroke(lay, circ(c, 22, 40, -90, -90 + 360 * hold), TURQL, 3.4)
    if badge:
        b = (c[0] + 17, c[1] + 15); disc39(lay, b[0], b[1], 17, "shield_half" if badge == "half" else "shield")
        if badge == "struck":
            r = 8; stroke(lay, [(b[0] - r * .8, b[1] + r * .8), (b[0] + r * .8, b[1] - r * .8)], INKDEEP, 3.6, alpha=190, round_caps=True); stroke(lay, [(b[0] - r * .8, b[1] + r * .8), (b[0] + r * .8, b[1] - r * .8)], CREAM, 2.0, round_caps=True)
    fr.hud.alpha_composite(lay)

# ----------------------------------------------------------------------------- cover: the disc on the prop, the arc on the ground, the preview on the move ring
OWNER_RING = {None: CHROME, "cell": TURQL, "rival": VIOLET, "police": AMBER}
def cover_disc(fr, at, full=True, owner=None, struck=False, lit=False, took=False, live=True, size=22):
    """rest (live False): a 17 px kit disc with the shield at 55 %. live: 22 px, the holder's ring (turquoise-light the cell, violet a rival, amber police, chrome free), the strike when his cover does not count"""
    lay = layer(fr)
    if not live:
        disc39(lay, at[0], at[1], 17, "shield" if full else "shield_half"); fr.hud.alpha_composite(mul_alpha(lay, 0.55)); return
    s = size * (1.18 if lit else 1.0); d = kd.sprite("markers/disc_dark_24", game_h=s); lay.alpha_composite(d, (int(round(at[0] * S - d.width / 2)), int(round(at[1] * S - d.height / 2))))
    stroke(lay, circ(at, s / 2 - 0.5, 36), OWNER_RING[owner], 2.0, closed=True); ico(lay, "shield" if full else "shield_half", at[0], at[1], s * 0.58, CREAM)
    if struck:
        r = s * 0.5; stroke(lay, [(at[0] - r * .78, at[1] + r * .78), (at[0] + r * .78, at[1] - r * .78)], INKDEEP, 3.8, alpha=190, round_caps=True); stroke(lay, [(at[0] - r * .78, at[1] + r * .78), (at[0] + r * .78, at[1] - r * .78)], CREAM, 2.0, round_caps=True)
    if lit: stroke(lay, circ(at, s / 2 + 2.6, 36), TURQL, 1.8, closed=True)
    if took:
        for r, al in ((s / 2 + 3.0, 255), (s / 2 + 6.8, 120)): stroke(lay, circ(at, r, 36), CREAM, 2.0, alpha=al, closed=True)
    fr.hud.alpha_composite(lay)

def cover_arc(fr, kneel, facing, half=62, full=True, colour=CREAM, dim=False, r_m=1.05):
    """the side it shields, on the ground at the kneel point: a heavy line when full, a thin one when partial, both solid (as built: dashes vanished on stone); cover counts only inside it, a flank beats it"""
    arc = ellipse_pts(kneel, r_m, facing - half, facing + half, 28); w = 3.6 if full else 2.0; a = 110 if dim else 255
    stroke(fr.world, arc, INKDEEP, w + 2.6, alpha=int(a * 0.6))
    stroke(fr.world, arc, colour, w, alpha=a)
    for e in (arc[0], arc[-1]): stroke(fr.world, [kneel, e], CREAM, 0.9, alpha=int(80 * a / 255))

def link(fr, a, b, colour=CREAM, alpha=200):
    """a thin dotted connector (the move ring's preview to the cover it would turn): cream over an ink underlay"""
    stroke(fr.world, [a, b], INKDEEP, 3.4, dash=(1.6, 4.0), alpha=int(alpha * 0.6)); stroke(fr.world, [a, b], colour, 1.6, dash=(1.6, 4.0), alpha=alpha)

def move_ring(fr, c, fill=0.34, glyph=None, struck=False, colour=TURQL, r_m=0.75):
    """the move ring on the ground (as built): its rim fills clockwise with the walk time; the glyph stands in its middle (the cover it snaps to)"""
    gline(fr, ellipse_pts(c, r_m), CREAM, 1.6, alpha=120, closed=True)
    if fill > 0: gline(fr, ellipse_pts(c, r_m, -90, -90 + 360 * fill, 64), colour, 3.2)
    lay = layer(fr)
    if glyph == "shield": disc39(lay, c[0], c[1] - 2, 20, "shield", ring=TURQL)
    elif glyph: ico(lay, glyph, c[0], c[1] - 3, 15, CREAM)
    fr.hud.alpha_composite(lay)

# ----------------------------------------------------------------------------- pinned (B), marked (Scout), the world marks of a move
def pin_disc(fr, c, frac=0.8, size=20):
    """B, pinned: the kit's dark disc, the kit's down chevron, a cream ring that drains over the pin's 2 s and starts again with every shot (hit or miss) at him"""
    lay = layer(fr); disc39(lay, c[0], c[1], size, "chev_down", rgb=CREAM); r = size / 2 + 2.4; pts = circ(c, r, 40, -90, -90 + 360 * frac)
    stroke(lay, pts, INKDEEP, 4.2, alpha=170); stroke(lay, pts, CREAM, 1.8); fr.hud.alpha_composite(lay)

def brackets(fr, feet, r_m=0.85, frac=1.0, preview=False):
    """Mark: four cream corner brackets round his ring; the 10 s drains as a turquoise-light arc outside them. preview: on hover, before the order, at 55 %"""
    al = 0.55 if preview else 1.0
    for ang in (45, 135, 225, 315):
        c = ground_pt(feet, r_m, ang); legs = [ground_pt(feet, r_m, ang + da) for da in (-13, 13)]
        stroke(fr.world, [legs[0], c, legs[1]], INKDEEP, 5.0, alpha=int(150 * al), round_caps=True); stroke(fr.world, [legs[0], c, legs[1]], CREAM, 2.2, alpha=int(255 * al), round_caps=True)
    if frac < 1.0:
        pts = ellipse_pts(feet, r_m + 0.3, -90, -90 + 360 * frac, 48); stroke(fr.world, pts, INKDEEP, 4.6, alpha=150); stroke(fr.world, pts, TURQL, 2.0)

def walk_to(fr, a, b, colour=VIOLET):
    """his next move (Mark): the rivals' route language (059): a dotted line to where he goes and a dashed ring there"""
    stroke(fr.world, [a, b], INKDEEP, 4.4, dash=(2.0, 4.6), alpha=150); stroke(fr.world, [a, b], colour, 2.4, dash=(2.0, 4.6))
    gline(fr, ellipse_pts(b, 0.55), colour, 2.2, dash=(2.2, 3.4), closed=True)

def shout(fr, c, t=0.5, r_max=12.0):
    """Talk down: one thin cream ring leaves the Fixer, out to the 12 m it reaches, in 0.6 s"""
    r = r_max * t; a = int(255 * (1 - t) ** 0.8)
    if r > 0.3: gline(fr, ellipse_pts(c, r), CREAM, 2.0, alpha=a, closed=True)

def grille(fr, c, lit=False, marker=True):
    """a heat-main grille in the paving (a mock prop: slats in a chrome frame), with the Tech's steam disc on it"""
    w, h = 0.8, 0.45; pts = [ground_xy(c, u, v, 0) for (u, v) in ((-w, -h), (w, -h), (w, h), (-w, h))]
    stroke(fr.world, pts, (60, 56, 52), 0.1, closed=True, fill=(46, 44, 42), fill_alpha=235); stroke(fr.world, pts, CHROME, 1.4, closed=True)
    for i in range(-3, 4): stroke(fr.world, [ground_xy(c, i * w / 3.6, -h * 0.8, 0), ground_xy(c, i * w / 3.6, h * 0.8, 0)], (150, 148, 144), 1.2)
    if marker:
        lay = layer(fr); disc39(lay, c[0], c[1] - 22, 20, "steam", ring=TURQL if lit else None); fr.hud.alpha_composite(lay)

def cloud(fr, c, r_m=4.0, seed=3, strength=1.0, lift=1.0):
    """the steam: a pale wash over the 4 m disc and billows over the grille (Glint and Vela own the real effect; this is the footprint it must fill)"""
    import random; rnd = random.Random(seed); lay = Image.new("RGBA", fr.world.size, (0, 0, 0, 0)); d = ImageDraw.Draw(lay)
    d.polygon([(int(p[0] * S), int(p[1] * S)) for p in ellipse_pts(c, r_m, 0, 360, 72)], fill=(248, 250, 250, int(46 * strength)))
    for i in range(46):
        a = rnd.uniform(0, 360); rr = r_m * (rnd.random() ** 0.65) * 0.92; p = ground_pt(c, rr, a); lf = rnd.uniform(0, 30) * lift * (1.0 - rr / r_m * 0.5)
        rad = rnd.uniform(14, 34) * S * (1.0 - 0.35 * rr / r_m); al = int(rnd.uniform(30, 70) * strength)
        d.ellipse((p[0] * S - rad, (p[1] - lf) * S - rad * 0.8, p[0] * S + rad, (p[1] - lf) * S + rad * 0.8), fill=(250, 252, 252, al))
    for i in range(24):
        p = (c[0] + rnd.uniform(-10, 10), c[1] - rnd.uniform(0, 44 * lift)); rad = rnd.uniform(10, 22) * S
        d.ellipse((p[0] * S - rad, p[1] * S - rad * 0.85, p[0] * S + rad, p[1] * S + rad * 0.85), fill=(252, 253, 253, int(rnd.uniform(55, 110) * strength)))
    fr.world.alpha_composite(lay.filter(ImageFilter.GaussianBlur(9)))

def ring_m(fr, c, r_m, colour=CREAM, dash=(6, 5), w=2.2, alpha=255, a0=0, a1=360):
    gline(fr, ellipse_pts(c, r_m, a0, a1, 72), colour, w, dash=dash, alpha=alpha, closed=(a1 - a0 == 360))

def fan(fr, a, b, spread_deg=7.0, n=3, miss_past=2.4):
    """Covering fire: the Enforcer's burst, one-handed: three dashed tracers fanned round the line to the target that end in puffs just past him (it never hits)"""
    ux, uy = (b[0] - a[0]), (b[1] - a[1]); L = math.hypot(ux, uy); base = math.atan2(uy, ux)
    for i in range(n):
        ang = base + math.radians((i - (n - 1) / 2) * spread_deg); e = (a[0] + math.cos(ang) * (L + miss_past * 3.0), a[1] + math.sin(ang) * (L + miss_past * 3.0))
        tracer(fr, a, e); puff(fr, e, 6.0)

# ----------------------------------------------------------------------------- the roster in a fight
def _wipe(fr, box, key):
    """take the capture's own small mark out (inpainted from the column's colour), once per frame and box"""
    done = fr.__dict__.setdefault("_wiped", set())
    if key in done: return
    done.add(key); x0, y0, x1, y1 = [int(v * S) for v in box]
    arr = np.array(fr.base.convert("RGB")); sub = arr[y0:y1, x0:x1].copy(); m = np.ones(sub.shape[:2], np.uint8) * 255
    m[:, :int(3 * S)] = 0; m[:, -int(3 * S):] = 0; m[:int(2 * S), :] = 0; m[-int(2 * S):, :] = 0
    arr[y0:y1, x0:x1] = cv2.inpaint(sub, m, 5, cv2.INPAINT_TELEA); fr.base = Image.fromarray(arr).convert("RGBA")
ROW_Y = (65, 161, 258, 355)
def roster_pips(fr, k, n, left, flash=0):
    """redraw agent k's pip inset on the roster column (as built: cream dots in a dark inset, a lost one a hollow ring; the inset sits 44 px under the portrait's centre, right-aligned at x 90)"""
    y0 = ROW_Y[k] + 44.5; _wipe(fr, (56, y0 - 8, 96, y0 + 8), ("pips", k)); pitch = 5.8 if n <= 8 else 4.7; wdt = (n - 1) * pitch + 9
    lay = layer(fr); x1 = 90.5; x0 = x1 - wdt
    stroke(lay, [(x0, y0 - 4.8), (x1, y0 - 4.8), (x1, y0 + 4.8), (x0, y0 + 4.8)], INKDEEP, 4.0, closed=True, fill=INKDEEP, fill_alpha=165)
    for i in range(n):
        cx = x0 + 4.5 + i * pitch; dia = 2.6 if n <= 8 else 2.3
        if i < left: stroke(lay, circ((cx, y0), dia, 12), CREAM, 0.1, closed=True, fill=CREAM)
        else: stroke(lay, circ((cx, y0), dia - 0.5, 12), CREAM, 1.0, closed=True)
        if flash and left <= i < left + flash: stroke(lay, circ((cx, y0), dia + 2.2, 18), CREAM, 1.4, closed=True)
    fr.hud.alpha_composite(lay)

def sidearm_mark(fr, k, drawn=True, firing=False):
    """the weapon slot under the portrait (as built): hollow when holstered, a light disc with the ink pistol when drawn, a turquoise-light ring when it fires"""
    y = ROW_Y[k] + 44.5; x = 31.5; _wipe(fr, (x - 9, y - 9, x + 9, y + 9), ("arm", k)); lay = layer(fr)
    if drawn:
        disc(lay, x, y, 14, "fire", light=True)
        if firing: stroke(lay, circ((x, y), 8.4, 24), TURQL, 1.8, closed=True)
    else: stroke(lay, circ((x, y), 6.4, 24), CHROME, 1.2, closed=True); icon(lay, "fire", x, y, 8, CREAM)
    fr.hud.alpha_composite(lay)

def portrait_under_fire(fr, k, n=10):
    """the live portrait's under-fire cue (as built, UnderFireTicks / CuePaint): short cream spikes round the rim; Forma's face for it is the squint"""
    c = (56, ROW_Y[k]); lay = layer(fr)
    for i in range(n):
        a = math.radians(i * 360 / n + 9); p = (c[0] + math.cos(a) * 36.5, c[1] + math.sin(a) * 36.5); q = (c[0] + math.cos(a) * 43.5, c[1] + math.sin(a) * 43.5)
        stroke(lay, [p, q], INKDEEP, 5.0, alpha=170, round_caps=True); stroke(lay, [p, q], CREAM, 3.0, round_caps=True)
    fr.hud.alpha_composite(lay)

_BTN = {}
def sidearm_button(fr, k, x=122):
    """the per-agent draw / holster button, lifted from the 0.2.122 capture (getaway-car-yaw210: a rounded square, chrome rim, the pistol cream)"""
    if "im" not in _BTN:
        src = Image.open(R + "out/batch/shots/getaway-car-yaw210.png").convert("RGB").crop((107, 51, 137, 81)); n = (int(30 * S), int(30 * S))
        im = src.resize(n, Image.LANCZOS).convert("RGBA"); m = Image.new("L", (n[0] * 4, n[1] * 4), 0)
        ImageDraw.Draw(m).rounded_rectangle((0.8 * S * 4, 0.8 * S * 4, (30 - 0.8) * S * 4, (30 - 0.5) * S * 4), radius=6.2 * S * 4, fill=255); im.putalpha(m.resize(n, Image.LANCZOS)); _BTN["im"] = im
    y = ROW_Y[k] + 1; fr.hud.alpha_composite(_BTN["im"], (int(round((x - 15) * S)), int(round((y - 15) * S))))

MOVE_ICON = ("mark", "megaphone", "steam", "burst"); MOVE_NAME = ("Mark", "Talk down", "Steam", "Covering fire")
def move_disc(fr, k, state="ready", frac=1.0, x=156, y=None):
    """the roster's move disc (a fight only): the kit's dark disc 26 px, the move's icon. ready = turquoise-light ring; cooling = icon 50 % and a turquoise-light arc filling over the 20 s;
    shut = the kit's lock at its corner (no one down yet, nothing in reach). y = a board key's own row (default: agent k's portrait row)"""
    y = ROW_Y[k] if y is None else y; lay = layer(fr); d = kd.sprite("markers/disc_dark_24", game_h=26); lay.alpha_composite(d, (int(round(x * S - d.width / 2)), int(round(y * S - d.height / 2))))
    if state == "armed": stroke(lay, circ((x, y), 12.6, 36), (31, 108, 103), 0.1, closed=True, fill=(31, 108, 103))
    ico(lay, MOVE_ICON[k], x, y, 16, CREAM, alpha=255 if state in ("ready", "armed") else 125); r = 15.2
    if state == "ready": stroke(lay, circ((x, y), r, 36), TURQL, 2.0, closed=True)
    elif state == "armed": stroke(lay, circ((x, y), r, 36), TURQL, 3.2, closed=True)
    elif state == "cooling":
        stroke(lay, circ((x, y), r, 36), INKDEEP, 2.0, alpha=110, closed=True); stroke(lay, circ((x, y), r, 48, -90, -90 + 360 * frac), TURQL, 2.0)
    elif state == "shut": ico(lay, "lock", x + 9, y + 9, 10, CREAM)
    fr.hud.alpha_composite(lay)

def busy_chip(fr, k, ic, frac, label, x=106):
    """the roster's busy chip (as built): plate_dark, the act's icon, a bar, the seconds; it stands where the draw button and the move disc stand while the move runs"""
    y = ROW_Y[k]; w = 104; lay = layer(fr); plate_dark(lay, x, y - 17, w, 34); ico(lay, ic, x + 17, y, 15)
    bx0, bx1 = x + 31, x + 31 + 38; stroke(lay, [(bx0, y), (bx1, y)], INKDEEP, 5.0, alpha=200, round_caps=True); stroke(lay, [(bx0, y), (bx0 + (bx1 - bx0) * frac, y)], TURQL, 3.6, round_caps=True)
    text(lay, (bx1 + 8, y + 0.5), label, JB(13)); fr.hud.alpha_composite(lay)

def roster_in_fight(fr, pips_by_agent, hp_by_agent, moves=("ready",) * 4, fracs=(1, 1, 1, 1), flash_k=None, drawn=True, discs=True, busy=None, firing=(0, 1, 2), under_fire=(1,)):
    """the roster column in a fight: the draw button and the weapon slot (drawn), the pip insets for this option, the move disc beside each portrait, the under-fire spikes"""
    for k in range(4):
        roster_pips(fr, k, pips_by_agent[k], hp_by_agent[k], flash=1 if flash_k == k else 0); sidearm_mark(fr, k, drawn, firing=k in firing)
        if k in under_fire: portrait_under_fire(fr, k)
        if busy and busy.get(k): busy_chip(fr, k, *busy[k])
        else:
            sidearm_button(fr, k)
            if discs: move_disc(fr, k, moves[k], fracs[k])

def crew_tally(fr, x, y, total, down=0, broken=0, mark_at=None):
    """the rivals' crew: one pip each (filled up, ringed down, dashed broken) and a brass tick over the pip where the crew breaks (more than half down)"""
    pw = 12 + 20 + total * 9 + 16; lay = layer(fr); plate_dark(lay, x, y - 13, pw, 26); cameo(lay, x + 17, y, 18, RIVAL); px = x + 36
    for i in range(total):
        c = (px + i * 9 + 3, y)
        if i < broken: stroke(lay, circ(c, 3.0, 12), CREAM, 1.2, dash=(1.6, 1.4), closed=True)
        elif i < broken + down: stroke(lay, circ(c, 3.0, 12), CREAM, 1.3, closed=True)
        else: stroke(lay, circ(c, 3.0, 12), CREAM, 0.1, closed=True, fill=CREAM)
    if mark_at is not None: tx = px + mark_at * 9 + 3; stroke(lay, [(tx - 4.5, y - 8.5), (tx + 4.5, y - 8.5)], BRASS, 1.2)
    fr.hud.alpha_composite(lay); return (x, y - 13, pw, 26)

# ----------------------------------------------------------------------------- the beat (C)
def beat_marker(fr, cx, y, cause, frac=1.0, face=None):
    """the beat's window marker over its subject: plate_dark 38 px, the cause (an icon, or a face), the quarter-time mark, a 2.5 px underline that drains over the 8 s on the wall"""
    WH = 38.0; f = JB(14.5); label = "¼×"; w = 11 + 16 + 8 + tw(label, f) + 12; x = cx - w / 2; lay = layer(fr)
    plate_dark(lay, x, y, w, WH)
    if face: cameo(lay, x + 11 + 8, y + WH / 2 - 1, 17, face)
    else: ico(lay, cause, x + 11 + 8, y + WH / 2 - 1, 15)
    text(lay, (x + 11 + 16 + 8, y + WH / 2 - 0.5), label, f); tx0, tx1, ty = x + 11, x + w - 11, y + WH - 9
    stroke(lay, [(tx0, ty + 1.25), (tx1, ty + 1.25)], INKDEEP, 2.5, alpha=153, round_caps=True); stroke(lay, [(tx0, ty + 1.25), (tx0 + (tx1 - tx0) * frac, ty + 1.25)], TURQL, 2.5, round_caps=True)
    tl = layer(fr); q = (cx, y + WH + 3); stroke(tl, [(q[0] - 7, q[1] - 10 + 2), (q[0] + 7, q[1] - 10 + 2), (q[0], q[1])], CHROME, 0.1, closed=True, fill=CHROME)
    stroke(tl, [(q[0] - 4.5, q[1] - 10), (q[0] + 4.5, q[1] - 10), (q[0], q[1] - 10 + 5.5)], TAILBODY, 0.1, closed=True, fill=TAILBODY)
    fr.hud.alpha_composite(lay); fr.hud.alpha_composite(tl); return (x, y, w, WH)

def beat_ring(fr, c, r_m=1.0, frac=1.0):
    """the beat's mark at its subject: one 3 px turquoise-light ring leaving it, and a dashed ring that drains with the beat"""
    gline(fr, ellipse_pts(c, r_m * 1.9), TURQL, 3.0, alpha=150, closed=True); gline(fr, ellipse_pts(c, r_m, -90, -90 + 360 * frac, 64), TURQL, 2.6)

def threat(fr, a, b, colour=VIOLET):
    """who aims at whom, in a beat (and on Alt): a dotted line (2 on, 5 off) marching to the target and an arrowhead, in the shooter's own colour (the lab's `theirs` line)"""
    ux, uy = (b[0] - a[0]), (b[1] - a[1]); L = math.hypot(ux, uy); ux, uy = ux / L, uy / L; e = (b[0] - ux * 10, b[1] - uy * 10)
    stroke(fr.hud, [a, e], INKDEEP, 3.6, dash=(2.2, 4.8), alpha=140); stroke(fr.hud, [a, e], colour, 2.0, dash=(2.2, 4.8))
    n = (-uy, ux); tri = [(b[0] - ux * 2, b[1] - uy * 2), (b[0] - ux * 12 + n[0] * 5, b[1] - uy * 12 + n[1] * 5), (b[0] - ux * 12 - n[0] * 5, b[1] - uy * 12 - n[1] * 5)]
    stroke(fr.hud, tri, INKDEEP, 3.0, alpha=170, closed=True, fill=INKDEEP, fill_alpha=170); stroke(fr.hud, tri, colour, 0.8, closed=True, fill=colour)

def refill(fr, k, x=156):
    """a beat refills every move: one cream ring leaves each disc"""
    y = ROW_Y[k]; stroke(fr.hud, circ((x, y), 18.5, 36), CREAM, 2.0, alpha=190, closed=True)

def bubble(fr, tail, words):
    """a speech bubble at the speaker (as built); the words are Fable's"""
    f = ImageFont.truetype(K + "fonts/Barlow-Medium.ttf", int(13 * S)); w = f.getlength(words) / S + 20; x = tail[0] - w / 2; y = tail[1] - 14 - 22; lay = layer(fr)
    stroke(lay, [(x, y), (x + w, y), (x + w, y + 22), (x, y + 22)], INKDEEP, 3.2, alpha=140, closed=True, fill=(250, 247, 240), fill_alpha=255)
    stroke(lay, [(tail[0] - 5, y + 21), (tail[0] + 5, y + 21), (tail[0], tail[1] - 4)], INKDEEP, 2.4, alpha=140, closed=True, fill=(250, 247, 240))
    ImageDraw.Draw(lay).text(((x + w / 2) * S, (y + 11.5) * S), words, font=f, fill=INK, anchor="mm"); fr.hud.alpha_composite(lay)

def callout(fr, c, n):
    """a board annotation (never part of the game): a cream disc with an ink numeral"""
    lay = layer(fr); stroke(lay, circ(c, 8.5, 24), INKDEEP, 3.0, closed=True, fill=INKDEEP, fill_alpha=200); stroke(lay, circ(c, 8.5, 24), CREAM, 0.1, closed=True, fill=CREAM)
    ImageDraw.Draw(lay).text((c[0] * S, c[1] * S + 0.5), str(n), font=JB(12), fill=INKDEEP, anchor="mm"); fr.hud.alpha_composite(lay)


# ----------------------------------------------------------------------------- board-only helpers (never in the game)
def cursor_arrow(fr, c):
    """the system arrow, for the stills (the build's captures leave the OS cursor out): a cream arrow on an ink edge, its tip at c"""
    pts = [(0, 0), (0, 17), (4.2, 13.2), (7.2, 19.5), (10.2, 18.2), (7.4, 12.0), (12.8, 12.0)]; P = [(c[0] + x, c[1] + y) for x, y in pts]
    stroke(fr.hud, P, INKDEEP, 3.4, closed=True, fill=INKDEEP, fill_alpha=255); stroke(fr.hud, P, CREAM, 0.1, closed=True, fill=CREAM)

def tether(fr, a, b):
    """as built (HudToolkit.Cover.DashLine): a turquoise-light dashed line from the cursor's ground to the cover point it will snap to"""
    stroke(fr.world, [a, b], INKDEEP, 3.4, dash=(6, 4), alpha=150); stroke(fr.world, [a, b], TURQL, 1.6, dash=(6, 4))

def sight(fr, a, b, alpha=210):
    """the line of fire from where the move would end to a hostile in cover (the flank preview): a thin dotted cream line over ink"""
    stroke(fr.world, [a, b], INKDEEP, 3.6, dash=(1.8, 4.2), alpha=int(alpha * 0.6)); stroke(fr.world, [a, b], CREAM, 1.8, dash=(1.8, 4.2), alpha=alpha)


def offer_disc(fr, c, ic, size=22):
    """a valid target for the armed move: the move's glyph in the kit's dark disc over a dashed turquoise-light ring (the target-mode marker's grammar)"""
    lay = layer(fr); stroke(lay, circ(c, size / 2 + 3.2, 24), INKDEEP, 4.0, alpha=140, closed=True)
    for k in range(10):
        stroke(lay, circ(c, size / 2 + 3.2, 6, k * 36, k * 36 + 20), TURQL, 1.8)
    disc(lay, c[0], c[1], size, ic); fr.hud.alpha_composite(lay)

def roster_row(k, state, frac=0.4, label="1 s", ic=None):
    """one agent's roster row at 2x, as the board shows it: 'busy' (the chip: the act's icon, a bar, the seconds) or 'cooling' (the draw button and the disc's arc) or 'ready'"""
    fr = scene(); y = ROW_Y[k]
    if state == "busy": busy_chip(fr, k, ic or MOVE_ICON[k], frac, label)
    else: sidearm_button(fr, k); move_disc(fr, k, state, frac)
    return fr.crop((0, y - 41, 236, y + 54)).convert("RGB")
