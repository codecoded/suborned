"""The project's own reader on round 4 (brief 101): each mock's picture, its HUD layer (today's .hud.png with the lifted pieces taken out, the new pieces
over it) and its manifest (today's, the lifted pieces' elements out, the new pieces' in, named as the build names them), and today's captures as they
are, run through tools/hud-budget (bun tools/hud-budget/cli.ts <folder> --json). Writes make/coverage.json and make/budget.txt. Glyph."""
import os, sys, json, shutil, subprocess
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import r4_frames as F
from r4_base import *

D = SCR4 + 'budget/'; T = SCR4 + 'today/'
SRCNAME = {'fight': 'r123-fight-safe', 'inset': 'r085-inset-inez', 'plate': 'r29-cone-alt', 'seed': 'seed-fire-alt', 'strip': 'hold-watch-reach', 'line': 'r123-body-safe', 'call': 't101-window'}


def centre_in(e, b):
    cx, cy = e['x'] + e['w'] / 2, e['y'] + e['h'] / 2; return b[0] <= cx <= b[2] and b[1] <= cy <= b[3]


def run(folder):
    r = subprocess.run(['bun', R + 'tools/hud-budget/cli.ts', folder, '--json'], capture_output=True, text=True, cwd=R)
    t = subprocess.run(['bun', R + 'tools/hud-budget/cli.ts', folder], capture_output=True, text=True, cwd=R)
    return json.loads(r.stdout), t.stdout


def main():
    F.main(); shutil.rmtree(D, ignore_errors=True); os.makedirs(D); shutil.rmtree(T, ignore_errors=True); os.makedirs(T)
    for key, n in SRCNAME.items():
        for ext in ('.png', '.hud.png', '.manifest.json', '.parts.json'): shutil.copy(SRC4 + n + ext, T + key + ext)
    for name, mk in F.MOCKS.items():
        src = SHOTS[mk['key']][0]; hud = np.array(Image.open(src + '.hud.png').convert('RGBA')); hud[:, :, 3] = np.minimum(hud[:, :, 3], mk['rest'])
        h = Image.fromarray(hud); h.alpha_composite(mk['layer'].img.resize((1920, 1080), Image.LANCZOS)); h.save(D + name + '.hud.png')
        shutil.copy(OUT4 + name + '.png', D + name + '.png'); shutil.copy(src + '.parts.json', D + name + '.parts.json')
        m = json.load(open(src + '.manifest.json'))
        keep = [e for e in m['elements'] if not any(centre_in(e, b) for b in mk['boxes']) and e.get('text') not in mk['drop']]
        for kind, (x, y, w, hh), text in mk['add']: keep.append({'kind': kind, 'x': x, 'y': y, 'w': w, 'h': hh, 'text': text})
        json.dump({'state': m.get('state', 'rest'), 'elements': keep}, open(D + name + '.manifest.json', 'w'), indent=1)
    today, ttxt = run(T); mock, mtxt = run(D)
    rep = lambda r: dict(pct=round(r['coveragePct'], 2), footprint=round(r['footprintPct'], 1), state=r['state'], pass_=r['pass'], failures=r['failures'], warnings=r['warnings'])
    cov = {'today': {os.path.basename(k)[:-4]: rep(v) for k, v in today.items()}, 'mock': {os.path.basename(k)[:-4]: rep(v) for k, v in mock.items()},
           'speech': {name: sum(1 for e in json.load(open(D + name + '.manifest.json'))['elements'] if e['kind'] == 'speech') for name in F.MOCKS},
           'speech_today': {key: sum(1 for e in json.load(open(T + key + '.manifest.json'))['elements'] if e['kind'] == 'speech') for key in SRCNAME},
           'moments': {k: dict(build=v[1], moment=v[2]) for k, v in SHOTS.items() if k in SRCNAME}}
    json.dump(cov, open(OUT4 + 'make/coverage.json', 'w'), indent=1)
    open(OUT4 + 'make/budget.txt', 'w').write('today (the 0.2.290 captures as they are):\n' + ttxt + '\nround 4 (the mocks):\n' + mtxt)
    print(ttxt); print(mtxt)


if __name__ == '__main__':
    main()
