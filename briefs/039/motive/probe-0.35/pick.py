import json, math
s=json.load(open('sim/data/handover.site.json')); pl=s['places']; g=s['grid']; f=g['floor']
b=json.load(open('contracts/world/civic-block.json'))
def h(x,y):
    ix=(x-g['x0'])//g['cell']; iy=(y-g['y0'])//g['cell']; row=f[ix]; k=0
    for i in range(0,len(row),2):
        k+=row[i+1]
        if iy<k: return row[i]
def inside(p,poly):
    x,y=p; c=False
    for i in range(len(poly)):
        x1,y1=poly[i]; x2,y2=poly[(i+1)%len(poly)]
        if (y1>y)!=(y2>y) and x < (x2-x1)*(y-y1)/(y2-y1)+x1: c=not c
    return c
areas=[[(q[0]*100,q[1]*100) for q in a['polygon']] for a in b['bays']+b['drives']]
def edged(p,a):
    return min(segd(p,a[i],a[(i+1)%len(a)]) for i in range(len(a)))
def ok(p):
    if h(*p)!=0: return False
    if any(inside(p,a) for a in areas): return False
    if any(edged(p,a)<60 for a in areas): return False
    if any(inside(p,o['poly']) or min(math.dist(p,q) for q in o['poly'])<100 for o in s['occluders']): return False
    return True
def segd(c,a,b):
    ax,ay=a; bx,by=b; dx,dy=bx-ax,by-ay; l2=dx*dx+dy*dy; t=0 if l2==0 else max(0,min(1,((c[0]-ax)*dx+(c[1]-ay)*dy)/l2))
    return math.hypot(c[0]-(ax+t*dx),c[1]-(ay+t*dy))
for stand,car,posts in [('square',(850,-4820),['ambush-%d'%i for i in range(1,6)]+['police-%d'%i for i in range(1,5)]),('service-drive',(6935,-1500),['svc-ambush-%d'%i for i in range(1,6)]+['svc-police-%d'%i for i in range(1,5)])]:
    ring=[(car[0]+300*math.cos(a*math.pi/8), car[1]+300*math.sin(a*math.pi/8)) for a in range(16)]
    best=[]
    for dx in range(-800,801,50):
        for dy in range(-800,801,50):
            p=(car[0]+dx,car[1]+dy); d=math.hypot(dx,dy)
            if d<350 or d>600 or not ok(p): continue
            per={q:sum(segd(p,tuple(pl[q]['at']),r)<=400 for r in ring) for q in posts}
            best.append((sum(per.values()),-d,p,per))
    best.sort(key=lambda x:(-x[0],x[1]*-1,x[2]))
    for t,nd,p,per in best[:3]: print(stand,p,f'{-nd/100:.1f} m',f'{t}/{16*len(posts)}',per)
