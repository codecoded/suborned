import sys
from PIL import Image, ImageDraw, ImageFont
d, out = sys.argv[1], sys.argv[2]
views = sys.argv[3:]
def font(sz):
    for p in ['/System/Library/Fonts/Supplemental/Arial Bold.ttf','/System/Library/Fonts/Helvetica.ttc']:
        try: return ImageFont.truetype(p, sz)
        except Exception: pass
    return ImageFont.load_default()
W = 960
rows = []
for v in views:
    ims = []
    for sfx, lab in (('', 'day'), ('-evening', 'evening'), ('-night', 'night')):
        im = Image.open(f'{d}/road-enamel-{v}{sfx}.jpg').convert('RGB')
        h = int(im.height * W / im.width); im = im.resize((W, h), Image.LANCZOS)
        dr = ImageDraw.Draw(im); dr.rectangle([0, 0, 190, 40], fill=(23, 48, 47)); dr.text((10, 5), f'{lab}', fill=(247, 244, 236), font=font(26))
        ims.append(im)
    rows.append(ims)
H = sum(r[0].height for r in rows)
b = Image.new('RGB', (W * 3 + 8, H + 4 * (len(rows) - 1)), (23, 48, 47))
y = 0
for r in rows:
    for i, im in enumerate(r): b.paste(im, (i * (W + 4), y))
    y += r[0].height + 4
b.save(out, quality=90)
print(out, b.size)
