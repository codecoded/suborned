import sys
sys.path.insert(0,'.')
from l4_common import *
KS = ['inez','job','facts12','unknown','agent','list8','list200','cash']
def sheet(o,nm):
    w,h=570,321; S_=Image.new('RGB',(1920,1080),(12,52,52)); d=ImageDraw.Draw(S_); f=font('Jost-Bold',24)
    for i,k in enumerate(KS):
        x,y=60+(i%3)*(w+30),4+(i//3)*(h+30); d.text((x,y-2),PAGES[k][0],font=f,fill=CREAM); S_.paste(Image.open(OUT4+f'{o}-{nm}-{k}.png').convert('RGB').resize((w,h),Image.LANCZOS),(x,y+26))
    f2=font('Jost-Bold',30); x,y=60+2*(w+30),4+2*(h+30)
    for i,t in enumerate(['Same code each time.','Only the data changes.','Nothing here is placed by hand.']): d.text((x+10,y+90+i*50),t,font=f2,fill=DIM)
    S_.save(OUT4+f'{o}-{nm}-sheet.png')
for o,n in (('B','deck'),('L','light'),('C','closeup')): sheet(o,n)
