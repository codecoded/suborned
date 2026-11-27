"""The sweeper's twin brush cartridges and the post runner's hatch, capsule and semaphore."""
from math import cos, pi, sin
from geometry import (Machine, Vector, around, panel_screen, IVORY, CORAL, CHROME, STEEL,
                      BRASS, TYRE, INK, LEATHER, CREAM, DARK_METAL, SERVICE_GREEN, BRISTLE)


def side_x(levels,power,y,z):
    """Point on a superelliptic casting for the flange of a pressed louvre."""
    lo,hi=next((a,b) for a,b in zip(levels,levels[1:]) if a[0]<=z<=b[0])
    t=(z-lo[0])/(hi[0]-lo[0])
    rx,ry,cx,cy=(a+(b-a)*t for a,b in zip(lo[1:5],hi[1:5]))
    return cx+rx*max(0,1-abs((y-cy)/ry)**(2/power))**(power/2)


def louvres(part,levels,power,side,y,z,width,spacing,number,lip=CHROME):
    for row in range(number):
        height=z+row*spacing
        outline=part.contour(y,height,width,.011,.004,2)
        def point(yy,zz,projection):
            return (side*(side_x(levels,power,yy,zz)+projection),yy,zz)
        points=[point(yy,zz,.003) for yy,zz in outline]
        part.face(points if side>0 else points[::-1],INK)
        ends=[point(y+v*width,height-.003,.005) for v in (-.47,0,.47)]
        part.pipe(ends,.0028,lip,part.n(7,5,4,3))


def axle_wheel(machine, body, name, x, y, z, radius, width):
    """Wheel pods become part of the casting on the far model."""
    sign=1 if x>=0 else -1
    part=body if machine.detail==3 else machine.part(name,(x,y,z),body)
    if machine.detail==3:
        part.spindle((x-sign*width*.5,y,z),(x+sign*width*.55,y,z),
                     [(0,radius*.87,TYRE),(.32,radius,TYRE),(.85,radius,TYRE),(1,radius*.66,CHROME)],8)
    else:
        profile=[(0,.70,DARK_METAL),(.12,.96,TYRE),(.26,1,TYRE),(.64,1,TYRE),
                 (.83,.96,TYRE),(.94,.75,TYRE),(.98,.71,CHROME),(1.02,.60,STEEL),
                 (1.12,.25,CHROME),(1.14,.16,BRASS)]
        if machine.detail==2:profile=[profile[i] for i in (0,2,3,5,7,9)]
        part.spindle((x-sign*width*.5,y,z),(x+sign*width*.5,y,z),
                     [(t,r*radius,finish) for t,r,finish in profile],part.n(44,23,11,8))
        if machine.detail<=1:
            for k in around(5):
                p=(x+sign*width*.60,y+radius*.48*cos(k),z+radius*.48*sin(k))
                part.rod(p,(p[0]+sign*.0018,p[1],p[2]),radius*.044,DARK_METAL,6)
    return part


