"""The project's own reader on round 3's mocks (brief 101): each mocked frame's picture, its HUD layer (today's .hud.png with the lifted pieces taken
out, the new pieces over it) and its manifest (today's, the lifted pieces' elements out, the new pieces' in, as the build would name them), run through
tools/hud-budget (bun tools/hud-budget/cli.ts <folder>). Glyph."""
import os, sys, json, shutil, subprocess
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import r3_frames as F
from r3_base import *

D = SCR3 + 'budget/'
NEW = {  # the new pieces' manifest elements, as the build would name them (kinds and texts of today's manifests)
    'quiet': [('image', (928, 189, 66, 66), 'attention callout'), ('pop', (905, 347, 112, 24), 'Camera on'), ('marker', (414, 79, 94, 26), 'noticed'),
              ('image', (472, 212, 23, 23), 'door turn'), ('image', (498, 194, 26, 26), 'body'), ('image', (498, 225, 26, 26), 'door marker'), ('image', (530, 226, 52, 24), 'door next step')],
    'window': [('chip', (900, 222, 150, 40), None), ('marker', (924, 296, 82, 28), '2:54'), ('image', (947, 487, 26, 26), 'door marker'), ('image', (947, 1033, 26, 26), 'hint reopen')],
    'card': [('card', (1500, 1003, 330, 64), 'Robots coming · a camera saw the Fixer stay too long · 2:46'), ('image', (1474, 999, 58, 58), 'card badge'),
             ('image', (906, 575, 26, 26), 'car marker'), ('image', (947, 1033, 26, 26), 'hint reopen'), ('image', (1795, 118, 14, 14), 'new dot')],
    'conv': [('card', (746, 226, 460, 176), "Bureau loan, and she's behind. seeing Arthur, the door guard. Help clear her Bureau loan. Pressure threaten to expose the affair."),
             ('image', (947, 1033, 26, 26), 'hint reopen')],
    'ring': [],  # the price rows stay out of the manifest, as today's build leaves its plates out (the reader judges them from the picture)
}
CHIP = {'window': {'facts': ['Guard away'], 'verbs': []}}


def centre_in(e, b): cx, cy = e['x'] + e['w'] / 2, e['y'] + e['h'] / 2; return b[0] <= cx <= b[2] and b[1] <= cy <= b[3]


def main():
    F.main(); shutil.rmtree(D, ignore_errors=True); os.makedirs(D)
    for key, (L, rest, boxes) in F.LAYERS3.items():
        src = SHOTS[key][0]; hud = np.array(Image.open(src + '.hud.png').convert('RGBA')); hud[:, :, 3] = np.minimum(hud[:, :, 3], rest)
        h = Image.fromarray(hud); h.alpha_composite(L.img.resize((1920, 1080), Image.LANCZOS)); h.save(D + key + '.hud.png')
        shutil.copy(OUT3 + key + '.png', D + key + '.png')
        m = json.load(open(src + '.manifest.json')); keep = [e for e in m['elements'] if not any(centre_in(e, b) for b in boxes)]
        for kind, (x, y, w, hh), text in NEW[key]:
            e = {'kind': kind, 'x': x, 'y': y, 'w': w, 'h': hh}
            if text: e['text'] = text
            if kind == 'chip': e.update(CHIP[key])
            keep.append(e)
        json.dump({'state': m.get('state', 'rest'), 'elements': keep}, open(D + key + '.manifest.json', 'w'), indent=1)
    r = subprocess.run(['bun', R + 'tools/hud-budget/cli.ts', D], capture_output=True, text=True, cwd=R); print(r.stdout[-5000:], r.stderr[-2000:])
    open(OUT3 + 'make/budget.txt', 'w').write(r.stdout)


if __name__ == '__main__':
    main()
