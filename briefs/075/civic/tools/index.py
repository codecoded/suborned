"""Brief 075 civic restyle (Latch): before | after pairs and index.html for briefs/075/civic/.
Usage: python3 -I briefs/075/civic/tools/index.py <repo root>
Frames (offscreen batchmode, out/scratch/latch/075civic):
- the old-built town (OldTown.unity, the swap's Handover zone): the golden view (the old reel at t 10, OldTownSwap.Shoot) and play zoom
  through the play camera (our play look), the café square and the Office front at yaw 30 and 210; before = the swap built from our scene
  with -civicRestyle off, after = with the restyle;
- our pipeline (HarbourDistrict, Glint075 -variant today): the golden view (Flyby074 at t 6) and the same four play frames.
Per pair: mean absolute difference over the frame and the % of pixels off by more than 12 (where the restyle shows), so a pair that does not
change (the golden views: the civic block is not in them) says so. Writes pairs/*.png, metrics.json and index.html."""
import sys, os, json, html
import numpy as np
from PIL import Image, ImageDraw, ImageFont

root = sys.argv[1]
C = os.path.join(root, 'briefs', '075', 'civic')
L = os.path.join(root, 'out', 'scratch', 'latch', '075civic')
font = ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial Bold.ttf', 26)
INK, CREAM = (17, 20, 26), (247, 244, 236)
os.makedirs(os.path.join(C, 'pairs'), exist_ok=True)


def row(files, labels, out, w=960, h=540):
    ims = [Image.open(f).convert('RGB').resize((w, h), Image.LANCZOS) if os.path.exists(f) else Image.new('RGB', (w, h), (60, 60, 60)) for f in files]
    g = Image.new('RGB', (len(ims) * (w + 8) + 8, h + 52), INK); d = ImageDraw.Draw(g)
    for i, (im, t) in enumerate(zip(ims, labels)):
        g.paste(im, (8 + i * (w + 8), 44)); d.text((14 + i * (w + 8), 10), t, fill=CREAM, font=font)
    g.save(out); return os.path.relpath(out, C)


def diff(a, b):
    if not (os.path.exists(a) and os.path.exists(b)): return None
    A = np.asarray(Image.open(a).convert('RGB'), dtype=np.float32); B = np.asarray(Image.open(b).convert('RGB'), dtype=np.float32)
    if A.shape != B.shape: return None
    d = np.abs(A - B).mean(-1)
    return {'mad_frame': round(float(d.mean()), 2), 'changed_px_pct': round(float((d > 12).mean() * 100), 2)}


rows = []
SB, SA = os.path.join(L, 'shots-before'), os.path.join(L, 'shots-after')
OB, OA = os.path.join(L, 'ours-before'), os.path.join(L, 'ours-after')
spec = [('swap-golden', 'Old-built town · golden view (old reel t 10)', os.path.join(SB, 'golden-before.png'), os.path.join(SA, 'golden-after.png'))]
for v in ('cafe-square', 'office-front'):
    for y in (30, 210):
        spec.append((f'swap-{v}-y{y:03d}', f'Old-built town · play zoom · {v.replace("-", " ")} · yaw {y}', os.path.join(SB, f'play-{v}-y{y:03d}-before.png'), os.path.join(SA, f'play-{v}-y{y:03d}-after.png')))
spec.append(('ours-golden', 'Our pipeline (HarbourDistrict) · golden view (Flyby074 t 6)', os.path.join(OB, 'today-golden.png'), os.path.join(OA, 'today-golden.png')))
for v in ('cafe-square', 'office-front'):
    for y in (30, 210):
        spec.append((f'ours-{v}-y{y:03d}', f'Our pipeline (HarbourDistrict) · play zoom · {v.replace("-", " ")} · yaw {y}', os.path.join(OB, f'today-day-{v}-y{y:03d}.png'), os.path.join(OA, f'today-day-{v}-y{y:03d}.png')))
