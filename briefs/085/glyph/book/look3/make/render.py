import sys, json
sys.path.insert(0, '.')
import options
from options import *
stats = {}
def quiet(im):
    """Share of the screen in 16 px cells with no ink: flat field or flat street only."""
    a = np.array(im.convert('L').resize((120, 68), Image.BOX)).astype(float); b = np.array(im.convert('L')).astype(float); n = 0
    for y in range(0, 1080, 16):
        for x in range(0, 1920, 16):
            if b[y:y+16, x:x+16].std() < 9: n += 1
    return round(100 * n / (68 * 120))
for o, (nm, f) in OPTS.items():
    for k in (['inez'] if o == 'B' else list(PAGES)):
        _, c, secs = PAGES[k]; P = f(c, secs); im = P.img.resize((1920, 1080), Image.LANCZOS).convert('RGB'); im.save(OUT + f'{o}-{nm}-{k}.png'); stats[f'{o}-{k}'] = (P.words, quiet(im))
options.STAR = True
P = sign_opt(PAGES['inez'][1], SECTIONS); P.img.resize((1920, 1080), Image.LANCZOS).convert('RGB').save(OUT + 'A-pole-inez-with-starburst.png'); options.STAR = False
print(json.dumps(stats))
