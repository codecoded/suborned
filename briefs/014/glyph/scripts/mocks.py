# Glyph, brief 014 A: the 11 moments of briefs/013/play/ redrawn under the six rules. Output: after/NN.png (screen), NN.hud.png (HUD layer alone), NN.manifest.json (speech and beats).
from g14 import *
import cv2, numpy as np
W1,H1=1726,1142
SHOT={os.path.basename(f)[:2]:f for f in sorted(glob.glob(R+'/briefs/013/play/*.png'))}
def layer(): return Image.new('RGBA',(W1*2,H1*2),(0,0,0,0))
ROSTER_Y={'F1':142,'F2':225,'F3':305,'F4':385}
KN={'enforcer':[('enter','normal'),('fire','normal'),('take','normal'),('takedown','normal')],'scout':[('watch','normal'),('listen','normal'),('ask','normal'),(None,'normal')],'tech':[('loop','normal'),('reroute','normal'),(None,'normal'),(None,'normal')]}
CAM={'F1':'scout','F2':'fixer','F3':'tech','F4':'enforcer'}
def pill_w(*a,**k):
    t=Image.new('RGBA',(1400,200),(0,0,0,0)); return status(t,0,0,*a,**k)[0]/2
def chrome(L,shot,sel,clock,cash,trail,alert,paused=True,nxt=None,chips=None,plate=None,armed=None,knobs=None,resp=None,keys=(3,False),scope_st=None):
    paste_roster(L,shot)
    for k,(g,fr,lab) in (chips or {}).items(): act_chip(L,108,ROSTER_Y[k]-12,g,fr,lab)
    if plate is not False:
        k=knobs or KN[CAM[sel]]; py=ROSTER_Y[sel]-27+(0 if (chips or {}).get(sel) is None else -8)
        verb_plate2(L,108,py,k,**(armed or {}))
        if (chips or {}).get(sel) is not None: pass
    pw=pill_w(clock,cash,trail,alert,paused,nxt); status(L,1712-pw,12,clock,cash,trail,alert,paused,nxt)
    corner_keys(L,1712-92,58,*keys)
    if resp: resp_pill(L,1712-160,100,*resp)
    scope(L,14,996,st=scope_st)
def compose(k,L,manifest):
    manifest['elements']+=[{'kind':'image','x':14,'y':92,'w':92,'h':352,'text':'roster portraits'},{'kind':'image','x':14,'y':996,'w':212,'h':132,'text':'plot scope'}]
    bg=Image.open(f'bg/{k}.png').convert('RGB').convert('RGBA')
    hud=L.resize((W1,H1),Image.LANCZOS); out=Image.alpha_composite(bg,hud)
    out.convert('RGB').save(f'after/{k}.png'); hud.save(f'after/{k}.hud.png'); json.dump(manifest,open(f'after/{k}.manifest.json','w'),indent=1)
def M(el,state,**kw): return {'state':state,'elements':el}
def el(kind,box,text=''): X,Y,W,H=box; return {'kind':kind,'x':X/2,'y':Y/2,'w':W/2,'h':H/2,'text':text}
CELL0=[(1,(2.5,-49),False),(2,(3.7,-49),False),(3,(4.9,-49),False),(4,(6.1,-49),False)]
CONES=[('camera',(49.5,-41.5),(2.25,11.5),22,9,False),('camera',(74,-40),(4.2,9),18,9,False)]
LOOPED=[('camera',(49.5,-41.5),(2.25,11.5),22,9,True),('camera',(74,-40),(4.2,9),18,9,False)]
VALE=lambda age:{'at':(76.25,-30),'age':age}
def S(**kw):
    d={'cones':CONES,'vale':VALE(0.5)}; d.update(kw); return d
