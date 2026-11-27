"""Flower clerk. A receiving hand carries the bowl; the other lifts a single pinned stem."""
from math import cos, pi, sin
from geometry import *


def flower(part,base,top,radius,seed,held=False):
    a,b=Vector(base),Vector(top)
    part.rod(a,b,.004 if part.owner.detail<2 else .006,LEAF,5)
    if part.owner.detail<=2:
        mid=a.lerp(b,.55); tip=mid+Vector((.041*cos(seed),.041*sin(seed),.036))
        across=Vector((-.012*sin(seed),.012*cos(seed),0))
        part.face([mid,mid.lerp(tip,.56)+across,tip,mid.lerp(tip,.56)-across],LEAF)
    normal=Vector((.35,-.73,.58)).normalized() if held else Vector((.18*cos(seed),.23*sin(seed),1)).normalized()
    across=Vector((1,0,0));across=(across-normal*normal.x).normalized()
    along=normal.cross(across)
    count=part.n(9,7,5,4)
    for i in range(count):
        angle=TAU*i/count+seed
        e=across*cos(angle)+along*sin(angle);side=normal.cross(e)
        middle=b+e*radius*.52+normal*radius*.24
        tip=b+e*radius*(.87+.13*sin(seed+i*2))+normal*radius*.59
        wing=side*radius*.39
        tint=PETAL if i%4 else CORAL
        part.face([b,middle-wing,tip-wing*.57,tip+normal*radius*.10,tip+wing*.57,middle+wing],tint)
    if part.owner.detail<=1:
        part.spindle(b,b+normal*.012,[(0,radius*.23,LEATHER),(1,radius*.20,BRASS)],8)


def arm_section(part,a,b,radius):
    stations=[(0,.74,DARK_METAL),(.10,1.10,CHROME),(.17,1.12,CREAM),
              (.30,1.00,STEEL),(.76,.72,CHROME),(.85,.71,STEEL),(.88,.90,BRASS),(1,.73,DARK_METAL)]
    if part.owner.detail>=2:stations=[stations[i] for i in (0,1,4,7)]
    part.spindle(a,b,[(t,radius*rr,mat) for t,rr,mat in stations],part.n(28,18,11,6))


