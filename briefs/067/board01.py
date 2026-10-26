# Glyph, brief 067, board 01 (C): squad boarding and Drive. python3 -I briefs/067/board01.py -> briefs/067/board-01-squad-board-and-drive.png
import sys; sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/067")
from g67 import *
OUTD = R + "briefs/067/"
SQR = R + "out/batch/shots/start-overhead-day.png"; SQH = R + "out/batch/shots/start-overhead-day.hud.png"
YARD = R + "briefs/023/latch/service-door-210.png"
CARSQ = (940, 836)

# ---- A: three selected, the pill on the car: one hold boards them
def pa():
    f = Frame(SQR)
    pill(f, (CARSQ[0], CARSQ[1] - 40), "Hold", "Board · 6 s", None, "car", faces=[SCOUT, FIXER, TECH])
    return f

# ---- B: the seat pips: ordered, one landed, all landed (as built, plus a turning pending pip)
def pb(states, turn):
    f = Frame(SQR); seat_pips(f, (CARSQ[0], CARSQ[1] - 24), states, turn); return f

# ---- C: the roster: a refused agent says why, in two words; the boarded wear the in-car badge
def pc():
    f = Frame(SQR)
    for k, y in ((0, 65), (1, 160)):                                       # the in-car badge in the status slot (as built: disc_dark_24 with the car)
        lay = layer(f); disc(lay, 51, y + 45, 16, "car"); f.hud.alpha_composite(lay)
    why_chip(f, 108, 258, "s_looping", "looping")
    why_chip(f, 108, 355, "s_carrying", "hands full")
    return f

# ---- D: the car's ring, nobody selected: Vale not aboard (left), Vale aboard with the Tech outside (right)
def pd(vale):
    f = Frame(SQR); C = (CARSQ[0] - 10, CARSQ[1] - 70)
    if not vale:
        ring67(f, C, [dict(icon="arrive", word="Drive", fields=[("time", "28 s")], cams=[SCOUT]),
                      dict(icon="car", word="Go", fields=[("camx", VALE)], cams=[SCOUT]),
                      dict(more=True)], lit=0)
    else:
        ring67(f, C, [dict(icon="car", word="Go", cams=[SCOUT]),
                      dict(icon="car", word="Board", fields=[("time", "4 s")], cams=[TECH]),
                      dict(icon="arrive", word="Drive", fields=[("time", "28 s")], cams=[SCOUT]),
                      dict(more=True)], lit=0)
    return f

# ---- E: the stands: at rest (left), live on hover (right)
def g(x, y): return (x * 0.96, y * 0.96)
YARD_BAY = g(985, 690); PSI_DRIVE = -143.8
def pe(live):
    f = Frame(YARD); car_ghost(f, YARD_BAY, PSI_DRIVE, CREAM, dash=None, with_disc=False)                 # Vale's car, parked
    ghost = ground_xy(YARD_BAY, -(CAR_L + 1.0), 0, PSI_DRIVE)
    if live:
        rt = [g(1990, 1100), g(1700, 990), g(1500, 930), ghost]
        gline(f, rt, CREAM, 2.4, dash=(7, 6)); chevrons(f, rt, 3.0, CREAM, 6.0)
        car_ghost(f, ghost, PSI_DRIVE + 180, TURQL, dash=None, alpha=255, with_disc=True, nose=True)
        pill(f, (ghost[0], ghost[1] - 40), "Hold", "Drive · 28 s", None, "arrive", faces=[SCOUT])
    else:
        car_ghost(f, ghost, PSI_DRIVE + 180, CREAM, dash=(7, 6), alpha=int(.55 * 255), with_disc=False, nose=True)
    return f

def strip(frames, box, labels, up=2):
    """a vertical strip of crops (game px box), each upscaled `up`, with a label to the left"""
    ims = [f.crop(box).convert("RGB") for f in frames]; ims = [im.resize((im.width * up, im.height * up), Image.LANCZOS) for im in ims]
    lw = 150; W = lw + ims[0].width; H = sum(im.height for im in ims) + 12 * (len(ims) - 1); out = Image.new("RGB", (W, H), BG); d = ImageDraw.Draw(out); y = 0
    for im, lab in zip(ims, labels):
        out.paste(im, (lw, y)); d.text((8, y + im.height / 2), lab, font=board_font(26), fill=BCREAM, anchor="lm"); y += im.height + 12
    return out

def side(a, b, gap=24):
    out = Image.new("RGB", (a.width + b.width + gap, max(a.height, b.height)), BG); out.paste(a, (0, 0)); out.paste(b, (a.width + gap, 0)); return out

if __name__ == "__main__":
    a = pa(); c = pc(); d1 = pd(False); d2 = pd(True); e1 = pe(False); e2 = pe(True)
    b1 = pb(["empty", "pending", "pending", "pending", "empty"], 20); b2 = pb(["empty", "full", "pending", "pending", "empty"], 140); b3 = pb(["empty", "full", "full", "full", "empty"], 0)
    B = {}
    B["board"] = budget(a, SQH, False); B["ring"] = budget(d1, SQH, False); B["chips"] = budget(c, SQH, False)
    pipbox = (868, 790, 1012, 858)
    A_ = a.crop((690, 690, 1210, 900)).convert("RGB"); B_ = strip([b1, b2, b3], pipbox, ["ordered", "one landed", "all landed"], up=2); C_ = c.crop((0, 20, 250, 420)).convert("RGB")
    D1 = d1.crop((760, 600, 1180, 940)).convert("RGB"); D2 = d2.crop((760, 600, 1180, 940)).convert("RGB")
    E1 = e1.crop((820, 560, 1300, 900)).convert("RGB"); E2 = e2.crop((820, 560, 1300, 900)).convert("RGB")
    items = [
        (A_, 0, 0, "One hold boards the squad", "Three selected, hover the car: pill_status 38 px, the car disc, Hold · Board · 6 s (the slowest doer's walk + act), a 24 px face for each doer. The same order from the ring's one Board wedge or a portrait drop."),
        (B_, 1070, 0, "The seat pips", "pending: a turquoise-light dashed ring turning once in 1.5 s · filled: the disc, with a 0.3 s landing beat and a ring leaving it. Vale's pip first and larger."),
        (C_, 1800, 0, "A refused agent says why", "the busy chip's frame (plate_dark 36 px): the cause's own icon and two words, 3 s. The boarded wear the in-car badge."),
        (side(D1, D2, 24), 0, 1010, "The car's ring, nobody selected: Vale not aboard (left) · Vale aboard, the Tech outside (right)", "left: Drive lit, Go last and never lit with a struck Vale cameo after it (it commits with the 0.5 s hold) · right: Go first and lit, Board with the Tech's cameo, Drive. The givers' cameos sit under each icon; the pills carry the price."),
        (side(E1, E2, 24), 0, 1910, "The stand outlines: at rest · live", "left: anyone aboard, nothing selected: the footprint at 55 %, no disc, no route · right: the car or the stand hovered: solid turquoise-light, the route, Hold · Drive · 28 s and the driver's face. The cream footprint beside it is Vale's parked car."),
    ]
    print(compose(items, OUTD + "board-01-squad-board-and-drive.png", "067 · C · squad boarding and Drive",
          "The square at yaw 30 and the yard at yaw 210, play zoom, 2 px per game px, drawn from the kit over 0.2.122 frames (placement indicative). Drive's icon is arrive: the ring would otherwise show three identical cars."))
    import json; print(json.dumps(B))
