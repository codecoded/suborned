# Contact boards from the check's pictures: python3 testbed/hud-style/prep/boards.py  (after bun run hud-style:check)
from PIL import Image, ImageDraw, ImageFont
O='/Users/sky/Projects/suborned/briefs/046/'
F=ImageFont.truetype('/Users/sky/Projects/suborned/unity/Assets/Suborned/UI/Resources/Kit/fonts/Jost-Bold.ttf',28)
names={'built':'As built 0.2.56','revisedA':'Revised A','creamPlate':'Cream plate','seafoamEnamel':'Seafoam enamel','creamTealDiscs':'Cream with teal discs','inkIvory':'Ink and ivory'}
rows=[]
for p,n in names.items():
    a=Image.open(O+f'crop2x-{p}-pills.png').convert('RGB'); b=Image.open(O+f'crop2x-{p}-chips.png').convert('RGB')
    w=1000; a=a.resize((w,int(a.height*w/a.width))); b=b.resize((w,int(b.height*w/b.width)))
    r=Image.new('RGB',(2020,max(a.height,b.height)+44),(247,244,236)); d=ImageDraw.Draw(r); d.text((10,6),n,font=F,fill=(23,48,47)); r.paste(a,(0,44)); r.paste(b,(1010,44)); rows.append(r)
s=Image.new('RGB',(2020,sum(r.height for r in rows)),(247,244,236)); y=0
for r in rows: s.paste(r,(0,y)); y+=r.height
s.save(O+'board-presets.png')
STY=['fill','rim','ticks','disc','line','wipe','pulse']; LAB=['Fill sweep','Rim fill','Segmented ticks','Radial on the disc','Underline','Word wipe','Charge glow']
for p,out in [('creamPlate','board-hold-cream.png'),('revisedA','board-hold-dark.png')]:
    ims=[Image.open(O+f'holdstrip-{p}-{a}.png').convert('RGB') for a in STY]; w,h=ims[0].size
    s=Image.new('RGB',(w*4,(h+44)*2),(247,244,236)); d=ImageDraw.Draw(s)
    for i,(im,n) in enumerate(zip(ims,LAB)):
        x=(i%4)*w; y=(i//4)*(h+44); d.text((x+10,y+6),n+'  (0, 25, 50, 75, 100 %)',font=F,fill=(23,48,47)); s.paste(im,(x,y+44))
    s.save(O+out)
s=Image.new('RGB',(2400,380*2+44),(247,244,236)); d=ImageDraw.Draw(s)
for r,p in enumerate(['revisedA','creamPlate']):
    for i,c in enumerate(['drawn','fired','carrying','violence','known','seen']):
        s.paste(Image.open(O+f'why-{p}-{c}.png').convert('RGB').resize((400,386)),(i*400,r*380+44))
        if r==0: d.text((i*400+10,6),c,font=F,fill=(23,48,47))
s.save(O+'board-why.png')
# roster board: the three layouts and every state, two presets
rows=[]
for p in ['revisedA','creamPlate']:
    im=Image.open(O+f'{p}-roster.png').convert('RGB').crop((0,40,1800,640)); r=Image.new('RGB',(1800,644),(247,244,236)); ImageDraw.Draw(r).text((10,4),names[p],font=F,fill=(23,48,47)); r.paste(im,(0,44)); rows.append(r)
s=Image.new('RGB',(1800,sum(r.height for r in rows)),(247,244,236)); y=0
for r in rows: s.paste(r,(0,y)); y+=r.height
s.save(O+'board-roster.png'); 

# ---- brief 046, Sky's picks 2026-10-22: roster without numbers, subtle under-fire cue, ground markers behind the agent
def lab(im,text,h=44):
    r=Image.new('RGB',(im.width,im.height+h),(247,244,236)); ImageDraw.Draw(r).text((10,6),text,font=F,fill=(23,48,47)); r.paste(im,(0,h)); return r
a=Image.open(O+'nonum-roster-2x.png').convert('RGB'); a=a.resize((int(a.width*0.62),int(a.height*0.62)))
b=Image.open(O+'nonum-states-2x.png').convert('RGB'); b=b.resize((int(b.width*0.62),int(b.height*0.62)))
s=Image.new('RGB',(a.width+b.width+30,max(a.height,b.height)+44),(247,244,236)); d=ImageDraw.Draw(s)
d.text((10,6),'Rail without numbers (left: with numbers beside it, 54 px; this one 44 px) and every state',font=F,fill=(23,48,47)); s.paste(a,(0,44)); s.paste(b,(a.width+30,44)); s.save(O+'board-roster-nonum.png')
STY=[('asis','As built (the problem)'),('arc','A  Arc on the ring'),('notch','B  Notch on the ring'),('streak','C  Streak in'),('ripple','D  Ripple')]
W,H=260,220
s=Image.new('RGB',(W*8,(H+34)*5+44),(247,244,236)); d=ImageDraw.Draw(s)
d.text((10,6),'Animation frames (0, 220, 440, 660 ms): pavement, then asphalt. Shooter behind-right (40 deg).',font=F,fill=(23,48,47))
for r,(st,name) in enumerate(STY):
    y=44+r*(H+34); d.text((10,y+2),name,font=F,fill=(23,48,47))
    for k,(g,t) in enumerate([(g,t) for g in ('pave','asph') for t in (0,220,440,660)]):
        im=Image.open(O+f'arrowfr-{st}-{g}-{t}.png').convert('RGB').resize((W,H)); s.paste(im,(k*W,y+34))
s.save(O+'board-arrows-frames.png')
tw=330; s=Image.new('RGB',(tw*3,int((tw*0.964+34)*5+44)),(247,244,236)); d=ImageDraw.Draw(s)
d.text((10,6),'Play zoom, the person about 65 px tall (captured at 3x, shown at 2x): pavement | asphalt | not selected',font=F,fill=(23,48,47))
for r,(st,name) in enumerate(STY):
    y=44+int(r*(tw*0.964+34)); d.text((10,y+2),name,font=F,fill=(23,48,47))
    for k,g in enumerate(('pave','asph','noring')):
        im=Image.open(O+f'arrow1x-{st}-{g}.png').convert('RGB'); im=im.resize((tw,int(im.height*tw/im.width))); s.paste(im,(k*tw,y+34))
s.save(O+'board-arrows-1x.png')
s=Image.new('RGB',(2020,3*(512+44)),(247,244,236)); d=ImageDraw.Draw(s)
for r,(c,t) in enumerate([('head','Cause disc over the head (and in the roster tab)'),('arc','Cause disc riding on the cue'),('portrait','Portrait only: the roster tab, nothing over the agent')]):
    im=Image.open(O+f'cue-{c}.png').convert('RGB'); im=im.resize((1008,512)); y=r*556; d.text((10,y+6),t,font=F,fill=(23,48,47)); s.paste(im,(0,y+44))
s=s.crop((0,0,1020,s.height)); s.save(O+'board-cue-place.png')
s=Image.new('RGB',(480*4,300+44),(247,244,236)); d=ImageDraw.Draw(s); d.text((10,6),'Ground markers: drawn over the agent (0.2.56) | decal behind the agent, on pavement and on asphalt',font=F,fill=(23,48,47))
for k,n in enumerate(['pave-off','pave-on','asph-off','asph-on']): s.paste(Image.open(O+f'occ-{n}.png').convert('RGB'),(k*480,44))
s.save(O+'board-ground-occlusion.png'); print('boards ok')
