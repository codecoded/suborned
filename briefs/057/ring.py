# Glyph, brief 057 section 1b: what the cell learned, ON the action ring (Sky 2026-10-24: "on the action ring, not the price box"),
# and a shorter price pill. Run: python3 -I briefs/057/ring.py   Writes board-09..11 beside this file.
# Redraws the ring's wedge icons and the pills from the game's own kit (kitdraw.py), over Sky's 0.2.99 shot (canvas extended to the left).
import sys, os, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kitdraw as kd
from kitdraw import *
import boards as bd
import cv2, numpy as np
OUT = R + "briefs/057/"; SC = R + "out/scratch/glyph/057/"; os.makedirs(SC, exist_ok=True)
OX = bd.OX; C = bd.C; RO, RI = 84, 30
BG = bd.BG; INKC = bd.INKC; cap_font = bd.cap_font; sheet = bd.sheet
LIT = {90}                                        # Pay is the lit wedge in Sky's shot

def blot(img, boxes, rad=9):
    a = np.array(img.convert("RGB")); m = np.zeros(a.shape[:2], "uint8")
    for x0, y0, x1, y1 in boxes: m[y0:y1, x0:x1] = 255
    return Image.fromarray(cv2.inpaint(a, m, rad, cv2.INPAINT_TELEA)).convert("RGBA")

def clean_base():
    """Sky's shot with the three pills and the four wedge icons taken out (the ring and pills are redrawn), on the extended canvas"""
    b = Image.open(SHOT).convert("RGBA")
    boxes = [(284, 96, 696, 168), (778, 418, 1600, 494), (156, 742, 822, 818),      # pills: Watch, Pay, Ask
             (430, 208, 548, 322), (610, 415, 705, 490), (270, 425, 380, 480), (440, 585, 540, 665)]  # icons: Watch, Pay, dots, Ask (+ the cup tag stays)
    b = blot(b, boxes, 5)
    c = Image.new("RGBA", (b.width + OX, b.height), BG + (255,)); c.paste(b, (OX, 0)); return c

def dirv(deg): a = math.radians(deg); return (math.sin(a), -math.cos(a))
def wpos(deg, r, dx=0.0, dy=0.0):
    d = dirv(deg); return (C[0] + d[0] * r * S + dx * S, C[1] + d[1] * r * S + dy * S)

ICON = {0: "pressure", 90: "pay", 180: "ask", 270: "help"}
MID = (RO + RI) / 2          # 57: where the verb icon sits

def put_icon(b, deg, lift=0.0):
    x, y = wpos(deg, MID, 0, -lift); im = icon(ICON[deg], 22, TURQL if deg in LIT else CREAM)
    paste(b, im, x - im.width / 2, y - im.height / 2)
def spark(b, deg):
    d = dirv(deg + 25); r = (RO - 9) * S; sparkle(b, C[0] + d[0] * r, C[1] + d[1] * r)

# ---- what each wedge knows (Inez after Listen: Fearful, in debt, the affair with the guard). Facts a quote read: Pay temp; Help need+temp; Pressure secret+need+temp.
WEDGES = {
    0:   dict(facts=[("secret", "kin"), ("need", "debt"), ("temp", "fearful")], mark="ok",   new=True,  spark=True),
    90:  dict(facts=[("temp", "fearful")],                                       mark="half", new=True,  spark=False),
    180: dict(facts=[],                                                          mark="no",   new=False, spark=False),
    270: dict(facts=[("need", "debt"), ("temp", "fearful")],                     mark="half", new=True,  spark=True),
}

# ---- the pills, short rule: $ and time always; Trail when above 0; Alert as the siren alone (its number only above 1); the eye only when open or unknown;
#      the outcome mark leaves the pill (it sits with its reason on the ring).
def pill_tokens(word, cash, t, trail=None, alert=None, eye=None, mark=None, short=True):
    L = [("word", word), ("gap", 11)]
    if cash: L += [("icon", "p_cash"), ("gap", 1), ("num", cash), ("gap", 10)]
    L += [("icon", "p_time"), ("gap", 1), ("num", t), ("gap", 10)]
    if short == "min": return L[:-1]
    if trail: L += [("icon", "p_trail"), ("gap", 1), ("num", trail), ("gap", 10)]
    if alert:
        L += [("icon", "p_alert")]
        if not short or alert != "1": L += [("gap", 1), ("num", alert)]
        L += [("gap", 10)]
    if eye: L += [("icon", "eye_" + eye, 17, CREAM if eye != "shut" else INK), ("gap", 10)]
    if mark and not short: L += [("mark", mark, False)]
    return L[:-1] if L[-1][0] == "gap" else L
