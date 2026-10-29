# Read-only independent seam/crack analysis of the saved joined tram surfaces.
# Maps scene surface objects -> mesh assets, decodes vertex/index buffers, and finds
# union boundary edges plus near-coincident boundary edges (cracks/T-junctions).
import re,sys,json,collections,glob,os
import numpy as np
U='/Users/sky/Projects/suborned/unity/'
scene=U+'Assets/Scenes/probe-recovery075-tramcorrected.unity'
hdr=re.compile(r'^--- !u!(\d+) &(-?\d+)')
docs={};cur=cls=None;buf=[]
with open(scene,errors='replace') as f:
    for line in f:
        m=hdr.match(line)
        if m:
            if cur and cls in('1','4','33'):docs[cur]=(cls,''.join(buf))
            cls,cur=m.group(1),m.group(2);buf=[];continue
        if cls in('1','4','33'):buf.append(line)
if cur and cls in('1','4','33'):docs[cur]=(cls,''.join(buf))
F=lambda t,k:(re.search(r'^\s*'+k+r': (.*)$',t,re.M) or [None,None])[1]
name={i:F(t,'m_Name') for i,(c,t) in docs.items() if c=='1'}
root=[i for i,n in name.items() if n=='Joined tram render surfaces (source topology)'][0]
trs={i:t for i,(c,t) in docs.items() if c=='4'}
go_of={i:re.search(r'm_GameObject: \{fileID: (-?\d+)',t).group(1) for i,t in trs.items() if 'm_GameObject: {fileID' in t}
rt=[i for i,g in go_of.items() if g==root][0]
kids=[i for i,t in trs.items() if re.search(r'm_Father: \{fileID: '+rt+r'\}',t)]
for k in kids: assert 'm_LocalPosition: {x: 0, y: 0, z: 0}' in trs[k] and 'm_LocalRotation: {x: 0, y: 0, z: 0, w: 1}' in trs[k] and 'm_LocalScale: {x: 1, y: 1, z: 1}' in trs[k], trs[k][:400]
assert 'm_LocalPosition: {x: 0, y: 0, z: 0}' in trs[rt]
mf={re.search(r'm_GameObject: \{fileID: (-?\d+)',t).group(1):re.search(r'm_Mesh: \{fileID: (-?\d+), guid: ([0-9a-f]+)',t).group(2) for i,(c,t) in docs.items() if c=='33' and 'm_Mesh: {fileID' in t and 'guid' in t}
guid2path={}
for p in glob.glob(U+'Assets/Suborned/OldTownArt/Swap/Tram*/*.asset.meta'):
    g=re.search(r'guid: ([0-9a-f]+)',open(p).read()).group(1);guid2path[g]=p[:-5]
tiles=[(name[go_of[k]],guid2path[mf[go_of[k]]]) for k in kids]
print('surface objects',len(tiles),'distinct assets',len({p for _,p in tiles}))
def decode(p):
    s=open(p).read();n=int(re.search(r'm_VertexCount: (\d+)',s)[1])
    raw=bytes.fromhex(re.sub(r'[^0-9a-f]','',re.search(r'_typelessdata: (.*)',s)[1]))
    fmt=int(re.search(r'm_IndexFormat: (\d+)',s)[1]);ib=bytes.fromhex(re.sub(r'[^0-9a-f]','',re.search(r'm_IndexBuffer: ?(.*)',s)[1]))
    ix=np.frombuffer(ib,dtype='<u4' if fmt else '<u2').astype(np.int64)
    subs=[(int(a),int(b),int(c)) for a,b,c in re.findall(r'firstByte: (\d+)\s+indexCount: (\d+)\s+topology: \d+\s+baseVertex: (\d+)',s)]
    w=4 if fmt else 2;tris=[]
    for fb,cnt,bv in subs: tris.append(ix[fb//w:fb//w+cnt]+bv)
    stride=len(raw)//n;pos=np.ndarray((n,3),dtype='<f4',buffer=raw,offset=0,strides=(stride,4)).copy()
    nrm=np.ndarray((n,3),dtype='<f4',buffer=raw,offset=12,strides=(stride,4)).copy()
    return pos,nrm,np.concatenate(tris).reshape(-1,3),stride
cache={};allpos=[];alltri=[];off=0;tile_of=[];stats=collections.Counter()
for i,(nm,p) in enumerate(tiles):
    if p not in cache: cache[p]=decode(p)
    pos,nrm,tri,stride=cache[p];allpos.append(pos);alltri.append(tri+off);tile_of.append(np.full(len(tri),i));off+=len(pos)
    stats['vertices']+=len(pos);stats['triangles']+=len(tri)
P=np.concatenate(allpos);T=np.concatenate(alltri);TO=np.concatenate(tile_of)
# weld by exact float bits
key=P.view(np.uint32).reshape(-1,3)
uk,inv=np.unique(key,axis=0,return_inverse=True);inv=inv.reshape(-1)
stats['uniquePositions']=len(uk)
W=inv[T]
# drop exact-degenerate (should be none)
E=np.concatenate([W[:,[0,1]],W[:,[1,2]],W[:,[2,0]]]);E.sort(axis=1)
ET=np.concatenate([TO,TO,TO])
ue,first,ec=np.unique(E,axis=0,return_index=True,return_counts=True)
stats['edges']=len(ue);stats['boundaryEdges']=int((ec==1).sum());stats['nonManifoldEdges']=int((ec>2).sum())
B=ue[ec==1];BT=ET[first[ec==1]];Q=uk.view(np.float32).reshape(-1,3).astype(np.float64)
a=Q[B[:,0]];b=Q[B[:,1]];mid=(a+b)/2
# bottom/down-facing area
tri=Q[W];n=np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0]);ar=np.linalg.norm(n,axis=1)/2;ny=n[:,1]/np.maximum(np.linalg.norm(n,axis=1),1e-300)
stats['areaTotal']=float(ar.sum());stats['areaDownFacing(ny<-0.5)']=float(ar[ny<-.5].sum());stats['areaUp(ny>0.5)']=float(ar[ny>.5].sum())
stats['minY']=float(Q[:,1].min());stats['maxY']=float(Q[:,1].max())
# near-coincident boundary edges: spatial hash midpoints at 2mm; pair partners within 0.5 mm whose segments overlap
cell=np.floor(mid/0.002).astype(np.int64)
from collections import defaultdict
h=defaultdict(list)
for i,c in enumerate(map(tuple,cell)): h[c].append(i)
def segdist(p1,q1,p2,q2):
    # approx: max distance of endpoints of seg2 to line seg1 and vice versa (Hausdorff-ish on short edges)
    def pd(p,a,b):
        ab=b-a;t=np.clip(np.dot(p-a,ab)/max(np.dot(ab,ab),1e-300),0,1);return np.linalg.norm(p-(a+t*ab))
    return min(max(pd(p2,p1,q1),pd(q2,p1,q1)),max(pd(p1,p2,q2),pd(q1,p2,q2)))
