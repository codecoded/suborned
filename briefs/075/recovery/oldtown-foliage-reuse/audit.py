import bpy,json,hashlib,re
from pathlib import Path
R=Path.cwd(); E=R/'briefs/075/recovery/oldtown-foliage-reuse'
old=R/'unity/Assets/Suborned/OldTownArt/DecoKit';cur=R/'unity/Assets/Suborned/Kit'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def snap(o):
 vs=[o.matrix_world@v.co for v in o.data.vertices]
 return dict(name=o.name,vertices=len(vs),triangles=sum(len(p.vertices)-2 for p in o.data.polygons),bounds=[[min(v[i] for v in vs) for i in range(3)],[max(v[i] for v in vs) for i in range(3)]],slots=[m.name if m else None for m in o.data.materials],bySlot=[sum(len(p.vertices)-2 for p in o.data.polygons if p.material_index==i) for i in range(len(o.data.materials))],geometryHash=hashlib.sha256(repr(([tuple(v) for v in vs],[(tuple(p.vertices),p.material_index,p.use_smooth) for p in o.data.polygons],[tuple(u.uv) for u in o.data.uv_layers.active.data] if o.data.uv_layers.active else [])).encode()).hexdigest())
report={};raw={}
for n in ['ShrubMound','FlowerBed','PottedFern','PottedPalm_Small','PottedTopiary','TopiaryCone','Cypress','FanPalm','CoconutPalm','BedTuft_A','PlanterRun_4_B']:
 report[n]={}
 for tag,root in [('old',old),('current',cur)]:
  paths=list((root/'Models').rglob('DK_'+n+'.fbx'))
  if not paths:continue
  p=paths[0];bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(p),use_custom_normals=True)
  o=next(o for o in bpy.context.scene.objects if o.type=='MESH');raw[n,tag]={'v':[tuple(o.matrix_world@v.co) for v in o.data.vertices],'polys':[(tuple(p.vertices),p.material_index,p.use_smooth) for p in o.data.polygons],'uv':[tuple(u.uv) for u in o.data.uv_layers.active.data] if o.data.uv_layers.active else [],'normals':[tuple(c.vector) for c in o.data.corner_normals]}
  report[n][tag]=dict(path=str(p.relative_to(R)),sha256=sha(p),metaSha256=sha(Path(str(p)+'.meta')),meshes=[snap(o) for o in bpy.context.scene.objects if o.type=='MESH'])
for n,a in report.items():
 if 'old' not in a or 'current' not in a:continue
 x=raw[n,'old'];y=raw[n,'current'];same=len(x['v'])==len(y['v']);delta=[(i,old,new) for i,(old,new) in enumerate(zip(x['v'],y['v'])) if max(abs(old[k]-new[k]) for k in range(3))>1e-6]
 a['comparison']={'fbxBytesIdentical':a['old']['sha256']==a['current']['sha256'],'polygonsMaterialsSmoothingExact':x['polys']==y['polys'],'uvExact':x['uv']==y['uv'],'cornerNormalsExact':x['normals']==y['normals'],'sameVertexCount':same,'changedVerticesAbove1Micrometre':len(delta),'onlyBelowGroundZClamping':bool(delta) and same and all(abs(v[0]-w[0])<1e-6 and abs(v[1]-w[1])<1e-6 and v[2]<0 and abs(w[2])<1e-6 for _,v,w in delta),'vertexMaxDeltaMetres':max([max(abs(v[k]-w[k]) for k in range(3)) for _,v,w in delta] or [0])}
(E/'audit.json').write_text(json.dumps(report,indent=2))
print('AUDIT COMPLETE')
