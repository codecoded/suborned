"""Brief 101 steps 4-6 (the roster, the scope, the street layer): the real captures beside Glyph's S mocks, and how much of the screen the HUD takes, measured the way
tools/hud-budget reads a capture (the HUD layer alone, alpha >= 24, at 1920 x 1080: keylines and shadows count; the ground marks are world meshes, so they don't).
  side-<moment>.png   this build | the S mock, each 960 x 540, the coverage under each
  close-<moment>.png  the roster and the scope at 2x: this build's 3840 x 2160 capture beside the mock's (upscaled 2x)
  coverage-r4.json    the table: the mock, the build before steps 4-6 (round 3's captures, 0.2.280), this build
Latch. Usage: python3 -I r4.py <this build's shots dir> <build label> [<proof dir with r101-first-shot>]"""
import json, os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont

R = '/Users/sky/Projects/suborned/'
OUT = R + 'briefs/101/latch/r4/'
MOCK = R + 'out/scratch/glyph/101r2/budget/'          # Glyph's mocks with their HUD layers (r2_budget.py)
BEFORE = R + 'out/scratch/latch/101/shots/'           # round 3's captures (0.2.280: steps 1-3, the old roster and scope)
ALPHA = 24
MOMENTS = [  # (key, this round's shot, round 3's shot, the mock, what it is)
    ('rest', 't101-rest', 't101-rest-b', 'S-rest', 'At rest: route G, 2:25'),
    ('quiet', 't101-quiet', 't101-quiet', 'S-quiet', 'Quiet: route G, 3:28.6, a guard radioing it in, our car boxed in'),
    ('loud', 't101-loud', 't101-loud', 'S-loud', 'The seal with police: loud run seed 3, 2:34.5'),
    ('aim', 'r101-first-shot', 't101-aim', 'S-aim', "The first shot: the Enforcer drawn, a right hold on the guard (the r101 proof's picture)"),
    ('busy', 't101-busy-aim', 't101-busy', 'S-busy', 'The busiest moment: the final fight, 3:47.5, the pointer on the blocker'),
]


def font(size, bold=False):
    for f in (R + 'unity/Assets/Suborned/UI/Resources/Kit/fonts/' + ('Jost-Bold.ttf' if bold else 'Jost-Medium.ttf'),):
        if os.path.exists(f): return ImageFont.truetype(f, size)
    return ImageFont.load_default()


def cover(hud):
    if not os.path.exists(hud): return None
    a = np.array(Image.open(hud).convert('RGBA').resize((1920, 1080), Image.LANCZOS))[:, :, 3]
    return round(float((a >= ALPHA).mean()) * 100, 2)


def side(shots, key, now, mock, title, label, cov):
    W, H = 1920, 540 + 120; im = Image.new('RGB', (W, H), (23, 48, 47)); d = ImageDraw.Draw(im)
    d.text((16, 14), f'{title}', font=font(30, True), fill=(247, 244, 236))
    for i, (path, cap) in enumerate(((shots + now + '.png', f'this build {label}: {cov["now"]} %'), (MOCK + mock + '.png', f'Glyph\'s S mock: {cov["mock"]} %'))):
        if os.path.exists(path): im.paste(Image.open(path).convert('RGB').resize((960, 540), Image.LANCZOS), (i * 960, 60))
        d.text((i * 960 + 16, 60 + 540 + 14), cap, font=font(26), fill=(247, 244, 236))
    os.makedirs(OUT, exist_ok=True); im.save(OUT + f'side-{key}.png')


def close(shots, key, now2x, mock, label):
    """The roster (0..200 x 0..420) and the scope (0..220 x 860..1080) at 2x: the build's 3840 capture beside the mock's 1920 one upscaled."""
    if not os.path.exists(shots + now2x + '.png'): return
    a = Image.open(shots + now2x + '.png').convert('RGB'); m = Image.open(MOCK + mock + '.png').convert('RGB').resize((3840, 2160), Image.LANCZOS)
    boxes = [(0, 0, 420, 860), (0, 1700, 440, 2160)]
    W = 2 * 440 + 60; Hh = 860 + 460 + 140; im = Image.new('RGB', (W, Hh), (23, 48, 47)); d = ImageDraw.Draw(im)
    d.text((16, 12), f'The faces and the lens at 2x: {label} (left) · the S mock (right)', font=font(24, True), fill=(247, 244, 236))
    y = 60
    for (x0, y0, x1, y1) in boxes:
        im.paste(a.crop((x0, y0, x1, y1)), (10, y)); im.paste(m.crop((x0, y0, x1, y1)), (10 + 440 + 40, y)); y += (y1 - y0) + 20
    os.makedirs(OUT, exist_ok=True); im.save(OUT + f'close-{key}.png')


def main():
    shots = sys.argv[1].rstrip('/') + '/'; label = sys.argv[2] if len(sys.argv) > 2 else 'this build'
    proof = sys.argv[3].rstrip('/') + '/' if len(sys.argv) > 3 else shots  # the proof's pictures (the first shot's hold: a shot can't hold a button)
    rows = {}
    for key, now, before, mock, title in MOMENTS:
        src = proof if now.startswith('r101-') else shots
        cov = {'mock': cover(MOCK + mock + '.hud.png'), 'before': cover(BEFORE + before + '.hud.png'), 'now': cover(src + now + '.hud.png')}
        rows[key] = dict(cov, shot=now, beforeShot=before, mockShot=mock, what=title)
        side(src, key, now, mock, title, label, cov)
    close(shots, 'rest', 't101-rest-2x', 'S-rest', label); close(shots, 'busy', 't101-busy-2x', 'S-busy', label)
    doc = {'method': 'the HUD layer alone (the capture\'s .hud.png), alpha >= 24, at 1920 x 1080: keylines and shadows count; ground marks are world meshes and do not',
           'build': label, 'before': 'round 3 (0.2.280): steps 1-3 built, the old roster, scope and street', 'mock': 'Glyph round 2, 1 · Street labels (briefs/101/glyph/r2/S-*.png)', 'rows': rows}
    json.dump(doc, open(R + 'briefs/101/latch/coverage-r4.json', 'w'), indent=1)
    for k, v in rows.items(): print(f"{k:6} mock {v['mock']} % · before {v['before']} % · now {v['now']} %")


if __name__ == '__main__':
    main()
