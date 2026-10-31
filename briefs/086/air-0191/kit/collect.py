#!/usr/bin/env python3
# Brief 086: the Air sitting's folder (pulled to out/scratch/splice/air-run) → results.json for Redline. Every number carries its view, size, zoom, render scale and warmth.
# usage: python3 -I collect.py <run folder> <results.json>
import json, os, re, sys
run, out = sys.argv[1], sys.argv[2]
LOG = open(os.path.join(run, 'machine.log')).read().splitlines()
mm = []
for l in open(os.path.join(run, 'macmon.jsonl')):
    try: d = json.loads(l)
    except Exception: continue
    t = d.get('timestamp'); mm.append(d)
from datetime import datetime
for d in mm: d['_t'] = datetime.fromisoformat(d['timestamp']).timestamp()
def window(t0, t1):
    w = [d for d in mm if t0 <= d['_t'] <= t1]
    if not w: return None
    avg = lambda f: round(sum(f(d) for d in w) / len(w), 2)
    return dict(samples=len(w), gpuMHz=avg(lambda d: d['gpu_freq_mhz']), gpuActive=avg(lambda d: d['gpu_active_ratio']), gpuW=avg(lambda d: d['gpu_power']), sysW=avg(lambda d: d['sys_power']), gpuC=avg(lambda d: d['temp']['gpu_temp_avg']), cpuC=avg(lambda d: d['temp']['cpu_temp_avg']), pcpuMHz=avg(lambda d: d['pcpu_freq_mhz']))
done = {}
for l in LOG:
    m = re.match(r'row-done (\S+) (\d+) unix (\d+)', l)
    if m: done.setdefault(m[1], {})[int(m[2])] = int(m[3])
span = {}
for l in LOG:
    m = re.match(r'(\S+)-(start|end) unix (\d+)', l)
    if m: span.setdefault(m[1], {})[m[2]] = int(m[3])
order = [k for k in span if k not in ('gate',) and os.path.isdir(os.path.join(run, k))]
SCALE = lambda k: int(re.match(r'scale-(\d+)', k)[1]) if k.startswith('scale-') else 100
PRESET = lambda k: 8 if k == 'warm-z8' else 14 if k == 'warm-z14' else 18.5 if k == 'warm-z18' else 10
rows = []
for k in order:
    f = os.path.join(run, k, 'tplong.json')
    if not os.path.exists(f): continue
    rep = json.load(open(f)); plan = {r['name']: r for r in json.load(open(os.path.join(os.path.dirname(__file__), ('scale' if k.startswith('scale') else 'cold-z10' if k == 'check' else k) + '.rows.json')))['rows']}
    for i, r in enumerate(rep['rows']):
        p = plan[r['name']]; end = done.get(k, {}).get(i + 1)
        third = p['view'] == 'follow'; first_follow = third and r['name'] == 'tp-street'
        win = window(end - p['seconds'] - 3, end - 1) if end and not first_follow else None
        rows.append(dict(launch=k, name=r['name'], view='third person' if third else 'play', sizeRequested=[p['w'], p['h']], size=r['screen'], zoom=None if third else PRESET(k), renderScalePct=SCALE(k), variant=p.get('variant') or None,
            warmth='cold' if k in ('check', 'cold-z10') and len(rows) < 3 else 'warm', unixEnd=end, frames=r['frames'], gpuMs=r['gpuMs'], mainMs=r['mainMs'], renderMs=r['renderMs'], counters=r['counters'], drawn=r['drawn'], status=r['status'], macmon=win, **({'error': r['error']} if r.get('error') else {})))
frames = None
pf = os.path.join(run, 'frames', 'proof.json')
if os.path.exists(pf):
    j = json.load(open(pf)); fr = j.get('frames', {})
    frames = dict(screen=j.get('screen'), framesAt=j.get('framesAt'), follow=fr.get('follow'), people=fr.get('people'))
pp = os.path.join(run, 'shipped', 'perf064.json')
shipped = json.load(open(pp)) if os.path.exists(pp) else None
th = [l.split() for l in open(os.path.join(run, 'thermal.log'))]
states = sorted({l[2] for l in th})
res = dict(schema='suborned.air-sitting/1', brief='086', build=rep.get('build') if rows else None, run=os.path.basename(run), machine=open(os.path.join(run, 'machine.txt')).read().splitlines(),
    span=span, firstUnix=min((s.get('start', 9e12) for s in span.values()), default=None), lastUnix=max((s.get('end', 0) for s in span.values()), default=None), thermalStates=states, rows=rows, framesProof=frames, shippedFullScreen=shipped,
    soak=[dict(name=r['name'], meanMs=r['frames']['meanMs'], p99Ms=r['frames']['p99Ms'], gpuMs=r['gpuMs'], macmon=r['macmon']) for r in rows if r['name'].startswith('soak-')])
json.dump(res, open(out, 'w'), indent=1); print(f"{len(rows)} rows, thermal states {states}, window {res['firstUnix']}..{res['lastUnix']}")
