"""Print the four-colour Harbour market and news trade sheet onto a shared 1k atlas."""
from pathlib import Path
import math
import random
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
FONTS = ROOT / "unity/Assets/Suborned/UI/Resources/Kit/fonts"
DEST = ROOT / "unity/Assets/Suborned/Resources/StreetLife/HarbourPrint.png"
SIZE = 256
INK, RED, TEAL = "#363937", "#a64833", "#265b58"
PAPER, KRAFT = "#f1e5cb", "#bf905d"
COLOURS = [KRAFT, "#ab794b", "#fff0ce", "#ffefcd", PAPER, "#e8dcc3", "#eddfb7", "#d5c08b",
           "#debd8e", "#265b58", "#72533b", "#e78b34", "#527849", "#cfaf73", "#963e52", "#514231"]
sheet = Image.new("RGB", (SIZE*4, SIZE*4))


def font(size, weight="Medium"):
    return ImageFont.truetype(str(FONTS / f"Jost-{weight}.ttf"), size)


def lettering(draw, y, text, size, colour=INK, weight="Medium"):
    draw.text((128,y),text,font=font(size,weight),anchor="mt",fill=colour)


def bands(draw, y, width, colour, thickness=2):
    draw.line((128-width//2,y,128+width//2,y),fill=colour,width=thickness)


def skyline(draw):
    draw.rectangle((22,79,234,151),fill="#b3b9a5")
    for x in range(23,233,5):
        h = 8+int(17*(.5+.5*math.sin(x*1.63)))
        draw.rectangle((x,140-h,x+4,143),fill="#758881")
    for x,w,h in ((55,22,41),(94,24,32),(130,25,48),(168,31,38),(197,19,26)):
        draw.rectangle((x-w//2,142-h,x+w//2,143),fill="#495c59")
        draw.rectangle((x-w//3,136-h,x+w//3,143),fill="#495c59")
        draw.line((x,131-h,x,138-h),fill=TEAL,width=2)
        for cx in range(x-w//3,x+w//3,4):
            draw.line((cx,144-h,cx,137),fill="#b6b7a5")
    draw.arc((-40,115,294,204),191,343,fill=PAPER,width=4)
    draw.arc((-40,115,294,204),194,342,fill=INK,width=1)
    for k in range(5):
        draw.line((25+k*40,146,44+k*40,146),fill=TEAL)


def columns(draw, top, bottom, cols, seed):
    rng=random.Random(seed)
    width=214//cols
    for i in range(cols):
        x=22+i*width
        for y in range(top,bottom,4):
            draw.line((x,y,x+rng.randint(width*2//3,width-7),y),fill="#706d60")
            if (y-top)%24==0: draw.line((x,y+1,x+width//2,y+1),fill=INK)


for tile, base in enumerate(COLOURS):
    im=Image.new("RGB",(SIZE,SIZE),base); d=ImageDraw.Draw(im)
    if tile==0:
        d.rounded_rectangle((18,26,238,231),radius=16,outline=TEAL,width=3)
        bands(d,64,169,TEAL); bands(d,189,169,TEAL)
        lettering(d,41,"HARBOUR  •  PRODUCE",13,TEAL,"Bold")
        d.ellipse((98,77,155,134),outline=TEAL,width=4)
        d.arc((114,64,154,91),188,335,fill=TEAL,width=5)
        lettering(d,143,"FRUIT MARKET",23,TEAL,"Bold")
        lettering(d,199,"GROWN ON THE TERRACES",11,TEAL)
    elif tile==1:
        d.line([(13,12),(117,118),(14,244)],fill="#916842",width=2)
        d.line([(240,12),(139,118),(240,244)],fill="#c59866",width=2)
    elif tile in (2,3):
        d.rounded_rectangle((8,9,248,247),radius=14,outline=TEAL,width=7)
        d.rounded_rectangle((17,19,239,237),radius=9,outline=RED,width=2)
        lettering(d,30,"HARBOUR MARKET",17,TEAL,"Bold")
        bands(d,60,166,RED,2); lettering(d,75,"CITRUS" if tile==2 else "PLUMS",43,TEAL,"Bold")
        lettering(d,125,"3¢" if tile==2 else "2¢",68,RED,"Bold")
        lettering(d,207,"EACH" if tile==2 else "PER PAIR",15,TEAL,"Bold")
    elif tile==4:
        bands(d,12,215,RED,3); lettering(d,20,"THE HARBOUR",24,INK,"Bold")
        lettering(d,45,"EVENING POST",27,INK,"Bold"); skyline(d)
        lettering(d,161,"FERRY LIGHTS RETURN",18,INK,"Bold")
        columns(d,188,238,3,17)
    elif tile==5:
        lettering(d,17,"HARBOUR EXCHANGE",19,TEAL,"Bold"); bands(d,48,214,TEAL,3)
        columns(d,61,222,3,41); bands(d,232,215,RED)
    elif tile in (6,7):
        d.rounded_rectangle((9,16,247,240),radius=18,outline=TEAL,width=6)
        lettering(d,37,"PNEUMATIC POST",22,TEAL,"Bold"); bands(d,78,190,RED)
        for i in range(3):
            d.line((44,108+i*12,108,108+i*12),fill=RED,width=4)
        d.ellipse((104,87,164,146),outline=RED,width=4)
        d.polygon([(115,130),(140,99),(151,130)],fill=TEAL)
        lettering(d,166,"COLLECTED" if tile==7 else "DISPATCH",27,TEAL,"Bold")
    # Fine scale and fibre variation keep areas of paper, ink and enamel legible in grazing light.
    rng=random.Random(1934+tile); pix=im.load()
    for y in range(SIZE):
        for x in range(SIZE):
            grain=rng.choice((-3,-2,-1,0,0,1,2,3))
            pix[x,y]=tuple(max(0,min(255,c+grain)) for c in pix[x,y])
    sheet.paste(im,((tile%4)*SIZE,(tile//4)*SIZE))
DEST.parent.mkdir(parents=True,exist_ok=True)
sheet.save(DEST,optimize=True)
