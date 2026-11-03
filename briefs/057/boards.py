# Boards for brief 057: "What the cell learned, at the ring" and "What a Reroute moves". Run: python3 -I briefs/057/boards.py
# Reads Sky's two 0.2.99 shots, the batch shot out/batch/shots/office-front-yaw210.png and the game's kit; writes PNGs beside this file.
import sys, os, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kitdraw as kd
from kitdraw import *
import cv2, numpy as np
OUT = R + "briefs/057/"; SC = R + "out/scratch/glyph/057/"; os.makedirs(SC, exist_ok=True)
OX = 720   # the canvas is extended 720 px to the left of Sky's crop so the left pill fits (his screen had the room)
C = (489 + OX, 449); RO, RI = 84, 30
BG = (23, 48, 47); INKC = (247, 244, 236)
FT = R + "testbed/hud-style/fonts/Jost-Medium-Tab.ttf"
def cap_font(n): return ImageFont.truetype(FT, n)

def dirv(deg):
    a = math.radians(deg); return (math.sin(a), -math.cos(a))
def inpaint(img, box, thr=185, grow=5):
    a = np.array(img.convert("RGB")); x0, y0, x1, y1 = box
    m = (a[y0:y1, x0:x1].min(axis=2) > thr).astype("uint8") * 255
    m = cv2.dilate(m, np.ones((grow, grow), np.uint8)); full = np.zeros(a.shape[:2], "uint8"); full[y0:y1, x0:x1] = m
    return Image.fromarray(cv2.inpaint(a, full, 6, cv2.INPAINT_TELEA)).convert("RGBA")
def wedge_icon(base, deg, name, lit=False):
    d = dirv(deg); mid = (RO + RI) / 2 * S; im = icon(name, 22, TURQL if lit else CREAM)
    paste(base, im, C[0] + d[0] * mid - im.width / 2, C[1] + d[1] * mid - im.height / 2)
def wedge_sparkle(base, deg, w=90):
    d = dirv(deg + w * 0.28); r = (RO - 9) * S; sparkle(base, C[0] + d[0] * r, C[1] + d[1] * r)
def shot_clean():
    b = Image.open(SHOT).convert("RGBA")
    b = inpaint(inpaint(b, (440, 250, 540, 320)), (280, 430, 360, 475))
    c = Image.new("RGBA", (b.width + OX, b.height), BG + (255,)); c.paste(b, (OX, 0)); return c   # the Watch binoculars and the ⋯ dots out, the after-Listen ring puts its own

# ---- the price lines for Inez (Fearful, debt, the affair with the guard). Numbers: people.rules approaches (Pay 70k, a step worse +20k = 90k; Help 40k; Pressure $0)
def fields(word, cash, t, trail, alert=None):
    L = [("word", word), ("gap", 11)]
    if cash: L += [("icon", "p_cash"), ("gap", 1), ("num", cash), ("gap", 10)]
    L += [("icon", "p_time"), ("gap", 1), ("num", t), ("gap", 10), ("icon", "p_trail"), ("gap", 1), ("num", trail), ("gap", 10)]
    if alert: L += [("icon", "p_alert"), ("gap", 1), ("num", alert), ("gap", 10)]
    L += [("icon", "eye_shut", 17, INK), ("gap", 10)]
    return L
def tail_B(kind, new):
    """option B: the facts the quote read, then the mark"""
    t = []
    gap = ("gap", 4)
    if kind == "pay": t += [("fact", "temp", "fearful", "learned", new), gap, ("mark", "half", new)]
    if kind == "help": t += [("fact", "need", "debt", "learned", new), gap, ("fact", "temp", "fearful", "learned", new), gap, ("mark", "half", new)]
    if kind == "pressure": t += [("fact", "secret", "kin", "learned", new), gap, ("fact", "need", "debt", "learned", new), gap, ("fact", "temp", "fearful", "learned", new), gap, ("mark", "ok", new)]
    return t
def tail_plain(kind, new):
    return [("mark", {"pay": "half", "help": "half", "pressure": "ok"}[kind], new)]
LINES = {"pay": lambda: fields("Pay", "90k", "55 s", "+2", "1"),
         "help": lambda: fields("Help", "40k", "55 s", "+1"),
         "pressure": lambda: fields("Pressure", None, "40 s", "+3", "1")}
