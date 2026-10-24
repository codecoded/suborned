import sys
from PIL import Image, ImageDraw, ImageFilter
D='/Users/sky/Projects/suborned/out/scratch/forma/063/'
GROUND=(178,174,171); RING=(86,84,83)
def cameo(src, out_base, S=512):
    im=Image.open(src).convert('RGBA').resize((S*2,S*2),Image.LANCZOS)
    g=Image.new('RGBA',im.size,GROUND+(255,))
    # soft top-left light like the roster ground
    ov=Image.new('L',im.size,0); d=ImageDraw.Draw(ov); d.ellipse((-S*0.6,-S*0.9,S*1.6,S*0.9),fill=40); ov=ov.filter(ImageFilter.GaussianBlur(S*0.5))
    g.paste((190,187,184,255),(0,0),ov)
    g.alpha_composite(im)
    W=S*2; m=Image.new('L',(W,W),0); ImageDraw.Draw(m).ellipse((0,0,W-1,W-1),fill=255)
    ring=Image.new('RGBA',(W,W),(0,0,0,0)); rd=ImageDraw.Draw(ring); rd.ellipse((0,0,W-1,W-1),outline=RING+(255,),width=int(W*0.022))
    g.putalpha(m); g.alpha_composite(ring)
    big=g.resize((S,S),Image.LANCZOS); big.save(out_base+'.png')
    g.resize((S//4,S//4),Image.LANCZOS).save(out_base+'@2x.png')   # 128 px, the roster's @2x
    return big
if __name__=='__main__':
    for n in sys.argv[1:]: cameo(D+f'r_{n}.png', D+f'cameo_{n}')
