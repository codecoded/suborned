# Glyph, brief 067 (2026-10-26): the pieces the batch 0.2.123 spec adds, drawn from the kit over real captures, on briefs/058/glib.py.
# Everything is read from unity/Assets/Suborned/UI/Resources/Kit (sprites, icons, badges, fonts); no colour here lacks a source
# (kit.uss cream / ink-deep / text-dim / chrome / turquoise-light; the style contract's signal colours for the world marks only).
import sys, math; sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/058")
from glib import *

SCOUT = "portraits/cameo_scout_normal"; FIXER = "portraits/cameo_fixer_normal"; TECH = "portraits/cameo_tech_normal"; ENF = "portraits/cameo_enforcer_normal"
VALE = "badges/npc_councillor"; GUARD = "badges/signal_guard"; RIVAL = "badges/signal_rival"; POLICE = "badges/signal_police"
ROLE = {"scout": SCOUT, "fixer": FIXER, "tech": TECH, "enforcer": ENF}

def mul_alpha(lay, a):
    if a < 1: lay.putalpha(lay.split()[3].point(lambda v: int(v * a)))
    return lay

def layer(fr): return Image.new("RGBA", fr.hud.size, (0, 0, 0, 0))

# ----------------------------------------------------------------------------- the cameo strike (a 2 px cream diagonal: the shut-way strike)
def cameo_struck(lay, cx, cy, size, rel):
    cameo(lay, cx, cy, size, rel)
    r = size * 0.5
    stroke(lay, [(cx - r * 0.78, cy + r * 0.78), (cx + r * 0.78, cy - r * 0.78)], INKDEEP, 3.6, alpha=170, round_caps=True)
    stroke(lay, [(cx - r * 0.78, cy + r * 0.78), (cx + r * 0.78, cy - r * 0.78)], CREAM, 2.0, round_caps=True)

# ----------------------------------------------------------------------------- pop (plate_dark 26 px): icon / cameos, no words or a short
def pop(fr, cx, cy, parts, dim=1.0):
    """parts: ("icon", name) | ("cam", rel) | ("camx", rel) | ("txt", s) | ("gap", g); plate_dark 26 px, centred on cx, cy"""
    f = JB(13.5); wd = 10
    for p in parts:
        wd += {"icon": 18, "cam": 17, "camx": 17, "gap": p[1] if p[0] == "gap" else 0}.get(p[0], 0) if p[0] != "txt" else tw(p[1], f) + 4
    wd += 6; x = cx - wd / 2; lay = layer(fr); plate_dark(lay, x, cy - 13, wd, 26); px = x + 10
    for p in parts:
        k = p[0]
        if k == "icon": icon(lay, p[1], px + 8, cy, 15 if p[1] != "pointer" else 13); px += 18
        elif k == "cam": cameo(lay, px + 8, cy, 15, p[1]); px += 17
        elif k == "camx": cameo_struck(lay, px + 8, cy, 15, p[1]); px += 17
        elif k == "gap": px += p[1]
        elif k == "txt": text(lay, (px, cy + 0.5), p[1], f); px += tw(p[1], f) + 4
    fr.hud.alpha_composite(mul_alpha(lay, dim)); return (x, cy - 13, wd, 26)

# ----------------------------------------------------------------------------- the why chip: the roster's busy chip frame (plate_dark 36 px), the cause icon, two words
def why_chip(fr, x, yc, ic, words):
    f = JB(12.5); w = 16 + 14 + 6 + tw(words, f) + 16; lay = layer(fr)
    plate_dark(lay, x, yc - 18, w, 36); icon(lay, ic, x + 16 + 7, yc, 14); text(lay, (x + 16 + 14 + 6, yc + 0.5), words, f)
    fr.hud.alpha_composite(lay); return (x, yc - 18, w, 36)