def place_pills(base, tails, newflag=True):
    """top = Pressure, right = Pay (as in the shot), left = Help; returns the boxes"""
    boxes = {}
    tk = LINES["pressure"]() + tails("pressure", newflag); w = pill_width(tk) - G(8)
    boxes["pressure"], _ = draw_pill(base, tk, C[0] - w / 2, 131.5)
    tk = LINES["pay"]() + tails("pay", newflag)
    boxes["pay"], _ = draw_pill(base, tk, 783 + OX, 457, lit=True)
    tk = LINES["help"]() + tails("help", newflag); w = pill_width(tk) - G(8)
    boxes["help"], _ = draw_pill(base, tk, 195 + OX - w, 457)
    return boxes
def after_ring(tails, newflag=True, extra=None):
    b = shot_clean()
    wedge_icon(b, 0, "pressure"); wedge_icon(b, 270, "help")
    wedge_sparkle(b, 0); wedge_sparkle(b, 270)       # the existing sparkle: an act that just opened, on Pressure and Help
    # the Pay wedge keeps its sparkle (in the shot already)
    boxes = place_pills(b, tails, newflag)
    if extra: extra(b)
    return b, boxes

# ---------------------------------------------------------------- option A: a tag at the hub (standing facts) + shape twins on the wedges
def extra_A(b):
    # the tag: plate_dark hung on the hub's lower rim, three slot tokens, each new one with the kit's unread dot
    toks = [("need", "debt"), ("temp", "fearful"), ("secret", "kin")]
    ims = [token_img(k, i) for k, i in toks]; gap = G(3); tw = sum(i.width for i in ims) + 2 * gap
    pw = int(tw + G(12) + G(8)); el = plate(pw); cx = C[0]; yc = C[1] + RI * S + G(2)
    paste(b, el, cx - pw / 2, yc - el.height / 2)
    x = cx - tw / 2
    for im in ims:
        paste(b, im, x, yc - im.height / 2); nub(b, x + im.width - G(1.5), yc - im.height / 2 + G(1.5)); x += im.width + gap
    # twins: the new fact's silhouette (no icon) on the outer edge of the wedge it priced, opposite the sparkle
    def twin(deg, kind, off):
        d = dirv(deg - 25 + off); r = (RO - 9) * S; cx_, cy_ = C[0] + d[0] * r, C[1] + d[1] * r
        w_, h_, rr = {"need": (8, 8, 4), "temp": (8, 8, 2.5), "secret": (11, 8, 2.5)}[kind]
        im = Image.new("RGBA", (int(G(w_) * 4), int(G(h_) * 4)), (0, 0, 0, 0)); dd = ImageDraw.Draw(im)
        dd.rounded_rectangle((0, 0, im.width - 1, im.height - 1), radius=G(rr) * 4, fill=CREAM + (255,), outline=INKDEEP + (255,), width=int(G(0.5) * 4))
        im = im.resize((int(G(w_)), int(G(h_))), Image.LANCZOS); paste(b, im, cx_ - im.width / 2, cy_ - im.height / 2)
    twin(90, "temp", 0)                                  # Pay
    twin(270, "need", 0); twin(270, "temp", 11)          # Help
    twin(0, "secret", -11); twin(0, "need", 0); twin(0, "temp", 11)  # Pressure
# ---------------------------------------------------------------- option C: the new facts ride onto the wedges (nothing standing)
def extra_C(b):
    def toks(deg, items):
        for n, (k, i) in enumerate(items):
            a = deg - 12 - 14 * n; d = dirv(a); r = (RO - 11) * S
            im = token_img(k, i); x, y = C[0] + d[0] * r, C[1] + d[1] * r
            paste(b, im, x - im.width / 2, y - im.height / 2); nub(b, x + im.width / 2 - G(1.5), y - im.height / 2 + G(1.5))
    toks(90 - 25 + 12 + 14, [("temp", "fearful")])
    toks(270 - 25 + 12 + 14, [("need", "debt"), ("temp", "fearful")])
    toks(0 - 25 + 12 + 14, [("secret", "kin"), ("need", "debt"), ("temp", "fearful")])

def crop2(img, box, scale=2):
    c = img.crop(box); return c.resize((int(c.width * scale), int(c.height * scale)), Image.LANCZOS)
def sheet(panels, cols, name, title, sub="", pad=30, capH=70, head=100, bg=BG):
    rows = [panels[k:k + cols] for k in range(0, len(panels), cols)]
    rh = [max(p[0].height for p in r) + capH + pad for r in rows]; rw = [sum(p[0].width + pad for p in r) for r in rows]
    tw = int(max(cap_font(36).getlength(title), cap_font(21).getlength(sub))) + 2 * pad
    im = Image.new("RGB", (max(pad + max(rw), tw), head + sum(rh)), bg); d = ImageDraw.Draw(im)
    d.text((pad, 22), title, font=cap_font(36), fill=INKC); d.text((pad, 66), sub, font=cap_font(21), fill=DIM)
    y = head
    for r, h in zip(rows, rh):
        x = pad
        for pic, cap, subc in r:
            d.text((x, y), cap, font=cap_font(26), fill=INKC); d.text((x, y + 34), subc, font=cap_font(19), fill=DIM)
            im.paste(pic.convert("RGB"), (x, y + capH)); x += pic.width + pad
        y += h
    im.save(OUT + name); print(name, im.size)


