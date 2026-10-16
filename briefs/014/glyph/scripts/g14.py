# Glyph, brief 014 A: pieces for the "less reading" mock-ups. Scratch; reads the 011 kit and the 013 glyphs. 2x pixel coordinates unless a name says 1x.
import sys, os, math, json
sys.path.insert(0,'/Users/sky/Projects/suborned/out/glyph013')
os.chdir('/Users/sky/Projects/suborned/out/glyph014')
from hud import *
import lib
R='/Users/sky/Projects/suborned'
G14=R+'/out/glyph014/glyphs'; os.makedirs(G14,exist_ok=True)
# ---------------------------------------------------------------- new glyphs (kit monoline, white, 32 box)
def pts_curve(f,x0=3,x1=29,n=24): return [(x0+(x1-x0)*i/n, f(i/n)) for i in range(n+1)]
def g_eye_open(d,k):
    up=pts_curve(lambda t:16-8.5*math.sin(math.pi*t)); dn=pts_curve(lambda t:16+8.5*math.sin(math.pi*t))
    line(d,k,up,2.8); line(d,k,dn,2.8); circ(d,k,16,16,4.4,fill=True)
def g_eye_shut(d,k):
    line(d,k,pts_curve(lambda t:11+8*math.sin(math.pi*t)),3.0)
    for t in (0.2,0.5,0.8):
        x=3+26*t; y=11+8*math.sin(math.pi*t); line(d,k,[(x,y),(x+(t-0.5)*5,y+5.2)],2.6)
def g_eye_unk(d,k):
    up=pts_curve(lambda t:16-9*math.sin(math.pi*t)); dn=pts_curve(lambda t:16+9*math.sin(math.pi*t))
    line(d,k,up,2.6); line(d,k,dn,2.6)
    arc(d,k,16,14.2,3.3,200,60,2.6); line(d,k,[(17.6,16.6),(16,18.4)],2.6); circ(d,k,16,21.2,1.4,fill=True)
def g_bang(d,k): line(d,k,[(16,4),(16,19)],4.4); circ(d,k,16,26,2.8,fill=True)
def g_car(d,k):
    line(d,k,[(3,22),(3,16),(8,15),(11,9),(22,9),(25.5,15),(29,16),(29,22),(3,22)],2.8); line(d,k,[(11,15),(25.5,15)],2.4)
    circ(d,k,9.5,23,3.4,fill=True); circ(d,k,23,23,3.4,fill=True)
def g_run(d,k):
    circ(d,k,20,6,3.2,fill=True); line(d,k,[(19,10),(14.5,18.5)],3.4); line(d,k,[(18,11.5),(25,14.5)],3.0); line(d,k,[(17.5,12),(11.5,12.5)],3.0)
    line(d,k,[(14.5,18.5),(20.5,23),(18.5,29)],3.2); line(d,k,[(14.5,18.5),(9,22),(5,21.5)],3.2); line(d,k,[(3,9),(8,9)],2.2); line(d,k,[(2,14),(7,14)],2.2)
def g_routine(d,k):
    circ(d,k,14,16,10.5,3.0); line(d,k,[(14,9),(14,16),(19,19)],2.8); arc(d,k,14,16,14.5,-60,40,2.4); line(d,k,[(24,5),(27.5,9.5),(21,10)],2.4)
def g_cone(d,k):
    line(d,k,[(5,16),(27,6)],2.8); line(d,k,[(5,16),(27,26)],2.8); arc(d,k,5,16,24,-24,24,2.8); circ(d,k,5,16,2.6,fill=True)
def g_arrive(d,k): line(d,k,[(4,16),(24,16)],3.4); line(d,k,[(17,8.5),(25,16),(17,23.5)],3.4); line(d,k,[(28,5),(28,27)],3.0)
def g_pointer(d,k): line(d,k,[(5,16),(25,16)],4.0); line(d,k,[(16,7),(26,16),(16,25)],4.0)
def g_found(d,k):
    for i in range(10):
        a0=36*i; arc(d,k,16,16,12.5,a0,a0+20,2.8)
    line(d,k,[(9.5,17.5),(20.5,17.5)],4.0); circ(d,k,22.6,15.4,2.4,fill=True)
GL14={'routine':g_routine,'cone':g_cone,'found':g_found,'eye_open':g_eye_open,'eye_shut':g_eye_shut,'eye_unk':g_eye_unk,'bang':g_bang,'car':g_car,'run':g_run,'arrive':g_arrive,'pointer':g_pointer}
for n,fn in GL14.items():
    im=lib._g(fn); im.resize((64,64),Image.LANCZOS).save(f'{G14}/{n}@2x.png'); im.resize((32,32),Image.LANCZOS).save(f'{G14}/{n}@1x.png')
_oldicon=lib.icon
def icon14(name):
    f=f'{G14}/{name}@2x.png'
    return Image.open(f).convert('RGBA') if os.path.exists(f) else _oldicon(name)