PILLS = {   # deg: (side, tokens)
    0:   ("top",   lambda short: pill_tokens("Pressure", None, "40 s", "+3", "1", "shut" if not short else None, "ok", short)),
    90:  ("right", lambda short: pill_tokens("Pay", "90k", "55 s", "+2", "1", "shut" if not short else None, None, short)),
    180: ("left",  lambda short: pill_tokens("Ask", None, "30 s", "+1", "1", "shut" if not short else None, None, short)),
    270: ("left",  lambda short: pill_tokens("Help", "40k", "55 s", "+1", None, "shut" if not short else None, None, short)),
}
def draw_pills(b, short=True, minrule=True):
    boxes = {}
    for deg, (side, mk) in PILLS.items():
        lit = deg in LIT; tk = mk(True if lit or not minrule else "min"); w = pill_width(tk) - G(8)
        if deg == 0: x, yc = C[0] - w / 2, 131.5
        elif deg == 90: x, yc = 783 + OX, 457
        elif deg == 180: x, yc = 165 + OX + 14, 780     # as Sky's Ask pill sat
        else: x, yc = 195 + OX - w, 457
        boxes[deg], _ = draw_pill(b, tk, x, yc, lit=lit)
    return boxes

def mark_img(kind, h=13): return sprite("badges/mark_" + kind, game_h=h)

# ---------------------------------------------------------------- option A: the facts under the verb (the cameo-row slot), with the mark that follows from them
def tok_sz(kind, k): return tuple(v * k for v in SHAPES[kind][:2])
def option_A(b, k=0.9, W=None):
    W = W or WEDGES
    for deg in (0, 90, 180, 270):
        wd = W[deg]; put_icon(b, deg, lift=8.0)
        if wd["spark"]: spark(b, deg)
        # the caption row: [facts][mark], centred under the icon (the row the group cameos use today, mid.y + 7)
        items = []
        for kind, ic in wd["facts"]: items.append(("t", token_img(kind, ic, "learned", k)))
        items.append(("m", mark_img(wd["mark"], 13)))
        gap = G(2); tw = sum(i[1].width for i in items) + gap * (len(items) - 1)
        cx, cy = wpos(deg, MID, 0, 11.0)
        x = cx - tw / 2
        for t, im in items:
            paste(b, im, x, cy - im.height / 2)
            if t == "t" and wd["new"]: nub(b, x + im.width - G(1.2), cy - im.height / 2 + G(1.2), g=5.5)
            x += im.width + gap
    return b

# ---------------------------------------------------------------- option B: the facts on the hub's rim, seated toward the ways they change; the mark badges the verb icon
def option_B(b, k=0.95):
    for deg in (0, 90, 180, 270):
        wd = WEDGES[deg]; put_icon(b, deg, lift=0.0)
        if wd["spark"]: spark(b, deg)
        x, y = wpos(deg, MID, 10.5, 9.5); mk = mark_img(wd["mark"], 15)
        # a kit disc under the mark so it reads on the teal and on the dark wedge
        dsc = sprite("markers/disc_dark_24", game_h=19); paste(b, dsc, x - dsc.width / 2, y - dsc.height / 2)
        paste(b, mark_img(wd["mark"], 15), x - mk.width / 2, y - mk.height / 2)
    # seats: need on the seam of the two ways it gates (Help | Pressure), secret under Pressure, temperament at the hub's foot (it prices every way)
    seats = [("need", "debt", 315), ("secret", "kin", 0), ("temp", "fearful", 180)]
    for kind, ic, deg in seats:
        im = token_img(kind, ic, "learned", k); x, y = wpos(deg, RI)
        # a chrome seat ring behind (the cameo's ring: 2 px chrome over 1 px ink-deep) so it sits in the rim like a gem
        rr = int(round(max(im.width, im.height) / 2 + G(2.2))); seat = Image.new("RGBA", (rr * 2 * 4, rr * 2 * 4), (0, 0, 0, 0)); dd = ImageDraw.Draw(seat)
        dd.ellipse((0, 0, rr * 8 - 1, rr * 8 - 1), fill=INKDEEP + (255,)); q = int(G(1) * 4 / 2); dd.ellipse((q, q, rr * 8 - 1 - q, rr * 8 - 1 - q), fill=CHROME + (255,))
        q2 = q + int(G(2) * 4 / 2); dd.ellipse((q2, q2, rr * 8 - 1 - q2, rr * 8 - 1 - q2), fill=INKDEEP + (255,))
        seat = seat.resize((rr * 2, rr * 2), Image.LANCZOS); paste(b, seat, x - rr, y - rr)
        paste(b, im, x - im.width / 2, y - im.height / 2); nub(b, x + im.width / 2 - G(0.5), y - im.height / 2 + G(0.5), g=6)
    return b