def pay_state(state):
    """Sky's shot with the Pay pill redrawn for a state; returns the pill crop at native shot px"""
    b = Image.open(SHOT).convert("RGBA")
    if state != "before":
        base = fields("Pay", "90k", "55 s", "+2", "1")
        if state == "new": tk = base + [("fact", "temp", "fearful", "learned", True), ("gap", 4), ("mark", "half", False)]
        elif state == "seen": tk = base + [("fact", "temp", "fearful", "learned", False), ("gap", 4), ("mark", "half", False)]
        elif state == "driver-new":
            tk = [("word", "Pay"), ("gap", 11), ("icon", "p_cash"), ("gap", 4), ("struck", "90k", 11.5), ("gap", 3), ("num", "70k"), ("gap", 10)] + base[5 + 0:]
            tk = tk[:-0 or None]
            tk = tk[:-2] + [("fact", "temp", "mercenary", "learned", True), ("gap", 4), ("mark", "ok", False)]
        elif state == "driver-seen":
            tk = [("word", "Pay"), ("gap", 11), ("icon", "p_cash"), ("gap", 1), ("num", "70k"), ("gap", 10)] + base[5:]
            tk = tk[:-2] + [("fact", "temp", "mercenary", "learned", False), ("gap", 4), ("mark", "ok", False)]
        draw_pill(b, tk, 783, 457, lit=True)
    return b.crop((740, 395, 1700, 520))

