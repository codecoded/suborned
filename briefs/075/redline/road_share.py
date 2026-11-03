# Redline 075: road-enamel share of the ground footprint of each play view (ortho zoom 14 at 38 deg, 16:9; building and car occlusion ignored, so an upper bound). COMPUTED.
# Run: python3 -I briefs/075/redline/road_share.py out/world/harbour-district.world.json
import json, math, sys
import numpy as np
B=json.load(open(sys.argv[1])); g=B['ground']
roads=[(np.array(r['line'],float), r['width']/2) for r in g['roads'] if r.get('surface')=='road-enamel']
circ=[(np.array(c['centre'],float), c['radius'], c['islandRadius']) for c in g['circuses']]
def onroad(P):
    m=np.zeros(len(P),bool)
    for L,h in roads:
        a=L[:-1]; b=L[1:]; ab=b-a; ll=(ab**2).sum(1); ll[ll==0]=1
        for i in range(0,len(a),200):
            A=a[i:i+200]; AB=ab[i:i+200]; LL=ll[i:i+200]
            t=np.clip(((P[:,None,:]-A[None])*AB[None]).sum(2)/LL[None],0,1)
            d=np.linalg.norm(P[:,None,:]-(A[None]+t[...,None]*AB[None]),axis=2).min(1)
            m|=d<=h
    for c,R,r in circ:
        d=np.linalg.norm(P-c,axis=1); m|=(d<=R)&(d>=r)
    return m
FOCI={'office-front':(-160,-281),'cell-open':(-170.5,-227.5),'cafe-square':(-159,-221.5),'heaviest':(-60,-200),'parade':(212,0),'crown-circus':(0,0),'wide-lamplight':(-215,-120)}
print('road-enamel line keys', list(g['roads'][0].keys()))
for n,(fx,fz) in FOCI.items():
    out=[]
    for yaw in (30,120,210,300):
        y=yaw*math.pi/180; right=np.array([math.cos(y),-math.sin(y)]); fwd=np.array([math.sin(y),math.cos(y)])
        hw=14*16/9; hd=14/math.sin(38*math.pi/180)
        u,v=np.meshgrid(np.linspace(-hw,hw,80),np.linspace(-hd,hd,72))
        P=np.array([fx,fz])+u.reshape(-1,1)*right+v.reshape(-1,1)*fwd
        out.append(float(round(onroad(P).mean()*100,1)))
    print(f'{n:15s}', out)
