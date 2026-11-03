# Glyph, brief 063 / 062 (2026-10-25): the 2x crop board of the new portraits at their real sizes (canvas 2 px per game px, a Retina scale-2 screen),
# beside the roster's cameos and the file-card mockup; zoomed 4x strip below. python3 -I briefs/063/portrait_board.py -> briefs/063/board-07-vale-portraits.png
import sys; sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/062")
from common import *
W, H = 1180, 560
im = Image.new("RGBA", (P(W), P(H)), (29, 58, 58, 255))
def kitimg(n): return load(K + "portraits/%s@2x.png" % n).convert("RGBA")
# 1 the file card (408 x 478, as 062 §4)
x, y, CW, CH = 24, 20, 408, 478
card = frame("panels/panel_card", CW, CH, 24, 0.5); put(im, card, x, y)
txt(im, (x + 24, y + 21), "TARGET", "Jost-Bold.ttf", 11.5, CREAM, "ls", 0.12)
ph = kitimg("card_councillor"); ph = ph.resize((P(360), P(296)), Image.LANCZOS); hdr = 30
rim = Image.new("RGBA", (ph.width + P(6), ph.height + P(6)), (0, 0, 0, 0)); ImageDraw.Draw(rim).rounded_rectangle((0, 0, rim.width - 1, rim.height - 1), radius=P(13), fill=INKDEEP + (255,))
put(im, rim, x + 22 - 3, y + hdr + 12 - 3); put(im, ph, x + 22, y + hdr + 12)
hairline(im, x + 22, y + hdr + 12 + 296 + 18, x + CW - 22, y + hdr + 12 + 296 + 18)
txt(im, (x + 24, y + hdr + 12 + 296 + 52), "Councillor Vale", "Jost-Bold.ttf", 28, INK, "ls")
ico(im, "take", x + 33, y + hdr + 12 + 296 + 80, 20, TEXTCARD); txt(im, (x + 52, y + hdr + 12 + 296 + 87), "Alive", "Jost-SemiBold.ttf", 17, TEXTCARD, "ls")
# 2 Intel cards (36 px face as c.Badge, Cameo inner 30), the old badge beside for the swap
ix = 470
txt(im, (ix, 34), "INTEL (36 px)", "Jost-Bold.ttf", 11, DIM, "ls", 0.1)
for i, (n, name) in enumerate((("face_councillor", "Councillor Vale"), ("face_waitress", "Inez"), ("face_driver", "The driver"))):
    cy = 62 + i * 52; pl = frame("markers/plate_dark", 300, 46, 24, 0.5); put(im, pl, ix, cy - 23)
    cameo(im, ix + 26, cy, 36, n); txt(im, (ix + 52, cy + 5), name, "Jost-Bold.ttf", 15, CREAM, "ls")
    sp = sprite("badges/" + {"face_councillor": "npc_councillor", "face_waitress": "npc_waitress", "face_driver": "npc_driver"}[n], game_h=24); put(im, sp, ix + 270, cy, "mm")
# 3 the feed (22 px chip)
txt(im, (ix, 230), "FEED (22 px)", "Jost-Bold.ttf", 11, DIM, "ls", 0.1)
for i, (n, name, line) in enumerate((("face_councillor", "Vale", ""), ("face_waitress", "Inez", ""), ("face_driver", "Driver", ""))):
    cy = 254 + i * 32; cameo(im, ix + 14, cy, 22, n); txt(im, (ix + 34, cy + 5), name, "Jost-Bold.ttf", 13.5, CREAM, "ls")
cameo(im, ix + 150, 254, 22, "face_councillor_model"); txt(im, (ix + 170, 259), "model, 22", "Jost-Bold.ttf", 12.5, DIM, "ls")
# 4 beside the roster's cameos and the painted/model pair at the sizes a chip uses
txt(im, (ix, 372), "WITH THE CELL (34 px, 24 px, 18 px)", "Jost-Bold.ttf", 11, DIM, "ls", 0.1)
row = ["cameo_scout_normal", "cameo_fixer_normal", "cameo_tech_normal", "cameo_enforcer_normal", "face_councillor", "face_councillor_model", "face_waitress", "face_driver"]
for i, n in enumerate(row):
    cx = ix + 20 + i * 44; cameo(im, cx, 402, 34, n)
    if i >= 4: cameo(im, cx, 438, 24, n); cameo(im, cx, 466, 18, n)
# 5 4x zoom of the faces
zx = 830
txt(im, (zx, 34), "FACES, 4x (64 px texels)", "Jost-Bold.ttf", 11, DIM, "ls", 0.1)
for i, n in enumerate(["face_councillor", "face_councillor_model", "face_waitress", "face_driver"]):
    f = kitimg(n).resize((P(64), P(64)), Image.NEAREST); put(im, f, zx + (i % 2) * 150, 52 + (i // 2) * 150)
txt(im, (zx, 372), "ground #b3b0ad, ring from Cameo()", "Jost-SemiBold.ttf", 12, DIM, "ls")
txt(im, (zx, 408), "BAKED cameo_*_normal (34 px) beside the cell's", "Jost-Bold.ttf", 11, DIM, "ls", 0.1)
for i, n in enumerate(["cameo_scout_normal", "cameo_enforcer_normal", "cameo_councillor_normal", "cameo_councillor_model", "cameo_waitress_normal", "cameo_driver_normal"]):
    put(im, kitimg(n).resize((P(34), P(34)), Image.LANCZOS), zx + i * 40, 420)
    put(im, kitimg(n).resize((P(22), P(22)), Image.LANCZOS), zx + i * 40 + 6, 470)
im.convert("RGB").save(R + "briefs/063/board-07-vale-portraits.png"); print(im.size)
