"""Before/after frame strips of the clips on a figure (python3 -I tools/figures/animstrip.py <before dir> <after dir> <out dir>).
Shots come from the scratch Unity project (FigCheck: the figure's own LOD0, one clip at six phases, three-quarter view)."""
import sys, os
from PIL import Image, ImageDraw
before, after, out = sys.argv[1:4]
os.makedirs(out, exist_ok=True)
FIGS = ['scout', 'fixer', 'tech', 'enforcer', 'c6', 'd2m']
WOMEN = {'scout', 'fixer', 'c6'}
FR = ['0.00', '0.17', '0.33', '0.50', '0.67', '0.83']
FRK = ['0.00', '0.12', '0.25', '0.40', '0.60', '0.90']
w, h = 150, 200
for fig in FIGS:
    states = ['idle', 'walk_f' if fig in WOMEN else 'walk', 'run', 'crouch', 'carried', 'run_armed', 'aim_pistol', 'knockout']
    W = Image.new('RGB', (110 + w * 6, (h + 2) * len(states) * 2 + 16), (214, 214, 204)); d = ImageDraw.Draw(W)
    d.text((4, 2), f'{fig}: before (the 17:41 install) above, after below, six phases of each clip', fill=(0, 0, 0))
    y = 16
    for st in states:
        for tag, dr in (('before', before), ('after', after)):
            d.text((4, y + 4), f'{st}', fill=(0, 0, 0)); d.text((4, y + 18), tag, fill=(150, 0, 0) if tag == 'before' else (0, 90, 0))
            for i, f in enumerate(FRK if st == 'knockout' else FR):
                p = f'{dr}/{fig}_{st}_{f}_three.png'
                if os.path.exists(p): W.paste(Image.open(p).convert('RGB').crop((40, 20, 440, 620)).resize((w, h)), (110 + i * w, y))
            y += h + 2
    W.save(f'{out}/{fig}.png'); print(fig, W.size)
