# Glyph, brief 062: direction B "On the street": the briefing drawn over the real Records Office frame, pieces anchored to the world.
import sys; sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/062")
from common import *
import portraits as pt
DOOR = (1123, 252); CAR = (1152, 346)         # 1080p coordinates in overhead-pickup

def stage(clean_cell=True):
    w = world()
    if clean_cell:      # take the cell and the stray ball out of the road: the briefing opens on the Office, the cell is somewhere else
        a = np.array(w.convert("RGB")); m = np.zeros(a.shape[:2], "uint8")
        for cx, cy in ((1786, 1018), (1920, 1018), (2054, 1027), (1900, 1123)):
            m[cy - 52:cy + 92, cx - 42:cx + 42] = 255
        m[1190:1300, 1500:1600] = 255
        small = cv2.resize(a, (1920, 1080), interpolation=cv2.INTER_AREA); ms = cv2.resize(m, (1920, 1080), interpolation=cv2.INTER_NEAREST)
        f = cv2.inpaint(small, ms, 4, cv2.INPAINT_TELEA); up = cv2.resize(f, (3840, 2160), interpolation=cv2.INTER_CUBIC)
        mm = cv2.GaussianBlur(m, (21, 21), 0)[..., None] / 255.0; w = Image.fromarray((a * (1 - mm) + up * mm).astype("uint8")).convert("RGBA")
    return w

def light_line_word(im, x, y, s, px=26, track=0.34, anchor="rs", marker=True, focused=True):
    f = jost("Jost-Medium.ttf", px); tw = tw_of(s, "Jost-Medium.ttf", px, track)
    x0 = x - tw if anchor[0] == "r" else x
    txt(im, (x0, y), s, "Jost-Medium.ttf", px, CREAM, "ls", track, shadow=(0.45, 1.2))
    if focused:
        d = ImageDraw.Draw(im, "RGBA"); d.rectangle((P(x0), P(y + 9), P(x0 + tw), P(y + 11)), fill=TURQL + (255,))
        if marker:
            cx, cy = x0 - 22, y - px * 0.36; r = 6.5
            d.polygon([(P(cx), P(cy - r)), (P(cx + r), P(cy)), (P(cx), P(cy + r)), (P(cx - r), P(cy))], fill=TURQL + (255,))
    return tw

def clock_strip(im, right=1856, top=40, W=560, H=82, dim=False):
    pl = frame("markers/plate_dark", W, H, 24, 0.5); put(im, pl, right - W, top)
    x0, x1 = right - W + 56, right - 56; L = x1 - x0; t = lambda m: x0 + L * m / 510.0
    y = top + 56
    d = ImageDraw.Draw(im, "RGBA")
    d.line((P(x0), P(y), P(x1), P(y)), fill=CREAM + (90,), width=P(2.5))
    d.line((P(x0), P(y), P(t(330)), P(y)), fill=TURQL + (230,), width=P(4.5))
    for m, ic, lab, strong in ((0, "p_time", "0:00", False), (330, "enter", "5:30", True), (510, "car", "8:30", False)):
        x = t(m); r = 4.5 if not strong else 7
        d.ellipse((P(x - r), P(y - r), P(x + r), P(y + r)), fill=(INKDEEP if not strong else CREAM) + (255,), outline=(CREAM if not strong else TURQL) + (255,), width=P(2))
        ico(im, ic, x - 22 + (6 if m == 0 else 0), top + 26, 17, TURQL if strong else CREAM)
        txt(im, (x - 6 + (6 if m == 0 else 0), top + 32), lab, "Jost-Bold.ttf", 17 if strong else 15.5, TURQL if strong else CREAM, "ls")
    return im

