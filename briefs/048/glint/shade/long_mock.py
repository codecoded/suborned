import numpy as np, sys
from PIL import Image, ImageFilter
from aces import *
ex_old=2**0.15; ex_new=2**-0.05
def hsv_arr(im):
    r,g,b=im[...,0],im[...,1],im[...,2]
    mx=im.max(2); mn=im.min(2); d=np.maximum(mx-mn,1e-6)
    h=np.where(mx==r,((g-b)/d)%6,np.where(mx==g,(b-r)/d+2,(r-g)/d+4))*60
    s=np.where(mx>0,(mx-mn)/np.maximum(mx,1e-6),0)
    return h,s,mx
def hsv2rgb(h,s,v):
    h=(h%360)/60; c=v*s; x=c*(1-np.abs(h%2-1)); m=v-c
    z=np.zeros_like(h)
    conds=[(h<1),(h<2),(h<3),(h<4),(h<5),(h>=5)]
    rs=[c,x,z,z,x,c]; gs=[x,c,c,x,z,z]; bs=[z,z,x,c,c,x]
    r=np.select(conds,rs); g=np.select(conds,gs); b=np.select(conds,bs)
    return np.stack([r+m,g+m,b+m],-1)
def lin_hex(h): return s2l(np.array([int(h[i:i+2],16) for i in (1,3,5)])/255)
old_haze=lin_hex('#f1e5cc')*1.3; new_haze=lin_hex('#fad9bd')*1.2
STR=0.62; sun=lin_hex('#ffe0d0')*np.sin(np.radians(38))*1.8; old_sky=lin_hex('#dedce0')*0.69
k_shade=(old_sky+(1-STR)*sun)/old_sky
src=sys.argv[1]; out=sys.argv[2]
im=np.asarray(Image.open(src).convert('RGB'),float)/255
H,W,_=im.shape; sc=W/2000
yy,xx=np.mgrid[0:H,0:W]; Yd=yy/sc; Xd=xx/sc
h,s,v=hsv_arr(im)
lin=aces_inv(s2l(im))/ex_old
res=lin.copy()
# --- shade (plaza and road shadows): up-facing, warm sun fill
shade=((v<0.74)&(((h>190)&(h<235)&(s>0.12)&(s<0.5))|((h>8)&(h<62)&(s>0.07)&(s<0.34)&(v>0.36))|((s<0.05)&(v>0.45)))&(Yd>420))
shade=np.asarray(Image.fromarray((shade*255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(1.0)),float)/255
res=res*(1-shade[...,None])+res*k_shade*shade[...,None]
# --- sky: peach cast on the blue-to-haze blend and the new haze colour toward the horizon
sky=((Yd<420)&((((h>192)&(h<226)&(s>0.07)))|((s<0.045)&(v>0.78)&(Yd<400))))
sky=np.asarray(Image.fromarray((sky*255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(2.0)),float)/255
R=im[...,0]
w=np.clip((R-0.60)/(0.866-0.60),0,1)           # 0 = body (#98bad6 R 152), 1 = haze (#dd R 221)
bump=4*w*(1-w)
peach=np.array([1.12,0.96,0.80])
mult=1+(peach-1)*(1.0*bump)[...,None]
ratio=(new_haze/old_haze)
hz=1+(ratio-1)*(w**2)[...,None]
res=res*(1-sky[...,None])+res*mult*hz*sky[...,None]
plainw=None
o=l2s(aces(res*ex_new))
# --- far plain and far haze: in display space, the sand family above y 470 loses saturation and turns peach (the plain now ends on the sky's horizon colour)
hh0,ss0,vv0=hsv_arr(o)
fam=np.clip(1-np.abs(hh0-42)/14,0,1)*np.clip((ss0-0.10)/0.06,0,1)*np.clip((vv0-0.74)/0.06,0,1)*np.clip((470-Yd)/60,0,1)*np.clip((Yd-170)/40,0,1)
fam=np.asarray(Image.fromarray((fam*255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(1.5)),float)/255
tgt_h,tgt_s=33,0.10
h2=hh0+(tgt_h-hh0)*fam; s2=ss0+(tgt_s-ss0)*fam; v2=np.clip(vv0+0.025*fam,0,1)
o=hsv2rgb(h2,s2,v2)
# --- lawn / greens hue (display space)
hh,ss,vv=hsv_arr(o)
def ramp(h,a,b,c,d_,amt):
    up=np.clip((h-a)/(b-a),0,1); dn=np.clip((d_-h)/(d_-c),0,1); return amt*np.minimum(up,dn)
def sm(x): return x*x*(3-2*x)
old=ramp(hh,70,95,140,170,8); new=ramp(hh,55,72,135,170,10)
o2=hsv2rgb(hh+(new-old),ss,vv)
o=np.where(((hh>55)&(hh<170)&(ss>0.15))[...,None],o2,o)
Image.fromarray((np.clip(o,0,1)*255+0.5).astype(np.uint8)).save(out)
def hx(c): return '#%02x%02x%02x'%tuple(int(round(x*255)) for x in c)
def hsv_str(c):
    a=np.array(c)[None,None,:]; h,s,v=hsv_arr(a); return f'H{h[0,0]:.0f} S{s[0,0]:.2f} V{v[0,0]:.2f}'
for k,(x,y) in {'sky top':(1000,30),'sky mid':(1000,150),'sky low':(1000,260),'horizon band':(300,340),'far plain right':(1700,400),'plain close right':(1900,500),'lawn':(700,640),'plaza':(300,1000),'plaza sun':(300,880),'street road':(1070,600)}.items():
    X,Y=int(x*sc),int(y*sc); sl=np.s_[Y-6:Y+6,X-6:X+6]
    a=im[sl].mean((0,1)); b=o[sl].mean((0,1)); print(f'{k:18s}',hx(a),hsv_str(a),'->',hx(b),hsv_str(b))
