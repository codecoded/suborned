import json,sys
from PIL import Image, ImageDraw, ImageFont
D='/Users/sky/Projects/suborned/briefs/028/palm/'
R=D+'render/'
F=lambda s,b=False: ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial Bold.ttf' if b else '/System/Library/Fonts/Supplemental/Arial.ttf', s)
BG=(24,46,46); FG=(247,244,236); DIM=(176,205,199)
def crop_play(k):
    im=Image.open(R+f'play-{k}.png'); return im.crop((90,60,610,860)).resize((390,600))
def close(k): return Image.open(R+f'close-{k}.png').resize((390,390))
def card(draw,x,y,title,lines,w=390):
    draw.text((x,y),title,font=F(19,True),fill=FG)
    for i,l in enumerate(lines): draw.text((x,y+28+i*21),l,font=F(15),fill=DIM)
# ---------- board 1
cols=[('old0','OLD CITY  CoconutPalm','6,310 tris | L1 1,576 | L2 378','old DecoKit city, the boulevard palm (166 in the old street)','L1/L2 are NOT from the old kit'),
('tb2','TESTBED  palm gameplay','1,880 tris (authored LOD 3 of 3)','near 6,102 | close 19,330, all native','imagegen atlas, Oct 1 testbed'),
('tb0','TESTBED  palm close','19,330 tris (the same palm, top level)','15 fronds, 780 leaflets, bent ringed trunk','nothing decimated'),
('ave0','GAME TODAY  Washingtonia','19,190 tris, NO LOD, 803 placed','the avenue palm: 15.6 m, crown 3.6 m','Vela, brief 028, Oct 20'),
('st0','GAME TODAY  CoconutPalm_Street','8,230 tris | L1 2,056 | L2 493','phoenix: 78 placed (lawns, median)','old palm on a taller trunk')]
W=390*5+20*6+0; H=90+600+10+390+110
c=Image.new('RGB',(W,H),BG); d=ImageDraw.Draw(c)
d.text((20,14),'Palms, 2026-10-22: one sun, one camera (play zoom: pitch 38, yaw 30; grey figure = 1.75 m). Blender, geometry and kit materials; the game adds its grade and haze.',font=F(21,True),fill=FG)
d.text((20,48),'Top row: play zoom, same scale.  Row 2: crown close-up.  Sky\'s flyby still: see board 3.',font=F(16),fill=DIM)
for i,(k,t,l1,l2,l3) in enumerate(cols):
    x=20+i*(390+20); y=90
    c.paste(crop_play(k),(x,y)); c.paste(close(k),(x,y+610))
    card(d,x,y+610+400,t,[l1,l2,l3])
c.save(D+'board-1-palms-play-and-close.png')
# ---------- board 2 LODs
info=json.load(open(R+'tris.json'))
rows=[('OLD CITY  CoconutPalm',['old0','old1','old2'],['L0  6,310 (old kit, byte-identical FBX)','L1  1,576 (this repo, 25% collapse)','L2  378 (this repo, 6% collapse)']),
('GAME  CoconutPalm_Street',['st0','st1','st2'],['L0  8,230 (Vela, Oct 20)','L1  2,056 (generator collapse 25%)','L2  493 (generator collapse 6%)']),
('TESTBED  palm',['tb0','tb1','tb2'],['close  19,330 (authored)','near  6,102 (authored)','gameplay  1,880 (authored)']),
('GAME  Washingtonia',['ave0'],['L0  19,190   no L1, no L2'])]
cw=300; W=60+3*cw+3*20+260; H=60+len(rows)*(cw+80)
b=Image.new('RGB',(W,H),BG); d=ImageDraw.Draw(b)
for j,l in enumerate(['Not in the old kit: the old city placed CoconutPalm at its full 6,310 everywhere.','L1 and L2 are this repo generator default collapse (export_kit.py),','adopted in kit.json on Oct 20 (brief 026). Testbed levels are authored.']): d.text((1000,90+j*22),l,font=F(16),fill=DIM)
d.text((20,14),'Distance levels: which exist from the palm\'s own source and which this repo made',font=F(21,True),fill=FG)
for r,(t,ks,caps) in enumerate(rows):
    y=60+r*(cw+80); d.text((20,y),t,font=F(19,True),fill=FG)
    for i,(k,cp) in enumerate(zip(ks,caps)):
        im=Image.open(R+f'close-{k}.png').resize((cw,cw)); x=20+i*(cw+20)
        b.paste(im,(x,y+30)); d.text((x,y+34+cw),cp,font=F(14),fill=DIM)
b.save(D+'board-2-levels.png')
# ---------- board 3 flyby vs game
still=Image.open(D+'ref-flyby-boulevard-old-city.png').convert('RGB'); game=Image.open(D+'ref-game-boulevard-third-now.png').convert('RGB')
w=1100; s=still.resize((w,int(still.height*w/still.width))); g=game.resize((w,int(game.height*w/game.width)))
H=60+max(s.height,g.height)+60
e=Image.new('RGB',(2*w+60,H),BG); d=ImageDraw.Draw(e)
d.text((20,12),'The old city\'s boulevard (Sky\'s flyby still, DK_CoconutPalm in tree pits, 166 in the old street)',font=F(20,True),fill=FG)
d.text((w+40,12),'The game today (third person, Washingtonia avenue palms, same boulevard idea)',font=F(20,True),fill=FG)
e.paste(s,(20,50)); e.paste(g,(w+40,50)); e.save(D+'board-3-flyby-vs-game.png')
print(c.size,b.size,e.size)