# ----------------------------------------------------------------------------- the car marker and its five seat pips (as built, with the pending state)
def seat_pips(fr, c, states, turn=0.0):
    """c = the marker's centre; states: five of 'empty' | 'pending' | 'full' (Vale first). The marker: disc_dark_24 26 px with the car icon in turquoise-light;
    the pips: a dark backing pill (4 px round), pitch 9, Vale 3.6 px radius, the others 2.9 (HudToolkit.Less)"""
    lay = layer(fr); disc(lay, c[0], c[1], 26, "car", rgb=TURQL)
    sc = (c[0], c[1] + 21); x0, x1 = sc[0] - 18 - 3.6 - 1.4 - 4, sc[0] + 18 + 2.9 + 1.4 + 4; y0, y1 = sc[1] - 9, sc[1] + 9
    stroke(lay, [(x0 + 9, y0), (x1 - 9, y0), (x1 - 9, y1), (x0 + 9, y1)], INK, 0.1, closed=True, fill=INK, fill_alpha=230)
    for k, side in ((x0 + 9, 1), (x1 - 9, 1)):
        pass
    pts = [(x0 + 9 + 9 * math.cos(math.radians(a)), sc[1] + 9 * math.sin(math.radians(a))) for a in range(90, 271, 15)] + [(x1 - 9 + 9 * math.cos(math.radians(a)), sc[1] + 9 * math.sin(math.radians(a))) for a in range(-90, 91, 15)]
    stroke(lay, pts, INK, 0.1, closed=True, fill=INK, fill_alpha=230)
    for k, st in enumerate(states):
        p = (sc[0] + (k - 2) * 9, sc[1]); r = 3.6 if k == 0 else 2.9
        stroke(lay, [(p[0] + (r + 1.4) * math.cos(math.radians(a)), p[1] + (r + 1.4) * math.sin(math.radians(a))) for a in range(0, 361, 20)], INKDEEP, 0.1, closed=True, fill=INKDEEP, fill_alpha=217)
        if st == "full":
            stroke(lay, [(p[0] + r * math.cos(math.radians(a)), p[1] + r * math.sin(math.radians(a))) for a in range(0, 361, 20)], TURQL, 0.1, closed=True, fill=TURQL)
        elif st == "pending":
            for d in range(3):
                a0 = turn + d * 120; arc = [(p[0] + (r - 0.4) * math.cos(math.radians(a)), p[1] + (r - 0.4) * math.sin(math.radians(a))) for a in range(int(a0), int(a0) + 71, 10)]
                stroke(lay, arc, TURQL, 1.2, round_caps=True)
        else:
            stroke(lay, [(p[0] + (r - 0.4) * math.cos(math.radians(a)), p[1] + (r - 0.4) * math.sin(math.radians(a))) for a in range(0, 361, 20)], CREAM, 1.2, closed=True)
    fr.hud.alpha_composite(lay)

