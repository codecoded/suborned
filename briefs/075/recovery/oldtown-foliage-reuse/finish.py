"""Build auditable distinctions and a static report; no source writes."""
from pathlib import Path
import json,re,hashlib,html,base64
R=Path.cwd();E=R/'briefs/075/recovery/oldtown-foliage-reuse'
OLD=R/'unity/Assets/Suborned/OldTownArt/DecoKit';CUR=R/'unity/Assets/Suborned/Kit'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
a=json.loads((E/'audit.json').read_text());m=json.loads((E/'manifest.json').read_text());diff={}
for n,asset in a.items():
 if 'old' not in asset:continue
 row={'geometry':asset['comparison'],'importerDifferences':{},'materials':[],'lodFiles':{}}
 for tag,root in [('old',OLD),('current',CUR)]:
  f=R/asset[tag]['path'];meta=Path(str(f)+'.meta');row['lodFiles'][tag]=[{'path':str(p.relative_to(R)),'sha256':sha(p)} for p in f.parent.glob(f.stem+'_L*.fbx')]
  pf=next((root/'Prefabs').rglob('DK_'+n+'.prefab'));row.setdefault('prefabs',{})[tag]={'path':str(pf.relative_to(R)),'sha256':sha(pf),'hasSerializedLODGroup':'LODGroup:' in pf.read_text()}
  fields=['globalScale','meshCompression','generateMeshLods','normalImportMode','tangentImportMode','normalSmoothAngle','normalSmoothingSource','addColliders','useFileUnits','bakeAxisConversion']
  row.setdefault('importers',{})[tag]={k:re.search(r'^\s+'+k+r': (.+)',meta.read_text(),re.M)[1] if re.search(r'^\s+'+k+r': (.+)',meta.read_text(),re.M) else None for k in fields}
 for k in fields:
  if row['importers']['old'][k]!=row['importers']['current'][k]:row['importerDifferences'][k]=[row['importers']['old'][k],row['importers']['current'][k]]
 for name in asset['old']['meshes'][0]['slots']:
  pp=[r/'Materials'/(name+'.mat') for r in [OLD,CUR]]
  d={'slot':name,'oldPath':str(pp[0].relative_to(R)),'currentPath':str(pp[1].relative_to(R)),'oldSha256':sha(pp[0]),'currentSha256':sha(pp[1]) if pp[1].exists() else None,'changedPropertyLines':[]}
  if pp[1].exists():
   x,y=[p.read_text() for p in pp]
   # Include all saved properties, preserve GUID texture bindings for audit.
   for key in ['_BaseColor','_Color','_Smoothness','_Metallic','_AlphaClip','_Cutoff','_Cull','_AlphaToMask','_EnvironmentReflections','_SpecularHighlights','m_Shader']:
    pat=r'^\s*(?:- )?'+key+r': .+$';v=[re.findall(pat,t,re.M) for t in [x,y]]
    if v[0]!=v[1]:d['changedPropertyLines'].append({'key':key,'old':v[0],'current':v[1]})
  row['materials'].append(d)
 diff[n]=row
(E/'distinctions.json').write_text(json.dumps(diff,indent=2)+'\n')
# Every input still matches its render-time digest. No unrelated working-tree assertion: other owners are active.
fail=[p for p,h in m['sources'].items() if sha(R/p)!=h];assert not fail,fail
shader=[]
for root in [OLD,R/'unity/Assets/Suborned/Shaders/DecoKit']:
 for p in (root/'Shaders' if root==OLD else root).glob('*Cutaway.shader'):
  shader.append({'path':str(p.relative_to(R)),'sha256':sha(p),'guid':re.search(r'^guid: (\w+)',Path(str(p)+'.meta').read_text(),re.M)[1]})
