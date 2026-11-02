"""The reference boards: real screenshots, annotated. Saved only under out/scratch, never in briefs or a commit. Glyph, brief 085."""
import sys
sys.path.insert(0,'.')
from l4_common import *
RF=R+'out/scratch/glyph/refs/'; W_=1000; B=(12,52,52)
ITEMS={1:[('Watch Dogs: Legion · team','legion-operatives',['The game\'s own 3D figure is the page.','Faces in a grid: one click to the next.'],[((60,120,425,470),'Faces, always there'),((430,100,860,700),'The figure dominates')]),
 ('Watch Dogs: Legion · a person','legion-bio',['Same page. Green "recent events" is history.','The metadata jokes are what §09 rejects.'],[((860,215,1200,335),'What happened: keep'),((860,490,1200,625),'Jokes no rule reads: skip')]),
 ('Watch Dogs: Legion · profiler','legion-profiler',['In play: a line joins the person to their','card, over the real world.'],[((560,210,650,460),'Person'),((750,240,1105,455),'Their card')])],
2:[('Desperados III · pause','desperados-pause',['World dimmed. A plain list, one big round','map. Almost nothing else.'],[((725,130,1205,600),'One big round picture')]),
 ('Desperados III · mission info','desperados-quest',['A small panel over the live world.','Icon tabs on top, not words.'],[((200,115,370,165),'Icon tabs'),((140,160,430,550),'Small panel, world stays')]),
 ('Desperados III · loading','desperados-loading',['The style lives in the drawn map,','not in the panel, which stays plain.'],[((720,190,1035,500),'Their world\'s own art')])],
3:[('XCOM 2 · a soldier','xcom-abilities',['Figure on a round stage in the middle.','Dense panels both sides. Locked rows shown.'],[((500,90,790,690),'Figure centre'),((90,190,500,490),'Locked still shown')]),
 ('The Outer Worlds · a faction','outer-reputation',['Figure at the edge, plain panels, word tabs.','A world of posters, the menu is quiet.'],[((400,45,870,75),'Word tabs: not what matters'),((905,160,1100,680),'Own figure')]),
 ('The Outer Worlds · loading','outer-poster',['Retro future comes from the poster art.','It\'s a picture, not a layout system.'],[((415,35,865,615),'One-off art, not a template')])],
4:[('Disco Elysium · character sheet','disco-sheet',['Strongest style of all: painted cards,','a big one forward. Needs a painting each.'],[((230,50,920,660),'A painted card each'),((945,40,1240,420),'One card forward')]),
 ('Phantom Doctrine · agents','phantom-agents',['Spy genre: list + portrait over a dim room.','Busy, and yes, this is the web look.'],[((350,190,525,525),'A list of faces'),((540,130,945,560),'Panels on panels')]),
 ('Hades · a choice','hades-boons',['Figure breaks out over the panel edge.','That overlap is why it isn\'t a web box.'],[((60,150,340,720),'Figure over the frame')])]}
def bar(t,subs):
    f1,f2=font('Jost-Bold',44),font('Jost-Bold',34); im=Image.new('RGB',(W_,70+46*len(subs)+14),B); d=ImageDraw.Draw(im); d.text((26,12),t,font=f1,fill=CREAM)
    for i,s in enumerate(subs): d.text((26,68+i*46),s,font=f2,fill=DIM)
    return im
def shot(n,notes):
    im=Image.open(RF+n+'.jpg').convert('RGB'); k=W_/im.width; im=im.resize((W_,round(im.height*k)),Image.LANCZOS).convert('RGBA'); d=ImageDraw.Draw(im); f=font('Jost-Bold',28)
    for (x0,y0,x1,y1),t in notes:
        box=[x0*k,y0*k,x1*k,y1*k]; d.rounded_rectangle(box,8,outline=(96,224,234,255),width=4); tw=f.getlength(t); lx=min(box[0],W_-tw-22); ly=max(0,box[1]-40) if box[1]>44 else box[1]+4
        d.rounded_rectangle((lx,ly,lx+tw+20,ly+38),8,fill=(12,52,52,255)); d.text((lx+10,ly+2),t,font=f,fill=(247,244,236,255))
    return im.convert('RGB')
for n,its in ITEMS.items():
    ps=[bar(f'Real games, {n} of 4',['Their pictures: reference only.'])]
    for t,f_,subs,notes in its: ps+=[bar(t,subs),shot(f_,notes)]
    H=sum(p.height for p in ps); b=Image.new('RGB',(W_,H),B); y=0
    for p in ps: b.paste(p,(0,y)); y+=p.height
    b.save(RF+f'refs-board-{n}.png',optimize=True); print(n,b.size)