def brush(machine, body, cx, suffix):
    """Tapered twisted tufts. The lower fringe is visible below each turning metal dish."""
    part=machine.part('Sweeper_Brush'+suffix,(cx,-.316,.055),body)
    detail=machine.detail
    num=part.n(40,26,15,8)
    centre=Vector((cx,-.316,0))
    top=[]; roots=[]; tips=[]
    for i,a in enumerate(around(num)):
        unit=Vector((cos(a),sin(a),0)); swept=Vector((cos(a+.095),sin(a+.095),0))
        top.append(centre+unit*.084+Vector((0,0,.079)))
        roots.append(centre+unit*.104+Vector((0,0,.060)))
        tips.append(centre+swept*(.141 if i%3 else .135)+Vector((0,0,.007 if i%3 else .009)))
    for i in range(num):
        j=(i+1)%num
        part.face([roots[i],roots[j],top[j],top[i]],BRISTLE)
        part.face([tips[i],tips[j],roots[j],roots[i]],BRISTLE)
        if detail<=1:
            for k in range(3):
                t=(k+.16)/3
                base=tips[i].lerp(tips[j],t); outward=base-centre;outward.z=0
                tip=base+outward*.035;tip.z=.002+(.0018*((i+k)%3))
                top_edge=roots[i].lerp(roots[j],t)+outward*.02
                delta=(tips[j]-tips[i])*.105
                part.face([tip,tip+delta*.42,top_edge+delta,top_edge],BRISTLE)
    levels=[(.056,.106,.106,cx,-.316,DARK_METAL),(.065,.111,.111,cx,-.316,BRASS),
            (.076,.108,.108,cx,-.316,CHROME),(.083,.098,.098,cx,-.316,CHROME),
            (.09,.077,.077,cx,-.316,IVORY),(.101,.069,.069,cx,-.316,STEEL),
            (.105,.039,.039,cx,-.316,BRASS)]
    if detail>=2:levels=[levels[i] for i in (0,2,4,6)]
    part.shell(levels,sides=part.n(40,26,14,8))
    if detail<=1:
        for a in around(3):
            x=cx+.078*cos(a);y=-.316+.078*sin(a)
            part.rod((x,y,.082),(x,y,.088),.008,BRASS,7)


def sweeper(detail):
    m=Machine(detail);p=m.part('Sweeper_Body')
    far=detail==3; mid=detail==2
    chassis=[(.071,.204,.296,0,-.002,INK),(.085,.262,.328,0,-.002,TYRE),
             (.104,.286,.335,0,.001,CHROME),(.117,.287,.338,0,.004,BRASS),
             (.124,.282,.332,0,.005,SERVICE_GREEN),(.166,.285,.330,0,.002,SERVICE_GREEN),
             (.212,.275,.326,0,.006,SERVICE_GREEN),(.236,.252,.296,0,.024,STEEL)]
    bonnet=[(.197,.244,.277,0,.037,INK),(.215,.261,.292,0,.038,CHROME),
            (.226,.261,.293,0,.038,IVORY),(.28,.249,.275,0,.034,IVORY),
            (.335,.231,.248,0,.04,IVORY),(.381,.205,.214,0,.049,CREAM),
            (.405,.164,.177,0,.056,IVORY),(.411,.115,.135,0,.064,IVORY)]
    if far:chassis=[chassis[i] for i in (0,2,4,6)];bonnet=[bonnet[i] for i in (0,2,4,6)]
    elif mid:chassis=chassis[::2];bonnet=[bonnet[i] for i in (0,2,4,5,7)]
    p.shell(chassis,.57,sides=p.n(48,32,20,8));p.shell(bonnet,.64,sides=p.n(48,32,20,10))
    for sg,suffix in [(-1,'L'),(1,'R')]:
        x=sg*.264; y=.143;radius=.119
        axle_wheel(m,p,'Sweeper_Wheel'+suffix,x,y,.121,radius,.077)
        if not far:
            steps=p.n(18,12,7,4)
            arc=[(sg*.283,y+.134*cos(pi*i/steps),.121+.133*sin(pi*i/steps)) for i in range(steps+1)]
            p.pipe(arc,.012,CHROME,p.n(10,7,5,4))
            louvres(p,chassis,.57,sg,-.113,.149,.132,.020,3 if not mid else 2)
        p.rod((sg*.152,-.236,.139),(sg*.219,-.316,.101),.023,STEEL,5 if far else 10)
        brush(m,p,sg*.219,suffix)
    # Lower bumper and chrome intake below the raised face. The two cartridges are in clear air.
    if not far:
        p.plaque(0,-.339,.143,.256,.090,.027,CHROME)
        p.plaque(0,-.341,.143,.236,.072,.022,INK)
        for i in range(3 if detail<2 else 2):
            p.rod((-.104,-.344,.124+i*.018),(.104,-.344,.124+i*.018),.005,STEEL,8)
        for sign in (-1,1):
            p.spindle((sign*.232,-.267,.12),(sign*.232,-.267,.158),[(0,.026,DARK_METAL),(.3,.029,CHROME),(1,.018,STEEL)],10)
    panel_screen(p,'Sweeper_Head',-.311,.279,.464,.178,.50)
    # Pressed louvres follow the removable bag lid. A leather grip bridges two brass feet.
    if not far:
        for sg in (-1,1):
            louvres(p,bonnet,.64,sg,.078,.270,.181,.019,4 if not mid else 3,STEEL)
            p.spindle((sg*.102,.066,.402),(sg*.102,.066,.430),[(0,.028,BRASS),(.22,.029,BRASS),(1,.017,STEEL)],p.n(22,14,8,5))
            if not mid:
                p.rod((sg*.245,-.110,.215),(sg*.251,-.111,.247),.009,BRASS,8)
    p.pipe([(-.102,.066,.401),(-.102,.066,.441),(-.085,.066,.466),
            (.085,.066,.466),(.102,.066,.441),(.102,.066,.401)],
           .014,BRASS,p.n(14,10,6,4))
    if not far:p.rod((-.061,.066,.466),(.061,.066,.466),.0185,LEATHER,p.n(19,12,8,5))
    return m


