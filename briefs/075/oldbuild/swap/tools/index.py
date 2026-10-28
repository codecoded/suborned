"""Brief 075 remap step 2, the swap (Latch): pairs, grids, regression numbers and index.html for briefs/075/oldbuild/swap/.
Usage: python3 -I briefs/075/oldbuild/swap/tools/index.py <repo root>
- pairs: circus (old reel t 20 | heights old | heights rule), golden view (old reel t 10 | old | rule), vista SpaceHQ old | ours (both heights);
- flyby/grid-swap.png: the old reel's stills | now (heights rule) at 0..60 s every 10 s, the old reel's own flight;
- play zoom regression of the Handover views against c044's zone (briefs/075/remap/s6/play-*.png): mean absolute difference over the frame and its
  middle half, % of pixels off by more than 12 (the s6 compare.py measure); the lighting is the old stage's, so the numbers are mostly light;
  geometry identity is zone-regression-*.json (OldTownSwap.Verify);
- index.html with every frame and the JSON evidence linked. Writes metrics.json."""
import sys, os, json, html
import numpy as np
from PIL import Image, ImageDraw, ImageFont

root = sys.argv[1]
S = os.path.join(root, 'briefs', '075', 'oldbuild', 'swap')
OLD = os.path.join(root, 'briefs', '074', 'flyby', 'old-stills')
C044 = os.path.join(root, 'briefs', '075', 'remap', 's6')
font = ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial Bold.ttf', 26)
small = ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial Bold.ttf', 20)
INK, CREAM, DIM = (17, 20, 26), (247, 244, 236), (176, 205, 199)
P = lambda *a: os.path.join(S, *a)


def row(files, labels, out, w=800, h=450):
    ims = [Image.open(f).convert('RGB').resize((w, h), Image.LANCZOS) if os.path.exists(f) else Image.new('RGB', (w, h), (60, 60, 60)) for f in files]
    g = Image.new('RGB', (len(ims) * (w + 8) + 8, h + 52), INK); d = ImageDraw.Draw(g)
    for i, (im, t) in enumerate(zip(ims, labels)):
        g.paste(im, (8 + i * (w + 8), 44)); d.text((14 + i * (w + 8), 10), t, fill=CREAM, font=font)
    g.save(out); return os.path.basename(out)


