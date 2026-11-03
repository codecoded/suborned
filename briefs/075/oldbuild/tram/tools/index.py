# Brief 075 tram bed (Latch): pairs, the stacked top-down strip and the audit numbers into tram/index.html.
# usage: python3 -I briefs/075/oldbuild/tram/tools/index.py
import json, os, sys, html
from PIL import Image, ImageDraw

T = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
SWAP2 = os.path.join(T, '..', 'swap2')

def p(*a): return os.path.join(T, *a)
def have(f): return os.path.exists(p(f))

def pair(left, right, out, ltxt, rtxt):
    a, b = Image.open(left).convert('RGB'), Image.open(right).convert('RGB')
    w, h = 960, int(960 * a.height / a.width)
    a, b = a.resize((w, h), Image.LANCZOS), b.resize((w, h), Image.LANCZOS)
    im = Image.new('RGB', (2 * w + 12, h + 34), (18, 22, 30)); d = ImageDraw.Draw(im)
    im.paste(a, (0, 34)); im.paste(b, (w + 12, 34))
    d.text((8, 10), ltxt, fill=(240, 240, 240)); d.text((w + 20, 10), rtxt, fill=(240, 240, 240))
    im.save(out)

def strip(tag):
    meta = p('strip', f'strip-{tag}.json')
    if not os.path.exists(meta): return None
    m = json.load(open(meta)); files = [p('strip', t['file']) for t in m['tiles'] if os.path.exists(p('strip', t['file']))]
    trunk = [f for f in files if Image.open(f).size[0] != Image.open(f).size[1]]
    loop = [f for f in files if f not in trunk]
    per, tw, th, gap = 4, 1200, 600, 8
    rows = [trunk[i:i + per] for i in range(0, len(trunk), per)]
    H = len(rows) * (th + gap) + (1200 + gap if loop else 0)
    im = Image.new('RGB', (per * tw + (per - 1) * gap, H), (18, 22, 30)); d = ImageDraw.Draw(im)
    for r, row in enumerate(rows):
        for c, f in enumerate(row):
            x, y = c * (tw + gap), r * (th + gap)
            im.paste(Image.open(f).convert('RGB').resize((tw, th)), (x, y))
            d.text((x + 8, y + 6), os.path.basename(f), fill=(255, 255, 255))
    if loop:
        y = len(rows) * (th + gap)
        im.paste(Image.open(loop[0]).convert('RGB').resize((1200, 1200)), (0, y)); d.text((8, y + 6), 'circus loop (north up)', fill=(255, 255, 255))
    out = p(f'strip-{tag}.png'); im.save(out); return os.path.basename(out)

frames = [('boulevard-vista', 'Boulevard, pair-vista camera (SpaceHQ ours)'), ('boulevard-bed', 'Boulevard, down at the bed'),
          ('boulevard-stop', 'Boulevard, the circus stop'), ('market-east', 'Market Street, looking east to the Boulevard'),
          ('market-west', 'Market Street, looking west (our ground past x -158)'), ('west-leg', 'The west leg (x -340), looking north'),
          ('circus', 'Crown Circus, the loop'), ('crown-avenue', 'Crown Avenue, looking south to the circus (no tram on the avenue)')]

sections = []
# the coordinator's pair: before = swap2's vista (the pair-vista left frame) unless a "before" tram shoot exists
before_vista = p('tram-boulevard-vista-before.png') if have('tram-boulevard-vista-before.png') else os.path.join(SWAP2, 'vista-rule-hq-ours.png')
if have('tram-boulevard-vista-rule.png'):
    pair(before_vista, p('tram-boulevard-vista-rule.png'), p('pair-boulevard-vista.png'), 'before (swap2, pair-vista left)', 'after (E11 + E12)')
    sections.append(('Boulevard: before | after (pair-vista camera)', ['pair-boulevard-vista.png']))
for fid, title in frames[1:]:
    a, b = f'tram-{fid}-before.png', f'tram-{fid}-rule.png'
    if have(a) and have(b):
        pair(p(a), p(b), p(f'pair-{fid}.png'), 'before', 'after (E11 + E12)'); sections.append((title + ': before | after', [f'pair-{fid}.png']))
    elif have(b): sections.append((title, [b]))
s = strip('rule')
if s: sections.append(('The whole tram route, top down (trunk from the west end, 100 m a tile, route along the width; then the circus loop)', [s]))
sb = strip('before')
if sb: sections.append(('Before: the same strip', [sb]))

def audit(f):
    if not have(f): return ''
    a = json.load(open(p(f)))
    rows = ''.join(f"<tr><td>{html.escape(k)}</td><td>{v['samples']}</td><td>{v['onBed']}</td><td>{v['level']}</td><td>{v['over']}</td><td>{v['levelNoBed']}</td></tr>" for k, v in a['byTrack'].items())
    def top(key):
        return ''.join(f"<li>{html.escape(x['source'])}: {x['samples']} samples, e.g. plan {', '.join(f'({q[0]}, {q[1]})' for q in x['at'][:3])}</li>" for x in a[key][:12]) or '<li>none</li>'
    return (f"<h3>{html.escape(f)}</h3><p>{html.escape(a['rule'])}. Our tiles {a['ourTiles']}; renderers read {a['renderersRead']}, unreadable {a['unreadable']}.</p>"
            f"<table><tr><th>track</th><th>samples</th><th>on our bed</th><th>level with it</th><th>over it</th><th>level, no bed</th></tr>{rows}"
            f"<tr><td><b>all</b></td><td>{a['samples']}</td><td>{a['onBed']}</td><td>{a['level']}</td><td>{a['over']}</td><td>{a['levelNoBed']}</td></tr></table>"
            f"<p>level with our bed, by source:</p><ul>{top('levelBySource')}</ul><p>over our bed, by source:</p><ul>{top('overBySource')}</ul>"
            f"<p>level off the bed, by source:</p><ul>{top('levelNoBedBySource')}</ul>")

body = ''.join(f"<h2>{html.escape(t)}</h2>" + ''.join(f'<a href="{f}"><img src="{f}"></a>' for f in fs) for t, fs in sections)
doc = f"""<!doctype html><meta charset="utf-8"><title>075 tram bed (Latch)</title>
<style>body{{background:#12161e;color:#e8e8e8;font:14px/1.45 system-ui;margin:24px;max-width:1960px}}img{{max-width:100%;display:block;margin:6px 0 18px}}
table{{border-collapse:collapse}}td,th{{border:1px solid #39404d;padding:3px 8px;text-align:right}}td:first-child{{text-align:left}}a{{color:#9cd}}</style>
<h1>Brief 075: the Boulevard tram bed (E11 + E12)</h1>
<p>Cause: the old road-level modules (Tile_TramRail, top at z 0) were coplanar with the road deck once E10 lifted it to 0, and the swap kept the old single track on the Boulevard instead of laying ours. E11 lifts old road modules 3 mm; E12 lays our double track on the Boulevard too and the old track, road modules and paint on our bed give way. Study camera (the old stage's look), offscreen.</p>
{body}<h2>Audit</h2>{audit('tram-audit-before.json')}{audit('tram-audit.json')}"""
open(p('index.html'), 'w').write(doc)
print('index.html:', len(sections), 'sections')
