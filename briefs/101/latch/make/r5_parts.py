"""Brief 101 round 3: each new piece cropped on its own at 2x, from the parts the capture names (<picture>.parts.json: the HUD's own rects, HudToolkit.Parts), so the
crop check reads every piece apart, not only Glyph's five frames. A 3840 x 2160 capture is cropped as it is; a 1080 picture is cropped and doubled (LANCZOS).
Usage: python3 -I r5_parts.py <out.png> <dir>/<picture>:<part>[+<part>...][@pad] ...   (a part name may end in * to take every part it starts)
Latch."""
import json, os, sys
from PIL import Image, ImageDraw, ImageFont

FONT = '/Users/sky/Projects/suborned/unity/Assets/Suborned/UI/Resources/Kit/fonts/Jost-Bold.ttf'


def parts_of(pic):
    """The capture's named parts (HudToolkit.Parts), then its manifest's elements as 'm:<text>' and 'k:<kind>#<i>' (the manifest names the street marks the parts list leaves out)."""
    base = pic[:-4] if pic.endswith('.png') else pic; out = {}
    p = base + '.parts.json'
    if os.path.exists(p): out.update(json.load(open(p)))
    m = base + '.manifest.json'
    if os.path.exists(m):
        seen = {}
        for e in json.load(open(m)).get('elements', []):
            r = [e['x'], e['y'], e['w'], e['h']]; t = (e.get('text') or '').strip(); k = e['kind']; i = seen.get(k, 0); seen[k] = i + 1
            out[f'k:{k}#{i}'] = r
            if t: out.setdefault(f'm:{t}', r)
    return out


def main():
    out = sys.argv[1]; f = ImageFont.truetype(FONT, 22); tiles = []
    for arg in sys.argv[2:]:
        pad = 18
        if '@' in arg: arg, pad = arg.rsplit('@', 1); pad = int(pad)
        pic, names = arg.split(':', 1); png = pic if pic.endswith('.png') else pic + '.png'
        if not os.path.exists(png): tiles.append((f'{os.path.basename(pic)}: no picture', None)); continue
        im = Image.open(png).convert('RGB'); parts = parts_of(png); k = im.width / 1920.0
        for n in names.split('+'):
            keys = [x for x in parts if (x.startswith(n[:-1]) if n.endswith('*') else x == n)]
            if not keys: tiles.append((f'{os.path.basename(pic)}: {n} not on screen', None)); continue
            for key in keys:
                x, y, w, h = parts[key]
                if w < 1 or h < 1: continue
                s = 2.0 / k  # to 2x of 1080
                b = (max(0, int((x - pad * k))), max(0, int((y - pad * k))), min(im.width, int(x + w + pad * k)), min(im.height, int(y + h + pad * k)))
                if b[2] - b[0] < 2 or b[3] - b[1] < 2: tiles.append((f'{os.path.basename(pic)} · {key}: off the frame', None)); continue
                c = im.crop(b); c = c.resize((max(1, int(c.width * s)), max(1, int(c.height * s))), Image.LANCZOS)
                tiles.append((f'{os.path.basename(pic)} · {key}', c))
    W = max([t.width for _, t in tiles if t is not None] + [600]) + 20; H = sum((t.height if t is not None else 0) + 40 for _, t in tiles) + 10
    sheet = Image.new('RGB', (W, H), (23, 48, 47)); d = ImageDraw.Draw(sheet); y = 8
    for name, t in tiles:
        d.text((10, y), name, font=f, fill=(247, 244, 236)); y += 30
        if t is not None: sheet.paste(t, (10, y)); y += t.height + 10
    os.makedirs(os.path.dirname(out) or '.', exist_ok=True); sheet.save(out); print(out, len(tiles), 'tiles')


if __name__ == '__main__':
    main()
