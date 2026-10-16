# Glyph, brief 014 A: the grammar sheet (price line, world markers, scope, lines, Alt, budget).
from g14 import *
import numpy as np
PAGE=(247,244,236,255); CW=2240; M=60
def bar(cv,y,n,s):
    d=ImageDraw.Draw(cv); d.rounded_rectangle((M,y,CW-M,y+64),radius=20,fill=C['ink']); text(cv,M+24,y+32,str(n),'d',22,'Bold','turquoise-light','lm'); text(cv,M+24+tw(str(n),'d',22,'Bold')+18,y+32,s,'d',22,'Bold','cream','lm'); return y+90
def bullets(cv,x,y,w,items,size=13): return notes(cv,x,y,w,items,size=size,col='ink',gap=6)
def dark(cv,box,r=30): d=ImageDraw.Draw(cv); d.rounded_rectangle(box,radius=r,fill=C['ink'])
def tile(bgname,crop,draw_fn,scale_up=1.0):
    """a street tile: crop of the patched street (1x px), drawn on at 2x, returns (1x actual, zoom)"""
    bg=Image.open(f'bg/{bgname}.png').convert('RGB').crop(crop).resize(((crop[2]-crop[0])*2,(crop[3]-crop[1])*2),Image.LANCZOS).convert('RGBA')
    draw_fn(bg); return bg
def people(cv,pts,kind='civ',s=2.4):
    for x,y,k in pts: person(cv,x,y,k,s)
# ------------------------------------------------------------------ 1 rest
def s_rest(cv,y):
    y=bar(cv,y,1,'At rest: what stays, how small')
    im=Image.open('after/06.png').convert('RGB'); iw=1500; ih=int(1142*iw/1726); cv.paste(im.resize((iw,ih),Image.LANCZOS),(M,y))
    hud=np.array(Image.open('after/06.hud.png').split()[3])>=24; tot=hud.size
    def share(r): x,y0,w,h=r; return hud[y0:y0+h,x:x+w].sum()/tot*100
    regs=[('Roster, four portraits with health pips',(10,88,96,360)),('Order plate for the selected agent, and one chip per busy agent',(106,110,235,330)),('Status pill: clock, next mark, $, Trail, Alert lamps; two keys',(1300,8,420,90)),('Plot scope (minimap)',(10,994,220,138))]
    x0=M+iw+50; yy=y
    text(cv,x0,yy+10,'WHAT IT COVERS (moment 6, % of the screen)','d',12,'SemiBold','deep-teal','lm'); yy+=50
    tsum=0
    for lab,r in regs:
        p=share(r); tsum+=p
        for i,l in enumerate(wrap(lab,'b',13,'Medium',CW-M-x0-150)): text(cv,x0,yy+i*32,l,'b',13,'Medium','ink')
        text(cv,CW-M-20,yy,f'{p:.1f}%','d',16,'Bold','ink','ra'); yy+=34*len(wrap(lab,'b',13,'Medium',CW-M-x0-150))+14
    full=hud.sum()/tot*100
    text(cv,x0,yy,'Everything else in the street','b',13,'Medium','ink'); text(cv,CW-M-20,yy,f'{max(0,full-tsum):.1f}%','d',16,'Bold','ink','ra'); yy+=50
    d=ImageDraw.Draw(cv); d.line([(x0,yy),(CW-M,yy)],fill=C['ink'],width=3); yy+=18
    text(cv,x0,yy+14,'Total at rest','d',17,'Bold','ink','lm'); text(cv,CW-M-20,yy+14,f'{full:.1f}%','d',20,'Bold','ink','rm'); yy+=70
    bullets(cv,x0,yy,CW-M-x0,['Budget: fail above 15% at rest (22% with an order card open, 30% with Alt held). Aim for under 10%. Old HUD, same moment: 14% by bounding boxes, with 4 lines and 3 blocks of running text.','Gone from rest: strips, the NEXT STEP box, the objective bar, the Intel and Response pills, the SCENES stack, the verb captions, the ticker.','The scope carries the persistent state (section 4); that is what lets the rest get this small.'],size=12.5)
    return y+ih+50
