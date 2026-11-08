"""The project's own reader on the take-control mocks (brief 101): each mocked frame's picture, its HUD layer (today's .hud.png with the lifted
pieces out, the new pieces over it) and its manifest (today's, the lifted pieces out, the new pieces in, named as the build names its tabs: the
Squad tab's word was an `image`, "squad button word"), run through tools/hud-budget (bun tools/hud-budget/cli.ts <folder>). Glyph."""
import os, sys, json, shutil, subprocess
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import tc_frames as F
from tc_frames import *

D = SCRT + 'budget/'
SRC = {'rest': R + 'out/scratch/latch/101r3/shots-288/t101-quiet', 'hover': R + 'out/scratch/latch/101r3/shots-288/t101-quiet', 'control': F.DRIVE[0], 'back': F.DRIVE[0]}
STATE = {'rest': 'rest', 'hover': 'active', 'control': 'rest', 'back': 'active'}


def inside(e, b): x0, y0, x1, y1 = b; cx, cy = e['x'] + e['w'] / 2, e['y'] + e['h'] / 2; return x0 <= cx <= x1 and y0 <= cy <= y1


def main():
    res = F.main(); shutil.rmtree(D, ignore_errors=True); os.makedirs(D)
    for key, v in res.items():
        src = SRC[key]; hud = np.array(Image.open(src + '.hud.png').convert('RGBA')); hud[:, :, 3] = np.minimum(hud[:, :, 3], v['rest'])
        h = Image.fromarray(hud); h.alpha_composite(v['layer'].img.resize((1920, 1080), Image.LANCZOS)); h.save(D + key + '.hud.png')
        shutil.copy(OUTT + key + '.png', D + key + '.png')
        m = json.load(open(src + '.manifest.json')); lifted = list(v.get('lifted', {}).values())
        els = [e for e in m['elements'] if not any(inside(e, b) for b in lifted)]
        for name, (x, y, w, hh) in v['new']: els.append({'kind': 'image', 'x': round(x), 'y': round(y), 'w': round(w), 'h': round(hh), 'text': name})
        json.dump({'state': STATE[key], 'elements': els}, open(D + key + '.manifest.json', 'w'), indent=1)
    r = subprocess.run(['bun', R + 'tools/hud-budget/cli.ts', D], capture_output=True, text=True, cwd=R); print(r.stdout[-4000:], r.stderr[-2000:])
    open(OUTT + 'make/budget.txt', 'w').write(r.stdout)


if __name__ == '__main__':
    main()
