"""Isolated recipe study; no production/Unity writes. Run guarded Blender -t 4."""
import hashlib, importlib.util, json, math, re, sys
from pathlib import Path
import bpy
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[4]
E=Path(__file__).resolve().parent
OUT=ROOT/'out/scratch/vela/foliage-refine'
sys.path.insert(0,str(ROOT/'tools/decokit'))
import kit, mats, recovery075_foliage as new
spec=importlib.util.spec_from_file_location('prior',E/'previous_recipe.py')
prior=importlib.util.module_from_spec(spec);spec.loader.exec_module(prior)
OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
names=[p+'_'+v for p in ('BedTuft','PlanterRun_4') for v in 'ABC']
generator_start_hash=hashlib.sha256((ROOT/'tools/decokit/recovery075_foliage.py').read_bytes()).hexdigest()
report={'status':'isolated candidates; not adopted','assets':{},'placements':[], 'renders':[], 'sources':{}}
objects={}
def snap(o):
 m=o.data
 return dict(bounds=[[min(v.co[i] for v in m.vertices) for i in range(3)],[max(v.co[i] for v in m.vertices) for i in range(3)]],triangles=sum(len(p.vertices)-2 for p in m.polygons),materials=[m.name for m in m.materials],trianglesByMaterial={mat.name:sum(len(p.vertices)-2 for p in m.polygons if p.material_index==i) for i,mat in enumerate(m.materials)})
def hard(o):
 m=o.data
 return [(m.materials[p.material_index].name,[tuple(v for v in m.vertices[i].co) for i in p.vertices]) for p in m.polygons if not m.materials[p.material_index].name.startswith(('DK_Leaf_','DK_Bloom_'))]
def traces(builder,name):
 calls=[]
 l,b=builder.leafy_lobe,builder.petal_bloom
 def leaf(*a,**kw):
  calls.append(['leaf',list(a[1]),a[2],a[3],a[4].name]);return l(*a,**kw)
 def bloom(*a,**kw):
  calls.append(['bloom',list(a[1]),a[2],a[3].name]);return b(*a,**kw)
 builder.leafy_lobe,builder.petal_bloom=leaf,bloom
 try:o=builder.build_candidate(name)
 finally:builder.leafy_lobe,builder.petal_bloom=l,b
 return o,calls
