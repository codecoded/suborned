# Glyph, brief 070: the briefing as a page (Sky 2026-10-26: "a proper page with just screengrabs / closeups of what matters").
# A mock only. Every word is read from the mission's data (contracts/missions/handover/brief.json, mission.json, text.json) or marked
# DRAFT for Fable; every picture is an existing capture (out/batch/shots, briefs/041/build/0314/proof-1080) with its HUD layer
# (<name>.hud.png) masked out and in-painted, standing in for the clean build-time capture named in README.md.
# Drawing helpers and kit colours come from briefs/062/common.py (kit sprites, Jost, the 1080p-game-px grid at 2x).
import sys, json, textwrap
sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/062")
from common import *
from build_b import light_line_word

OUT = R + "briefs/070/glyph/"; SC = R + "out/scratch/glyph/070/"; os.makedirs(SC, exist_ok=True)
MISSION = json.load(open(R + "contracts/missions/handover/mission.json"))
BRIEF = json.load(open(R + "contracts/missions/handover/brief.json"))
TEXT = json.load(open(R + "contracts/missions/handover/text.json"))
BLOCK = json.load(open(R + "contracts/world/civic-block.json"))
V = BRIEF["variants"][BRIEF["pick"]]
DEST = {d["name"]: d["at"] for d in MISSION["destinations"]}
PTS = {p["id"]: p["at"] for p in MISSION["points"]}

PAGE = (228, 224, 211)          # style contract palette.cream_stone #e4e0d3 ("stone and paper")
PRINT = (247, 244, 236)         # cream #f7f4ec, the print border
BAND = (12, 52, 52)             # the kit's panel body #0c3434 (the chrome tail's body)
DRAFT = (153, 120, 77)          # brass: the DRAFT tag is a hairline-weight word, not a fill

# ------------------------------------------------------------------ stills: an existing capture, HUD masked out, cropped
PROOF = R + "briefs/041/build/0314/proof-1080/"; SHOTS = R + "out/batch/shots/"
def clean(path):
    base = np.array(Image.open(path + ".png").convert("RGB"))
    hud = np.array(Image.open(path + ".hud.png").convert("RGBA").split()[3]); m = ((hud > 8) * 255).astype("uint8")
    m = cv2.dilate(m, np.ones((7, 7), np.uint8)); out = cv2.inpaint(base, m, 9, cv2.INPAINT_TELEA)
    return Image.fromarray(out)
def still(path, crop, w, h):
    im = clean(path).crop(crop); s = max(P(w) / im.width, P(h) / im.height)
    im = im.resize((int(im.width * s + 0.5), int(im.height * s + 0.5)), Image.LANCZOS)
    cx, cy = (im.width - P(w)) // 2, (im.height - P(h)) // 2
    return im.crop((cx, cy, cx + P(w), cy + P(h))).convert("RGBA")

def print_frame(im, sp, x, y, w, h, pin=None, tag=None):
    """a still as a print on the page: cream border, chrome 1 px rim, soft shadow; pin = the place number in the kit disc"""
    b = 6; fr = Image.new("RGBA", (P(w + 2 * b), P(h + 2 * b)), PRINT + (255,))
    ImageDraw.Draw(fr).rectangle((0, 0, fr.width - 1, fr.height - 1), outline=CHROME + (255,), width=P(1))
    fr.alpha_composite(sp, (P(b), P(b))); shadowed(im, rounded(fr, P(3)), x - b, y - b, blur=10, alpha=0.35, dy=5)
    if pin: pin_disc(im, x + 20, y + 20, pin)
    if tag: stand_in(im, x + w - 8, y + h - 8, tag)
def pin_disc(im, x, y, label, px=30):
    disc(im, x, y, px); cameo_ring(im, x, y, px)
    txt(im, (x, y + 0.5), label, "Jost-Bold.ttf", 15, CREAM, "mm")
def cameo_ring(im, x, y, px):
    d = ImageDraw.Draw(im, "RGBA"); r = px / 2
    d.ellipse((P(x - r), P(y - r), P(x + r), P(y + r)), outline=CHROME + (255,), width=P(2))
