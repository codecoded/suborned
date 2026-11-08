"""The project's own reader on the mocks: each frame's picture, its HUD layer alone (.hud.png, from the clear-mode render) and a manifest naming
the speech bubbles and the news card, run through tools/hud-budget (bun tools/hud-budget/cli.ts <folder> --json). Glyph, brief 101 round 2."""
import sys, os, json, subprocess, shutil
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from r2_base import *
import r2_street as S, r2_tokens as T

D = SCR2 + 'budget/'
SPEECH = {'quiet': QUIET2['speech'], 'loud': LOUD2['speech'], 'aim': [], 'rest': [], 'busy': BUSY['speech']}
NEWS = {'S': {'quiet': (1520, 990, 400, 90), 'loud': (1660, 500, 260, 150), 'busy': (1560, 990, 360, 90)},
        'T': {'quiet': (1580, 990, 340, 90), 'loud': (1500, 990, 420, 90), 'busy': (1580, 990, 340, 90)}}
NEWS_TEXT = {'quiet': QUIET2['news'], 'loud': LOUD2['news'], 'busy': BUSY['news']}


def manifest(d, m):
    el = []
    for who, t, (x, y) in SPEECH[m]: w = P0.tw(t, 15.5, 'Barlow-Medium') + 44; el.append({'kind': 'speech', 'x': round(x - w / 2), 'y': round(y - 14), 'w': round(w), 'h': 28, 'text': t})
    if m in NEWS[d]: x, y, w, h = NEWS[d][m]; n = NEWS_TEXT[m]; el.append({'kind': 'card', 'x': x, 'y': y, 'w': w, 'h': h, 'text': f"{n['title']} · {n['where']} · {n['when']}"})
    clock = {'rest': QUIET2, 'quiet': QUIET2, 'loud': LOUD2, 'aim': AIM2, 'busy': BUSY}[m]['clock']  # the time as a clock reading (budget.ts isClock: a marker), as the build declares its clock
    el.append({'kind': 'line', 'x': 1786 if d == 'S' else 1826, 'y': 14, 'w': 114 if d == 'S' else 78, 'h': 48, 'text': clock})
    if m == 'busy': el.append({'kind': 'image', 'x': 940, 'y': 520, 'w': 70, 'h': 46, 'text': 'car held marker'})  # the lock, count and seat pips: art, as the build's manifest has it
    return {'state': 'active' if m in ('aim', 'busy') else 'rest', 'elements': el}


if __name__ == '__main__':
    P0 = Page(2); shutil.rmtree(D, ignore_errors=True); os.makedirs(D)
    MODE['clear'] = True; S.render(); T.render()
    for name, layer in LAYERS.items():
        d, m = name.split('-'); layer.save(D + name + '.hud.png'); shutil.copy(OUT2 + name + '.png', D + name + '.png')
        json.dump(manifest(d, m), open(D + name + '.manifest.json', 'w'))
    r = subprocess.run(['bun', R + 'tools/hud-budget/cli.ts', D], capture_output=True, text=True, cwd=R); print(r.stdout[-6000:], r.stderr[-2000:])
