import json,sys,statistics as st,collections as C
def q(v,p):
    v=sorted(v); return v[min(len(v)-1,int(len(v)*p))] if v else None
def clk(t): return '-' if t is None else f'{t//600}:{(t//10)%60:02d}'
def rows(n):
    for l in open(f'/Users/sky/Projects/suborned/out/scratch/motive/085p/res/{n}.jsonl'):
        r=json.loads(l)
        if 'error' in r: print('ERR',r['id'],r['error'][:90]); continue
        yield r
for n in sys.argv[1:]:
    by=C.defaultdict(list)
    for r in rows(n): by[r['plan']].append(r)
    print('##',n)
    for p,rs in sorted(by.items()):
        w=[r for r in rs if r['outcome']=='success']
        f=C.Counter(r['fail'] if r['outcome']=='failure' else r['outcome'] for r in rs if r['outcome']!='success')
        win=[r['marks']['mp.away']-r['marks']['call.waitress'] for r in rs if 'mp.away' in r['marks']]
        marg=[(r['marks']['mp.away']-r['marks']['enter.in'])/10 for r in rs if 'enter.in' in r['marks'] and 'mp.away' in r['marks']]
        rt=[(r['marks']['enter.in']-r['marks']['call.waitress'])/10 for r in rs if 'enter.in' in r['marks'] and 'call.waitress' in r['marks']]
        fight=[r['stats']['fightS'] for r in rs if r['stats']['fightS']>0]
        print(f"{p:28} win {len(w)*100/len(rs):5.1f}% n{len(rs)} T {q([r['trail'] for r in w],.5)} A {q([r['alert'] for r in w],.5)} down/win {sum(r['stats']['down'] for r in w)/max(1,len(w)):.2f} fightmed {q(fight,.5)} p95 {q(fight,.95)} win {win and clk(q(win,.5))} call→enter med {q(rt,.5)} margin min/med/max {q(marg,0)}/{q(marg,.5)}/{q(marg,1)} fails {dict(f)}")