lib.icon=icon14; icon=icon14
# colours: signals reserved
RED=(224,58,47,255); AMBER=(240,160,32,255); VIOLET=(138,79,208,255); CREAM=C['cream']; INK=C['ink']; INKD=C['ink-deep']; TQ=C['turquoise']; TQL=C['turquoise-light']
SIG={'camera':RED,'guard':AMBER,'rival':VIOLET,'none':CREAM}
def ds(cv,x,y,w,h,r=14,a=70,off=4): shadow(cv,(x,y,x+w,y+h),r=r,a=a,off=off)
# ---------------------------------------------------------------- world markers (over a person: at most two; 22 px at 1x)
def mdisc(cv,cx,cy,glyph,size=22,rim=None,dashed=False,col=None,fill=None,sq=False):
    s=size*2; d=ImageDraw.Draw(cv)
    if rim is not None:
        if dashed: dashed_ring(cv,cx,cy,s/2+5,s/2+5,rim,n=18,w=5,duty=0.6)
        else: d.ellipse((cx-s/2-5,cy-s/2-5,cx+s/2+5,cy+s/2+5),outline=rim,width=5)
    if sq: d.rounded_rectangle((cx-s/2,cy-s/2,cx+s/2,cy+s/2),radius=int(s*0.22),fill=fill or INKD,outline=CREAM,width=3)
    else: d.ellipse((cx-s/2,cy-s/2,cx+s/2,cy+s/2),fill=fill or INKD,outline=(247,244,236,235),width=3)
    if glyph: put_icon(cv,glyph,cx,cy,s*0.66,col or CREAM,center=True)
def m_eye(cv,cx,cy,state,watcher='guard',size=22):
    """eye over a watcher or a target. rim = whose eye it is (signal colour); shape: open / shut / dashed-rim ?"""
    g={'open':'eye_open','shut':'eye_shut','unk':'eye_unk'}[state]
    mdisc(cv,cx,cy,g,size,rim=SIG[watcher],dashed=(state=='unk'))
def m_unknown(cv,cx,cy,watcher='guard',size=22): mdisc(cv,cx,cy,'p_unknown',size,rim=SIG[watcher],dashed=True)
def m_wake(cv,cx,cy,frac,pulse=False,size=20,tied=False,dead=False,found=False):
    """a body: the ring EMPTIES to the wake. dead = square plate, no ring."""
    if dead: mdisc(cv,cx,cy,'body_dead',size,sq=True)
    else:
        r=size+9; d=ImageDraw.Draw(cv)
        d.arc((cx-r,cy-r,cx+r,cy+r),0,360,fill=(247,244,236,110),width=5)
        if frac>0: d.arc((cx-r,cy-r,cx+r,cy+r),-90,-90+360*frac,fill=TQL,width=6)
        if pulse: d.arc((cx-r-8,cy-r-8,cx+r+8,cy+r+8),0,360,fill=(96,224,234,150),width=3)
        mdisc(cv,cx,cy,'body_out',size)
    if tied: mdisc(cv,cx+size+8,cy-size+4,'restrain',13)
    if found: dashed_ring(cv,cx,cy,size+22,size+22,CREAM,n=26,w=4)
def m_lock(cv,cx,cy,frac=None,ghost=False,size=24,tape=True):
    """lock on a held door; ring counts to the stand-down; ghost = the preview (hollow, dashed)."""
    d=ImageDraw.Draw(cv); s=size*2
    if ghost:
        dashed_ring(cv,cx,cy,s/2+2,s/2+2,CREAM,n=20,w=4,duty=0.55)
        d.ellipse((cx-s/2+4,cy-s/2+4,cx+s/2-4,cy+s/2-4),fill=(11,22,21,110)); put_icon(cv,'lock',cx,cy,s*0.6,(247,244,236,210),center=True); return
    if tape:
        st=spr('stripes_warn'); st=st.crop((0,int(st.height*0.38),st.width,int(st.height*0.62))); tw_=int(s*2.0); sc=tw_/st.width; im=st.resize((tw_,max(8,int(st.height*sc*0.6))),Image.LANCZOS); cv.paste(im,(int(cx-tw_/2),int(cy+s/2+8)),im)
    if frac is not None:
        r=s/2+9; d.arc((cx-r,cy-r,cx+r,cy+r),0,360,fill=(247,244,236,110),width=5); d.arc((cx-r,cy-r,cx+r,cy+r),-90,-90+360*frac,fill=TQL,width=6)
    mdisc(cv,cx,cy,'lock',size)
def m_witness(cv,cx,cy,tx=None,ty=None,frac=0.6,how='call'):
    """a report on its way: "!" over the witness, a dotted line to the phone they walk to; radio = no line."""
    if tx is not None:
        pts=[(cx,cy+34),((cx+tx)/2+10,(cy+ty)/2+26),(tx,ty)]
        dotted(cv,[(cx,cy+34),(tx,ty)],CREAM,step=18,r=4)
        mdisc(cv,tx,ty,'call' if how=='call' else 'radio',20)
    r=34; d=ImageDraw.Draw(cv); d.arc((cx-r,cy-r,cx+r,cy+r),0,360,fill=(247,244,236,110),width=5); d.arc((cx-r,cy-r,cx+r,cy+r),-90,-90+360*frac,fill=TQL,width=6)
    mdisc(cv,cx,cy,'bang',22)
