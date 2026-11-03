import json,re,struct,zlib,math,collections
from pathlib import Path
from PIL import Image
ROOT=Path('/Users/sky/Projects/suborned');OUT=ROOT/'out/scratch/codex/asset-quality-review'; AR=ROOT/'unity/Assets/Suborned'

def fbx(path):
    meshes=[]; arrays=[]
    with path.open('rb') as f:
        if f.read(23)!=b'Kaydara FBX Binary  \x00\x1a\x00':return {'path':str(path.relative_to(ROOT)),'error':'not binary FBX'}
        ver=struct.unpack('<I',f.read(4))[0]; wide=ver>=7500; fmt='<QQQB' if wide else '<IIIB'; hsize=25 if wide else 13
        def node(parent='',mesh=None):
            pos=f.tell();head=f.read(hsize)
            if len(head)<hsize:return False
            end,nprops,plen,nlen=struct.unpack(fmt,head)
            if end==0:return False
            name=f.read(nlen).decode('utf8','replace'); pend=f.tell()+plen;props=[]
            if name=='Geometry':mesh={'name':'','vertices':0,'tris':0,'uvLayers':0,'normalLayers':0,'tangentLayers':0,'badValues':0,'uvQuantized':0,'uvValues':0};meshes.append(mesh)
            for i in range(nprops):
                typ=f.read(1).decode();
                if typ in 'YCFDIL':
                    ff={'Y':'h','C':'?','F':'f','D':'d','I':'i','L':'q'}[typ];v=struct.unpack('<'+ff,f.read(struct.calcsize(ff)))[0];props.append(v)
                elif typ in 'SR':
                    n=struct.unpack('<I',f.read(4))[0];v=f.read(n);props.append(v.decode('utf8','replace') if typ=='S' else None)
                elif typ in 'fdilbc':
                    n,enc,nbytes=struct.unpack('<III',f.read(12));raw=f.read(nbytes)
                    if mesh is not None and name in ['Vertices','PolygonVertexIndex','UV','Normals','Tangents']:
                        raw=zlib.decompress(raw) if enc==1 else raw;ff={'f':'f','d':'d','i':'i','l':'q','b':'?','c':'b'}[typ];vals=struct.unpack('<'+str(n)+ff,raw)
                        if name=='Vertices':mesh['vertices']+=n//3
                        elif name=='PolygonVertexIndex':mesh['tris']+=n-2*sum(v<0 for v in vals)
                        else:
                            key={'UV':'uvLayers','Normals':'normalLayers','Tangents':'tangentLayers'}[name];mesh[key]+=1
                        if name!='PolygonVertexIndex':mesh['badValues']+=sum(not math.isfinite(v) for v in vals)
                        if name=='UV':
                            mesh['uvValues']+=n;mesh['uvQuantized']+=sum(abs(v*1000-round(v*1000))<0.0001 for v in vals if math.isfinite(v))
                    props.append({'length':n})
                else:raise ValueError((path,name,typ,f.tell()))
            f.seek(pend)
            if name=='Geometry' and len(props)>1:mesh['name']=props[1]
            while f.tell()<end-hsize:
                if not node(name,mesh):break
            f.seek(end);return True
        while node():pass
    return {'path':str(path.relative_to(ROOT)),'meshes':meshes,'tris':sum(x['tris'] for x in meshes),'vertices':sum(x['vertices'] for x in meshes),'badValues':sum(x['badValues'] for x in meshes),'missingUV':sum(x['vertices']>0 and x['uvLayers']==0 for x in meshes),'missingNormals':sum(x['vertices']>0 and x['normalLayers']==0 for x in meshes)}

guid={}
for p in AR.rglob('*.meta'):
 t=p.read_text(errors='replace');m=re.search(r'^guid: (\w+)',t,re.M)
 if m:guid[m[1]]=p.with_suffix('')
