#!/usr/bin/env python3
# Brief 086 (Redline): for each placed old-art prefab, does it carry LODs, and does its FBX equal the kit's L0 (same sha256)?
# Reads placed-census.json, the old-art prefabs and models, contracts/world/kit.json, unity/Assets/Suborned/Kit. Read-only; prints JSON.
import hashlib, json, os, re, sys
ROOT = '/Users/sky/Projects/suborned'; A = f'{ROOT}/unity/Assets/Suborned'
sha = lambda p: hashlib.sha256(open(p, 'rb').read()).hexdigest()
d = json.load(open(f'{ROOT}/briefs/086/redline/placed-census.json'))
kit = {r['id']: r for r in json.load(open(f'{ROOT}/contracts/world/kit.json'))['assets']}
g2p = {}
for b, _, fs in os.walk(f'{A}/OldTownArt/DecoKit/Models'):
    for f in fs:
        if f.endswith('.meta'):
            for line in open(os.path.join(b, f), errors='ignore'):
                if line.startswith('guid:'): g2p[line[5:].strip()] = os.path.join(b, f[:-5]); break
out = []
for r in d['placed']:
    if 'OldTownArt' not in r['path']: continue
    p = f"{ROOT}/unity/Assets/{r['path']}"
    t = open(p, errors='ignore').read()
    m = re.search(r'm_SourcePrefab: \{fileID: \d+, guid: ([0-9a-f]+)', t)
    fbx = g2p.get(m.group(1)) if m else None
    own = sha(fbx) if fbx and os.path.exists(fbx) else None
    k = kit.get(r['piece'])
    kf = next((f for f in (k or {}).get('files', []) if f.get('role') == 'fbx'), None)
    fbx_lods = bool(re.search(r'_L\d', ' '.join(os.listdir(os.path.dirname(fbx))))) if fbx else None
    siblings = sorted(x for x in os.listdir(os.path.dirname(fbx)) if re.match(re.escape(os.path.basename(fbx)[:-4]) + r'(_L\d|_LOD\d).*\.fbx$', x)) if fbx else []
    meta = open(fbx + '.meta', errors='ignore').read() if fbx else ''
    out.append(dict(piece=r['piece'], placed=r['placed'], by=r['by'], tris=r['tris'], placedTris=r['placedTris'],
        prefabLODGroup='!u!205' in t, oldFbxLevels=siblings, sameAsKitL0=(own == kf['sha256']) if own and kf else None,
        kitStatus=(k or {}).get('status'), kitLods=[l.get('tris') for l in (k or {}).get('lods') or []], kitFiles=[l.get('file') for l in (k or {}).get('lods') or []][:1],
        fbxHasLodNames=bool(re.search(r'importAnimation', meta)) and None))
json.dump(out, sys.stdout, indent=0)
