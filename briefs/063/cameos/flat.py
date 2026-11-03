# Glyph, brief 063 (2026-10-25): the painted cameos on the roster's grey ground, ring-less and full-bleed square, for the HUD's Cameo() helper
# (which draws the chrome ring itself: 2 px #cbc6c3 over 1 px ink-deep, so the painted ring and its cream paper band are removed here).
#   python3 -I briefs/063/cameos/flat.py [names]  -> briefs/063/cameos/flat/{vale_C,inez_A,driver_A}_face{,@2x,@4x}.png  (32 / 64 / 128 px); names default to the three; B's vale_B_face* stay in flat/ (run `flat.py vale_B` to remake)
# The ground is #b3b0ad (179,176,173): the roster's cameo and the model renders' own ground (briefs/063/cameos/model_*.png).
from PIL import Image
import numpy as np, cv2, os
HERE = os.path.dirname(os.path.abspath(__file__)); GREY = np.array([179, 176, 173], np.float32)
import sys
for n in (sys.argv[1:] or ["vale_C", "inez_A", "driver_A"]):
    a = np.array(Image.open(os.path.join(HERE, n + ".png")).convert("RGBA")); rgb = a[..., :3]
    lab = cv2.cvtColor(rgb, cv2.COLOR_RGB2LAB).astype(np.float32); ys, xs = np.mgrid[0:512, 0:512]; r = np.hypot(xs - 255.5, ys - 255.5)
    g = np.median(lab[(r > 205) & (r < 222) & (ys < 200) & (xs < 200)], axis=0); d = np.linalg.norm(lab - g, axis=2)
    cand = (((d < 14) & (r < 238)) | ((d < 30) & (r > 218) & (r < 238))).astype(np.uint8)
    k, lbl = cv2.connectedComponents(cand); edge = np.unique(lbl[(r > 205) & (r < 238) & (cand > 0)]); mask = np.isin(lbl, edge[edge > 0]).astype(np.float32)
    if n == 'vale_C':
        # the ivory jacket is paper-cream, so the flood eats the shoulders: keep a hand-set polygon below the collar's lower edge (the ring covers the rest)
        keep = np.zeros((512, 512), np.uint8)
        cv2.fillPoly(keep, [np.array([(60, 430), (96, 396), (150, 424), (273, 479), (300, 480), (338, 494), (338, 512), (60, 512)], np.int32)], 1)
        mask[keep > 0] = 0.0
    mask[r >= 238] = 1.0                                             # outside the paper band: the dark ring and the corners are ground too
    # the figure's own clipped edge (shoulders) stays: the ring covers it
    soft = cv2.GaussianBlur(mask, (0, 0), 1.1)[..., None]; out = (rgb.astype(np.float32) * (1 - soft) + GREY * soft)
    # the dark painted ring pixels (r in 238..256) are replaced above; shoulders reaching r 232..238 keep their own paint
    im = Image.fromarray(np.clip(out, 0, 255).astype("uint8"), "RGB")
    # full-bleed: crop to the square inscribed 2 px inside the disc's ring, the hand of the ring covers the rest
    c = im.crop((8, 8, 504, 504))
    for nm, px in (("", 32), ("@2x", 64), ("@4x", 128)):
        c.resize((px, px), Image.LANCZOS).save(os.path.join(HERE, "flat", n + "_face" + nm + ".png"))
print("ok")
