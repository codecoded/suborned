"""Assemble the proxy tiles into sheets with the measured numbers. python3 -I tf_tiles.py <proxy dir> <outdir>"""
import sys, json
from PIL import Image, ImageDraw, ImageFont
P, O = sys.argv[1], sys.argv[2]
m = json.load(open(f"{P}/metrics.json"))["results"]
F = ImageFont.truetype("/System/Library/Fonts/Helvetica.ttc", 17); FB = ImageFont.truetype("/System/Library/Fonts/Helvetica.ttc", 20)
poses = [("walk_contact", "walk, mid-stride"), ("walk_pass", "walk, legs passing"), ("run_flight", "run, mid-stride"), ("run_pass", "run, legs passing"),
         ("crouch_low", "crouch, low | (junction box)"), ("crouch_deep", "crouch, deep"), ("fight", "fight stance | braced shot"), ("kerb", "step up onto | an 18 cm kerb")]
cols = [("AD", "front"), ("AD", "side"), ("FX10", "front"), ("FX10", "side"), ("FX10", "game")]
sc = 0.62; tw, th = int(420 * sc), int(600 * sc); lw = 190; hh = 52
def cm(v): return f"{v*100:.1f} cm" if v >= 0.0005 else "none"
def sheet(rows, name, cols=cols, heads=("as drawn", "as drawn", "proposed", "proposed", "proposed")):
    S = Image.new("RGB", (lw + tw * len(cols), hh + th * len(rows)), "white"); d = ImageDraw.Draw(S)
    for c, ((vn, kind), h) in enumerate(zip(cols, heads)):
        d.text((lw + c * tw + 8, 8), h, fill=(20, 20, 20), font=FB); d.text((lw + c * tw + 8, 30), {"front": "front", "side": "side", "game": "game camera (yaw 30, pitch 38)"}[kind], fill=(100, 100, 100), font=F)
    for r, (pn, label) in enumerate(rows):
        y = hh + r * th
        [d.text((8, y + 8 + 22 * i), t, fill=(20, 20, 20), font=FB) for i, t in enumerate(label.split(" | "))]
        for vn, nm in (("AD", "as drawn"), ("FX10", "proposed")):
            mm = m[vn][pn]["metrics"]; k = 90 if vn == "AD" else 150
            d.text((8, y + k + 10), nm, fill=(100, 100, 100), font=F)
            d.text((8, y + k + 32), f"legs {cm(mm['leg_leg'][0])}", fill=(190, 0, 150) if mm["leg_leg"][0] > 0.02 else (20, 110, 60), font=F)
            d.text((8, y + k + 52), f"thigh {cm(mm['through_leg'][0])}", fill=(190, 0, 150) if mm["through_leg"][0] > 0.02 else (20, 110, 60), font=F)
        for c, (vn, kind) in enumerate(cols):
            im = Image.open(f"{P}/tiles/{vn}-{pn}-{kind}.png").convert("RGB").resize((tw, th), Image.LANCZOS); S.paste(im, (lw + c * tw, y))
    S.save(f"{O}/{name}.png"); print(name, S.size)
sheet(poses[:4], "proxy-sheet-1"); sheet(poses[4:], "proxy-sheet-2")
# the round 4 bell shorts beside the proposed trousers in the poses where they meet
b = [("walk_pass", "walk, legs passing"), ("run_pass", "run, legs passing"), ("crouch_deep", "crouch, deep")]
S = Image.new("RGB", (lw + tw * 4, hh + th * 3), "white"); d = ImageDraw.Draw(S)
for c, (h, k) in enumerate((("round 4 bell shorts", "front"), ("round 4 bell shorts", "side"), ("proposed trousers", "front"), ("proposed trousers", "side"))):
    d.text((lw + c * tw + 8, 8), h, fill=(20, 20, 20), font=FB); d.text((lw + c * tw + 8, 30), k, fill=(100, 100, 100), font=F)
for r, (pn, label) in enumerate(b):
    y = hh + r * th; d.text((8, y + 10), label, fill=(20, 20, 20), font=FB)
    for k, (vn, nm) in enumerate((("BELL", "bell"), ("FX10", "proposed"))):
        mm = m[vn][pn]["metrics"]; d.text((8, y + 70 + k * 80), nm, fill=(100, 100, 100), font=F)
        d.text((8, y + 92 + k * 80), f"legs {cm(mm['leg_leg'][0])}", fill=(190, 0, 150) if mm["leg_leg"][0] > 0.02 else (20, 110, 60), font=F)
        d.text((8, y + 112 + k * 80), f"thigh {cm(mm['through_leg'][0])}", fill=(190, 0, 150) if mm["through_leg"][0] > 0.02 else (20, 110, 60), font=F)
    for c, (vn, kind) in enumerate((("BELL", "front"), ("BELL", "side"), ("FX10", "front"), ("FX10", "side"))):
        S.paste(Image.open(f"{P}/tiles/{vn}-{pn}-{kind}.png").convert("RGB").resize((tw, th), Image.LANCZOS), (lw + c * tw, y))
S.save(f"{O}/proxy-sheet-bell.png"); print("bell", S.size)
