import json,sys,collections,math
def load(f): return json.load(open(f))
def state(p,t,P):
    st=p['stops']
    for i,s in enumerate(st):
        if s[1]<=t<s[2]: return (s[0],'at') if s[3]==1 else None
        if i+1<len(st) and s[2]<=t<st[i+1][1]:
            if s[0]=='beyond' or st[i+1][0]=='beyond': return None
            return ('walking','walk')
    return None
def group(pl):
    if pl=='walking': return 'walking between places'
    if pl=='cafe-entrance': return 'café door (cafe-entrance)'
    if pl.startswith('cafe-table'): return 'café terrace chairs'
    if pl.startswith('cafe-stand'): return 'café counter window (standing points)'
    if pl in('office-entrance',): return 'Office public door'
    if pl=='official-entrance': return 'Office official door'
    if pl=='office-bay': return 'official bay'
    if pl.startswith('t1-records-stand') or pl=='t1-records': return 'tram stop (T1 Records) and its queue'
    if pl=='s1-gardens': return 'sky rail kiosk (S1 Gardens)'
    return pl
def table(f,W=510,step=10):
    d=load(f); P=d['places']; per=collections.defaultdict(lambda: collections.defaultdict(set)); tot=[]
    for t in range(0,W+1,step):
        n=0
        for p in d['people']:
            s=state(p,t,P)
            if s: per[group(s[0])][t].add(p['id']); n+=1
        tot.append(n)
    rows={}
    for g,v in per.items():
        allp=set().union(*v.values()); cnt=[len(v.get(t,())) for t in range(0,W+1,step)]
        rows[g]=(len(allp),max(cnt),sum(cnt)/len(cnt))
    return rows,tot,d
b,tb,db=table(sys.argv[1]); a,ta,da=table(sys.argv[2])
keys=sorted(set(b)|set(a), key=lambda k:-(a.get(k,(0,))[0]+b.get(k,(0,))[0]))
print('| place group | before: people / most at once / mean | after: people / most at once / mean |'); print('|---|---|---|')
f=lambda r: '0 / 0 / 0' if r is None else f'{r[0]} / {r[1]} / {r[2]:.1f}'
for k in keys: print(f'| {k} | {f(b.get(k))} | {f(a.get(k))} |')
print()
def seg(t,lo,hi): return t[lo//10:hi//10+1]
for lab,lo,hi in (('0:00-5:20 (before the chime)',0,320),('5:30-8:30 (the chime on)',330,510)):
    sb,sa=seg(tb,lo,hi),seg(ta,lo,hi)
    print(f'drawn on the street {lab}: before min {min(sb)} / mean {sum(sb)/len(sb):.1f} / max {max(sb)}; after min {min(sa)} / mean {sum(sa)/len(sa):.1f} / max {max(sa)}')
# customers per minute, arrivals/departures, guard-visit window
def cust(d,t):
    n=0
    for p in d['people']:
        s=state(p,t,d['places'])
        if s and s[1]=='at' and (s[0].startswith('cafe-table') or s[0].startswith('cafe-stand')): n+=1
    return n
print('\ncustomers (people at a terrace chair or the counter window), after, per 30 s:')
print(' '.join(f"{t//60}:{t%60:02d}={cust(da,t)}" for t in range(0,511,30)))
print('customers before:',' '.join(f"{t//60}:{t%60:02d}={cust(db,t)}" for t in range(0,511,30)))
for lab,d in (('before',db),('after',da)):
    arr=dep=0
    for p in d['people']:
        for s in p['stops']:
            if s[3]==1 and (s[0].startswith('cafe-table') or s[0].startswith('cafe-stand')):
                if 0<s[1]<=510: arr+=1
                if 0<s[2]<=510: dep+=1
    print(f'{lab}: arrivals 0:00-8:30 {arr}, departures {dep}')
# guard visit window: customers at 10 s steps 1:52..4:30
cs=[cust(da,t) for t in range(112,271,10)]
print('guard-visit span 1:52-4:30 customers (10 s steps), after: min',min(cs),'max',max(cs),'mean %.1f'%(sum(cs)/len(cs)))
cb=[cust(db,t) for t in range(112,271,10)]
print('same span before: min',min(cb),'max',max(cb))
# within 13 m of the door and of beside-inez
door=[ -10.3,-29.0]; ins=[-8.2,-29.0]
def near(d,t,c,r=13):
    n=0
    for p in d['people']:
        s=state(p,t,d['places'])
        if s and s[1]=='at':
            q=[x/100 for x in d['places'][s[0]]]
            if math.dist(q,c)<=r: n+=1
    return n
for lab,d in (('before',db),('after',da)):
    xs=[near(d,t,door) for t in range(112,271,10)]
    print(f'{lab}: people on the street within 13 m of the café door, 1:52-4:30: min {min(xs)} max {max(xs)}; within 3 m of beside-inez: max',max(near(d,t,ins,3) for t in range(0,511,10)))
