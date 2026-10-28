"""Brief 075-E step 1 (Latch): the 075 rebuild against the 075-E streamline rebuild, offscreen estimates (After072 estimator) and the colourway census -> briefs/075/latch/stream/counts.{json,md}.
Usage: python3 -I briefs/075/latch/stream/report.py <repo root>"""
import sys, os, json
root = sys.argv[1]; d = os.path.join(root, 'briefs', '075', 'latch', 'stream')
L = lambda p: json.load(open(os.path.join(d, p)))
ob, oa, cb, ca = L('../offscreen.json'), L('offscreen.json'), L('../census.json'), L('census.json')
rows = []
for x, y in zip(ob['modes']['every'], oa['modes']['every']):
    assert x['view'] == y['view'] and x['yaw'] == y['yaw']
    r = {'view': f"{x['view']} y{int(x['yaw']):03d}"}
    for k, f in (('materials', lambda v: v['materials']), ('setPass', lambda v: v['setPassEstimate']['total']), ('fwd', lambda v: v['setPassEstimate']['forward']),
                 ('shadowVariants', lambda v: v['setPassEstimate']['shadow']), ('mainRenderers', lambda v: v['mainRenderers']), ('mainTris', lambda v: v['mainTriangles']),
                 ('shadowTris', lambda v: v['shadow']['triangles']), ('casters', lambda v: v['shadow']['uniqueCasters']), ('frameMsMedian', lambda v: v['frameMs']['median']), ('modules', lambda v: v['modules']['modulesInFrustum'])):
        r[k] = [f(x), f(y), f(y) - f(x)]
    rows.append(r)
out = {'schema': 'suborned.075e-counts/1', 'before': {'export': ob['export'], 'when': ob['when']}, 'after': {'export': oa['export'], 'when': oa['when']},
       'note': ob['notUnityStats'], 'views': rows,
       'census': {'before': {k: cb[k] for k in ('columns', 'materials')}, 'after': {k: ca[k] for k in ('columns', 'materials')},
                  'columnMeshesPlaced': {'before': cb['columns']['distinctMeshesPlaced'], 'after': ca['columns']['distinctMeshesPlaced']}, 'proxyMaterialsAfter': ca['shadowProxies']['materials'], 'moduleRootsByWallMaterialAfter': ca['moduleRootsByWallMaterial'],
                  'moduleShaderVariants': {'before': cb['moduleShaderVariants'], 'after': ca['moduleShaderVariants']}}}
json.dump(out, open(os.path.join(d, 'counts.json'), 'w'), indent=1)
md = ['# 075-E step 1 counts: the 075 rebuild against the streamline rebuild (batchmode offscreen, estimates, not UnityStats)', '',
      f"Before: {ob['export']} ({ob['when']}). After: {oa['export']} ({oa['when']}).", '',
      '| view | materials | SetPass est. (fwd) | shadow variants | main renderers | main tris | shadow tris | casters | modules in frustum | frame ms (median, indicative) |', '|---|---|---|---|---|---|---|---|---|---|']
fmt = lambda t: f"{t[0]:,} → {t[1]:,} ({t[2]:+,})"
for r in rows:
    md.append(f"| {r['view']} | {fmt(r['materials'])} | {fmt(r['setPass'])} ({fmt(r['fwd'])}) | {fmt(r['shadowVariants'])} | {fmt(r['mainRenderers'])} | {fmt(r['mainTris'])} | {fmt(r['shadowTris'])} | {fmt(r['casters'])} | {fmt(r['modules'])} | {r['frameMsMedian'][0]} → {r['frameMsMedian'][1]} |")
md += ['', '## Census', '', '| | before | after |', '|---|---|---|']
for g in ('columns', 'materials'):
    for k in cb[g]: md.append(f"| {g}.{k} | {cb[g][k]:,} | {ca[g][k]:,} |")
md += ['', f"Shadow proxy materials after ({len(ca['shadowProxies']['materials'])}): {', '.join(ca['shadowProxies']['materials'])}", '',
       f"Module shader variants before: {cb['moduleShaderVariants']}", '', f"after: {ca['moduleShaderVariants']}", '',
       f"Module roots by wall material after: {ca['moduleRootsByWallMaterial']}", '']
open(os.path.join(d, 'counts.md'), 'w').write('\n'.join(md)); print('\n'.join(md))
