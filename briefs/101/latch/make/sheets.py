"""Brief 101, Sky's mix: each moment's real capture with its two corners beside Glyph's round 2 mocks' (S: street labels, T: corner tokens), so Sky can see which
corner reads better on the game's own ground ("street labels top right but tokens bottom right perhaps": the capture decides). One sheet per moment, 1920 wide:
the capture at 1080, then the top right (now | S | T) and the bottom right (now | S | T) at 1.28x. The corners of this build come from the 2x capture (true 2x,
scaled down); the mocks' from their 1080 pictures. Latch. Usage: python3 -I sheets.py <shots dir> <out dir> <build>"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

R = '/Users/sky/Projects/suborned/'
MOCK = R + 'briefs/101/glyph/r2/'
FONT = R + 'unity/Assets/Suborned/UI/Resources/Kit/fonts/Jost-SemiBold.ttf'
MOMENTS = {  # name: (capture at 1080, its 2x capture or None, the mocks' moment, caption)
    'rest': ('t101-rest-b', 't101-rest-2x', 'rest', 'At rest (route G, 2:25; the mocks: 3:28 with nothing on)'),
    'loud': ('t101-loud', 't101-loud-2x', 'loud', 'The seal with police coming (loud run seed 3, 2:34.5)'),
    'busy': ('t101-busy', 't101-busy-2x', 'busy', 'The busiest moment: the final fight (loud run seed 3, 3:47.5)'),
    'quiet': ('t101-quiet', None, 'quiet', 'Quiet: our car boxed in (route G, 3:28.6)'),
}
TR, BR = (1420, 0, 1920, 250), (1420, 950, 1920, 1080)


def corner(src2x, src1x, box, k=1.28):
    w, h = box[2] - box[0], box[3] - box[1]
    if src2x: return src2x.crop(tuple(v * 2 for v in box)).resize((round(w * k), round(h * k)), Image.LANCZOS)
    return src1x.crop(box).resize((round(w * k), round(h * k)), Image.LANCZOS)


def sheet(shots, out, build, key):
    cap, cap2, m, caption = MOMENTS[key]
    now = Image.open(os.path.join(shots, cap + '.png')).convert('RGB'); now2 = Image.open(os.path.join(shots, cap2 + '.png')).convert('RGB') if cap2 else None
    S = Image.open(MOCK + f'S-{m}.png').convert('RGB'); T = Image.open(MOCK + f'T-{m}.png').convert('RGB')
    f = ImageFont.truetype(FONT, 26); fs = ImageFont.truetype(FONT, 20)
    rows = [(TR, 'Top right'), (BR, 'Bottom right')]
    tiles = [[corner(now2, now, b), corner(None, S, b), corner(None, T, b)] for b, _ in rows]
    H = 60 + 1080 + sum(40 + t[0].height + 10 for t in tiles) + 20
    sh = Image.new('RGB', (1920, H), (24, 30, 30)); g = ImageDraw.Draw(sh)
    g.text((20, 16), f'{caption} · build {build} (old town)', fill=(247, 244, 236), font=f); sh.paste(now, (0, 60)); y = 60 + 1080 + 10
    for (b, name), t in zip(rows, tiles):
        for i, lab in enumerate([f'{name}: this build', f'{name}: 1 · Street labels (S)', f'{name}: 2 · Corner tokens (T)']):
            hot = (i == 1 and b == TR) or (i == 2 and b == BR)  # Sky's mix: S top right, T bottom right
            g.text((i * 640 + 12, y + 8), lab + ('  · the mix' if hot else ''), fill=(232, 98, 44) if hot else (176, 205, 199), font=fs)
            sh.paste(t[i], (i * 640, y + 40))
        y += 40 + t[0].height + 10
    sh.save(os.path.join(out, f'mix-{key}.png')); return os.path.join(out, f'mix-{key}.png')


if __name__ == '__main__':
    shots, out, build = sys.argv[1], sys.argv[2], sys.argv[3]; os.makedirs(out, exist_ok=True)
    for k in MOMENTS: print(sheet(shots, out, build, k))
