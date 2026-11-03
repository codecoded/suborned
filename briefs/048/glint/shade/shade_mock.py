import numpy as np, sys, colorsys
from PIL import Image
from aces import *
ex=2**0.15
def lin_hex(h): return s2l(np.array([int(h[i:i+2],16) for i in (1,3,5)])/255)
old_sky=lin_hex('#dedce0')*0.69; old_eq=lin_hex('#faeadc')*0.71
SKY=sys.argv[1] if len(sys.argv)>1 else '#f0e3d6'; SKYV=float(sys.argv[2]) if len(sys.argv)>2 else 0.82
SUN_OLD,SUN_NEW=1.8,float(sys.argv[3]) if len(sys.argv)>3 else 1.55
LIFT_OLD,LIFT_NEW=0.03,float(sys.argv[4]) if len(sys.argv)>4 else 0.045
STR=float(sys.argv[5]) if len(sys.argv)>5 else 1.0
new_sky=lin_hex(SKY)*SKYV
sun=lin_hex('#ffe0d0')*np.sin(np.radians(38))
lit_old=sun*SUN_OLD+old_sky; lit_new=sun*SUN_NEW+new_sky
shade_new=new_sky+(1-STR)*sun*SUN_NEW
k_lit=lit_new/lit_old; k_shade=shade_new/old_sky
print('k_lit',k_lit.round(3),'k_shade',k_shade.round(3))
def run(src,hud,out):
    im=np.asarray(Image.open(src).convert('RGB'),float)/255
    H,W,_=im.shape
    ha=np.asarray(Image.open(hud).convert('RGBA'))[...,3]>0 if hud else np.zeros((H,W),bool)
    hsv=np.array([colorsys.rgb_to_hsv(*p) for p in im.reshape(-1,3)[::1]]) if False else None
    mx=im.max(2); mn=im.min(2); V=mx; S=np.where(mx>0,(mx-mn)/np.maximum(mx,1e-6),0)
    r,g,b=im[...,0],im[...,1],im[...,2]
    # hue
    d=np.maximum(mx-mn,1e-6)
    h=np.where(mx==r,((g-b)/d)%6,np.where(mx==g,(b-r)/d+2,(r-g)/d+4))*60
    shade=((V<0.68)&(
        ((h>175)&(h<240)&(S>0.12)&(S<0.5)) |                 # blue road in shade
        ((h>8)&(h<62)&(S>0.07)&(S<0.34)&(V>0.36)) |         # warm paving / cream in shade
        ((S<0.05)&(V>0.38))))                                # neutral slab in shade
    # soft edge
    from PIL import ImageFilter
    m=Image.fromarray((shade*255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(1.2))
    m=np.asarray(m,float)/255
    m=m*(~ha)
    lin=aces_inv(s2l(im))/ex
    lit=lin*k_lit
    sh=lin*k_shade
    sh=sh*(1-(LIFT_NEW-LIFT_OLD))+(LIFT_NEW-LIFT_OLD)
    res=lit*(1-m[...,None])+sh*m[...,None]
    o=l2s(aces(res*ex))
    # keep hud and untouched exactly
    o=np.where(ha[...,None],im,o)
    # untouched lit pixels: keep original to avoid inversion noise
    keep=(m<0.02)[...,None]
    o=np.where(keep & ~ha[...,None], im, o)
    Image.fromarray((np.clip(o,0,1)*255+0.5).astype(np.uint8)).save(out)
    return im,o,m
if __name__=='__main__':
    im,o,m=run('/Users/sky/Projects/suborned/briefs/049/build/2160-0299/ring-loop-names.png','/Users/sky/Projects/suborned/briefs/049/build/2160-0299/ring-loop-names.hud.png','shade_after.png')
    def hsv(c):
        h,s,v=colorsys.rgb_to_hsv(*c);return f'#%02x%02x%02x H{h*360:.0f} S{s:.2f} V{v:.2f}'%tuple(int(x*255) for x in c)
    for k,(x,y) in {'road shade':(1400,800),'slab shade':(3100,500),'slab lit':(3500,500),'paving shade':(1000,500),'roof near tank':(500,1450),'road lit':(2400,150)}.items():
        sl=np.s_[y-6:y+6,x-6:x+6]
        print(k,hsv(im[sl].mean((0,1))),'->',hsv(o[sl].mean((0,1))))
