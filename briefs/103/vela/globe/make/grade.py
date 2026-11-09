"""Brief 103 (Vela): the play grade's colour adjustments on a Blender render (ACES 1.3 view), so the renders sit next to Sky's frame.
light.json post.colour: contrast 10, saturation 25 (URP Color Adjustments). Applied here in display space: saturation x1.35 about the
luma, contrast x1.08 about 0.45, and the old town's warmer balance (green x0.965, blue x0.9: Sky's frame's stone is sand, not cream). An approximation of the look, used the same way on every tile.

python3 grade.py <in.png> <out.png>"""
import sys

import numpy as np
from PIL import Image

SAT, CON, PIVOT, WARM = 1.35, 1.08, 0.45, (1.0, 0.965, 0.9)


def grade(im):
    a = np.asarray(im.convert("RGB"), dtype=np.float32) / 255.0 * np.array(WARM, dtype=np.float32)
    y = (a * np.array([0.2126, 0.7152, 0.0722], dtype=np.float32)).sum(axis=2, keepdims=True)
    a = y + (a - y) * SAT
    a = (a - PIVOT) * CON + PIVOT
    return Image.fromarray((np.clip(a, 0, 1) * 255 + 0.5).astype(np.uint8))


if __name__ == "__main__":
    grade(Image.open(sys.argv[1])).save(sys.argv[2])
