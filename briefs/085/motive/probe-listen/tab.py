import json,sys,collections as C
R='/Users/sky/Projects/suborned/out/scratch/motive/085l/res/'
def q(v,p):
    v=sorted(v); return v[min(len(v)-1,int(len(v)*p))] if v else None
for n in sys.argv[1:]:
    by=C.defaultdict(list)
    for l in open(R+n+'.jsonl'):
        r=json.loads(l)
        if 'error' in r: print('ERR',r['error'][:120]); continue
        by[r['plan']].append(r)
    print('##',n)
    for p,rs in sorted(by.items()):
        w=[r for r in rs if r['outcome']=='success']
        f=C.Counter(r['fail'] if r['outcome']=='failure' else r['outcome'] for r in rs if r['outcome']!='success')
        lg=[(r['marks']['scout.listen.heard']-r['given'][k])/10 for r in rs for k in r['given'] if k.startswith('listen') and 'scout.listen.heard' in r['marks']]
        sp=C.Counter(k[3:] for r in rs for k in r['marks'] if k.startswith('mp.'))
        ds=[r['marks'][k]/100 for r in rs for k in r['marks'] if k in('mp.seat','mp.lean')]
        print(f"{p:18} win {100*len(w)/len(rs):5.1f} A>=2 {100*sum(r['alert']>=2 for r in w)/max(1,len(w)):3.0f}% rep {sum('attention.reporting' in r['marks'] for r in rs):3} order→heard med {q(lg,.5)} p95 {q(lg,.95)} max {q(lg,1)} spots {dict(sp)} dist med {q(ds,.5)} max {q(ds,1)} refused-med {q([r['refused'] for r in rs],.5)} fails {dict(f.most_common(4))}")
