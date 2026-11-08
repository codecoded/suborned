"""Brief 101 round 3 (Glyph's r3 board, Sky's yes): this build's captures beside Glyph's r3 frames, and how much of the screen the HUD takes, measured the way
tools/hud-budget reads a capture (the HUD layer alone, alpha >= 24, at 1920 x 1080: keylines and shadows count; ground marks are world meshes, so they don't).
  side-<frame>.png    this build | Glyph's r3 frame, each 960 x 540, the coverage under each
  crops-<frame>.png   the frame's new pieces at 2x (a 3840 x 2160 capture where the shots have one, else the 1080 picture upscaled), beside the mock's
  coverage-r5.json    the table: Glyph's "today" (0.2.286/0.2.287), his mock, this build
Latch. Usage: python3 -I r5.py <shots dir> <proof dir> <build label>"""
import json, os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont

R = '/Users/sky/Projects/suborned/'
OUT = R + 'briefs/101/latch/r5/'
MOCK = R + 'briefs/101/glyph/r3/'                       # Glyph's five frames (1920 x 1080)
MOCKHUD = R + 'out/scratch/glyph/101r3/budget/'        # their HUD layers (r3_budget.py)
GCOV = R + 'briefs/101/glyph/r3/make/coverage.json'    # his today / mock shares per frame
ALPHA = 24
# (frame, where this build's picture is: 'shots' or 'proof', its name, what it is)
FRAMES = [
    ('quiet', 'shots', 't101-quiet', 'Quiet: route G, 3:28.6 (a guard radios in the knocked-out guard; the square camera on the Tech)'),
    ('window', 'proof', 'done-call-window', "The call lands: the guard's away window and the Fixer's chip (the proof's done-call-window)"),
    ('card', 'proof', 'r123-body-safe', "r123's moment: Inez's card waits (no dock beside the news)"),
    ('conv', 'proof', 'r085-conv-cards', "Inez's card after the conversation, on her (r085's proof)"),
    ('ring', 'proof', 'r101-ring-plates', 'The act ring on Inez (r101 proof)'),
]
# crops at 2x: (frame, box at 1080 (x0, y0, x1, y1), what)
CROPS = {
    'quiet': [((380, 40, 640, 280), 'the door: tabs, the capsule; the attention clock'), ((840, 160, 1060, 460), 'the callout, Camera on, the report')],
    'window': [((820, 200, 1110, 360), "the done chip and the window's timed tab"), ((900, 460, 1000, 540), 'the door marker')],
    'conv': [((760, 210, 1160, 440), "the slip on Inez")],
    'ring': [((820, 340, 1260, 640), 'the ring: labels, the cream rim (this build opened it at the pointer, lower right)', (1300, 680, 1740, 980))],
    'card': [((1740, 90, 1920, 170), "the key faces: Inez's dot")],
}


def font(size, bold=False):
    f = R + 'unity/Assets/Suborned/UI/Resources/Kit/fonts/' + ('Jost-Bold.ttf' if bold else 'Jost-Medium.ttf')
    return ImageFont.truetype(f, size) if os.path.exists(f) else ImageFont.load_default()


def cover(hud):
    if not os.path.exists(hud): return None
    a = np.array(Image.open(hud).convert('RGBA').resize((1920, 1080), Image.LANCZOS))[:, :, 3]
    return round(float((a >= ALPHA).mean()) * 100, 2)


def side(key, now_png, what, label, cov):
    W, H = 1920, 540 + 120; im = Image.new('RGB', (W, H), (23, 48, 47)); d = ImageDraw.Draw(im)
    d.text((16, 14), what, font=font(28, True), fill=(247, 244, 236))
    for i, (path, cap) in enumerate(((now_png, f'this build {label}: {cov["now"]} %'), (MOCK + key + '.png', f"Glyph's r3 frame: {cov['mock']} % (today {cov['today']} %)"))):
        if path and os.path.exists(path): im.paste(Image.open(path).convert('RGB').resize((960, 540), Image.LANCZOS), (i * 960, 60))
        d.text((i * 960 + 16, 60 + 540 + 14), cap, font=font(26), fill=(247, 244, 236))
    os.makedirs(OUT, exist_ok=True); im.save(OUT + f'side-{key}.png')


def crops(key, now_png, now2x, label):
    if key not in CROPS: return
    a = Image.open(now2x).convert('RGB') if now2x and os.path.exists(now2x) else Image.open(now_png).convert('RGB').resize((3840, 2160), Image.LANCZOS)
    m = Image.open(MOCK + key + '.png').convert('RGB').resize((3840, 2160), Image.LANCZOS)
    rows = []
    for item in CROPS[key]:
        (x0, y0, x1, y1), what = item[0], item[1]; own = item[2] if len(item) > 2 else item[0]
        b = (x0 * 2, y0 * 2, x1 * 2, y1 * 2); bo = tuple(v * 2 for v in own); rows.append((a.crop(bo), m.crop(b), what))
    W = max(r[0].width for r in rows) * 2 + 60; Hh = sum(r[0].height + 50 for r in rows) + 60
    im = Image.new('RGB', (W, Hh), (23, 48, 47)); d = ImageDraw.Draw(im); y = 10
    d.text((16, y), f'{key} at 2x: {label}{" (3840 capture)" if now2x and os.path.exists(now2x) else " (1080 picture upscaled)"} | Glyph\'s mock', font=font(24, True), fill=(247, 244, 236)); y += 46
    for n, mm, what in rows:
        d.text((16, y), what, font=font(20), fill=(247, 244, 236)); y += 28
        im.paste(n, (10, y)); im.paste(mm, (10 + n.width + 40, y)); y += n.height + 22
    im.save(OUT + f'crops-{key}.png')


def main():
    shots = sys.argv[1].rstrip('/') + '/'; proof = sys.argv[2].rstrip('/') + '/'; label = sys.argv[3] if len(sys.argv) > 3 else 'this build'
    g = json.load(open(GCOV))['frames']; rows = {}
    for key, src, name, what in FRAMES:
        base = (shots if src == 'shots' else proof) + name
        cov = {'today': g[key]['before_pct'], 'mock': g[key]['after_pct'], 'mockLayer': cover(MOCKHUD + key + '.hud.png'), 'now': cover(base + '.hud.png')}
        rows[key] = dict(cov, picture=os.path.relpath(base + '.png', R), what=what)
        side(key, base + '.png', what, label, cov); crops(key, base + '.png', (shots + name + '-2x.png') if src == 'shots' else None, label)
    doc = {'method': "the HUD layer alone (the capture's .hud.png), alpha >= 24, at 1920 x 1080 (tools/hud-budget's reading): keylines and shadows count, ground marks do not",
           'build': label, 'today': "Glyph's 'today' (0.2.286 / 0.2.287), briefs/101/glyph/r3/make/coverage.json", 'mock': "Glyph's r3 frames, the same file", 'rows': rows}
    json.dump(doc, open(R + 'briefs/101/latch/coverage-r5.json', 'w'), indent=1)
    for k, v in rows.items(): print(f"{k:7} today {v['today']} % · mock {v['mock']} % · now {v['now']} %")


if __name__ == '__main__':
    main()