pairs=[];maxgap=0;gaps=[]
for i in range(len(B)):
    c=tuple(cell[i])
    for dx in(-1,0,1):
        for dy in(-1,0,1):
            for dz in(-1,0,1):
                for j in h.get((c[0]+dx,c[1]+dy,c[2]+dz),[]):
                    if j<=i:continue
                    d=segdist(a[i],b[i],a[j],b[j])
                    if d<=5e-4:
                        pairs.append((i,j,d));gaps.append(d)
stats['nearCoincidentBoundaryEdgePairs(<=0.5mm)']=len(pairs)
cross=[(i,j,d) for i,j,d in pairs if BT[i]!=BT[j]];same=[(i,j,d) for i,j,d in pairs if BT[i]==BT[j]]
cg=np.array([d for _,_,d in cross]) if cross else np.zeros(0);sg=np.array([d for _,_,d in same]) if same else np.zeros(0)
stats['crossTilePairs']=len(cross);stats['crossTileGapZero']=int((cg==0).sum());stats['crossTileGap>0']=int((cg>0).sum());stats['crossTileGap>1um']=int((cg>1e-6).sum());stats['crossTileGap>10um']=int((cg>1e-5).sum());stats['crossTileGap>100um']=int((cg>1e-4).sum());stats['crossTileGapMax']=float(cg.max()) if len(cg) else 0
stats['sameTilePairs']=len(same);stats['sameTileGap>10um']=int((sg>1e-5).sum())
pt=collections.Counter(int(BT[i]) for i,_,_ in same);vals=np.array(list(pt.values()));stats['sameTilePairsPerTile(min,median,max)']=[int(vals.min()),float(np.median(vals)),int(vals.max())] if len(vals) else []
ctop=[(i,j,d) for i,j,d in cross if max(a[i][1],b[i][1])>=-0.001 and d>0];stats['crossTileGap>0_nearTop']=len(ctop);stats['crossTileGap>10um_nearTop']=sum(1 for x in ctop if x[2]>1e-5)
crossSeamTiles=collections.Counter(tuple(sorted((int(BT[i]),int(BT[j])))) for i,j,d in cross if d>1e-5);stats['tilePairsWithCrack>10um']=len(crossSeamTiles)
pairs=cross
if gaps:
    g=np.array(gaps);stats['pairGapMax_m']=float(g.max());stats['pairGapZero']=int((g==0).sum());stats['pairGap>1um']=int((g>1e-6).sum());stats['pairGap>10um']=int((g>1e-5).sum())
# lengths of boundary edges & whether boundary edges are on top surface (y near max)
L=np.linalg.norm(b-a,axis=1);stats['boundaryEdgeLengthTotal_m']=float(L.sum())
top=(np.maximum(a[:,1],b[:,1])>=-0.001)
stats['boundaryEdgesWithAnEndpointNearTop(y>=-1mm)']=int(top.sum())
ex=[{'a':a[i].tolist(),'b':b[i].tolist(),'pa':a[j].tolist(),'pb':b[j].tolist(),'gap':d} for i,j,d in sorted(pairs,key=lambda x:-x[2])[:12]]
json.dump({'stats':stats,'worstPairs':ex},open('seams.json','w'),indent=1)
print(json.dumps(stats,indent=1))
