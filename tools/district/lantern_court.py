"""Lantern Court. Metres, local north +Y, origin at the public armillary."""
from math import pi, sin, cos, radians, ceil
from pathlib import Path
from mathutils import Vector, Quaternion
import bpy, json, sys
sys.path.insert(0,str(Path(__file__).resolve().parent))
from ferry import build, use_sheet
from ferry_front import box

out,master=[Path(v).resolve() for v in sys.argv[sys.argv.index('--')+1:]]
plan=json.loads((Path(__file__).resolve().parents[2]/'contracts/world/harbour-district.plan.json').read_text())
court=next(m for m in plan['megastructures'] if m['kind']=='residential courtyard')
cx,cy=court['clock']['at']; TAU=2*pi
STONE, SOFFIT, CELADON, GLASS, SMOKE, BLIND, BRASS, OXBLOOD, LOUNGE, EARTH, SIGN, SOIL, OPAL, STEEL, DIAL, TERRAZZO=range(16)
UP=Vector((0,0,1))
use_sheet(out.parent/'Materials/Play/CourtColour.jpg')


def disc(s,x,y,z,r,h,slot,n=None):
    n=n or (40,20,12)[s.rank]
    bottom=[(x+r*cos(i*TAU/n),y+r*sin(i*TAU/n),z) for i in range(n)]
    top=[(a,b,z+h) for a,b,c in bottom]
    for i in range(n):
        j=(i+1)%n;s.face([bottom[i],bottom[j],top[j],top[i]],slot,smooth=True)
    s.face(top,slot); s.face(bottom[::-1],slot)


