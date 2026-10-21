"""Contact sheet from cell_review.py frames: python3 -I cell_sheet.py <dir> <out.png> <take,...> <phases>"""
import sys
from PIL import Image, ImageDraw
d, out, takes, n = sys.argv[1], sys.argv[2], sys.argv[3].split(','), int(sys.argv[4])
im0 = Image.open(f'{d}/{takes[0]}_0.png'); w, h = im0.size
S = Image.new('RGB', (80 + w * n, h * len(takes)), (240, 236, 226)); dr = ImageDraw.Draw(S)
for r, t in enumerate(takes):
    dr.text((4, r * h + h // 2), t, fill=(0, 0, 0))
    for i in range(n): S.paste(Image.open(f'{d}/{t}_{i}.png').convert('RGB'), (80 + i * w, r * h))
S.save(out)
