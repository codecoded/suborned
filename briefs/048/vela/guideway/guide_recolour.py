import sys
from PIL import Image, ImageFilter, ImageDraw, ImageFont
import numpy as np
SRC='briefs/028/corners/game-0305/r29-boulevard-market-play-210.png'
OUT='briefs/048/vela/guideway/'
im=Image.open(SRC).convert('RGB'); a=np.asarray(im).astype(float)
R,G,B=a[...,0],a[...,1],a[...,2]
m=((R/(B+1)>1.12)&(R/(B+1)<1.38)&(G/(B+1)>1.07)&(G/(B+1)<1.30)&(R>=G-1)&(R-G<16)&(R>45)&(R<160)&(G>40))
H,W=m.shape
yy,xx=np.mgrid[0:H,0:W]
# corridor around the guideway: line through (320,40)-(600,340) in the 960 frame, x4
p0=np.array([320*4.,40*4.]);p1=np.array([600*4.,340*4.]);d=(p1-p0)/np.linalg.norm(p1-p0);n=np.array([-d[1],d[0]])
perp=(xx-p0[0])*n[0]+(yy-p0[1])*n[1]; along=(xx-p0[0])*d[0]+(yy-p0[1])*d[1]
m&=(abs(perp)<300)&(along>-200)&(along<2300)
mk=Image.fromarray((m*255).astype(np.uint8)).filter(ImageFilter.MaxFilter(3)).filter(ImageFilter.GaussianBlur(1.2))
mk=np.asarray(mk).astype(float)/255
def s2l(c): return np.where(c<=0.04045,c/12.92,((c+0.055)/1.055)**2.4)
def l2s(c): c=np.clip(c,0,1); return np.where(c<=0.0031308,c*12.92,1.055*c**(1/2.4)-0.055)
def hx(h): h=h.lstrip('#'); return np.array([int(h[i:i+2],16)/255 for i in (0,2,4)])
old=s2l(hx('#6f7a72'))
def recolour(new):
    ratio=s2l(hx(new))/old
    lin=s2l(a/255)*ratio
    out=l2s(lin)*255
    r=a*(1-mk[...,None])+out*mk[...,None]
    return Image.fromarray(np.clip(r+.5,0,255).astype(np.uint8))
cands={'A chrome #cbc6c3':'#cbc6c3','B silver #b3c7cf':'#b3c7cf','C pale seafoam #9bcfc2':'#9bcfc2','D cream #e9e2d2':'#e9e2d2'}
crop=(900,0,3300,1700)  # in 3840 frame
tiles=[('now #6f7a72',im)]+[(k,recolour(v)) for k,v in cands.items()]
for k,t in tiles[1:]: t.save(OUT+'play210-'+k.split()[0]+'.png')
cw=(crop[2]-crop[0])//2; ch=(crop[3]-crop[1])//2
sheet=Image.new('RGB',(cw*3,ch*2+0),'white')
for i,(k,t) in enumerate(tiles[:5]):
    c=t.crop(crop).resize((cw,ch),Image.LANCZOS)
    dr=ImageDraw.Draw(c); dr.rectangle((0,0,cw,34),fill=(23,48,47)); dr.text((10,10),k,fill=(247,244,236))
    sheet.paste(c,((i%3)*cw,(i//3)*ch))
sheet.save(OUT+'guideway-candidates.png')
print(sheet.size, mk.sum())
