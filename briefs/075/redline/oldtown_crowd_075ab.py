import re,sys,math,json,collections
scene,guidfile=sys.argv[1],sys.argv[2]
glb={}
for line in open(guidfile):
    s=line.rsplit(" ",1)
    if len(s)<2: continue
    p,g=s[0],s[1].strip()
    if p.endswith('.glb'): glb[g]=p.rsplit('/',1)[1][:-4]
tris={'RFM_CivilianBubbleMicroV1':(21526,26),'RFM_CivilianFamilyWagonV1':(23484,26),'RFM_CivilianNeighborhoodShuttleV1':(23346,26),'RFM_CivilianStreamlineTaxiV1':(23016,26),'RFM_ServiceDeliveryVanV1':(22132,25),'RFM_CityCommuterV1':(11100,53),'RFM_PeopleCommuterV2':(32098,29),'RFM_PeopleCourierV2':(24704,24),'RFM_PeopleDinerOwnerV2':(23378,21),'RFM_PeopleElderV2':(24160,18),'RFM_PeopleMechanicV2':(25522,25),'RFM_PeopleTransitOfficerV2':(25974,23)}
inst=[];transforms={}
cur=None;kind=None;fid=None
def flush():
    if kind=='1001' and cur and cur.get('src') in glb: inst.append(cur)
    if kind=='4' and cur is not None: transforms[fid]=cur
for line in open(scene):
    if line.startswith('--- !u!'):
        flush()
        m=re.match(r'--- !u!(\d+) &(-?\d+)',line); kind,fid=m.group(1),m.group(2); cur={}
        continue
    if kind=='1001':
        m=re.search(r'm_SourcePrefab: \{fileID: -?\d+, guid: ([0-9a-f]+)',line)
        if m: cur['src']=m.group(1)
        m=re.search(r'm_TransformParent: \{fileID: (-?\d+)',line)
        if m: cur['parent']=m.group(1)
        if 'propertyPath' in line: cur.pop('_k',None)
        m=re.search(r'propertyPath: m_LocalPosition\.([xyz])',line)
        if m: cur['_k']=m.group(1)
        m=re.match(r'\s+value: (.*)',line)
        if m and '_k' in cur: cur.setdefault('pos',{})[cur.pop('_k')]=float(m.group(1) or 0)
    elif kind=='4':
        m=re.search(r'm_LocalPosition: \{x: ([-\d.e]+), y: ([-\d.e]+), z: ([-\d.e]+)',line)
        if m: cur['lp']=tuple(map(float,m.groups()))
        m=re.search(r'm_Father: \{fileID: (-?\d+)',line)
        if m: cur['father']=m.group(1)
        m=re.search(r'm_LocalScale: \{x: ([-\d.e]+)',line)
        if m: cur['ls']=float(m.group(1))
flush()
def world(t):
    x=y=z=0;n=0
    while t and t in transforms and n<20:
        tr=transforms[t]; lp=tr.get('lp',(0,0,0)); x+=lp[0];y+=lp[1];z+=lp[2]; t=tr.get('father'); n+=1
    return x,y,z
parents=collections.Counter(i.get('parent') for i in inst)
print('instances',len(inst),'parents',{p:(c,world(p)) for p,c in parents.most_common(6)})
pts=[]
for i in inst:
    px,py,pz=world(i.get('parent'))
    p=i.get('pos',{}); pts.append((glb[i['src']],px+p.get('x',0),pz+p.get('z',0)))
views=[('circus',0,-30),('boulevard-east',14,-140),('gardens-station',-191,0),('harbour-terrace',60,-120),('harbour-station',117,-140),('skyport-station',114,0),('southside',30,-280),('office-front',-160,-281),('cafe-square',-170.5,-227.5),('zone-seam-east',-84,-200)]
W,D=49.8/2,(28/math.sin(math.radians(38)))/2
out={}
for name,fx,fy in views:
    best=None
    for yaw in (30,210):
        a=math.radians(yaw); ux,uz=math.sin(a),math.cos(a); rx,rz=uz,-ux
        cx,cz=fx,fy+206
        people=cars=t=d=0
        for n,x,z in pts:
            dx,dz=x-cx,z-cz
            if abs(dx*rx+dz*rz)<=W and abs(dx*ux+dz*uz)<=D*1.15:
                tt,dd=tris[n]; t+=tt; d+=dd
                if 'Civilian' in n or 'Van' in n: cars+=1
                else: people+=1
        out[f'{name}-y{yaw:03d}']=dict(people=people,cars=cars,tris=t,submeshes=d)
print(json.dumps(out,indent=0))
xs=[p[1] for p in pts]; zs=[p[2]-206 for p in pts]
print('plan extent x',min(xs),max(xs),'y',min(zs),max(zs))