def vale_card(im, x=1448, y=132, W=408, H=478):
    card = frame("panels/panel_card", W, H, 24, 0.5)      # slice top 56 @2x is the teal header
    shadowed(im, card, x, y, blur=18, alpha=0.45, dy=10)
    txt(im, (x + 24, y + 21), "TARGET", "Jost-Bold.ttf", 11.5, CREAM, "ls", 0.12)
    ph = pt.photo("councillor", (P(360), P(296)), silver=True); ph = rounded(ph, P(10))
    hdr = 30
    # photo with an ink-deep rim and a brass hairline inside the card
    rim = Image.new("RGBA", (ph.width + P(6), ph.height + P(6)), (0, 0, 0, 0)); ImageDraw.Draw(rim).rounded_rectangle((0, 0, rim.width - 1, rim.height - 1), radius=P(12), fill=INKDEEP + (255,))
    put(im, rim, x + 22 - 3, y + hdr + 12 - 3); put(im, ph, x + 22, y + hdr + 12)
    hairline(im, x + 22, y + hdr + 12 + 296 + 18, x + W - 22, y + hdr + 12 + 296 + 18)
    txt(im, (x + 24, y + hdr + 12 + 296 + 52), "Councillor Vale", "Jost-Bold.ttf", 28, INK, "ls")
    ico(im, "take", x + 33, y + hdr + 12 + 296 + 80, 20, TEXTCARD); txt(im, (x + 52, y + hdr + 12 + 296 + 87), "Alive", "Jost-SemiBold.ttf", 17, TEXTCARD, "ls")
    return (x, y, W, H)

def way_card(im, x, y, title, face, mid, end, W=408, H=112, midbadge=None):
    card = frame("panels/panel_dark", W, H, 35, 0.5); shadowed(im, card, x, y, blur=16, alpha=0.4, dy=8)
    txt(im, (x + 26, y + 31), title, "Jost-Bold.ttf", 15.5, CREAM, "ls")
    yc = y + 70; cx = x + 58
    cameo(im, cx, yc, 54, face)
    ico(im, "pointer", cx + 64, yc, 20, DIM)
    disc(im, cx + 128, yc, 50)
    if midbadge: put(im, sprite(midbadge, game_h=32), cx + 128, yc, "mm")
    else: ico(im, mid, cx + 128, yc, 28)
    ico(im, "pointer", cx + 192, yc, 20, DIM)
    disc(im, cx + 256, yc, 50); ico(im, end, cx + 256, yc, 28)
    return (x, y, W, H)

def strip_bottom(im):
    gradient(im, (0, 860, 1920, 1080), INKDEEP, 0.0, 0.82)
    roles = ("scout", "fixer", "tech", "enforcer")
    for i, r in enumerate(roles):
        x = 98 + i * 82; cameo(im, x, 992, 62, "portrait_%s_normal" % r)
        txt(im, (x, 1042), r.upper(), "Jost-SemiBold.ttf", 11.5, DIM, "ms", 0.12)
    pw = 168; pl = frame("markers/plate_dark", pw, 40, 24, 0.5); put(im, pl, 440, 972); ico(im, "p_cash", 464, 992, 17); txt(im, (482, 998), "$100,000", "Jost-Bold.ttf", 15.5, CREAM, "ls")
    # Day / Night: the light line's tab words
    txt(im, (1440, 1000), "DAY", "Jost-Medium.ttf", 16, CREAM, "ls", 0.22); ImageDraw.Draw(im, "RGBA").rectangle((P(1440), P(1009), P(1440 + tw_of("DAY", "Jost-Medium.ttf", 16, 0.22)), P(1011)), fill=TURQL + (255,))
    txt(im, (1514, 1000), "NIGHT", "Jost-Medium.ttf", 16, (247, 244, 236, 130), "ls", 0.22)
    light_line_word(im, 1856, 1004, "BEGIN")