for name in names:
 baseline=kit.build(name)
 old,t0=traces(prior,name)
 # Recorded first-round exporter clamped its generated source envelope.
 box=snap(baseline)['bounds']
 for v in old.data.vertices:
  for i in range(3):v.co[i]=max(box[0][i],min(box[1][i],v.co[i]))
 candidate,t1=traces(new,name)
 assert t0==t1,(name,'seeded positions changed')
 assert hard(baseline)==hard(candidate),(name,'trough changed')
 states={'source':baseline,'previous':old,'refined':candidate}
 for tag,o in states.items():o.name=name+'_'+tag;o.hide_render=True;objects[name,tag]=o
 a=snap(candidate)
 assert all(box[0][i]-1e-6<=a['bounds'][0][i] and a['bounds'][1][i]<=box[1][i]+1e-6 for i in range(3)),(name,box,a['bounds'])
 assert not any(m.type=='DECIMATE' for m in candidate.modifiers)
 bpy.ops.object.select_all(action='DESELECT');candidate.select_set(True);bpy.context.view_layer.objects.active=candidate
 f=OUT/(name+'_Refine075.fbx')
 bpy.ops.export_scene.fbx(filepath=str(f),use_selection=True,object_types={'MESH'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y',bake_space_transform=True,use_mesh_modifiers=False,mesh_smooth_type='OFF',use_tspace=False,add_leaf_bones=False,path_mode='STRIP',embed_textures=False)
 report['assets'][name]={tag:snap(o) for tag,o in states.items()}
 report['assets'][name].update(fbx=str(f.relative_to(ROOT)),sha256=hashlib.sha256(f.read_bytes()).hexdigest(),nonPlantPolygonPositionsExact=True,seededCallsExact=True,sourceEnvelopeContained=True,seededCalls=t1)
# Placement-aware preflight against Latch's FINAL THREE, never rejected tufts.
world=json.loads((ROOT/'out/world/harbour-district.world.json').read_text())
proof=json.loads((ROOT/'briefs/075/recovery/latch-integration/bounds-proof.json').read_text())
for row in proof['placements']:
 id=row['id'];mass=next(m for m in world['civic'] if m['id']==id)
 if mass['kit']['id']=='Cypress_Play':
  report['placements'].append({'id':id,'status':'unchanged; no new cypress supplied','baselineTriangles':5004,'deltaTriangles':0});continue
 k=mass['kit'];o=objects[k['id'],'refined'];points=[]
 for p in o.data.polygons:
  if not o.data.materials[p.material_index].name.startswith(('DK_Leaf_','DK_Bloom_')):continue
  for vi in p.vertices:
   x,y,z=o.data.vertices[vi].co
   # FBX/Unity handedness: (-BlenderX, BlenderZ, BlenderY), then Unity Y rotation -yaw.
   a=k['yaw'];points.append((k['at'][0]-math.cos(a)*x-math.sin(a)*y,k['base']+z,k['at'][1]-math.sin(a)*x+math.cos(a)*y))
 bounds=[[min(p[i] for p in points) for i in range(3)],[max(p[i] for p in points) for i in range(3)]]
 n=[float(s) for s in re.findall(r'-?\d+\.\d+',row['sourceBounds'])];lo=[n[i]-n[i+3] for i in range(3)];hi=[n[i]+n[i+3] for i in range(3)]
 passed=all(lo[i]-.001<=bounds[0][i] and bounds[1][i]<=hi[i]+.001 for i in range(3))
 assert passed,(id,bounds,lo,hi)
 report['placements'].append(dict(id=id,kit=k,sourceWorldBounds=[lo,hi],candidatePlantWorldBounds=bounds,preflightContained1mm=passed,proofLimit='source-space axis/pose preflight, not actual Unity FBX import validation'))
# Full-resolution texture reused, with explicit material recipe for Latch.
img,_=mats._texture('DK_Leaf_Shrub');p=Path(img.filepath)
report['texture']={'path':str(p),'dimensions':list(img.size),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()}
report['materialRecipe']={'namePrefix':'DK_Leaf_Refine075_','baseColorLinear':'0.75 * original DK_Leaf_Civic or DK_Leaf_CivicDark + 0.25 * shrub source RGB','alpha':'source alpha > 0.5','roughness':.85,'specularIORLevelBlender':.1,'core':'original seeded civic material instead of DK_Leaf_CoreShrub','normal':'lobe normal, z lift .35, jitter .025','unity':'requires explicit review-only material creation; do not map refined slots to unchanged DK_Leaf_Shrub'}
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=16;scene.cycles.use_denoising=True
scene.world.color=(.35,.35,.35);scene.view_settings.view_transform='AgX'
bpy.ops.object.light_add(type='AREA',location=(-3,-4,9));bpy.context.object.data.energy=1800;bpy.context.object.data.size=7
bpy.ops.object.camera_add();cam=scene.camera=bpy.context.object;cam.data.type='ORTHO'
def shot(name,tag,play=False,yaw=30):
 o=objects[name,tag];o.hide_render=False
 target=Vector((0,0,snap(objects[name,'source'])['bounds'][1][2]*.48))
 a=math.radians(yaw);pitch=math.radians(38)
 cam.location=target+Vector((math.sin(a)*math.cos(pitch)*10,-math.cos(a)*math.cos(pitch)*10,math.sin(pitch)*10))
 cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
 scene.render.resolution_x,scene.render.resolution_y=(1920,1080) if play else (1200,800)
 cam.data.ortho_scale=28*1920/1080 if play else (5 if 'Run' in name else 1.2)
 stem=f'{name}-{tag}-'+(f'play-y{yaw}' if play else 'close')
 scene.render.filepath=str(E/(stem+'.png'));bpy.ops.render.render(write_still=True);o.hide_render=True
 report['renders'].append(dict(file=stem+'.png',horizontalOrthoScale=cam.data.ortho_scale,pitch=38,yaw=yaw,engine='Cycles isolated; no Unity grade/occluders'))
for name in names:shot(name,'refined')
for name in ('BedTuft_A','PlanterRun_4_B'):
 for tag in ('source','previous'):shot(name,tag)
 for yaw in (30,210):
  for tag in ('source','previous','refined'):shot(name,tag,True,yaw)
for p in [ROOT/'tools/decokit/recovery075_foliage.py',E/'previous_recipe.py',Path(__file__)]:report['sources'][str(p.relative_to(ROOT))]=hashlib.sha256(p.read_bytes()).hexdigest()
assert generator_start_hash==report['sources']['tools/decokit/recovery075_foliage.py'], 'generator edited during run'
(E/'metrics.json').write_text(json.dumps(report,indent=2)+'\n')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'foliage-refine.blend'))
print('FOLIAGE REFINE PASS: 6 assets, exact seeded calls/nonplant positions, source bounds, two placed-envelope preflights, 22 renders')
