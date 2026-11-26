"""Harbour trades. Local X runs along the counter, -Y is its customer side, Z is up, in metres."""
import sys
from math import cos, pi, sin
from pathlib import Path

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from ferry import TAU, build, use_sheet
from ferry_front import box

IVORY, TEAL, TIMBER, CANE, BRASS, SILVER, LEATHER, CANVAS, CITRUS, PLUM, SIGNS, DIAL = range(12)


def turned(s, x, y, profile):
    """Section of a spun/enamelled part. Each station gives height, radius and finish."""
    sides = (32, 18, 10)[s.rank]
    last = None
    for z, radius, finish in profile:
        ring = [(x+radius*cos(i*TAU/sides), y+radius*sin(i*TAU/sides), z) for i in range(sides)]
        if last is None:
            s.face(ring[::-1], finish)
        else:
            for i, a in enumerate(last):
                j = (i+1) % sides
                s.face([a,last[j],ring[j],ring[i]], finish, smooth=True)
        last = ring
    s.face(last, finish)


def wheel(s, x, y, h, radius, spokes=True):
    """A real opening between rim and hub makes the barrow readable from across the road."""
    count = (24, 16, 10)[s.rank]
    if not spokes:
        s.beam((x,y-.048,h),(x,y+.048,h),radius,TEAL,count)
    else:
        rings = []
        for r,offset in [(radius-.036,-.033),(radius,-.027),(radius,.027),(radius-.036,.033)]:
            rings.append([(x+r*cos(TAU*i/count),y+offset,h+r*sin(TAU*i/count)) for i in range(count)])
        for k in range(4):
            for i in range(count):
                j=(i+1)%count
                s.face([rings[k][i],rings[k][j],rings[(k+1)%4][j],rings[(k+1)%4][i]], TEAL if k==1 else SILVER, smooth=True)
        for i in range((8,6,4)[s.rank]):
            a = TAU*i/(8,6,4)[s.rank]
            s.beam((x,y,h),(x+(radius-.03)*cos(a),y,h+(radius-.03)*sin(a)),.014,SILVER,4)
    s.beam((x,y-.060,h),(x,y+.060,h),.060,SILVER,(12,8,6)[s.rank])


def label(s, x,y,z, width,height, row, heading=-pi/2):
    # Each enamel nameplate occupies a separate band of the same printed atlas cell.
    s.plate(x,y,z,width,height,heading,SIGNS,section=(0,(3-row)/4,1,(4-row)/4))


def bouquet(s, x,y,z, width,height, variety, phase):
    # Fold and lean the cut stems. A sloped pair of leaves gives the command camera a crown to see.
    for i in range((3,2,2)[s.rank]):
        a=phase+i*pi/(3,2,2)[s.rank]
        right=Vector((cos(a),sin(a),0)); away=Vector((-sin(a),cos(a),0)); up=Vector((0,0,1))
        base=Vector((x,y,z))
        crown=base+up*height+away*height*(.28 if i%2==0 else -.38)
        foot=base-up*.018
        root=[foot-right*width*.45,foot,foot+right*width*.45]
        tip=[crown-right*width*.52,crown+away*width*.19+up*.04,crown+right*width*.52]
        for k in (0,1):
            quad=[root[k],root[k+1],tip[k+1],tip[k]]
            uv=[(k/2,0),((k+1)/2,0),((k+1)/2,1),(k/2,1)]
            s.face(quad,12+variety,uv)
            s.face(quad[::-1],12+variety,uv[::-1])


def flower_barrow(s):
    s.count=(5,3,1)[s.rank]
    for y in (-.43,.43):
        wheel(s,-.29,y,.36,.36)
        s.beam((-.75,y*.72,.59),(.97,y*.77,.97),.040,TIMBER,8)
        s.beam((.48,y*.75,.58),(.64,y*.84,.035),.029,TEAL,6)
        s.beam((.64,y*.84,.035),(.82,y*.84,.035),.027,SILVER,6)
    s.tiers(-.10,0,[(.52,1.46,.70,.15,TEAL),(.59,1.62,.80,.15,TEAL),
                       (.82,1.71,.84,.16,CANE),(.855,1.73,.86,.16,TIMBER)])
    if s.rank<2:
        for y in (-.42,.42):
            for x in (-.77,-.47,.26,.57):
                s.beam((x,y,.61),(x,y,.84),.016,TIMBER,4)
    label(s,-.07,-.430,.716,.67,.18,0)
    label(s,-.07,.430,.716,.67,.18,0,pi/2)
    for i,(x,y,h,variety) in enumerate([(-.58,.19,.63,1),(-.11,.18,.67,2),(.37,.16,.51,0),
                                          (-.55,-.18,.43,3),(-.07,-.20,.52,0),(.41,-.17,.48,3)]):
        s.beam((x,y,.82),(x,y,.99),.093,SILVER,(14,9,6)[s.rank],.126)
        if s.rank<2:
            s.beam((x,y,.971),(x,y,1.0),.135,IVORY,12)
        bouquet(s,x,y,.97,.58,h,variety,i*2.17+.2)


