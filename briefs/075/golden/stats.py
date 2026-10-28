# Brief 075 golden view (Latch): the frame stats Glint used (briefs/075/golden/steps.json), as JSON on stdout.
# satMean: HSV saturation (max - min) / max on 0..255, mean over every pixel; luma: Rec. 601 (0.299 R + 0.587 G + 0.114 B), median, p5, p95,
# and the medians of the left and right thirds. Reproduces Glint's numbers for before.png (75.0 / 129.5 / 47.1 / 224.9 / 192.6 / 73.6).
# usage: python3 -I briefs/075/golden/stats.py <png>
import json, sys
import numpy as np
from PIL import Image

a = np.asarray(Image.open(sys.argv[1]).convert('RGB'), dtype=np.float32)
mx, mn = a.max(-1), a.min(-1)
sat = np.where(mx > 0, (mx - mn) / np.maximum(mx, 1e-6), 0) * 255
luma = a @ np.array([0.299, 0.587, 0.114], dtype=np.float32)
w = luma.shape[1]
r = lambda v: round(float(v), 1)
print(json.dumps({'satMean': r(sat.mean()), 'lumaMedian': r(np.median(luma)), 'p5': r(np.percentile(luma, 5)), 'p95': r(np.percentile(luma, 95)),
                  'leftThirdMedian': r(np.median(luma[:, : w // 3])), 'rightThirdMedian': r(np.median(luma[:, 2 * w // 3 :]))}))