def m_pip(cv,cx,cy,frac,size=18,soon=False):
    """clock pip: a learned check-in. the wedge is the time left to it. amber rim = a guard's."""
    s=size*2; d=ImageDraw.Draw(cv)
    d.ellipse((cx-s/2-5,cy-s/2-5,cx+s/2+5,cy+s/2+5),outline=AMBER,width=5); d.ellipse((cx-s/2,cy-s/2,cx+s/2,cy+s/2),fill=INKD)
    d.pieslice((cx-s/2+5,cy-s/2+5,cx+s/2-5,cy+s/2-5),-90,-90+360*frac,fill=CREAM)
    if soon: dashed_ring(cv,cx,cy,s/2+13,s/2+13,CREAM,n=16,w=3)
def m_tri(cv,cx,cy,size=14):
    s=size*2; d=ImageDraw.Draw(cv); pts=[(cx-s/2,cy-s*0.45),(cx+s/2,cy-s*0.45),(cx,cy+s*0.55)]
    d.polygon([(x,y+3) for x,y in pts],fill=(11,22,21,90)); d.polygon(pts,fill=TQL,outline=INKD)
    d.line(pts+[pts[0]],fill=INKD,width=3,joint='curve')
def m_corona(cv,cx,cy,w=150):
    ground_ring(cv,cx,cy,'ring_ground_hint',w)
def m_stash(cv,cx,cy,pips,filled=0,hollow=False,size=18):
    s=size*2; d=ImageDraw.Draw(cv)
    if hollow: dashed_ring(cv,cx,cy,s/2+3,s/2+3,CREAM,n=18,w=4); d.ellipse((cx-s/2,cy-s/2,cx+s/2,cy+s/2),fill=(11,22,21,150))
    else: d.ellipse((cx-s/2,cy-s/2,cx+s/2,cy+s/2),fill=INKD,outline=CREAM,width=3)
    put_icon(cv,'stash',cx,cy,s*0.62,CREAM,center=True)
    for i in range(pips):
        px=cx+s/2+14+i*17; py=cy
        if i<filled: d.ellipse((px-6,py-6,px+6,py+6),fill=CREAM)
        else: d.ellipse((px-6,py-6,px+6,py+6),outline=CREAM,width=3)
def m_badge(cv,cx,cy,name,count=1,size=22):
    badge(cv,name,cx,cy,size)
    if count>1:
        d=ImageDraw.Draw(cv); d.ellipse((cx+size-12,cy-size-6,cx+size+16,cy-size+22),fill=INKD,outline=CREAM,width=2); text(cv,cx+size+2,cy-size+8,str(count),'d',9.5,'Bold','cream','mm')
def m_cone(cv,apex,pts,kind='camera',looped=False,fill_a=64):
    col=SIG[kind][:3]
    cone(cv,apex,pts,col=col,fill_a=fill_a,looped=looped)
def wedge(apex,ang_deg,reach,half_deg):
    a0=math.radians(ang_deg-half_deg); a1=math.radians(ang_deg+half_deg); n=8
    return apex,[(apex[0]+reach*math.cos(a0+(a1-a0)*i/n),apex[1]+reach*math.sin(a0+(a1-a0)*i/n)) for i in range(n+1)]
def m_progress_ring(cv,cx,cy,frac,r=36):
    d=ImageDraw.Draw(cv); d.arc((cx-r,cy-r,cx+r,cy+r),0,360,fill=(247,244,236,110),width=5); d.arc((cx-r,cy-r,cx+r,cy+r),-90,-90+360*frac,fill=TQL,width=7)
def m_edge(cv,cx,cy,ang,glyph=None,badge_name=None,rim=TQL,count=0):
    """edge pointer: a disc at the screen edge with a pointing chevron; ang in degrees (0 = right)"""
    d=ImageDraw.Draw(cv); r=28
    d.ellipse((cx-r,cy-r,cx+r,cy+r),fill=INKD,outline=rim,width=5)
    a=math.radians(ang); tipx,tipy=cx+(r+14)*math.cos(a),cy+(r+14)*math.sin(a); b1=(cx+(r-2)*math.cos(a+0.55),cy+(r-2)*math.sin(a+0.55)); b2=(cx+(r-2)*math.cos(a-0.55),cy+(r-2)*math.sin(a-0.55))
    d.polygon([(tipx,tipy),b1,b2],fill=rim)
    if badge_name: badge(cv,badge_name,cx,cy,15)
    elif glyph: put_icon(cv,glyph,cx,cy,30,CREAM,center=True)
    if count: text(cv,cx+r+6,cy+r-6,str(count),'d',11,'Bold','cream','mm')
