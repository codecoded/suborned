"""Lookdev balls for Cycles <-> Unity parity: mirror chrome, ivory + red lacquer, 18% grey diffuse.
Not a kit asset: category "Calibration" is skipped by the Unity showroom."""
import math

from kit import asset
from mats import pal, register

register("DK_Calib_Grey18", "rubber", "#767676")  # sRGB 118 = 18% linear, matte


def sphere(s, x, r, mat, seg=48):
    prof = [(r * math.sin(t), r - r * math.cos(t)) for t in (math.pi * i / 24 for i in range(25))]
    with s.at((x, 0, 0)):
        s.lathe(prof, mat, seg=seg)


@asset("LookdevBalls", "Calibration", bevel=0.0)
def lookdev_balls(s):
    for i, m in enumerate((pal.Chrome, pal.Enamel_Ivory, pal.Enamel_Red, pal.Calib_Grey18)):
        sphere(s, (i - 1.5) * 0.62, 0.28, m)
