#!/usr/bin/env python3
# Brief 086 (Redline): count what OldTown.unity places, read-only, from the scene YAML, with no Unity.
#
#   python3 -I briefs/086/redline/placed_census.py [scene] > briefs/086/redline/placed-census.json
#
# Output per source prefab or FBX: instances placed, by branch (handover zone, network, the old town, other); the LOD
# levels and triangles from the catalogues; whether it has a LODGroup and whether it casts. Direct scene renderers
# (merged batches with embedded meshes) are summed per branch and by name stem, with triangles from `m_SubMeshes`.
# Limits: it does not resolve nested-prefab overrides of mesh or of shadow mode, and a placed piece's tris are its
# catalogue tris (the prefab's own L0, unscaled). Inactive and renderer-disabled instances are counted apart.
import json, os, re, sys, collections

ROOT = '/Users/sky/Projects/suborned'
SCENE = sys.argv[1] if len(sys.argv) > 1 else f'{ROOT}/unity/Assets/Scenes/OldTown.unity'
ASSETS = f'{ROOT}/unity/Assets'

guid2path = {}
for base, _, files in os.walk(ASSETS):
    if '/probe' in base: continue
    for f in files:
        if f.endswith('.meta'):
            with open(os.path.join(base, f), errors='ignore') as h:
                for line in h:
                    if line.startswith('guid:'):
                        guid2path[line[5:].strip()] = os.path.join(base, f[:-5])[len(ASSETS) + 1:]
                        break

docs = collections.defaultdict(dict)  # kind -> id -> fields
hdr = re.compile(r'^--- !u!(\d+) &(-?\d+)( stripped)?')
fid = re.compile(r'fileID: (-?\d+)')
guid = re.compile(r'guid: ([0-9a-f]+)')
kind, cur = None, None
want = {'1', '4', '23', '33', '43', '205', '1001'}
with open(SCENE, errors='ignore') as h:
    for line in h:
        if line.startswith('---'):
            m = hdr.match(line)
            kind = m.group(1) if m else None
            cur = None
            if kind in want:
                cur = docs[kind].setdefault(m.group(2), {})
                if m.group(3): cur['stripped'] = True
                if kind == '1001': cur['mods'] = []
                if kind == '43': cur['idx'] = 0
                if kind == '205': cur['heights'] = []
            continue
        if cur is None: continue
        s = line.strip()
        if kind == '1':
            if s.startswith('m_Name:'): cur['name'] = s[7:].strip()
            elif s.startswith('m_IsActive:'): cur['active'] = s.endswith('1')
            elif s.startswith('m_PrefabInstance:'): cur['pi'] = fid.search(s).group(1)
        elif kind == '4':
            if s.startswith('m_GameObject:'): cur['go'] = fid.search(s).group(1)
            elif s.startswith('m_Father:'): cur['father'] = fid.search(s).group(1)
            elif s.startswith('m_PrefabInstance:'): cur['pi'] = fid.search(s).group(1)
        elif kind == '23':
            if s.startswith('m_GameObject:'): cur['go'] = fid.search(s).group(1)
            elif s.startswith('m_CastShadows:'): cur['cast'] = int(s.split(':')[1])
            elif s.startswith('m_Enabled:'): cur['enabled'] = s.endswith('1')
            elif s.startswith('m_StaticBatchInfo'): pass
        elif kind == '33':
            if s.startswith('m_GameObject:'): cur['go'] = fid.search(s).group(1)
            elif s.startswith('m_Mesh:'):
                cur['mesh'] = fid.search(s).group(1)
                g = guid.search(s)
                if g: cur['meshGuid'] = g.group(1)
        elif kind == '43':
            if s.startswith('indexCount:'): cur['idx'] += int(s.split(':')[1])
        elif kind == '205':
            if s.startswith('m_GameObject:'): cur['go'] = fid.search(s).group(1)
            elif s.startswith('m_FadeMode:'): cur['fade'] = int(s.split(':')[1])
            elif s.startswith('m_Size:'): cur['size'] = float(s.split(':')[1])
            elif s.startswith('- screenRelativeHeight:'): cur['heights'].append(float(s.split(':')[1]))
        elif kind == '1001':
            if s.startswith('m_TransformParent:'): cur['parent'] = fid.search(s).group(1)
            elif s.startswith('m_SourcePrefab:'):
                g = guid.search(s)
                if g: cur['src'] = g.group(1)
            elif s.startswith('propertyPath:'): cur['_p'] = s[13:].strip()
            elif s.startswith('value:') and '_p' in cur:
                if cur['_p'] in ('m_IsActive', 'm_CastShadows', 'm_Name'): cur['mods'].append((cur['_p'], s[6:].strip()))
                del cur['_p']

T, G, PI = docs['4'], docs['1'], docs['1001']

# The scene's transforms and prefab instances form the tree: a stripped transform stands for its instance's root.
tr_pi = {tid: t.get('pi') for tid, t in T.items() if t.get('stripped')}

