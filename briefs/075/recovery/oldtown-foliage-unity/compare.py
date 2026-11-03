# Brief 075 OldTown foliage trial (Latch): exact baseline vs trial scene comparison from Recovery075.Manifest / Restyle075.Manifest output.
# usage: python3 -I compare.py <baseline-manifest> <trial-manifest> <baseline-restyle> <trial-restyle> <prior-paving-manifest> <out.json>
import json, sys
b, t, rb, rt, prior = (json.load(open(p)) for p in sys.argv[1:6])
G = 'oldtown-foliage075'
PLANT = ('DK_Leaf_', 'DK_Bloom_', 'DK_Flower_')
CHANGED = {'batches/civic/' + n + '@-3,-4#-12,-14' for n in ('DK_Bloom_Coral', 'DK_Bloom_Cream', 'DK_Bloom_Flamingo', 'DK_Bloom_Peach', 'DK_Leaf_CivicDark', 'DK_Leaf_Civic')}
br = {r['path']: r for r in b['renderers']}; tr = {r['path']: r for r in t['renderers']}
added = sorted(set(tr) - set(br)); removed = sorted(set(br) - set(tr))
diff = []
for p in sorted(set(br) & set(tr)):
    x, y = br[p], tr[p]
    keys = [k for k in x if x[k] != y.get(k)]
    if keys: diff.append({'path': p, 'fields': keys, 'triangles': [x['triangles'], y['triangles']], 'nonFoliageSame': x['nonFoliage'] == y['nonFoliage'], 'materialsSame': x['materials'] == y['materials'], 'expected': p in CHANGED})
bt, tt = set(b['transforms']), set(t['transforms'])
t_added = sorted(tt - bt); t_removed = sorted(bt - tt)
out = {
  'renderersBaseline': len(br), 'renderersTrial': len(tr),
  'addedRenderers': [{'path': p, 'triangles': tr[p]['triangles'], 'materials': tr[p]['materials'], 'shadow': tr[p]['shadow'], 'alpha': tr[p]['alphaMaterials']} for p in added],
  'removedRenderers': removed,
  'changedRenderers': diff,
  'unexpectedRendererChanges': [d['path'] for d in diff if not d['expected'] or not d['nonFoliageSame'] or not d['materialsSame']],
  'addedRenderersAllUnderTrialGroup': all('/' + G + '/' in p for p in added),
  'trianglesBaseline': b['meshTrianglesAllLODsIncludingInactive'], 'trianglesTrial': t['meshTrianglesAllLODsIncludingInactive'],
  'trianglesDelta': t['meshTrianglesAllLODsIncludingInactive'] - b['meshTrianglesAllLODsIncludingInactive'],
  'materialSlotsDelta': t['materialSlotsAllLODs'] - b['materialSlotsAllLODs'],
  'shadowCastersDelta': t['shadowCastingRendererCount'] - b['shadowCastingRendererCount'],
  'alphaRenderersDelta': t['alphaClippedRendererCount'] - b['alphaClippedRendererCount'],
  'transformsAdded': len(t_added), 'transformsAddedAllUnderTrialGroup': all('/' + G for x in t_added for _ in [0]) and all(('/' + G) in x.split('|')[0] for x in t_added),
  'transformsRemovedOrChanged': t_removed,
  'collidersIdentical': b['colliders'] == t['colliders'], 'colliderCount': len(t['colliders']),
  'navIdentical': b['nav'] == t['nav'], 'nav': t['nav'],
  'restyleIdentical': rb == rt,
  'baselineMatchesPriorPavingManifest': {'renderers': b['renderers'] == prior['renderers'], 'transforms': b['transforms'] == prior['transforms'], 'colliders': b['colliders'] == prior['colliders'], 'nav': b['nav'] == prior['nav']},
}
if not out['restyleIdentical']:
    def flat(o, p=''):
        if isinstance(o, dict):
            for k, v in o.items(): yield from flat(v, p + '/' + str(k))
        elif isinstance(o, list):
            for i, v in enumerate(o): yield from flat(v, p + '[' + str(i) + ']')
        else: yield p, o
    fb, ft = dict(flat(rb)), dict(flat(rt))
    out['restyleDiffs'] = sorted(k for k in set(fb) | set(ft) if fb.get(k) != ft.get(k))[:200]
out['pass'] = (not out['unexpectedRendererChanges'] and not removed and out['addedRenderersAllUnderTrialGroup'] and len(added) == 8 and not t_removed
               and out['collidersIdentical'] and out['navIdentical'] and out['transformsAddedAllUnderTrialGroup'])
json.dump(out, open(sys.argv[6], 'w'), indent=1)
print(json.dumps({k: v for k, v in out.items() if k not in ('addedRenderers', 'changedRenderers', 'restyleDiffs')}, indent=1))
