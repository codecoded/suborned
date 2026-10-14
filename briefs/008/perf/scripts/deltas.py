# B6 (Latch): per-switch deltas from a perf report JSON, against the pass's first variant (the reference column).
# Per stop and variant: frame p50 and p95 (median of the focused repeats, analyse.py's rule), draws (the GPU Resident Drawer's
# camera draws x passes (2 with SSAO's depth-normals prepass) + shadow draws), triangles (profiler, every pass) and shadow casters (profiler), each as the variant's value and its delta.
# Usage: python3 deltas.py report.json [pass] [--html] [--json]
import json, sys, statistics as st
from collections import defaultdict

args = [a for a in sys.argv[1:] if not a.startswith('--')]
r = json.load(open(args[0])); pname = args[1] if len(args) > 1 else 'ab'
HTML, JSON = '--html' in sys.argv, '--json' in sys.argv
STOPS = ['home', 'cafe-square', 'parade', 'crown-circus', 'harbour-quay', 'heaviest', 'north-edge']
p = next(x for x in r['passes'] if x['name'] == pname)
cells = defaultdict(list)
for s in r['segments']:
    if s['pass'] == pname and s['repeatOf'] < 0: cells[(s['stop'], s['variant'])].append(s)
VAR = {v['name']: v for v in r['variants']}
BUILT_NORMALS = 'DepthNormals' in r.get('ssaoAsBuilt', '') and r.get('ssaoAsBuilt', '').startswith('on')
def prepass(v):
    """Whether the variant draws the camera view twice (SSAO's depth-normals prepass, then forward)."""
    m = VAR.get(v, {}).get('ssao', '') or 'on'
    return m == 'normals' or (m == 'on' and BUILT_NORMALS)
focused = lambda s: s['unfocusedFrames'] * 100 <= s['frames']
def fold(stop, v):
    a = cells[(stop, v)]; f = [s for s in a if focused(s)] or a
    if not f: return None
    d = next((s['drawer'] for s in f if s.get('drawer') and s['drawer'].get('cameraInstances')), {}) or {}
    cam_passes = 2 if prepass(v) else 1
    return {
        'p50': st.median(s['frameMs']['p50'] for s in f), 'p95': st.median(s['frameMs']['p95'] for s in f),
        'spread': max(s['frameMs']['p50'] for s in f) - min(s['frameMs']['p50'] for s in f), 'n': len(f), 'of': len(a),
        'draws': (cam_passes * d.get('cameraDraws', 0) + d.get('shadowDraws', 0)) if d else None, 'prepass': cam_passes == 2,
        'shadowTris': d.get('shadowTriangles'), 'cameraTris': d.get('cameraTriangles'),
        'tris': st.median(s['trianglesP50'] for s in f), 'casters': st.median(s['shadowCastersP50'] for s in f),
        'setPass': st.median(s['setPassP50'] for s in f), 'nan': max(s['nanPixels'] for s in f),
    }
vs = [v for v in p['variants'] if any(k[1] == v for k in cells)] or sorted({k[1] for k in cells})
ref = vs[0]
stops = [s for s in STOPS if any(k[0] == s for k in cells)]
out = {'device': r['device'], 'gpu': r['gpu'], 'pass': pname, 'reference': ref, 'variants': vs, 'stops': stops, 'cells': {}}
for stop in stops:
    for v in vs:
        c = fold(stop, v)
        if c: out['cells'][f'{stop}|{v}'] = c
if JSON: print(json.dumps(out, indent=1)); sys.exit()

def fmt_delta(a, b, unit='', pct=True, k=1):
    if a is None or b is None: return '–'
    d = a - b
    s = f"{d / k:+,.2f}{unit}" if unit == ' ms' else f"{d / k:+,.0f}{unit}"
    return s + (f" ({100 * (a / b - 1):+.0f}%)" if pct and b else '')

if not HTML:
    print(f"{r['device']} {r['gpu']} pass {pname}, reference {ref}")
    for v in vs[1:]:
        print(f"\n== {v} vs {ref}")
        print('stop'.ljust(14) + 'p95 ms'.rjust(22) + 'p50 ms'.rjust(22) + 'draws'.rjust(16) + 'triangles'.rjust(24) + 'casters'.rjust(16))
        for stop in stops:
            a, b = out['cells'].get(f'{stop}|{v}'), out['cells'].get(f'{stop}|{ref}')
            if not a or not b: continue
            print(stop.ljust(14) + fmt_delta(a['p95'], b['p95'], ' ms').rjust(22) + fmt_delta(a['p50'], b['p50'], ' ms').rjust(22)
                  + fmt_delta(a['draws'], b['draws']).rjust(16) + fmt_delta(a['tris'], b['tris'], 'k', True, 1000).rjust(24) + fmt_delta(a['casters'], b['casters']).rjust(16))
else:
    print(f"<table><tr><th>switch</th><th>stop</th><th>p95 ms</th><th>Δ p95</th><th>Δ p50</th><th>draws</th><th>Δ draws</th><th>triangles</th><th>Δ triangles</th><th>casters</th><th>Δ casters</th></tr>")
    for v in vs:
        for i, stop in enumerate(stops):
            a, b = out['cells'].get(f'{stop}|{v}'), out['cells'].get(f'{stop}|{ref}')
            if not a or not b: continue
            first = f"<td rowspan={len(stops)}><b>{v}</b></td>" if i == 0 else ''
            same = v == ref
            print(f"<tr>{first}<td>{stop}</td><td>{a['p95']:.2f}</td><td>{'ref' if same else fmt_delta(a['p95'], b['p95'], ' ms')}</td><td>{'ref' if same else fmt_delta(a['p50'], b['p50'], ' ms')}</td>"
                  f"<td>{a['draws']}</td><td>{'' if same else fmt_delta(a['draws'], b['draws'])}</td><td>{a['tris'] / 1e6:.2f} M</td><td>{'' if same else fmt_delta(a['tris'], b['tris'], 'k', True, 1000)}</td>"
                  f"<td>{a['casters']:.0f}</td><td>{'' if same else fmt_delta(a['casters'], b['casters'])}</td></tr>")
    print("</table>")
