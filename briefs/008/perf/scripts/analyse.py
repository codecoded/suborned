# B6 (Latch): the A/B tables from a perf report JSON, folded from its segments with PerfSummary's rule (repeats that lost focus for
# over 1% of their frames are left out when the cell has focused ones). With load.txt (a ps sample a minute during the run), repeats
# during which another process ran at >= 50% CPU are left out the same way. Usage: python3 analyse.py report.json [load.txt]
import json, sys, statistics as st
from collections import defaultdict
from datetime import datetime, timezone, timedelta

r = json.load(open(sys.argv[1]))
print('device', r['device'], r['gpu'], 'unity', r['unityVersion'], r['build'], 'window', r['screenWidth'], 'x', r['screenHeight'], r['fullScreenMode'])
print('fingerprint', r['fingerprint'][:8], 'export', r['exportSha256'][:8], 'ssao as built:', r['ssaoAsBuilt'], '| proxies', r['proxyRenderers'], 'renderers', r['proxyTriangles'], 'triangles')
for k in ('machineStart', 'machineEnd'):
    m = r[k]; print(k, m['when'], 'busy', m['busy'], 'thermal', m['thermal'], 'lowPower', m['lowPower'], 'AC', m['onAC'])
    for w in m['warnings']: print('   !', w)
w = r['warmup']; print('warm-up', round(w['seconds']), 's, cycles', w['cycles'], 'settled', w['settled'], 'last change', w['lastChange'])
if r.get('error'): print('ERROR', r['error'])

busy = set()
if len(sys.argv) > 2:
    day = r['date'][:10]; samples = []; t = None
    for line in open(sys.argv[2]):
        line = line.rstrip('\n')
        if len(line) == 8 and line[2] == ':': t = datetime.fromisoformat(f"{day}T{line}+00:00"); continue
        cpu, _, comm = line.strip().partition(' ')
        if t and float(cpu) >= 50 and 'SubornedPerf' not in comm and 'WindowServer' not in comm: samples.append((t, float(cpu), comm.strip()[:50]))
    at = datetime.fromisoformat(r['date'].replace('Z', '+00:00'))
    for s in r['segments']:
        end = at + timedelta(seconds=s['settleSeconds'] + s['seconds'])
        hits = [x for x in samples if at - timedelta(seconds=30) <= x[0] <= end + timedelta(seconds=30)]
        if hits: busy.add(s['id'])
        at = end
    print('load: segments with another process >= 50% CPU:', len(busy), 'of', len(r['segments']), sorted({x[2] for x in samples}))
focused = lambda s: s['unfocusedFrames'] * 100 <= s['frames'] and s['id'] not in busy
cells = defaultdict(list)
for s in r['segments']:
    if s['repeatOf'] < 0: cells[(s['pass'], s['stop'], s['variant'])].append(s)
def fold(key):
    a = cells[key]; f = [s for s in a if focused(s)] or a
    return f, len(a) - len(f)
med = lambda xs: st.median(xs)

for p in r['passes']:
    keys = [k for k in cells if k[0] == p['name']]
    if not keys: continue
    stops = list(dict.fromkeys(k[1] for k in keys)); vs = [v for v in p['variants'] if any(k[2] == v for k in keys)]
    left = sum(fold(k)[1] for k in keys)
    print(f"\n== {p['name']} (vSync {p['vSync']}, {p['repeats']} repeats, {left} unfocused or busy repeats left out): frame p50 ms (vs {vs[0]}), median of repeats [p95]")
    print('stop'.ljust(14) + ''.join(v.rjust(22) for v in vs))
    for stp in stops:
        b = med([s['frameMs']['p50'] for s in fold((p['name'], stp, vs[0]))[0]])
        line = stp.ljust(14)
        for v in vs:
            f, _ = fold((p['name'], stp, v)); m = med([s['frameMs']['p50'] for s in f]); q = med([s['frameMs']['p95'] for s in f])
            line += (f"{m:.2f}" + ('' if v == vs[0] else f" ({100 * (m / b - 1):+.0f}%)") + f" [{q:.1f}]").rjust(22)
        print(line)
    if p['vSync'] or p['name'] == 'pan':
        for k in keys:
            for s in cells[k]:
                print(f"  {s['stop']:14}{s['variant']:11} frames {s['frames']:5} in {s['seconds']:.0f}s  p50 {s['frameMs']['p50']:.2f} p95 {s['frameMs']['p95']:.2f} p99 {s['frameMs']['p99']:.2f}  past {p['missMs']} ms {s['missedShare']:.2%}  longest {s['longestStallMs']:.1f}  unfocused {s['unfocusedFrames']}")

print('\n== drawer, per frame: camera triangles | shadow triangles (all splits)')
for k in cells:
    if k[2] in ('base', 'proxies', 'cascades-2', 'fixes-13', 'fixes-123'):
        d = next((s['drawer'] for s in cells[k] if s['drawer'] and s['drawer'].get('cameraInstances')), None)
        if d: print(f"  {k[0]:5} {k[1]:13} {k[2]:11} {d['cameraTriangles'] // 1000}k | {d['shadowTriangles'] // 1000}k in {d['shadowSplits']} splits")
print('\nranking', r['ranking'])
print('A..A', [(c['pass'], c['stop'], c['variant'], c['firstP50'], c['lastP50'], f"{c['change']:+.1%}") for c in r['repeatChecks']])
th = defaultdict(int)
for s in r['segments']: th[s['thermal']] += 1
print('thermal per segment', dict(th), '| NaN pixels > 0 in', [s['id'] for s in r['segments'] if s['nanPixels'] > 0], '| unfocused segments', sum(1 for s in r['segments'] if not focused(s)), 'of', len(r['segments']))
print('settle: hitches', sum(s.get('settleHitches', 0) for s in r['segments']), 'worst', max(s.get('settleWorstMs', 0) for s in r['segments']), 'ms')
