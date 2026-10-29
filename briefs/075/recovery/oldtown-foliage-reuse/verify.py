import bpy,json,math
from pathlib import Path
R=Path.cwd();E=R/'briefs/075/recovery/oldtown-foliage-reuse';O=R/'out/scratch/vela/oldtown-foliage-reuse'
m=json.loads((E/'manifest.json').read_text());bpy.ops.wm.open_mainfile(filepath=str(O/'direct-reuse.blend'))
def imported(p):
 bpy.ops.object.select_all(action='DESELECT');bpy.ops.import_scene.fbx(filepath=str(R/p),use_custom_normals=True);o=next(o for o in bpy.context.selected_objects if o.type=='MESH');bpy.context.view_layer.objects.active=o;bpy.ops.object.transform_apply(location=False,rotation=True,scale=True);return o
results=[]
for row in m['placements']:
 original=imported(next(v['path'] for n,vv in json.loads((E/'audit.json').read_text()).items() if n==row['reuseAsset'] for tag,v in vv.items() if tag=='old'))
 # Original current trough A is not in inventory; use exact manifest binding path.
 currentpath=next(p for p in m['fbxMaterialBindings'] if p.endswith('/DK_'+row['world']['kit']['id']+'.fbx') and '/Kit/' in p)
 current=imported(currentpath)
 checked=[]
 for part in row['replacementParts']:
  o=bpy.data.objects[part['name']];retain=part['name'].endswith('_retained_container') or part['name']=='existing_palm_pot_trunk';ref=current if retain else original;slots=row['retainedCurrentSlots'] if retain else row.get('reusedSourceSlots',list(range(len(ref.data.materials))))
  src=[p for p in ref.data.polygons if p.material_index in slots];dst=list(o.data.polygons);assert len(src)==len(dst)
  assert len(o.data.vertices)==len(ref.data.vertices)
  error=max((v.co-w.co).length for v,w in zip(o.data.vertices,ref.data.vertices));assert error<1e-6,(part['name'],error)
  assert all(tuple(p.vertices)==tuple(q.vertices) and p.material_index==q.material_index and p.use_smooth==q.use_smooth for p,q in zip(dst,src))
  source_loops=[ix for p in src for ix in p.loop_indices]
  assert all((o.data.uv_layers.active.data[i].uv-ref.data.uv_layers.active.data[j].uv).length<1e-6 for i,j in enumerate(source_loops))
  normal_error=max((o.data.corner_normals[i].vector-ref.data.corner_normals[j].vector).length for i,j in enumerate(source_loops));assert normal_error<.0002,(part['name'],normal_error)
  assert max(o.scale)-min(o.scale)<1e-7
  checked.append({'part':part['name'],'sourceSlots':slots,'originalVerticesFacesSlotsUVsSmoothingPreserved':True,'maxVertexErrorMetres':error,'maxCornerNormalError':normal_error})
 lo,hi=row['baseline']['bounds'];inside=all(all(lo[i]-.001<=part['bounds'][0][i] and part['bounds'][1][i]<=hi[i]+.001 for i in range(3)) for part in row['replacementParts']);assert inside,row['id']
 results.append({'placement':row['id'],'parts':checked,'containedInExistingLocalRendererEnvelope1mm':inside,'limit':'Mesh envelope preflight only. No scene door, route, terrain, collider, nav or simulation validation; Latch owns Unity check.'})
(E/'mesh-verification.json').write_text(json.dumps(results,indent=2)+'\n');print('PASS: original vertices/faces/material slots/UVs/smoothing/custom normals; uniform scales; 3 placement envelopes')
