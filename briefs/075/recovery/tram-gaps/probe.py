"""Read-only six-point geometry probe. Run from repository root with python3.
No Unity, generator, scene mutation, or production writes. Requires numpy.
Float32 sampling follows SwapTramAudit; footprint tests use double precision.
"""
import json, math, hashlib
from pathlib import Path
import numpy as np
D=Path('briefs/075/recovery/tram-gaps')
f=np.float32
w=json.load(open('out/world/harbour-district.world.json'))
audit=json.load(open('briefs/075/recovery/tram-fix/audit-after.json'))
pins=json.load(open('briefs/075/recovery/tram-fix/inputs-after.json'))
files=list(pins)+['briefs/075/recovery/tram-fix/audit-after.json','tools/world/build.ts','tools/plan/geom.ts','tools/world/gen/street/tramstreet.ts','tools/decokit/ground.py','unity/Assets/Suborned/Editor/OldTown/Swap/SwapNetwork.cs','unity/Assets/Suborned/Editor/OldTown/Swap/SwapTramAudit.cs','unity/Assets/Suborned/Editor/OldTown/DecoKit/DecoKitPlacer.cs']
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
before={p:sha(p) for p in files}
assert before['out/world/harbour-district.world.json']==audit['export']
assert all(before[p]==v for p,v in pins.items()), 'pinned input drift'
# Reconstruct exactly the small audit sampling domain, not a generation sweep.
samples=[]
for tr in w['transit']['tram']:
    line=np.array(tr['line'],dtype=np.float32); carry=f(0)
    for i,(a,b) in enumerate(zip(line,line[1:]),1):
        v=b-a; le=f(np.sqrt(f(v[0]*v[0]+v[1]*v[1])))
        if le<1e-4:continue
        d=v/le; n=np.array([-d[1],d[0]],dtype=np.float32); s=carry
        while s<le:
            for o in ([-f(tr['spacing'])*f(.5),f(tr['spacing'])*f(.5)] if tr['spacing'] else [f(0)]):
                for lat in [f(-.6),f(0),f(.6)]:
                    q=a+d*s+n*f(o+lat)
                    samples.append(dict(track=tr['id'],segment=i-1,a=a.tolist(),b=b.tolist(),s=float(s),offset=float(o),lateral=float(lat),q=q.tolist()))
            s=f(s+f(1))
        # Mathf.Repeat(carry-len,1)
        t=f(carry-le);carry=f(t-f(np.floor(t)))
assert len(samples)==audit['samples'],(len(samples),audit['samples'])
rails=[p for p in w['ground']['pieces'] if p['kind']=='tram-rail']
tiles=[]
for p in rails:
    le=p['size'][0];n=max(1,round(le/2));step=le/n
    u=np.array([math.cos(p['yaw']),math.sin(p['yaw'])]);v=np.array([-u[1],u[0]])
    for i in range(n):
        c=np.array(p['at'])+u*(-le/2+step*(i+.5))
        tiles.append(dict(id=p['id']+'#'+str(i),parent=p['id'],c=c,u=u,v=v,step=step))
assert len(tiles)==audit['ourTiles']
centres=np.array([t['c'] for t in tiles]);us=np.array([t['u'] for t in tiles]);vs=np.array([t['v'] for t in tiles]);steps=np.array([t['step'] for t in tiles])
def nearest(q,mode):
    ds=np.array(q)-centres;x=np.sum(ds*us,axis=1);y=np.sum(ds*vs,axis=1)
    hx=steps/2 if mode=='scaled' else np.ones(len(tiles));hy=np.ones(len(tiles))
    excess=np.maximum(np.abs(x)-hx,0);ey=np.maximum(np.abs(y)-hy,0)
    dsq=excess**2+ey**2;k=int(np.argmin(dsq));t=tiles[k]
    return dict(tile=t['id'],parent=t['parent'],centre=t['c'].tolist(),step=t['step'],local=[float(x[k]),float(y[k])],outside_mm=1000*math.sqrt(float(dsq[k])),audit_covered=bool(np.any((np.abs(x)<=hx+.001)&(np.abs(y)<=hy+.001))))
def exp_near(q):
    rr=[]
    for p in rails:
        u=np.array([math.cos(p['yaw']),math.sin(p['yaw'])]);v=np.array([-u[1],u[0]]);d=np.array(q)-p['at'];x=float(d@u);y=float(d@v)
        gap=math.hypot(max(0,abs(x)-p['size'][0]/2),max(0,abs(y)-p['size'][1]/2))
        rr.append((gap,p,x,y))
    gap,p,x,y=min(rr,key=lambda z:z[0]);return dict(id=p['id'],at=p['at'],yaw=p['yaw'],size=p['size'],local=[x,y],outside_mm=gap*1000)