def m_refuse(cv,x,y,reason='lock',words='Held'):
    """a refusal at the click: the reason glyph, at most four words, the number as a number. Tail at (x,y). ~2 s."""
    wd=tw(words,'d',14,'Bold'); W=int(46+40+wd+30); H=64
    shadow(cv,(x+4,y-H+14,x+4+W,y+8),r=22,a=70,off=6); panel(cv,'chip_price',x+4,y-H-8,W,H+24)
    put_icon(cv,reason,x+30,y-H+12,34,CREAM); mk=spr('mark_no').resize((34,34),Image.LANCZOS); cv.paste(mk,(int(x+30+40),int(y-H+12)),mk); text(cv,x+30+40+44,y-H+29,words,'d',14,'Bold','cream','lm')
# ---------------------------------------------------------------- price grammar
def cap(cv,x,y,w,h,col=CREAM):
    """the dashed capsule: this number only happens if the body is found"""
    dashed_ring(cv,x+w/2,y+h/2,w/2,h/2,col,n=max(10,int(w/9)),w=3,duty=0.55)
def price(cv,x,cy,items,ink=False,size=14.5,gap=22):
    """one line: [icon] n ... items=(icon,text,cond). cond=True: only if the body is FOUND; those items follow one dashed-ring glyph (the world's "found" ring). Eye = a round badge. returns right edge (2x)."""
    col=INK if ink else CREAM; prev=False
    for ic,t,cnd in items:
        if cnd and not prev: put_icon(cv,'found',x,cy-16,32,col); x+=32+10
        prev=cnd
        w0=tw(t,'d',size,'Bold') if t else 0; isz=38 if ic.startswith('eye') else 32; wi=isz+(8 if t else 0)+w0
        if ic.startswith('mark_'):
            m=spr(ic).resize((36,36),Image.LANCZOS); cv.paste(m,(int(x),int(cy-18)),m); wi=36
        elif ic.startswith('eye'):
            dd=ImageDraw.Draw(cv); r=isz/2+2; ex,ey=x+isz/2,cy
            dd.ellipse((ex-r,ey-r,ex+r,ey+r),fill=INKD,outline=(247,244,236,235),width=3) if not ic=='eye_unk' else (dd.ellipse((ex-r,ey-r,ex+r,ey+r),fill=INKD),dashed_ring(cv,ex,ey,r,r,CREAM,n=14,w=3))
            put_icon(cv,ic,ex,ey,isz*0.9,RED if ic=='eye_open' else CREAM,center=True); wi=int(2*r)-isz+wi
        else: put_icon(cv,ic,x,cy-isz/2,isz,col)
        if t: text(cv,x+isz+8,cy,t,'d',size,'Bold','ink' if ink else 'cream','lm')
        x+=wi+gap
    return x
def chip2(cv,x,y,lead,items):
    """hover chip, tail bottom-left at (x,y) = the pointer. lead = the order's glyph."""
    w=0
    pv=False
    for ic,t,cnd in items: w+=(42 if cnd and not pv else 0)+(42 if ic.startswith('eye') else 32)+(8 if t else 0)+(tw(t,'d',14.5,'Bold') if t else 0)+22; pv=cnd
    W=int(40+(46 if lead else 0)+w+22); H=84; x0=x+4; y0=y-H+12
    shadow(cv,(x0,y0+6,x0+W,y0+H-24),r=18,a=70,off=8); panel(cv,'chip_price',x0,y0,W,H)
    cx=x0+34
    if lead: put_icon(cv,lead,cx,y0+31-20,40,CREAM); cx+=50
    price(cv,cx,y0+31,items); return x0+W
# ---------------------------------------------------------------- HUD pieces
def paste_roster(cv,shot,sel_y=None,scale=0.8,x=14,y=92):
    """the kit's roster column, cut from the play shot (same art), scaled. Rounded mask."""
    box=(14,92,127,528); im=shot.crop(box).convert('RGBA'); m=Image.new('L',im.size,0); ImageDraw.Draw(m).rounded_rectangle((0,0,im.width-1,im.height-1),radius=20,fill=255); im.putalpha(m)
    im=im.resize((int(im.width*2*scale),int(im.height*2*scale)),Image.LANCZOS); cv.alpha_composite(im,(int(x*2),int(y*2))); return im.size
def act_chip(cv,x,y,glyph,frac=None,label='',w=96,h=24):
    """what a busy agent is doing: one glyph, a bar, a number. 1x coordinates; no words."""
    X,Y,W,H=x*2,y*2,w*2,h*2; shadow(cv,(X,Y,X+W,Y+H),r=22,a=60,off=4); panel(cv,'plate_dark',X,Y,W,H)
    put_icon(cv,glyph,X+16,Y+8,H-16,CREAM)
    bx=X+16+H-8; d=ImageDraw.Draw(cv)
    if frac is not None:
        bw=W-(bx-X)-(10+(tw(label,'d',12,'Bold') if label else 0))-20
        d.rounded_rectangle((bx,Y+H/2-6,bx+bw,Y+H/2+6),radius=6,fill=(36,68,66,255)); d.rounded_rectangle((bx+2,Y+H/2-4,bx+2+max(4,(bw-4)*frac),Y+H/2+4),radius=4,fill=TQL)
        if label: text(cv,bx+bw+12,Y+H/2,label,'d',12,'Bold','cream','lm')
    elif label: text(cv,bx,Y+H/2,label,'d',12,'Bold','cream','lm')