metrics = {}
for key, title, b, a in spec:
    png = row([b, a], ['before (restyle off)', 'after (Vela\'s restyle)'], os.path.join(C, 'pairs', f'{key}.png'))
    m = diff(b, a); metrics[key] = {'before': os.path.relpath(b, root), 'after': os.path.relpath(a, root), **(m or {'missing': True})}
    rows.append((title, png, m))
json.dump(metrics, open(os.path.join(C, 'metrics.json'), 'w'), indent=1)

reg = json.load(open(os.path.join(C, 'regression.json'))) if os.path.exists(os.path.join(C, 'regression.json')) else {'pairs': {}}
rh = ''
for k, v in reg['pairs'].items():
    z = v['inZone']
    rh += (f'<tr><td>{html.escape(k)}</td><td>{"yes" if v["materialsOnly"] else "NO"}</td><td>{v["outsideZoneUnchanged"]}</td><td>{z["batchGeometryEqualIgnoringMaterial"]} ({z["batchTriangles"][0]:,} / {z["batchTriangles"][1]:,} tris)</td>'
           f'<td>{z["kitEqualIgnoringMaterial"]} ({z["kitRenderers"][0]} / {z["kitRenderers"][1]})</td><td>{z["transformsEqual"]}</td><td>{z["collidersEqual"]} ({z["colliders"][1]})</td>'
           f'<td>{z["kitRenderersWithASwap"]}</td><td>{len(z["overlayAfter"])}</td><td>{v["playNav"]["equal"]}</td></tr>')
body = ''.join(f'<h2>{html.escape(t)}</h2><p class="m">{"mean abs diff " + str(m["mad_frame"]) + " · pixels changed " + str(m["changed_px_pct"]) + " %" if m else "frame missing"}</p><img src="{html.escape(p)}">' for t, p, m in rows)
page = f'''<!doctype html><meta charset="utf-8"><title>Brief 075 · civic restyle · before | after</title>
<style>body{{background:#11141a;color:#f7f4ec;font:15px/1.45 -apple-system,Helvetica,sans-serif;margin:24px;max-width:1960px}}img{{width:100%;border:1px solid #333}}h2{{margin:28px 0 4px;font-size:18px}}.m{{color:#b0cdc7;margin:0 0 6px}}
table{{border-collapse:collapse;margin:8px 0 18px}}td,th{{border:1px solid #39414a;padding:4px 8px;text-align:left;font-size:13px}}code{{color:#60e0ea}}</style>
<h1>Brief 075 · civic restyle (Vela's restyle.json) · before | after</h1>
<p>Built by code (<code>CivicRestyle</code> in HarbourWorldBuilder; <code>-civicRestyle off</code> for the before), offscreen batchmode only.
The swap's Handover zone is our scene as built, so the old-built town takes the restyle through the same build. Records Office walls in DK_Limestone_Tex, the charcoal spandrels in DK_GlassDark,
the café in Naples cream (DK_Render_White__circus) with coral sill bands and a teal fascia, the square and office forecourt in DK_Terrazzo_Ivory, the bed kerbs and planters in panel stone, two collider-free hanging baskets on the café's outer piers.</p>
<h2>Regression (restyle075 manifests, compare.py)</h2>
<table><tr><th>pair</th><th>materials only</th><th>outside zone unchanged</th><th>zone geometry equal (material-blind)</th><th>kit renderers equal (material-blind)</th><th>transforms</th><th>colliders</th><th>kit renderers with a slot swap</th><th>overlay entries</th><th>play nav equal</th></tr>{rh}</table>
<p>Full numbers: <code>regression.json</code>, <code>metrics.json</code>; manifests in <code>out/scratch/latch/075civic/manifest/</code>.</p>
{body}'''
open(os.path.join(C, 'index.html'), 'w').write(page)
print(json.dumps(metrics, indent=1))
