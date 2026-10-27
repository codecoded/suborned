# Brief 072-G item 5 (Latch): the rebuild after Vela's palette, the L2 window holes and Glint's grade, against the textured v4 capture (072-F).
# Inputs: briefs/072/latch/textured/{offscreen,texmem}.json, briefs/072/texture/area-after.json (textured v4);
#         this folder: offscreen.json, texmem.json, area-g.json, heads-day.json; briefs/072/shots/f1/f1.json and f1g/f1.json.
# usage: python3 -I briefs/072/latch/g/report.py   → report.md beside it
import json, os
import numpy as np
from PIL import Image
here = os.path.dirname(os.path.abspath(__file__)); B = os.path.join(here, '..', '..')
J = lambda *p: json.load(open(os.path.join(here, *p)))
before, after = J('..', 'textured', 'offscreen.json'), J('offscreen.json')
tb, ta = J('..', 'textured', 'texmem.json'), J('texmem.json')
ab, aa = J('..', '..', 'texture', 'area-after.json'), J('area-g.json')
heads = J('heads-day.json')
n = lambda x: f"{x:,.0f}"
d = lambda a, b: f"{'+' if a - b >= 0 else ''}{a - b:,.0f}"
L = [f"# 072-G rebuild against the textured v4 capture (batchmode offscreen), {after['when']}", "",
     f"Textured v4: {before['export']} ({before['when']}). 072-G: {after['export']} ({after['when']}). Same views, camera and estimator ({after['camera']}).", "",
     "**Nothing here is UnityStats.** " + after['notUnityStats'], "",
     "## Counts, as built (every face): textured v4 → 072-G", "",
     "| view | yaw | main tris | materials | SetPass est. (fwd + dn + shadow) | casters | shadow tris | batchmode ms (median) |",
     "|---|---:|---:|---:|---:|---:|---:|---:|"]
bv = {(v['view'], int(v['yaw'])): v for v in before['modes']['every']}
for v in after['modes']['every']:
    k = (v['view'], int(v['yaw'])); b = bv[k]; sb, sa = b['setPassEstimate'], v['setPassEstimate']
    L.append(f"| {k[0]} | {k[1]} | {n(b['mainTriangles'])} → {n(v['mainTriangles'])} ({d(v['mainTriangles'], b['mainTriangles'])}) | {b['materials']} → {v['materials']} | "
             f"{sb['total']} → {sa['total']} ({sa['forward']} + {sa['depthNormals']} + {sa['shadow']}; {d(sa['total'], sb['total'])}) | {b['shadow']['uniqueCasters']} → {v['shadow']['uniqueCasters']} | "
             f"{n(b['shadow']['triangles'])} → {n(v['shadow']['triangles'])} ({d(v['shadow']['triangles'], b['shadow']['triangles'])}) | {b['frameMs']['median']} → {v['frameMs']['median']} |")
MiB = lambda x: f"{x:.2f} MiB"
L += ["", "District module shadow proxies (builder log): 4,654 every-face module proxies from L2, 966,462 → 1,375,578 triangles (+409,116, the window holes); all shadow proxies 6,458, 3,711,900 → 4,121,016 triangles.", "",
      "## Texture memory of the scene", "",
      "| | textures | GPU (format x size x mips x layers) | Editor runtime | surface_* set GPU |", "|---|---:|---:|---:|---:|",
      f"| textured v4 ({tb['when']}) | {tb['textures']} | {MiB(tb['gpuMiB'])} | {MiB(tb['runtimeMiB'])} | {MiB(tb['surfaceSet']['gpuMiB'])} |",
      f"| 072-G ({ta['when']}) | {ta['textures']} | {MiB(ta['gpuMiB'])} | {MiB(ta['runtimeMiB'])} | {MiB(ta['surfaceSet']['gpuMiB'])} |",
      f"| delta | {ta['textures'] - tb['textures']:+d} | {ta['gpuMiB'] - tb['gpuMiB']:+.2f} MiB | {ta['runtimeMiB'] - tb['runtimeMiB']:+.2f} MiB | budget 24 MiB |", ""]
def kinds(a):
    out = {}
    for m in a['materials']:
        k = m['maps']['kind']; o = out.setdefault(k, [0, 0.0]); o[0] += 1; o[1] += m['meanShare']
    return out
kb, ka = kinds(ab), kinds(aa)
L += ["## Screen area by map kind (mean of the 8 play views; all materials drawn opaque)", "",
      "| kind | materials textured v4 → 072-G | mean share textured v4 → 072-G |", "|---|---:|---:|"]
for k in sorted(set(kb) | set(ka), key=lambda k: -max(kb.get(k, [0, 0])[1], ka.get(k, [0, 0])[1])):
    b, a = kb.get(k, [0, 0.0]), ka.get(k, [0, 0.0])
    L.append(f"| {k} | {b[0]} → {a[0]} | {b[1] * 100:.2f} % → {a[1] * 100:.2f} % |")
flat_b = sum(v[1] for k, v in kb.items() if k in ('flat', 'swatch-atlas'))
flat_a = sum(v[1] for k, v in ka.items() if k in ('flat', 'swatch-atlas'))
L += ["", f"Flat colour left (flat + swatch-atlas without detail): {flat_b * 100:.2f} % → {flat_a * 100:.2f} % of the frame.", ""]
# F1: proxy against L0 at the same three walls
L += ["## F1: module shadows from the L2 proxies against their own LODs (same three walls as briefs/072/shots/f1/f1.json)", "",
      "| wall | before holes: mean abs diff / px > 24 | with holes: mean abs diff / px > 24 |", "|---|---:|---:|"]
f1 = json.load(open(os.path.join(B, 'shots', 'f1g', 'f1.json')))
for i, s in enumerate(f1['shots'], 1):
    row = []
    for k in ('f1', 'f1g'):
        a = np.asarray(Image.open(os.path.join(B, 'shots', k, f'f1-{i}-proxy.png')).convert('L')).astype(float)
        c = np.asarray(Image.open(os.path.join(B, 'shots', k, f'f1-{i}-l0.png')).convert('L')).astype(float)
        df = np.abs(a - c); row.append(f"{df.mean():.2f} / {(df > 24).mean() * 100:.2f} %")
    L.append(f"| {s['facade']} ({s['kit']}, yaw {s['yaw']}) | {row[0]} | {row[1]} |")
L += ["", "## Crossing heads by day (signals.json `target`, runtime look installed; Signals072, 3840 x 2160, face-on yaw)", "", heads['note'], "",
      "| shot | state | target | measured (rim blue excluded) | dE2000 | model `shows` |", "|---|---|---|---|---:|---|"]
for r in heads['rows']:
    L.append(f"| {r['shot']} | {r['state']} | {r['target']} | {r['rim_blue_excluded'][0]} | {r['rim_blue_excluded'][1]} | {r['model']} |")
open(os.path.join(here, 'report.md'), 'w').write('\n'.join(L) + '\n')
print('\n'.join(L))
