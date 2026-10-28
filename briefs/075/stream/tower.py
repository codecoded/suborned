"""Scratch (Vela 075-E): CornerTower_12 (brass canopy fixed) at play zoom and street level, beside StreamBay_6 bays. blender -b -P tower.py -- out"""
import math, sys
from pathlib import Path
import bpy
from mathutils import Euler, Vector
sys.path.insert(0, "/Users/sky/Projects/suborned/tools/decokit")
sys.argv = [sys.argv[0], "--", sys.argv[sys.argv.index("--") + 1], "none"]
exec(open(str(Path(__file__).parent / "sheet.py")).read().split("ZOOM14 = 28.0 * 16 / 9")[0])
ZOOM14 = 28.0 * 16 / 9
place("CornerTower_12", (0, 0, 0), 0)
for i in range(2):
    place("StreamBay_6", (8 + 6 * i, 0, 0), 0)
    place("StreamBay_6", (-5, 8 + 6 * i, 0), -90)
ground()
shoot("tower_play30", (0, 4, 10), 30, ZOOM14)
shoot("tower_wide30", (0, 4, 30), 30, 80)
street("tower_street", (-14, -22, 1.7), (-1, 1, 6), lens=28)
