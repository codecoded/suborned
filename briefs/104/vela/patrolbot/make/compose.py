"""Brief 104 (Vela): lay PatrolBot104's render tiles (render.py) out as review sheets. python3 -I compose.py <tiles_dir> <out_dir>
  turnaround.png    front, left, back, right and top on one ground line, as patrol-robot-turnaround.png (400 px/m; guide lines at the ground, ball top, shoulder,
                    face centre, the beacon's foot and top)
  three-quarter.png closed and open side by side
  hatch-open.png    the open hatch, close
  play-zoom.png     the play camera at 54 px/m, both balls (left the cream-halves variant, right the kit's gunmetal) beside the 1.8 m figure, beacons lit:
                    native pixels on the left, the same pixels x4 on the right
  beacon.png        the beacon close, unlit and lit"""

import sys
from pathlib import Path

from PIL import Image, ImageDraw

BG, LINE = (222, 222, 222), (150, 150, 150)
tiles, out = Path(sys.argv[1]), Path(sys.argv[2])
out.mkdir(parents=True, exist_ok=True)
T = {p.stem: Image.open(p).convert("RGBA") for p in tiles.glob("*.png")}


def on(bg, im, xy):
    bg.alpha_composite(im, xy)


# turnaround: ortho tiles share 400 px/m and the target z 0.67 at the frame centre, so z 0 sits at (h/2 + 0.67 * 400) from the top
names = ["ortho_front", "ortho_left", "ortho_back", "ortho_right"]
H = T["ortho_front"].height
pad = 30
W = sum(T[n].width for n in names) + T["ortho_top"].width + pad * 6
sheet = Image.new("RGBA", (W, H + 2 * pad), BG + (255,))
d = ImageDraw.Draw(sheet)
ground = pad + round(H / 2 + 0.67 * 400)
for z in (0.0, 0.40, 0.70, 0.91, 1.105, 1.271):
    y = ground - round(z * 400)
    d.line([(0, y), (W, y)], fill=LINE, width=1)
x = pad
for n in names:
    on(sheet, T[n], (x, pad))
    x += T[n].width + pad
top = T["ortho_top"]
on(sheet, top, (x, ground - round(0.85 * 400) - top.height // 2))
sheet.convert("RGB").save(out / "turnaround.png")

# three-quarter, closed and open
a, b = T["three_closed"], T["three_open"]
tq = Image.new("RGBA", (a.width + b.width + 3 * pad, max(a.height, b.height) + 2 * pad), (240, 232, 218, 255))
on(tq, a, (pad, pad))
on(tq, b, (2 * pad + a.width, pad))
tq.convert("RGB").save(out / "three-quarter.png")

h = T["hatch_open"]
hb = Image.new("RGBA", h.size, (240, 232, 218, 255))
on(hb, h, (0, 0))
hb.convert("RGB").save(out / "hatch-open.png")

# play zoom: native and x4 (nearest, so the pixels are the game's), on a pavement grey
p = T["play_ball"]
pv = Image.new("RGBA", p.size, (176, 172, 164, 255))
on(pv, p, (0, 0))
big = pv.resize((p.width * 4, p.height * 4), Image.NEAREST)
pz = Image.new("RGB", (p.width + big.width + 3 * pad, big.height + 2 * pad), BG)
pz.paste(pv.convert("RGB"), (pad, pad))
pz.paste(big.convert("RGB"), (2 * pad + p.width, pad))
pz.save(out / "play-zoom.png")

a, b = T["beacon_unlit"], T["beacon_lit"]
bc = Image.new("RGBA", (a.width + b.width + 3 * pad, a.height + 2 * pad), (240, 232, 218, 255))
on(bc, a, (pad, pad))
on(bc, b, (2 * pad + a.width, pad))
bc.convert("RGB").save(out / "beacon.png")
print(f"[compose] 5 sheets -> {out}")
