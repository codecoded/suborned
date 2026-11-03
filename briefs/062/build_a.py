# Glyph, brief 062: direction A "The file": a paper dossier on a dark blotter; records are physical in this world (stamped forms, photographs, plans).
import sys, json; sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/062")
from common import *
import portraits as pt, build_b as bb
BLOCK = json.load(open(R + "contracts/world/civic-block.json"))
MAPBOX = (770, 150, 1800, 590)               # game px
X0, X1, Y0, Y1 = -27.0, 84.0, -53.0, -9.0     # block metres shown
def mp(x, y):
    sx = (MAPBOX[2] - MAPBOX[0]) / (X1 - X0); sy = (MAPBOX[3] - MAPBOX[1]) / (Y1 - Y0); s = min(sx, sy)
    ox = MAPBOX[0] + ((MAPBOX[2] - MAPBOX[0]) - (X1 - X0) * s) / 2; oy = MAPBOX[1] + ((MAPBOX[3] - MAPBOX[1]) - (Y1 - Y0) * s) / 2
    return ox + (x - X0) * s, oy + (y - Y0) * s
def blotter():
    im = Image.new("RGBA", (3840, 2160), INK + (255,)); rng = np.random.default_rng(2)
    n = rng.normal(0, 3.2, (2160, 3840)); a = np.array(im).astype(float); a[..., :3] += n[..., None]; im = Image.fromarray(a.clip(0, 255).astype("uint8")).convert("RGBA")
    radial(im, 960, 520, 1500, 900, (36, 68, 66), 0.9); vignette(im, 0.55, 0.45); return im
def paper(im, box, rgb=IVORY, r=14):
    x0, y0, x1, y1 = box; w, h = P(x1 - x0), P(y1 - y0)
    sp = Image.new("RGBA", (w, h), (0, 0, 0, 0)); d = ImageDraw.Draw(sp); d.rounded_rectangle((0, 0, w - 1, h - 1), radius=P(r), fill=rgb + (255,))
    n = np.random.default_rng(4).normal(0, 2.0, (h, w)); a = np.array(sp).astype(float); a[..., :3] += n[..., None]; sp = Image.fromarray(a.clip(0, 255).astype("uint8"), "RGBA")
    shadowed(im, sp, x0, y0, blur=26, alpha=0.55, dy=16)
def draw_map(im):
    x0, y0, x1, y1 = MAPBOX; w, h = P(x1 - x0), P(y1 - y0)
    m = Image.new("RGBA", (w, h), (0, 0, 0, 0)); Q = 2; big = Image.new("RGBA", (w * Q, h * Q), (0, 0, 0, 0)); d = ImageDraw.Draw(big)
    def poly(pts, fill, outline=None, wd=0):
        pp = [((mp(x, y)[0] - x0) * kd.S * Q, (mp(x, y)[1] - y0) * kd.S * Q) for x, y in pts]
        d.polygon(pp, fill=fill, outline=outline)
        if outline and wd: d.line(pp + [pp[0]], fill=outline, width=int(wd * kd.S * Q))
    def rect(b): return [(b[0], b[1]), (b[2], b[1]), (b[2], b[3]), (b[0], b[3])]
    poly(rect((X0, Y0, X1, Y1)), (248, 238, 214, 255))                        # cream paving
    # the avenue and the bays, in the soft lilac-grey asphalt
    poly(rect((13, -52.5, 82, -47)), (206, 214, 226, 255)); poly(rect((77, -44, 84, -9)), (206, 214, 226, 255))
    for k in ("bays", "drives"):
        for o in BLOCK[k]: poly(o["polygon"], (206, 214, 226, 255))
    # seafoam inlay bands: a ring about the fountain and the way to the Office
    cx, cy = mp(6.5, -30)
    sc_ = min((MAPBOX[2] - MAPBOX[0]) / (X1 - X0), (MAPBOX[3] - MAPBOX[1]) / (Y1 - Y0)); rr_ = 12 * sc_ * kd.S * Q; ccx, ccy = (cx - x0) * kd.S * Q, (cy - y0) * kd.S * Q
    d.ellipse((ccx - rr_, ccy - rr_, ccx + rr_, ccy + rr_), outline=SEAFOAM + (255,), width=int(4 * kd.S * Q))
    poly(rect((18, -31.2, 55, -28.8)), SEAFOAM + (255,))
    for o in BLOCK["objects"]:
        k = o["kind"]; col = {"fountain": (96, 150, 190, 255), "planter": (150, 196, 150, 255), "table": (230, 120, 80, 255)}.get(k, (200, 190, 170, 255))
        poly(o["polygon"], col)
    for o in BLOCK["structures"]:
        poly(o["polygon"], (214, 206, 188, 255), (150, 140, 120, 255), 0.6)
    for o in BLOCK["buildings"]:
        poly(o["polygon"], (236, 228, 208, 255), DEEPTEAL + (255,), 2.2)
        pp = o["polygon"]; poly([(p[0] + 1.2, p[1] + 1.2) for p in pp], (252, 246, 232, 255))
    m = big.resize((w, h), Image.LANCZOS); put(im, m, x0, y0)
