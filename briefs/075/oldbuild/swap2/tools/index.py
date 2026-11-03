"""Brief 075 swap, second build (Latch): pairs, regression numbers and index.html for briefs/075/oldbuild/swap2/.
Usage: python3 -I briefs/075/oldbuild/swap2/tools/index.py <repo root>
- golden Boulevard (old reel t 10) and Crown Circus (old reel t 20): old | now (the presentation look: the old stage with Glint's vintage pick);
- the Boulevard vista and golden view with our SpaceHQ_Portal (default) beside the old gold HQ (switch off);
- play zoom, the Handover cafe square and Office front at yaw 30 and 210: c044 (briefs/075/remap/s6, our look) | the first swap (old stage light) |
  now (the play camera with our play look). Numbers per frame: mean absolute difference against c044 over the frame and its middle half, % of
  pixels off by more than 12 (the s6 compare.py measure), and the look itself: luma contrast (std of Rec.709 luma), the dark share (luma < 90,
  where shadows read) and the mean luma, so "washed out" is a number;
- the aerial over the west: the first swap | now. Writes metrics.json and index.html."""
import sys, os, json, html
import numpy as np
from PIL import Image, ImageDraw, ImageFont

root = sys.argv[1]
S = os.path.join(root, 'briefs', '075', 'oldbuild', 'swap2')
S1 = os.path.join(root, 'briefs', '075', 'oldbuild', 'swap')
OLD = os.path.join(root, 'briefs', '074', 'flyby', 'old-stills')
C044 = os.path.join(root, 'briefs', '075', 'remap', 's6')
font = ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial Bold.ttf', 26)
INK, CREAM = (17, 20, 26), (247, 244, 236)
P = lambda *a: os.path.join(S, *a)


def row(files, labels, out, w=800, h=450):
    ims = [Image.open(f).convert('RGB').resize((w, h), Image.LANCZOS) if os.path.exists(f) else Image.new('RGB', (w, h), (60, 60, 60)) for f in files]
    g = Image.new('RGB', (len(ims) * (w + 8) + 8, h + 52), INK); d = ImageDraw.Draw(g)
    for i, (im, t) in enumerate(zip(ims, labels)):
        g.paste(im, (8 + i * (w + 8), 44)); d.text((14 + i * (w + 8), 10), t, fill=CREAM, font=font)
    g.save(out); return os.path.basename(out)


def arr(p): return np.asarray(Image.open(p).convert('RGB'), dtype=np.float32)


