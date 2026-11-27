"""Paint and response in separate maps. One opaque material serves all rigid body parts."""
from pathlib import Path
from math import sin, pi
from PIL import Image, ImageDraw
import random

HERE=Path(__file__).resolve().parent
OUTPUT=HERE.parents[2]/'unity/Assets/Suborned/Scenery/HarbourRobots'
# sRGB paint, metallic, perceptual smoothness, clear coat.
SWATCHES=[
 ('warm ivory',(237,215,176),0,.49,.47),('oxblood enamel',(141,53,43),0,.50,.5),
 ('seafoam enamel',(155,182,154),0,.51,.5),('polished chrome',(200,208,205),.94,.85,0),
 ('brushed aluminium',(167,176,173),.90,.55,0),('satin brass',(184,148,92),.84,.53,0),
 ('tyre',(40,39,37),0,.22,0),('smoked glass',(22,35,34),.18,.76,.55),
 ('case leather',(110,55,39),0,.34,.12),('varnished wood',(162,108,66),0,.43,.26),
 ('coral petal',(233,109,86),0,.29,0),('leaf',(81,111,68),0,.32,0),
 ('porcelain highlight',(244,226,194),0,.52,.43),('cream paper',(224,212,181),0,.24,0),
 ('blued steel',(67,76,75),.78,.50,0),('recess',(39,47,44),.14,.29,0)]


def make():
    OUTPUT.mkdir(parents=True,exist_ok=True);size=128
    colour=Image.new('RGB',(size*4,size*4)); response=Image.new('RGBA',colour.size)
    rng=random.Random(119)
    for idx,(name,rgb,metal,smooth,coat) in enumerate(SWATCHES):
        x0=(idx%4)*size;y0=(idx//4)*size
        for y in range(size):
            grain=rng.uniform(-2.,2.)
            for x in range(size):
                noise=rng.uniform(-1,1)
                grain_colour=noise*1.1
                if name=='varnished wood':grain_colour=5*sin(y*.49+sin(x*.045)*1.3)+grain+noise*1.5
                elif name=='case leather':grain_colour=noise*6+1.8*sin(x*.75)*sin(y*.69)
                elif name=='brushed aluminium':grain_colour=grain*1.4+noise*.8
                shade=tuple(min(255,max(0,round(c+grain_colour)))for c in rgb)
                point=(x0+x,y0+y)
                colour.putpixel(point,shade)
                response.putpixel(point,(round(metal*255),round(coat*255),0,round((smooth+.012*noise)*255)))
    colour.save(OUTPUT/'ServiceColour.png',optimize=True);response.save(OUTPUT/'ServiceSurface.png',optimize=True)
    # Smoked destination glass, drawn at four fixed states. The matrix has physically separate dots even at rest.
    for name,tint in [('Porter',(255,180,72)),('Clerk',(132,232,207))]:
        atlas=Image.new('RGB',(512,48),(13,23,22));draw=ImageDraw.Draw(atlas)
        for state in range(4):
            for row in range(5):
                for col in range(20):
                    tallies=col in (2,3,16,17) and row in (1,3)
                    arrow=5<=col<=9 and abs(row-2)==(9-col)%4 if state in (0,2) else 9<=col<=13 and abs(row-2)==(col-9)%4
                    bars=col in (12,13,14) and row==2 if state in (0,2) else col in (5,6,7) and row==2
                    on=tallies or arrow or bars
                    x=state*128+7+col*6;y=9+row*7
                    draw.ellipse((x,y,x+3,y+3),fill=tint if on else (29,44,40))
        atlas.save(OUTPUT/(name+'Display.png'),optimize=True)


if __name__=='__main__':make()
