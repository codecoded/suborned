import sys
from PIL import Image
# usage: ink.py img x y w h [mode dark|light] [inner_frac]
f,x,y,w,h=sys.argv[1],*map(float,sys.argv[2:6]); mode=sys.argv[6] if len(sys.argv)>6 else 'dark'; fr=float(sys.argv[7]) if len(sys.argv)>7 else 0.36
im=Image.open(f).convert('RGB'); cx,cy=x+w/2,y+h/2; R=min(w,h)*fr
L=lambda p:(p[0]*0.3+p[1]*0.59+p[2]*0.11)
xs=[];ys=[];sw=sx=sy=0
for py in range(int(cy-R)-1,int(cy+R)+2):
  for px in range(int(cx-R)-1,int(cx+R)+2):
    if (px+0.5-cx)**2+(py+0.5-cy)**2>R*R: continue
    l=L(im.getpixel((px,py))); wgt=max(0,(215-l)) if mode=='dark' else max(0,l-170)
    if wgt>0: sw+=wgt; sx+=wgt*(px+0.5); sy+=wgt*(py+0.5)
    if wgt>40: xs.append(px); ys.append(py)
print('box centre (%.1f,%.1f)'%(cx,cy), 'ink centroid dx %+.2f dy %+.2f'%(sx/sw-cx,sy/sw-cy) if sw else 'no ink', 'ink bbox dx %+.1f dy %+.1f'%((min(xs)+max(xs)+1)/2-cx,(min(ys)+max(ys)+1)/2-cy) if xs else '')
im.crop((int(x)-4,int(y)-4,int(x+w)+4,int(y+h)+4)).resize((int(w+8)*10,int(h+8)*10),Image.NEAREST).save(sys.argv[8] if len(sys.argv)>8 else '/dev/null','PNG')
