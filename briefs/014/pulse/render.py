import json, sys, math
from PIL import Image, ImageDraw, ImageFont
g=json.load(open('/Users/sky/Projects/suborned/out/pulse/geom.json'))
X0,X1,Y0,Y1=-30,8,-50,-8   # metres, block frame
S=16
def px(p): return ((p[0]-X0)*S, (p[1]-Y0)*S)
def pos(person,t,P):
    st=person['stops']
    for i,s in enumerate(st):
        if s[1]<=t<s[2]: return (P[s[0]],'at',s[0]) if s[3]==1 else None
        if i+1<len(st) and s[2]<=t<st[i+1][1]:
            if s[0]=='beyond' or st[i+1][0]=='beyond': return None
            a=P[s[0]]; b=P[st[i+1][0]]; f=(t-s[2])/(st[i+1][1]-s[2])
            return ([a[0]+(b[0]-a[0])*f, a[1]+(b[1]-a[1])*f],'walk',s[0])
    return None
def panel(simfile,t,title):
    d=json.load(open(simfile)); P={k:[v[0]/100,v[1]/100] for k,v in d['places'].items()}
    W=int((X1-X0)*S); H=int((Y1-Y0)*S)+34
    im=Image.new('RGB',(W,H),(246,238,222)); dr=ImageDraw.Draw(im)
    f=ImageFont.load_default()
    def poly(pts,fill,outline=None):
        dr.polygon([px(p) for p in pts],fill=fill,outline=outline)
    for b in g['buildings']:
        poly(b['poly'],(214,204,188),(150,140,125))
    for o in g['objects']:
        if o['kind']=='planter': poly(o['poly'],(170,200,170))
        if o['kind']=='table': poly(o['poly'],(235,225,205),(170,160,140))
    for k,v in P.items():
        if k.startswith('cafe-table') and 'seat' in k:
            x,y=px(v); dr.ellipse([x-3,y-3,x+3,y+3],outline=(150,140,125))
        if k.startswith('cafe-stand'):
            x,y=px(v); dr.rectangle([x-1,y-1,x+1,y+1],fill=(200,190,170))
    x,y=px(P['cafe-entrance']); dr.rectangle([x-6,y-6,x+6,y+6],outline=(60,60,60),width=2)
    bx,by=px([-8.2,-29.0]); dr.line([bx-4,by-4,bx+4,by+4],fill=(60,60,60)); dr.line([bx-4,by+4,bx+4,by-4],fill=(60,60,60))
    n=0; at_door=0
    for p in d['people']:
        r=pos(p,t,P)
        if not r: continue
        (q,kind,pl)=r
        x,y=px(q)
        if x<-10 or y<-10 or x>W+10 or y>H: continue
        n+=1
        if pl=='cafe-entrance' and kind=='at':
            at_door+=1; col=(30,45,90)
        elif kind=='walk': col=(110,110,120)
        else: col=(30,140,150)
        r_=6
        dr.ellipse([x-r_,y-r_,x+r_,y+r_],fill=col,outline=(255,255,255))
        if p['id']=='p-cafe-14': dr.ellipse([x-10,y-10,x+10,y+10],outline=(30,45,90),width=2)
    dr.rectangle([0,H-34,W,H],fill=(236,226,206))
    dr.text((6,H-30),f'{title}',fill=(30,30,30),font=f)
    dr.text((6,H-16),f'{n} people drawn in view, {at_door} at the cafe door',fill=(30,30,30),font=f)
    return im
before='/Users/sky/Projects/suborned/sim/data/handover.people.json'; after='/Users/sky/Projects/suborned/briefs/014/pulse/handover.people.after.json'
times=[(30,'0:30'),(190,'3:10'),(300,'5:00')]
ims=[[panel(f,t,f'{lab}  mission {tl}') for t,tl in times] for lab,f in (('BEFORE (r016, tree today)',before),('AFTER',after))]
W=sum(i.width for i in ims[0])+20; H=ims[0][0].height*2+10
out=Image.new('RGB',(W,H),(255,255,255))
for r,row in enumerate(ims):
    x=0
    for im in row: out.paste(im,(x,r*(im.height+10))); x+=im.width+10
out.save(sys.argv[1])
print(out.size)
