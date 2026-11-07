"""scratch (Splice, brief 098): area-level difference between two shot folders: both shots shrunk 16x (box filter), so edges and sub-pixel motion
average out and only shading over areas remains (a wrong variant changes areas: no clouds, no lightmap, no fog). Max and mean over 16x16 cells.
usage: python3 -I areadiff.py <dirA> <dirB>"""
import sys, os, json
import numpy as np
from PIL import Image
a, b = sys.argv[1], sys.argv[2]; out = {}
for f in sorted(os.listdir(a)):
    if not f.endswith('.jpg') or not os.path.exists(os.path.join(b, f)): continue
    x = np.asarray(Image.open(os.path.join(a, f)).convert('RGB'), dtype=np.float32)
    y = np.asarray(Image.open(os.path.join(b, f)).convert('RGB'), dtype=np.float32)
    h, w = (x.shape[0] // 16) * 16, (x.shape[1] // 16) * 16
    sx = x[:h, :w].reshape(h // 16, 16, w // 16, 16, 3).mean(axis=(1, 3)); sy = y[:h, :w].reshape(h // 16, 16, w // 16, 16, 3).mean(axis=(1, 3))
    d = np.abs(sx - sy).max(axis=2)
    out[f[:-4]] = {"mean": round(float(d.mean()), 2), "p99": round(float(np.percentile(d, 99)), 1), "cells>8": round(float((d > 8).mean() * 100), 2)}
print(json.dumps(out))