def fruit(s, x,y,z, radius, slot, tint):
    sides=(8,6,5)[s.rank]
    levels = [(-.84,.47),(-.37,.94),(.35,1),(.85,.59)] if s.rank==0 else [(-.73,.57),(.08,1),(.76,.61)]
    rings=[]
    for h,r in levels:
        rings.append([(x+radius*r*cos(TAU*i/sides),y+radius*r*sin(TAU*i/sides),z+radius*h) for i in range(sides)])
    # Each swatch contains four harvest colours. Keep contiguous patches across the individual fruit.
    u0,v0=(tint%2)*.50+.07,(tint//2)*.50+.07
    s.face(rings[0][::-1],slot,[(u0+.17,v0+.17)]*sides)
    for lower,upper in zip(rings,rings[1:]):
        for i in range(sides):
            j=(i+1)%sides
            u=u0+.32*i/sides; uu=u0+.32*(i+1)/sides
            s.face([lower[i],lower[j],upper[j],upper[i]],slot,[(u,v0),(uu,v0),(uu,v0+.35),(u,v0+.35)],smooth=True)
    s.face(rings[-1],slot,[(u0+.17,v0+.17)]*sides)
    if not s.rank:
        s.face([(x-.010,y,z+radius*.87),(x+.038,y-.009,z+radius*.93),(x+.034,y+.018,z+radius*.89)],TEAL)


def tray(s,x,y,h,slot,index):
    s.shape(x,y,.76,.46,.055,h,h+.065,TIMBER)
    if s.rank==2:
        # At a few dozen screen pixels a fruit mound and its slatted front carry the colour bands.
        s.shape(x,y-.23,.78,.035,.012,h+.05,h+.17,CANE)
        s.tiers(x,y,[(h+.075,.69,.40,.08,slot),(h+.18,.64,.36,.12,slot),(h+.235,.47,.23,.09,slot)])
        return
    for dy in (-.23,.23):
        s.shape(x,y+dy,.78,.036,.012,h+.07,h+.17,CANE)
    for dx in (-.38,.38):
        s.shape(x+dx,y,.038,.47,.011,h+.06,h+.20,TIMBER)
    nx,ny=((5,3),(4,2),(3,2))[s.rank]
    for row in range(ny):
        for col in range(nx):
            xx=x+(col/(nx-1)-.5)*.58 + .027*sin(row*4+index+col)
            yy=y+(row/(ny-1)-.5)*.28 + .021*cos(index*9+col*3)
            r=(.083,.098,.116)[s.rank]*(1+.09*cos(col*7+row*4))
            fruit(s,xx,yy,h+.15+.022*cos(col*8+row*2+index),r,slot,(index+col%2)%4)
    if s.rank==0:
        for i in range(3):
            fruit(s,x+(i-1)*.18,y+.047*sin(i+index),h+.248,.077,slot,(index+i%2)%4)


def packing_crate(s,x,y,z,w,d,h,stock):
    if s.rank==2:
        s.shape(x,y,w,d,.025,z,z+h,TIMBER)
        return
    def slat(cx,cy,width,depth,lo,hi,finish):
        box(s,cx-width/2,cx+width/2,cy-depth/2,cy+depth/2,lo,hi,finish)
    s.shape(x,y,w-.03,d-.03,.022,z+.012,z+.048,TIMBER)
    for dx in (-w/2+.028,w/2-.028):
        for dy in (-d/2+.028,d/2-.028):
            slat(x+dx,y+dy,.049,.049,z,z+h,CANE)
    rows=3 if s.rank==0 else 2
    for layer in range(rows):
        level=z+.024+layer*(h-.032)/rows
        for dy in (-d/2,d/2):
            slat(x,y+dy,w,.027,level,level+(h-.052)/rows,TIMBER)
        for dx in (-w/2,w/2):
            slat(x+dx,y,.028,d,level,level+(h-.052)/rows,CANE)
    if stock:
        for row in (0,1):
            for col in range(3):
                fruit(s,x+(col-1)*w*.24,y+(row-.5)*d*.51,z+h-.005,.083,CITRUS,(col+row)%4)


def shade(s):
    # A curved, pitched sail over an open counter. Screen-printed stripes continue across its rolled nose.
    xs=(-1.4,1.4)
    section=[(-.92,2.18),(-.85,2.30),(-.61,2.38),(-.18,2.52),(.24,2.59),(.67,2.61),(.73,2.58)]
    for i,((y,z),(yy,zz)) in enumerate(zip(section,section[1:])):
        q=[(xs[0],y,z),(xs[1],y,z),(xs[1],yy,zz),(xs[0],yy,zz)]
        uv=[(0,i/6),(1,i/6),(1,(i+1)/6),(0,(i+1)/6)]
        s.face(q,CANVAS,uv)
        s.face([(x,n,h-.022) for x,n,h in q[::-1]],CANVAS,uv[::-1])
    for i in range(16):
        x=-1.4+2.8*i/16
        xx=x+2.8/16
        mid=(x+xx)/2
        s.face([(x,-.922,2.19),(x,-.922,2.085),(mid,-.925,2.035),(xx,-.922,2.085),(xx,-.922,2.19)],
               CANVAS,[(i/16,.9),(i/16,.1),((i+.5)/16,0),((i+1)/16,.1),((i+1)/16,.9)])
    for x in (-1.27,1.27):
        s.beam((x,.57,.08),(x,.57,2.6),.037,TEAL,8)
        s.beam((x,.57,1.89),(x,-.87,2.20),.027,SILVER,6)
    s.beam((-1.39,-.91,2.18),(1.39,-.91,2.18),.022,SILVER,6)
    for x in (-.70,.70):
        s.beam((x,.32,2.54),(x,.32,2.35),.014,SILVER,6)
        s.beam((x,.32,2.28),(x,.32,2.35),.11,TEAL,12,.043)
        rim=[(x+.096*cos(i*TAU/12),.32+.096*sin(i*TAU/12),2.275) for i in range(12)]
        s.face(rim[::-1],DIAL,[(.018,.982)]*12)


def produce_stall(s):
    s.count=(4,2,1)[s.rank]
    for x in (-1.12,1.12):
        wheel(s,x,.42,.16,.16,False)
        s.beam((x,-.46,.028),(x,-.46,.79),.052,TEAL,8)
    s.tiers(0,0,[(.17,2.38,.98,.17,TEAL),(.28,2.52,1.10,.19,TEAL),
                   (.73,2.54,1.13,.19,IVORY),(.78,2.62,1.18,.18,TIMBER)])
    label(s,0,-.570,.488,1.60,.32,1)
    s.shape(0,.28,2.47,.48,.082,.77,.962,TIMBER)
    for row in (0,1):
        for col in range(3):
            x=(col-1)*.83; y=-.35+row*.56; h=.76+row*.20
            tray(s,x,y,h,(CITRUS,PLUM,CITRUS,PLUM,CITRUS,PLUM)[row*3+col],row*3+col)
    # The lower stock belongs on the paving. Crates sit partly under the raked display, within its clearance.
    packing_crate(s,-.98,-.81,.009,.60,.43,.26,False)
    packing_crate(s,-.95,-.80,.272,.57,.40,.23,True)
    packing_crate(s,1.38,.01,.009,.42,.50,.29,True)
    # Mechanical market scale at the counter end. Its ivory face sits above the stock.
    s.shape(.99,.48,.34,.29,.065,1.06,1.13,TEAL)
    s.beam((.99,.48,1.11),(.99,.48,1.44),.048,TEAL,10)
    s.beam((.99,.38,1.38),(.99,.55,1.38),.156,IVORY,24)
    s.plate(.99,.374,1.38,.29,.29,-pi/2,DIAL,True)
    s.beam((.99,.48,1.49),(.99,.48,1.57),.073,SILVER,10,.22)
    shade(s)


def pneumatic_post(s):
    s.count=(5,3,1)[s.rank]
    turned(s,0,0,[(.009,.37,TEAL),(.06,.44,TEAL),(.135,.44,IVORY),(.19,.36,IVORY),
        (.30,.29,TEAL),(1.00,.31,IVORY),(1.055,.333,SILVER),(1.32,.333,TEAL),
        (1.345,.315,SILVER),(1.63,.30,IVORY),(1.71,.33,IVORY),
        (1.75,.337,SILVER),(1.80,.26,IVORY),(1.84,.12,IVORY)])
    # The pressure chamber is offset. Its curved return and cradled transfer capsules distinguish it from a street bin.
    s.beam((.31,0,.32),(.43,0,.48),.071,SILVER,12)
    s.beam((.43,0,.47),(.43,0,1.38),.069,SILVER,12)
    s.beam((.43,0,1.38),(.29,0,1.51),.070,SILVER,12)
    s.beam((0,-.21,1.49),(0,-.321,1.49),.127,SILVER,20)
    s.plate(0,-.325,1.49,.221,.221,-pi/2,DIAL,True)
    s.beam((0,-.27,1.135),(0,-.358,1.135),.194,SILVER,24)
    s.beam((0,-.355,1.135),(0,-.37,1.135),.156,TEAL,20)
    s.beam((0,-.37,1.135),(0,-.392,1.135),.11,LEATHER,16)
    for dx in (-.075,.075):
        s.beam((dx,-.387,1.135),(dx,-.427,1.14),.018,SILVER,6)
    s.beam((-.08,-.427,1.14),(.08,-.427,1.14),.024,IVORY,8)
    s.shape(0,-.296,.42,.043,.017,.755,.897,TEAL)
    label(s,0,-.32,.826,.408,.13,2)
    for x in (-.185,.185):
        s.beam((x,-.22,.39),(x,-.296,.58),.049,LEATHER,10)
        s.beam((x,-.288,.559),(x,-.302,.596),.051,SILVER,10)
    # A parcel is sent on one side and collected on the other. The street sees the service from either approach.
    s.shape(0,.278,.43,.082,.03,.79,1.19,TEAL)
    s.shape(0,.317,.35,.047,.021,.85,1.05,SILVER)
    s.shape(0,.343,.27,.022,.01,.90,.961,TEAL)
    s.beam((-.079,.364,.898),(.079,.364,.898),.021,IVORY,8)
    label(s,0,.326,1.113,.385,.113,2,pi/2)
    s.beam((0,.256,1.49),(0,.316,1.49),.092,SILVER,16)
    s.plate(0,.319,1.49,.159,.159,pi/2,DIAL,True)


def case(s,x,y,z,w,d,h,finish,rotated=False):
    # Leather piping follows the rounded body. Doubled bands wrap over the lid, with catches on the travelling side.
    rad=min(.081,h*.30)
    s.tiers(x,y,[(z,w-.045,d-.04,rad,finish),(z+.027,w,d,rad,finish),
                  (z+h*.68,w,d,rad,finish),(z+h*.71,w+.012,d+.012,rad,BRASS),
                  (z+h-.026,w,d,rad,finish),(z+h,w-.03,d-.03,rad,finish)])
    for dx in (-w*.32,w*.32):
        s.shape(x+dx,y,.059,d+.01,.014,z+.02,z+h+.007,TIMBER if finish==LEATHER else LEATHER)
        if s.rank==0:
            s.shape(x+dx,y-d*.5-.009,.065,.016,.005,z+h*.43,z+h*.64,BRASS)
    if s.rank<2:
        for dx in (-.078,.078):
            s.beam((x+dx,y-.02,z+h),(x+dx,y-.02,z+h+.064),.017,LEATHER,6)
        s.beam((x-.078,y-.02,z+h+.064),(x+.078,y-.02,z+h+.064),.022,LEATHER,6)
    if rotated:
        s.plate(x+w*.15,y-d*.5-.017,z+h*.42,w*.26,h*.31,-pi/2,SIGNS,section=(.1,0,.85,.25))


def porter(s):
    s.count=(5,3,1)[s.rank]
    for x in (-.63,.62):
        for y in (-.36,.36):
            wheel(s,x,y,.16,.16,False)
    s.tiers(0,0,[(.19,1.76,.82,.14,TEAL),(.245,1.84,.88,.15,SILVER),(.30,1.84,.87,.15,TIMBER)])
    for y in (-.34,.34):
        s.beam((-.79,y,.29),(-.86,y,1.10),.036,SILVER,8)
        s.beam((-.86,y,1.10),(-.81,y,1.19),.037,SILVER,8)
    s.beam((-.81,-.345,1.19),(-.81,.345,1.19),.046,LEATHER,10)
    case(s,.23,.01,.30,1.10,.66,.32,LEATHER,True)
    case(s,.19,.045,.62,.95,.50,.25,TEAL)
    case(s,.13,.03,.87,.62,.36,.21,IVORY,True)
    case(s,-.56,-.025,.30,.34,.61,.65,TIMBER)


if __name__=='__main__':
    out,master=[Path(v).resolve() for v in sys.argv[sys.argv.index('--')+1:]]
    use_sheet(out.parent/'Materials/Play/StreetTradesColour.png')
    mat=bpy.data.materials['Harbour architectural finishes']
    mat.name='Harbour enamel, canvas and cut flowers'
    node=next(n for n in mat.node_tree.nodes if n.type=='TEX_IMAGE')
    bsdf=mat.node_tree.nodes.get('Principled BSDF')
    mat.node_tree.links.new(node.outputs['Alpha'],bsdf.inputs['Alpha'])
    bsdf.inputs['Roughness'].default_value=.62
    for name,fn,limits in [('FlowerBarrow',flower_barrow,(5800,3100,1500)),
                           ('ProduceStall',produce_stall,(15000,5600,2400)),
                           ('PneumaticPost',pneumatic_post,(4000,2400,1600)),
                           ('LuggageTrolley',porter,(5500,3000,1600))]:
        build(name,fn,limits,out)
    bpy.ops.wm.save_as_mainfile(filepath=str(master))
