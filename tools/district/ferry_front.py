"""Metre-scale joinery for the Harbour pier head. Local -X is the boarding side."""
from math import pi
from mathutils import Vector

STONE, SOFFIT, TILE, OFFICE, DISPATCH, LOUNGE, BRONZE, OXIDE, MAP_ROOM, VESPER, JUPITER, WOOD, OPAL, ZINC, CLOCK, PRINT = range(16)
UP = Vector((0, 0, 1))


def box(s, x0, x1, y0, y1, z0, z1, slot):
    lo = [(x0,y0,z0),(x1,y0,z0),(x1,y1,z0),(x0,y1,z0)]
    hi = [(x,y,z1) for x,y,z in lo]
    for i in range(4):
        j = (i+1)%4
        s.face([lo[i],lo[j],hi[j],hi[i]],slot)
    s.face(lo[::-1],slot)
    s.face(hi,slot)


def window_band(s, x, y, width, depth, radius, sill, head, booking=False):
    """Rooms stand behind the line of the sash. Jambs mask the back wall at grazing angles."""
    outline = s.contour(x,y,width,depth,radius,(8,5,3)[s.rank])
    west = x-width/2
    for side,(a,b) in enumerate(zip(outline,outline[1:]+outline[:1])):
        if booking and abs(a[0]-west)<.001 and abs(b[0]-west)<.001:
            continue
        start,end = Vector((*a,0)),Vector((*b,0))
        along = (end-start).normalized()
        inward = Vector((-along.y,along.x,0))
        length = (end-start).length
        count = max(1,round(length/((2.45,2.8,4.5)[s.rank])))
        span = length/count
        for bay in range(count):
            origin = start+along*span*bay
            def pt(u,d,z):
                return origin+along*u+inward*d+UP*z
            face = lambda u0,u1,d,z0,z1,slot: s.face([pt(u0,d,z0),pt(u1,d,z0),pt(u1,d,z1),pt(u0,d,z1)],slot)
            divide = min(.085,span*.09)
            inset = min((.38,.25,.10)[s.rank],span*.3)
            jamb = min(.115,span*.11)
            room = LOUNGE if (side//3+bay//2)%6 == 2 else OXIDE
            phase = (side + bay) % 2
            s.face([pt(jamb,inset,sill+.08),pt(span-jamb,inset,sill+.08),pt(span-jamb,inset,head-.10),pt(jamb,inset,head-.10)],room,
                   [(phase*.5,0),((phase+1)*.5,0),((phase+1)*.5,1),(phase*.5,1)])
            for u0,u1 in [(0,jamb),(span-jamb,span)]:
                face(u0,u1,0,sill,head,BRONZE)
            for u in [jamb,span-jamb]:
                vs=[pt(u,0,sill),pt(u,inset,sill+.08),pt(u,inset,head-.10),pt(u,0,head)]
                s.face(vs if u==jamb else vs[::-1],WOOD)
            s.face([pt(jamb,0,sill),pt(span-jamb,0,sill),pt(span-jamb,inset,sill+.08),pt(jamb,inset,sill+.08)],STONE)
            s.face([pt(jamb,0,head),pt(jamb,inset,head-.10),pt(span-jamb,inset,head-.10),pt(span-jamb,0,head)],SOFFIT)
            if s.rank<2:
                face(jamb,span-jamb,-.025,head-.64,head-.59,BRONZE)
                if span>1.4:
                    u=span*.50
                    face(u-divide*.33,u+divide*.33,-.018,sill,head,BRONZE)


def pier(s, y, width, height):
    # Thin bedding joints and a kicked plinth. Both stay behind the boarding edge.
    x0, x1 = -12.21,-10.71
    box(s,x0+.06,x1,y-width/2,y+width/2,.12,height,STONE)
    box(s,x0,x1,y-width/2-.035,y+width/2+.035,.06,.43,SOFFIT)
    if s.rank<2:
        for z in (.79,1.48,2.17,2.86):
            if z<height: box(s,x0+.047,x0+.064,y-width/2,y+width/2,z,z+.012,SOFFIT)


def lamp(s, y, z):
    x=-12.17
    s.beam((x,y,z),(x-.16,y,z),.145,BRONZE,12)
    s.beam((x-.16,y,z-.22),(x-.16,y,z+.22),.095,OPAL,(14,9,6)[s.rank])
    for h in [-.23,.23]:
        s.beam((x-.16,y,z+h-.021),(x-.16,y,z+h+.021),.111,BRONZE,10)


def poster(s, y, slot):
    box(s,-12.19,-12.10,y-.47,y+.47,1.04,2.42,BRONZE)
    s.plate(-12.195,y,1.73,.875,1.30,pi,slot)


def office(s):
    # The ferry is seaward of the walking mesh. The front edge of the counter meets it at x=-12.64.
    # Left to right from a passenger's viewpoint is local -Y.
    front = -12.09
    for y,w in [(-12.95,.88),(-10.49,.44),(-4.27,.58),(-1.17,.60),(4.2,.58),(8.9,.52),(13.0,.78)]:
        pier(s,y,w,4.22)
    # Back walls, walls of the reveals, a counter where paper actually passes hands.
    def booth(centre,width,slot):
        a,b=centre-width/2,centre+width/2
        s.plate(-10.21,centre,2.02,width,2.82,pi,slot)
        box(s,front,-10.17,a-.09,a,.23,3.47,WOOD)
        box(s,front,-10.17,b,b+.09,.23,3.47,WOOD)
        box(s,front,-10.17,a,b,3.37,3.49,SOFFIT)
        box(s,front+.035,-10.12,a,b,.10,.96,TILE)
        box(s,-12.61,-10.48,a-.06,b+.06,.966,1.055,SOFFIT)
        box(s,-12.62,-12.55,a-.06,b+.06,.99,1.025,BRONZE)
        for u in (a+.10,b-.10):
            box(s,front-.025,front+.065,u-.029,u+.029,1.055,3.34,BRONZE)
        if width>5:
            box(s,front-.02,front+.12,centre-.037,centre+.037,1.055,3.34,BRONZE)
        # Transom laid over a physically open handover slot.
        box(s,front-.02,front+.10,a,b,2.57,2.64,BRONZE)
        if s.rank<2:
            for u in (centre-.32,centre+.32):
                box(s,front,front+.065,u-.017,u+.017,2.64,3.34,BRONZE)
            s.plate(front-.038,centre,2.945,.40,.28,pi,PRINT,section=(.68,.045,.965,.25))
    booth(-7.37,5.65,OFFICE)
    booth(6.55,4.03,DISPATCH)
    # A pair of 0.90 m leaves beneath a fixed fanlight. The handle is usable at hand height.
    door_y=-2.71
    a,b=door_y-1.16,door_y+1.16
    box(s,-11.67,-10.2,a-.03,b+.03,.07,.14,SOFFIT)
    s.plate(-10.65,door_y,1.47,2.33,2.70,pi,MAP_ROOM)
    for u in (a,door_y,b):
        box(s,-11.60,-11.48,u-.036,u+.036,.15,2.42,BRONZE)
    for z in (.17,2.37):
        box(s,-11.60,-11.48,a,b,z,z+.07,BRONZE)
    for u in (door_y-.13,door_y+.13):
        s.beam((-11.70,u,.91),(-11.70,u,1.30),.019,BRONZE,8)
        for z in (.93,1.28): s.beam((-11.70,u,z),(-11.56,u,z),.017,BRONZE,6)
    box(s,-12.02,-10.64,a-.13,a,.14,3.40,STONE)
    box(s,-12.02,-10.64,b,b+.13,.14,3.40,STONE)
    box(s,-12.02,-10.64,a,b,2.66,3.40,STONE)
    # Separate illustrated destinations beside the booking aperture. Printed at ordinary poster scale.
    for lo,hi in [(-13.70,-10.54),(-1.13,4.17),(8.95,13.70)]:
        box(s,front,-10.37,lo,hi,.14,3.42,STONE)
    for u,slot in [(-11.85,VESPER),(1.03,JUPITER),(2.31,VESPER),(11.1,JUPITER)]: poster(s,u,slot)
    # A small vitrolite fascia, a rolled zinc drip and a ribbed soffit over the ticket queue.
    box(s,-12.10,-10.36,-13.75,13.75,3.40,4.32,STONE)
    box(s,-12.18,-12.09,-10.26,-4.48,2.93,3.52,BRONZE)
    s.plate(-12.188,-7.37,3.225,5.68,.50,pi,PRINT,section=(.01,.52,.99,.98))
    for y in (-10.48,-4.28,4.18,8.9):
        if s.rank==0 or y in (-10.48,-4.28):lamp(s,y,2.46)
    if s.rank<2:
        # A timber waiting bench against a wall, away from the public path.
        box(s,-12.49,-12.06,9.08,10.32,.43,.50,WOOD)
        for u in (9.25,10.13): box(s,-12.39,-12.24,u-.035,u+.035,.11,.43,BRONZE)
        for u in (-9.37,-7.43,-5.49,5.42,7.54):
            s.beam((-11.95,u,3.82),(-11.95,u,4.24),.025,BRONZE,6)
            s.beam((-11.95,u,3.74),(-11.95,u,3.82),.145,OPAL,12)


def boarding_canopy(s):
    # One tapered cantilever identifies the passenger entrance, like the wing of a flying boat.
    # The root bolts to the terminal. Its nose clears the apron by more than 3.8 m.
    n=(20,12,6)[s.rank]; sections=[]
    for k in range(n+1):
        t=-1+2*k/n
        span=1-t*t
        y=-6.8+8.15*t
        root=-11.72
        reach=.52+4.30*span
        tip=root-reach
        sections.append([(root,y,3.94),(tip+.48*reach,y,3.78+.10*span),(tip,y,3.96+.22*span),
                         (tip+.035,y,4.025+.22*span),(tip+.49*reach,y,4.06+.14*span),(root,y,4.13)])
    for a,b in zip(sections,sections[1:]):
        for i,slot in enumerate((SOFFIT,SOFFIT,BRONZE,TILE,STONE,STONE)):
            j=(i+1)%6
            s.face([a[i],a[j],b[j],b[i]],slot)
    s.face(sections[0][::-1],STONE);s.face(sections[-1],STONE)
    for t in (-.80,-.42,0,.42,.80):
        y=-6.8+8.15*t;p=1-t*t
        s.beam((-12.08,y,3.12),(-12.36-1.63*p,y,3.82),.077,BRONZE,8,.046)
        if s.rank==0:
            s.beam((-12.12,y,3.925),(-12.46-2.52*p,y,3.886),.034,OPAL,8)
