from pathlib import Path
import argparse,json,hashlib,sys
D=Path(__file__).resolve().parent; ROOT=D.parents[3]
parser=argparse.ArgumentParser(description='Compare pinned Recovery075 manifests; fail if any check fails.')
parser.add_argument('--output-dir',type=Path,default=D,help='Write results here without replacing the captured evidence.')
output=parser.parse_args().output_dir
output.mkdir(parents=True,exist_ok=True)
load=lambda p:json.loads((D/p).read_text())
b,p,f=[load(t+'.json') for t in ['before','paving','foliage']]
by=lambda m:{r['path']:r for r in m['renderers']}
B,P,F=map(by,[b,p,f]); changed=[]; extra=[]
for k in sorted(P.keys()|F.keys()):
 if k not in P or k not in F:extra.append(k);continue
 if P[k]!=F[k]:changed.append({'path':k,'fields':[q for q in P[k] if P[k][q]!=F[k][q]],'beforeTriangles':P[k]['triangles'],'afterTriangles':F[k]['triangles'],'nonFoliageEqual':P[k]['nonFoliage']==F[k]['nonFoliage']})
placements=load('placements.json')['placements']; expected={r['renderer'] for r in placements}
expected_ids={'civic/run-west-cafe/1','civic/civic-office-north/tuft-0','civic/cyp-office-e'}
# Paving rebatches material groups; Restyle075 hashes the world-space triangle multiset independent of batch grouping.
br,pr,fr=[load(t+'-restyle.json') for t in ['before','paving','foliage']]
r={
 'status':'PROVISIONAL; G-3a59>58 BLOCKED; no performance acceptance',
 'checks':{
  'foliage_exact_three_placements':len(placements)==3 and {q['id'] for q in placements}==expected_ids,
  'foliage_changes_only_selected_renderers_and_logged_batches':set(c['path'] for c in changed)==(expected & P.keys())|{b for q in placements for b in q.get('batchesChanged',[])} and set(extra)==expected-P.keys(),
  'foliage_original_trough_and_trunk_geometry_retained':all(c['nonFoliageEqual'] for c in changed),
  'foliage_two_baked_placements_gain_renderers':len(F)-len(P)==2,
  'foliage_colliders_exact':p['colliders']==f['colliders'],
  'foliage_transforms_exact':p['transforms']==f['transforms'],
  'foliage_nav_exact':p['nav']==f['nav'],
  'paving_colliders_exact':b['colliders']==p['colliders'],
  'paving_nav_exact':b['nav']==p['nav'],
  'paving_ground_geometry_unchanged_in_zone':br['inZone']['batchGeometryHash']==pr['inZone']['batchGeometryHash'],
  'paving_ground_geometry_unchanged_outside':br['outside']['batchGeometryHash']==pr['outside']['batchGeometryHash'],
  'paving_kit_geometry_unchanged':all(br[k]['kitHash']==pr[k]['kitHash'] for k in ['inZone','outside']),
  'paving_outside_materials_unchanged':br['outside']['batchGeometryWithMaterialHash']==pr['outside']['batchGeometryWithMaterialHash'] and br['outside']['kitWithMaterialHash']==pr['outside']['kitWithMaterialHash'],
 },
 'foliageChangedRenderers':changed,'unexpectedPaths':extra,
 'counts':{tag:{k:m[k] for k in ['meshTrianglesAllLODsIncludingInactive','materialSlotsAllLODs','shadowCastingRendererCount','alphaClippedRendererCount']}|{'rendererCount':len(m['renderers'])} for tag,m in [('before',b),('paving',p),('foliage',f)]},
 'nav':[m['nav'] for m in [b,p,f]],
 'pavingTransformDifferences':sorted(set(b['transforms'])^set(p['transforms']))[:24],
}
pin=load('inputs-before.json');after={k:hashlib.sha256((ROOT/k).read_bytes()).hexdigest() for k in pin}
(output/'inputs-after.json').write_text(json.dumps(after,indent=2)+'\n')
r['inputChanges']=[k for k in pin if pin[k]!=after[k]]
r['checks']['world_walk_sim_plan_catalogue_and_candidate_sources_unchanged']=not [k for k in r['inputChanges'] if not k.startswith('unity/ProjectSettings/') and k!='briefs/055/geometry.summary.json']
(output/'comparison.json').write_text(json.dumps(r,indent=2)+'\n')
print(json.dumps(r['checks'],indent=2));print('Changed foliage renderers:',len(changed));print('Input changes:',r['inputChanges'])
failed=[name for name,passed in r['checks'].items() if not passed]
if failed:
 print('Failed checks: '+', '.join(failed),file=sys.stderr)
sys.exit(1 if failed else 0)