def hint_chip(txt):
    f = cap_font(21); w = int(f.getlength(txt)) + 24; im = Image.new("RGBA", (w, 34), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    d.rounded_rectangle((0, 0, w - 1, 33), 8, fill=(11, 22, 21, 255), outline=(247, 244, 236, 200), width=1); d.text((12, 17), txt, font=f, fill=INKC, anchor="lm"); return im


# ---------------------------------------------------------------- the Reroute board
OFFICE = R + "out/batch/shots/office-front-yaw210.png"
def ov_line(size, pts, width, rgb, alpha=1.0, dash=None, Q=3):
    im = Image.new("RGBA", (size[0] * Q, size[1] * Q), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    P = [(x * Q, y * Q) for x, y in pts]
    if not dash: d.line(P, fill=rgb + (int(255 * alpha),), width=int(width * Q), joint="curve")
    else:
        on, off = dash[0] * Q, dash[1] * Q; run = 0.0; pen = True
        for (x0, y0), (x1, y1) in zip(P, P[1:]):
            L = math.hypot(x1 - x0, y1 - y0); t = 0.0
            while t < L:
                seg = min((on if pen else off) - run, L - t)
                if pen: d.line((x0 + (x1 - x0) * t / L, y0 + (y1 - y0) * t / L, x0 + (x1 - x0) * (t + seg) / L, y0 + (y1 - y0) * (t + seg) / L), fill=rgb + (int(255 * alpha),), width=int(width * Q))
                t += seg; run += seg
                if run >= (on if pen else off) - 1e-6: pen = not pen; run = 0.0
    return im.resize(size, Image.LANCZOS)
def world_marks(base):
    """the hover of a Reroute wedge, drawn at 1080p (1 px per game px): where the patrol is and sweeps now (solid), where it goes (dotted, chevrons),
    where it waits (a dashed ring and its badge; off screen here, so the edge chip points). Cream over ink-deep, the kit's line language (briefs/054 hud29 spec)."""
    kd.S = 1.0
    W, H = base.size
    sweep = [(930, 560), (1070, 380)]; patrol = (990, 470)
    route = [(930, 560), (800, 700), (700, 860), (618, 1052)]
    ov = Image.new("RGBA", base.size, (0, 0, 0, 0))
    def stroke(pts, w, dash=None):
        for rgb, ww, a in ((INKDEEP, w * 2.3, 0.6), (CREAM, w, 1.0)):
            ov.alpha_composite(ov_line(base.size, pts, ww, rgb, a, dash))
    stroke(sweep, 2.9)
    for (x, y) in sweep:                       # end ticks across the sweep
        ux, uy = sweep[1][0] - sweep[0][0], sweep[1][1] - sweep[0][1]; n = math.hypot(ux, uy); nx, ny = -uy / n, ux / n
        stroke([(x - nx * 10, y - ny * 10), (x + nx * 10, y + ny * 10)], 2.9)
    stroke(route, 2.9, dash=(13, 11))
    # chevrons along the route
    for (x0, y0), (x1, y1) in zip(route, route[1:]):
        n = math.hypot(x1 - x0, y1 - y0); ux, uy = (x1 - x0) / n, (y1 - y0) / n; px_, py_ = -uy, ux
        cx, cy = x0 + ux * n * 0.55, y0 + uy * n * 0.55
        tri = [(cx + ux * 9, cy + uy * 9), (cx - ux * 6 + px_ * 8, cy - uy * 6 + py_ * 8), (cx - ux * 6 - px_ * 8, cy - uy * 6 - py_ * 8)]
        t = Image.new("RGBA", base.size, (0, 0, 0, 0)); td = ImageDraw.Draw(t); td.polygon(tri, fill=CREAM + (255,), outline=INKDEEP + (255,)); ov.alpha_composite(t)
    # the patrol now: a ground ring (solid, the hover ring's size) and its badge over the ring
    ring = Image.new("RGBA", base.size, (0, 0, 0, 0)); rd = ImageDraw.Draw(ring)
    for rgb, ww in ((INKDEEP, 5), (CREAM, 2.4)):
        rd.ellipse((patrol[0] - 26, patrol[1] - 16, patrol[0] + 26, patrol[1] + 16), outline=rgb + (255,), width=int(ww))
    ov.alpha_composite(ring)
    base.alpha_composite(ov)
    cm = cameo_img("badges/signal_unit", 22); paste(base, cm, patrol[0] - cm.width / 2, patrol[1] - 16 - cm.height)
    # the edge chip: the wait spot is 45 m up the street, below the frame: badge and an arrow toward it
    ex, ey = 618, 1052; ang = math.degrees(math.atan2(route[-1][1] - route[-2][1], route[-1][0] - route[-2][0]))
    arrow = icon("pointer", 14, CREAM).rotate(-ang, expand=True, resample=Image.BICUBIC)
    w = int(G(5 + 18 + 4 + 14 + 5)); pl = plate(w)
    paste(base, pl, ex - w / 2, ey - pl.height / 2 - 4)
    paste(base, cm_small := cameo_img("badges/signal_unit", 17), ex - w / 2 + G(4 + 5) , ey - 4 - cm_small.height / 2)
    paste(base, arrow, ex - w / 2 + G(4 + 5 + 17 + 4), ey - 4 - arrow.height / 2)
    kd.S = 2.98
    return base

def reroute_panels():
    s2 = Image.open(SHOT2).convert("RGBA"); pad = 170
    def ext(im): c = Image.new("RGBA", (im.width + pad, im.height), BG + (255,)); c.paste(im, (pad, 0)); return c
    today = ext(s2)
    pick = ext(s2)
    right = 485 + pad; yc = 135
    tk = [("word", "Reroute"), ("gap", 5), ("cameo", "badges/signal_unit"), ("gap", 9), ("icon", "p_time"), ("gap", 1), ("num", "80 s"), ("gap", 10), ("icon", "p_trail"), ("gap", 1), ("num", "+1")]
    w = pill_width(tk) - G(8); draw_pill(pick, tk, right - w, yc, lit=True)
    return today, pick

def others_panel():
    s2 = Image.open(SHOT2).convert("RGBA"); pad = 170
    rows = []
    specs = [([("word", "Loop"), ("gap", 5), ("cameo", "badges/signal_camera"), ("gap", 5), ("word", "entrance"), ("gap", 9), ("icon", "p_time"), ("gap", 1), ("num", "62 s"), ("gap", 10), ("icon", "p_trail"), ("gap", 1), ("num", "+1")], "Loop, from the cabinet (three cameras: the picture, then the tag word)"),
             ([("word", "Call"), ("gap", 5), ("cameo", "badges/signal_guard"), ("gap", 9), ("icon", "p_time"), ("gap", 1), ("num", "6 s"), ("gap", 10), ("icon", "p_trail"), ("gap", 1), ("num", "+1")], "Call, from Inez (the one it moves is the door guard)")]
    for tk, cap in specs:
        c = Image.new("RGBA", (s2.width + pad, 120), BG + (255,)); c.paste(s2.crop((0, 0, s2.width, 120)), (pad, 0))
        w = pill_width(tk) - G(8); draw_pill(c, tk, 485 + pad - w, 60); rows.append((c, cap, ""))
    return rows

def reroute_board():
    today, pick = reroute_panels()
    office = Image.open(OFFICE).convert("RGBA"); world = world_marks(office.copy())
    wc = world.crop((300, 180, 1500, 1080)).resize((900, 675), Image.LANCZOS)
    sheet([(today, "Today (Sky's 0.2.99 shot)", "the pill names no object"),
           (pick, "Pick: the object's cameo after the verb", "the patrol's badge in the kit's chrome ring; the price line is as it was"),
           (wc, "While the Reroute wedge is hovered", "the patrol's sweep now (solid), where it goes (dotted, chevrons), where it waits (edge chip: it is 45 m off screen). Placement indicative")],
          1, "board-04-reroute.png", "057 - what is Reroute moving? the patrol, and where it goes",
          "Over Sky's shot and the office-front batch frame. Cream and ink-deep only; no new colour, no words.", capH=74)
    sheet(others_panel(), 1, "board-05-remote-verbs.png", "057 - the same gap on the other verbs whose object is not under the ring", "Loop and Call on the same cabinet and Inez rings; Enter, Take, Board and False tip already sit on their object.", capH=60)
if __name__ == "__main__":
    which = sys.argv[1:] or ["inez", "reroute"]
    if "inez" in which:
        today = shot_today = Image.new("RGBA", (Image.open(SHOT).width + OX, 888), BG + (255,)); today.paste(Image.open(SHOT).convert("RGBA"), (OX, 0))
        bB, _ = after_ring(tail_B); bBs, _ = after_ring(tail_B, newflag=False)
        bA, _ = after_ring(tail_plain, True, extra_A); bC, _ = after_ring(tail_plain, True, extra_C)
        for nm, im in (("today", today), ("A", bA), ("B", bB), ("C", bC), ("Bs", bBs)): im.convert("RGB").save(SC + "after-%s.png" % nm)
        W0 = 40; sc = 0.52
        def pan(im): c = im.crop((W0, 0, im.width, im.height)); return c.resize((int(c.width * sc), int(c.height * sc)), Image.LANCZOS)
        sheet([(pan(today), "Today, 0.2.99 (Sky's shot)", "Pay's price ends in a dashed ?; nothing says what Listen taught"),
               (pan(bA), "A  Dossier tag", "the three slots on the hub's rim; shape twins on the wedges; the price pill changes"),
               (pan(bB), "B  Reasons in the pill  (pick)", "each price line ends with the facts it read, then its mark; the new ones carry the kit's unread dot"),
               (pan(bC), "C  Burst on the wedges", "only what the last act taught, on the wedge it priced; gone once seen")],
              2, "board-01-options.png", "057 - what the cell learned, at the ring: three ways, after Listen on Inez",
              "Over Sky's 0.2.99 shot (canvas extended to the left). Inez is Fearful, owes a debt, loves the guard. Revised A: kit icons, kit marks, no new colour, no words.", capH=74)
        # states of the pick
        rows = [(pay_state("before"), "1  Now", "the ring on Inez before Listen: Pay ends in the dashed ?"),
                (pay_state("new"), "2  Listen has just landed", "her temperament (Fearful) rides on Pay; the ? is now a half mark: works, one step worse. Dot = new"),
                (pay_state("seen"), "3  Seen", "the ring has shown it for 1.2 s: the dot is gone, the token and the mark stay (that is the price's reason)"),
                (pay_state("driver-new"), "4  When a number moves (the driver, Mercenary)", "the dim struck 90k is the worst case it showed before; 70k is the price now"),
                (pay_state("driver-seen"), "5  The driver, seen", "the struck number and the dot are gone")]
        sheet(rows, 1, "board-02-pick-states.png", "057 pick B: one Pay pill through the cell's learning", "Pill redrawn from the kit over Sky's shot (3 px per game px). Rows 4-5: the driver's Pay, illustrative.", capH=74)
        # detail: the three pills at native scale, the clipping check
        def pc(im, box): return im.crop(box)
        _, bx = after_ring(tail_B)
        det = []
        for k, cap in (("pressure", "Pressure: three facts it read, then the mark"), ("help", "Help: the need and the temperament, then half (two orders)"), ("pay", "Pay: the temperament, then half")):
            x0, y0, x1, y1 = bx[k]; det.append((bB.crop((int(x0 - 40), int(y0 - 20), int(x1 + 40), int(y1 + 20))), cap, "native crop, 3 px per game px (2x review: scale by 0.67 to read at the kit's 2x)"))
        sheet(det, 1, "board-03-pick-detail.png", "057 pick B: the three pills, cropped for clipping, centring and padding", "No token touches the pill's rim or the dot clips it; the tail has the same 6 px padding as the other pills.", capH=74)
        print("inez done")
    if "reroute" in which: reroute_board()