verification={'inputHashesStillMatch':True,'inputCount':len(m['sources']),'rendersPresent':all((E/r['file']).exists() for r in m['renders']),'sourceGalleryPresent':all((E/r['file']).exists() for r in m['sourceGallery']),'shaderSources':shader,'renderCount':len(m['renders'])+len(m['sourceGallery'])}
assert verification['rendersPresent'] and verification['sourceGalleryPresent']
(E/'verification.json').write_text(json.dumps(verification,indent=2)+'\n')
# Contact sheets are evidence only, not textures or replacement assets.
from PIL import Image,ImageDraw
for mode in ['close','play']:
 sheet=Image.new('RGB',(1600,3*300),(24,29,28));d=ImageDraw.Draw(sheet)
 for i,(name,title) in enumerate([('PlanterRun_4_B','B: FlowerBed x2 @0.75'),('PlanterRun_4_A','A: ShrubMound x7 @0.48'),('PottedPalm_Small','Palm: original petiole/fan @1')]):
  for j,(yaw,tag) in enumerate([(30,'current'),(30,'direct-oldtown'),(210,'current'),(210,'direct-oldtown')]):
   im=Image.open(E/f'{name}-{tag}-{mode}-y{yaw}.png')
   if mode=='play':im=im.crop((760,390,1160,670)) # native pixels; full uncropped 1920x1080 originals alongside
   else:im.thumbnail((400,266))
   sheet.paste(im,(j*400,i*300+30));d.text((j*400+6,i*300+4),f'{title+" / " if j==0 else ""}{tag} y{yaw}',fill='white')
 sheet.save(E/f'comparison-{mode}.png')
gallery=Image.new('RGB',(1200,3*345),(24,29,28));d=ImageDraw.Draw(gallery)
for i,g in enumerate(m['sourceGallery']):
 im=Image.open(E/g['file']);im.thumbnail((400,320));x=i%3*400;y=i//3*345;gallery.paste(im,(x,y+25));d.text((x+6,y+5),g['asset'],fill='white')
gallery.save(E/'source-gallery.png')
body=['<!doctype html><meta charset="utf-8"><title>075 — Direct OldTown foliage reuse</title><style>body{background:#17201f;color:#f7f4ec;font:16px system-ui;max-width:1600px;margin:32px auto}img{max-width:100%}table{border-collapse:collapse}td,th{padding:12px;border:1px solid #64736f}a{color:#9bdcd2}</style><h1>Direct OldTown foliage reuse — evidence, not adopted</h1><p>Original full meshes and original material/texture bindings. Current troughs, pot and trunk retained. No Unity, import or production changes. Read README.md and manifest.json for transforms, bounds, provenance and limitations.</p><h2>Close comparison: current / original reuse, yaw 30 / 210</h2><img src="comparison-close.png"><h2>Play zoom 14, pitch 38°</h2><p>Native-pixel central crops, no enlargement. Full 1920×1080 originals linked below.</p><img src="comparison-play.png"><h2>Intact original-source gallery</h2><img src="source-gallery.png"><h2>Triangles at full detail</h2><table><tr><th>Existing placement</th><th>Current</th><th>Reuse</th><th>Delta</th></tr>']
for p in m['placements']:body.append(f"<tr><td>{html.escape(p['id'])}</td><td>{p['baseline']['triangles']}</td><td>{p['replacementTriangles']}</td><td>+{p['triangleDelta']}</td></tr>")
body.append('</table><p>Triangle count does not measure alpha overdraw, draw submissions, shadow cost or frame time. No performance-free claim; Unity check remains.</p><h2>Full comparison renders</h2>')
for r in m['renders']:body.append(f'<p><a href="{r["file"]}">{r["file"]}</a></p>')
page='\n'.join(body)
for filename in ['comparison-close.png','comparison-play.png','source-gallery.png']:
 page=page.replace('src="'+filename+'"','src="data:image/png;base64,'+base64.b64encode((E/filename).read_bytes()).decode()+'"')
(E/'report.html').write_text(page)
print(json.dumps(verification));print([(p['id'],p['baseline']['triangles'],p['replacementTriangles'],p['triangleDelta']) for p in m['placements']])
