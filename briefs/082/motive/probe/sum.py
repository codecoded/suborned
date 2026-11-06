import json,sys,statistics as st
rows={}
for l in open(sys.argv[1]):
    r=json.loads(l)
    if r.get('error'): print('ERR',str(r.get('error'))[:300]); continue
    rows.setdefault(r.get('plan') or r['id'],[]).append(r)
q=lambda v,p: sorted(v)[min(len(v)-1,int(len(v)*p))] if v else 0
for p,R in rows.items():
    w=[r for r in R if r['outcome']=='success']; loud=lambda r:r['gunfire']>0 or r['alert']>=2
    fails={}
    for r in R:
        if r['outcome']!='success':
            k=r['fail'] if r['outcome']=='failure' else r['outcome']; fails[k]=fails.get(k,0)+1
    fs=[r['stats']['fightS'] for r in R if r['stats']['fightS']>0]; wf=[r['stats']['fightS'] for r in w if r['stats']['fightS']>0]
    print(f"{p:26} win {100*len(w)/len(R):5.1f}% loudwins {sum(map(loud,w)):3d} fight all {q(fs,.5)} ({q(fs,.25)}-{q(fs,.75)}, p95 {q(fs,.95)}) wins {q(wf,.5)} | T {q([r['trail'] for r in w],.5)} max {max([r['trail'] for r in w],default=0)} St {q([r['standing'] for r in w],.5)} min {min([r['standing'] for r in w],default=0)} A {q([r['alert'] for r in w],.5)} hp {q([r['stats']['hpLost'] for r in w],.5)} down {sum(r['stats']['down'] for r in w)/max(1,len(w)):.2f} end {q([r['tick'] for r in w],.5)//600}:{q([r['tick'] for r in w],.5)//10%60:02d} {fails}")
