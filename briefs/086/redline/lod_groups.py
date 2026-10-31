#!/usr/bin/env python3
# Brief 086 (Redline): the LODGroups OldTown.unity holds, by branch and by group name, with catalogue triangles. Read-only; prints JSON.
import json, re, sys, collections
ROOT = '/Users/sky/Projects/suborned'
S = f'{ROOT}/unity/Assets/Scenes/OldTown.unity'
hdr = re.compile(r'^--- !u!(\d+) &(-?\d+)'); fid = re.compile(r'fileID: (-?\d+)')
G, T, L = {}, {}, {}
go2t = {}
kind = cur = None
for line in open(S, errors='ignore'):
    if line.startswith('---'):
        m = hdr.match(line); kind = m.group(1) if m else None; cur = None
        if kind == '1': cur = G.setdefault(m.group(2), {})
        elif kind == '4': cur = T.setdefault(m.group(2), {})
        elif kind == '205': cur = L.setdefault(m.group(2), {'h': [], 'w': []})
        continue
    if cur is None: continue
    s = line.strip()
    if kind == '1' and s.startswith('m_Name:'): cur['n'] = s[7:].strip()
    elif kind == '4':
        if s.startswith('m_GameObject:'): cur['go'] = fid.search(s).group(1); go2t[cur['go']] = None
        elif s.startswith('m_Father:'): cur['f'] = fid.search(s).group(1)
    elif kind == '205':
        if s.startswith('m_GameObject:'): cur['go'] = fid.search(s).group(1)
        elif s.startswith('m_Size:'): cur['size'] = float(s.split(':')[1])
        elif s.startswith('- screenRelativeHeight:'): cur['h'].append(float(s.split(':')[1]))
for tid, t in T.items():
    if t.get('go'): go2t[t['go']] = tid
def top(tid):
    names = []
    for _ in range(64):
        t = T.get(tid)
        if not t or 'go' not in t: break
        names.append(G.get(t['go'], {}).get('n', '?')); tid = t.get('f')
    return list(reversed(names))
kit = {r['id']: r for r in json.load(open(f'{ROOT}/contracts/world/kit.json'))['assets']}
rows = collections.defaultdict(lambda: dict(n=0, branches=collections.Counter(), levels=collections.Counter(), h=None, size=None))
for l in L.values():
    g = G.get(l.get('go'), {}); nm = re.sub(r'^DK_', '', re.sub(r'( \(\d+\)| #.*|-\d+)$', '', g.get('n', '?')))
    ch = top(go2t.get(l.get('go')))
    r = rows[nm]; r['n'] += 1; r['branches'][ch[0] if ch else '?'] += 1; r['levels'][len(l['h'])] += 1; r['h'] = [round(x, 4) for x in l['h']]; r['size'] = l.get('size')
out = []
for nm, r in rows.items():
    k = kit.get(nm)
    out.append(dict(group=nm, n=r['n'], branches=dict(r['branches']), levels=dict(r['levels']), heights=r['h'], size=r['size'], tris=k['tris'] if k else None, lods=[x.get('tris') for x in (k or {}).get('lods') or []], lodClass=(k or {}).get('lodClass'), lodAtPlay=bool((k or {}).get('lodAtPlay')), placedTris=(k['tris'] * r['n']) if k else None))
out.sort(key=lambda r: -(r['placedTris'] or 0))
brs = collections.Counter()
for r in out:
    for b, n in r['branches'].items(): brs[b] += n
json.dump(dict(total=len(L), byRoot=brs.most_common(), rows=out), sys.stdout, indent=0)