def chain(tid, limit=64):
    out, active = [], True
    while tid and tid != '0' and limit:
        limit -= 1
        t = T.get(tid)
        if not t: break
        if t.get('stripped'):
            p = PI.get(t.get('pi'), {})
            for k, v in p.get('mods', []):
                if k == 'm_IsActive' and v == '0': active = False
            tid = p.get('parent')
            continue
        g = G.get(t.get('go'), {})
        if g.get('active') is False: active = False
        out.append(g.get('name', '?'))
        tid = t.get('father')
    return list(reversed(out)), active

def branch(names):
    if not names: return 'root'
    n = names[0]
    if n.startswith('Ours'): return 'handover'
    if n.startswith('Network'): return 'network'
    return 'oldtown'

# catalogues: ours (contracts/world/kit.json) and the old art's manifest
kit = json.load(open(f'{ROOT}/contracts/world/kit.json'))
rows = {}
for r in kit['assets']:
    rows[r['id']] = dict(tris=r['tris'], lods=[l.get('tris') for l in r.get('lods') or []], screens=[l.get('screen') for l in r.get('lods') or []], category=r['category'], lodClass=r.get('lodClass'), lodAtPlay=bool(r.get('lodAtPlay')), size=r.get('size'), status=r.get('status'))
old = {}
for r in json.load(open(f'{ASSETS}/Suborned/OldTownArt/DecoKit/decokit_manifest.json'))['assets']:
    old[r['name']] = dict(tris=r['tris'], size=r.get('size'), category=r['category'], lodFar=r.get('lodFar'), near=r.get('near'), far=r.get('far'), keys=sorted(r.keys()))

placed = collections.defaultdict(lambda: collections.Counter())
inactive = collections.Counter()
for pid, p in PI.items():
    src = p.get('src')
    path = guid2path.get(src, f'guid:{src}')
    names, active = chain(p.get('parent'))
    for k, v in p.get('mods', []):
        if k == 'm_IsActive' and v == '0': active = False
    if not active: inactive[path] += 1; continue
    placed[path][branch(names)] += 1

# direct renderers: merged batches and other scene meshes
lodded = set()
for l in docs['205'].values():
    pass
direct = collections.defaultdict(lambda: dict(n=0, tris=0, casters=0, castTris=0, external=0))
fgo = {f.get('go'): f for f in docs['33'].values()}
go2tr = {t.get('go'): tid for tid, t in T.items()}
stem = re.compile(r'[@#].*$')
ext = collections.Counter()
for rid, r in docs['23'].items():
    gid = r.get('go')
    g = G.get(gid)
    if not g or r.get('enabled') is False: continue
    names, active = chain(go2tr.get(gid))
    if not active: continue
    f = fgo.get(gid, {})
    tris = docs['43'].get(f.get('mesh'), {}).get('idx', 0) // 3
    if f.get('meshGuid'):
        ext[guid2path.get(f['meshGuid'], f['meshGuid'])] += 1
    br = branch(names)
    # the two levels below the root name the layer (batches/roofs, street, ...), the name's stem the material
    layer = '/'.join(names[1:3]) if br != 'oldtown' else (names[0] if names else '?')
    d = direct[(br, layer)]
    d['n'] += 1; d['tris'] += tris
    if r.get('cast', 1) != 0: d['casters'] += 1; d['castTris'] += tris
    if f.get('meshGuid'): d['external'] += 1

def name_of(path):
    b = os.path.splitext(os.path.basename(path))[0]
    return re.sub(r'^DK_', '', b)

out = []
for path, c in placed.items():
    nm = name_of(path)
    k = rows.get(nm) or rows.get(re.sub(r'_(TB|Play)$', '', nm))
    o = old.get(nm)
    src_tris = k['tris'] if k else (o['tris'] if o else None)
    out.append(dict(piece=nm, path=path, placed=sum(c.values()), by=dict(c), tris=src_tris, catalogue='kit' if k else 'old' if o else None,
                    lodTris=k['lods'] if k else None, screens=k['screens'] if k else None, lodClass=k['lodClass'] if k else None,
                    lodAtPlay=k['lodAtPlay'] if k else None, size=(k or o or {}).get('size'),
                    placedTris=(sum(c.values()) * src_tris) if src_tris else None))
out.sort(key=lambda r: -(r['placedTris'] or 0))

summary = dict(scene=SCENE, bytes=os.path.getsize(SCENE),
    docs={k: len(v) for k, v in docs.items()},
    lodGroups=dict(n=len(docs['205']), fadeModes=collections.Counter(l.get('fade') for l in docs['205'].values()),
                   levels=collections.Counter(len(l['heights']) for l in docs['205'].values())),
    embeddedMeshTris=sum(m['idx'] for m in docs['43'].values()) // 3,
    prefabInstancesPlaced=sum(sum(c.values()) for c in placed.values()), prefabInstancesInactive=sum(inactive.values()),
    unresolvedTris=sum(r['placed'] for r in out if r['tris'] is None))
direct_rows = sorted(({'branch': k[0], 'layer': k[1], **v} for k, v in direct.items()), key=lambda r: -r['tris'])
json.dump(dict(summary=summary, placed=out, direct=direct_rows[:80], externalDirect=ext.most_common(60), inactive=inactive.most_common(20)), sys.stdout, indent=0, default=str)