def run():
    shots={k:Image.open(f).convert('RGB') for k,f in SHOT.items()}
    # ------------------------------------------------ 01
    k='01'; L=layer(); sc=S(cell=[(1,(30,-45),False),(2,(8,-49),False),(3,(24,-18),True),(4,(30,-47),False)],view=(26,-30))
    chrome(L,shots[k],'F3','0:13','$100,000',0,0,True,chips={'F1':('s_moving',.2,'24 s'),'F3':('run',.5,'1 s'),'F4':('s_moving',.2,'25 s')},scope_st=sc)
    hl=goal_line(L,863,1075,[('Watch the',None),('café terrace.','place')],agent='scout',verb='watch')
    mo=order_sheet(L,790,330,330,'tech','signal_camera','Cabinet',[
      dict(glyph='loop',label='Entrance',items=[('p_time','62 s',False),('p_trail','+1',False)],key=1),
      dict(glyph='loop',label='Pickup',items=[('p_time','62 s',False),('p_trail','+1',False)],key=2),
      dict(glyph='loop',label='Mast',items=[('p_time','62 s',False),('p_trail','+1',False)],key=3),
      dict(glyph='reroute',label='Patrol',items=[('p_time','45 s',False),('p_trail','+1',False)],key=4)],hover=0)
    m_unknown(L,1490,2090*0+700,'camera') if False else None
    compose(k,L,M([el('line',hl,'Watch the café terrace.')],'active'))
    # ------------------------------------------------ 02
    k='02'; L=layer(); sc=S(cell=[(1,(28,-33),True),(2,(8,-49),False),(3,(24,-18),False),(4,(30,-47),False)],people=[('npc_waitress',(-10,-29),'n'),('guard',(51.75,-30),'guard')],view=(40,-30))
    chrome(L,shots[k],'F1','0:50','$100,000',0,0,True,chips={'F1':('watch',.55,'5 s'),'F3':('loop',.4,'27 s')},scope_st=sc)
    m_unknown(L,1550,840,'guard'); m_unknown(L,1900,1090,'guard')
    b=bubble(L,2262,810,'The patrol comes round, then nothing for minutes.')
    compose(k,L,M([el('speech',b,'The patrol comes round, then nothing for minutes.')],'rest'))
    # ------------------------------------------------ 03
    k='03'; L=layer(); sc=S(cell=[(1,(28,-33),False),(2,(8,-49),False),(3,(24,-18),False),(4,(44,-30),True)],people=[('npc_waitress',(-10,-29),'n'),('guard',(51.75,-30),'guard')],view=(48,-30),cones=LOOPED,route=[(26.6,-17),(40,-17),(48.6,-30)])
    ap,pts=wedge((1722,1010),-100,300,26); m_cone(L,ap,pts,'guard')
    chrome(L,shots[k],'F4','2:01','$100,000',2,0,True,chips={'F3':('loop',.5,'31 s')},scope_st=sc)
    way_plate(L,108,ROSTER_Y['F4']-27,3,'Knock out','way_knock',['way_knock','way_kill'],0)
    m_eye(L,1720,945,'shut','guard'); m_pip(L,1768,985,0.5)
    chip2(L,1790,990,'way_knock',[('p_time','7 s',False),('p_trail','+4',True),('p_alert','1',True),('eye_shut','',False)])
    compose(k,L,M([],'active'))
    # ------------------------------------------------ 04
    k='04'; L=layer(); sc=S(cell=[(1,(28,-33),False),(2,(8,-49),False),(3,(24,-18),False),(4,(44,-30),True)],people=[('npc_waitress',(-10,-29),'n'),('guard',(51.75,-30),'guard')],view=(48,-30),cones=LOOPED)
    ap,pts=wedge((1722,1010),-100,300,26); m_cone(L,ap,pts,'guard')
    chrome(L,shots[k],'F4','2:01','$100,000',2,0,True,chips={'F3':('loop',.5,'31 s')},scope_st=sc)
    m_eye(L,1720,945,'shut','guard'); m_pip(L,1768,985,0.5)
    m_lock(L,1870,860,ghost=True); m_wake(L,1722,1060,1.0,ghost=False) if False else None
    dashed_ring(L,1722,1050,52,52,TQL,n=26,w=5)
    order_sheet(L,860,420,352,'enforcer','signal_guard','Guard',[
      dict(glyph='way_knock',label='Knock out',items=[('p_time','7 s',False),('p_trail','+4',True),('p_alert','1',True),('eye_shut','',False)],key=1),
      dict(glyph='way_kill',label='Kill',items=[('p_time','4 s',False),('p_trail','+8',True),('p_alert','2',True),('eye_shut','',False)],key=2),
      dict(glyph='fire',label='Fire',items=[('p_trail','+2/hit',False),('p_alert','≥2',False)],key=3)],hover=0)
    compose(k,L,M([],'active'))
    # ------------------------------------------------ 05
    k='05'; L=layer(); sc=S(cell=[(1,(28,-33),False),(2,(8,-49),False),(3,(24,-18),False),(4,(50,-30),True)],people=[('npc_waitress',(-10,-29),'n')],view=(48,-30),cones=LOOPED)
    chrome(L,shots[k],'F4','2:04','$100,000',2,0,True,chips={'F3':('loop',.5,'28 s'),'F4':('takedown',.45,'4 s')},scope_st=sc)
    m_progress_ring(L,1770,1440,0.45,40)
    sp=bubble(L,1770,1330,'Close and quiet.')
    compose(k,L,M([el('speech',sp,'Close and quiet.')],'rest'))
    # ------------------------------------------------ 06
    k='06'; L=layer(); sc=S(cell=[(1,(28,-33),False),(2,(8,-49),False),(3,(24,-18),False),(4,(54,-31),False)],bodies=[(50,-31,False)],view=(50,-30),cones=CONES)
    chrome(L,shots[k],'F4','2:14','$100,000',2,0,True,chips={'F3':('s_moving',.15,'23 s'),'F4':('s_inside',.1,'32 s')},scope_st=sc)
    m_wake(L,1810,1380,0.95)
    compose(k,L,M([],'rest'))
    # ------------------------------------------------ 07
    k='07'; L=layer(); sc=S(cell=[(1,(28,-33),False),(2,(8,-49),False),(3,(24,-18),False),(4,(56,-38),True)],view=(48,-34),cones=LOOPED,stash=[(46,-35),(62.5,-43.2)])
    chrome(L,shots[k],'F4','2:12','$100,000',2,0,True,chips={'F3':('loop',.5,'25 s')},knobs=[('drop','normal'),('stash','normal'),('take','disabled'),('takedown','disabled')],scope_st=sc)
    pts=[(2006,740),(1900,1020),(1560,1400)]
    ap,wp=wedge((1990,1140),150,280,22); m_cone(L,ap,wp,'guard')
    dotted(L,[(2006,730),(1930,950),(1760,1210),(1500,1400)],CREAM,step=20,r=4,seg_fn=lambda x,y:'hatch' if (x>1750 and x<1960 and y>1000 and y<1250) else None)
    mdisc(L,2006,590,'s_carrying',24)
    m_stash(L,1470,1380,1,0); m_stash(L,1580,1430,3,0,hollow=False)
    m_lock(L,1620,1360,ghost=True,size=18)
    chip2(L,1340,1380,'s_carrying',[('p_time','18 s',False),('eye_unk','',False)])
    compose(k,L,M([],'rest'))
    # ------------------------------------------------ 08
    k='08'; L=layer(); sc=S(cell=[(1,(28,-33),False),(2,(8,-49),False),(3,(24,-18),False),(4,(56,-38),True)],view=(48,-34),cones=LOOPED,stash=[(46,-35)],units=[('guard',(62,-18),170,2)])
    chrome(L,shots[k],'F4','2:34','$100,000',2,1,True,chips={'F3':('loop',.5,'23 s')},resp=(0,0,2),scope_st=sc)
    m_stash(L,1750,1215,1,1)
    m_edge(L,2200,70,-90,glyph=None,badge_name='signal_guard',rim=AMBER,count=2)
    bs=[]; h,b1=beat(L,1712,1030,'Police coming.',icon_='signal_police') if False else (0,None)
    h1,b1=beat(L,1712,1000,'Reported.',icon_='call'); h2,b2=beat(L,1712,1042,'Police coming.',icon_='signal_police')
    compose(k,L,M([el('beat',b1,'Reported.'),el('beat',b2,'Police coming.')],'rest'))
    # ------------------------------------------------ 09
    k='09'; L=layer(); sc=S(cell=[(1,(70,-34),False),(2,(74,-34),False),(3,(76,-30),False),(4,(77,-31),True)],vale={'at':(77.5,-31),'exact':True},view=(70,-30),cones=CONES,bodies=[(50,-31,False)])
    chrome(L,shots[k],'F4','3:39','$100,000',6,1,True,chips=None,scope_st=sc,plate=False)
    m_wake(L,1810,1380,0.7)
    bx=2*432; by=2*160; bw=2*470; bh=2*150
    shadow(L,(bx,by,bx+bw,by+bh),r=44,a=90,off=10); panel(L,'panel_dark_modal',bx,by,bw,bh)
    text(L,bx+bw/2,by+54,'Vale is in the car.','d',24,'Bold','cream','mm')
    for i,(lab,xo) in enumerate((('Debrief',-130),('Replay',130))):
        im=nine('btn_text_normal',210,62); L.alpha_composite(im,(int(bx+bw/2+xo-105),int(by+bh-96))); text(L,bx+bw/2+xo,by+bh-65,lab,'d',15,'Bold','ink','mm')
    compose(k,L,M([el('beat',(bx,by+14,bw,80),'Vale is in the car.')],'active'))
    # ------------------------------------------------ 10 / 11
    for k in ('10','11'):
        L=layer(); sc=S(cell=[(1,(28,-33),False),(2,(8,-49),False),(3,(24,-18),False),(4,(54,-31),True)],view=(50,-30),cones=CONES,locks=[(51.75,-30,0.8)],bodies=[(50,-31,False)],units=[('police',(60,-20),150,2)])
        chrome(L,shots[k],'F4','2:29','$100,000',6,1,True,chips={'F3':('s_moving',.5,'7 s')},resp=(2,2,0),scope_st=sc)
        m_lock(L,1840,1260,frac=0.8); m_wake(L,1812,1405,0.74,found=True); m_badge(L,1900,1395,'signal_police',2); m_badge(L,1838,1428,'signal_guard',1)
        if k=='10':
            h1,b1=beat(L,1712,1000,'Body found.',icon_='body_out'); h2,b2=beat(L,1712,1042,'Police coming.',icon_='signal_police')
            compose(k,L,M([el('beat',b1,'Body found.'),el('beat',b2,'Police coming.')],'rest'))
        else:
            d=ImageDraw.Draw(L); d.ellipse((1840-44,1260-44,1840+44,1260+44),outline=CREAM,width=4); m_refuse(L,1930,1340,'lock','Held')
            compose(k,L,M([],'rest'))
