"""Read-only independent decode of saved render mesh payloads and settled evidence."""
import json,re,collections,hashlib,math,subprocess
from pathlib import Path
import numpy as np
D=Path('briefs/075/recovery/tram-settled');R=Path.cwd();sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
b=json.load(open(D/'audit-before.json'));a=json.load(open(D/'audit-after.json'))
r={'audit':{k:[b.get(k),a.get(k)] for k in ['schema','export','samples','ourTiles','onBed','offBed','level','over','groundOverSampleEntries','expectedTramUndersideSampleEntries','levelNoBed','unreadable']},'residualLevel':a['levelBySource'],'residualOver':a['overBySource'],'proof':json.load(open(D/'mesh-proof.json')),'preservation':json.load(open(D/'preservation.json'))}
rows=[]
for p in sorted(Path('unity/Assets/Suborned/OldTownArt/Swap/TramSettled').glob('*.asset')):
 s=p.read_text();n=int(re.search(r'm_VertexCount: (\d+)',s)[1]);raw=bytes.fromhex(re.search(r'_typelessdata: (.*)',s)[1].strip().removesuffix('y'));fmt=int(re.search(r'm_IndexFormat: (\d+)',s)[1]);ix=np.frombuffer(bytes.fromhex(re.search(r'm_IndexBuffer:(.*)',s)[1].strip().removesuffix('y')),dtype='<u4' if fmt else '<u2').reshape(-1,3)
 if not n:rows.append({'asset':str(p),'vertices':0,'triangles':0,'zeroArea':0,'nonfinite':0});continue
 stride=len(raw)//n;v=np.ndarray((n,3),dtype='<f4',buffer=raw,strides=(stride,4)).astype('float64');tri=v[ix];cross=np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0]);area=np.linalg.norm(cross,axis=1)*.5
 rows.append({'asset':str(p),'sha256':sha(p),'vertices':n,'triangles':len(ix),'zeroArea':int((area==0).sum()),'nonfinite':int((~np.isfinite(tri)).sum()),'surfaceArea':float(area.sum()),'minimumPositiveArea':float(area[area>0].min()) if (area>0).any() else None})
r['savedMeshDecode']={'meshCount':len(rows),'triangles':sum(x['triangles'] for x in rows),'zeroArea':sum(x['zeroArea'] for x in rows),'nonfinite':sum(x['nonfinite'] for x in rows),'surfaceArea':sum(x.get('surfaceArea',0) for x in rows)}
(D/'saved-meshes.json').write_text(json.dumps(rows,indent=2)+'\n')
pins=json.load(open(D/'pins-before.json'));after={p:sha(p) for p in pins};r['protectedInputCount']=len(pins);r['changedExistingSourcePaths']=[p for p in pins if after[p]!=pins[p] and p.startswith('unity/Assets/Suborned/Editor/OldTown/Swap/')];r['unexpectedPinDrift']=[p for p in pins if after[p]!=pins[p] and not p.startswith('unity/Assets/Suborned/Editor/OldTown/Swap/')]
r['videos']=[]
for p in sorted((D/'after').glob('*.mp4')):
 data=json.loads(subprocess.check_output(['ffprobe','-v','error','-select_streams','v:0','-show_entries','stream=width,height,r_frame_rate,nb_frames,duration','-of','json',str(p)]))['streams'][0];r['videos'].append({'path':str(p),**data})
r['acceptance']='HOLD; measurements only; independent Redline review required; unresolved values are not waived'
(D/'verification.json').write_text(json.dumps(r,indent=2)+'\n');print(json.dumps({k:v for k,v in r.items() if k not in ['residualLevel','residualOver','preservation']},indent=2))
