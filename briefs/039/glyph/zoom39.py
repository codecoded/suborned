# Glyph, brief 039: a contact strip of the figures in a scene at 4x, for choosing frames. python3 -I zoom39.py <scene.png> <out.png> x,y ...  (feet in game px)
import sys
from PIL import Image, ImageDraw
im = Image.open(sys.argv[1]).convert("RGB"); out = sys.argv[2]; pts = [tuple(int(v) for v in a.split(",")) for a in sys.argv[3:]]
W, H = 72, 80; tiles = []
for (x, y) in pts:
    c = im.crop((x - W // 2, y - H + 12, x + W // 2, y + 12)).resize((W * 4, H * 4), Image.LANCZOS); tiles.append(c)
s = Image.new("RGB", (W * 4 * len(tiles), H * 4)); [s.paste(t, (i * W * 4, 0)) for i, t in enumerate(tiles)]
s.save(out); print(out, s.size)