run()
print('done')
# ---------------------------------------------------------------- 12: the Alt layer on moment 04 (not one of the 11; the new moment)
def alt():
    shots={k:Image.open(f).convert('RGB') for k,f in SHOT.items()}
    k='04'; L=layer(); sc=S(cell=[(1,(28,-33),False),(2,(8,-49),False),(3,(24,-18),False),(4,(44,-30),True)],people=[('npc_waitress',(-10,-29),'n'),('guard',(51.75,-30),'guard')],view=(48,-30),cones=LOOPED,stash=[(46,-35)])
    chrome(L,shots[k],'F4','2:01','$100,000',2,0,True,nxt='3:10',chips={'F1':('watch',None,'0:35'),'F3':('loop',.5,'31 s'),'F2':('s_ready',None,'')},scope_st=sc)
    ap,pts=wedge((1722,1010),-100,300,26); m_cone(L,ap,pts,'guard')
    m_eye(L,1720,945,'shut','guard'); m_pip(L,1768,985,0.5); m_lock(L,1870,860,ghost=True)
    dashed_ring(L,1722,1050,52,52,TQL,n=26,w=5)
    # name tags: a role, in the street where the person is (two words at most)
    for (x,y,s_,rim) in ((1722,1112,'Guard',AMBER),(1550,1020,'Clerk',None),(1900,1060,'Enforcer',None)):
        wd=tw(s_,'d',12,'Bold')+34; panel(L,'plate_dark',x-wd/2,y,wd,40); text(L,x,y+20,s_,'d',12,'Bold','cream','mm')
        if rim: d=ImageDraw.Draw(L); d.rounded_rectangle((x-wd/2-3,y-3,x+wd/2+3,y+43),radius=22,outline=rim,width=4)
    m_unknown(L,1550,980,'guard'); mdisc(L,1380,1280,'call',20)
    order_sheet(L,860,420,352,'enforcer','signal_guard','Guard',[
      dict(glyph='way_knock',label='Knock out',items=[('p_time','7 s',False),('p_trail','+4',True),('p_alert','1',True),('eye_shut','',False)],key=1,more=[('units','16 s'),('door_held','1:10'),('body_out','4:07'),('call','22 s')]),
      dict(glyph='way_kill',label='Kill',items=[('p_time','4 s',False),('p_trail','+8',True),('p_alert','2',True),('eye_shut','',False)],key=2,more=[('units','63 s'),('door_held','3:00'),('restrain','1')]),
      dict(glyph='fire',label='Fire',items=[('p_trail','+2/hit',False),('p_alert','≥2',False)],key=3,more=[('units','15 s'),('way_kill','2')])],hover=0,alt=True)
    compose('12',L,M([],'alt'))
alt()