CROP = (140 + OX - 40 - 600, 70, 1620 + OX - 600 + 40, 840)
def view(im, scale=0.62):
    c = im.crop((OX - 600 + 60 + 40, 50, OX + 1620, 850)); return c.resize((int(c.width * scale), int(c.height * scale)), Image.LANCZOS)

# ======================================================================= board 10: the pick (A) through the cell's learning, at 3 px per game px
def ring_crop(im): return im.crop((int(C[0] - 300), 150, int(C[0] + 300), 750))
def with_pill(im, deg, short=True, only=None):
    tk = PILLS[deg][1](short); return tk
def state_before():
    """before Listen: Watch (top), Pay (right, outcome unknown), Ask (bottom), the way that is not open yet is the dots"""
    b = clean_base()
    # the ring: the shot's own wedges, icons redrawn
    ico = {0: "watch", 90: "pay", 180: "ask"}
    for deg, nm in ico.items():
        x, y = wpos(deg, MID, 0, -8 if deg else 0); im = icon(nm, 22, TURQL if deg in LIT else CREAM); paste(b, im, x - im.width / 2, y - im.height / 2)
    # the more-ways dots on the left wedge, as the game draws them
    x, y = wpos(270, MID); d = ImageDraw.Draw(b)
    for i in (-1, 0, 1): cx = x + i * (3.2 + 2.4 * 1.4) * S; r = 2.4 * S; d.ellipse((cx - r, y - r, cx + r, y + r), fill=CREAM)
    # captions: only the unknown temperament, as the dashed mark, under Pay; the Ask's no
    for deg, mk in ((90, "unk"), (180, "no")):
        im = mark_img(mk, 13); cx, cy = wpos(deg, MID, 0, 11.0); paste(b, im, cx - im.width / 2, cy - im.height / 2)
    # pills: Watch top, Pay right, Ask bottom, short
    tkw = [("word", "Watch"), ("gap", 11), ("icon", "p_time"), ("gap", 1), ("num", "20 s"), ("gap", 10), ("icon", "routine", 17)]
    w = pill_width(tkw) - G(8); draw_pill(b, tkw, C[0] - w / 2, 131.5, lit=False)
    tk = PILLS[90][1](True); draw_pill(b, tk, 783 + OX, 457, lit=True)
    tk = PILLS[180][1](True); draw_pill(b, tk, 165 + OX + 14, 780)
    return b
def build_states():
    base = clean_base()
    wl = {k: dict(v) for k, v in WEDGES.items()}
    s2 = base.copy(); draw_pills(s2, True); option_A(s2, W=wl)
    wl3 = {k: dict(v, new=False) for k, v in WEDGES.items()}
    s3 = base.copy(); draw_pills(s3, True); option_A(s3, W=wl3)
    # a sparkle without dots after the dots clear: the sparkle keeps its 90 s
    return state_before(), s2, s3

def pill_row(tk, bgcrop, lit=False, width=1500, h=96):
    """a pill on a plain strip of the shot's own background (a crop of its pavement), at 3 px per game px"""
    im = bgcrop.resize((width, h)).convert("RGBA"); w = pill_width(tk) - G(8)
    draw_pill(im, tk, G(10), h / 2, lit=lit); return im, w / S
