import json,sys,collections,statistics as st
rows=[json.loads(l) for l in open(sys.argv[1]) if l.startswith('{')]
def first(lg,code,p=lambda e:True):
  for e in lg:
    if e['code']==code and p(e): return e['tick']/10
C=collections.Counter(); D=collections.defaultdict(list)
for r in rows:
  lg=r['log']; td=first(lg,'take.done')
  C[(r['fail'] or r['outcome'])+'/'+str((r.get('cause') or {}).get('why',''))]+=1
  boards=sum(1 for e in lg if e['code']=='board.in'); C['boarded%d'%boards]+=1
  downs=len({e['code'].split('.')[0] for e in lg if e['code'] in ('scout.down','fixer.down','tech.down')}); C['opsdown%d'%downs]+=1
  if td:
    g=first(lg,'withdrawn') or first(lg,'car.leaves',lambda e:e['tick']/10>td) 
    if r['outcome']!='failure' or r['fail']=='withdrawn': D['td->end'].append(r['tick']/10-td)
    for c in ('fight.ambush','car.exploded','rivals.drives-off','car.unboxed'):
      t=first(lg,c)
      if t: D['td->'+c].append(t-td)
for k,v in D.items(): v=sorted(v); print(f'{k:22s} n{len(v):4d} med {st.median(v):6.1f} p25 {v[len(v)//4]:6.1f} p75 {v[3*len(v)//4]:6.1f} p90 {v[9*len(v)//10]:6.1f}')
print(C.most_common())