def glazing(s,x,y,w,d,r,z0,z1,seed):
    """A continuous ribbon follows the rounded bow. Paired lights share one room behind their sash."""
    outline=s.contour(x,y,w,d,r,(10,6,3)[s.rank])
    for j,(a,b) in enumerate(zip(outline,outline[1:]+outline[:1])):
        origin,end=Vector((*a,0)),Vector((*b,0))
        along=(end-origin).normalized(); inside=Vector((-along.y,along.x,0))
        length=(end-origin).length
        count=max(1,round(length/(2.25,3.35,6.30)[s.rank]))
        spacing=length/count
        for k in range(count):
            left=origin+along*spacing*k
            def v(t,d,h):return left+along*t+inside*d+UP*h
            def pane(lo,hi,depth,low,high,slot,uv=None):
                s.face([v(lo,depth,low),v(hi,depth,low),v(hi,depth,high),v(lo,depth,high)],slot,uv)
            opening=.14 if s.rank<2 else .05
            slot=(SMOKE,GLASS,SMOKE,LOUNGE,BLIND,GLASS)[(j//5+k//2+seed)%6]
            phase=(j+k)%2
            u0,u1=(.02+phase*.48,.49+phase*.48)
            pane(0,spacing,opening,z0,z1,slot,[(u0,0),(u1,0),(u1,1),(u0,1)])
            if s.rank<2:
                m=min(.047,spacing*.06)
                pane(0,m,0,z0,z1,STEEL)
                s.face([v(m,0,z0),v(m,opening,z0),v(m,opening,z1),v(m,0,z1)],STEEL)
                pane(0,spacing,.018,z1-.42,z1-.38,STEEL)
    s.shape(x,y,w+.09,d+.09,r+.045,z0-.045,z0+.025,STEEL)


def balcony(s,x,y,w,d,r,h,project=1):
    # Bracketed limestone slab, inset celadon ceramic nosing, and a thin rolled weathering lip.
    s.tiers(x,y,[(h-.32,w+.2,d+.2,r+.1,SOFFIT),(h-.19,w+2*project-.12,d+2*project-.12,r+project-.06,SOFFIT),
                 (h-.13,w+2*project,d+2*project,r+project,CELADON),(h+.12,w+2*project,d+2*project,r+project,STONE),
                 (h+.17,w+2*project-.08,d+2*project-.08,r+project-.04,STONE)])
    if project>.69:
        # Seamless curved guard rail on the bow. Shared thin rods replace ornamental bars at distance.
        edges=s.contour(x,y,w+2*project-.23,d+2*project-.23,r+project-.115,(8,5,3)[s.rank],(3.9,5.4,12)[s.rank])
        levels=(.59,1.14) if s.rank==0 else (1.14,)
        for k,(a,b) in enumerate(zip(edges,edges[1:]+edges[:1])):
            for dz in levels:
                s.beam((*a,h+dz),(*b,h+dz),.034 if dz<1 else .043,BRASS,4)
            if k%2==0 and s.rank<2:s.beam((*a,h+.15),(*a,h+1.14),.032,STEEL,4)


def entrance(s,x,y,z,heading):
    # Two full-height doors set into a shaded bronze porch. Local right is along the shopfront.
    normal=Vector((cos(heading),sin(heading),0));right=Vector((-sin(heading),cos(heading),0));o=Vector((x,y,z))
    def at(u,d,h):return o+right*u+normal*d+UP*h
    s.plate(x,y,1.47,2.18,2.61,heading,LOUNGE)
    for u in (-1.10,0,1.10):
        s.beam(at(u,.042,.11),at(u,.042,2.82),.036,BRASS,6)
    for h in (.13,2.40,2.84):
        s.beam(at(-1.12,.042,h),at(1.12,.042,h),.038,BRASS,6)
    for u in (-.13,.13):s.beam(at(u,.10,.89),at(u,.10,1.24),.017,BRASS,6)


def wing(s,w):
    x0,x1,y0,y1=w['rect'];x=(x0+x1)/2-cx;y=(y0+y1)/2-cy
    width,depth=x1-x0,y1-y0;radius=w['radius'];floors=w['floors'];key=w['id']
    accent=OXBLOOD if key in ('south','market') else CELADON
    s.shape(x,y,width,depth,radius,.02,.37,SOFFIT)
    glazing(s,x,y,width-.28,depth-.28,radius-.14,.37,3.47,floors)
    # A single long box cannot make a street. Each entrance has a structural pier, shaded threshold and nameable bay.
    ends=s.contour(x,y,width+.08,depth+.08,radius+.04,(3,2,1)[s.rank],6.7)
    for i,p in enumerate(ends):
        if i%3==0:
            s.beam((*p,.19),(*p,3.61),.15,STONE,(9,7,5)[s.rank],.13)
    balcony(s,x,y,width,depth,radius,3.69,1.18 if key in ('bow','north','west') else .63)
    entrance(s,x,y-depth/2-.02,0,-pi/2)
    for floor in range(1,floors):
        z=3.85+(floor-1)*3.05
        inset=.96 if floor==floors-1 and key in ('north','market','south') else 0
        w1,d1,r1=width-2*inset,depth-2*inset,radius-inset
        # Chest-high solid spandrel and a light, continuous window separated by a metal drip.
        s.shape(x,y,w1,d1,r1,z,z+.74,STONE)
        s.shape(x,y,w1+.08,d1+.08,r1+.04,z+.58,z+.69,accent)
        glazing(s,x,y,w1-.23,d1-.23,r1-.115,z+.74,z+2.77,floor+floors)
        balcony(s,x,y,w1,d1,r1,z+2.91,.94 if (floor==1 and key!='east') or key=='bow' else .41)
    z=3.85+(floors-1)*3.05+.12
    s.shape(x,y,width-2.8,depth-2.8,max(1,radius-1.4),z,z+.63,STONE)
    # Set-back roof lantern, the same vocabulary as the ferry rather than another storey on every wing.
    if key in ('south','east','market'):
        rw,rd=width*.50,depth*.48
        s.shape(x,y,rw,rd,min(rw,rd)*.27,z+.42,z+1.11,CELADON)
        s.shape(x,y,rw+.6,rd+.6,min(rw,rd)*.27+.30,z+1.11,z+1.25,STONE)


def bridge(s):
    a,b,c,d=court['bridge']['rect'];x=(a+b)/2-cx;y=(c+d)/2-cy;w=b-a
    base=court['bridge']['base'];depth=d-c;height=court['bridge']['height']
    # The enclosed pedestrian gallery crosses above a real passage to Market Street.
    s.shape(x,y,w,depth,1.6,base,base+.48,STONE)
    glazing(s,x,y,w-.3,depth-.32,1.44,base+.45,base+height-.35,8)
    s.shape(x,y,w+.28,depth+.40,1.75,base+height-.37,base+height,STONE)
    for side in (-1,1):
        yy=y+side*(depth*.5-.12)
        old=None
        for i in range((22,14,8)[s.rank]+1):
            t=i/(22,14,8)[s.rank];p=(x-w/2+t*w,yy,base-3.65+3.62*sin(pi*t)**.63)
            if old:s.beam(old,p,.19,STONE,(9,6,4)[s.rank])
            old=p
    s.plate(x,y-depth/2-.015,base+.79,5.9,.79,-pi/2,SIGN)


def spire(s):
    x,y=court['spire']['at'];x-=cx;y-=cy;z=court['spire']['height']
    # A stepped astronomical clock tower grows out of the residential wing.
    for dx,h,wide in [(-2.0,23.1,2.1),(0,28.2,4.5),(2.1,24.6,2.1)]:
        s.tiers(x+dx,y,[(15.0,wide+1.2,6.9,1.02,STONE),(h-4.6,wide+.25,5.5,.94,STONE),
                         (h-2.5,wide,3.95,.83,STONE),(h,wide*.63,2.4,.68,STONE)])
        if s.rank<2:
            for k in [-.52,0,.52]:
                s.beam((x+dx+k,y-2.06,h-4.0),(x+dx+k*.7,y-1.23,h-.12),.061,BRASS,6)
    s.shape(x,y,1.67,2.0,.77,27.6,28.92,OPAL)
    s.tiers(x,y,[(28.91,1.92,2.06,.92,BRASS),(29.12,1.72,1.85,.78,BRASS),(29.58,.50,.55,.22,STEEL)])
    s.beam((x,y,29.5),(x,y,z),.075,BRASS,10,.021)
    for side in [-1,1]:
        s.plate(x,y+side*2.85,23.83,2.90,2.90,side*pi/2,DIAL,True)


def ring(s,centre,radius,thickness,axis,angle,n):
    rot=Quaternion(Vector(axis),radians(angle));mid=Vector(centre);old=None
    for i in range(n+1):
        a=TAU*i/n;p=mid+rot@Vector((radius*cos(a),radius*sin(a),0))
        if old is not None:s.beam(old,p,thickness,BRASS,(8,6,4)[s.rank])
        old=p


def globe(s):
    radius=court['clock']['radius'];n=(64,32,16)[s.rank]
    disc(s,0,0,.01,radius,.36,SOFFIT);disc(s,0,0,.36,radius+.08,.12,STONE);disc(s,0,0,.48,radius-.21,.055,CELADON)
    disc(s,0,0,.52,1.24,.27,STONE);disc(s,0,0,.79,.87,.35,BRASS)
    centre=Vector((0,0,4.15))
    for a in (0,TAU/3,2*TAU/3):
        p=Vector((cos(a),sin(a),0))
        s.beam(p*.75+UP*.96,p*1.54+UP*2.32,.14,STEEL,9,.095)
        s.beam(p*1.54+UP*2.32,p*2.61+UP*3.46,.095,BRASS,8,.065)
    tilt=Quaternion(Vector((0,1,0)),radians(-23.5));rad=1.71
    def point(u,v):
        a=TAU*u;lat=pi*(v-.5)
        return centre+tilt@Vector((rad*cos(lat)*cos(a),rad*cos(lat)*sin(a),rad*sin(lat)))
    lon,lat=(48,24) if s.rank==0 else (28,14) if s.rank==1 else (16,8)
    for j in range(lat):
        for i in range(lon):
            uv=[(i/lon,j/lat),((i+1)/lon,j/lat),((i+1)/lon,(j+1)/lat),(i/lon,(j+1)/lat)]
            if j==0:uv.pop(0)
            elif j==lat-1:uv.pop(2)
            s.face([point(u,v) for u,v in uv],EARTH,uv,smooth=True)
    for r,thick,axis,angle in [(2.09,.083,(0,1,0),-67),(2.42,.067,(1,.28,0),64),(2.72,.105,(1,.22,0),-18)]:
        ring(s,centre,r,thick,axis,angle,n)
    s.beam(centre+tilt@Vector((0,0,-2.08)),centre+tilt@Vector((0,0,2.29)),.056,BRASS,10)
    # The outer dial has quadrant markings. Limit them to the metres at which the face can be read.
    if s.rank==0:
        for i in range(24):
            a=TAU*i/24
            s.beam((2.72*cos(a),2.72*sin(a),.49),(2.89*cos(a),2.89*sin(a),.49),.017,BRASS,4)
    for bed in court['planting']:
        p,q=bed['at'];p-=cx;q-=cy;r=bed['radius']
        disc(s,p,q,.02,r,.44,STONE);disc(s,p,q,.46,r+.055,.075,SOFFIT);disc(s,p,q,.53,r-.10,.012,SOIL)


groups=[('CourtWest',('west','south')),('CourtCrescent',('north','bow')),('CourtMarket',('east','market'))]
for name,ids in groups:
    def district(s):
        for w in court['wings']:
            if w['id'] in ids:wing(s,w)
        if 'north' in ids:bridge(s);spire(s)
    build(name,district,(95000,43000,15000),out)
build('MeridianClock',globe,(12500,5500,2600),out)
bpy.ops.wm.save_as_mainfile(filepath=str(master))
