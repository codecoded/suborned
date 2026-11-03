"""Read-only FBX/Unity-material reuse study. Writes only this evidence folder + own scratch."""
import bpy,json,re,hashlib,math
from pathlib import Path
from mathutils import Vector
R=Path.cwd();E=R/'briefs/075/recovery/oldtown-foliage-reuse';O=R/'out/scratch/vela/oldtown-foliage-reuse'
OLD=R/'unity/Assets/Suborned/OldTownArt/DecoKit';CUR=R/'unity/Assets/Suborned/Kit'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def rel(p):return str(p.relative_to(R))
audit=json.loads((E/'audit.json').read_text()); report={'status':'review-only direct reuse; no production adoption','sources':{},'materials':{},'placements':[],'renders':[],'shaderLimits':['Cycles Principled approximates URP Lit/Complex Lit BRDF; Unity AlphaToMask MSAA coverage, reflection probes, grade and generated runtime LOD selection are not reproduced. Full source-resolution images, original UVs, white/tinted BaseColor, cutoff and two-sided semantics are mapped explicitly.']}
guid={}
for root in [OLD,CUR]:
 d={}
 for p in root.rglob('*.meta'):
  m=re.search(r'^guid: (\w+)',p.read_text(),re.M)
  if m:d[m[1]]=Path(str(p)[:-5])
 guid[root]=d
bpy.ops.wm.read_factory_settings(use_empty=True)
cache={}
def material(root,name):
 key=rel(root)+'/'+name
 if key in cache:return cache[key]
 p=root/'Materials'/(name+'.mat');s=p.read_text();report['sources'][rel(p)]=sha(p)
 def f(n,default=0):
  m=re.search(r'- '+re.escape(n)+r': ([-\d.eE]+)',s);return float(m[1]) if m else default
 def color(n):
  m=re.search(r'- '+n+r': \{r: ([^,]+), g: ([^,]+), b: ([^,]+), a: ([^}]+)',s);
  raw=tuple(map(float,m.groups())) if m else (1,1,1,1)
  return tuple((v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4) for v in raw[:3])+(raw[3],)
 m=bpy.data.materials.new(key);m.use_nodes=True;nd=m.node_tree.nodes;lk=m.node_tree.links;bs=nd.get('Principled BSDF');bs.inputs['Base Color'].default_value=color('_BaseColor');bs.inputs['Metallic'].default_value=f('_Metallic');bs.inputs['Roughness'].default_value=1-f('_Smoothness',.5);bs.inputs['Coat Weight'].default_value=f('_ClearCoatMask');bs.inputs['Coat Roughness'].default_value=1-f('_ClearCoatSmoothness',.97)
 bs.inputs['Specular IOR Level'].default_value=.5 if f('_SpecularHighlights',1) else 0
 textures={}
 for prop,gg,scx,scy,ofx,ofy in re.findall(r'- (\w+):\s+m_Texture: \{fileID: [^,}]+(?:, guid: (\w+), type: \d+)?\}\s+m_Scale: \{x: ([^,]+), y: ([^}]+)\}\s+m_Offset: \{x: ([^,]+), y: ([^}]+)\}',s):
  if not gg:continue
  path=guid[root].get(gg)
  assert path and path.exists(),(name,prop,gg,'missing original texture')
  textures[prop]={'path':rel(path),'sha256':sha(path),'scale':[float(scx),float(scy)],'offset':[float(ofx),float(ofy)]};report['sources'][rel(path)]=sha(path)
  if prop not in ['_BaseMap','_BumpMap']:continue
  tex=nd.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(path),check_existing=True);textures[prop]['dimensions']=list(tex.image.size)
  uv=nd.new('ShaderNodeTexCoord');mp=nd.new('ShaderNodeMapping');mp.inputs['Scale'].default_value=(float(scx),float(scy),1);mp.inputs['Location'].default_value=(float(ofx),float(ofy),0);lk.new(uv.outputs['UV'],mp.inputs['Vector']);lk.new(mp.outputs['Vector'],tex.inputs['Vector'])
  if prop=='_BaseMap':
   mul=nd.new('ShaderNodeMixRGB');mul.blend_type='MULTIPLY';mul.inputs[0].default_value=1;mul.inputs[2].default_value=color('_BaseColor');lk.new(tex.outputs['Color'],mul.inputs[1]);lk.new(mul.outputs[0],bs.inputs['Base Color'])
   if f('_AlphaClip'):
    cut=nd.new('ShaderNodeMath');cut.operation='GREATER_THAN';cut.inputs[1].default_value=f('_Cutoff',.5);lk.new(tex.outputs['Alpha'],cut.inputs[0]);lk.new(cut.outputs[0],bs.inputs['Alpha'])
  else:
   tex.image.colorspace_settings.name='Non-Color';normal=nd.new('ShaderNodeNormalMap');normal.inputs['Strength'].default_value=f('_BumpScale',1);lk.new(tex.outputs['Color'],normal.inputs['Color']);lk.new(normal.outputs['Normal'],bs.inputs['Normal'])
 m.use_backface_culling=f('_Cull',2)!=0
 report['materials'][key]={'path':rel(p),'sha256':sha(p),'shaderGuid':re.search(r'm_Shader:.*guid: (\w+)',s)[1],'textures':textures,'baseColor':color('_BaseColor'),'metallic':f('_Metallic'),'smoothness':f('_Smoothness'),'alphaClip':f('_AlphaClip'),'cutoff':f('_Cutoff'),'cull':f('_Cull',2),'alphaToMask':f('_AlphaToMask'),'environmentReflections':f('_EnvironmentReflections',1),'specularHighlights':f('_SpecularHighlights',1)}
 cache[key]=m;return m

