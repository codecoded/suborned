# Motive 059: read the probe batches. results-head vs c036's record; the yard probes per entry.
import json, sys, glob
c036 = json.load(open('/Users/sky/Projects/suborned/runs/missions/handover/c036/run.json'))['plays']
def rows(f):
    for l in open(f):
        l = l.strip()
        if l: yield json.loads(l)
def name(r): return (r.get('script') or r.get('path') or r.get('id') or '').split('/')[-1].replace('.json', '')
same = diff = 0
for r in rows('results-head.jsonl'):
    n = name(r); c = c036.get(n)
    k = (r.get('outcome'), r.get('fail'), r.get('tick'), r.get('trail'))
    if c and k == (c['outcome'], c['fail'], c['tick'], c['trail']): same += 1
    else: diff += 1; print('DIFF', n, k, c and (c['outcome'], c['fail'], c['tick'], c['trail']))
print(f'head inputs vs c036: {same} same, {diff} differ')
for v in ('north', 'apron-south', 'official', 'apron-bay'):
    for r in rows(f'results-{v}.jsonl'):
        m = r.get('marks', {})
        print(f"{v:12s} {name(r):24s} {r.get('outcome'):10s} {r.get('fail',''):10s} {r['tick']/10:6.1f}s T{r['trail']:>3} A{r['alert']} carFree={r.get('carFreeS')} fight={r.get('fight')} blocker={m.get('fight.blocker')} free={m.get('car.free')} left={r.get('left')}")
