"""Contact sheet of the chosen clips: three frames of each on the Enforcer (python3 -I tools/figures/clipsheet.py <shots dir> <manifest.json> <out.png>)."""
import sys, json
from PIL import Image, ImageDraw
shots, man, out = sys.argv[1:4]
clips = json.load(open(man))['clips']
W, H, cols = 104, 138, 4
rws = (len(clips) + cols - 1) // cols
img = Image.new('RGB', (cols * (W * 3 + 8), rws * (H + 28)), (214, 214, 204)); d = ImageDraw.Draw(img)
for k, c in enumerate(clips):
    cx = (k % cols) * (W * 3 + 8); cy = (k // cols) * (H + 28)
    d.text((cx + 3, cy + 2), f"{c['name']}  {c['seconds']} s{'  loop' if c['loop'] else ''}", fill=(0, 0, 0))
    d.text((cx + 3, cy + 14), c['use'][:52], fill=(70, 70, 70))
    for j, f in enumerate((0.20, 0.55, 0.85)):
        try: im = Image.open(f"{shots}/enforcer_{c['name']}_{f:.2f}_front.png").convert('RGB').resize((W, H))
        except Exception: continue
        img.paste(im, (cx + j * W, cy + 28))
img.save(out); print(out, img.size)
