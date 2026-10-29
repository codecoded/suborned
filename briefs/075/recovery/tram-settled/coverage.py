import json,math,pathlib
D=pathlib.Path('briefs/075/recovery/tram-settled')
polys=json.loads((D/'source-footprints.json').read_text());grid={}
for p in polys:
 poly=p['polygon']
 for x in range(math.floor(min(v[0] for v in poly)/2),math.floor(max(v[0] for v in poly)/2)+1):
  for y in range(math.floor(min(v[1] for v in poly)/2),math.floor(max(v[1] for v in poly)/2)+1):grid.setdefault((x,y),[]).append(poly)
def contains(q,p):return all((b[0]-a[0])*(q[1]-a[1])-(b[1]-a[1])*(q[0]-a[0])>=-1e-8 for a,b in zip(p,p[1:]+p[:1]))
def covered(q):return any(contains(q,p) for p in grid.get((math.floor(q[0]/2),math.floor(q[1]/2)),[]))
top=json.load(open('briefs/075/recovery/tram-topology/topology.json'));segs={(s['track'],s['segment']):s for tr in top['tracks'] for s in tr['segments']}
misses=[];count=0
for t in top['tiles']:
 s=segs[t['track'],t['segment']];g=s['geometry'];k=0 if g['kind']=='line' else g['sweep']/s['length'];u=s['startTangent'];n=[-u[1],u[0]]
 for f in [0,.25,.5,.75,1]:
  d=t['interval'][0]*(1-f)+t['interval'][1]*f;a=k*d
  p=[s['start'][i]+(u[i]*d if k==0 else u[i]*math.sin(a)/k+n[i]*2*math.sin(a/2)**2/k) for i in range(2)]
  v=[u[i]*math.cos(a)+n[i]*math.sin(a) for i in range(2)];normal=[-v[1],v[0]]
  for lateral in [-.6,0,.6]:
   q=[p[i]+normal[i]*lateral for i in range(2)];count+=1
   if not covered(q):misses.append(q)
e=json.load(open('briefs/075/recovery/tram-gaps/evidence.json'));recorded=[]
for r in e['rows']:
 points=[r['sample']['q'],r['arc']['exact_point'],r['arc']['centre_point']];recorded.append([covered(q) for q in points])
assert not misses,misses[:10];assert all(all(row) for row in recorded)
(D/'coverage.json').write_text(json.dumps({'sourceArcAndLineProbes':count,'misses':len(misses),'originalSixAndExactArcCentre':recorded,'scope':'Full exact source interval footprint sweep at five positions and three laterals, separate from actual mesh proof.'},indent=2)+'\n')
print('PASS exact source interval coverage',count,'probes; original six/exact arc/centre 18/18')
