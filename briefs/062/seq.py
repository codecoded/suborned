# Glyph, brief 062: boards of the pick (B): the start sequence, the directions, today's pieces, and the 2x crops.
import sys, json; sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/062")
from common import *
import portraits as pt, build_b as bb, build_a, build_c
FT = R + "testbed/hud-style/fonts/Jost-Medium-Tab.ttf"; BG = (23, 48, 47)
def cf(n): return ImageFont.truetype(FT, n)
def sheet(panels, cols, name, title, sub="", pad=30, capH=84, head=104, w_each=None):
    rows = [panels[k:k + cols] for k in range(0, len(panels), cols)]
    rh = [max(p[0].height for p in r) + capH + pad for r in rows]; rw = [sum(p[0].width + pad for p in r) for r in rows]
    tw = int(max(cf(36).getlength(title), cf(21).getlength(sub))) + 2 * pad
    im = Image.new("RGB", (max(pad + max(rw), tw), head + sum(rh)), BG); d = ImageDraw.Draw(im)
    d.text((pad, 22), title, font=cf(36), fill=CREAM); d.text((pad, 68), sub, font=cf(21), fill=DIM)
    y = head
    for r, h in zip(rows, rh):
        x = pad
        for pic, cap, subc in r:
            d.text((x, y), cap, font=cf(27), fill=CREAM); d.text((x, y + 36), subc, font=cf(19), fill=DIM)
            im.paste(pic.convert("RGB"), (x, y + capH)); x += pic.width + pad
        y += h
    im.save(OUT + name); print(name, im.size)
def th(im, w): return im.convert("RGB").resize((w, int(im.height * w / im.width)), Image.LANCZOS)

# ---------------------------------------------------------------- world frames without HUD
def world_start():
    base = Image.open(R + "out/batch/shots/start-overhead-day.png").convert("RGBA"); hud = Image.open(R + "out/batch/shots/start-overhead-day.hud.png").convert("RGBA").split()[3]
    m = (np.array(hud) > 8).astype("uint8") * 255; m = cv2.dilate(m, np.ones((7, 7), np.uint8))
    a = np.array(base.convert("RGB")); f = cv2.inpaint(a, m, 6, cv2.INPAINT_TELEA); return Image.fromarray(f).convert("RGBA").resize((3840, 2160), Image.BICUBIC), Image.open(R + "out/batch/shots/start-overhead-day.hud.png").convert("RGBA")
