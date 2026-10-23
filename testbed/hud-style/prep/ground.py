# HUD style lab, ground backdrops for the ground-marker and under-fire arrow pictures (brief 046).
# Source: the only real play-zoom frame with a clean person on the ground, briefs/024/calm-ring/work-m1-rest.png (the city guard in the upper left).
# The guard is cut out (polygon + GrabCut) so ground decals can be drawn UNDER him: ground-agent.png (RGBA). Two windows 300 x 290 native px:
#   ground-pave.jpg  the guard in place on pavement;  ground-asph.jpg  the guard pasted onto asphalt with a soft contact shadow.
# ground.json = feet, head top, cutout box per window (native px). The lab draws them at 0.55 (guard about 66 px tall = play zoom) or larger.
# Run: python3 -I testbed/hud-style/prep/ground.py   (needs opencv, numpy, pillow)
import cv2, json, numpy as np
SRC='/Users/sky/Projects/suborned/briefs/024/calm-ring/work-m1-rest.png'
O='/Users/sky/Projects/suborned/testbed/hud-style/bg/'
im=cv2.imread(SRC); H,W=im.shape[:2]
# guard outline in tile coordinates (tile = crop at y0=90), traced from the 5x view; GrabCut refines the edge
P5=[(318,92),(345,68),(410,66),(442,85),(445,120),(432,145),(448,170),(470,215),(472,300),(455,350),(445,380),(447,440),(468,500),(472,590),(480,615),(478,648),(420,655),(362,642),(358,622),(400,606),(432,588),(420,520),(398,470),(350,540),(300,610),(285,635),(262,662),(220,655),(170,625),(172,605),(240,600),(290,560),(320,500),(330,420),(335,380),(312,330),(320,290),(335,230),(350,190),(345,150),(325,130)]
P=np.array([(140+x/5,90+60+y/5) for x,y in P5],np.float32)   # global coords
poly=np.zeros((H,W),np.uint8); cv2.fillPoly(poly,[np.round(P*8).astype(np.int32)],255,shift=3)
inner=cv2.erode(poly,np.ones((5,5),np.uint8)); outer=cv2.dilate(poly,np.ones((7,7),np.uint8))
mask=np.full((H,W),cv2.GC_BGD,np.uint8); mask[outer>0]=cv2.GC_PR_BGD; mask[poly>0]=cv2.GC_PR_FGD; mask[inner>0]=cv2.GC_FGD
bg=np.zeros((1,65));fg=np.zeros((1,65)); cv2.grabCut(im,mask,None,bg,fg,6,cv2.GC_INIT_WITH_MASK)
m=((mask==cv2.GC_FGD)|(mask==cv2.GC_PR_FGD)).astype(np.uint8)
n,lab,st,_=cv2.connectedComponentsWithStats(m); k=1+np.argmax(st[1:,cv2.CC_STAT_AREA]); m=(lab==k).astype(np.uint8)
m=cv2.morphologyEx(m,cv2.MORPH_CLOSE,np.ones((3,3),np.uint8)); ff=m.copy(); cv2.floodFill(ff,None,(0,0),2); m=((ff!=2)|(m>0)).astype(np.uint8)
m=cv2.erode(m,np.ones((3,3),np.uint8))   # one pixel in, so no pavement-coloured fringe shows on the asphalt
ys,xs=np.where(m>0); bx0,by0,bx1,by1=xs.min(),ys.min(),xs.max()+1,ys.max()+1
alpha=cv2.GaussianBlur((m*255).astype(np.uint8),(0,0),0.7)
rgba=np.dstack([im,alpha])[by0:by1,bx0:bx1]
cv2.imwrite(O+'ground-agent.png',rgba)
feet=(204,276)   # global: the point between his shoes
WW,WH=300,290
meta={'win':[WW,WH],'k':0.55,'agent':{'w':int(bx1-bx0),'h':int(by1-by0),'feetInBox':[feet[0]-int(bx0),feet[1]-int(by0)]}}
# pavement window: guard in place
wx,wy=54,96; pave=im[wy:wy+WH,wx:wx+WW].copy(); cv2.imwrite(O+'ground-pave.jpg',pave,[cv2.IMWRITE_JPEG_QUALITY,92])
meta['pave']={'feet':[feet[0]-wx,feet[1]-wy],'agentBox':[int(bx0)-wx,int(by0)-wy]}
# asphalt window: paste the guard (feet at 150,180) with a soft contact shadow
ax,ay=470,560; asp=im[ay:ay+WH,ax:ax+WW].copy().astype(np.float32)
fx,fy=150,180
sh=np.zeros((WH,WW),np.float32); cv2.ellipse(sh,(fx+6,fy+2),(46,13),0,0,360,1,-1); sh=cv2.GaussianBlur(sh,(0,0),7); asp*=(1-0.38*sh)[...,None]
tx,ty=fx-(feet[0]-int(bx0)),fy-(feet[1]-int(by0)); a=(alpha[by0:by1,bx0:bx1].astype(np.float32)/255)[...,None]
h,w=a.shape[:2]; reg=asp[ty:ty+h,tx:tx+w]; asp[ty:ty+h,tx:tx+w]=reg*(1-a)+im[by0:by1,bx0:bx1].astype(np.float32)*a
cv2.imwrite(O+'ground-asph.jpg',np.clip(asp,0,255).astype(np.uint8),[cv2.IMWRITE_JPEG_QUALITY,92])
meta['asph']={'feet':[fx,fy],'agentBox':[tx,ty]}
json.dump(meta,open(O+'ground.json','w'),indent=1); print(meta)
