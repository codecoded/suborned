"""Printwork, surface swatches and cut stems share one UV sheet across Harbour street trades."""
from math import cos, pi, sin
from pathlib import Path
import sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
OUTPUT = HERE.parents[1] / 'unity/Assets/Suborned/Materials/Play'
BOTANICALS = HERE / 'art/cut-flowers.png'
CELL = 320

def prepare_flowers(source):
    # The four isolated photographic sprays are stock in the florist's buckets.
    src = Image.open(source).convert('RGBA')
    out = Image.new('RGBA', (640, 640))
    for i in range(4):
        x, y = i % 2, i // 2
        half = src.width // 2
        spray = src.crop((x*half, y*half, (x+1)*half, (y+1)*half))
        bounds = spray.getchannel('A').point(lambda a: 255 if a > 32 else 0).getbbox()
        if not bounds:
            raise ValueError('Flower reference has an empty quadrant')
        spray = spray.crop(bounds)
        spray.thumbnail((299, 299), Image.Resampling.LANCZOS)
        out.paste(spray, (x*CELL+(CELL-spray.width)//2, y*CELL+CELL-12-spray.height))
    out.save(BOTANICALS, optimize=True)

def font(size, bold=False):
    return ImageFont.truetype('/System/Library/Fonts/Supplemental/Futura.ttc', size, index=1 if bold else 0)

def lettering():
    img = Image.new('RGB', (CELL,CELL), '#1b4444')
    pen = ImageDraw.Draw(img)
    labels = [('CUT FLOWERS', '#315648'), ('CITRUS MARKET', '#355653'),
              ('HARBOUR POST', '#266361'), ('HARBOUR LINE', '#334f61')]
    for i, (label, ground) in enumerate(labels):
        y = i*80
        pen.rectangle((0,y,CELL,y+79),fill=ground)
        pen.line((19,y+12,301,y+12),fill='#cfb997',width=2)
        pen.line((19,y+67,301,y+67),fill='#cfb997',width=2)
        pen.text((160,y+38),label,fill='#f7efd9',font=font(27,True),anchor='mm',stroke_width=0)
    return img

def gauge():
    img = Image.new('RGB',(CELL,CELL),'#24524e')
    d = ImageDraw.Draw(img)
    for r, c in [(151,'#64766c'),(145,'#e1dcba'),(136,'#ebe2c9')]:
        d.ellipse((160-r,160-r,160+r,160+r),fill=c)
    for i in range(31):
        a = (135 + i*9)*pi/180
        r = 114 if i%5 == 0 else 124
        d.line((160+r*cos(a),160+r*sin(a),160+132*cos(a),160+132*sin(a)),fill='#33504c',width=4 if i%5==0 else 2)
        if i%5 == 0:
            d.text((160+97*cos(a),160+97*sin(a)),str(i//5),font=font(20),fill='#3c4f49',anchor='mm')
    d.text((160,210),'HARBOUR',font=font(19),fill='#56665e',anchor='mm')
    d.text((160,232),'CALIBRATED',font=font(12),fill='#64756c',anchor='mm')
    d.polygon(((150,169),(229,75),(166,174)),fill='#a34332')
    d.ellipse((151,151,169,169),fill='#465950')
    # Opal diffuser sample, outside the dial's circular UV island.
    d.rectangle((10,10,23,23),fill='#f4e8c9')
    return img

def weave(seed):
    y,x=np.mgrid[:CELL,:CELL]
    rng=np.random.default_rng(seed)
    grit=rng.normal(0,1.0,(CELL,CELL))
    bases=['d6cfb5','345e59','87684f','a78c69','9b7952','9baaa4','694b40','cec7b1','ca9253','99534b']
    for i,base in enumerate(bases):
        rgb=np.array([int(base[k:k+2],16) for k in (0,2,4)])[None,None,:]
        grain=grit*(2.5 if i in (2,3,6,7,8,9) else 1)
        if i==2: grain=grain+5*np.sin(y*.51+2*np.sin(x/72))+4*np.sin(y*.22+x/160)
        if i==3: grain=grain+np.where(((x//10+y//7)%2)==0,6,-6)+4*np.sin(y*.85)
        if i==5: grain=grain+2*np.sin(y*2.7)
        if i==6: grain=grain+2*np.cos(x*.72+y*.53)
        if i==7:
            rgb=np.where(((x//40)%2==0)[:,:,None],rgb,np.array([94,131,121]))
            rgb=np.where(((x%80)>75)[:,:,None],np.array([167,121,96]),rgb)
            grain=grain+2.2*np.sin(x*pi/2)+2.2*np.sin(y*pi/2)+6*np.cos(y*pi/155)
        if i in (8,9):
            variants=([(198,127,53),(219,168,66),(184,112,48),(209,150,55)] if i==8
                      else [(118,64,62),(133,79,61),(138,150,82),(113,132,70)])
            rgb=np.array(variants)[(x//(CELL//2)+2*(y//(CELL//2))).clip(0,3)]
            grain=grain+3*np.sin(x*.16)*np.sin(y*.19)
        yield Image.fromarray(np.clip(rgb+grain[:,:,None],0,255).astype(np.uint8))

def bake():
    img=Image.new('RGBA',(CELL*4,CELL*4))
    surfaces=list(weave(118))+[lettering(),gauge()]
    flowers=Image.open(BOTANICALS)
    for i in range(4):
        x,y=i%2*CELL,i//2*CELL
        surfaces.append(flowers.crop((x,y,x+CELL,y+CELL)))
    for i,tile in enumerate(surfaces):
        img.paste(tile,(i%4*CELL,i//4*CELL))
    img.save(OUTPUT/'StreetTradesColour.png', optimize=True)
    response=Image.new('RGBA',(256,256))
    pen=ImageDraw.Draw(response)
    for i,(metal,smooth) in enumerate([(33,110),(42,122),(0,52),(0,38),(165,134),(149,125),(1,61),(0,41),
                                         (0,56),(0,66),(20,97),(56,115),(0,26),(0,30),(0,28),(0,24)]):
        x,y=i%4*64,i//4*64
        pen.rectangle((x,y,x+63,y+63),fill=(metal,0,0,smooth))
    response.save(OUTPUT/'StreetTradesMetal.png')
    lights=Image.new('RGB',(1024,1024))
    pen=ImageDraw.Draw(lights)
    pen.rectangle((3*256+7,2*256+7,3*256+20,2*256+20),fill='#ffddaa')
    # Phosphor on the mechanical dial keeps the needle readable under the awning at dusk.
    pen.line((3*256+122,2*256+136,3*256+181,2*256+62),fill='#55352b',width=2)
    lights.save(OUTPUT/'StreetTradesLights.png',optimize=True)

if __name__=='__main__':
    if len(sys.argv)==2:
        prepare_flowers(sys.argv[1])
    bake()
