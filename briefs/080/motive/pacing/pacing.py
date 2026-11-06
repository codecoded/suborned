import json,sys,re
D='out/simview/dump/'
def clk(t): return f"{t//600}:{(t//10)%60:02d}"
OPS=['scout','fixer','tech','enforcer']
for f in ['route-a','route-b','route-g','route-g-kill','loud','loud-fight']:
    d=json.load(open(D+f+'.json')); end=d['tick']
    ents={e['id']:e for e in d['entities']}
    print(f"\n=== {f} {d['outcome']} end {clk(end)}")
    # agent state share
    for op in OPS:
        st=ents[op]['st']; tot={}
        for i,s in enumerate(st):
            e=st[i+1][0] if i+1<len(st) else end
            e=min(e,end)
            name=re.sub(r'[:(].*','',s[2]).strip().lower()
            tot[name]=tot.get(name,0)+max(0,e-s[0])
        print(' ',op,' '.join(f"{k}={v//10}s" for k,v in sorted(tot.items(),key=lambda x:-x[1])[:7]))
    # all-cell busy timeline: union of cell not idle
    def busy(op,t):
        s=None
        for x in ents[op]['st']:
            if x[0]<=t:s=x
            else:break
        return s[2]
    # timeline of log lines, filtered
    log=[l for l in d['log']]
    codes={}
    for l in log: codes.setdefault(l[1].split('.')[0],0); codes[l[1].split('.')[0]]+=1
    print('  log codes',codes)
    # events: player-visible world things: not order.*, not cell's own
    ev=[(l[0],l[1],l[2]) for l in log if not l[1].startswith('order') and not l[1].startswith('fire.')]
    ts=sorted(set(e[0] for e in ev)|{0,end})
    gaps=sorted(((ts[i+1]-ts[i],ts[i]) for i in range(len(ts)-1)),reverse=True)[:5]
    print('  longest world-event gaps',[f"{g//10}s from {clk(t)}" for g,t in gaps])
    ot=sorted(set(o[0] for o in d['orders'])|{0,end})
    gaps=sorted(((ot[i+1]-ot[i],ot[i]) for i in range(len(ot)-1)),reverse=True)[:5]
    print('  longest order gaps',[f"{g//10}s from {clk(t)}" for g,t in gaps])
    # no one in the cell doing anything (all idle), spans
    ch=sorted(set(x[0] for op in OPS for x in ents[op]['st'])|{0,end}); spans=[]
    for i in range(len(ch)-1):
        a=ch[i];b=ch[i+1]
        if all(busy(op,a).lower().startswith(('idle','in car','waiting')) for op in OPS): spans.append((b-a,a))
    tot=sum(s[0] for s in spans)
    print('  whole cell idle total',tot//10,'s; spans',[f"{g//10}s@{clk(t)}" for g,t in sorted(spans,reverse=True)[:5]])
    print('  orders:',' | '.join(f"{clk(o[0])} {o[1].get('op','')} {o[1].get('do','')} {o[1].get('target',o[1].get('to','')) if not isinstance(o[1].get('to'),list) else ''}" for o in d['orders'] if o[1].get('do') not in ('fire',))[:1500])
    print('  world:',' | '.join(f"{clk(e[0])} {e[1]}" for e in ev)[:1700])