def mad(a, b):
    A, B = (np.asarray(Image.open(p).convert('RGB'), dtype=np.float32) for p in (a, b))
    h, w = A.shape[:2]; c = (slice(h // 4, 3 * h // 4), slice(w // 4, 3 * w // 4)); diff = np.abs(A - B).mean(-1)
    return {'mad_frame': round(float(diff.mean()), 2), 'mad_centre': round(float(diff[c].mean()), 2), 'changed_px_pct': round(float((diff > 12).mean() * 100), 2)}


pairs = []
pairs.append(('Crown Circus: the old reel\'s camera at t 20', row([os.path.join(OLD, 'flyby_t20.png'), P('circus-old.png'), P('circus-rule.png')], ['OLD reel t 20', 'swap · heights "old"', 'swap · heights "rule" (default)'], P('pair-circus.png'))))
pairs.append(('Boulevard golden view: the old reel at t 10', row([os.path.join(OLD, 'flyby_t10.png'), P('golden-old.png'), P('golden-rule.png')], ['OLD reel t 10', 'swap · heights "old"', 'swap · heights "rule" (default)'], P('pair-golden.png'))))
for h in ('rule', 'old'):
    pairs.append((f'Boulevard vista, heights "{h}": SpaceHQ old | ours', row([P(f'vista-{h}-hq-old.png'), P(f'vista-{h}-hq-ours.png')], ['SpaceHQ "old" (default): the old gold HQ', 'SpaceHQ "ours": SpaceHQ_Portal'], P(f'pair-vista-{h}.png'))))
    pairs.append((f'Golden view, heights "{h}": SpaceHQ old | ours', row([P(f'golden-{h}-hq-old.png'), P(f'golden-{h}-hq-ours.png')], ['SpaceHQ "old"', 'SpaceHQ "ours"'], P(f'pair-golden-hq-{h}.png'))))

metrics = {'regression_vs_c044': {}}
for v in ('office-front-y030', 'office-front-y210', 'cafe-square-y030', 'cafe-square-y210'):
    base, now = os.path.join(C044, f'play-{v}.png'), P(f'play-{v}-rule.png')
    if not (os.path.exists(base) and os.path.exists(now)): continue
    r = mad(base, now); metrics['regression_vs_c044'][v] = r
    if os.path.exists(P(f'play-{v}-old.png')): metrics['regression_vs_c044'][v + ' (heights old)'] = mad(base, P(f'play-{v}-old.png'))
    pairs.append((f'Handover {v}: c044 zone (our look) | swap (old stage) · mean diff {r["mad_frame"]} (centre {r["mad_centre"]}), {r["changed_px_pct"]}% px > 12', row([base, now], ['c044 zone (remap s6, our look)', 'swap, heights rule (old stage light)'], P(f'pair-play-{v}.png'), 960, 540)))

# the flyby grid: old reel | now
ts = [t for t in ('00', '10', '20', '30', '40', '50', '60') if os.path.exists(P('flyby', f'now-rule_t{t}.png'))]
if ts:
    TW, TH, G, head, lab = 800, 450, 8, 52, 34
    g = Image.new('RGB', (2 * TW + 3 * G, head + len(ts) * (lab + TH + G)), INK); d = ImageDraw.Draw(g)
    for j, t in enumerate(('OLD reel', 'now: the swap (heights rule)')): d.text((G + j * (TW + G) + 6, 14), t, fill=CREAM, font=font)
    for i, t in enumerate(ts):
        y = head + i * (lab + TH + G); d.text((G + 6, y + 6), f't {int(t)} s', fill=DIM, font=small)
        for j, f in enumerate((os.path.join(OLD, f'flyby_t{t}.png'), P('flyby', f'now-rule_t{t}.png'))):
            g.paste(Image.open(f).convert('RGB').resize((TW, TH), Image.LANCZOS), (G + j * (TW + G), y + lab))
    g.save(P('flyby', 'grid-swap.png'))
    metrics['flyby_vs_old_reel'] = {t: mad(os.path.join(OLD, f'flyby_t{t}.png'), P('flyby', f'now-rule_t{t}.png')) for t in ts}

json.dump(metrics, open(P('metrics.json'), 'w'), indent=1)

def J(name):
    p = P(name)
    return json.load(open(p)) if os.path.exists(p) else {}
zr, zo = J('zone-regression-OldTown.json'), J('zone-regression-OldTown-heights-old.json')
sr, so = J('swap-rule.json'), J('swap-old.json')
occ = open(P('occlusion.md')).read() if os.path.exists(P('occlusion.md')) else '(occlusion not run)'
shots = sorted(f for f in os.listdir(S) if f.endswith('.png') and (f.startswith('district-') or f.startswith('play-')))
E = html.escape
body = [f'<h1>Brief 075 remap step 2 · the swap: the old builders build the town, ours in the Handover zone, our network on top</h1>',
        '<p>Latch, 2026-10-29. Built by <code>Suborned.OldTown.Editor.Swap.OldTownSwap.Build</code> (unity/Assets/Suborned/Editor/OldTown/Swap) into Assets/Scenes/OldTown.unity (heights "rule", default) and OldTown-heights-old.unity; SpaceHQ switch in both (default "old"). Offscreen batchmode renders with the old stage (sky, sun, grade, haze, lens).</p>',
        '<h2>Zone regression (geometry)</h2>',
        f'<pre>{E(json.dumps({k: zr.get(k) for k in ("ours", "swapped", "identical", "kitRenderersIdentical", "materialCountsThatDiffer", "oldRenderersInZoneByPath")}, indent=1))}</pre>',
        '<h2>Occlusion</h2>', f'<pre>{E(occ)}</pre>']
for title, f in pairs: body.append(f'<h2>{E(title)}</h2><a href="{f}"><img src="{f}"></a>')
if ts: body.append('<h2>Flyby on the old reel\'s route (ReviewVideo encode): old | now</h2><p><a href="flyby/flyby-rule.mp4">flyby/flyby-rule.mp4</a></p><a href="flyby/grid-swap.png"><img src="flyby/grid-swap.png"></a>')
body.append('<h2>District and play-zoom frames</h2>' + ''.join(f'<figure><a href="{f}"><img src="{f}"></a><figcaption>{E(f)}</figcaption></figure>' for f in shots))
for name, s in (('heights rule', sr), ('heights old', so)):
    body.append(f'<h2>Build log, {name}</h2><pre>{E(chr(10).join(s.get("log", [])))}</pre>')
body.append('<h2>Evidence</h2><ul>' + ''.join(f'<li><a href="{f}">{f}</a></li>' for f in ('built.json', 'built-heights-old.json', 'occluders-rule.json', 'occluders-old.json', 'occlusion.json', 'swap-rule.json', 'swap-old.json', 'zone-regression-OldTown.json', 'zone-regression-OldTown-heights-old.json', 'metrics.json', '../keepdrop.html')) + '</ul>')
open(P('index.html'), 'w').write('<!doctype html><meta charset="utf-8"><title>075 swap</title><style>body{background:#11141a;color:#f7f4ec;font:15px/1.45 -apple-system,Helvetica,sans-serif;margin:24px;max-width:1700px}img{max-width:100%;border:1px solid #333}pre{background:#0b1615;padding:10px;overflow:auto;white-space:pre-wrap;font-size:12px}figure{display:inline-block;width:32%;margin:4px}figcaption{font-size:12px;color:#b0cdc7}a{color:#60e0ea}</style>' + '\n'.join(body))
print(P('index.html'))