def status(cv,x,y,clock,cash,trail,alert,paused=False,nxt=None,resp=None):
    """the slim pill at rest: clock, $, T, Alert lamps. Pause is a glyph. Next event only inside 60 s. 1x coords. returns 2x (w,h)"""
    pass
    nxt=nxt or '?  3:10'
    X=x*2; H=40*2; W=int(52+tw(clock,'d',19,'Bold')+(60 if paused else 0)+(36+tw(nxt,'d',14,'Bold')+34 if nxt else 0)+34+tw(cash,'d',14,'Bold')+34+34+tw(str(trail),'d',14,'Bold')+34+3*34+64)
    Y=y*2; shadow(cv,(X,Y,X+W,Y+H),r=36,a=60,off=5); panel(cv,'pill_status',X,Y,W,H)
    cx=X+54; text(cv,cx,Y+H/2,clock,'d',19,'Bold','cream','lm'); cx+=tw(clock,'d',19,'Bold')+30
    if paused: mdisc(cv,cx+20,Y+H/2,'pause',20,col=TQL); cx+=60
    if nxt: put_icon(cv,'nextevent',cx,Y+H/2-15,30,TQL); text(cv,cx+36,Y+H/2,nxt,'d',14,'Bold','cream','lm'); cx+=36+tw(nxt,'d',14,'Bold')+34
    cx+=10; put_icon(cv,'p_cash',cx,Y+H/2-15,30,CREAM); text(cv,cx+36,Y+H/2,cash,'d',14,'Bold','cream','lm'); cx+=36+tw(cash,'d',14,'Bold')+22
    put_icon(cv,'p_trail',cx,Y+H/2-15,30,CREAM); text(cv,cx+36,Y+H/2,str(trail),'d',14,'Bold','cream','lm'); cx+=36+tw(str(trail),'d',14,'Bold')+22
    d=ImageDraw.Draw(cv)
    for i in range(3):
        px=cx+14+i*34
        if i<alert: d.ellipse((px-10,Y+H/2-10,px+10,Y+H/2+10),fill=CREAM)
        else: d.ellipse((px-10,Y+H/2-10,px+10,Y+H/2+10),outline=CREAM,width=3)
    return W,H
def corner_keys(cv,x,y,intel='3/4',unread=False):
    """two small round keys at the end of the pill: people known (I) and history (H). 1x."""
    X,Y=x*2,y*2
    mdisc(cv,X+24,Y+24,'unknown',22); text(cv,X+24+26,Y+24,str(intel),'d',12,'Bold','cream','lm')
    mdisc(cv,X+24+78,Y+24,'papers',22)
    if unread: d=ImageDraw.Draw(cv); d.ellipse((X+24+78+14,Y+4,X+24+78+26,Y+16),fill=TQ)
def resp_pill(cv,x,y,police,guards,coming):
    """the response, in icons: officers, robots, how many are still coming. 1x"""
    X,Y=x*2,y*2; W=216; H=36
    shadow(cv,(X,Y,X+W*1.0,Y+H*2),r=30,a=60,off=4); panel(cv,'pill_status',X,Y,W+90,H*2-4)
    cx=X+34; cy=Y+H-2
    for nm,n in (('signal_police',police),('signal_guard',guards)):
        badge(cv,nm,cx+14,cy,14); text(cv,cx+38,cy,str(n),'d',14,'Bold','cream','lm'); cx+=84
    put_icon(cv,'arrive',cx-8,cy-15,30,CREAM); text(cv,cx+30,cy,str(coming),'d',14,'Bold','cream','lm')
def beat(cv,xr,y,s,kind='icon',icon_='papers',w=None):
    """a story beat: one short line, right edge at xr (1x). returns 2x height"""
    t=tw(s,'b',14,'SemiBold'); W=int(t+96); H=68; X=xr*2-W; Y=y*2
    shadow(cv,(X,Y,X+W,Y+H),r=26,a=60,off=4); panel(cv,'plate_dark',X,Y,W,H)
    (badge(cv,icon_,X+38,Y+H/2,17) if icon_.startswith(('signal','npc')) else mdisc(cv,X+38,Y+H/2,icon_,20)); text(cv,X+68,Y+H/2,s,'b',14,'SemiBold','cream','lm'); return H,(X,Y,W,H)
