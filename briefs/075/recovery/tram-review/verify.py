"""Read-only review of saved evidence/current inputs; writes only this review directory."""
from pathlib import Path
import json,hashlib,collections,re,numpy as np
R=Path(__file__).resolve().parents[4]; D=R/'briefs/075/recovery/tram-fix'; O=Path(__file__).resolve().parent
load=lambda n:json.loads((D/n).read_text())
hashfile=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
a,b=load('audit-after.json'),load('audit-before.json')
r={'audit':{k:[b.get(k),a.get(k)] for k in ['schema','export','ourTiles','samples','onBed','offBed','level','over','levelNoBed','unreadable','editorReadDespiteRuntimeFlag']},'offBedCoordinatesEqual':a['offBedAt']==b['offBedAt'],'levelSourcesEqual':a['levelBySource']==b['levelBySource']}
r['removedOverSources']=[x for x in b['overBySource'] if x['source'] not in {y['source'] for y in a['overBySource']}]
mb,ma=load('manifest-before.json'),load('manifest-after.json')
r['manifest']={k:mb[k]==ma[k] for k in ['colliders','nav','transforms']}
r['colliderRecords']=len(ma['colliders'])
# Unlike the supplied comparator, preserve duplicate paths in the comparison.
canon=lambda row:json.dumps(row,sort_keys=True)
cb,ca=collections.Counter(map(canon,mb['renderers'])),collections.Counter(map(canon,ma['renderers']))
r['rendererRowsRemoved']=[json.loads(s) for s in (cb-ca).elements()];r['rendererRowsAdded']=[json.loads(s) for s in (ca-cb).elements()]
r['rendererRowCount']=[len(mb['renderers']),len(ma['renderers'])]
r['duplicateRendererPaths']=sum(n-1 for n in collections.Counter(x['path'] for x in mb['renderers']).values() if n>1)
r['inputsBeforeAfterEqual']=load('inputs-before.json')==load('inputs-after.json')
r['currentPins']={}
for n in ['inputs-before.json','render-inputs-before.json']:
 pins=load(n);bad=[p for p,h in pins.items() if not (R/p).is_file() or hashfile(R/p)!=h];r['currentPins'][n]={'count':len(pins),'mismatches':bad}
r['sourceSnapshotComparison']={p.name:hashfile(p)==hashfile(R/'unity/Assets/Suborned/Editor/OldTown/Swap'/p.name) for p in (R/'out/scratch/latch/tram-fix').glob('*.cs') if (R/'unity/Assets/Suborned/Editor/OldTown/Swap'/p.name).exists()}
# Inspect local generated triangle payloads. Unity byte-array YAML appends 'y'.
r['generatedMeshTriangles']=[]
for p in sorted((R/'unity/Assets/Suborned/OldTownArt/Swap/TramFix').glob('*.asset')):
 s=p.read_text();count=int(re.search(r'm_VertexCount: (\d+)',s)[1]); buf=re.search(r'm_IndexBuffer:(.*)',s)[1].strip().removesuffix('y'); vb=bytes.fromhex(re.search(r'_typelessdata: (.*)',s)[1]); indices=np.frombuffer(bytes.fromhex(buf),dtype='<u2').reshape(-1,3)
 if not count:r['generatedMeshTriangles'].append({'asset':p.name,'triangles':0});continue
 stride=len(vb)//count;v=np.ndarray((count,3),dtype='<f4',buffer=vb,strides=(stride,4)).astype('float64');t=v[indices];area2=np.linalg.norm(np.cross(t[:,1]-t[:,0],t[:,2]-t[:,0]),axis=1)
 r['generatedMeshTriangles'].append({'asset':p.name,'vertices':count,'triangles':len(t),'nonfinite':int((~np.isfinite(t)).sum()),'zeroArea':int((area2==0).sum()),'areaBelow1eMinus10':int((area2*.5<1e-10).sum())})
(O/'verification.json').write_text(json.dumps(r,indent=2))
print(json.dumps({k:v for k,v in r.items() if k not in ['rendererRowsAdded','rendererRowsRemoved','removedOverSources']},indent=2))
assert [b['over'],a['over']]==[748,165]
assert all(r['manifest'].values()) and r['inputsBeforeAfterEqual']
assert len(r['rendererRowsRemoved'])==len(r['rendererRowsAdded'])==8
assert not any(x['mismatches'] for x in r['currentPins'].values())
assert b['schema']==a['schema']=='latch.tram-bed-audit/2'
assert b['samples']==a['samples']==9462 and b['level']==a['level']==18 and b['offBed']==a['offBed']==6
assert b['unreadable']==a['unreadable']==0
print('PASS: saved evidence and current pins; no Unity execution')
