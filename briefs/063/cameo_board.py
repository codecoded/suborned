# Glyph (2026-10-25), brief 063: the cameo ground and the patrol disc, in the kit's cameo ring at 18 / 24 / 32 / 48 px.  python3 -I briefs/063/cameo_board.py
# (run briefs/063/cameos/flat.py first: it makes the roster-grey faces)
import sys; sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/058")
from glib import *
names = ["vale_B", "inez_A", "driver_A"]; cream_dir = "briefs/063/cameos/"; flat_dir = "briefs/063/cameos/flat/"
board = Image.new("RGB", (1500, 800), BG); d = ImageDraw.Draw(board)
d.text((24, 16), "063 · painted cameos in the kit's cameo ring, 18 / 24 / 32 / 48 px", font=board_font(34), fill=BCREAM)
d.text((24, 62), "Roster grey (picked) against the paper's cream; and the patrol's installed disc. 2 px per game px on the roster's teal.", font=body_font(22), fill=(176, 205, 199))
y = 130
def tile(path, sz):
    lay = Image.new("RGBA", (int(sz * 2 * 1.3), int(sz * 2 * 1.3)), (0, 0, 0, 0)); cameo_png(lay, lay.width / S / 2, lay.height / S / 2, sz, path)
    bg = Image.new("RGBA", lay.size, (60, 80, 78, 255)); bg.alpha_composite(lay); return bg.convert("RGB")
for dirp, suffix, lab in [(flat_dir, "_face@4x.png", "roster grey (pick): the white beard and the pale collar keep their edges"), (cream_dir, "@2x.png", "paper cream (as painted): reads as a badge, and the white beard loses contrast")]:
    d.text((24, y), lab, font=board_font(24), fill=BCREAM); x = 24; yy = y + 40
    for nm in names:
        for sz in (18, 24, 32, 48): t = tile(dirp + nm + suffix, sz); board.paste(t, (x, yy)); x += t.width + 8
        x += 40
    y += 220
d.text((24, y), "npc_patrol: the face in the Cameo() ring (left four), the bezelled badge (right three)", font=board_font(24), fill=BCREAM); x = 24; yy = y + 40
for sz in (18, 24, 32, 48): t = tile(K + "badges/npc_patrol_face@2x.png", sz); board.paste(t, (x, yy)); x += t.width + 8
x += 40
for sz in (24, 32, 48):
    b = kd.sprite("badges/npc_patrol", game_h=sz); bg = Image.new("RGBA", (b.width + 20, b.height + 20), (60, 80, 78, 255)); bg.alpha_composite(b, (10, 10)); board.paste(bg.convert("RGB"), (x, yy)); x += bg.width + 8
board.save("/Users/sky/Projects/suborned/briefs/063/board-06-cameo-ground-and-patrol.png")
