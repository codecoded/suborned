import json, math, sys
w=json.load(open(sys.argv[1])); s=json.load(open(sys.argv[2]))
pl=s['places']; P=lambda k: tuple(pl[k].get('stand') or pl[k]['at'])
def blk(a): px,py=a[0],a[1]; return (round((-230-py)*100), round((px+130)*100))
g=s['grid']; print('grid', {k:g[k] for k in g if k not in('cells','blocked','rows','data','solid')})
ps=[(p['id'],p['kind'],blk(p['at']),p.get('edge')) for p in w['ground']['pieces'] if p['kind'] in('manhole','drain')]
def d(a,b): return math.hypot(a[0]-b[0],a[1]-b[1])/100
def segd(c,a,b):
    ax,ay=a; bx,by=b; dx,dy=bx-ax,by-ay; l2=dx*dx+dy*dy; t=0 if l2==0 else max(0,min(1,((c[0]-ax)*dx+(c[1]-ay)*dy)/l2))
    return math.hypot(c[0]-(ax+t*dx),c[1]-(ay+t*dy))/100
stands={'square':('d:Getaway bay',['ambush-%d'%i for i in range(1,6)]+['police-%d'%i for i in range(1,5)]),
        'service-drive':('service-drive',['svc-ambush-%d'%i for i in range(1,6)]+['svc-police-%d'%i for i in range(1,5)])}
walks={'square':[('d:Official entrance','d:Getaway bay'),('d:Service lane','d:Getaway bay')],'service-drive':[('d:Service lane','service-drive')]}
for sid,(car,posts) in stands.items():
    c=P(car); print(f'\n== {sid} car {c}')
    for gid,kind,at,edge in sorted(ps,key=lambda x:d(x[2],c)):
        dc=d(at,c)
        if dc>35: continue
        screens=[p for p in posts if segd(at,c,P(p))<=4]
        wk=[f'{a[2:] if a.startswith("d:") else a}' for a,b in walks[sid] if segd(at,P(a),P(b))<=4]
        print(f'{gid:32s} {kind:7s} {str(at):16s} {dc:5.1f} m from car  edge {edge}  screens car-to {screens}  on walk {wk}')