def speech(cv,cx,y,agent,s):
    """a cell line, bottom centre: one line, a cameo. 1x"""
    t=tw(s,'b',14.5,'SemiBold'); W=int(t+112); H=60; X=cx*2-W/2; Y=y*2
    shadow(cv,(X,Y,X+W,Y+H),r=26,a=60,off=4); panel(cv,'plate_dark',X,Y,W,H)
    cm=spr(f'cameo_{agent}_normal').resize((44,44),Image.LANCZOS); cv.paste(cm,(int(X+16),int(Y+8)),cm); text(cv,X+72,Y+H/2,s,'b',14.5,'SemiBold','cream','lm'); return (X,Y,W,H)
def bubble(cv,x,y,s,tail='down',size=13.5):
    """speech in the world: a cream plate over the speaker, one line; x,y = tail tip (2x)"""
    t=tw(s,'b',size,'SemiBold'); W=int(t+44); H=52; X=x-W/2; Y=y-H-18
    shadow(cv,(X,Y,X+W,Y+H),r=22,a=60,off=4); panel(cv,'plate_light',X,Y,W,H)
    d=ImageDraw.Draw(cv); d.polygon([(x-10,Y+H-2),(x+10,Y+H-2),(x,Y+H+16)],fill=C['card-ivory'] if 'card-ivory' in C else (252,228,204,255))
    text(cv,X+W/2,Y+H/2,s,'b',size,'SemiBold','ink','mm'); return (X,Y,W,H)
NOUN={'person':TQL,'place':TQL,'guard':AMBER,'rival':VIOLET,'camera':RED}
def goal_line(cv,cx,y,parts,agent=None,verb=None,size=15):
    """the objective, or the Next Step hint: ONE imperative, <= 6 words, one bracketed noun per marker. The noun takes its marker's colour and a tiny shape (triangle = person, ring = place), so colour is never alone. 1x centre x, top y. returns 2x box"""
    wsp=tw(' ','b',size,'SemiBold'); wid=40+(56 if agent else 0)+(40 if verb else 0)
    for t,k in parts: wid+=tw(t,'b',size,'SemiBold')+(26 if k else 0)+wsp
    H=60; X=cx*2-wid/2; Y=y*2; shadow(cv,(X,Y,X+wid,Y+H),r=26,a=60,off=4); panel(cv,'plate_dark',X,Y,wid,H)
    x=X+24
    if agent: cm=spr(f'cameo_{agent}_normal').resize((44,44),Image.LANCZOS); cv.paste(cm,(int(x-4),int(Y+8)),cm); dashed_ring(cv,x+18,Y+30,26,26,TQL,n=16,w=4); x+=52
    if verb: put_icon(cv,verb,x,Y+13,34,TQL); x+=44
    d=ImageDraw.Draw(cv)
    for t,k in parts:
        col=NOUN.get(k) if k else None
        if k in ('person','guard','rival','camera'): d.polygon([(x,Y+22),(x+18,Y+22),(x+9,Y+38)],fill=col,outline=INKD); x+=26
        elif k=='place': d.ellipse((x,Y+21,x+18,Y+39),outline=col,width=4); x+=26
        text(cv,x,Y+H/2,t,'b',size,'SemiBold',col if col else 'cream','lm'); x+=tw(t,'b',size,'SemiBold')+wsp
    return (X,Y,wid,H)
def verb_plate2(cv,x,y,knobs,armed=None,armed_name=None,way=None):
    verb_plate(cv,x*2,y*2,knobs,armed,armed_name,way)
