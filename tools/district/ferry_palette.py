"""Generate the shared opaque ferry and garden trim sheet. Requires Pillow."""
from pathlib import Path
import random
import sys
from PIL import Image, ImageDraw, ImageFont
from math import sin
root = Path(sys.argv[1])
tile, cols, rows = 128, 4, 4
# sRGB albedo, metallic, smoothness. The same slots are used by every fitted LOD.
paints = [
    ((224,213,182), 0,125), ((174,153,107),24,109), ((26,104,103),53,157), ((42,89,90),74,206),
    ((44,98,99),61,199), ((71,103,94),53,193), ((190,150,85),188,177), ((183,91,68),39,154),
    ((44,80,54),0,61), ((63,106,59),0,77), ((103,137,66),0,72), ((67,55,42),0,66),
    ((226,189,126),0,155), ((116,162,147),63,148), ((210,212,175),20,122), ((35,84,83),43,165)
]
albedo = Image.new('RGB',(tile*cols,tile*rows))
mask = Image.new('RGBA',albedo.size)
light = Image.new('RGB',albedo.size)
rng = random.Random(116)
for i,(colour,metal,gloss) in enumerate(paints):
    p = Image.new('RGB',(tile,tile)); m = Image.new('RGBA',p.size); e = Image.new('RGB',p.size)
    for y in range(tile):
        for x in range(tile):
            bead = rng.uniform(-3,3) if i in (0,1,6,11) else rng.uniform(-1.3,1.3)
            v = bead
            if i in (3,4,5):
                # An opaque recessed window. The cool upper pane catches sky over a darker, warm interior.
                v += 14*(1-y/tile)-12 + 6*sin((x+1.6*y)/39)
            if i in (8,9,10): v += 2*sin(x/7+y/5)
            p.putpixel((x,y),tuple(max(0,min(255,round(k+v))) for k in colour))
            m.putpixel((x,y),(metal,0,0,round(gloss+bead)))
    d=ImageDraw.Draw(p); h=ImageDraw.Draw(e)
    if i in (3,4,5):
        d.rectangle((5,8,122,12), fill=(109,137,120)); d.rectangle((7,96,120,104),fill=(52,78,66))
        d.line((8,104,119,104),fill=(134,135,105),width=2)
        h.rectangle((8,14,119,94),fill=((26,73,68),(43,94,88),(132,101,58))[i-3])
        if i==5:
            d.rectangle((15,63,45,94),fill=(145,126,86)); d.rectangle((64,48,92,93),fill=(110,109,78))
            d.line((8,20,118,20),fill=(187,166,115),width=2);h.line((8,20,118,20),fill=(241,187,104),width=3)
        for xx in (6,120): d.line((xx,8,xx,119),fill=(40,69,67),width=3)
    if i==12:
        h.rectangle((0,0,tile,tile),fill=(237,178,102))
    if i==14:
        # Analogue clock dial viewed from both sides of the inlet.
        from math import cos,pi
        d.ellipse((7,7,121,121),fill=(227,218,177),outline=(167,134,75),width=5)
        h.ellipse((12,12,116,116),fill=(126,110,72))
        for k in range(12):
            a=k*pi/6
            d.line((64+42*sin(a),64-42*cos(a),64+50*sin(a),64-50*cos(a)),fill=(26,81,79),width=3)
        d.line((45,42,64,64,91,51),fill=(23,68,67),width=5)
        d.ellipse((60,60,68,68),fill=(152,75,47))
    if i==15:
        candidates=['/System/Library/Fonts/Supplemental/Futura.ttc','/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf']
        font=next((f for f in candidates if Path(f).exists()),'DejaVuSans.ttf')
        fascia=Image.new('RGB',(600,100),(26,72,73)); fd=ImageDraw.Draw(fascia)
        fd.rounded_rectangle((4,5,595,94),radius=14,outline=(192,163,112),width=5)
        fd.text((300,37),'HARBOUR TRAVEL',font=ImageFont.truetype(font,48),anchor='mm',fill=(247,226,181))
        fd.text((300,72),'RAIL    •    FERRY',font=ImageFont.truetype(font,15),anchor='mm',fill=(215,188,140))
        p.paste(fascia.resize((124,56),Image.Resampling.LANCZOS),(2,3))
        e.paste(fascia.point(lambda v: max(0,int((v-105)*.72))).resize((124,56),Image.Resampling.LANCZOS),(2,3))
        # Two illustrated destination prints and the ticket clerk's enamel route map.
        board=Image.new('RGB',(468,150),(42,73,70)); bd=ImageDraw.Draw(board)
        for x,cream in [(6,(230,211,168)),(154,(225,205,161)),(302,(227,211,170))]:
            bd.rectangle((x,5,x+139,144),fill=cream,outline=(181,142,78),width=3)
        bd.rectangle((14,13,136,99),fill=(91,154,157));bd.ellipse((79,19,112,52),fill=(236,184,98))
        bd.polygon([(20,74),(129,74),(112,88),(43,88)],fill=(39,65,67))
        bd.rectangle((44,60,105,72),fill=(236,215,170));bd.rectangle((66,46,76,60),fill=(164,83,61))
        bd.rectangle((162,13,284,99),fill=(186,122,85))
        bd.polygon([(163,92),(237,36),(284,89)],fill=(106,140,123));bd.line((165,90,280,65),fill=(44,79,76),width=5)
        bd.rounded_rectangle((184,61,257,77),radius=7,fill=(234,212,167));bd.line((192,67,251,67),fill=(54,97,94),width=4)
        for dx,dy,tint in [(0,0,(44,110,105)),(12,11,(177,95,71))]:
            points=[(315+dx,76+dy),(343+dx,43+dy),(377+dx,58+dy),(407+dx,28+dy)]
            bd.line(points,fill=tint,width=5)
            for x,y in points:bd.ellipse((x-4,y-4,x+4,y+4),fill=(239,218,172),outline=tint,width=2)
        for x,label in [(76,'HARBOUR'),(222,'CROWN'),(371,'CONNECTIONS')]:
            bd.text((x,120),label,font=ImageFont.truetype(font,13),fill=(52,74,68),anchor='mm')
        p.paste(board.resize((124,56),Image.Resampling.LANCZOS),(2,69))
    xy=(i%cols*tile,i//cols*tile)
    albedo.paste(p,xy);mask.paste(m,xy);light.paste(e,xy)
for tex,suffix in [(albedo,'Colour'),(mask,'Metal'),(light,'Lights')]:
    tex.save(root/('Ferry'+suffix+'.png'),optimize=True)
