"""Brief 074-C (Latch): today against colourways, side by side (each half 960 x 540, labelled) -> briefs/074/shots/compare/.
Usage: python3 -I briefs/074/latch/compare.py <repo root>"""
import sys, os
from PIL import Image, ImageDraw, ImageFont
root = sys.argv[1]
b, a, o = (os.path.join(root, 'briefs', '074', 'shots', d) for d in ('before', 'after', 'compare'))
os.makedirs(o, exist_ok=True)
try: font = ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial Bold.ttf', 26)
except OSError: font = ImageFont.load_default()
names = sorted(n for n in os.listdir(a) if n.endswith('.png') and n != 'planets.png' and os.path.exists(os.path.join(b, n)))
for n in names:
    L, R = (Image.open(os.path.join(d, n)).convert('RGB').resize((960, 540), Image.LANCZOS) for d in (b, a))
    c = Image.new('RGB', (1928, 540), (11, 22, 21)); c.paste(L, (0, 0)); c.paste(R, (968, 0))
    d = ImageDraw.Draw(c)
    for x, t in ((12, 'today: one shared colourway'), (980, 'brief 074: colourway per building')):
        d.rectangle((x - 6, 8, x + d.textlength(t, font=font) + 6, 44), fill=(11, 22, 21)); d.text((x, 12), t, fill=(247, 244, 236), font=font)
    d.text((12, 506), n[:-4], fill=(247, 244, 236), font=font)
    c.save(os.path.join(o, n)); print(os.path.join(o, n))
# full-size pair of the wide view, stacked
if os.path.exists(os.path.join(a, 'wide-lamplight-y030.png')):
    L, R = (Image.open(os.path.join(d, 'wide-lamplight-y030.png')).convert('RGB') for d in (b, a))
    c = Image.new('RGB', (1920, 2168), (11, 22, 21)); c.paste(L, (0, 0)); c.paste(R, (0, 1088)); c.save(os.path.join(o, 'wide-lamplight-y030-full.png'))
