#!/usr/bin/env python3
# Brief 086 (Redline): the ranked key-item list, from placed-census.json, lod-groups.json, piece-truth.json, the two catalogues and the
# 0.2.181 tp-long census. Read-only. Writes key-items.json and prints a markdown table. Key item = 50,000+ placed L0 triangles, or a piece
# in a tp-long census top 30 with 20+ drawn, or a piece Sky named. "placed" counts instances in OldTown.unity with its renderers on.
import json, re, collections
R = '/Users/sky/Projects/suborned/'; RL = R + 'briefs/086/redline/'
truth = {x['piece']: x for x in json.load(open(RL + 'piece-truth.json'))}
groups = {r['group']: r for r in json.load(open(RL + 'lod-groups.json'))['rows']}
old = {r['name']: r for r in json.load(open(R + 'unity/Assets/Suborned/OldTownArt/DecoKit/decokit_manifest.json'))['assets']}
kit = {r['id']: r for r in json.load(open(R + 'contracts/world/kit.json'))['assets']}
def census(view):
    c = collections.Counter()
    for p in json.load(open(R + f'out/batch/tplong/census-{view}.json'))['byPiece']:
        c[re.sub(r'^DK_', '', p['key'].split(' #')[0])] += p['drawn']
    return c
tw, ts = census('tp-worst'), census('tp-street')
names = set(truth) | set(groups)
rows = []
for n in names:
    t, g = truth.get(n), groups.get(n)
    oa_n = t['placed'] if t else 0; oa_t = old[n]['tris'] if t and n in old else 0
    k = kit.get(n, {}); our_n = g['n'] if g else 0; our_t = k.get('tris', 0)
    cost = oa_n * oa_t + our_n * our_t
    alias = {'Kerb_Granite_0': 'Kerb_Granite'}.get(n, n)
    rows.append(dict(piece=n, oldN=oa_n, oldTris=oa_t, oursN=our_n, oursTris=our_t, placedTris=cost,
        kitLods=[x.get('tris') for x in k.get('lods') or []], oursLevels=len(g['heights']) if g else 0, oursHeights=g['heights'] if g else [], size=(g['size'] if g else None),
        lodAtPlay=bool(k.get('lodAtPlay')), sameL0=t['sameAsKitL0'] if t else None, tpWorst=tw.get(alias, 0), tpStreet=ts.get(alias, 0), prefabLod=t['prefabLODGroup'] if t else None))
rows.sort(key=lambda r: -r['placedTris'])
named = {'Hydrant', 'Mailbox', 'Bollard', 'Streetlamp'}
key = [r for r in rows if r['placedTris'] >= 50000 or r['tpWorst'] >= 20 or r['tpStreet'] >= 20 or r['piece'] in named]
json.dump(dict(rule=__doc__ if False else 'see header', total=len(rows), key=key, all=rows), open(RL + 'key-items.json', 'w'), indent=0)
def state(r):
    a = []
    if r['oldN']: a.append('old: ' + ('prefab LOD' if r['prefabLod'] else 'none'))
    if r['oursN']: a.append('ours: ' + ('cull only' if r['oursLevels'] == 1 else f"{r['oursLevels'] - 1} level{'s' if r['oursLevels'] > 2 else ''} + cull"))
    return ', '.join(a)
print('| # | piece | old-art placed x tris | ours placed x tris | placed L0 tris | kit levels unused in old-art | LODs in the scene | tp-worst / tp-street drawn |')
print('|---|---|---:|---:|---:|---|---|---:|')
for i, r in enumerate(key, 1):
    print(f"| {i} | {r['piece']} | {r['oldN']} x {r['oldTris']:,} | {r['oursN']} x {r['oursTris']:,} | {r['placedTris']:,} | {' / '.join(f'{x:,}' for x in r['kitLods']) or 'none'} | {state(r)} | {r['tpWorst']} / {r['tpStreet']} |")
print('\nrows', len(key), 'of', len(rows), 'sum', sum(r['placedTris'] for r in rows), 'key sum', sum(r['placedTris'] for r in key))