def load(n,tag='old'):
 root=OLD if tag=='old' else CUR;p=R/audit[n][tag]['path'];report['sources'][rel(p)]=sha(p);report['sources'][rel(Path(str(p)+'.meta'))]=sha(Path(str(p)+'.meta'))
 bpy.ops.object.select_all(action='DESELECT');bpy.ops.import_scene.fbx(filepath=str(p),use_custom_normals=True)
 obs=[o for o in bpy.context.selected_objects if o.type=='MESH'];assert len(obs)==1
 o=obs[0];bpy.context.view_layer.objects.active=o;bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
 for i,mat in enumerate(o.data.materials):o.data.materials[i]=material(root,re.sub(r'\.\d+$','',mat.name))
 
 meta=Path(str(p)+'.meta').read_text();bindings=[]
 for nm,gg in re.findall(r'name: (DK_\w+)\s+second: \{fileID: 2100000, guid: (\w+)',meta):
  mp=guid[root].get(gg);assert mp==root/'Materials'/(nm+'.mat'),(p,nm,mp,'unexpected binding')
  bindings.append({'slotName':nm,'materialGUID':gg,'materialPath':rel(mp)})
 report.setdefault('fbxMaterialBindings',{})[rel(p)]=bindings
 o.hide_render=True;return o

def subset(o,slots,label):
 # Copy intact source faces, retaining their original vertices/UVs/normals; remove only entire material submeshes.
 n=o.copy();n.data=o.data.copy();bpy.context.collection.objects.link(n);n.name=label
 src=o.data;faces=[p for p in src.polygons if p.material_index in slots]
 me=bpy.data.meshes.new(label+'_original_submeshes');me.from_pydata([tuple(v.co) for v in src.vertices],[],[tuple(p.vertices) for p in faces]);me.update()
 for mat in src.materials:me.materials.append(mat)
 for p,q in zip(me.polygons,faces):p.material_index=q.material_index;p.use_smooth=q.use_smooth
 for uv in src.uv_layers:
  dst=me.uv_layers.new(name=uv.name)
  for i,ix in enumerate(ix for p in faces for ix in p.loop_indices):dst.data[i].uv=uv.data[ix].uv
 me.normals_split_custom_set([tuple(src.corner_normals[ix].vector) for p in faces for ix in p.loop_indices]);n.data=me
 return n