def clerk(detail):
    m=Machine(detail)
    m.scale=1.1
    p=m.part('Clerk_Body')
    # The entire skirt is a tapered, swept casting. An inset dark return makes its rim read in silhouette.
    levels=[(.191,.263,.237,0,.013,INK),(.202,.304,.253,0,.008,STEEL),
            (.215,.322,.267,0,.005,CHROME),(.231,.324,.265,0,.005,MINT),
            (.254,.316,.261,0,.007,MINT),(.267,.314,.258,0,.008,BRASS),
            (.281,.311,.255,0,.009,MINT),(.44,.257,.218,0,.027,MINT),
            (.64,.174,.16,0,.043,MINT),(.755,.125,.112,0,.042,MINT),
            (.783,.112,.102,0,.04,BRASS),(.789,.103,.097,0,.04,STEEL)]
    if detail==2:levels=[levels[i] for i in (0,2,3,5,6,7,8,9,11)]
    if detail==3:levels=[levels[i] for i in (0,2,3,7,9,11)]
    p.shell(levels,sides=p.n(64,40,24,12))
    if detail<=2:
        for angle in (-2.2,-.94):
            path=[(cos(angle)*rx,cy+sin(angle)*ry,z) for z,rx,ry,_,cy,_ in levels[6:-1]]
            if len(path)>=2:p.pipe(path,.005 if detail<2 else .007,BRASS,p.n(8,6,4,3))
    for index,(x,y) in enumerate([(-.219,-.105),(.219,-.105),(0,.184)]):
        z=.082
        p.spindle((x,y,.127),(x,y,.225),[(0,.027,STEEL),(.54,.038,CHROME),(1,.042,INK)],p.n(22,14,8,5))
        wheel_part=m.part('Clerk_Caster'+str(index),(x,y,z),p)
        wheel(wheel_part,(x,y,z),.081,.064,-1 if x<0 else 1)
        if detail<=1:
            sg=-1 if x<0 else 1
            p.pipe([(x+sg*.043,y,z),(x+sg*.051,y,.124),(x+sg*.032,y,.151),(x,y,.153)],.014,STEEL)
    # Offset sprung waist and a two-lobed, cantilevered shoulder shell.
    p.shell([(.782,.106,.096,0,.04,INK),(.806,.122,.106,0,.04,DARK_METAL),
             (.909,.125,.109,0,.028,INK),(.925,.136,.113,0,.023,CHROME)])
    for i in range(p.n(7,5,3,1)):
        z=.805+i*.098/max(1,p.n(7,5,3,1)-1)
        p.spindle((0,.035,z),(0,.035,z+.009),[(0,.119,INK),(.4,.126,STEEL),(1,.119,INK)],p.n(34,22,12,7))
    p.shell([(.916,.121,.100,0,.022,DARK_METAL),(.943,.143,.12,0,.017,BRASS),
             (.99,.173,.137,0,.013,IVORY),(1.055,.196,.149,0,.021,CREAM),
             (1.107,.199,.132,0,.036,IVORY),(1.146,.143,.100,0,.046,IVORY),
             (1.154,.085,.075,0,.045,CHROME)],.81)
    for sg in (-1,1):
        bearing(p,sg*.189,.032,1.07,.082,sg)
    p.spindle((0,.038,1.148),(0,.033,1.286),[(0,.070,STEEL),(.12,.078,BRASS),(.2,.051,INK),(.75,.05,DARK_METAL),(.90,.061,CHROME),(1,.06,BRASS)])
    head=m.part('Clerk_Head',(0,.034,1.278),p)
    # The broad cream brow dips at its nose; the dark glass under the visor is a separate shell.
    head.shell([(1.253,.114,.099,0,.022,STEEL),(1.271,.161,.148,0,.022,INK),
                (1.311,.185,.177,0,.014,INK),(1.365,.184,.181,0,.012,INK)],.80,tilt=-.20)
    head.shell([(1.318,.161,.165,0,.033,DARK_METAL),(1.323,.191,.205,0,.012,BRASS),
                (1.337,.202,.224,0,.008,IVORY),(1.367,.206,.222,0,.015,IVORY),
                (1.418,.186,.190,0,.045,IVORY),(1.466,.140,.144,0,.067,CREAM),
                (1.486,.086,.087,0,.071,IVORY)],.86,tilt=-.43)
    # A projecting glass brow lies just under the shell. Keep the display clear of the shell's front lip.
    panel_screen(head,'Clerk_Head',-.204,1.346,.385,.166,.26)
    if detail<=2:
        for k in range(p.n(6,4,2,0)):
            for sg in (-1,1):
                yy=.071+k*.014
                head.rod((sg*.171,yy,1.327),(sg*.163,yy+.013,1.36),.0038,INK,5)
    # Hold the load at one side, leaving light between the tapered body and each arm.
    for sg,tag in [(1,'Bowl'),(-1,'Pick')]:
        shoulder=(sg*.195,.028,1.073)
        elbow=(sg*(.285 if sg==1 else .31),-.078,.858 if sg==1 else .954)
        wrist=(.166,-.353,.822) if sg==1 else (-.22,-.298,1.129)
        upper=m.part('Clerk_'+tag+'Upper',shoulder,p)
        arm_section(upper,shoulder,elbow,.039)
        lower=m.part('Clerk_'+tag+'Fore',elbow,upper)
        bearing(lower,*elbow,.043,sg)
        arm_section(lower,elbow,wrist,.031)
        hand=m.part('Clerk_'+tag+'Hand',wrist,lower)
        bearing(hand,*wrist,.032,sg)
        w=Vector(wrist)
        if tag=='Bowl':
            for xoff in (-.024,.024):
                pts=[w+Vector((xoff,0,0)),w+Vector((xoff,-.054,.008)),w+Vector((xoff,-.09,.032))]
                hand.pipe(pts,.009,STEEL)
            bowl=m.part('Clerk_Bowl',(.146,-.416,.862),hand)
            bowl.shell([(.842,.049,.045,.146,-.416,BRASS),(.849,.096,.08,.146,-.416,STEEL),
                        (.876,.141,.119,.146,-.416,CHROME),(.913,.163,.142,.146,-.416,BRASS),
                        (.923,.163,.142,.146,-.416,BRASS),(.926,.153,.131,.146,-.416,LEATHER)])
            pos=(.146,-.416,.926)
            load=m.part('Clerk_Flowers',pos,bowl)
            count=p.n(12,8,5,3)
            for i in range(count):
                angle=i*2.40; ring=.104*(.4+.6*((i+.5)/count)**.5)
                x=.146+ring*cos(angle);y=-.416+ring*.85*sin(angle);z=.975+.040*sin(i*2.13)
                flower(load,(.146+(x-.146)*.51,-.416+(y+.416)*.51,.913),(x,y,z),.039,angle)
            paper=m.part('Clerk_Papers',pos,bowl)
            for i in range(3 if detail<=1 else 1):
                paper.shell([(.924+i*.015,.101,.055,.145,-.407,PAPER),(.937+i*.015,.104,.055,.145,-.407,PAPER)],.22,sides=p.n(16,12,8,6))
                if detail<=1:paper.rod((.067,-.437,.939+i*.015),(.20,-.437,.939+i*.015),.0025,INK,4)
        else:
            for sign in (-1,1):
                a=w+Vector((sign*.019,-.015,.009));mid=w+Vector((sign*.027,-.043,.025));tip=w+Vector((sign*.007,-.059,.045))
                hand.pipe([a,mid,tip],.009,STEEL)
            stem=m.part('Clerk_Stem',w,hand)
            flower(stem,(-.222,-.365,1.143),(-.279,-.386,1.299),.052,1.6,held=True)
    return m