def title(im, x=64, y=58):
    gradient(im, (0, 0, 980, 330), INKDEEP, 0.0, 0.0)
    radial(im, 300, 80, 700, 420, INKDEEP, 0.82)
    txt(im, (x, y), "THE HANDOVER", "Jost-Medium.ttf", 15, CREAM, "ls", 0.30, shadow=(0.5, 1))
    ImageDraw.Draw(im, "RGBA").rectangle((P(x), P(y + 12), P(x + 132), P(y + 14)), fill=TURQL + (255,))
    txt(im, (x, y + 62), "Vale has a file on you.", "Jost-Bold.ttf", 34, CREAM, "ls", shadow=(0.5, 1.5))
    txt(im, (x, y + 96), "Take him alive before it reaches Security.", "Jost-Regular-Tab.ttf", 19, (247, 244, 236, 235), "ls", shadow=(0.5, 1))

def world_marks(im):
    ring_ground(im, DOOR[0], DOOR[1], 27, 15, dash=True)
    ring_ground(im, CAR[0], CAR[1], 44, 24, dash=True)
    # Records Office tag over the canopy
    pl = frame("markers/plate_dark", 196, 40, 24, 0.5); put(im, pl, DOOR[0] - 70, 96); ico(im, "papers", DOOR[0] - 70 + 28, 116, 17); txt(im, (DOOR[0] - 70 + 46, 121), "Records Office", "Jost-Bold.ttf", 13.5, CREAM, "ls")
    tail = Image.new("RGBA", (P(18), P(11)), (0, 0, 0, 0)); ImageDraw.Draw(tail).polygon([(0, 0), (P(18) - 1, 0), (P(9), P(11) - 1)], fill=(12, 52, 52, 255)); put(im, tail, DOOR[0] - 4, 135, "mt")

def blank(): return Image.new("RGBA", (3840, 2160), (0, 0, 0, 0))
def layers(with_ui=True, scene=None):
    """the briefing as separate layers, so the start sequence can move them: base (scene + vignette + title scrim), title, bottom, right (clock, cards, leaders, marks)"""
    base = (scene or stage()).copy(); vignette(base, 0.42, 0.5); radial(base, 300, 80, 700, 420, INKDEEP, 0.82)
    L = {"base": base, "title": blank(), "bottom": blank(), "right": blank()}
    title(L["title"])
    if with_ui:
        strip_bottom(L["bottom"]); r = L["right"]
        clock_strip(r); vale_card(r)
        way_card(r, 1448, 640, "Front door", pt.photo("waitress", (P(200), P(220))), None, "enter", midbadge="badges/signal_guard")
        way_card(r, 1448, 768, "Pickup", pt.photo("driver", (P(200), P(220))), "car", "take")
        stroke(r, [(1448, 340), (1360, 300), (DOOR[0] + 28, DOOR[1] - 4)], 2.4)
        stroke(r, [(1448, 696), (1330, 560), (DOOR[0] + 14, DOOR[1] + 16)], 2.4, dash=(9, 8))
        stroke(r, [(1448, 824), (1350, 640), (CAR[0] + 44, CAR[1] + 14)], 2.4, dash=(9, 8))
        world_marks(r)
    return L
def flatten(L, alpha=1.0, dx=(0, 0, 0), dy=(0, 0, 0), keys=("title", "bottom", "right")):
    im = L["base"].copy()
    for k, ox, oy in zip(keys, dx, dy):
        lay = L[k]
        if alpha < 1: lay = lay.copy(); a = lay.split()[3].point(lambda v: int(v * alpha)); lay.putalpha(a)
        sh = Image.new("RGBA", lay.size, (0, 0, 0, 0)); sh.alpha_composite(lay, (0, 0)) if (ox, oy) == (0, 0) else sh.paste(lay, (P(ox), P(oy)))
        im.alpha_composite(sh)
    return im
def build(with_ui=True):
    return flatten(layers(with_ui))
if __name__ == "__main__":
    im = build(); im.convert("RGB").save(OUT + "dir-b-street.png"); im.resize((1920, 1080), Image.LANCZOS).convert("RGB").save(SC + "dir-b-1080.png"); print("ok")