def info(o):
 bpy.context.view_layer.update()
 used={i for p in o.data.polygons for i in p.vertices};vs=[o.matrix_world@o.data.vertices[i].co for i in used]
 return {'triangles':sum(len(p.vertices)-2 for p in o.data.polygons),'bounds':[[min(v[i] for v in vs) for i in range(3)],[max(v[i] for v in vs) for i in range(3)]],'slotTriangles':{str(i):sum(len(p.vertices)-2 for p in o.data.polygons if p.material_index==i) for i in range(len(o.data.materials))}}
restylepath=R/'unity/Assets/Suborned/Editor/World/civic-restyle.json';restyle=json.loads(restylepath.read_text());report['sources'][rel(restylepath)]=sha(restylepath)
worldpath=R/'out/world/harbour-district.world.json';world=json.loads(worldpath.read_text());report['sources'][rel(worldpath)]=sha(worldpath)
sets={}
for name,placement,reuse,count,scale in [('PlanterRun_4_B','civic/run-west-cafe/1','FlowerBed',2,.75),('PlanterRun_4_A','civic/run-west-cafe/0','ShrubMound',7,.48),('PottedPalm_Small','civic/cafe-palm-0','PottedPalm_Small',1,1)]:
 if name not in audit:
  p=next((CUR/'Models').rglob('DK_'+name+'.fbx'));audit[name]={'current':{'path':rel(p)}}
 baseline=load(name,'current');source=load(reuse)
 overrides=next(m for m in world['civic'] if m['id']==placement).get('materials',{})
 for i,mat in enumerate(baseline.data.materials):
  key=mat.name.split('/')[-1]
  if key in overrides:baseline.data.materials[i]=material(CUR,overrides[key])
  for rule in restyle['remap']:
   if name in rule['pieces'] and key==rule['from']:baseline.data.materials[i]=material(CUR,Path(rule['to']).stem)
 row={'id':placement,'world':next(m for m in world['civic'] if m['id']==placement),'sourceAsset':rel(OLD),'reuseAsset':reuse,'uniformScale':scale,'instances':count,'sourceSlots':audit[reuse]['old']['meshes'][0]['slots'],'sourceFBX':audit[reuse]['old']['path'],'baseline':info(baseline),'replacementParts':[],'localCoordinates':'Blender metres, +Z up; FBX export basis used by civic evidence: Unity (-x,z,y), then Unity yaw=-world.kit.yaw'}
 parts=[]
 if name.startswith('Planter'):
  hard=[i for i,m in enumerate(baseline.data.materials) if not any(t in m.name.split('/')[-1] for t in ['Leaf_','Bloom_'])];retained=subset(baseline,hard,name+'_retained_container');parts.append(retained);row['retainedCurrentSlots']=hard
  for i in range(count):
   n=source.copy();n.data=source.data;bpy.context.collection.objects.link(n);n.name=reuse+f'_intact_{i}';n.scale=(scale,)*3;n.location=(((-1,1)[i] if count==2 else (i-3)*.52),0,.645);parts.append(n)
 else:
  # Existing cafe pot and trunk stay exactly; only original petiole+fan submeshes are reused at scale 1.
  kept=list(range(7));fol=[7,8];retained=subset(baseline,kept,'existing_palm_pot_trunk');n=subset(source,fol,'old_original_palm_petiole_fan');parts=[retained,n];row['retainedCurrentSlots']=kept;row['reusedSourceSlots']=fol
 for o in parts:
  row['replacementParts'].append({'name':o.name,'location':list(o.location),'scale':list(o.scale),'rotationEuler':list(o.rotation_euler),**info(o)})
  o.hide_render=True
 
 def unityBounds(lohi,k):
  pts=[]
  from itertools import product
  for x,y,z in product(*zip(*lohi)):
   a=k['yaw'];pts.append((k['at'][0]-math.cos(a)*x-math.sin(a)*y,k['base']+z,k['at'][1]-math.sin(a)*x+math.cos(a)*y))
  return [[min(p[i] for p in pts) for i in range(3)],[max(p[i] for p in pts) for i in range(3)]]
 row['baselineWorldBoundsConservative']=unityBounds(row['baseline']['bounds'],row['world']['kit'])
 for part in row['replacementParts']:part['worldBoundsConservative']=unityBounds(part['bounds'],row['world']['kit'])
 row['replacementTriangles']=sum(info(o)['triangles'] for o in parts);row['triangleDelta']=row['replacementTriangles']-row['baseline']['triangles'];report['placements'].append(row);sets[name]={'current': [baseline],'direct-oldtown':parts}
