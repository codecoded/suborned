# Glyph, brief 063 / 062 (2026-10-25): board 08, Vale C. The 2x crop board of the redone portraits at their real sizes (canvas 2 px per game px, a Retina scale-2 screen):
# the file card (painted, as installed) beside the model's duotone (review only, not installed), Intel and the feed painted against model, the roster row with the cell,
# the carry plate (18 px) and the live chip (model), 4x faces, the baked cameos. python3 -I briefs/063/portrait_board_08.py -> briefs/063/board-08-vale-C-portraits.png
import sys; sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/062")
from common import *
W, H = 1760, 600
im = Image.new("RGBA", (P(W), P(H)), (29, 58, 58, 255))
def kitimg(n): return load(K + "portraits/%s@2x.png" % n).convert("RGBA")
REV = R + "briefs/063/cameos/review/"
def card_at(x, y, ph, label, name, note):
    CW, CH = 408, 478
    put(im, frame("panels/panel_card", CW, CH, 24, 0.5), x, y)
    txt(im, (x + 24, y + 21), "TARGET", "Jost-Bold.ttf", 11.5, CREAM, "ls", 0.12)
    ph = ph.convert("RGBA").resize((P(360), P(296)), Image.LANCZOS); hdr = 30
    rim = Image.new("RGBA", (ph.width + P(6), ph.height + P(6)), (0, 0, 0, 0)); ImageDraw.Draw(rim).rounded_rectangle((0, 0, rim.width - 1, rim.height - 1), radius=P(13), fill=INKDEEP + (255,))
    put(im, rim, x + 22 - 3, y + hdr + 12 - 3); put(im, ph, x + 22, y + hdr + 12)
    hairline(im, x + 22, y + hdr + 12 + 296 + 18, x + CW - 22, y + hdr + 12 + 296 + 18)
    txt(im, (x + 24, y + hdr + 12 + 296 + 52), name, "Jost-Bold.ttf", 28, INK, "ls")
    ico(im, "take", x + 33, y + hdr + 12 + 296 + 80, 20, TEXTCARD); txt(im, (x + 52, y + hdr + 12 + 296 + 87), "Alive", "Jost-SemiBold.ttf", 17, TEXTCARD, "ls")
    txt(im, (x + 24, y + CH + 22), label, "Jost-Bold.ttf", 11, DIM, "ls", 0.1); txt(im, (x + 24, y + CH + 40), note, "Jost-SemiBold.ttf", 12, DIM, "ls")
card_at(24, 20, kitimg("card_councillor"), "FILE CARD, INSTALLED (painted duotone)", "Councillor Vale", "card_councillor@2x, 720 x 592")
card_at(450, 20, load(REV + "card_councillor_model@2x.png"), "THE MODEL AT THE SAME SIZE (review only)", "Councillor Vale", "not installed: soft, waxy, eyes smeared")
# Intel, 36 px: painted (installed) | model
ix = 890
txt(im, (ix, 34), "INTEL (36 px): painted | model (review)", "Jost-Bold.ttf", 11, DIM, "ls", 0.1)
for i, (n, name) in enumerate((("face_councillor", "Councillor Vale"), ("face_waitress", "Inez"), ("face_driver", "The driver"))):
    cy = 62 + i * 52; put(im, frame("markers/plate_dark", 290, 46, 24, 0.5), ix, cy - 23)
    cameo(im, ix + 26, cy, 36, n); txt(im, (ix + 52, cy + 5), name, "Jost-Bold.ttf", 15, CREAM, "ls")
    put(im, sprite("badges/" + {"face_councillor": "npc_councillor", "face_waitress": "npc_waitress", "face_driver": "npc_driver"}[n], game_h=24), ix + 262, cy, "mm")
put(im, frame("markers/plate_dark", 170, 46, 24, 0.5), ix + 310, 62 - 23); cameo(im, ix + 336, 62, 36, "face_councillor_model"); txt(im, (ix + 362, 67), "Vale", "Jost-Bold.ttf", 15, CREAM, "ls")
# feed, 22 px
txt(im, (ix, 230), "FEED (22 px)", "Jost-Bold.ttf", 11, DIM, "ls", 0.1)
for i, (n, name) in enumerate((("face_councillor", "Vale"), ("face_waitress", "Inez"), ("face_driver", "Driver"))):
    cy = 254 + i * 30; cameo(im, ix + 14, cy, 22, n); txt(im, (ix + 34, cy + 5), name, "Jost-Bold.ttf", 13.5, CREAM, "ls")
cameo(im, ix + 150, 254, 22, "face_councillor_model"); txt(im, (ix + 170, 259), "model, 22", "Jost-Bold.ttf", 12.5, DIM, "ls")
# the carry plate (18 px) and the live chip, model
txt(im, (ix + 300, 230), "CARRY (18), CHIP (32)", "Jost-Bold.ttf", 11, DIM, "ls", 0.1)
put(im, frame("markers/plate_dark", 96, 26, 24, 0.5), ix + 300, 241); cameo(im, ix + 316, 254, 18, "face_councillor_model"); ico(im, "take", ix + 348, 254, 14, CREAM); txt(im, (ix + 362, 259), "0.6", "Jost-Bold.ttf", 14.5, CREAM, "ls")
put(im, frame("markers/plate_dark", 120, 44, 24, 0.5), ix + 300, 276); cameo(im, ix + 324, 298, 32, "face_councillor_model"); txt(im, (ix + 348, 303), "Vale", "Jost-Bold.ttf", 14.5, CREAM, "ls")
# the roster row: the cell, the model Vale, the painted Vale for comparison, the others, at 34 / 24 / 18
ry = 372
txt(im, (ix, ry+0), "WITH THE CELL (34, 24, 18 px)", "Jost-Bold.ttf", 11, DIM, "ls", 0.1)
txt(im, (ix + 20, ry + 128), "scout, fixer, tech, enforcer | model Vale, painted Vale, Inez, driver", "Jost-SemiBold.ttf", 12, DIM, "ls")
row = ["cameo_scout_normal", "cameo_fixer_normal", "cameo_tech_normal", "cameo_enforcer_normal", "face_councillor_model", "face_councillor", "face_waitress", "face_driver"]
for i, n in enumerate(row):
    cx = ix + 20 + i * 44; cameo(im, cx, ry + 30, 34, n)
    if i >= 4: cameo(im, cx, ry + 66, 24, n); cameo(im, cx, ry + 94, 18, n)
# 4x faces and the baked cameos
zx = 1420
txt(im, (zx, 34), "FACES, 4x (64 px texels)", "Jost-Bold.ttf", 11, DIM, "ls", 0.1)
for i, n in enumerate(["face_councillor", "face_councillor_model", "face_waitress", "face_driver"]):
    put(im, kitimg(n).resize((P(64), P(64)), Image.NEAREST), zx + (i % 2) * 150, 52 + (i // 2) * 150)
txt(im, (zx, 358), "ground #b3b0ad, ring from Cameo()", "Jost-SemiBold.ttf", 12, DIM, "ls")
txt(im, (zx, 408), "BAKED cameo_*_normal (34 px), 22 px below", "Jost-Bold.ttf", 11, DIM, "ls", 0.1)
for i, n in enumerate(["cameo_scout_normal", "cameo_enforcer_normal", "cameo_councillor_normal", "cameo_councillor_model", "cameo_waitress_normal", "cameo_driver_normal"]):
    put(im, kitimg(n).resize((P(34), P(34)), Image.LANCZOS), zx + (i % 3) * 44, 420 + (i // 3) * 56)
im.convert("RGB").save(R + "briefs/063/board-08-vale-C-portraits.png"); print(im.size)
