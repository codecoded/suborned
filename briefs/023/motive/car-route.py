import json, heapq, math, sys
s=json.load(open('/Users/sky/Projects/suborned/sim/data/handover.site.json'))
d=s['drive']; x0,y0,c,nx,ny=d['x0'],d['y0'],d['cell'],d['nx'],d['ny']; rows=d['rows']
free=lambda i,j: 0<=i<nx and 0<=j<ny and rows[i][j]=='.'
def cell(p): return (int((p[0]-x0)//c), int((p[1]-y0)//c))
def cen(i,j): return (x0+(i+.5)*c, y0+(j+.5)*c)
def nearest(p):
  ci,cj=cell(p); best=None
  r=12
  for i in range(ci-r,ci+r+1):
    for j in range(cj-r,cj+r+1):
      if free(i,j):
        q=cen(i,j); dd=(q[0]-p[0])**2+(q[1]-p[1])**2
        if dd<=600**2 and (best is None or dd<best[0]): best=(dd,(i,j))
  return best[1]
def astar(a,b):
  g={a:0}; prev={}; pq=[(0,0,a)]
  while pq:
    f,_,u=heapq.heappop(pq)
    if u==b: break
    for di in (-1,0,1):
      for dj in (-1,0,1):
        if di==dj==0: continue
        v=(u[0]+di,u[1]+dj)
        if not free(*v): continue
        if di and dj and (not free(u[0]+di,u[1]) or not free(u[0],u[1]+dj)): continue
        ng=g[u]+(141 if di and dj else 100)
        if ng<g.get(v,1e18):
          g[v]=ng; prev[v]=u; h=max(abs(v[0]-b[0]),abs(v[1]-b[1]))*100
          heapq.heappush(pq,(ng+h,v[0]*ny+v[1],v))
  path=[b]
  while path[-1]!=a: path.append(prev[path[-1]])
  return path[::-1]
def P(n): return s['places'][n]['at']
pick=P('d:Official drop-off'); bay=P('service-bay')
print('pickup',pick,'bay',bay)
path=astar(nearest(pick),nearest(bay))
pts=[cen(*q) for q in path]
L=sum(math.dist(pts[k],pts[k+1]) for k in range(len(pts)-1))
print('cells',len(path),'grid len m',L/100)
# print path coarse
for k,q in enumerate(pts):
  if k%4==0 or k==len(pts)-1: print(k, round(q[0]/100,2), round(q[1]/100,2))
# min clearance to non-drivable along path
def clear(i,j):
  for r in range(0,20):
    for di in range(-r,r+1):
      for dj in range(-r,r+1):
        if max(abs(di),abs(dj))==r and not free(i+di,j+dj): return r*c/100
  return 99
print('clearance (m) along path:', [clear(*q) for q in path][::3])
blk=P('block-spot'); print('block-spot',blk, 'nearest drivable', cen(*nearest(blk)))
bp=astar(nearest(pick),nearest(blk)); bpts=[cen(*q) for q in bp]
print('block path m', sum(math.dist(bpts[k],bpts[k+1]) for k in range(len(bpts)-1))/100, 'straight', math.dist(bpts[0],bpts[-1])/100)
# straight-line (string-pulled) length estimate of service path: corners
corners=[(78.25,-31.25),(78.25,-27.25),(80.25,-23.25),(80.25,-17.25),(79.25,-16.25),(63.75,-15.25)]
road=sum(math.dist(pts[k],pts[k+1]) for k in range(len(pts)-1) if pts[k][0]>=8025 and pts[k+1][0]>=8025)/100
print('road m', round(road,1), 'bay+drive m', round(L/100-road,1), 'time at 5 / 2 m/s', round(road/5+(L/100-road)/2,1), 's')