def stand_in(im, x, y, s):
    """marks a still that is a stand-in for a capture Latch would take: a small plate, bottom right of the print"""
    w = tw_of(s, "Jost-Bold.ttf", 11) + 22; pl = frame("markers/plate_dark", w, 22, 24, 0.5); put(im, pl, x - w, y - 22)
    txt(im, (x - w / 2, y - 11 + 0.5), s, "Jost-Bold.ttf", 11, DIM, "mm")
def draft(im, x, y):
    txt(im, (x, y), "DRAFT", "Jost-Bold.ttf", 9.5, DRAFT, "ls", 0.18)

def wrap(im, x, y, s, width, px=17, name="Jost-Medium.ttf", fill=INK, lead=1.38):
    words = s.split(); line = ""; yy = y
    for w_ in words:
        t = (line + " " + w_).strip()
        if tw_of(t, name, px) > width and line:
            txt(im, (x, yy), line, name, px, fill, "ls"); yy += px * lead; line = w_
        else: line = t
    if line: txt(im, (x, yy), line, name, px, fill, "ls"); yy += px * lead
    return yy
def cap1(s): return s[:1].upper() + s[1:]
def label(im, x, y, s):
    txt(im, (x, y), s, "Jost-Bold.ttf", 12.5, (31, 108, 103), "ls", 0.26)

# ------------------------------------------------------------------ the plan: drawn from civic-block.json and mission.json, pins by place
def plan(im, box):
    X0, X1, Y0, Y1 = -26.0, 84.0, -54.0, -8.0
    x0, y0, x1, y1 = box; s = min((x1 - x0) / (X1 - X0), (y1 - y0) / (Y1 - Y0))
    ox = x0 + ((x1 - x0) - (X1 - X0) * s) / 2; oy = y0 + ((y1 - y0) - (Y1 - Y0) * s) / 2
    mp = lambda x, y: (ox + (x - X0) * s, oy + (y - Y0) * s)
    Q = 3; w, h = P(x1 - x0), P(y1 - y0); big = Image.new("RGBA", (w * Q, h * Q), (0, 0, 0, 0)); d = ImageDraw.Draw(big)
    def poly(pts, fill, outline=None, wd=0):
        pp = [((mp(*p)[0] - x0) * kd.S * Q, (mp(*p)[1] - y0) * kd.S * Q) for p in pts]
        d.polygon(pp, fill=fill)
        if outline: d.line(pp + [pp[0]], fill=outline, width=max(1, int(wd * kd.S * Q)))
    rect = lambda b: [(b[0], b[1]), (b[2], b[1]), (b[2], b[3]), (b[0], b[3])]
    d.rounded_rectangle((0, 0, w * Q - 1, h * Q - 1), radius=P(4) * Q, fill=(248, 238, 219, 255))       # world role pavement #f9efdc
    asphalt = (206, 210, 216, 255)
    poly(rect((13, -53.5, 84, -47.2)), asphalt); poly(rect((79.6, -47.2, 84, -8)), asphalt)
    for k in ("bays", "drives"):
        for o in BLOCK[k]: poly(o["polygon"], asphalt)
    for o in BLOCK["objects"]:
        col = {"fountain": (114, 181, 167, 255), "planter": (170, 200, 170, 255)}.get(o["kind"])
        if col: poly(o["polygon"], col)
    for o in BLOCK["buildings"]: poly(o["polygon"], (255, 252, 244, 255), DEEPTEAL + (255,), 1.6)
    big = big.resize((w, h), Image.LANCZOS); put(im, big, x0, y0)
    hairline(im, x0, y0, x1, y0, BRASS, 1); hairline(im, x0, y1, x1, y1, BRASS, 1)
    txt(im, mp(-17, -24), "Café", "Jost-Bold.ttf", 11.5, (31, 108, 103), "mm")
    txt(im, mp(64, -31.5), "Records", "Jost-Bold.ttf", 11.5, (31, 108, 103), "mm"); txt(im, mp(64, -27.5), "Office", "Jost-Bold.ttf", 11.5, (31, 108, 103), "mm")
    pins = [("1", DEST["Café door"]), ("2", DEST["Office entrance"]), ("3", DEST["Meet driver"]), ("4", PTS["service-bay"])]
    for n, at in pins: pin_disc(im, *mp(*at), n, px=24)
    gx, gy = mp(*DEST["Getaway bay"]); pin_disc(im, gx, gy, "", px=24); ico(im, "car", gx, gy, 13)
    return mp

