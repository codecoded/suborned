"""Independent saved payload / frame verification. Does not launch Unity or mutate prior evidence."""
from pathlib import Path
import json,re,hashlib,subprocess,collections
import numpy as np
D=Path('briefs/075/recovery/tram-corrected');F=D/'checks/final';sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
assets=json.load(open(F/'mesh-assets.json'));paths=sorted({r['asset'] for r in assets['assets']});rows=[]
for asset in paths:
 p=Path('unity')/asset;s=p.read_text();n=int(re.search(r'm_VertexCount: (\d+)',s)[1]);raw=bytes.fromhex(re.search(r'_typelessdata: (.*)',s)[1].strip().removesuffix('y'));fmt=int(re.search(r'm_IndexFormat: (\d+)',s)[1]);ix=np.frombuffer(bytes.fromhex(re.search(r'm_IndexBuffer:(.*)',s)[1].strip().removesuffix('y')),dtype='<u4' if fmt else '<u2').reshape(-1,3)
 row={'asset':str(p),'sha256':sha(p),'vertices':n,'triangles':len(ix),'zeroArea':0,'nonfinite':0,'frameFailures':0,'basisFailures':0,'uvDegenerateTriangles':0,'surfaceArea':0.0}
 if n:
  stride=len(raw)//n
  channels=re.findall(r'- stream: (\d+)\s+offset: (\d+)\s+format: (\d+)\s+dimension: (\d+)',s[s.index('m_Channels:'):s.index('m_DataSize:')])
  def channel(k):
   stream,offset,form,dim=map(int,channels[k]);assert stream==0 and form==0,(asset,k,channels[k]);return np.ndarray((n,dim),dtype='<f4',buffer=raw,offset=offset,strides=(stride,4)).astype('float64')
  v=channel(0);tri=v[ix];a=tri[:,1]-tri[:,0];b=tri[:,2]-tri[:,0];area=np.linalg.norm(np.cross(a,b),axis=1)*.5;row['zeroArea']=int((area==0).sum());row['nonfinite']=int((~np.isfinite(v)).sum());row['surfaceArea']=float(area.sum())
  if int(channels[2][3]):
   normal=channel(1);t=channel(2);uv=channel(4);nl=np.linalg.norm(normal,axis=1);tl=np.linalg.norm(t[:,:3],axis=1);dot=np.sum(normal*t[:,:3],axis=1)
   row['nonfinite']+=int((~np.isfinite(normal)).sum()+(~np.isfinite(t)).sum()+(~np.isfinite(uv)).sum());row['frameFailures']=int(((np.abs(nl-1)>3e-6)|(np.abs(tl-1)>3e-6)|(np.abs(dot)>3e-6)|(np.abs(t[:,3])!=1)).sum());row['maxNormalTangentDot']=float(np.abs(dot).max(initial=0))
   ut=uv[ix];du=ut[:,1]-ut[:,0];dv=ut[:,2]-ut[:,0];det=du[:,0]*dv[:,1]-dv[:,0]*du[:,1];regular=det!=0;row['uvDegenerateTriangles']=int((~regular).sum())
   pu=(a[regular]*dv[regular,1,None]-b[regular]*du[regular,1,None])/det[regular,None];pv=(b[regular]*du[regular,0,None]-a[regular]*dv[regular,0,None])/det[regular,None]
   for corner in range(3):
    j=ix[regular,corner];nn=normal[j]/np.linalg.norm(normal[j],axis=1)[:,None];expected=pu-nn*np.sum(pu*nn,axis=1)[:,None];length=np.linalg.norm(expected,axis=1);good=length>=1e-20;expected[good]/=length[good,None];err=np.linalg.norm(expected-t[j,:3],axis=1);sign=np.where(np.sum(np.cross(nn,t[j,:3])*pv,axis=1)<0,-1,1);row['basisFailures']+=int((good&((err>3e-6)|(sign!=t[j,3]))).sum())
 rows.append(row)
(D/'saved-meshes.json').write_text(json.dumps(rows,indent=2)+'\n')
pins=json.load(open(D/'pins-before.json'));after={p:sha(p) for p in pins};unexpected=[p for p,h in pins.items() if after[p]!=h and not p.startswith('unity/Assets/Suborned/Editor/OldTown/Swap/')]
(D/'pins-after.json').write_text(json.dumps(after,indent=2)+'\n')
surface=json.load(open(D/'saved-audit-surface-proof.json'));coverage=json.load(open(F/'saved-coverage.json'));owner=json.load(open(F/'saved-ownership.json'));attrs=json.load(open(F/'saved-attributes.json'));pres=json.load(open(F/'memory-preservation.json'));audit0=json.load(open(F/'audit-before.json'));audit1=json.load(open(F/'audit-after.json'))
r={'savedDecode':{k:sum(row[k] for row in rows) for k in ['vertices','triangles','zeroArea','nonfinite','frameFailures','basisFailures','uvDegenerateTriangles','surfaceArea']},'meshes':len(rows),'created':assets['created'],'reused':assets['reused'],'protectedInputCount':len(pins),'unexpectedPinDrift':unexpected,'actualAuditSurfaceProof':surface,'savedCoverage':coverage,'savedOwnership':owner,'savedAttributes':attrs,'preservation':pres,'noOpControlDiffs':json.load(open(F/'material-noop-control.json')),'uvDensity':json.load(open(F/'memory-density.json')),'kerbCaps':json.load(open(F/'memory-kerb-caps.json')),'audit':{k:[audit0.get(k),audit1.get(k)] for k in ['schema','samples','onBed','offBed','level','over','groundOverSampleEntries','expectedTramUndersideSampleEntries','unreadable']},'videos':[],'status':'candidate only; HOLD pending NEW independent Redline review; G-3a 59>58 and sight HOLD unchanged'}
for p in sorted((F/'after').glob('*.mp4')):
 stream=json.loads(subprocess.check_output(['ffprobe','-v','error','-select_streams','v:0','-show_entries','stream=width,height,r_frame_rate,nb_frames,duration','-of','json',str(p)]))['streams'][0];r['videos'].append({'path':str(p),**stream})
(D/'verification.json').write_text(json.dumps(r,indent=2)+'\n')
assert surface['samples']==9462 and not surface['misses'] and surface['originalAndExactArcMisses']==0
assert not unexpected,unexpected
assert all(r['savedDecode'][k]==0 for k in ['zeroArea','nonfinite','frameFailures','basisFailures']),r['savedDecode']
assert coverage['bedOnlyMisses']==coverage['railProfileFailures']==coverage['strictBedMisses']==coverage['strictRailMisses']==0,coverage
assert owner['coplanarPositiveAreaPairs']==0,owner
assert attrs['emitted']['frameErrors']==attrs['emitted']['basisOrHandednessErrors']==0,attrs
assert not pres['originalTransformChanges'] and not pres['colliderChanges'] and not pres['sourceMeshChanges'] and not pres['signalBindingChanges'] and not pres['materialPropertyDifferences'] and pres['navIdentical'],pres
assert not r['noOpControlDiffs'] and r['uvDensity']['failuresAgainstDerivedFloatBound']==0
assert audit0['samples']==audit1['samples']==9462 and audit1['offBed']==audit1['groundOverSampleEntries']==audit1['unreadable']==0
print('PASS',json.dumps({'savedDecode':r['savedDecode'],'created':r['created'],'reused':r['reused'],'protected':len(pins),'coverageQueries':coverage['bedQueries']+coverage['railQueries'],'caps':len(r['kerbCaps'])}))
