import sys
sys.path.insert(0,'.')
from l4_common import *
W_=1000; B=(12,52,52)
def pic(n,w=W_): im=Image.open(OUT4+n+'.png').convert('RGB'); return im.resize((w,round(im.height*w/im.width)),Image.LANCZOS)
def crop(n,box,w=W_): im=Image.open(OUT4+n+'.png').convert('RGB').crop(box); return im.resize((w,round(im.height*w/im.width)),Image.LANCZOS)
def bar(t,subs=(),tag=None):
    f1,f2=font('Jost-Bold',50),font('Jost-Bold',34); im=Image.new('RGB',(W_,84+48*len(subs)+12),B); d=ImageDraw.Draw(im); d.text((30,16),t,font=f1,fill=CREAM)
    for i,s in enumerate(subs): d.text((30,82+i*48),s,font=f2,fill=DIM)
    if tag:
        tw=f2.getlength(tag); d.rounded_rectangle((W_-tw-66,18,W_-24,78),14,outline=TURQ,width=4); d.text((W_-tw-45,26),tag,font=f2,fill=TURQ)
    return im
def grid(ns,cols=2):
    w=(W_-8*(cols-1))//cols; h=round(w*9/16); rows=-(-len(ns)//cols); im=Image.new('RGB',(W_,rows*(h+8)),B)
    for i,n in enumerate(ns): im.paste(pic(n,w),((i%cols)*(w+8),(i//cols)*(h+8)))
    return im
def stack(ps,name):
    H=sum(p.height for p in ps); b=Image.new('RGB',(W_,H),B); y=0
    for p in ps: b.paste(p,(0,y)); y+=p.height
    b.save(OUT4+name,optimize=True); print(name,b.size)
def rest(o,nm): return grid([f'{o}-{nm}-{k}' for k in ['facts12','unknown','agent','list8','list200','cash']])
stack([bar('The book, after looking at real games',['Inez, just turned. Real type, real facts.','Which one had the promise?']),bar('The light page',['Your round two B, calmed down.'],tag='Pick this'),pic('L-light-inez'),bar('The close-up',['Our real figure, as Legion and XCOM do.'],tag='Second'),pic('C-closeup-inez'),bar('The deck',['Round three\'s B, as real cards.']),pic('B-deck-inez')],'board.png')
stack([bar('Light page: the job, and reading it'),pic('L-light-job'),crop('L-light-inez',(980,60,1620,680)),bar('It holds anything, no redesign',['12 facts, mostly "?", an agent, 8 people,','200 people, a new Cash section.']),rest('L','light')],'board-2-light.png')
stack([bar('Close-up: the job, and reading it'),pic('C-closeup-job'),crop('C-closeup-inez',(980,60,1620,680)),bar('It holds anything, no redesign',['12 facts, mostly "?", an agent, 8 people,','200 people, a new Cash section.']),rest('C','closeup')],'board-3-closeup.png')
stack([bar('Deck: the job, and reading it'),pic('B-deck-job'),crop('B-deck-inez',(700,110,1340,720)),bar('It holds anything, no redesign',['12 facts, mostly "?", an agent, 8 people,','200 people, a new Cash section.']),rest('B','deck')],'board-4-deck.png')
