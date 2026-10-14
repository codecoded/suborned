# Counts (SetPass, triangles, shadow casters; machine-independent) per stop and variant from the stopped Studio run of the as-was
# player (no run.json): segment ids mapped back through PerfSchedule's order (pass ab, rotated repeats and variants).
import json, csv, sys, statistics as st
from collections import defaultdict
plan = json.load(open(sys.argv[1])); rows = sys.argv[2]
p = next(x for x in plan['passes'] if x['name'] == 'ab')
stops = p['stops']; vs = p['variants']; seg = []
for r in range(p['repeats']):
    for k in range(len(stops)):
        stop = stops[(r + k) % len(stops)]
        for j in range(len(vs)): seg.append((r, stop, vs[(r + k + j) % len(vs)]))
acc = defaultdict(lambda: defaultdict(list))
for row in csv.DictReader(open(rows)):
    i = int(row['seg'])
    if i >= len(seg): continue
    r, stop, v = seg[i]
    for c in ('setPassCalls', 'triangles', 'shadowCasters'): acc[(stop, v)][c].append(float(row[c]))
out = {f'{s}|{v}': {c: st.median(x) for c, x in d.items()} | {'frames': len(d['triangles'])} for (s, v), d in acc.items()}
json.dump(out, open(sys.argv[3], 'w'), indent=1)
for k in sorted(out): print(k, {c: int(x) for c, x in out[k].items()})
