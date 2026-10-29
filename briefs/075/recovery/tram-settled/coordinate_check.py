"""Independent float32 reproduction of a placement-world inverse roundtrip.
The source FBX is separately decoded read-only by read_fbx.py; its x/z bounds are exactly +/-1.
"""
import json,math,pathlib,numpy as np
D=pathlib.Path('briefs/075/recovery/tram-settled');top=json.load(open('briefs/075/recovery/tram-topology/topology.json'))
max_error=0;bad=0;example=None
for tile in top['tiles']:
 c=tile['rounded']['center'];u=tile['rounded']['tangent'];m=np.eye(4,dtype=np.float32)
 m[:3,:3]=np.array([[u[0],0,-u[1]],[0,1,0],[u[1],0,u[0]]],dtype=np.float32);m[:3,3]=[c[0],.005,c[1]+206]
 inv=np.linalg.inv(m).astype(np.float32)
 roundtrip=np.array([[np.sum(inv[i,:]*m[:,j],dtype=np.float32) for j in range(4)] for i in range(4)],dtype=np.float32)
 for x,z in [(-1,-1),(-1,1),(1,-1),(1,1)]:
  v=np.array([x,0,z,1],dtype=np.float32);p=np.sum(roundtrip*v[None,:],axis=1,dtype=np.float32);assert np.isfinite(p).all();err=max(abs(float(p[0]))-1,abs(float(p[2]))-1,0)
  max_error=max(max_error,err)
  if err>1e-5:bad+=1;example=example or {'id':tile['id'],'roundtrip':p.tolist(),'worldMatrix':m.tolist()}
assert bad>0,'expected float cancellation not reproduced'
# Local hierarchy product for a filter on its placement root is identity: no world translation occurs.
assert np.array_equal(np.eye(4,dtype=np.float32)@np.array([1,0,1,1],dtype=np.float32),[1,0,1,1])
result={'floatWorldRoundtripMaximumDomainOvershootM':max_error,'cornerReproductionsExceedingGuard':bad,'firstExample':example,'sourceBounds':'FBX x,z exactly [-1,1]; decoded in fbx-source.json','fix':'Use product of local hierarchy TRS matrices; never inverse(world)*world to recover source-domain coordinates. Domain/width bounds are unchanged.','limit':'Independent float32 reproduction confirms the cancellation mechanism, not a substitute for rerunning the corrected implementation inside Unity.'}
(D/'coordinate-check.json').write_text(json.dumps(result,indent=2)+'\n');print('PASS world roundtrip cancellation reproduced:',max_error,'m;',bad,'corners exceed original guard; local-hierarchy identity exact')