# ----------------------------------------------------------------------------- a verb pill with options (pill_status 38 px, the offer's look; lock state at 80 %)
def pill(fr, q, head, label, tail_words, ic, faces=(), lock=False, harm=False, dim=1.0, stack=0, stack_i=0):
    """q = where the tail's tip touches; head = the dim first word ("Hold"), label = cream words, tail_words = dim words after a dot (the short lock),
    ic = the disc's icon; lock = the locked look (lock disc, 80 %, no head); stack = the stack tab's dot count"""
    H = 38.0; fh = JB(15.5); dsc = 30; padl = 19; padr = 20
    hw = tw(head, fh) if head else 0; dotw = tw("·", fh) + 10
    bw = tw(label, fh) + ((dotw + tw(tail_words, fh)) if tail_words else 0)
    w = padl + dsc + 9 + ((hw + dotw) if head else 0) + bw + (10 + 24 + (len(faces) - 1) * 17 if faces else 0) + padr
    w = max(w, 2 * 40.2 + 20); x = q[0] - w / 2; y = q[1] - 10 - H - 11; lay = layer(fr)
    if harm: lay.alpha_composite(kd.nine("panels/panel_strip", int(w * S), int(H * S), 25, 0.3 * S), (int(x * S), int(y * S)))
    else: lay.alpha_composite(nine_h("panels/pill_status", int(round(w * S)), int(round(H * S)), 93), (int(round(x * S)), int(round(y * S))))
    cy = y + H / 2 - 0.5; cx = x + padl + dsc / 2
    disc(lay, cx, cy, dsc, "lock" if lock else ic, light=harm)
    tx = x + padl + dsc + 9
    if head: text(lay, (tx, cy + 0.6), head, fh, DIM); tx += hw + 5; text(lay, (tx, cy + 0.6), "·", fh, DIM); tx += tw("·", fh) + 5
    text(lay, (tx, cy + 0.6), label, fh); tx += tw(label, fh)
    if tail_words:
        tx += 5; text(lay, (tx, cy + 0.6), "·", fh, DIM); tx += tw("·", fh) + 5; text(lay, (tx, cy + 0.6), tail_words, fh, DIM); tx += tw(tail_words, fh)
    tx += 10
    for i, fb in enumerate(faces): cameo(lay, tx + 12 + i * 17, cy, 24, fb)
    mul_alpha(lay, dim); fr.hud.alpha_composite(lay)
    tl = layer(fr); stroke(tl, [(q[0] - 9, q[1] - 13 + 2), (q[0] + 9, q[1] - 13 + 2), (q[0], q[1])], CHROME, 0.1, closed=True, fill=CHROME)
    stroke(tl, [(q[0] - 6, q[1] - 13), (q[0] + 6, q[1] - 13), (q[0], q[1] - 13 + 7.5)], TAILBODY, 0.1, closed=True, fill=TAILBODY); fr.hud.alpha_composite(mul_alpha(tl, dim))
    if stack > 1: stack_tab(fr, (x + w / 2, y), stack, stack_i)
    return (x, y, w, H)

def stack_tab(fr, at, n, i):
    """the stack tab: plate_dark 16 px high seated on the pill's top edge, one dot a candidate (6 px, 9 px pitch), the active one filled cream"""
    w = 14 + 9 * (n - 1) + 6; lay = layer(fr); plate_dark(lay, at[0] - w / 2, at[1] - 10, w, 16)
    for k in range(n):
        p = (at[0] + (k - (n - 1) / 2) * 9, at[1] - 2)
        circ = [(p[0] + 3 * math.cos(math.radians(a)), p[1] + 3 * math.sin(math.radians(a))) for a in range(0, 361, 30)]
        if k == i: stroke(lay, circ, CREAM, 0.1, closed=True, fill=CREAM)
        else: stroke(lay, circ, CREAM, 1.4, closed=True)
    fr.hud.alpha_composite(lay)