def board_pills():
    shot = Image.open(SHOT).convert("RGBA"); strip = shot.crop((1000, 560, 1792, 700))
    P = lambda deg, v: PILLS[deg][1](v)
    watch = [("word", "Watch"), ("gap", 11), ("icon", "p_time"), ("gap", 1), ("num", "20 s"), ("gap", 10), ("icon", "routine", 17)]
    watch_min = watch[:5]
    odd_before = [("word", "Knock out"), ("gap", 11), ("icon", "p_time"), ("gap", 1), ("num", "14 s"), ("gap", 10), ("icon", "p_trail"), ("gap", 1), ("num", "+3"), ("gap", 10),
                  ("icon", "p_alert"), ("gap", 1), ("num", "≥2"), ("gap", 10), ("icon", "eye_open", 17, CREAM)]
    cases = [("Watch", None, watch, watch_min, (288, 100, 690, 165), False),
             ("Pay (lit wedge)", None, P(90, True), P(90, "min"), (783, 424, 1595, 490), True),
             ("Ask", None, P(180, True), P(180, "min"), (160, 745, 818, 815), False),
             ("Pressure", P(0, False), P(0, True), P(0, "min"), None, False),
             ("Help", P(270, False), P(270, True), P(270, "min"), None, False),
             ("Something unusual: seen, Alert 2 or more", odd_before, odd_before, odd_before[:6], None, False)]
    out = []
    for name, btk, atk, mtk, rb, lit in cases:
        if rb: b_im = shot.crop(rb).convert("RGBA"); bw = (rb[2] - rb[0] - 24) / S
        else: b_im, bw = pill_row(btk, strip, lit, 1000, 96)
        a_im, aw = pill_row(atk, strip, lit, 800, 96); m_im, mw = pill_row(mtk, strip, False, 800, 96)
        out.append((name, b_im, a_im, m_im, bw, aw, mw))
    return out

def build_boards():
    s1, s2, s3 = build_states()
    for n, i in (("s1", s1), ("s2", s2), ("s3", s3)): i.convert("RGB").save(SC + "state-%s.png" % n)
    sheet([(ring_crop(s1), "1  Before Listen", "Pay ends in the dashed mark: temperament unknown"),
           (ring_crop(s2), "2  Listen has just landed", "the facts sit under the ways they price; the dot is new"),
           (ring_crop(s3), "3  Seen", "the dots clear after 1.2 s on the ring")],
          3, "board-10-ring-pick-states.png", "057 s1b pick A: the ring through what Listen taught", "Native 3 px per game px (1.5x the kit's 2x). Same wedges, same sparkle; only the caption row under each verb changes.", capH=100)
    # board 9 again with the final geometry
    today = Image.new("RGBA", (Image.open(SHOT).width + OX, Image.open(SHOT).height), BG + (255,)); today.paste(Image.open(SHOT).convert("RGBA"), (OX, 0))
    base = clean_base(); a = base.copy(); draw_pills(a, True); option_A(a)
    bB = base.copy(); draw_pills(bB, True); option_B(bB)
    sheet([(view(today), "Today, 0.2.99 (Sky's shot)", "the ring knows nothing of what Listen taught; the price box carries a dashed ?"),
           (view(a), "A  Under the verb  (pick)", "each way's own facts under its icon, then the mark they give; the dot is the kit's unread dot"),
           (view(bB), "B  On the hub", "the three facts seated in the hub's rim toward the ways they change; the mark badges each verb")],
          1, "board-09-ring-options.png", "057 s1b - what the cell learned, ON the ring", "Over Sky's 0.2.99 shot, after Listen on Inez. Icons only; the price pills are the short ones (board 11).", capH=74)
    # detail: B and A ring crops side by side, native
    sheet([(ring_crop(a), "A  Under the verb", "native crop"), (ring_crop(bB), "B  On the hub", "native crop")], 2,
          "board-12-ring-detail.png", "057 s1b: the two rings cropped for clipping and centring", "3 px per game px. Tokens are 13 px (Intel's 15 x 0.9), icons 9 px; the dot is 5.5 px.", capH=74)
    # pills
    rows = []; widths = []
    for name, b_im, a_im, m_im, bw, aw, mw in board_pills():
        rows.append((b_im, name + " - today", "%d game px wide" % round(bw)))
        rows.append((a_im, "after: the lit wedge", "%d px  (%d %% of today)" % (round(aw), round(100 * aw / bw))))
        rows.append((m_im, "after: the other wedges", "%d px  (%d %%)" % (round(mw), round(100 * mw / bw))))
        widths.append((name, round(bw), round(aw), round(mw)))
    sheet(rows, 3, "board-11-pills.png", "057 s1b: the price pill, shorter", "Today, then the short line on the lit wedge, then the quieter line on the others. Alt shows the full line again.", capH=74)
    print(widths)

if __name__ == "__main__":
    build_boards()
