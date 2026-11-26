"""The residential estate's continuous 4.32 m square of honed stone and bedding joints."""
from pathlib import Path
import numpy as np
from PIL import Image

assets=Path(__file__).resolve().parents[2]/'unity/Assets/Suborned'
stock=assets/'OldTownArt/DecoKit/Textures'
out=assets/'Materials/Play'
size=1024
y,x=np.indices((size,size))
rng=np.random.default_rng(117)
row=y//128
offset=(row%2)*128
u=(x+offset)%256
v=y%128
block=(x+offset)//256%4
tones=rng.uniform(-8,8,(8,4))[row,block]
joint=np.minimum.reduce([u,255-u,v,127-v])
cut=np.clip(1-joint/2.4,0,1)
slab=Image.open(stock/'limestone_polished_v1.png').convert('L').resize((size,size),Image.Resampling.LANCZOS)
stone=np.asarray(slab,dtype=float); stone-=stone.mean()
# Stone is cooler in its small inclusions. Mortar sees less light under a projecting arris.
tone=stone*.78+tones-24*cut
rgb=np.clip([175,162,141]+tone[:,:,None],0,255).astype('uint8')
Image.fromarray(rgb).save(out/'ResidenceStone.jpg',quality=93,optimize=True)
normal=np.asarray(Image.open(stock/'limestone_polished_v1_normal.png').convert('RGB').resize((size,size)),dtype=float)/127.5-1
height=-cut*.64
dx=(np.roll(height,-1,axis=1)-np.roll(height,1,axis=1))/2
dy=(np.roll(height,-1,axis=0)-np.roll(height,1,axis=0))/2
normal[:,:,0]=normal[:,:,0]*.44-dx;normal[:,:,1]=normal[:,:,1]*.44+dy
normal/=np.maximum(.001,np.linalg.norm(normal,axis=2))[:,:,None]
Image.fromarray(np.clip((normal+1)*127.5,0,255).astype('uint8')).save(out/'ResidenceStoneNormal.png',optimize=True)
