# Glyph, brief 039: a contact sheet of library clips on one figure at the play camera, N frames a clip: python3 -I posesheet39.py <figure> <heading> <out.png> clip[:f1,f2,..] ...
import sys, os, json
sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/039/glyph")
import fig39 as f
from PIL import Image, ImageDraw
INFO = json.load(open('/Users/sky/Projects/suborned/out/scratch/glyph/039/poses/clipinfo.json'))
def frames_of(name):
    key = [k for k in INFO if k.startswith(name + '__') or k == name + '.fbx']
    return INFO[key[0]] if key else [1, 30]
fig, heading, out = sys.argv[1], float(sys.argv[2]), sys.argv[3]
rows = []
for spec in sys.argv[4:]:
    name, _, fr = spec.partition(':'); rng = frames_of(name)
    fl = [int(x) for x in fr.split(',')] if fr else [int(rng[0] + (rng[1] - rng[0]) * k / 5.0) for k in range(6)]
    clip = f.lib(name) if os.path.exists(f.LIBD) and any(x.startswith(name + '__') for x in os.listdir(f.LIBD)) else name
    PPM = 75.0; TW, TH = 120, 150; n = len(fl)
    figs = [dict(id=fig, heading=heading, x=(i - (n - 1) / 2.0) * (TW / PPM), y=0.0, clip=clip, frame=fr_) for i, fr_ in enumerate(fl)]
    im, _ = f.render(figs, TW * n, TH, look=(0, 0, 0.8), ppm=PPM, samples=16)
    img = Image.fromarray(im); d = ImageDraw.Draw(img); d.text((4, 3), name + "  " + str(fl), fill=(20, 20, 20))
    for i, fr_ in enumerate(fl): d.text((i * TW + 4, TH - 14), "f%d" % fr_, fill=(20, 20, 20))
    rows.append(img)
sheet = Image.new('RGB', (max(r.width for r in rows), sum(r.height for r in rows)), (200, 200, 200)); y = 0
for r in rows: sheet.paste(r, (0, y)); y += r.height
sheet.save(out); print(out, sheet.size)
