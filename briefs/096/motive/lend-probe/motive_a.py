import json,sys,collections,statistics as st
rows=[json.loads(l) for l in open(sys.argv[1]) if l.startswith('{')]
def first(lg,code,pred=lambda e:True):
  for e in lg:
    if e['code']==code and pred(e): return e['tick']/10
  return None
D=collections.defaultdict(list);C=collections.Counter()
for r in rows:
  lg=r['log']; s=first(lg,'fight.ambush') or first(lg,'fight.safe'); 
  if s is None: C['nofight']+=1; continue
  ev={}
  for c in ('rivals.retreat','rivals.drives-off','car.exploded','car.burning','car.unboxed','board.in','getaway.go','fight.over','enter.out','take.done','lookout.arrives','car.disabled'):
    t=first(lg,c); 
    if t is not None and t>=s-1: ev[c]=t
  go=[e['tick']/10 for e in lg if e['code'] in ('getaway.leaves','car.leaves','go.leaves','getaway.go')]
  ends=r['tick']/10
  D['spring->end'].append(ends-s)
  for c,t in ev.items(): D['spring->'+c].append(t-s)
  C['unbox:'+('drive-off' if 'rivals.drives-off' in ev else 'explode' if 'car.exploded' in ev else 'none')]+=1
  C[r['fail'] or 'win']+=1
for k,v in sorted(D.items()):
  v=sorted(v); print(f'{k:28s} n{len(v):4d} med {st.median(v):6.1f} p25 {v[len(v)//4]:6.1f} p75 {v[3*len(v)//4]:6.1f}')
print(C)