# ----------------------------------------------------------------------------- the ring with shut wedges, cameos under the icon, pills outside
def ring67(fr, C, wedges, lit=None):
    """wedges: list of dict(icon, word, fields (tokens after the word), violent, shut, cams (rels), lock (short lock text), more)"""
    n = len(wedges); segs = [dict(icon=w.get("icon", "dot"), violent=w.get("violent", False), shut=w.get("shut", False)) for w in wedges]
    ring_wheel(fr, C, segs, lit=lit if lit is not None else -1)
    w360 = 360.0 / n
    for i, w in enumerate(wedges):
        d = Dir(i * w360) if n > 1 else (0, -1); mid = ((RO + RI) / 2 * d[0], (RO + RI) / 2 * d[1])
        if w.get("shut"):
            a = Dir(i * w360 - w360 * 0.3); b = Dir(i * w360 + w360 * 0.3); rm = (RO + RI) / 2
            stroke(fr.hud, [(C[0] + a[0] * rm, C[1] + a[1] * rm), (C[0] + b[0] * rm, C[1] + b[1] * rm)], CREAM, 1.6)
        if w.get("more"):
            for k in (-1, 0, 1): 
                p = (C[0] + mid[0] + k * 6.56, C[1] + mid[1]); stroke(fr.hud, [(p[0] + 2.4 * math.cos(math.radians(a)), p[1] + 2.4 * math.sin(math.radians(a))) for a in range(0, 361, 30)], CREAM, 0.1, closed=True, fill=CREAM)
            continue
        cams = w.get("cams", [])
        for j, rel in enumerate(cams): cameo(fr.hud, C[0] + mid[0] + (j - (len(cams) - 1) / 2) * 15, C[1] + mid[1] + 17, 14, rel)
    for i, w in enumerate(wedges):
        if w.get("more"): continue
        toks = [("word", w["word"])]
        if w.get("lock"): toks += [("gap", 7), ("icon", "lock", 13), ("gap", 3), ("num", w["lock"], DIM)]
        for f in w.get("fields", []):
            toks.append(("gap", 10))
            if f[0] == "time": toks += [("icon", "p_time"), ("gap", 1), ("num", f[1])]
            elif f[0] == "cam": toks += [("cameo", f[1])]   # an object's cameo after the verb (a way that moves something else); the givers' cameos sit under the wedge icon
            elif f[0] == "camx": toks += [("cameo", f[1], "struck")]
            elif f[0] == "trail": toks += [("icon", "p_trail"), ("gap", 1), ("num", f[1])]
            elif f[0] == "alert": toks += [("icon", "p_alert"), ("gap", 1), ("num", f[1])]
            elif f[0] == "draws": toks += [("icon", "fire", 13), ("gap", 2), ("num", f[1], DIM)]
        if w.get("violent"): toks += [("gap", 8), ("icon", "bang", 14)]
        box, xs, yc = ring_pill_x(fr, C, i * w360, toks, lit=(lit == i))
        for t, x in zip(toks, xs):
            if t[0] == "cameo" and len(t) > 2 and t[2] == "struck":
                cx, cy = x / S + 9, yc / S; r = 9
                stroke(fr.hud, [(cx - r * 0.78, cy + r * 0.78), (cx + r * 0.78, cy - r * 0.78)], INKDEEP, 3.4, alpha=170, round_caps=True)
                stroke(fr.hud, [(cx - r * 0.78, cy + r * 0.78), (cx + r * 0.78, cy - r * 0.78)], CREAM, 2.0, round_caps=True)


def ring_pill_x(fr, C, deg, tokens, lit=False):
    """glib.ring_pill, keeping where each token landed (to strike a cameo): returns the plate box, the tokens' x (shot px) and the centre y (shot px)"""
    d = Dir(deg); at = (d[0] * (RO + 10), d[1] * (RO + 10)); pw = kd.pill_width(tokens) / S
    right, left = d[0] > 0.2, d[0] < -0.2
    px = at[0] if right else at[0] - pw if left else at[0] - pw / 2
    py = at[1] - 15 + (0 if abs(d[0]) > 0.2 else (-14 if d[1] < 0 else 14))
    x_vis = (C[0] + px) * S + kd.G(4); yc = (C[1] + py + 15) * S
    box, xs = kd.draw_pill(fr.hud, tokens, x_vis, yc, lit=lit, width=int(pw * S))
    return (C[0] + px, C[1] + py, pw, 30), xs, yc

# ----------------------------------------------------------------------------- a board composer at native scale (2 px per game px): panels placed by hand, captions wrapped, no downscale
def wrap(txt, font, width):
    words = txt.split(); lines = []; cur = ""
    for w in words:
        t = (cur + " " + w).strip()
        if font.getlength(t) <= width: cur = t
        else: lines.append(cur); cur = w
    if cur: lines.append(cur)
    return lines

def compose(items, path, title, sub, W=2400, pad=28, top=200):
    """items: (image, x, y, caption, small) with x, y in board px from the top-left of the panel area; returns the board's size"""
    cf, sf = board_font(30), body_font(23); H = top
    cap_lines = []
    for im, x, y, cap, small in items:
        lines = wrap(small, sf, max(im.width, 520)); cap_lines.append(lines); H = max(H, top + y + im.height + 14 + 40 + 30 * len(lines) + pad)
    out = Image.new("RGB", (W, H), BG); d = ImageDraw.Draw(out)
    d.text((pad, 28), title, font=board_font(46), fill=BCREAM)
    for k, ln in enumerate(wrap(sub, body_font(26), W - 2 * pad)): d.text((pad, 92 + 32 * k), ln, font=body_font(26), fill=(176, 205, 199))
    for (im, x, y, cap, small), lines in zip(items, cap_lines):
        out.paste(im.convert("RGB"), (pad + x, top + y)); yy = top + y + im.height + 14
        d.text((pad + x, yy), cap, font=cf, fill=BCREAM)
        for k, ln in enumerate(lines): d.text((pad + x, yy + 40 + 30 * k), ln, font=sf, fill=(176, 205, 199))
    out.save(path); return out.size

