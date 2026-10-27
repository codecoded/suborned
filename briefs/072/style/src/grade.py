"""072-B follow-up: the DecoKit street grade approximated as a post step over the Blender renders, and the old-city reference frames.
Run: python3 -I grade.py   (reads out/scratch/vela-style/shots/final, writes out/scratch/vela-style/grade and .../html)

An APPROXIMATION of a grade Glint would set in Unity, not the game's renderer. It follows the URP order on display-decoded values:
  postExposure: colour *= 2^EV (UberPost, linear, before the LUT)
  contrast:     in ARRI LogC around ACEScc mid grey 0.4135884, factor 1 + contrast/100 (LutBuilderHdr)
  saturation:   luma (Rec.709, linear) + (1 + saturation/100) * (colour - luma) (LutBuilderHdr)
The Blender frames are already AgX tone-mapped, so the grade lands after the tone map instead of before ACES: it is stronger in
highlights than URP would be. Both grades (today's and DecoKit's) go through the same pass, so the pair shows only their difference.
Exposure: today light.json sun 1.8, postExposure +0.15; the old DecoKit street rig (DecoKitStreetDemo.cs) sun 3, postExposure -0.8.
Lit-surface exposure is matched on sun x 2^EV: DecoKit -0.8 + log2(3 / 1.8) = -0.06 EV against today's +0.15 (a net -0.21 EV).
"""
import json, math, os
import numpy as np
from PIL import Image, ImageDraw, ImageFont

R = "/Users/sky/Projects/suborned/"
S = R + "out/scratch/vela-style/"
F, G, H = S + "shots/final/", S + "grade/", S + "html/"
OLD = R + "briefs/026/old-decokit/"
os.makedirs(G, exist_ok=True)

SUN_MATCH = math.log2(3.0 / 1.8)
GRADES = {
    "today": {"postExposure": 0.15, "contrast": 6, "saturation": 18, "src": "unity/Assets/Suborned/Editor/World/light.json post.colour"},
    "decokit": {"postExposure": -0.8 + SUN_MATCH, "contrast": 10, "saturation": 25, "raw_postExposure": -0.8,
                "src": "the-suborned game/Assets/TheSuborned/Art/DecoKit/Generated/DK_StreetGrade.asset (sun 3 in DecoKitStreetDemo.cs)"},
}

def s2l(c): return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)
def l2s(c): c = np.clip(c, 0, 1); return np.where(c <= 0.0031308, c * 12.92, 1.055 * c ** (1 / 2.4) - 0.055)
# ARRI LogC (EI 800), the parameters URP's Color.hlsl uses
CUT, A, B, C, D, E, Fp = 0.011361, 5.555556, 0.047996, 0.244161, 0.386036, 5.301883, 0.092819
def to_logc(x): return np.where(x > CUT, C * np.log10(A * np.maximum(x, 0) + B) + D, E * x + Fp)
def from_logc(x): return np.where(x > E * CUT + Fp, (10 ** ((x - D) / C) - B) / A, (x - Fp) / E)
MID = 0.4135884
LUMA = np.array([0.2126, 0.7152, 0.0722])

def grade(rgb8, g):
    x = s2l(rgb8.astype(np.float64) / 255) * 2 ** g["postExposure"]
    x = from_logc((to_logc(x) - MID) * (1 + g["contrast"] / 100) + MID)
    y = (x @ LUMA)[..., None]
    x = y + (1 + g["saturation"] / 100) * (x - y)
    return (l2s(x) * 255 + 0.5).astype(np.uint8)

def stats(rgb8):
    v = rgb8.astype(np.float64) / 255; mx, mn = v.max(-1), v.min(-1)
    sat = np.where(mx > 0, (mx - mn) / np.maximum(mx, 1e-6), 0)
    return {"mean_sat": round(float(sat.mean()), 3), "p10_value": round(float(np.percentile(mx, 10)), 3), "mean_value": round(float(mx.mean()), 3)}

def load(p): return np.asarray(Image.open(p).convert("RGB"))

FONT = ImageFont.truetype("/System/Library/Fonts/Helvetica.ttc", 21, index=1)
def label(im, text):
    d = ImageDraw.Draw(im); x0, y0, x1, y1 = d.textbbox((0, 0), text, font=FONT)
    d.rectangle((10, 10, 10 + (x1 - x0) + 16, 10 + (y1 - y0) + 14), fill=(23, 48, 47)); d.text((18 - x0, 17 - y0), text, font=FONT, fill=(247, 244, 236))
    return im
def pair(left, right, out, w=800, h=450):
    m = Image.new("RGB", (2 * w + 8, h), (247, 244, 236))
    for i, (a, t) in enumerate((left, right)):
        im = Image.fromarray(a) if isinstance(a, np.ndarray) else a
        m.paste(label(im.convert("RGB").resize((w, h), Image.LANCZOS), t), (i * (w + 8), 0))
    m.save(H + out, quality=86, optimize=True); return out

report = {"method": __doc__.strip(), "grades": GRADES, "frames": {}}
# 1. loosened (today's grade) | loosened (DecoKit grade), same render, two grades
for cam in ("cafe-play-30", "office-play-210", "district-play-30"):
    a = load(F + f"{cam}__after.png"); t, d = grade(a, GRADES["today"]), grade(a, GRADES["decokit"])
    Image.fromarray(d).save(G + f"{cam}__after_dkgrade.png"); Image.fromarray(t).save(G + f"{cam}__after_todaygrade.png")
    pair((t, "Loosened, today's grade"), (d, "Loosened, DecoKit grade (approx.)"), f"{cam}-grade.jpg")
    report["frames"][cam] = {"loosened_ungraded": stats(a), "loosened_today_grade": stats(t), "loosened_decokit_grade": stats(d)}
# 2. the road row kept: loosened + asphalt, both grades (the combined look)
for cam in ("office-play-210", "district-play-30"):
    p = load(F + f"{cam}__plus.png"); t, d = grade(p, GRADES["today"]), grade(p, GRADES["decokit"])
    Image.fromarray(d).save(G + f"{cam}__plus_dkgrade.png")
    pair((t, "Loosened + asphalt, today's grade"), (d, "Loosened + asphalt, DecoKit grade (approx.)"), f"{cam}-road-grade.jpg")
    report["frames"][cam].update({"loosened_asphalt_today_grade": stats(t), "loosened_asphalt_decokit_grade": stats(d)})
# 3. the old DecoKit city, brief 026 captures at the same play-zoom cameras (no new renders)
for a, b, out in (("cafe-play-30", "crossing-play-210", "old-decokit-play.jpg"),):
    pair((Image.open(OLD + a + ".png"), "Old DecoKit city: " + a), (Image.open(OLD + b + ".png"), "Old DecoKit city: " + b), out)
    for n in (a, b): report["frames"]["old-decokit/" + n] = stats(load(OLD + n + ".png"))
json.dump(report, open(G + "grade.report.json", "w"), indent=1)
print(json.dumps(report["frames"], indent=1))
