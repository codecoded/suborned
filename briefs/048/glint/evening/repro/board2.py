import sys
from PIL import Image, ImageDraw, ImageFont
d, out = sys.argv[1], sys.argv[2]; names = sys.argv[3:]
W = 960
def font(sz):
    for p in ['/System/Library/Fonts/Supplemental/Arial Bold.ttf','/System/Library/Fonts/Helvetica.ttc']:
        try: return ImageFont.truetype(p, sz)
        except Exception: pass
    return ImageFont.load_default()
rows = []
for n in names:
    ims = []
    for ph in ('day','evening','night'):
        im = Image.open(f'{d}/{ph}/{n}.jpg').convert('RGB'); im = im.resize((W, int(im.height*W/im.width)), Image.LANCZOS)
        dr = ImageDraw.Draw(im); dr.rectangle([0,0,200,40], fill=(23,48,47)); dr.text((10,5), ph, fill=(247,244,236), font=font(26)); ims.append(im)
    rows.append(ims)
H = sum(r[0].height for r in rows) + 4*(len(rows)-1)
b = Image.new('RGB', (W*3+8, H), (23,48,47)); y = 0
for r in rows:
    for i, im in enumerate(r): b.paste(im, (i*(W+4), y))
    y += r[0].height + 4
b.save(out, quality=90); print(out, b.size)