def mblur(im, length, angle):
    a = np.array(im.convert("RGB")); k = np.zeros((length, length), np.float32); k[length // 2, :] = 1; M = cv2.getRotationMatrix2D((length / 2 - 0.5, length / 2 - 0.5), angle, 1.0)
    k = cv2.warpAffine(k, M, (length, length)); k /= k.sum(); return Image.fromarray(cv2.filter2D(a, -1, k)).convert("RGBA")

def sequence():
    L = bb.layers(); f1 = bb.flatten(L)
    f2 = bb.flatten(L, 0.45, dx=(0, 0, 70), dy=(-34, 44, 0))
    b0 = L["base"].copy()
    ws, hud = world_start()
    ws_v = ws.copy(); vignette(ws_v, 0.42, 0.5)
    # frame 3: the Office frame and the cell frame set along the glide with a soft seam: indicative, not a real in-between
    Wd, Hd = 3840, 2160; Z = 1.22; cell = ws_v.copy()
    canvas = cell.resize((int(Wd * Z), int(Hd * Z)), Image.LANCZOS).crop((int(Wd * Z) - Wd, 0, int(Wd * Z), Hd))    # the plaza seen from further up the street, still gliding in
    mid = mblur(canvas, 61, 215); vignette(mid, 0.45, 0.4)
    f3 = mid
    # arrows along the glide: two chevrons in the world's cream-over-ink language
    d3 = f3.copy(); stroke(d3, [(1180, 120), (1060, 260), (940, 400)], 2.6)
    f4 = ws.copy(); vignette(f4, 0.0, 0.5)
    for (x, y) in ((848, 575), (800, 628), (893, 636), (822, 704)): ring_ground(f4, x, y, 24, 14, dash=False, w=2.2)
    # the roster and the clock arrive: the roster first
    pass
    f5 = Image.open(R + "out/batch/shots/start-overhead-day.png").convert("RGBA").resize((3840, 2160), Image.BICUBIC)
    frames = [(f1, "1  The briefing", "pictures and one line; Begin is lit; the world is the real street"),
              (f2, "2  Begin", "the board lifts away (title up, cards right, strip down) in 0.3 s; the clock has not started"),
              (d3, "3  The camera flies", "one eased glide west along the street, about 3 s; any key skips it. Indicative frame"),
              (f4, "4  It settles on the cell", "the four ground rings pulse once; the roster is about to slide in"),
              (f5, "5  Play", "the clock starts at the settle (0:00); the goal line is up")]
    pan = [(th(f, 704), c, s) for f, c, s in frames]
    # timing strip
    W = 704 * 3 + 60; T = Image.new("RGB", (704, 396), BG); d = ImageDraw.Draw(T)
    d.text((10, 10), "Timing (seconds after Begin)", font=cf(26), fill=CREAM)
    rows = [("0.0", "Begin pressed: the word's line fills, a soft chime (Chime)"), ("0.0 - 0.3", "title up, cards right, strip down; the scene stays lit"),
            ("0.3 - 3.3", "the camera glides from the Office to the cell, eased in and out"), ("3.3", "settle: rings pulse, the roster slides in (0.25 s)"),
            ("3.6", "the clock bar fades in and the clock starts at 0:00"), ("any time", "a key or click lands the camera now; the sequence still ends the same")]
    for i, (a, b) in enumerate(rows):
        d.text((10, 62 + i * 52), a, font=cf(21), fill=TURQL); d.text((120, 62 + i * 52), b, font=cf(19), fill=CREAM)
    pan.append((T, "Timing", "numbers are starting values, tuned by playing"))
    sheet(pan, 3, "board-03-start-sequence.png", "062 - the game start, Begin to the first second", "Pick B. Frames 1, 2, 4 are composed from the real Records Office frame and the real start frame (HUD taken out); frame 3 is the start frame a moment before it settles, indicative.", capH=84)

def directions():
    a = build_a.build(); b = bb.build(); c = build_c.build()
    sheet([(th(a, 1100), "A  The file", "a paper dossier on a blotter: photograph, plan, two ways, the dial. Most to read; the map is flat"),
           (th(b, 1100), "B  On the street  (pick)", "the real Records Office; the file card, the two ways and the clock anchored to it; Begin, then the camera flies"),
           (th(c, 1100), "C  Lobby cards", "three framed pictures from the game over the key art; the dial beside. Handsome; the pictures are crops, not the place")],
          1, "board-02-directions.png", "062 - the briefing, three directions", "Kit frames and colours only (panel_card, panel_dark, plate_dark, cameos, light line). Faces: renders of the game's own figures as file photographs (stand-ins, see the brief).", capH=84)

def today():
    names = [("out/batch/shots/briefing-day.png", "The in-game briefing card", "one small plate on the live scene: title, one line, four faces, three stats, Day / Night, Begin"),
             ("briefs/051/build/0299/1920x1080/brief.png", "The mission brief (New game)", "a wall of text on the key art: hook, three lines, four fields, four bullets, a sign-off"),
             ("briefs/051/build/0299/1920x1080/menu.png", "The menu (restyle A)", "kept"), ("briefs/051/build/0299/1920x1080/loading.png", "The loading screen", "kept"),
             ("out/batch/shots/start-overhead-day.png", "The moment play begins", "the clock is already running at 0:05; the camera is where it was; nothing says where you are or why")]
    pan = [(th(Image.open(R + p).convert("RGB"), 880), c, s) for p, c, s in names]
    sheet(pan, 2, "board-01-today.png", "062 - today's four pieces (0.2.99)", "From the latest batch and the 0.2.99 front-end captures.", capH=84)

def crops():
    im = bb.build(); big = im
    def cr(box, w=1200): c = big.crop(tuple(P(v) for v in box)); return c.convert("RGB").resize((w, int(c.height * w / c.width)), Image.LANCZOS) if c.width > w else c.convert("RGB")
    pans = [(cr((1436, 120, 1868, 620)), "The file card", "kit panel_card; the teal header, the photograph, name and one fact; native 2 px per game px"),
            (cr((1436, 630, 1868, 890)), "The two ways", "kit panel_dark; a face, the guard's badge, the door; a face, the car, the take"),
            (cr((1288, 36, 1868, 130)), "The clock strip", "kit plate_dark; the sweep to 5:30 in turquoise-light; no sentence"),
            (cr((40, 940, 1900, 1070), 1800), "The bottom strip", "faces and roles, purse, Day / Night, Begin with its marker and line (light line, as the menu)"),
            (cr((40, 30, 700, 190), 1320 if False else 1000), "The title", "two short lines")]
    sheet(pans, 2, "board-04-pick-crops.png", "062 pick B - 2x crops", "Native 2 px per game px (3840 x 2160), cropped for clipping, centring and padding.", capH=84)

def flow():
    a = [("out/batch/shots/briefing-day.png", "Today"), ]
if __name__ == "__main__":
    for w in sys.argv[1:] or ["directions", "today", "sequence", "crops"]: globals()[w]()
