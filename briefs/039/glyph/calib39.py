# Glyph, brief 039: the 2x crop check of every new piece on one plate (art bible: "check every HUD piece cropped at 2x"). python3 -I briefs/039/glyph/calib39.py
import sys
sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/039/glyph")
from heroes39 import *
fr = blank_frame(720, 290, (200, 190, 170))
cover_disc(fr, (30, 30), True, None, live=False); cover_disc(fr, (70, 30), True, "rival"); cover_disc(fr, (110, 30), False, "cell", lit=True); cover_disc(fr, (150, 30), True, "rival", struck=True); cover_disc(fr, (190, 30), True, "rival", took=True)
pin_disc(fr, (240, 30), 0.78); pin_disc(fr, (280, 30), 0.3)
lay = layer(fr)
for i, st in enumerate(("ready", "cooling", "shut")):
    d = kd.sprite("markers/disc_dark_24", game_h=26); x = 330 + i * 36; y = 30
    lay.alpha_composite(d, (int(round(x * S - d.width / 2)), int(round(y * S - d.height / 2)))); ico(lay, MOVE_ICON[i], x, y, 16, CREAM, alpha=255 if st == "ready" else 125)
    if st == "ready": stroke(lay, circ((x, y), 15.2, 36), TURQL, 2.0, closed=True)
    elif st == "cooling": stroke(lay, circ((x, y), 15.2, 36), INKDEEP, 2.0, alpha=110, closed=True); stroke(lay, circ((x, y), 15.2, 48, -90, -90 + 360 * 0.4), TURQL, 2.0)
    else: ico(lay, "lock", x + 9, y + 9, 10, CREAM)
fr.hud.alpha_composite(lay)
beat_marker(fr, 60, 56, "bang", 0.8); beat_marker(fr, 150, 56, None, 0.5, face=RIVAL); beat_marker(fr, 240, 56, "s_downed", 0.9); beat_marker(fr, 330, 56, "car", 0.2)
crew_tally(fr, 20, 118, 5, 1, 1, mark_at=2); crew_tally(fr, 140, 118, 3, 0, 0, mark_at=1)
pill39(fr, (215, 215), "Right hold", [("txt", "Covering fire · 4 s")], "burst", faces=[ENF], harm=True)
pill39(fr, (540, 215), "Hold", [("txt", "Mark · 1 s")], "mark", faces=[SCOUT])
pill39(fr, (215, 275), "Right hold", [("icon", "s_downed"), ("txt", "9 s")], "fire", faces=[SCOUT, FIXER], harm=True); pill39(fr, (540, 275), "Hold", [("txt", "Talk down · 2 s")], "megaphone", faces=[FIXER])
im = fr.compose().convert("RGB"); im = im.resize((im.width * 2, im.height * 2), Image.LANCZOS); im.save(R + "briefs/039/glyph/check-2x-new-pieces.png"); print(im.size)