# ------------------------------------------------------------------ 2 price line
FIELDS=[('name','verb or way','text'),('$','p_cash','$90,000'),('time','p_time','7 s'),('Trail','p_trail','+4'),('Alert','p_alert','1'),('eye','eye_shut',''),('learns','routine','cone'),('pace','s_carrying','1.4 m/s'),('answer','mark_ok','')]
def s_price(cv,y):
    y=bar(cv,y,2,'Order and action cards: one line per way, icons and numbers')
    dark(cv,(M,y,CW-M,y+300))
    text(cv,M+30,y+34,'THE FIELDS, IN THIS ORDER (Fable\'s list; a zero is not shown; "?" where the engine says "?")','d',12,'SemiBold','text-dim','lm')
    x=M+40
    for nm,ic,ex in FIELDS:
        w=230
        d=ImageDraw.Draw(cv); d.rounded_rectangle((x,y+64,x+w-16,y+230),radius=18,fill=(36,68,66,255))
        text(cv,x+(w-16)/2,y+84,nm.upper(),'d',11,'SemiBold','turquoise-light','mm')
        if nm=='name': text(cv,x+(w-16)/2,y+150,'Knock out','d',15,'Bold','cream','mm')
        elif nm=='learns': put_icon(cv,'routine',x+40,y+120,48,CREAM); put_icon(cv,'cone',x+104,y+120,48,CREAM); text(cv,x+(w-16)/2,y+198,'routine, sight','b',11,'Medium','text-dim','mm')
        elif nm=='eye':
            for j,st in enumerate(('open','shut','unk')): m_eye(cv,x+30+j*62,y+148,st,'none',24)
            text(cv,x+(w-16)/2,y+198,'seen  nobody  ?','b',11,'Medium','text-dim','mm')
        elif nm=='answer':
            for j,mk in enumerate(('mark_ok','mark_no','mark_half','mark_unk')): m=spr(mk).resize((40,40),Image.LANCZOS); cv.paste(m,(int(x+14+j*46),int(y+128)),m)
            text(cv,x+(w-16)/2,y+198,'works  refuse  not yet  ?','b',11,'Medium','text-dim','mm')
        else:
            put_icon(cv,ic,x+30,y+118,54,CREAM); text(cv,x+96,y+146,ex,'d',16,'Bold','cream','lm')
            text(cv,x+(w-16)/2,y+198,{'$':'cash','time':'walk + act','Trail':'traced','Alert':'if it goes wrong','pace':'carry speed'}.get(nm,''),'b',11,'Medium','text-dim','mm')
        x+=w
    text(cv,M+40,y+262,'Conditional group: the dashed-ring glyph (the same ring that marks a found body in the street) precedes the numbers that only happen if the body is found.','b',12,'Medium','pale-seafoam','lm')
    y+=330
    # worked lines
    text(cv,M,y+10,'THE LINES, WORKED (numbers are Fable\'s from the 013 screenshots)','d',12,'SemiBold','deep-teal','lm'); y+=36
    exs=[('Knock out','way_knock',[('p_time','7 s',False),('p_trail','+4',True),('p_alert','1',True),('eye_shut','',False)]),
         ('Kill','way_kill',[('p_time','4 s',False),('p_trail','+8',True),('p_alert','2',True),('eye_shut','',False)]),
         ('Ask','ask',[('p_time','25 s',False),('p_trail','+1',False),('mark_no','',False)]),
         ('Pay','pay',[('p_cash','90,000',False),('p_time','50 s',False),('p_trail','+2',False),('mark_ok','',False)]),
         ('Help','help',[('p_cash','40,000',False),('p_time','50 s',False),('p_trail','+1',False),('mark_half','',False)]),
         ('Pressure','pressure',[('p_time','35 s',False),('p_trail','+3',False),('mark_ok','',False)]),
         ('Fire','fire',[('p_trail','+2/hit',False),('p_alert','≥2',False)]),
         ('Watch','watch',[('p_time','12 s',False),('routine','',False),('cone','',False)]),
         ('Carry','s_carrying',[('p_time','5 s',False),('s_carrying','1.4 m/s',False),('eye_unk','',False)]),
         ('Enter','enter',[('p_time','9 s',False),('eye_open','',False),('mark_no','',False)])]
    col=0
    for i,(nm,gl,items) in enumerate(exs):
        cx=M+(i%2)*((CW-2*M)//2); cy=y+(i//2)*96
        card=nine('plate_light',(CW-2*M)//2-30,80); cv.alpha_composite(card,(int(cx),int(cy)))
        put_icon(cv,gl,cx+22,cy+22,36,INK); text(cv,cx+74,cy+40,nm,'d',14.5,'Bold','ink','lm')
        price(cv,cx+74+250,cy+40,items,ink=True,size=14.5,gap=26)
    y+=96*5+20
    # the dim, locked row and the hover chip
    text(cv,M,y+10,'LOCKED ROW, HOVER CHIP, TINTED CANCEL','d',12,'SemiBold','deep-teal','lm'); y+=36
    card=nine('plate_light',620,80); cv.alpha_composite(card,(M,y)); lk=Image.new('RGBA',card.size,(0,0,0,0)); 
    put_icon(cv,'lock',M+22,y+22,36,(23,48,47,150)); text(cv,M+74,y+40,'Loop','d',14.5,'Bold',(23,48,47,150),'lm'); price(cv,M+74+250,y+40,[('p_time','62 s',False),('p_trail','+1',False)],ink=True); d=ImageDraw.Draw(cv); ov=Image.new('RGBA',card.size,(240,236,226,110)); cv.alpha_composite(ov,(M,y))
    text(cv,M+660,y+40,'dimmed, with its price; the click gives the pop','b',12,'Medium','ink','lm')
    chip2(cv,M+60,y+240,'way_knock',[('p_time','7 s',False),('p_trail','+4',True),('p_alert','1',True),('eye_shut','',False)])
    text(cv,M+560,y+190,'the hover chip is the same line, led by the way\'s glyph','b',12,'Medium','ink','lm')
    # cancel
    for j,(costly,lab) in enumerate(((False,'free: plain'),(True,'costs something: tinted, dashed'))):
        cx=M+1150+j*420; cy=y+34
        mdisc(cv,cx,cy,'close_x',22,rim=(TQ if costly else None),dashed=costly,col=(TQL if costly else CREAM)); text(cv,cx+34,cy,lab,'b',12,'Medium','ink','lm')
    return y+300
# ------------------------------------------------------------------ 3 markers
PB=lambda bg: bg
def street(bgn='10',crop=(690,540,900,690)): return Image.open(f'bg/{bgn}.png').convert('RGB').crop(crop).resize(((crop[2]-crop[0])*2,(crop[3]-crop[1])*2),Image.LANCZOS).convert('RGBA')
def mk_tiles():
    T=[]
    def t_eye(c):
        for i,(st,w,x) in enumerate((('open','guard',70),('shut','guard',210),('unk','guard',350))):
            person(c,x,230,'guard',2.4); m_eye(c,x,120,st,w)
        person(c,420+60,230,'civ',2.4)
    T.append(('Eye mark','Over a watcher: open = looks at the act, shut = cannot, dashed "?" = not yet watched. The rim is whose eye it is: amber guard, red camera, violet rival. Over the target: shut = from behind.',t_eye))
    def t_wake(c):
        prone(c,60,215,'guard',1.5,-8); m_wake(c,60,150,1.0)
        prone(c,170,215,'guard',1.5,6); m_wake(c,170,150,0.5)
        prone(c,280,215,'guard',1.5,-4); m_wake(c,280,150,0.15,pulse=True)
        prone(c,390,215,'civ',1.5,8,dead=True); m_wake(c,390,150,0,dead=True)
        prone(c,500,215,'guard',1.5,-6); m_wake(c,500,150,0.6,tied=True,found=True)
    T.append(('Wake ring','The ring on a body empties to the wake; the last 15 s it doubles. Square plate = dead, no ring. Small second disc = tied. Dashed ring = a report about it has landed.',t_wake))
    def t_lock(c):
        d=ImageDraw.Draw(c)
        for x0,ghost in ((120,False),(380,True)):
            d.rectangle((x0-46,70,x0+46,200),fill=(120,140,138,255),outline=(60,90,88,255),width=4); d.rectangle((x0-30,90,x0+30,200),fill=(30,52,50,255))
            m_lock(c,x0,40 if not ghost else 40,frac=(0.7 if not ghost else None),ghost=ghost)
        person(c,250,235,'guard',2.4)
    T.append(('Lock on a held door','Appears when it happens, with its ring to the stand-down and tape at the foot. Hollow dashed lock = the preview ("would hold") while a way is hovered.',t_lock))
    def t_wit(c):
        person(c,90,235,'civ',2.4); m_witness(c,90,110,tx=330,ty=210,frac=0.6)
        person(c,450,235,'civ',2.4); m_witness(c,450,110,how='radio',frac=0.35)
        d=ImageDraw.Draw(c); d.rectangle((318,215,342,260),fill=(60,100,150,255))
    T.append(('"!" and the dotted line','A "!" over the witness with a ring to the landing, and a dotted line to the phone they walk to: take them down first to stop it. A radio has no line, nothing to stop.',t_wit))
    def t_pip(c):
        person(c,100,235,'guard',2.4); m_pip(c,100,120,0.8)
        person(c,260,235,'guard',2.4); m_pip(c,260,120,0.25,soon=True)
        person(c,420,235,'guard',2.4); m_unknown(c,420,120,'guard')
        person(c,560,235,'civ',2.4); m_unknown(c,560,120,'none')
    T.append(('Clock pip and "?"','Once his routine is learned: the wedge is the time to his next check-in; dashes pulse inside 10 s. Before: a dashed "?" over every watcher the Scout has not watched.',t_pip))
    def t_cone(c):
        ap,pts=wedge((120,230),-40,200,24); m_cone(c,ap,pts,'camera'); ap,pts=wedge((330,230),-30,200,24); m_cone(c,ap,pts,'guard')
        ap,pts=wedge((540,230),-20,200,24); m_cone(c,ap,pts,'rival'); ap,pts=wedge((740,230),-30,200,24); m_cone(c,ap,pts,'camera',looped=True)
        for x,k in ((120,'agent'),(330,'guard'),(540,'civ'),(740,'civ')): person(c,x,235,k,1.8)
    T.append(('Cones, as today','Red camera, amber guard, violet rival, grey when looped. Drawn unlit, outside the post grade, with the far edge dashed (shape cue). Learned ones only; Alt shows all.',t_cone))
    def t_obj(c):
        person(c,110,235,'vale',2.4); m_tri(c,110,95); ground_ring(c,380,215,'ring_ground_hint',160); d=ImageDraw.Draw(c); d.rounded_rectangle((350,170,410,215),radius=12,fill=(60,70,96,255)); m_tri(c,380,100)
        m_edge(c,560,150,0,badge_name='npc_councillor')
    T.append(('Objective marks','The target person gets an inverted triangle, a place gets a corona on the ground, and what is off screen gets an edge arrow. Turquoise: the one highlight, never a signal colour. They show with the line, at Alt, and when it changes.',t_obj))
    def t_stash(c):
        m_stash(c,70,100,1,0); m_stash(c,230,100,3,1); m_stash(c,420,100,3,0,hollow=True)
        person(c,520,235,'agent',2.4); mdisc(c,520,100,'s_carrying',24)
        m_badge(c,640,150,'signal_police',2); m_badge(c,720,150,'signal_guard',1)
    T.append(('Stashes, carry, units','Round plate with pips (filled = a body inside), hollow dashed when behind a building. A carry disc over the carrier. Unit badges merge within 2.5 m into one with a count; Alt splits them.',t_stash))
    return T
def s_markers(cv,y):
    y=bar(cv,y,3,'World markers that replace text (play zoom: actual size, then twice that)')
    T=mk_tiles()
    for i,(nm,why,fn) in enumerate(T):
        cy=y+i*400
        c=Image.open('bg/10.png').convert('RGB').crop((690,520,1100,700)).resize((820,360),Image.LANCZOS).convert('RGBA'); fn(c)
        a=c.resize((410,180),Image.LANCZOS); cv.paste(a,(M,cy+40)); cv.paste(c.convert('RGB'),(M+440,cy+40))
        d=ImageDraw.Draw(cv); d.rounded_rectangle((M-4,cy+36,M+414,cy+224),radius=6,outline=C['ink'],width=2)
        text(cv,M,cy+8,nm.upper(),'d',13,'SemiBold','deep-teal','lm'); text(cv,M+440,cy+8,'twice the size','b',11.5,'Medium','text-grey','lm')
        for j,l in enumerate(wrap(why,'b',13,'Medium',CW-M-1330-40)): text(cv,M+1330,cy+60+j*34,l,'b',13,'Medium','ink')
    return y+400*len(T)+10
# ------------------------------------------------------------------ 4 scope
def s_scope(cv,y):
    y=bar(cv,y,4,'The plot scope: where the persistent state lives')
    CELL=[(1,(30,-45),False),(2,(8,-49),False),(3,(24,-18),True),(4,(30,-47),False)]
    states=[('0:13 · the briefing told us where Vale will be, not where he is now',dict(cell=CELL,cones=CONES0,vale={'at':(76.25,-30),'age':0.95},view=(26,-30),people=[])),
            ('2:01 · the Scout has watched the guard and Inez; the Tech loops the entrance camera',dict(cell=[(1,(28,-33),False),(2,(8,-49),False),(3,(24,-18),False),(4,(44,-30),True)],cones=LOOPED0,vale={'at':(76.25,-30),'age':0.55},view=(48,-30),people=[('npc_waitress',(-10,-29),'n'),('guard',(51.75,-30),'guard')],route=[(26.6,-17),(40,-17),(48.6,-30)])),
            ('2:29 · a body, a held door, units on the way; an agent now sees Vale (exact)',dict(cell=[(1,(28,-33),False),(2,(70,-34),False),(3,(24,-18),False),(4,(54,-31),True)],cones=CONES0,vale={'at':(77.5,-31),'exact':True},view=(52,-30),people=[('guard',(51.75,-30),'guard')],locks=[(51.75,-30,0.8)],bodies=[(50,-31,False)],stash=[(46,-35)],reports=[(40,-38,30,-48)],units=[('police',(60,-20),150,2)]))]
    x=M
    for i,(lab,st) in enumerate(states):
        c=Image.new('RGBA',(212*2+40,132*2+40),(0,0,0,0)); scope(c,10,10,st=st); c=c.crop((0,0,212*2+40,132*2+40)); c=c.resize((int(c.width*1.35),int(c.height*1.35)),Image.LANCZOS)
        cv.alpha_composite(c,(x,y)); 
        for j,l in enumerate(wrap(lab,'b',12,'Medium',c.width-30)): text(cv,x+8,y+c.height+8+j*30,l,'b',12,'Medium','ink')
        x+=c.width+20
    y+=int((132*2+40)*1.35)+120
    legend=[('Size and corner','212 x 132 px at 1080p, bottom left, under the roster: 1.5% of the screen. Rotated to the camera\'s fixed yaw, so up on the scope is up on the street.'),
     ('Cell','Four numbered discs (F1 to F4); the selected one is ringed turquoise. Click one to select it, double click to centre.'),
     ('Told and learned','A blip shows only what the cell knows. Vale: a dashed ring round his last known place, growing with its age, with a "?" in it; an exact disc with his badge only while an agent sees him. A watcher not yet watched is not drawn.'),
     ('Watchers','Learned cones in their signal colours (red camera, amber guard, violet rival), grey when looped; a guard is an amber dot; the patrol\'s route is a dotted amber line once learned. Inez, the driver, Vale are their kit badges.'),
     ('Events','Held door = lock; body = knocked-out or dead disc; stash = diamond; a report in transit = "!" with its dotted line to the phone; units arriving = amber arrow from the edge they come from, with their badge.'),
     ('Camera and levels','A white trapezoid is what the main view shows. Three pips on the right say street, deck, sky concourse for the selected agent; the scope draws that level.'),
     ('Click','Left click or drag pans the main camera there; clicking a cell disc selects it. It gives no move orders: an order needs the street\'s price chip, the run price and the cones, and a map click would hide all three.'),
     ('Edge pointers','On the main view, a round arrow at the screen edge for Vale (turquoise) and for anything urgent (units arriving, amber, with a count). They show while the thing is off screen, and fade when it enters.')]
    cw=(CW-2*M)//2
    for i,(h,t) in enumerate(legend):
        cx=M+(i%2)*cw; 
    yy=y
    colh=[yy,yy]
    for i,(h,t) in enumerate(legend):
        col=i%2; cx=M+col*cw; text(cv,cx,colh[col],h.upper(),'d',12,'SemiBold','deep-teal','lm'); n=wrap(t,'b',12.5,'Medium',cw-40)
        for j,l in enumerate(n): text(cv,cx,colh[col]+34+j*30,l,'b',12.5,'Medium','ink')
        colh[col]+=34+len(n)*30+34
    # edge pointers on a street tile
    return max(colh)+10
CONES0=[('camera',(49.5,-41.5),(2.25,11.5),22,9,False),('camera',(74,-40),(4.2,9),18,9,False)]
LOOPED0=[('camera',(49.5,-41.5),(2.25,11.5),22,9,True),('camera',(74,-40),(4.2,9),18,9,False)]
# ------------------------------------------------------------------ 5 lines, beats, pops, loud card
def pop(cv,x,y,ic,txt,icon2=None):
    wd=tw(txt,'d',14,'Bold'); W=int(40+44+wd+34); panel(cv,'chip_price',x,y,W,84)
    put_icon(cv,ic,x+30,y+22,36,CREAM); text(cv,x+30+48,y+31,txt,'d',14,'Bold','cream','lm') if False else text(cv,x+30+48,y+40,txt,'d',14,'Bold','cream','lm'); return W
def s_lines(cv,y):
    y=bar(cv,y,5,'Lines in the street: objective, hint, beats, bubbles, refusals, the loud card')
    c=Image.open('bg/10.png').convert('RGB').crop((300,860,1100,1060)).resize((1600,400),Image.LANCZOS).convert('RGBA')
    goals=[('Objective, 0:00 and when it changes (6 s)',[('Take',None),('Vale',  'person'),('alive.',None)],None,None),
           ('Objective once the Enforcer holds him',[('Get',None),('Vale','person'),('to the',None),('car.','place')],None,None),
           ('Next Step hint: one imperative, six words at most',[('Watch the',None),('café terrace.','place')],'scout','watch'),
           ('',[('Reroute the',None),('patrol.','guard')],'tech','reroute'),
           ('',[('Give the',None),('rivals','rival'),('a false tip.',None)],'scout','listen')]
    yy=y
    cv.paste(c.convert('RGB'),(M,yy)); 
    for i,(lab,parts,ag,vb) in enumerate(goals):
        goal_line(c,400+0,40+i*70,parts,agent=ag,verb=vb) if False else None
    c2=c.copy(); 
    for i,(lab,parts,ag,vb) in enumerate(goals): goal_line(c2,400,10+i*38,parts,agent=ag,verb=vb)
    cv.paste(c2.convert('RGB'),(M,yy))
    x=M+1640
    bullets(cv,x,yy,CW-M-x,['The noun takes the colour of the marker it names, plus a tiny shape so colour is never alone: a triangle for a person, a ring for a place. Guard and patrol are amber, rivals violet, cameras red (signals stay reserved); everything the cell owns or goes to is turquoise.','The objective shows for 6 s, then fades. It returns at the 5:30 chime and the 5:50 exit, and when the clock pill is hovered. The hint shares its place: one line at a time.','The hint line carries the agent\'s portrait, pulsing, and the verb\'s icon: it names no one.'],size=12.5)
    yy+=420
    # beats
    text(cv,M,yy+8,'STORY BEATS (nine lines; at most two on screen, 4 s each; the speaker\'s bubble is the beat when in view)','d',12,'SemiBold','deep-teal','lm'); yy+=36
    BE=[('Rivals here.','signal_rival'),('Inez called the guard.','npc_waitress'),('Vale is out.','npc_councillor'),('Camera blind.','signal_camera'),('Reported.','call'),('Police coming.','signal_police'),('Robots coming.','signal_guard'),('District sealing.','lock'),('Body found.','body_out')]
    dk=Image.new('RGBA',(CW-2*M,260),(208,196,186,255)); 
    for i,(t,ic) in enumerate(BE): beat(dk,330+(i%3)*350,8+(i//3)*38,t,icon_=ic)
    cv.paste(dk,(M,yy)); yy+=280
    # bubbles
    text(cv,M,yy+8,'SPEECH BUBBLES AND REFUSAL POPS','d',12,'SemiBold','deep-teal','lm'); yy+=40
    bg=Image.open('bg/10.png').convert('RGB').crop((300,860,1100,1000)).resize((1600,280),Image.LANCZOS).convert('RGBA')
    person(bg,250,250,'civ',2.4); bubble(bg,250,150,'Quiet on the door?'); person(bg,640,250,'guard',2.4); r=bubble(bg,640,150,'The patrol comes round, then nothing.'); 
    d=ImageDraw.Draw(bg); d.rounded_rectangle((r[0]-6,r[1]-6,r[0]+r[2]+6,r[1]+r[3]+6),radius=26,outline=AMBER,width=5)
    person(bg,1080,250,'agent',2.4); bubble(bg,1080,150,'Close and quiet.'); d.rounded_rectangle((1080-120,150-52-18-6,1080+120,150-18+6),radius=26,outline=TQ,width=5) if False else None
    cv.paste(bg.convert('RGB'),(M,yy)); 
    x=M+1640
    bullets(cv,x,yy,CW-M-x,['A bubble sits on the speaker, one line, 3 s, two at once. Cream plate, ink text. The rim says who: civilians none, guards and police amber, rivals violet, the cell turquoise.','No name tag: the speaker is what it sits on. A speaker off screen has no bubble; the beat speaks instead.'],size=12.5)
    yy+=300
    POPS=[('s_moving','Too far · 4 m'),('watch','Watch first'),('listen','Listen first'),('pay','Short · $140,000'),('p_time','Too late · 5:50'),('lock','Held'),('s_carrying','Hands full'),('fire','Not drawn'),('loop','Tied to cabinet'),('mark_no','Never Vale')]
    xx=M; y2=yy
    for ic,t in POPS:
        w=pop(cv,xx,y2,ic,t) if not ic.startswith('mark') else pop(cv,xx,y2,'lock',t)
        xx+=w+24
        if xx>CW-M-300: xx=M; y2+=100
    yy=y2+110
    bullets(cv,M,yy,CW-2*M,['A refusal is one pop at the click: the family\'s glyph, at most four words, the number as a number, about 2 s. Families and their glyphs (Fable\'s table): reach s_moving · Watch first watch · Listen first listen · wrong agent that agent\'s cameo · money pay · a window p_time · guns fire · bodies s_carrying · doors lock or door_held · no otherwise a slashed circle. The target pulses as today.'],size=12.5)
    yy+=150
    # going loud
    text(cv,M,yy+8,'THE GOING-LOUD CARD, WITHOUT WORDS (the 0.5 s hold is the ring filling; no pause)','d',12,'SemiBold','deep-teal','lm'); yy+=40
    cx0=M; X,Y,Wd,Hd=cx0,yy,1180,330
    shadow(cv,(X,Y,X+Wd,Y+Hd),r=44,a=80,off=8); panel(cv,'panel_dark_modal',X,Y,Wd,Hd)
    items=[('p_alert','3',''),('signal_police','16 s',''),('signal_rival','40 s',''),('lock','8:30','seal'),('fire','+2',''),('way_kill','+2',''),('body_dead','+2','')]
    xx=X+60
    for ic,t,_ in items:
        if ic.startswith('signal'): badge(cv,ic,xx+22,Y+80,22)
        else: put_icon(cv,ic,xx,Y+54,52,CREAM)
        text(cv,xx+60,Y+80,t,'d',15,'Bold','cream','lm'); xx+=60+tw(t,'d',15,'Bold')+44
    for j,(nm,xp) in enumerate((('signal_guard',X+160),('npc_waitress',X+280))):
        badge(cv,nm,xp,Y+190,24); d=ImageDraw.Draw(cv); d.line([(xp-30,Y+160),(xp+30,Y+220)],fill=CREAM,width=6)
    put_icon(cv,'restrain',X+380,Y+164,52,CREAM); d=ImageDraw.Draw(cv); d.line([(X+370,Y+158),(X+440,Y+226)],fill=CREAM,width=6)
    # hold ring
    cx,cy=X+Wd-200,Y+190; d.ellipse((cx-60,cy-60,cx+60,cy+60),fill=INKD,outline=(247,244,236,120),width=5); d.arc((cx-60,cy-60,cx+60,cy+60),-90,150,fill=TQL,width=10); put_icon(cv,'fire',cx,cy,64,CREAM,center=True)
    x=X+Wd+50
    bullets(cv,x,yy,CW-M-x,['The same facts as today\'s card, as icons: loud, Alert 3, police in 16 s, rivals in 40 s, the seal, the allies lost (crossed), who is captured (crossed cuffs), Trail per shot, hit and down. The sentences are Alt.','Hold the fire key 0.5 s: the ring fills and the order goes. Release or Esc cancels.'],size=12.5)
    return yy+Hd+60
# ------------------------------------------------------------------ 6 alt
def s_alt(cv,y):
    y=bar(cv,y,6,'The Alt layer: full detail, held, never at rest')
    im=Image.open('after/12.png').convert('RGB'); iw=1500; ih=int(1142*iw/1726); cv.paste(im.resize((iw,ih),Image.LANCZOS),(M,y))
    x=M+iw+50
    bullets(cv,x,y,CW-M-x,['Hold Alt (or Tab-hold on a pad): it shows while held and nothing sticks.','In the world, every learned mark at once: cones, eye marks, clock pips, wake rings, found rings, locks, stash plates, call boxes, unit badges split; name tags (a name or a role, two words) on every person the cell knows; "?" on those it does not.','On a card: under each way, one line of icon-and-number fragments: units in 16 s, door held 1:10, wakes 4:07, report in 22 s. No sentence: a fragment is an icon and a number, at most five words.','On the pill: the next mark, the Intel count and the history key; on the roster: the health pips as numbers and each busy agent\'s seconds.','Long text is a second step: hover a row while Alt is held and the old sentence ("Slow and quiet. They wake after a while...") shows beside it, once, as a tooltip. That is where Fable\'s `line` and `does` live.','Budget: up to 30% of the screen; running text limited to fragments (a line of more than five words fails).'],size=12.5)
    return y+ih+60
def main():
    cv=Image.new('RGBA',(CW,16000),PAGE)
    text(cv,M,60,'Brief 014 A · Less reading: the visual grammar','d',28,'Bold','ink','lm')
    for i,l in enumerate(wrap('Glyph, 2026-10-16. Kit of rounds 4 v10 and 6b, the 013 glyphs, and eight new ones (eye open, shut, ?; found ring; "!"; car; run; arrive; pointer; routine; cone). Nothing new in style. Signals stay: cones red, guards and police amber, rivals violet; nothing else uses them. Sections 1 to 6; the before and after of the eleven 013 moments is the other sheet (less-moments.png).','b',13,'Medium',CW-2*M)): text(cv,M,110+i*34,l,'b',13,'Medium','ink','lm')
    y=190
    for fn in (s_rest,s_price,s_markers,s_scope,s_lines,s_alt): y=fn(cv,y)
    cv=cv.crop((0,0,CW,y)); cv.convert('RGB').save(R+'/briefs/014/glyph/less-grammar.png'); print(cv.size)
main()