# ---------------------------------------------------------------- the plot scope (minimap)
BLOCK=json.load(open(R+'/contracts/world/civic-block.json'))
_TH=math.radians(11); _c,_s=math.cos(_TH),math.sin(_TH)
def _uv(bx,by): return (bx*_c-by*_s, bx*_s+by*_c)
_X0,_X1,_Y0,_Y1=-24,84,-52,-12
_cor=[_uv(a,b) for a in (_X0,_X1) for b in (_Y0,_Y1)]
_umin,_umax=min(p[0] for p in _cor),max(p[0] for p in _cor); _vmin,_vmax=min(p[1] for p in _cor),max(p[1] for p in _cor)
def scope(cv,x,y,w=212,h=132,st=None):
    """plot scope, 1x box (x,y,w,h), drawn at 2x. Only what the cell knows: told and learned apply (a ? and an age ring where it does not know)."""
    st=st or {}; X,Y,W,H=x*2,y*2,w*2,h*2; pad=16
    shadow(cv,(X,Y,X+W,Y+H),r=36,a=80,off=6); panel(cv,'panel_dark',X,Y,W,H)
    gx0,gy0,gw,gh=X+pad,Y+pad,W-2*pad,H-2*pad
    glass=Image.new('RGBA',(gw,gh),(0,0,0,0)); g=ImageDraw.Draw(glass); g.rounded_rectangle((0,0,gw-1,gh-1),radius=22,fill=(8,18,17,255))
    sc=min(gw/(_umax-_umin),gh/(_vmax-_vmin))*1.0; ox=(gw-(_umax-_umin)*sc)/2-_umin*sc; oy=(gh-(_vmax-_vmin)*sc)/2-_vmin*sc
    P=lambda bx,by:(lambda u,v:(u*sc+ox,v*sc+oy))(*_uv(bx,by))
    for i in range(1,6): g.line([(0,gh*i/6),(gw,gh*i/6)],fill=(36,68,66,70),width=1)
    for i in range(1,9): g.line([(gw*i/9,0),(gw*i/9,gh)],fill=(36,68,66,70),width=1)
    g.polygon([P(_X0,_Y0),P(_X1,_Y0),P(_X1,_Y1),P(_X0,_Y1)],fill=(30,58,56,255))              # the block's ground
    for b in BLOCK['buildings']:
        g.polygon([P(*p) for p in b['polygon']],fill=(66,120,112,255),outline=(155,207,194,255))
    for s_ in BLOCK['structures']:
        if s_['category'] in ('boarding-canopy','civic-monument'): g.polygon([P(*p) for p in s_['polygon']],fill=(110,150,142,255))
    for o in BLOCK['objects']:
        if o['kind']=='fountain': g.polygon([P(*p) for p in o['polygon']],outline=(155,207,194,255),fill=(36,84,86,255))
    for b in BLOCK['bays']:
        pp=[P(*p) for p in b['polygon']]; g.line(pp+[pp[0]],fill=(155,207,194,150),width=2)
    # level pips (street 0, deck, concourse): only the selected agent's level is full
    lv=st.get('level',0)
    for i in range(3):
        cx,cy=gw-18,gh-18-i*18
        if i==lv: g.ellipse((cx-5,cy-5,cx+5,cy+5),fill=(247,244,236,255))
        else: g.ellipse((cx-5,cy-5,cx+5,cy+5),outline=(247,244,236,200),width=2)
    cv.alpha_composite(glass,(gx0,gy0))
    def Q(bx,by): px,py=P(bx,by); return (gx0+px,gy0+py)
    ov=Image.new('RGBA',cv.size,(0,0,0,0)); d=ImageDraw.Draw(ov)
    # learned cones (red camera, amber guard, violet rival); greyed when looped
    for kind,apex,dirv,reach,half,looped in st.get('cones',[]):
        a=math.atan2(dirv[1],dirv[0]); ha=math.atan(half/reach); pts=[Q(apex[0]+reach*math.cos(a-ha+2*ha*i/8),apex[1]+reach*math.sin(a-ha+2*ha*i/8)) for i in range(9)]
        col=(150,150,150) if looped else SIG[kind][:3]; poly=[Q(*apex)]+pts
        d.polygon(poly,fill=col+(95,)); d.line(poly+[poly[0]],fill=col+(255,),width=3)
    if st.get('route'): 
        pts=[Q(*p) for p in st['route']]
        for a,b in zip(pts,pts[1:]):
            n=max(1,int(math.hypot(b[0]-a[0],b[1]-a[1])/12))
            for i in range(n): t=i/n; d.ellipse((a[0]+(b[0]-a[0])*t-3,a[1]+(b[1]-a[1])*t-3,a[0]+(b[0]-a[0])*t+3,a[1]+(b[1]-a[1])*t+3),fill=AMBER)
    cv.alpha_composite(ov)
    # held doors, bodies, stashes, reports
    for bx,by,frac in st.get('locks',[]): px,py=Q(bx,by); mdisc(cv,px,py,'lock',11)
    for bx,by,dead in st.get('bodies',[]): px,py=Q(bx,by); mdisc(cv,px,py,'body_dead' if dead else 'body_out',11,sq=dead)
    for bx,by in st.get('stash',[]): px,py=Q(bx,by); dd=ImageDraw.Draw(cv); dd.polygon([(px,py-9),(px+9,py),(px,py+9),(px-9,py)],outline=CREAM,fill=INKD,width=2)
    for bx,by,tx,ty in st.get('reports',[]):
        a=Q(bx,by); b=Q(tx,ty); dotted(cv,[a,b],CREAM,step=11,r=2); mdisc(cv,a[0],a[1],'bang',11)
    # people the cell knows: Vale's last known place (ring grows with age; "?" where unknown), key people as blips
    v=st.get('vale')
    if v:
        px,py=Q(v['at'][0],v['at'][1]); age=v.get('age',0.3)
        dashed_ring(cv,px,py,22+age*26,22+age*26,TQL,n=16,w=4)
        if v.get('exact'): mdisc(cv,px,py,None,13,rim=TQL); badge(cv,'npc_councillor',px,py,10)
        else: mdisc(cv,px,py,'p_unknown',13,rim=None,col=TQL)
    for nm,pos,kind in st.get('people',[]):
        px,py=Q(*pos); dd=ImageDraw.Draw(cv)
        if kind=='guard': dd.ellipse((px-8,py-8,px+8,py+8),fill=AMBER,outline=INKD,width=2)
        else: badge(cv,nm,px,py,11)
    for n,(bx,by),selected in st.get('cell',[]):
        px,py=Q(bx,by); dd=ImageDraw.Draw(cv)
        if selected: dd.ellipse((px-17,py-17,px+17,py+17),outline=TQL,width=4)
        dd.ellipse((px-12,py-12,px+12,py+12),fill=INKD,outline=CREAM,width=3); text(cv,px,py,str(n),'d',10,'Bold','cream','mm')
    for kind,(bx,by),ang,n in st.get('units',[]):
        px,py=Q(bx,by); dd=ImageDraw.Draw(cv); a=math.radians(ang)
        dd.polygon([(px+16*math.cos(a),py+16*math.sin(a)),(px+10*math.cos(a+2.5),py+10*math.sin(a+2.5)),(px+10*math.cos(a-2.5),py+10*math.sin(a-2.5))],fill=AMBER)
        badge(cv,'signal_police' if kind=='police' else 'signal_guard',px-12*math.cos(a),py-12*math.sin(a),9)
    vw=st.get('view')                                                                                    # where the main camera is looking
    if vw:
        c=Q(*vw); dd=ImageDraw.Draw(cv); k=42; poly=[(c[0]-k*1.1,c[1]-k*0.5),(c[0]+k*1.1,c[1]-k*0.5),(c[0]+k*0.9,c[1]+k*0.5),(c[0]-k*0.9,c[1]+k*0.5)]
        dd.line(poly+[poly[0]],fill=(247,244,236,190),width=3)
    return (X,Y,W,H)