# ------------------------------------------------------------------ the page
def build():
    im = Image.new("RGBA", (3840, 2160), PAGE + (255,))
    rng = np.random.default_rng(7); a = np.array(im).astype(float); a[..., :3] += rng.normal(0, 1.6, (2160, 3840))[..., None]
    im = Image.fromarray(a.clip(0, 255).astype("uint8")).convert("RGBA")
    d = ImageDraw.Draw(im, "RGBA"); d.rectangle((0, 0, P(1920), P(6)), fill=SEAFOAM + (255,)); d.rectangle((0, P(6), P(1920), P(7)), fill=BRASS + (255,))

    # header: title, hook, objective (brief.json title, variants[pick].hook, .dossier.objective); place and time (brief.place, brief.time)
    txt(im, (56, 62), BRIEF["title"].upper(), "Jost-Medium.ttf", 15, INK, "ls", 0.30)
    txt(im, (56, 118), V["hook"], "Jost-Bold.ttf", 46, INK, "ls")
    txt(im, (56, 154), V["dossier"]["objective"], "Jost-Medium.ttf", 22, TEXTCARD, "ls")
    txt(im, (1864, 62), BRIEF["place"] + "  ·  " + BRIEF["time"], "Jost-Medium.ttf", 15, TEXTCARD, "rs", 0.04)
    hairline(im, 56, 180, 1864, 180, BRASS, 1)

    # left: the target (portraits/card_<target.role>, roles[target].name, dossier.target)
    label(im, 56, 214, "TARGET")
    ph = load(K + "portraits/card_councillor@2x.png"); ph = ph.resize((P(392), int(P(392) * ph.height / ph.width)), Image.LANCZOS)
    print_frame(im, ph, 60, 228, 392, ph.height / kd.S)
    ty = 228 + ph.height / kd.S + 44
    txt(im, (56, ty), "Councillor Vale", "Jost-Bold.ttf", 30, INK, "ls")
    txt(im, (56, ty + 30), "Holds the file on you.", "Jost-Medium.ttf", 18, TEXTCARD, "ls")
    txt(im, (56, ty + 56), "Comes out at 5:50 and leaves by car.", "Jost-Medium.ttf", 18, TEXTCARD, "ls"); draft(im, 370, ty + 56)
    # left: where (the plan, pins numbered as on the stills)
    label(im, 56, 694, "WHERE")
    plan(im, (56, 706, 456, 846))
    ly = 874
    leg = [("1", "Café"), ("2", "Public door"), ("3", "Official bay"), ("4", "Service bay")]
    for i, (n, s) in enumerate(leg):
        x = 56 + (i % 2) * 200; y = ly + (i // 2) * 26
        pin_disc(im, x + 11, y - 5, n, px=22); txt(im, (x + 30, y), s, "Jost-Bold.ttf", 14, INK, "ls")
    x = 56; y = ly + 52; pin_disc(im, x + 11, y - 5, "", px=22); ico(im, "car", x + 11, y - 5, 12); txt(im, (x + 30, y), "Getaway car · you start here", "Jost-Bold.ttf", 14, INK, "ls")

    # middle: two ways in, each two stills with their place number, then one shared ending
    X = 500; label(im, X, 214, "TWO WAYS IN")
    SW, SH = 360, 226
    rows = [
        dict(y=232, key="front-door", title="Front door", when="Before 5:50",
             text=[(cap1(TEXT["briefing"]["hints"]["front-door"].split(": ", 1)[1]), False),
                   ("Turn her, and her call takes him off the door for a few minutes. A camera watches the door too.", True)],
             stills=[(PROOF + "r067-convo-inez-1", (190, 350, 780, 720), "1", "Inez, café waitress", None),
                     (PROOF + "levels-guard-landing", (760, 290, 1360, 667), "2", "The door guard", "stand-in · brief-guard")]),
        dict(y=532, key="pickup", title="Pickup", when="From 6:10",
             text=[(cap1(TEXT["briefing"]["hints"]["pickup"].split(": ", 1)[1]), False),
                   ("Turn him before 5:40 and he drives Vale round to the back, and waits.", True)],
             stills=[(PROOF + "r067-official-car", (600, 150, 1360, 640), "3", "The driver, at Vale's car", None),
                     (PROOF + "r059-625-parked", (620, 280, 1240, 680), "4", "Vale's car at the service bay", "stand-in · brief-service-bay")]),
    ]
    for r in rows:
        y = r["y"]
        txt(im, (X, y + 28), r["title"], "Jost-Bold.ttf", 26, INK, "ls")
        pl_w = tw_of(r["when"], "Jost-Bold.ttf", 13.5) + 40; pl = frame("markers/plate_dark", pl_w, 26, 24, 0.5); put(im, pl, X, y + 42)
        ico(im, "p_time", X + 15, y + 55, 13); txt(im, (X + 27, y + 59.5), r["when"], "Jost-Bold.ttf", 13.5, CREAM, "ls")
        yy = y + 106
        for s, is_draft in r["text"]:
            if is_draft: draft(im, X, yy - 4); yy += 12
            yy = wrap(im, X, yy, s, 250, 17) + 8
        for i, (src, crop, pin, cap, tag) in enumerate(r["stills"]):
            sx = 790 + i * (SW + 44)
            print_frame(im, still(src, crop, SW, SH), sx, y + 8, SW, SH, pin=pin, tag=tag)
            txt(im, (sx, y + SH + 36), cap, "Jost-Bold.ttf", 15, INK, "ls")
            if i == 0: ico(im, "pointer", sx + SW + 22, y + 8 + SH / 2, 18, (31, 108, 103))
    # the shared ending: both ways end in the getaway car
    EX = 1590; EW, EH = 274, 470
    ico(im, "pointer", EX - 22, 232 + 8 + SH / 2, 18, (31, 108, 103)); ico(im, "pointer", EX - 22, 532 + 8 + SH / 2, 18, (31, 108, 103))
    print_frame(im, still(PROOF + "r29-squarebay-play", (800, 160, 1130, 700), EW, EH), EX, 240, EW, EH, tag="stand-in · brief-getaway")
    pin_disc(im, EX + 20, 260, "", 30); ico(im, "car", EX + 20, 260, 15)
    txt(im, (EX, 240 + EH + 34), "The getaway car", "Jost-Bold.ttf", 15, INK, "ls")
    wrap(im, EX, 240 + EH + 58, TEXT["briefing"]["win"], EW, 15, "Jost-Medium.ttf", TEXTCARD)

    # the clock in words (mission.json timeline at; text.json briefing.timeline label)
    label(im, X, 828, "THE CLOCK")
    marks = [("0:00", "You start by the getaway car", True)] + [(t["at"], t["label"], False) for t in TEXT["briefing"]["timeline"] if t["at"] in ("2:35", "5:30", "5:40", "5:50", "6:10", "8:30")]
    # stations at even spacing, like a line map: the times are printed, so the spacing need not be to scale
    tx0, tx1 = X + 6, 1858; n = len(marks); step = (tx1 - tx0) / n; ly_ = 852
    d = ImageDraw.Draw(im, "RGBA"); d.rectangle((P(tx0), P(ly_ - 1), P(tx0 + step * (n - 1) + 6), P(ly_ + 1)), fill=INK + (255,))
    for i, (at, lab, dr) in enumerate(marks):
        x = tx0 + i * step
        lit = at in ("5:50", "8:30")
        d.ellipse((P(x - 6), P(ly_ - 6), P(x + 6), P(ly_ + 6)), fill=(INK if lit else PAGE) + (255,), outline=INK + (255,), width=P(2))
        txt(im, (x - 6, ly_ + 38), at, "Jost-Medium-Tab.ttf", 20, INK, "ls")
        wrap(im, x - 6, ly_ + 60, lab, step - 22, 13.5, "Jost-Medium.ttf", TEXTCARD, 1.3)
        if dr: draft(im, x + 44, ly_ + 38)

    # bottom band: the cell (portrait + one line for this mission), purse, Heat, Day / Night, Begin
    d.rectangle((0, P(966), P(1920), P(1080)), fill=BAND + (255,)); d.rectangle((0, P(966), P(1920), P(967)), fill=BRASS + (255,))
    cell = [("scout", "Scout", "Listens to Inez and the driver. Can send the rivals off."),
            ("fixer", "Fixer", "Turns Inez or the driver, with cash or help."),
            ("tech", "Tech", "Loops the door camera. Moves the yard patrol."),
            ("enforcer", "Enforcer", "Goes in for Vale. Only he can carry him.")]
    for i, (r, name, line) in enumerate(cell):
        x = 56 + i * 290
        cameo(im, x + 30, 1023, 60, "portrait_%s_normal" % r)
        txt(im, (x + 72, 1006), name.upper(), "Jost-Bold.ttf", 12.5, CREAM, "ls", 0.16)
        wrap(im, x + 72, 1027, line, 196, 13.5, "Jost-Medium.ttf", DIM, 1.3)
    draft(im, 56 + 3 * 290 + 72 + tw_of("ENFORCER", "Jost-Bold.ttf", 12.5, 0.16) + 10, 1006)
    pw = 168; pl = frame("markers/plate_dark", pw, 36, 24, 0.5); put(im, pl, 1250, 990)
    ico(im, "p_cash", 1272, 1008, 16); txt(im, (1290, 1013.5), "$100,000", "Jost-Bold.ttf", 15.5, CREAM, "ls")
    txt(im, (1250, 1052), "HEAT", "Jost-Bold.ttf", 11, DIM, "ls", 0.2); txt(im, (1250 + tw_of("HEAT", "Jost-Bold.ttf", 11, 0.2) + 8, 1052), TEXT["briefing"]["heat"]["unknown"], "Jost-Bold.ttf", 14, CREAM, "ls")
    txt(im, (1490, 1030), "DAY", "Jost-Medium.ttf", 16, CREAM, "ls", 0.22); d.rectangle((P(1490), P(1039), P(1490 + tw_of("DAY", "Jost-Medium.ttf", 16, 0.22)), P(1041)), fill=TURQL + (255,))
    txt(im, (1562, 1030), "NIGHT", "Jost-Medium.ttf", 16, (247, 244, 236, 130), "ls", 0.22)
    light_line_word(im, 1862, 1034, "BEGIN")
    return im

if __name__ == "__main__":
    im = build().convert("RGB")
    im.save(OUT + "briefing-page-mock@2x.png")
    one = im.resize((1920, 1080), Image.LANCZOS); one.save(OUT + "briefing-page-mock.png")
    # the 2x crop check: four 960 x 540 corners of the 3840 x 2160 page, at full 2x
    crops = [(0, 0), (1920, 0), (0, 1080), (1920, 1080)]
    sheet = Image.new("RGB", (3840, 2160 + 60), (11, 22, 21))
    for (x, y) in crops: sheet.paste(im.crop((x, y, x + 1920, y + 1080)), (x, y + (60 if y else 0)))
    sheet.save(SC + "check-2x-quads.png")
    # before / after
    before = Image.open(SHOTS + "briefing-day.png").convert("RGB")
    ba = Image.new("RGB", (3840 + 40, 1080 + 90), (11, 22, 21)); ba.paste(before, (0, 90)); ba.paste(one, (1960, 90))
    dd = ImageDraw.Draw(ba); f = ImageFont.truetype(K + "fonts/Jost-Bold.ttf", 40)
    dd.text((24, 24), "Today: cards over the street (build 0.2.128)", font=f, fill=CREAM); dd.text((1984, 24), "Proposed: the briefing as a page", font=f, fill=CREAM)
    ba.save(OUT + "before-after.png"); print("ok")
