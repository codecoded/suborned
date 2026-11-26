"""Laminate, glazed masonry, glass and the enamel globe at Lantern Court."""
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter
from math import pi,sin,cos

HERE=Path(__file__).resolve().parent
OUT=HERE.parents[1]/'unity/Assets/Suborned/Materials/Play'
N=384
rng=np.random.default_rng(1717)
y,x=np.indices((N,N));n=rng.normal(0,1.6,(N,N))
base=Image.new('RGB',(N*4,N*4));mask=Image.new('RGBA',(512,512));lights=Image.new('RGB',(512,512))
rooms=Image.open(OUT/'ResidenceWindows.jpg').convert('RGB')
sample=Image.open(OUT/'ResidenceStone.jpg').convert('L').resize((N,N),Image.Resampling.LANCZOS)
grain=np.array(sample).astype(float);grain-=grain.mean()


def rgb(v,texture):
    return Image.fromarray(np.clip(np.array(v)+texture[:,:,None],0,255).astype('uint8'))


def put(slot,im,metal,gloss,emit=None):
    def pad(image,side):
        border=round(side*.035)
        data=np.asarray(image.resize((side-2*border,side-2*border),Image.Resampling.LANCZOS))
        return Image.fromarray(np.pad(data,((border,border),(border,border),(0,0)),mode='edge'))
    for target,side,tile in [(base,N,im),(mask,128,Image.new('RGBA',(N,N),(metal,0,0,gloss))),(lights,128,emit or Image.new('RGB',(N,N)))]:
        target.paste(pad(tile,side),(slot%4*side,slot//4*side))


put(0,rgb((215,205,180),grain*.77),0,102)                # shell limestone
put(1,rgb((169,157,135),grain*.48),0,108)                # shaded terrazzo
grout=np.minimum(x%32,31-x%32)<1
put(2,rgb((105,157,148),n-grout*14),25,155)              # glazed celadon
for index,quad in [(3,0),(4,2),(5,1),(8,3)]:
    ix,iy=quad%2*512,quad//2*512
    r=rooms.crop((ix+18,iy+18,ix+494,iy+494)).resize((N,N))
    raw=np.array(r).astype(float)
    # Daylit tinted glass over the flats, with a higher sky value in the top third.
    tint=np.array((72,113,125))[None,None,:]+(1-y/N)[:,:,None]*16
    glass=np.clip(raw*.62+tint*.47,0,255).astype('uint8')
    put(index,Image.fromarray(glass),57,195,r.point(lambda v:max(0,(v-114)*.27)))
put(6,rgb((176,148,95),n+1.8*np.cos(y*.8)),190,161)  # aged polished brass
put(7,rgb((145,69,57),grain*.41),10,120)             # oxblood vitreous panels
# Cartography from Natural Earth's public-domain 1:110m land, kept as a small source mask.
land=np.asarray(Image.open(HERE/'art/meridian-land.png').resize((N,N),Image.Resampling.LANCZOS))[:,:,None]/255
sea=np.array([45,108,115])[None,None,:]+(np.cos(y/N*2*pi)[:,:,None]*4)
metal_land=np.array([200,169,107])[None,None,:]+grain[:,:,None]*.33
maprgb=Image.fromarray(np.clip(sea*(1-land)+metal_land*land,0,255).astype('uint8'))
grid=ImageDraw.Draw(maprgb)
for lon in range(0,N,32):grid.line((lon,0,lon,N),fill=(101,142,132),width=1)
for lat in range(0,N,48):grid.line((0,lat,N,lat),fill=(96,140,133),width=1)
put(9,maprgb,103,171)
fontpath=next((f for f in ['/System/Library/Fonts/Supplemental/Futura.ttc','/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'] if Path(f).is_file()),'DejaVuSans.ttf')
sign=Image.new('RGB',(1056,170),(30,61,69));s=ImageDraw.Draw(sign)
s.rounded_rectangle((3,3,1052,166),radius=27,outline=(202,177,114),width=4)
s.text((528,65),'L A N T E R N   C O U R T',anchor='mm',font=ImageFont.truetype(fontpath,51),fill=(243,225,181))
s.text((528,124),'M E R I D I A N    W A L K',anchor='mm',font=ImageFont.truetype(fontpath,22),fill=(206,187,142))
put(10,sign,63,153,sign.point(lambda v:max(0,(v-115)*.55)))
put(11,rgb((59,73,53),grain*.65),0,64)
put(12,rgb((211,226,212),n*.50),0,157,Image.new('RGB',(N,N),(133,181,164)))
put(13,rgb((68,84,89),n),125,155)
dial=Image.new('RGB',(N,N),(66,81,81));d=ImageDraw.Draw(dial)
d.ellipse((7,7,N-7,N-7),fill=(218,211,182),outline=(163,139,92),width=11);c=N/2
for k in range(60):
    a=k*pi/30;r=144 if k%5==0 else 158
    d.line((c+r*sin(a),c-r*cos(a),c+168*sin(a),c-168*cos(a)),fill=(44,76,74),width=6 if k%5==0 else 2)
d.line((125,121,c,c,268,154),fill=(38,68,68),width=10)
put(14,dial,11,145,dial.point(lambda v:max(0,(v-115)*.42)))
put(15,rgb((181,175,153),grain*.85+n*.66),0,105)
# A power-of-two atlas keeps the imported compression and the mip chain predictable on Metal.
base.resize((2048,2048),Image.Resampling.LANCZOS).save(OUT/'CourtColour.jpg',quality=93,subsampling=0,optimize=True)
mask.save(OUT/'CourtMetal.png',optimize=True);lights.save(OUT/'CourtLights.png',optimize=True)
