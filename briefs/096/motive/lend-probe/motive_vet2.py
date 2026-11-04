import json,sys,collections,statistics as st
rows=[json.loads(l) for l in open(sys.argv[1]) if l.startswith('{')]
def first(lg,code,pred=lambda e:True):
  for e in lg:
    if e['code']==code and pred(e): return e['tick']/10
  return None
D=collections.defaultdict(list); C=collections.Counter()
for r in rows:
  lg=r['log']
  dis=first(lg,'car.disabled'); ret=first(lg,'rivals.retreat'); gone=[e['tick']/10 for e in lg if e['code']=='rivals.gone']
  ds=first(lg,'drawn.seen'); arr=first(lg,'security.arrives') or first(lg,'response.arrives')
  claimed=first(lg,'security.gang-claimed'); called=first(lg,'security.called')
  downs=sum(1 for e in lg if e['code']=='rivals.down'); C['downs%d'%downs]+=1
  C['retreat' if ret else 'noretreat']+=1
  if dis: D['dis'].append(dis)
  if ret and dis: D['ret-dis'].append(ret-dis)
  if gone and ret: D['lastgone-ret'].append(max(gone)-ret)
  if ds and called: D['drawnseen-called'].append(ds-called)
  if claimed and called: D['claim-called'].append(claimed-called)
  C['gone%d'%len(gone)]+=1
for k,v in D.items(): print(k, 'n',len(v),'med',round(st.median(v),1),'p25',round(sorted(v)[len(v)//4],1),'p75',round(sorted(v)[3*len(v)//4],1))
print(C)
codes=collections.Counter(e['code'] for r in rows for e in r['log'] if e['code'].startswith(('security','response','police')))
print(codes.most_common(20))
