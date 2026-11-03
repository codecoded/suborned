# GUID clash analysis between the staged old copy and our unity/Assets (+ Packages metas).
import os, re, sys, json, hashlib
STAGE = sys.argv[1]; OURS = sys.argv[2]; OUT = sys.argv[3]
GUID = re.compile(rb'^guid: ([0-9a-f]{32})', re.M)
def metas(root, skip=()):
    m = {}
    for d, dirs, files in os.walk(root):
        dirs[:] = [x for x in dirs if os.path.join(d, x) not in skip]
        for f in files:
            if f.endswith('.meta'):
                p = os.path.join(d, f)
                g = GUID.search(open(p, 'rb').read())
                if g: m.setdefault(g.group(1).decode(), []).append(p)
    return m
old = metas(STAGE)
ours = metas(os.path.join(OURS, 'Assets'), skip=(os.path.join(OURS, 'Assets/Suborned/OldTownArt'),))
clash = sorted(set(old) & set(ours))
rows = []
for g in clash:
    o = old[g][0]; n = ours[g][0]
    same = open(o[:-5], 'rb').read() == open(n[:-5], 'rb').read() if os.path.isfile(o[:-5]) and os.path.isfile(n[:-5]) else None
    rows.append({'guid': g, 'old': os.path.relpath(o[:-5], STAGE), 'ours': os.path.relpath(n[:-5], OURS), 'same_bytes': same})
json.dump({'old_assets': len(old), 'our_assets': len(ours), 'clashes': rows}, open(OUT, 'w'), indent=1)
print(len(old), 'old guids;', len(ours), 'ours;', len(clash), 'clash;', sum(1 for r in rows if r['same_bytes']), 'byte-identical')
from collections import Counter
print(Counter(r['old'].split('/')[2] if r['old'].count('/')>2 else r['old'] for r in rows).most_common(12))