# No extra ground, buildings, planters or collider copied into scene.
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True;scene.world=bpy.data.worlds.new('StudyWorld');scene.world.color=(.35,.35,.35);scene.view_settings.view_transform='AgX'
bpy.ops.object.light_add(type='AREA',location=(-3,-4,9));bpy.context.object.data.energy=1800;bpy.context.object.data.size=7
bpy.ops.object.camera_add();cam=scene.camera=bpy.context.object;cam.data.type='ORTHO'
for name,states in sets.items():
 target=Vector((0,0,.8 if 'Palm' in name else .65))
 for yaw in [30,210]:
  a=math.radians(yaw);pitch=math.radians(38);cam.location=target+Vector((math.sin(a)*math.cos(pitch)*10,-math.cos(a)*math.cos(pitch)*10,math.sin(pitch)*10));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
  for mode in ['close','play']:
   scene.render.resolution_x,scene.render.resolution_y=(1200,800) if mode=='close' else (1920,1080);scene.render.resolution_percentage=100;cam.data.ortho_scale=(5 if 'Planter' in name else 2.7) if mode=='close' else 28*1920/1080
   for tag,objects in states.items():
    for o in objects:o.hide_render=False
    fn=f'{name}-{tag}-{mode}-y{yaw}.png';scene.render.filepath=str(E/fn);bpy.ops.render.render(write_still=True)
    for o in objects:o.hide_render=True
    report['renders'].append({'file':fn,'yaw':yaw,'pitch':38,'horizontalOrthoScale':cam.data.ortho_scale,'pixels':[scene.render.resolution_x,scene.render.resolution_y]})

# Full intact OldTown source gallery, independent of placement fit.
for name in ['ShrubMound','FlowerBed','PottedFern','PottedPalm_Small','PottedTopiary','TopiaryCone','Cypress','FanPalm','CoconutPalm']:
 o=load(name);b=info(o)['bounds'];target=Vector(tuple((b[0][i]+b[1][i])/2 for i in range(3)));cam.location=target+Vector((5,-8,7));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=max(b[1][0]-b[0][0],(b[1][2]-b[0][2])*1.5,b[1][1]-b[0][1])*1.4;scene.render.resolution_x=1000;scene.render.resolution_y=800;o.hide_render=False;fn='source-'+name+'.png';scene.render.filepath=str(E/fn);bpy.ops.render.render(write_still=True);o.hide_render=True;report.setdefault('sourceGallery',[]).append({'asset':name,'file':fn,'source':audit[name]['old']['path']})
report['scriptSha256']=sha(Path(__file__));(E/'manifest.json').write_text(json.dumps(report,indent=2)+'\n');bpy.ops.wm.save_as_mainfile(filepath=str(O/'direct-reuse.blend'));print('DIRECT REUSE COMPLETE: 24 renders, three cafe placements')
