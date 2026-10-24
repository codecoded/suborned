# Glint, brief 048, "long tier: blue sky, warm distance" (2026-10-23). MOCK over the 0.2.94 long shot, not an engine render.
# Sky pixels: moved along the blue-to-haze blend with the band height changed (w_new = w_old ** (0.12 / hh_new)), new haze colour and a deeper body.
# Hazed far pixels: shifted by the change of haze colour in proportion to how close they are to the old haze. Depth is not known: read for the look.
#   python3 -I long_mock.py <out.png>
import sys, numpy as np
from PIL import Image, ImageDraw, ImageFont
R='/Users/sky/Projects/suborned/briefs/048/'
src=np.asarray(Image.open(R+'build/0294/tp-street-long.jpg').convert('RGB')).astype(np.float32)/255
H,W,_=src.shape
hx=lambda h:np.array([int(h[i:i+2],16) for i in (1,3,5)],np.float32)/255
HZ_OLD=hx('#dccdb0')          # rendered old haze (far plain)
B_OLD=hx('#aec4d7')           # rendered old sky at the top of the frame
HZ_NEW=hx('#f1e6d0')          # target rendered haze: V .94, S .13 (the old still reads #efe0c3 near the horizon)
B_NEW=hx('#8db4dc')           # target body: the follow tier's own sky colour (#90b4d4), no bloom veil
HH_OLD,HH_NEW=0.12,float(sys.argv[2]) if len(sys.argv)>2 else 0.075
# per-row sky estimate: median over columns, rows above 0.3 of the frame
rows=int(H*0.34); med=np.median(src[:rows],axis=1)       # (rows,3)
d=HZ_OLD-B_OLD
w_old=np.clip(((med-B_OLD)@d)/(d@d),0,1)
# rows beyond the top ramp: smooth it
k=np.ones(25)/25; w_old=np.convolve(np.pad(w_old,12,mode='edge'),k,'valid')
w_new=w_old**(HH_OLD/HH_NEW)
sky_old=B_OLD+np.outer(w_old,d)
sky_new=B_NEW+np.outer(w_new,HZ_NEW-B_NEW)
out=src.copy()
# sky mask: pixel close to the row's old sky colour
dist=np.linalg.norm(src[:rows]-sky_old[:,None,:],axis=2)
m=np.exp(-(dist/0.045)**2)
out[:rows]=src[:rows]+m[...,None]*(sky_new-sky_old)[:,None,:]
# hazed far pixels (everything, weighted by the sky mask's complement above the skyline)
fd=np.linalg.norm(src-HZ_OLD,axis=2)
f=np.clip(1-fd/0.15,0,1)
f[:rows]*=(1-m)
out=out+f[...,None]*(HZ_NEW-HZ_OLD)
out=np.clip(out,0,1)
im=Image.fromarray((out*255+.5).astype(np.uint8))
im.save(sys.argv[1])
