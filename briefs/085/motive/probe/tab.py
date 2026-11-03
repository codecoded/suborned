import json,collections as C
R='res/'
V=['off','s15r220','s5r220','s5r360','s0r360']
P=['quiet-a','help-then-press','press-late0','press-late20','press-late40','press-late60','press-late80','press-late100','press-unplaced','press-unplaced-distracted','press-call-first','press-call-first-distracted','thin-quiet','careless']
def load(n):
    d=C.defaultdict(list)
    for l in open(R+n+'.jsonl'):
        r=json.loads(l); d[r['plan']].append(r)
    return d
def q(v,p):
    v=sorted(v); return v[min(len(v)-1,int(len(v)*p))] if v else None
D={v:load(v) for v in V}
def tt(r): return r['trail']+(r.get('campaignTrail') or 0)+(r.get('openCases') or 0)*3
for p in P:
    print('|',p,'|')
    for v in V:
        rs=D[v][p]; w=[r for r in rs if r['outcome']=='success']
        win=[r['marks']['mp.away']-r['marks']['call.waitress'] for r in rs if 'mp.away' in r['marks']]
        mg=[(r['marks']['mp.away']-r['marks']['enter.in'])/10 for r in rs if 'enter.in' in r['marks'] and 'mp.away' in r['marks']]
        c=C.Counter(r['fail'] for r in rs if r['outcome']=='failure')
        print(f"  {v:8} win {100*len(w)/len(rs):5.1f}  A>=2 {100*sum(r['alert']>=2 for r in w)/max(1,len(w)):4.0f}% of wins  T mean {sum(tt(r) for r in w)/max(1,len(w)):5.1f} seen-guard {c['entry-seen-guard']:3} window {q(win,0)}-{q(win,1)} margin min/med {q(mg,0)}/{q(mg,.5)} down/win {sum(r['stats']['down'] for r in w)/max(1,len(w)):.2f}")