# ---------------------------------------------------------------- the order sheet (right click): one line per way
def order_sheet(cv,x,y,w,agent,target,label,rows,hover=None,alt=False):
    """x,y,w 1x. rows: dict(glyph,label,items,key,more=[(icon,text)...]). returns 2x box"""
    W=w*2; rh=72; ext=64 if alt else 0; H=int(104+len(rows)*(rh+ext+10)+14); X,Y=x*2,y*2
    shadow(cv,(X,Y,X+W,Y+H),r=36,a=80,off=8); panel(cv,'panel_dark',X,Y,W,H)
    cm=spr(f'cameo_{agent}_normal').resize((60,60),Image.LANCZOS); cv.paste(cm,(int(X+26),int(Y+22)),cm)
    d=ImageDraw.Draw(cv); d.line([(X+104,Y+52),(X+130,Y+52)],fill=CREAM,width=4); d.polygon([(X+142,Y+52),(X+128,Y+43),(X+128,Y+61)],fill=CREAM)
    if target.startswith('signal') or target.startswith('npc'): badge(cv,target,X+182,Y+52,22)
    else: mdisc(cv,X+182,Y+52,target,22)
    text(cv,X+222,Y+52,label,'d',15,'Bold','cream','lm')
    yy=Y+92
    for i,r in enumerate(rows):
        hh=rh+(ext if alt else 0); card=nine('plate_light',W-40,hh); cv.alpha_composite(card,(int(X+20),int(yy)))
        if hover==i: d.rounded_rectangle((X+16,yy-4,X+W-16,yy+hh+4),radius=26,outline=TQ,width=5)
        put_icon(cv,r['glyph'],X+40,yy+rh/2-17,34,INK); text(cv,X+84,yy+rh/2,r['label'],'d',14.5,'Bold','ink','lm')
        px=X+84+max(tw(rr['label'],'d',14.5,'Bold') for rr in rows)+26
        price(cv,px,yy+rh/2,r['items'],ink=True,size=14.5,gap=18)
        kp=nine('plate_dark',44,38); cv.alpha_composite(kp,(int(X+W-86),int(yy+rh/2-19))); text(cv,X+W-64,yy+rh/2,str(r.get('key','')),'d',11,'Bold','cream','mm')
        if alt and r.get('more'):
            xx=X+84; cy2=yy+rh+ext/2+2; d.line([(X+40,yy+rh+2),(X+W-60,yy+rh+2)],fill=(23,48,47,60),width=2)
            for ic,t in r['more']:
                put_icon(cv,ic,xx,cy2-15,30,INK); text(cv,xx+36,cy2,t,'d',12.5,'Bold','ink','lm'); xx+=36+tw(t,'d',12.5,'Bold')+20
        yy+=hh+10
    return (X,Y,W,H)

def way_plate(cv,knob_x,py,i,name,glyph,ways,on):
    """armed Takedown: one name plate (the way's glyph and its name) and the two ways as round keys; 4 again flips. 1x knob_x,py = plate top-left x, y"""
    cx=(knob_x+(42.3+47.6*i))*2; y=py*2+108+8; wd=tw(name,'d',12.5,'Bold')+84
    panel(cv,'plate_dark',cx-wd/2,y,wd,40); put_icon(cv,glyph,cx-wd/2+14,y+7,26,CREAM); text(cv,cx-wd/2+50,y+20,name,'d',12.5,'Bold','cream','lm')
    for j,g in enumerate(ways):
        mx=cx-24+j*56; my=y+40+30
        mdisc(cv,mx,my,g,18,rim=TQ if j==on else None)