def build():
    im = blotter()
    paper(im, (84, 70, 1836, 892), IVORY)
    hairline(im, 760, 98, 760, 864, (190, 180, 160), 1.5)          # the fold
    hairline(im, 112, 100, 1808, 100, BRASS, 1.0); hairline(im, 112, 862, 1808, 862, BRASS, 1.0)
    # left page: the photograph, the stamp, the name
    ph = rounded(pt.photo("councillor", (P(360), P(470)), silver=True), P(6)); frm = Image.new("RGBA", (ph.width + P(20), ph.height + P(20)), (252, 248, 238, 255))
    frm = rounded(frm, P(6)); shadowed(im, frm, 160, 150, blur=14, alpha=0.4, dy=8); put(im, ph, 170, 160)
    clip = Image.new("RGBA", (P(34), P(86)), (0, 0, 0, 0)); cd = ImageDraw.Draw(clip); cd.rounded_rectangle((P(3), P(2), P(31), P(84)), radius=P(13), outline=CHROME + (255,), width=P(4)); cd.rounded_rectangle((P(9), P(16), P(25), P(70)), radius=P(8), outline=(150, 146, 144, 255), width=P(3))
    put(im, clip, 318, 118)
    stamp = Image.new("RGBA", (P(190), P(60)), (0, 0, 0, 0)); sd = ImageDraw.Draw(stamp); sd.rounded_rectangle((0, 0, P(190) - 1, P(60) - 1), radius=P(8), fill=DEEPTEAL + (255,), outline=CREAM + (255,), width=P(2.5))
    sd.text((P(95), P(31)), "TARGET", font=jost("Jost-Bold.ttf", 28), fill=CREAM + (255,), anchor="mm"); stamp = stamp.rotate(7, expand=True, resample=Image.BICUBIC); put(im, stamp, 450, 520)
    txt(im, (164, 722), "Councillor Vale", "Jost-Bold.ttf", 40, INK, "ls")
    ico(im, "take", 178, 764, 24, TEXTCARD); txt(im, (202, 773), "Alive", "Jost-SemiBold.ttf", 22, TEXTCARD, "ls")
    ico(im, "papers", 330, 764, 24, TEXTCARD); txt(im, (354, 773), "Records Office", "Jost-SemiBold.ttf", 22, TEXTCARD, "ls")
    # right page: the plan with the two ways
    draw_map(im)
    hairline(im, MAPBOX[0], MAPBOX[1], MAPBOX[2], MAPBOX[1], BRASS, 1); hairline(im, MAPBOX[0], MAPBOX[3], MAPBOX[2], MAPBOX[3], BRASS, 1)
    crew = mp(0.5, -46); cafe = mp(-10.3, -29); door = mp(64, -41.25); car = mp(78, -31)
    stroke(im, [crew, mp(-2, -38), cafe], 2.6, dash=(8, 7))                       # the Scout to Inez
    stroke(im, [cafe, mp(30, -30), mp(52, -34), door], 2.6, dash=(8, 7))          # the way in, front door
    stroke(im, [crew, mp(30, -49.5), mp(70, -49.5), mp(79, -44), car], 2.6, dash=(8, 7))   # the pickup
    ring_ground(im, door[0], door[1], 14, 9, dash=True); ring_ground(im, car[0], car[1], 15, 10, dash=True)
    cameo(im, cafe[0], cafe[1] - 22, 38, pt.file_photo(pt.B + "waitress.png", (300, 300), (160, 190, 720, 750)))
    cameo(im, car[0] - 6, car[1] - 36, 38, pt.file_photo(pt.B + "driver.png", (300, 300), (160, 190, 720, 750)))
    for i, r in enumerate(("scout", "fixer", "tech", "enforcer")): cameo(im, crew[0] - 40 + i * 24, crew[1] + (i % 2) * 6, 30, "portrait_%s_normal" % r)
    txt(im, mp(-23.5, -17.5), "Café", "Jost-SemiBold.ttf", 14, (31, 108, 103), "ls"); txt(im, mp(56, -17.5), "Records Office", "Jost-SemiBold.ttf", 14, (31, 108, 103), "ls")
    ico(im, "enter", door[0], door[1] - 28, 20, INK)
    # the two ways under the plan, as chips on paper
    for i, (title, face, mid, end, badge) in enumerate((("Front door", "waitress", None, "enter", "badges/signal_guard"), ("Pickup", "driver", "car", "take", None))):
        x = 790 + i * 408; y = 618
        pl = frame("panels/panel_dark", 396, 106, 35, 0.5); shadowed(im, pl, x, y, blur=12, alpha=0.4, dy=6)
        txt(im, (x + 26, y + 30), title, "Jost-Bold.ttf", 15.5, CREAM, "ls")
        yc = y + 68; cx = x + 56
        cameo(im, cx, yc, 52, pt.file_photo(pt.B + face + ".png", (300, 300), (160, 190, 720, 750)))
        ico(im, "pointer", cx + 56, yc, 20, DIM); disc(im, cx + 112, yc, 48)
        if badge: put(im, sprite(badge, game_h=30), cx + 112, yc, "mm")
        else: ico(im, mid, cx + 112, yc, 27)
        ico(im, "pointer", cx + 168, yc, 20, DIM); disc(im, cx + 224, yc, 48); ico(im, end, cx + 224, yc, 27)
    # the clock to the handover: a dial stamped on the plan page
    dial_x, dial_y, rr = 1700, 690, 76
    d = ImageDraw.Draw(im, "RGBA"); Q = 1
    def arcp(a): return (P(dial_x + rr * math.sin(math.radians(a))), P(dial_y - rr * math.cos(math.radians(a))))
    d.ellipse((P(dial_x - rr), P(dial_y - rr), P(dial_x + rr), P(dial_y + rr)), fill=CREAM + (255,), outline=DEEPTEAL + (255,), width=P(4))
    ang = 360 * 330 / 510.0
    d.pieslice((P(dial_x - rr + 8), P(dial_y - rr + 8), P(dial_x + rr - 8), P(dial_y + rr - 8)), -90, -90 + ang, fill=(176, 226, 226, 255))
    for k in range(17):
        a = 360 * k / 17; r0 = rr - 8 if k % 2 else rr - 13
        d.line((*[P(v) for v in (dial_x + r0 * math.sin(math.radians(a)), dial_y - r0 * math.cos(math.radians(a)), dial_x + (rr - 4) * math.sin(math.radians(a)), dial_y - (rr - 4) * math.cos(math.radians(a)))],), fill=INK + (255,), width=P(1.4))
    ha = arcp(ang); d.line((P(dial_x), P(dial_y), *ha), fill=INK + (255,), width=P(3)); d.ellipse((P(dial_x - 5), P(dial_y - 5), P(dial_x + 5), P(dial_y + 5)), fill=INK + (255,))
    txt(im, (dial_x, dial_y + 40), "5:30", "Jost-Bold.ttf", 22, INK, "ms", 0.0)
    ico(im, "enter", dial_x, dial_y - 36, 22, DEEPTEAL)
    txt(im, (dial_x - rr - 14, dial_y + rr + 22), "0:00", "Jost-Bold.ttf", 15, TEXTCARD, "ls"); txt(im, (dial_x + rr + 14, dial_y + rr + 22), "8:30", "Jost-Bold.ttf", 15, TEXTCARD, "rs")
    # title, bottom bar
    txt(im, (112, 126), "THE HANDOVER", "Jost-Medium.ttf", 15, INK, "ls", 0.30)
    txt(im, (790, 130), "Vale has a file on you.", "Jost-Bold.ttf", 24, INK, "ls")
    gradient(im, (0, 930, 1920, 1080), INKDEEP, 0.0, 0.6)
    bb.strip_bottom(im)
    return im
if __name__ == "__main__":
    im = build(); im.convert("RGB").save(OUT + "dir-a-file.png"); im.resize((1920, 1080), Image.LANCZOS).convert("RGB").save(SC + "dir-a-1080.png"); print("ok")