rows=[]
for track,x,y in audit['offBedAt']:
    s=min((s for s in samples if s['track']==track),key=lambda s:math.dist(s['q'],[x,y]))
    assert math.dist(s['q'],[x,y])<.001
    q=s['q'];cur=nearest(q,'fixed');scaled=nearest(q,'scaled');ex=exp_near(q)
    # The relevant exact 25 m fillet from plan, not a fitted spline.
    c=[-365,255] if y>0 else [-315,-385];sgn=-1 if y>0 else 1
    r=25-sgn*s['offset'];rd=math.dist(q,c);theta=math.atan2(q[1]-c[1],q[0]-c[0]);exact=[c[0]+(r-sgn*s['lateral'])*math.cos(theta),c[1]+(r-sgn*s['lateral'])*math.sin(theta)]
    # A second point on true centreline at this angle distinguishes audit-offset effects.
    centre=[c[0]+r*math.cos(theta),c[1]+r*math.sin(theta)]
    rows.append(dict(reported=[x,y],sample=s,current=cur,scale_only=scaled,export=ex,arc=dict(c=c,track_radius=r,radial_distance=rd,ideal_sample_radius=r-sgn*s['lateral'],radial_error_mm=(rd-(r-sgn*s['lateral']))*1000,exact_point=exact,exact_current=nearest(exact,'fixed'),centre_point=centre,centre_current=nearest(centre,'fixed')),classification='real swap tile gap; export continuous at this sample' if ex['outside_mm']<1e-6 else 'real swap tile gap AND export footprint gap at this sample'))
after={p:sha(p) for p in files};assert after==before,'inputs changed during probe'
result=dict(status='HOLD; G-3a BLOCKED; sight patch HOLD',method='float32 audit sampling; analytic oriented rectangles in plan frame (old frame is translation +206); six targets only',hashes=before,pinned_inputs_match=True,inputs_unchanged=True,samples=len(samples),tiles=len(tiles),rows=rows)
(D/'evidence.json').write_text(json.dumps(result,indent=2)+'\n')
for r in rows:
 print(r['reported'],'seg',r['sample']['segment'],'off/lat',r['sample']['offset'],r['sample']['lateral'],'tile',r['current']['tile'],'step',round(r['current']['step'],6),'local',*[round(x,6) for x in r['current']['local']],'gap-mm',round(r['current']['outside_mm'],3),'scaled-mm',round(r['scale_only']['outside_mm'],3),'export-mm',round(r['export']['outside_mm'],3),'arc-error-mm',round(r['arc']['radial_error_mm'],3),'exact-gap-mm',round(r['arc']['exact_current']['outside_mm'],3))
print('PASS: 48 pinned inputs match; all inputs unchanged; samples/tiles',len(samples),len(tiles))
# Proposed geometry only: join offset tangent lines at shared mitres, no widening.
# Select curve intervals by plan circle/radius; no sample or piece-ID exception.
def cross(a,b): return a[0]*b[1]-a[1]*b[0]
def intersect(a,u,b,v):
    return a+u*(cross(b-a,v)/cross(u,v))
def inside(q,poly):
    cs=[cross(np.array(b)-a,np.array(q)-a) for a,b in zip(poly,poly[1:]+poly[:1])]
    return min(cs)>=-1e-7 or max(cs)<=1e-7
proof=[]
for hw in [1.0,.6775]:
    polys=[]; groups=[]
    for circle in [[-365,255],[-315,-385]]:
        for radius in [23.2,26.8]:
            ps=[p for p in rails if abs(math.dist(p['at'],circle)-radius)<.002]
            ps.sort(key=lambda p: math.atan2(p['at'][1]-circle[1],p['at'][0]-circle[0])%(2*math.pi))
            assert len(ps) in [18,21],len(ps)
            pp=[np.array(p['at']) for p in ps];uu=[np.array([math.cos(p['yaw']),math.sin(p['yaw'])]) for p in ps];nn=[np.array([-u[1],u[0]]) for u in uu]
            joints=[]
            for i in range(len(ps)-1):
                joints.append([intersect(pp[i]+sg*hw*nn[i],uu[i],pp[i+1]+sg*hw*nn[i+1],uu[i+1]) for sg in [-1,1]])
            for i,p in enumerate(ps):
                # End caps not changed here; the six interior targets test only joins.
                left=joints[i-1] if i else [pp[i]-uu[i]*p['size'][0]/2+sg*hw*nn[i] for sg in [-1,1]]
                right=joints[i] if i<len(ps)-1 else [pp[i]+uu[i]*p['size'][0]/2+sg*hw*nn[i] for sg in [-1,1]]
                poly=[left[0],right[0],right[1],left[1]]
                polys.append([x.tolist() for x in poly])
                # All points remain inside each original infinite tangent-width band.
                assert all(abs(float((v-pp[i])@nn[i]))<=hw+1e-7 for v in poly)
            delta=math.pi/(2*len(ps));step=radius*delta
            groups.append(dict(circle=circle,radius=radius,n=len(ps),arc_step=step,delta=delta,sagitta_mm=1000*radius*(1-math.cos(delta/2)),required_full_rect_length=2*(radius+hw)*math.tan(delta/2),arc_length_shortfall_mm=1000*(2*(radius+hw)*math.tan(delta/2)-step)))
    covered=[any(inside(r['sample']['q'],poly) for poly in polys) for r in rows]
    exact_covered=[any(inside(r['arc']['exact_point'],poly) for poly in polys) for r in rows]
    assert all(covered) and all(exact_covered)
    proof.append(dict(half_width=hw,groups=groups,target_coverage=covered,exact_arc_coverage=exact_covered,polygons=polys,limits='isolated tangent-line mitre proof only; endpoint-to-straight joins, mesh height/material/UV and scene rendering require owner integration'))
(D/'proposed-join-proof.json').write_text(json.dumps(proof,indent=2)+'\n')
print('PASS: general shared-mitre proof covers 6/6 audit + 6/6 exact-arc targets at both 2 m swap and 1.355 m export widths; width unchanged; no production patch applied')
