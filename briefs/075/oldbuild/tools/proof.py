# proof.png: the old reel's t=20 still (left) | the old builders run in our project (middle) | |difference| x8 (right),
# with the pixel-difference numbers written to proof.json.
import sys, json
import numpy as np
from PIL import Image, ImageDraw
old_p, new_p, out_png, out_json = sys.argv[1:5]
a = np.asarray(Image.open(old_p).convert('RGB')).astype(np.int16)
b = np.asarray(Image.open(new_p).convert('RGB')).astype(np.int16)
assert a.shape == b.shape, (a.shape, b.shape)
d = np.abs(a - b); px = d.max(axis=2)
m = {
    'old': old_p, 'new': new_p, 'size': [a.shape[1], a.shape[0]],
    'mean_abs_diff_0_255': round(float(d.mean()), 4),
    'rmse_0_255': round(float(np.sqrt((d.astype(np.float64) ** 2).mean())), 4),
    'psnr_db': round(float(20 * np.log10(255 / max(np.sqrt((d.astype(np.float64) ** 2).mean()), 1e-9))), 2),
    'max_abs_diff': int(d.max()),
    'pixels_identical_pct': round(float((px == 0).mean() * 100), 3),
    'pixels_diff_gt_2_pct': round(float((px > 2).mean() * 100), 3),
    'pixels_diff_gt_8_pct': round(float((px > 8).mean() * 100), 3),
    'pixels_diff_gt_32_pct': round(float((px > 32).mean() * 100), 3),
}
json.dump(m, open(out_json, 'w'), indent=1); print(json.dumps(m, indent=1))
w, h = a.shape[1], a.shape[0]; s = 2  # half size per panel
heat = np.clip(d * 8, 0, 255).astype(np.uint8)
panels = [Image.fromarray(x.astype(np.uint8)).resize((w // s, h // s), Image.LANCZOS) for x in (a, b, heat)]
canvas = Image.new('RGB', (3 * w // s, h // s + 40), (20, 20, 20))
for i, p in enumerate(panels): canvas.paste(p, (i * w // s, 40))
dr = ImageDraw.Draw(canvas)
labels = ['old reel t=20 (briefs/074/flyby/old-stills/flyby_t20.png)', 'old builders in our project (OldTownProof, t=20)',
          f"|diff| x8  mean {m['mean_abs_diff_0_255']}/255  PSNR {m['psnr_db']} dB  >8: {m['pixels_diff_gt_8_pct']}%"]
for i, t in enumerate(labels): dr.text((i * w // s + 10, 12), t, fill=(240, 240, 240))
canvas.save(out_png)
