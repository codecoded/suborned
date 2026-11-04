import json,sys,collections
rows=[json.loads(l) for l in open(sys.argv[1]) if l.startswith('{')]
C=collections.Counter
claim=C(); blastop=0; fails=C(); placed=C(); lastrival=[]; disabled_t=[]; explode_t=[]; breakpre=0; first_drawn=C()
for r in rows:
  fails[r['fail'] or 'win']+=1
  lg=r['log']; codes=[e['code'] for e in lg]
  cl=[e for e in lg if e['code']=='security.gang-claimed']
  if cl: claim[cl[0]['text'].split(': ',1)[1][:50]]+=1
  else: claim['never claimed' if 'security.gang' in codes else 'no gang filed']+=1
  if any(e['code']=='car.blast-hits' and any(o in e['text'] for o in ('scout','tech','fixer','enforcer')) for e in lg): blastop+=1
  d=[e for e in lg if e['code']=='drawn.seen']
  if d: first_drawn[d[0]['text'][:60]]+=1
  for e in lg:
    if e['code']=='car.disabled': disabled_t.append(e['tick']/10); break
print(len(rows),'fails',dict(fails)); print('claim',claim.most_common()); print('blast hits an op',blastop)
print('first drawn.seen',first_drawn.most_common(6))
import statistics
if disabled_t: print('disabled median',statistics.median(disabled_t))