# ----------------------------------------------------------------------------- the lock reticle (D): disc_dark_24 26 px with the kit's crosshair, the shooters' cameos under it
def reticle(fr, c, state="locked", cams=(), turn=0.0, scale=1.0, alpha=1.0, flash=0.0):
    """state: locked | held | set | released. c = the disc's centre (game px). cams: the shooters' cameo rels, 16 px with their ring, overlapped 4 px."""
    lay = layer(fr); d = 26 * scale
    a_disc = (0.7 if state == "held" else 1.0) * alpha
    dd = kd.sprite("markers/disc_dark_24", game_h=d); lay.alpha_composite(dd, (int(round(c[0] * S - dd.width / 2)), int(round(c[1] * S - dd.height / 2))))
    cx = kd.sprite("overlays/crosshair", game_h=16 * scale); cx = kd.tint(cx, CREAM)
    if state == "held": cx.putalpha(cx.split()[3].point(lambda v: int(v * 0.55)))
    lay.alpha_composite(cx, (int(round(c[0] * S - cx.width / 2)), int(round(c[1] * S - cx.height / 2))))
    if state == "held":                                                       # a dashed cream ring (3 on / 3 off, 1.5 px) round the disc, turning
        r = 15.5; n = 12
        for k in range(n):
            a0 = turn + k * 360 / n; arc = [(c[0] + r * math.cos(math.radians(a)), c[1] + r * math.sin(math.radians(a))) for a in range(int(a0), int(a0 + 360 / n / 2) + 1, 5)]
            stroke(lay, arc, INKDEEP, 3.6, alpha=150); stroke(lay, arc, CREAM, 1.5)
    if flash > 0:                                                             # the set beat: one cream ring leaving
        r = 14 + 9 * (1 - flash); ring = [(c[0] + r * math.cos(math.radians(a)), c[1] + r * math.sin(math.radians(a))) for a in range(0, 361, 10)]
        stroke(lay, ring, CREAM, 3.0, alpha=int(255 * flash), closed=True)
    n = len(cams)
    for i, rel in enumerate(cams): cameo(lay, c[0] + (i - (n - 1) / 2) * 12, c[1] + 13 * scale + 8, 16, rel)
    fr.hud.alpha_composite(mul_alpha(lay, a_disc if state != "held" else alpha))

def handoff(fr, a, b, alpha=1.0):
    """the re-target's hand-off line: cream 1.6 px over ink-deep 4 px at 60 %, from the old reticle to the new"""
    stroke(fr.hud, [a, b], INKDEEP, 4.0, alpha=int(150 * alpha)); stroke(fr.hud, [a, b], CREAM, 1.6, alpha=int(255 * alpha))

def edge_chip_lock(fr, x, y, rot):
    """the off-screen lock: plate_dark 22 x 52, the lock disc (16 px) and the pointer turned toward the target"""
    plate_dark(fr.hud, x - 26, y - 11, 52, 22)
    dd = kd.sprite("markers/disc_dark_24", game_h=16); fr.hud.alpha_composite(dd, (int(round((x - 12) * S - dd.width / 2)), int(round(y * S - dd.height / 2))))
    cx = kd.tint(kd.sprite("overlays/crosshair", game_h=10), CREAM); fr.hud.alpha_composite(cx, (int(round((x - 12) * S - cx.width / 2)), int(round(y * S - cx.height / 2))))
    im = kd.icon("pointer", 14, CREAM).rotate(-rot, resample=Image.BICUBIC, expand=True); fr.hud.alpha_composite(im, (int((x + 11) * S - im.width / 2), int(y * S - im.height / 2)))
