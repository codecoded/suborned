# Glyph, brief 039: the height (metres) and screen footprint of a pose, with a picture, for choosing and for the pose sheet. python3 -I heights39.py <fig> <heading> <out.png> clip:frame[:lib] ...
import sys, os, json
sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/039/glyph")
import fig39 as f
from PIL import Image, ImageDraw
fig, heading, out = sys.argv[1], float(sys.argv[2]), sys.argv[3]; tiles = []; rows = []
for spec in sys.argv[4:]:
    parts = spec.split(":"); name = parts[0]; fr = int(parts[1]); is_lib = any(x.startswith(name + "__") for x in os.listdir(f.LIBD))
    clip = f.lib(name) if is_lib else name
    PPM = 90.0; TW, TH = 150, 170
    im, pref = f.render([dict(id=fig, heading=heading, x=0.0, y=0.0, clip=clip, frame=fr)], TW, TH, look=(0, 0, 0.6), ppm=PPM, samples=24)
    d = json.load(open(pref + "_dims.json"))[0]
    img = Image.fromarray(im); dr = ImageDraw.Draw(img); dr.text((3, 3), "%s f%d" % (name, fr), fill=(10, 10, 10)); dr.text((3, 14), "h %.2f m  w %.2f" % (d["zmax"], max(d["xspan"], d["yspan"])), fill=(10, 10, 10))
    tiles.append(img); rows.append((name, fr, round(d["zmax"], 2), round(d["xspan"], 2), round(d["yspan"], 2)))
cols = min(6, len(tiles)); nr = (len(tiles) + cols - 1) // cols
sheet = Image.new("RGB", (cols * 150, nr * 170), (200, 200, 200))
for i, t in enumerate(tiles): sheet.paste(t, ((i % cols) * 150, (i // cols) * 170))
sheet.save(out); print(out, sheet.size)
for r in rows: print(r)
