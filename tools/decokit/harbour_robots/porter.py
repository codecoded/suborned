"""Quay porter. The deck is the front. The pin centres form two equal parallel links."""
from math import cos, pi, sin
from geometry import *


def arm_outline(a,b,r,samples):
    a,b=Vector(a),Vector(b)
    direction=(b-a).normalized()
    normal=Vector((-direction.y,direction.x))
    result=[]
    for centre, sign in ((b,1),(a,-1)):
        for i in range(samples+1):
            angle=pi*i/samples
            result.append(tuple(centre + r*sign*(normal*cos(angle)+direction*sin(angle))))
    return result


def porter(detail):
    m=Machine(detail)
    m.scale=1.05
    p=m.part('Porter_Body')
    p.shell([(.095,.273,.69,0,-.02,DARK_METAL),(.13,.327,.78,0,-.04,INK),
             (.19,.346,.8,0,-.04,CORAL),(.23,.323,.76,0,-.02,CORAL)],.44)
    # A long cast mudguard across the rear axle, with a bonnet that narrows into the deck.
    side=[(-.46,.18),(-.31,.197),(-.19,.26),(-.08,.45),(.005,.59),(.10,.69),
          (.21,.762),(.35,.805),(.50,.822),(.63,.795),(.73,.73),(.82,.62),
          (.856,.47),(.86,.25),(.80,.159),(.59,.14),(.16,.142),(-.24,.145)]
    if detail>=2:side=side[::2]
    p.side_casting(side,(-.337,.337),CORAL)
    p.shell([(.67,.252,.301,0,.413,CORAL),(.706,.286,.302,0,.42,CHROME),
             (.728,.27,.288,0,.425,IVORY),(.82,.229,.243,0,.445,IVORY),
             (.86,.168,.171,0,.455,CREAM)],.73)
    for side_sign in (-1,1):
        x=side_sign*.36; at=(x,.50,.337)
        w=m.part('Porter_Wheel'+('L' if side_sign<0 else 'R'),at,p)
        wheel(w,at,.335,.125,side_sign)
        if detail<=1:
            # Stamped finger grooves and a convex centre cap catch a stripe of light as the wheel rolls.
            for a in around(5):
                yy=.50+.137*cos(a); zz=.337+.137*sin(a)
                end=(side_sign*.448, yy+.030*cos(a+.24), zz+.030*sin(a+.24))
                w.rod((side_sign*.448,yy,zz),end,.005,DARK_METAL,6)
            w.spindle((side_sign*.446,.5,.337),(side_sign*.459,.5,.337),
                      [(0,.080,INK),(.15,.082,CHROME),(.86,.060,STEEL),(1,.026,CHROME)],w.n(36,22,10,8))
        if detail<=2:
            arc=[(side_sign*.352,.50+.372*cos(pi*i/18),.337+.372*sin(pi*i/18)) for i in range(19)]
            p.pipe(arc,.0175,CHROME,p.n(10,7,5,4))
        if detail<=1:
            # Recess forward of the wheel. The louvres read against a single ivory insert.
            inset=[(-.148,.229),(-.055,.463),(.124,.523),(.121,.259),(.069,.229)]
            px=side_sign*.34
            p.side_casting(inset,sorted((px,px+side_sign*.009)),IVORY)
            for i in range(4):
                zz=.285+i*.029
                p.rod((side_sign*.350,-.073+i*.009,zz),(side_sign*.350,.081,zz+.02),.0058,DARK_METAL,6)
    # A front caster peeks below the deck. Its fork is carried by the chassis.
    p.rod((0,-.689,.20),(0,-.689,.12),.048,STEEL)
    foot=m.part('Porter_NoseWheel',(0,-.67,.103),p)
    wheel(foot,(0,-.67,.103),.103,.105,1)
    # The bellows, collars and bright piston rods are visible through the neck.
    p.spindle((0,.443,.846),(0,.443,1.105),
              [(0,.132,STEEL),(.06,.137,CHROME),(.13,.095,INK),(.80,.082,DARK_METAL),
               (.85,.111,BRASS),(.96,.125,CHROME),(1,.119,STEEL)])
    if detail<=2:
        for xx in (-.081,.081):
            for yy in (.386,.505):
                p.rod((xx,yy,.876),(xx,yy,1.078),.018,CHROME)
        for zz in (.901,.938,.976,1.013):
            p.spindle((0,.443,zz),(0,.443,zz+.010),[(0,.091,STEEL),(1,.091,DARK_METAL)],p.n(28,16,9,6))
    h=m.part('Porter_Head',(0,.443,1.104),p)
    h.shell([(1.048,.241,.12,0,.44,DARK_METAL),(1.063,.338,.154,0,.44,CHROME),
             (1.091,.376,.176,0,.44,IVORY),(1.238,.387,.181,0,.44,IVORY),
             (1.289,.372,.170,0,.448,CREAM),(1.316,.311,.143,0,.45,IVORY),
             (1.325,.238,.11,0,.45,IVORY)],.55)
    panel_screen(h,'Porter_Head',.195,1.187,.704,.270)
    if detail<=2:
        for i in range((5,4,2,0)[detail]):
            yy=.47+i*.016
            for sg in (-1,1):
                h.rod((sg*.378,yy,1.142),(sg*.378,yy+.007,1.207),.0055,INK,5)
        h.rod((-.21,.382,1.320),(.21,.382,1.320),.005,CHROME,6)
    # The tray and the case share a rigid pivot. The parallel links rotate separately.
    tray=m.part('Porter_Deck',(0,-.477,.306),p)
    tray.shell([(.217,.327,.501,0,-.491,INK),(.232,.372,.537,0,-.493,CHROME),
                (.249,.382,.539,0,-.493,STEEL),(.260,.382,.539,0,-.493,CHROME),
                (.279,.374,.533,0,-.493,BRASS),(.292,.36,.524,0,-.493,WOOD)],.40)
    if detail<=2:
        for i in range((-4 if detail<2 else -2),(5 if detail<2 else 3)):
            xx=i*(.067 if detail<2 else .12)
            tray.rod((xx,-.966,.295),(xx,-.01,.295),.004 if detail<2 else .006,INK,4)
    for tag,z in [('Upper',.506),('Lower',.371)]:
        link=m.part('Porter_Link'+tag,(0,.118,z),p)
        ay,by=.118,-.477
        for sg in (-1,1):
            x=sg*.335
            outline=arm_outline((ay,z),(by,z-.142),.041 if tag=='Upper' else .037,link.n(9,6,3,2))
            link.side_casting(outline,(x-.022,x+.022),STEEL if tag=='Upper' else CHROME)
            for yy,zz in ((ay,z),(by,z-.142)):
                bearing(link,x+sg*.027,yy,zz,.052,sg)
            if detail<=1:
                start=(sg*.359,ay-.062,z-.015);end=(sg*.359,by+.069,z-.126)
                link.rod(start,end,.007,DARK_METAL,7)
    for sg in (-1,1):
        # Both distal pins bolt onto the same upright on the tray. They remain captive at full lift.
        x=sg*.306
        tray.side_casting([(-.533,.236),(-.495,.210),(-.434,.250),(-.442,.363),
                           (-.468,.396),(-.499,.395),(-.529,.359)],(x-.018,x+.018),STEEL)
    # Upright stitched case. The bowed handle is open, with real negative space beneath it.
    cy=-.267
    tray.shell([(.295,.249,.129,0,cy,LEATHER),(.312,.265,.146,0,cy,LEATHER),
                (.56,.265,.146,0,cy,LEATHER),(.585,.25,.131,0,cy,LEATHER)],.28)
    for xx in (-.174,.174):
        path=[(xx,cy-.149,.307),(xx,cy-.149,.547),(xx,cy-.14,.577),
              (xx,cy-.119,.589),(xx,cy+.118,.589),(xx,cy+.144,.56),(xx,cy+.148,.314)]
        for a,b in zip(path,path[1:]):
            dx=Vector((.011 if detail<3 else .017,0,0))
            a,b=Vector(a),Vector(b)
            tray.face([a-dx,a+dx,b+dx,b-dx],DARK_METAL)
        if detail<=2:
            tray.plaque(xx,cy-.152,.453,.047,.044,.005,BRASS)
            tray.plaque(xx,cy-.155,.454,.027,.024,.003,LEATHER)
    if detail<=2:
        tray.pipe([(-.088,cy,.589),(-.074,cy,.627),(-.051,cy,.639),
                   (.050,cy,.639),(.074,cy,.627),(.088,cy,.589)],.012,LEATHER)
        for xx in (-.087,.087):
            tray.rod((xx,cy,.584),(xx,cy,.598),.018,BRASS,9)
    if detail<=1:
        for xx in (-.242,.242):
            tray.pipe([(xx,cy-.128,.529),(xx,cy-.129,.565),(xx,cy-.113,.578)],.007,BRASS,6)
    return m
