# Motive 059: walk measures for rulings 3-5 (reads sim/data/handover.site.json; writes nothing but stdout)
import json, heapq, math, sys
site = json.load(open(sys.argv[1]))
W = site['grid']; X0, Y0, C, NX, NY = W['x0'], W['y0'], W['cell'], W['nx'], W['ny']
def P(n): return tuple(site['places'][n]['at'])
def cell(p): return (int((p[0]-X0)//C), int((p[1]-Y0)//C))
def cen(i,j): return (X0+(i+.5)*C, Y0+(j+.5)*C)
def free(i,j): return 0<=i<NX and 0<=j<NY and W['rows'][i][j]=='.'
def nearest(p):
    ci,cj=cell(p); best=None
    for i in range(ci-12,ci+13):
        for j in range(cj-12,cj+13):
            if free(i,j):
                q=cen(i,j); d=(q[0]-p[0])**2+(q[1]-p[1])**2
                if best is None or d<best[0]: best=(d,(i,j))
    return best[1]
def astar(a,b):
    a,b=nearest(a),nearest(b); g={a:0}; prev={}; pq=[(0,a)]
    while pq:
        f,u=heapq.heappop(pq)
        if u==b: break
        for di in (-1,0,1):
            for dj in (-1,0,1):
                if di==dj==0: continue
                v=(u[0]+di,u[1]+dj)
                if not free(*v): continue
                if di and dj and (not free(u[0]+di,u[1]) or not free(u[0],u[1]+dj)): continue
                ng=g[u]+(141 if di and dj else 100)
                if ng<g.get(v,1e18): g[v]=ng; prev[v]=u; heapq.heappush(pq,(ng+max(abs(v[0]-b[0]),abs(v[1]-b[1]))*100,v))
    path=[b]
    while path[-1]!=a: path.append(prev[path[-1]])
    return [cen(*q) for q in path[::-1]]
def L(pts): return sum(math.dist(pts[k],pts[k+1]) for k in range(len(pts)-1))/100
YARD=(6935,-1500)
pts={'security-entry':P('security-entry'),'office-entrance':P('d:Office entrance'),'office-approach':P('d:Office approach'),'service-lane':P('d:Service lane'),
     'apron-south-edge':(8600,-6400),'apron-north-edge':(8600,1500),'apron-at-official-bay':(8600,-3100),'rival-park':P('rival-park'),'lookout(94,-27)':(9400,-2700),
     'square-car':P('d:Getaway bay')}
for i in range(1,6): pts[f'ambush-{i}']=P(f'ambush-{i}')
for i in range(1,5): pts[f'police-{i}']=P(f'police-{i}')
print('to the yard stand (69.35,-15.0), walk grid, metres; seconds at police 2.2, rival 1.6, op 1.4 m/s')
for k,p in pts.items():
    m=L(astar(p,YARD)); print(f'  {k:24s} {m:6.1f} m  police {m/2.2:5.1f} s  rival {m/1.6:5.1f} s  op {m/1.4:5.1f} s')
sp=L(astar(P('security-entry'),P('d:Office approach'))); sq=L(astar(P('d:Office approach'),P('d:Service lane')))
print(f'response path: entry->approach {sp:.1f} m, approach->service lane {sq:.1f} m, total {sp+sq:.1f} m = {(sp+sq)/2.2:.1f} s at 2.2 m/s')
print('square stand: security-entry -> police-1..4:', [round(L(astar(P('security-entry'),P(f'police-{i}'))),1) for i in range(1,5)])
print('square: security-entry -> getaway bay', round(L(astar(P('security-entry'),P('d:Getaway bay'))),1))
print('service lane -> yard car', round(math.dist(P('d:Service lane'),YARD)/100,2), 'm straight;', round(L(astar(P('d:Service lane'),YARD)),1),'m walk')
print('drop stop (34,-38) -> yard', round(L(astar((3400,-3800),YARD)),1), 'm')
print('rival-park (84,-31) -> yard', round(L(astar(P('rival-park'),YARD)),1), 'm walk')
print('official entrance -> yard', round(L(astar(P('d:Official entrance'),YARD)),1), 'm;', '-> drive mouth (80,-15)', round(L(astar(P('d:Official entrance'),(8000,-1500))),1),'m')
for st in ['d:Getaway bay','police-1','police-2','police-3','police-4']:
    print('to', st, ': north', round(L(astar(P('security-entry'),P(st))),1), 'official', round(L(astar(P('d:Official entrance'),P(st))),1))
