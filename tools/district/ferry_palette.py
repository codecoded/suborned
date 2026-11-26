"""Pack the pier head's shared colour atlas and its smaller linear material maps."""
from pathlib import Path
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ASSETS = HERE.parents[1] / 'unity/Assets/Suborned'
OUT = Path(sys.argv[1]) if len(sys.argv)>1 else ASSETS / 'Materials/Play'
SIZE = 512
POSTERS = HERE.parent / 'decokit/art/posters'
STONE = ASSETS / 'OldTownArt/DecoKit/Textures'
rng = np.random.default_rng(117)
yy, xx = np.indices((SIZE,SIZE))
colour = Image.new('RGB',(4*SIZE,4*SIZE))
metal = Image.new('RGBA',(512,512))
lights = Image.new('RGB',(512,512))
normal = Image.new('RGB',(1024,1024),(128,128,255))


def font(size):
    for path in ('/System/Library/Fonts/Supplemental/Futura.ttc','/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'):
        if Path(path).exists(): return ImageFont.truetype(path,size)
    return ImageFont.truetype('DejaVuSans.ttf',size)


def guarded(im):
    # Duplicate the edge through the padding occupied by Model.face. Mip filtering can never
    # borrow a bright ticket poster or a lamp from the neighbouring patch at street distances.
    border=round(SIZE*.035)
    content=im.resize((SIZE-2*border,SIZE-2*border),Image.Resampling.LANCZOS)
    return Image.fromarray(np.pad(np.asarray(content),((border,border),(border,border),(0,0)),mode='edge'))


def store(slot, rgb, metallic, gloss, emission=None, bump=None):
    def put(dst, tile, scale):
        dst.paste(guarded(tile).resize((scale,scale),Image.Resampling.LANCZOS),(slot%4*scale,slot//4*scale))
    put(colour,rgb.convert('RGB'),SIZE)
    values=np.zeros((SIZE,SIZE,4),np.uint8); values[:,:,0]=metallic
    values[:,:,3]=np.clip(gloss,0,255)
    put(metal,Image.fromarray(values),128)
    if emission is not None: put(lights,emission,128)
    if bump is not None: put(normal,bump,256)


def paint(base,texture):
    return Image.fromarray(np.clip(np.array(base)+texture[:,:,None],0,255).astype('uint8'))


limestone=Image.open(STONE/'limestone_polished_v1.png').convert('RGB').resize((SIZE,SIZE))
stone_normal=Image.open(STONE/'limestone_polished_v1_normal.png').convert('RGB').resize((SIZE,SIZE))
grain=np.asarray(limestone.convert('L')).astype(float); grain-=grain.mean()
noise=rng.normal(0,1.7,(SIZE,SIZE))
store(0,paint((178,165,141),grain*1.15),0,99+grain*.40,bump=stone_normal)
# Honed dark sill stone, alternating narrow glazed tile, aged bronze, an oxidised cast band.
store(1,paint((112,103,88),grain*.78),0,134+grain*.43,bump=stone_normal)
tile=(xx%84<3)|(yy%43<3)
tilegrain=rng.uniform(-7,7,(13,8))[yy//43,xx//84]
store(2,paint((66,103,97),noise+tilegrain-tile*16),0,151+tilegrain*.8-tile*48)
store(6,paint((135,114,82),noise*.6+np.sin(yy*.8)*1.7),181,148+noise*.7)
store(7,paint((126,85,70),grain*.5),9,102+noise)
store(11,paint((88,66,49),noise*.7+4*np.sin(yy/4+2*np.sin(xx/99))+3*np.sin(yy/15)),0,114+noise)
store(12,paint((228,218,189),noise*.45),0,152,Image.new('RGB',(SIZE,SIZE),(240,213,163)))
seam=np.minimum(xx%128,127-xx%128)
store(13,paint((90,105,97),noise+grain*.4-7*(seam<3)+4*((seam>=3)&(seam<6))),107,119+noise)

rooms=Image.open(HERE/'art/terminal-rooms.jpg').convert('RGB')
half=rooms.width//2
for slot,i in [(3,0),(4,1),(5,2),(8,3)]:
    x,y=i%2*half,i//2*half
    room=rooms.crop((x+2,y+2,x+half-2,y+half-2)).resize((SIZE,SIZE),Image.Resampling.LANCZOS)
    # A lit rear wall has a weak low-frequency bounce; the brightest practicals persist at night.
    light=room.point(lambda c: max(0,(c-68)*.48)).convert('RGB')
    store(slot,room,0,66,light)
for slot,name in [(9,'skyport'),(10,'jupiter')]:
    # UVs map the square to a 0.875 x 1.30 m sheet in its own brass frame.
    poster=Image.open(POSTERS/(name+'.png')).convert('RGB').resize((SIZE,SIZE),Image.Resampling.LANCZOS)
    store(slot,poster,4,84)

from math import sin,cos,pi
clock=Image.new('RGB',(SIZE,SIZE),(46,58,56)); draw=ImageDraw.Draw(clock)
draw.ellipse((13,13,499,499),fill=(214,208,182),outline=(129,108,76),width=13)
for k in range(60):
    angle=k*pi/30; r=191 if k%5==0 else 211
    draw.line((256+r*sin(angle),256-r*cos(angle),256+225*sin(angle),256-225*cos(angle)),fill=(45,60,57),width=8 if k%5==0 else 3)
draw.line((180,167,256,256,363,203),fill=(41,53,48),width=12)
draw.ellipse((247,247,265,265),fill=(131,87,49))
store(14,clock,21,122,clock.point(lambda v:max(0,(v-105)*.48)))

# Broad enamel fascia and the much smaller brass queue number each get a dedicated UV island.
label=Image.new('RGB',(SIZE,SIZE),(39,66,63)); graphic=Image.new('RGB',(1120,106),(39,66,63))
d=ImageDraw.Draw(graphic);d.rounded_rectangle((4,5,1116,101),radius=8,outline=(174,155,111),width=3)
d.text((560,52),'H A R B O U R   T R A V E L',anchor='mm',font=font(48),fill=(232,220,180))
label.paste(graphic.resize((502,236),Image.Resampling.LANCZOS),(5,10))
ticket=Image.new('RGB',(146,104),(106,92,67));d=ImageDraw.Draw(ticket)
d.rounded_rectangle((3,3,142,100),radius=8,outline=(199,182,138),width=3)
d.text((73,52),'TICKETS',anchor='mm',font=font(24),fill=(240,225,182));label.paste(ticket,(348,385))
store(15,label,42,127,label.point(lambda c:max(0,(c-113)*.40)))
for sheet,suffix in [(colour,'Colour'),(metal,'Metal'),(lights,'Lights'),(normal,'Normal')]:
    sheet.save(OUT/('Ferry'+suffix+'.png'),optimize=True)