def runner(detail):
    m=Machine(detail);p=m.part('Runner_Body')
    far=detail==3
    levels=[(.066,.103,.138,0,.011,INK),(.078,.135,.156,0,.009,STEEL),(.091,.144,.166,0,.008,BRASS),
            (.108,.145,.170,0,.009,CORAL),(.238,.159,.160,0,.016,CORAL),(.413,.165,.134,0,.014,CORAL),
            (.504,.155,.122,0,.009,CORAL),(.550,.129,.107,0,.002,CORAL),(.574,.091,.083,0,-.008,BRASS)]
    if far:levels=[levels[i] for i in (0,2,4,6,8)]
    elif detail==2:levels=[levels[i] for i in (0,2,3,4,6,7,8)]
    p.shell(levels,.59,sides=p.n(48,30,16,8))
    for sg,suffix in [(-1,'L'),(1,'R')]:
        x=sg*.165;cy=.035
        axle_wheel(m,p,'Runner_Wheel'+suffix,x,cy,.151,.150,.077)
        if not far:
            steps=p.n(22,13,7,4)
            arc=[(sg*.177,cy+.169*cos(pi*i/steps),.151+.169*sin(pi*i/steps)) for i in range(steps+1)]
            p.pipe(arc,.017,CORAL,p.n(13,9,6,4))
            p.pipe([(v[0]+sg*.013,v[1],v[2]) for v in arc],.005,BRASS,p.n(8,6,4,3))
    p.rod((0,.105,.133),(0,.24,.077),.027,STEEL,6 if far else 12)
    axle_wheel(m,p,'Runner_Caster',0,.224,.068,.067,.041)
    # Mail hopper with a dark letter slot beneath its chromed flap. Pivot runs across the top edge.
    y=-.154
    hatch=m.part('Runner_Hatch',(0,y,.441),p) if not far else p
    if not far:
        p.plaque(0,y-.006,.375,.225,.157,.020,BRASS)
        p.plaque(0,y-.008,.375,.212,.146,.017,INK)
    hatch.shell([(.315,.084,.012,0,y-.008,BRASS),(.326,.101,.017,0,y-.011,CREAM),
                 (.429,.102,.016,0,y,IVORY),(.443,.092,.012,0,y,BRASS)],.29,sides=p.n(24,16,10,6))
    if not far:
        hatch.plaque(0,y-.028,.402,.154,.018,.005,INK)
        hatch.rod((-.081,y-.031,.417),(.081,y-.031,.417),.0045,CHROME,8)
        hatch.rod((0,y-.026,.318),(0,y-.027,.342),.011,BRASS,10)
        for i in range(p.n(4,4,2,1)):
            zz=.155+i*.024
            p.plaque(0,-.163,zz,.113,.014,.005,INK)
            p.rod((-.052,-.165,zz+.002),(.052,-.165,zz+.002),.0036,CHROME,6)
    # The delivery capsule lies fore and aft, supported by a steel saddle on the right flank.
    cx=.170;cz=.430
    a=(cx,-.032,cz);b=(cx,.227,cz)
    profile=[(0,.053,BRASS),(.04,.064,BRASS),(.08,.068,CHROME),(.16,.063,CREAM),
             (.19,.067,BRASS),(.23,.063,CREAM),(.70,.063,CREAM),(.74,.068,BRASS),(.79,.063,CREAM),
             (.91,.064,CREAM),(.94,.069,CHROME),(1,.050,BRASS)]
    if detail>=2:profile=[profile[i] for i in (0,2,5,6,10,11)]
    p.spindle(a,b,profile,p.n(36,20,10,7))
    if not far:
        for yy in (.022,.154):
            p.pipe([(.121,yy,.462),(.148,yy,.488),(.186,yy,.494),(.230,yy,.467),
                    (.243,yy,.432),(.228,yy,.391),(.184,yy,.364),(.130,yy,.371)],.0075,BRASS,p.n(12,7,4,4))
        p.pipe([(.122,-.01,.362),(.195,-.01,.356),(.203,.180,.356),(.123,.180,.362)],.009,STEEL,p.n(10,6,4,4))
        p.spindle((cx,-.033,cz),(cx,-.040,cz),[(0,.044,STEEL),(.55,.034,BRASS),(1,.025,LEATHER)],p.n(32,16,8,5))
    head=p if far else m.part('Runner_Head',(0,-.012,.554),p)
    hood=[(.536,.119,.079,0,-.006,STEEL),(.552,.173,.107,0,-.013,BRASS),
                (.572,.198,.126,0,-.016,IVORY),(.638,.208,.134,0,-.010,IVORY),
                (.685,.190,.118,0,-.005,CREAM),(.701,.142,.090,0,.001,IVORY)]
    head.shell([hood[i] for i in (0,2,3,5)] if far else hood,.69,sides=p.n(48,32,16,8))
    panel_screen(head,'Runner_Head',-.163,.621,.373,.157,.35)
    if not far:
        for sg in (-1,1):
            louvres(head,hood,.69,sg,.012,.596,.120,.018,3 if detail<=1 else 2)
    # The flag folds down over the capsule in transit and stands up while the hatch is open.
    fx=.138;fy=.102;fz=.529
    flag=m.part('Runner_Flag',(fx,fy,fz),p)
    pole=[(fx,fy,fz),(fx,fy+.025,.616),(fx,fy+.065,.777)]
    flag.pipe(pole,.0092,BRASS,p.n(16,12,7,4))
    flag.rod((fx-.022,fy,fz),(fx+.022,fy,fz),.028,BRASS,p.n(24,16,8,5))
    end=Vector(pole[-1]);flag.rod(end,end+Vector((0,0,.014)),.013,BRASS,p.n(16,10,6,4))
    # The enameled swallowtail is a physical double-sided plate with a rolled brass edging.
    flag_outline=[(0,.766),(.112,.765),(.088,.741),(.111,.718),(0,.714)]
    blade=[(fx+.83*d,fy+.061+.55*d+(zz-.766)*.28,zz) for d,zz in flag_outline]
    flat=[(x+.002,y-.002,z) for x,y,z in blade]
    flag.face(blade,CORAL);flag.face(flat[::-1],CORAL)
    if not far:
        flag.pipe(blade+blade[:1],.0034,BRASS,p.n(8,5,4,3))
    return m
