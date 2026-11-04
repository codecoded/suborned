import json,sys,collections,statistics as st
rows=[json.loads(l) for l in open(sys.argv[1]) if l.startswith('{')]
def first(lg,code):
  for e in lg:
    if e['code']==code: return e['tick']/10
D=collections.defaultdict(list);C=collections.Counter()
def clk(s): return f'{int(s//60)}:{s%60:04.1f}'
for r in rows:
  lg=r['log']
  for c in ('car.parks','take.done','fight.ambush','rivals.lookout-posted','rivals.lookout-follows','board.in','car.unboxed','rivals.drives-off','car.exploded'):
    t=first(lg,c)
    if t: D[c].append(t)
  C[(r['fail'] or 'win')+' / '+(r.get('cause') or {}).get('why','')]+=1
  td=first(lg,'take.done')
  if td: D['end-take.done'].append(r['tick']/10-td)
for k,v in D.items(): v=sorted(v); print(f'{k:26s} n{len(v):4d} med {clk(st.median(v))} p10 {clk(v[len(v)//10])} p90 {clk(v[9*len(v)//10])}')
print(C.most_common())
