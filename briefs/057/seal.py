# Board for brief 057 section 4: the seal clock, the car's state, the fail line. Run: python3 -I briefs/057/seal.py
import sys, os, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kitdraw as kd
from kitdraw import *
R_ = R; OUT = R + "briefs/057/"
SHOT4 = "/Users/sky/.t3/userdata/attachments/6f2fb9e2-3d6f-4cb6-bf62-66a9d065638d-b82fd537-e87b-4868-bb57-61080580893b.png"
BG = (23, 48, 47); AMBER = (240, 160, 32); DEEP = (31, 108, 103); PLATE = (15, 39, 40)
def fnt(n): return ImageFont.truetype(R + "testbed/hud-style/fonts/Jost-Medium-Tab.ttf", n)
def kf(name, n): return ImageFont.truetype(K + "fonts/" + name, n)

# ---- 1. the card: Sky's shot, the sub-line and the story line rewritten from what happened
card = Image.open(SHOT4).convert("RGB"); d = ImageDraw.Draw(card)
def wipe(box):
    x0, y0, x1, y1 = box; strip = card.crop((1000, y0, 1100, y1)).resize((x1 - x0, y1 - y0)); card.paste(strip, (x0, y0))
def _size():
    for s in range(30, 60):
        if kf("Barlow-Medium.ttf", s).getlength("Sealed off before Vale reached the car.") >= 752: return s
    return 44
SZ = _size()
def put(xy, txt, size=None): size = size or SZ; d.text(xy, txt, font=kf("Barlow-Medium.ttf", size), fill=(247, 244, 236), anchor="ls")
wipe((100, 195, 1000, 255)); put((113, 238), "Sealed in. Police held the car, so it could not leave.")
wipe((100, 515, 1600, 562)); put((113, 552), "was taken outside. Vale reached the getaway car. Police held it.", int(SZ * 0.92))
card_small = card

