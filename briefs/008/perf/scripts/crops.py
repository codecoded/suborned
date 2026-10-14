# B6 (Latch): before/after crops for Vela and Glint. Per fix, one sheet: a row per stop, [base | fix | |diff| x8], each a
# native-resolution window where the fix changes the image most. Usage: python3 crops.py shots_dir out_dir [reference variant]
import sys, os, re
from PIL import Image, ImageChops, ImageOps, ImageDraw

src, dst = sys.argv[1], sys.argv[2]; os.makedirs(dst, exist_ok=True)
REF = sys.argv[3] if len(sys.argv) > 3 else 'base'  # the reference column: `as-was` for runs since B6's second pass
W, H, STEP = 480, 270, 30
shots = {}
for f in sorted(os.listdir(src)):
    m = re.match(r'\d+-(.+)-(as-was|base|ssao-depth|ssao-off|proxies|cascades-2|fixes-13|fixes-123|fade-off|shadows-off)\.png$', f)
    if m: shots.setdefault(m[1], {}).setdefault(m[2], os.path.join(src, f))

def window(diff):
    g = diff.convert('L'); w, h = g.size; small = g.resize((w // STEP, h // STEP), Image.BOX)
    px = small.load(); bw, bh = W // STEP, H // STEP; best = (-1, 0, 0)
    for y in range(small.size[1] - bh + 1):
        for x in range(small.size[0] - bw + 1):
            s = sum(px[x + i, y + j] for i in range(bw) for j in range(bh))
            if s > best[0]: best = (s, x * STEP, y * STEP)
    return best[1], best[2]

for fix in [v for v in ('base', 'ssao-depth', 'ssao-off', 'proxies', 'cascades-2', 'fixes-13', 'fixes-123') if v != REF]:
    stops = [s for s in shots if REF in shots[s] and fix in shots[s]]
    if not stops: continue
    sheet = Image.new('RGB', (W * 3, H * len(stops)), 'white'); d = ImageDraw.Draw(sheet)
    for row, stop in enumerate(stops):
        a = Image.open(shots[stop][REF]).convert('RGB'); b = Image.open(shots[stop][fix]).convert('RGB')
        diff = ImageChops.difference(a, b); x, y = window(diff); box = (x, y, x + W, y + H)
        amp = Image.eval(diff.crop(box), lambda v: min(255, v * 8))
        for col, im in enumerate((a.crop(box), b.crop(box), ImageOps.invert(amp))): sheet.paste(im, (col * W, row * H))
        d.text((6, row * H + 4), f'{stop} {REF} @({x},{y})', fill='black'); d.text((W + 6, row * H + 4), fix, fill='black'); d.text((2 * W + 6, row * H + 4), '|diff| x8 (inverted)', fill='black')
    sheet.save(os.path.join(dst, f'crops-{fix}.jpg'), quality=92)
    print(fix, len(stops), 'stops')
