"""Isolated compile + geometry checks. No Unity, generators or existing evidence writes."""
import collections, hashlib, json, math, pathlib, subprocess
D=pathlib.Path('briefs/075/recovery/tram-joins')
S=pathlib.Path('out/scratch/latch/tram-joins');S.mkdir(parents=True,exist_ok=True)
sha=lambda p:hashlib.sha256(pathlib.Path(p).read_bytes()).hexdigest()
pins=json.loads((D/'inputs-before.json').read_text())
before={p:sha(p) for p in pins}
# Any concurrent owner edits remain visible in the report, never reset or repinned silently.
drift={p:{'pinned':pins[p],'current':before[p]} for p in pins if before[p]!=pins[p]}
result=subprocess.run(['dotnet','run','--project',str(D/'check.csproj')],capture_output=True,text=True,timeout=120)
(D/'check.log').write_text(result.stdout+result.stderr);(S/'check.log').write_text(result.stdout+result.stderr)
print(result.stdout+result.stderr,end='');assert result.returncode==0
polys=json.loads((D/'footprints.json').read_text())
def inside(q,p):
 return all((b[0]-a[0])*(q[1]-a[1])-(b[1]-a[1])*(q[0]-a[0])>=-1e-8 for a,b in zip(p,p[1:]+p[:1]))
checks=0; failures=[]
# Independent exact-arc sweep from recorded plan geometry, including tangent endpoints.
for cx,cy,a0 in [(-365,255,0),(-315,-385,math.pi)]:
 nearby=[p['polygon'] for p in polys if any(math.hypot(v[0]-cx,v[1]-cy)<29 for v in p['polygon'])]
 for radius in [23.2,26.8]:
  for i in range(1001):
   a=a0+math.pi*.5*i/1000
   for lateral in [-.6,0,.6]:
    r=radius+lateral;q=[cx+r*math.cos(a),cy+r*math.sin(a)];checks+=1
    if not any(inside(q,p) for p in nearby):failures.append(q)
assert not failures,failures[:10]
after={p:sha(p) for p in pins}; assert after==before,'concurrent old-input modification during run'
(D/'inputs-after.json').write_text(json.dumps(after,indent=2)+'\n')
rejected=json.loads((D/'rejected-joins.json').read_text())
summary={'status':'HOLD; helper only; production hookup pending','compiled_actual_helper':True,'dense_exact_arc_probes':checks,'dense_exact_arc_misses':len(failures),'width_m':2,'export_tiles':len(polys),'rejected_export_boundaries':len(rejected),'rejection_kinds':dict(collections.Counter(s.split(': ',1)[1].split(':')[0] for s in rejected)),'pinned_existing_inputs':len(pins),'unchanged_during_run':after==before,'external_owner_drift_since_pin':drift,'note':'Supported runs are split only at explicit rejected joins. Their open caps do NOT certify rejected boundaries. No surface, rail mesh, route-clearance or rendering acceptance.'}
(D/'evidence.json').write_text(json.dumps(summary,indent=2)+'\n')
print(f'PASS independent {checks} exact-arc probes; {len(pins)} old hashes stable during run; external baseline drift: {list(drift)}')
