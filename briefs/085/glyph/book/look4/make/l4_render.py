import sys, json
sys.path.insert(0, '.')
from l4_common import *
import l4_closeup, l4_deck, l4_light
OPTS = dict(B=('deck', l4_deck.deck), L=('light', l4_light.light), C=('closeup', l4_closeup.closeup))
want = sys.argv[1:] or list(PAGES); st = {}
for o, (nm, f) in OPTS.items():
    for k in want:
        _, c, secs = PAGES[k]; P = f(c, secs); P.img.resize((1920, 1080), Image.LANCZOS).convert('RGB').save(OUT4 + f'{o}-{nm}-{k}.png'); st[f'{o}-{k}'] = P.words
print(json.dumps(st))
