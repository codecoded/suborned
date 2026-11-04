import json,sys,collections,statistics as st
rows=[json.loads(l) for l in open(sys.argv[1]) if l.startswith('{')]
def first(lg,code,p=lambda e:True):
  for e in lg:
    if e['code']==code and p(e): return e['tick']/10
D=collections.defaultdict(list);C=collections.Counter()
for r in rows:
  lg=r['log']; s=first(lg,'fight.ambush')
  if s is None: continue
  br=first(lg,'rivals.retreat',lambda e:e['tick']/10>=s); ex=first(lg,'car.exploded'); do=first(lg,'rivals.drives-off'); bl=first(lg,'fire.car:rivals',lambda e:e['tick']/10>=s)
  wd=first(lg,'rivals.down',lambda e:'rival-5' in e['text']) 
  C['wheelman down' if wd else 'wheelman up']+=1
  if br: D['spring->break'].append(br-s)
  if ex and br: D['break->explode'].append(ex-br)
  if ex and bl: D['firstcarshot->explode'].append(ex-bl)
  if do and br: D['break->driveoff'].append(do-br)
  if wd and br and wd<=br: C['wheelman down before break']+=1
for k,v in D.items(): v=sorted(v); print(f'{k:24s} n{len(v):4d} med {st.median(v):6.1f} p25 {v[len(v)//4]:6.1f} p75 {v[3*len(v)//4]:6.1f}')
print(C)