materials=[];textures=[]
for fam in ['Kit','People']:
 for p in (AR/fam).rglob('*.mat'):
  t=p.read_text();slots=dict(re.findall(r'- (\w+):\n\s+m_Texture: \{fileID: [^,}]+, guid: (\w+)',t));kw=re.search(r'm_ValidKeywords:(.*?)(?:m_InvalidKeywords:)',t,re.S)
  materials.append({'family':fam,'id':p.stem,'path':str(p.relative_to(ROOT)),'slots':{s:str(guid[g].relative_to(ROOT)) if g in guid else g for s,g in slots.items()},'missingTextures':[g for g in slots.values() if g not in guid],'keywords':re.findall(r'- (_\w+)',kw[1]) if kw else [],'floats':{k:float(v) for k,v in re.findall(r'- (_\w+): ([\d.eE+-]+)\n',t)}})
 for p in (AR/fam).rglob('*'):
  if p.suffix.lower() not in ['.jpg','.png']:continue
  with Image.open(p) as im:wh=im.size
  mp=Path(str(p)+'.meta');t=mp.read_text() if mp.exists() else ''
  fields={}
  for key in ['sRGBTexture','textureType','enableMipMap','mipMapsPreserveCoverage','filterMode','aniso','maxTextureSize','streamingMipmaps']:
   m=re.search(r'^\s*'+key+r': ([^\n]*)',t,re.M);fields[key]=m[1] if m else None
  textures.append({'family':fam,'path':str(p.relative_to(ROOT)),'size':wh,**fields})
models=[]
for fam in ['Kit','People']:
 for p in sorted((AR/fam).rglob('*.fbx')):
  try:r=fbx(p)
  except Exception as e:r={'path':str(p.relative_to(ROOT)),'error':repr(e)}
  r['family']=fam;models.append(r)
kit=json.load(open(ROOT/'contracts/world/kit.json'))['assets'];world=json.load(open(ROOT/'out/world/harbour-district.world.json'));placements=collections.Counter()
def walk(o):
 if isinstance(o,dict):
  if isinstance(o.get('kit'),dict):placements[o['kit']['id']]+=1
  if isinstance(o.get('kits'),list):
   for x in o['kits']:placements[x['id']]+=1
  for v in o.values():walk(v)
 elif isinstance(o,list):
  for v in o:walk(v)
walk(world)
md={x['id']:x for x in materials if x['family']=='Kit'}
for x in kit:
 x['exportPlacements']=placements[x['id']];x['texturedMaterials']=sum(bool(md.get(m,{}).get('slots')) for m in x.get('materials',[]));x['normalMaterials']=sum('_BumpMap' in md.get(m,{}).get('slots',{}) or '_DetailNormalMap' in md.get(m,{}).get('slots',{}) for m in x.get('materials',[]))
figures=json.load(open(AR/'People/manifest.json'))['figures'];cell=json.load(open(ROOT/'briefs/020/people/cell-generic/manifest-cell.json'))['figures'];ov={x['id']:x for x in cell}
for f in figures:
 if f['id'] in ov:f['effectiveOverride']=ov[f['id']]
summary={'catalogue':len(kit),'statuses':dict(collections.Counter(x['status'] for x in kit)),'models':dict(collections.Counter(x['family'] for x in models)),'materials':dict(collections.Counter(x['family'] for x in materials)),'textures':dict(collections.Counter(x['family'] for x in textures)),'figures':len(figures),'modelParseErrors':[x for x in models if 'error'in x],'missingUV':[x['path'] for x in models if x.get('missingUV')],'missingNormals':[x['path'] for x in models if x.get('missingNormals')],'nonfiniteModelData':[x['path'] for x in models if x.get('badValues')],'brokenTextureRefs':sum(len(x['missingTextures']) for x in materials),'kitUntextured':sum(not x['slots'] for x in materials if x['family']=='Kit'),'adoptedWithLods':sum(bool(x.get('lods')) for x in kit if x['status']=='adopted'),'worldKitIds':len(placements),'worldKitPlacements':sum(placements.values()),'note':'Placement counts cover the generated world JSON; they do not equal simultaneously visible instances or runtime placements. FBX scan checks stored arrays, not Unity-imported meshes. Import postprocessors override some raw texture metadata.'}
result={'summary':summary,'kit':kit,'models':models,'materials':materials,'textures':textures,'figures':figures}
(OUT/'audit.json').write_text(json.dumps(result,indent=1));print(json.dumps(summary,indent=1));print('quantizedUV meshes',sum(m.get('uvValues',0)>0 and m['uvQuantized']/m['uvValues']>.99 for x in models for m in x.get('meshes',[])))