def mad(a, b):
    A, B = arr(a), arr(b)
    h, w = A.shape[:2]; c = (slice(h // 4, 3 * h // 4), slice(w // 4, 3 * w // 4)); diff = np.abs(A - B).mean(-1)
    return {'mad_frame': round(float(diff.mean()), 2), 'mad_centre': round(float(diff[c].mean()), 2), 'changed_px_pct': round(float((diff > 12).mean() * 100), 2)}


def look(p):
    A = arr(p); y = A[..., 0] * .2126 + A[..., 1] * .7152 + A[..., 2] * .0722
    return {'luma_contrast': round(float(y.std()), 1), 'dark_pct': round(float((y < 90).mean() * 100), 1), 'luma_mean': round(float(y.mean()), 1)}


pairs, metrics = [], {'play_vs_c044': {}, 'look': {}}
pairs.append(('Boulevard golden view (the old reel at t 10): old | now', row([os.path.join(OLD, 'flyby_t10.png'), P('golden-rule.png')], ['OLD reel t 10', 'now (swap 2, presentation look)'], P('pair-golden.png'))))
pairs.append(('Crown Circus (the old reel at t 20): old | now', row([os.path.join(OLD, 'flyby_t20.png'), P('circus-rule.png')], ['OLD reel t 20', 'now (swap 2, ring kerb r 29)'], P('pair-circus.png'))))
pairs.append(('The Boulevard vista: our SpaceHQ_Portal (default) | the old gold HQ (switch off)', row([P('vista-rule-hq-ours.png'), P('vista-rule-hq-old.png')], ['SpaceHQ "ours" (default), old finish', 'SpaceHQ "old" (off)'], P('pair-vista.png'))))
pairs.append(('Golden view: SpaceHQ ours | old', row([P('golden-rule-hq-ours.png'), P('golden-rule-hq-old.png')], ['SpaceHQ "ours" (default)', 'SpaceHQ "old" (off)'], P('pair-golden-hq.png'))))
for v in ('cafe-square-y030', 'cafe-square-y210', 'office-front-y030', 'office-front-y210'):
    base, first, now, oldlook = os.path.join(C044, f'play-{v}.png'), os.path.join(S1, f'play-{v}-rule.png'), P(f'play-{v}-rule.png'), P(f'play-{v}-rule-oldlook.png')
    if not (os.path.exists(base) and os.path.exists(now)): continue
    m = {'now': mad(base, now)}
    if os.path.exists(first): m['first_swap'] = mad(base, first)
    metrics['play_vs_c044'][v] = m
    metrics['look'][v] = {k: look(f) for k, f in (('c044', base), ('first_swap', first), ('now', now), ('now_old_stage', oldlook)) if os.path.exists(f)}
    L = metrics['look'][v]
    t = f'Handover {v}: c044 | first swap | now. vs c044: now {m["now"]["mad_frame"]} (first {m.get("first_swap", {}).get("mad_frame", "-")}); contrast c044 {L["c044"]["luma_contrast"]}, first {L.get("first_swap", {}).get("luma_contrast", "-")}, now {L["now"]["luma_contrast"]}; dark share {L["c044"]["dark_pct"]} / {L.get("first_swap", {}).get("dark_pct", "-")} / {L["now"]["dark_pct"]} %'
    pairs.append((t, row([base, first, now], ['c044 (our look, our district)', 'first swap (old stage light)', 'now (play camera, our play look)'], P(f'pair-play-{v}.png'), 640, 360)))
pairs.append(('The aerial over the west: the first swap | now', row([os.path.join(S1, 'district-aerial-rule.png'), P('district-aerial-rule.png')], ['first swap', 'now (west filled)'], P('pair-aerial.png'))))
pairs.append(('Over the west: from the south-east | from the north', row([P('district-aerial_w-rule.png'), P('district-aerial_w2-rule.png')], ['now: the west from the south-east', 'now: the west from the north'], P('pair-west.png'))))
pairs.append(('Top-down, the whole sheet: the first swap | now', row([os.path.join(S1, 'district-plan_full-rule.png'), P('district-plan_full-rule.png')], ['first swap', 'now'], P('pair-plan.png'))))
json.dump(metrics, open(P('metrics.json'), 'w'), indent=1)

def J(name):
    p = P(name)
    return json.load(open(p)) if os.path.exists(p) else {}
sw = J('swap-rule.json')
E = html.escape
shots = sorted(f for f in os.listdir(S) if f.endswith('.png') and (f.startswith('district-') or f.startswith('play-')))
body = ['<h1>Brief 075 swap, second build: plan 0.5.5, our SpaceHQ, the play look, the west</h1>',
        '<p>Latch, 2026-10-29. <code>Suborned.OldTown.Editor.Swap.OldTownSwap.Build -swapHq ours</code> into Assets/Scenes/OldTown.unity (heights "rule"), then <code>.Shoot</code>; offscreen batchmode. '
        'The presentation cameras keep the old stage (sky, sun, haze, lens) with Glint\'s vintage pick B; the play frames come from the scene\'s play camera with our play look (TacticalShadows, no fog, our grade, no baked crowd, no MSAA, no soft focus or flare), old materials.</p>',
        f'<h2>Numbers</h2><pre>{E(json.dumps(metrics, indent=1))}</pre>']
for title, f in pairs: body.append(f'<h2>{E(title)}</h2><a href="{f}"><img src="{f}"></a>')
body.append('<h2>All frames</h2>' + ''.join(f'<figure><a href="{f}"><img src="{f}"></a><figcaption>{E(f)}</figcaption></figure>' for f in shots))
body.append(f'<h2>Build log</h2><pre>{E(chr(10).join(sw.get("log", [])))}</pre>')
body.append('<h2>Evidence</h2><ul>' + ''.join(f'<li><a href="{f}">{f}</a></li>' for f in ('built.json', 'occluders-rule.json', 'swap-rule.json', 'metrics.json')) + '</ul>')
open(P('index.html'), 'w').write('<!doctype html><meta charset="utf-8"><title>075 swap 2</title><style>body{background:#11141a;color:#f7f4ec;font:15px/1.45 -apple-system,Helvetica,sans-serif;margin:24px;max-width:1700px}img{max-width:100%;border:1px solid #333}pre{background:#0b1615;padding:10px;overflow:auto;white-space:pre-wrap;font-size:12px}figure{display:inline-block;width:32%;margin:4px}figcaption{font-size:12px;color:#b0cdc7}a{color:#60e0ea}</style>' + '\n'.join(body))
print(P('index.html'))