# ---- 2. the clock bar and the seal marker (shot 3 px = 1 game px at 1080p, drawn at 2x)
kd.S = 2.0
office = Image.open(R + "out/batch/shots/office-front-yaw210.png").convert("RGBA")
def bar(state):
    base = office.crop((1431, 0, 1920, 125)).resize((489 * 2, 125 * 2), Image.LANCZOS)
    S_ = kd.S; txt, frac, last, cause = state
    h = int(38 * S_); w = int((12 + 16 + 6 + 38 + 12) * S_)
    pl = nine("markers/plate_dark", w, h, 24, S_ / 2 * 0.9)
    right = (1795 - 1431) * 2; x = right - w; y = int(62 * S_)
    if cause:   # on hover: the cause as an icon on a kit disc in the chrome ring, left of the marker (no sentence)
        dsc = cameo_img("markers/disc_dark_24", 30)
        ic = icon("fire", 16); dsc.alpha_composite(ic, ((dsc.width - ic.width) // 2, (dsc.height - ic.height) // 2))
        base.alpha_composite(dsc, (int(x - dsc.width - 6 * S_), int(y + (h - dsc.height) / 2)))
    base.alpha_composite(pl, (x, y))
    base.alpha_composite(icon("lock", 17, CREAM), (int(x + 12 * S_), int(y + (h - 17 * S_) / 2)))
    f = font("Jost-Bold.ttf", 15.5); dd = ImageDraw.Draw(base)
    dd.text((x + 12 * S_ + 23 * S_, y + h / 2 + 1), txt, font=f, fill=AMBER if last else CREAM, anchor="lm")
    uw = w - 2 * 12 * S_; uy = y + h - 9 * S_
    dd.rectangle((x + 12 * S_, uy, x + 12 * S_ + uw * frac, uy + 2.5 * S_), fill=AMBER if last else TURQL)
    kd.S = S_; return base.convert("RGB")
states = [(("2:25", 1.0, False, True)), (("0:48", 0.33, False, False)), (("0:09", 0.06, True, False))]
bars = [bar(s) for s in states]
kd.S = 2.0

# ---- 3. the car's Go way: ready, and held (a mock of the ring's Go wedge and its pill beside it)
car = Image.open(R + "out/batch/shots/getaway-car-yaw210.png").convert("RGBA")
def go_panel(held):
    W, H = 760, 330; S_ = 2.0; kd.S = S_
    base = car.crop((380, 360, 380 + W // 2 * 1, 360 + H // 2 * 1)).resize((W, H), Image.LANCZOS)
    c = (150, 165); ro, ri = 84 * S_ * 0.8, 30 * S_ * 0.8
    ov = Image.new("RGBA", (W, H), (0, 0, 0, 0)); od = ImageDraw.Draw(ov)
    # a quarter of the ring: the Go wedge at 3 o'clock (90 deg wide), plate when held, deep teal and lit when ready
    pts = []
    for a in range(-45, 46, 3): pts.append((c[0] + ro * math.sin(math.radians(a + 90)), c[1] - ro * math.cos(math.radians(a + 90))))
    for a in range(45, -46, -3): pts.append((c[0] + ri * math.sin(math.radians(a + 90)), c[1] - ri * math.cos(math.radians(a + 90))))
    od.polygon(pts, fill=(PLATE + (235,)) if held else (DEEP + (255,)))
    base.alpha_composite(ov)
    ic = icon("car", 20, (160, 170, 165) if held else TURQL)
    mid = (ro + ri) / 2; base.alpha_composite(ic, (int(c[0] + mid - ic.width / 2), int(c[1] - ic.height / 2)))
    if held:   # shut: the kit's struck way, no colour
        dd = ImageDraw.Draw(base); dd.line((c[0] + mid - 18, c[1] + 16, c[0] + mid + 18, c[1] - 16), fill=CREAM, width=3)
    else:
        sparkle(base, c[0] + ro * 0.9 * math.sin(math.radians(90 + 25)), c[1] - ro * 0.9 * math.cos(math.radians(90 + 25)), 6.0)
    # the pill
    tk = [("word", "Go"), ("gap", 8)]
    if held: tk += [("icon", "lock", 14), ("gap", 5), ("cameo", "badges/signal_police"), ("gap", 2), ("cameo", "badges/signal_police"), ("gap", 2), ("cameo", "badges/signal_police")]
    else: tk += [("icon", "p_trail"), ("gap", 1), ("num", "+1")]
    px = c[0] + ro + 16 * S_ * 0.6
    draw_pill(base, tk, px, c[1], lit=not held)
    if held:   # dim the word and the lock (the pill is a refusal, not a button)
        pass
    return base.convert("RGB")
go_ready, go_held = go_panel(False), go_panel(True)

def cap(img, title, sub, w=None):
    im = img if w is None else img.resize((w, int(img.height * w / img.width)), Image.LANCZOS)
    c = Image.new("RGB", (max(im.width, 700), im.height + 84), BG); q = ImageDraw.Draw(c)
    q.text((0, 0), title, font=fnt(27), fill=(247, 244, 236)); q.text((0, 36), sub, font=fnt(19), fill=(176, 205, 199)); c.paste(im, (0, 84)); return c
cw = 1000
parts_top = [cap(Image.open(SHOT4).convert("RGB"), "Today: the card said both", "\"Sealed off before Vale reached the car.\" and, below, \"Vale reached the getaway car.\"", cw),
             cap(card_small, "After: what happened (the car was held)", "the card's line and the story line agree; the debrief names the holders", cw)]
parts_bar = [cap(bars[0], "1  The seal is set: the marker appears, with its cause on hover", "lock, m:ss, a draining underline; the fire disc = the shooting that sealed it", None),
             cap(bars[1], "2  Running", "underline drains over the seal's length", None),
             cap(bars[2], "3  Last 10 s", "amber text and underline, blinking (the window marker's rule)", None)]
parts_go = [cap(go_ready, "Go ready (Vale aboard, car free)", "the wedge is lit and leads the ring; the star says it just opened", None),
            cap(go_held, "Go shut (held)", "the wedge struck, the holders as police badges on the pill; the same holders ringed in the world", None)]
W = max(sum(p.width for p in parts_top) + 30, sum(p.width for p in parts_bar) + 60, sum(p.width for p in parts_go) + 30) + 60
H = 110 + max(p.height for p in parts_top) + 30 + max(p.height for p in parts_bar) + 30 + max(p.height for p in parts_go) + 30
out = Image.new("RGB", (W, H), BG); q = ImageDraw.Draw(out)
q.text((30, 20), "057 - the seal clock, the car's state, and a fail card that says what happened", font=fnt(36), fill=(247, 244, 236))
q.text((30, 66), "Over Sky's 0.2.99 fail card, the clock bar of the office-front batch shot and the getaway-car frame. Kit sprites, cream, turquoise-light and the window marker's amber only.", font=fnt(20), fill=(176, 205, 199))
y = 110
for row in (parts_top, parts_bar, parts_go):
    x = 30
    for p in row: out.paste(p, (x, y)); x += p.width + 30
    y += max(p.height for p in row) + 30
out.save(OUT + "board-08-seal.png"); print(out.size)
