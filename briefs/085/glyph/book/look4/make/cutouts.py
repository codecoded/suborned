"""Figures cut from our own art with GrabCut: the paintings and the cell portraits. Saved to out/scratch only. Glyph, brief 085."""
import numpy as np, cv2
from PIL import Image
R='/Users/sky/Projects/suborned/'; O=R+'out/scratch/glyph/look4/'
srcs={'waitress':('briefs/063/cameos/inez_A.png',None),'driver':('briefs/063/cameos/driver_A.png',None),'councillor':('briefs/063/cameos/vale_C.png',None)}
for r in ['scout','fixer','tech','enforcer']: srcs[r]=(f'briefs/033/portraits/{r}_states.png',(90,70,650,630))
for k,(p,c) in srcs.items():
    im=Image.open(R+p).convert('RGB')
    if c: im=im.crop(c)
    im=im.resize((1024,1024),Image.LANCZOS); a=np.array(im)[:,:,::-1].copy(); m=np.full(a.shape[:2],cv2.GC_PR_FGD,np.uint8)
    b=26; m[:b,:]=cv2.GC_BGD; m[:,:b]=cv2.GC_BGD; m[:,-b:]=cv2.GC_BGD
    m[:300,:150]=cv2.GC_BGD; m[:300,-150:]=cv2.GC_BGD; m[420:640,360:660]=cv2.GC_FGD; m[800:,380:640]=cv2.GC_FGD
    bg,fg=np.zeros((1,65)),np.zeros((1,65)); cv2.grabCut(a,m,None,bg,fg,6,cv2.GC_INIT_WITH_MASK)
    al=np.where((m==cv2.GC_FGD)|(m==cv2.GC_PR_FGD),255,0).astype(np.uint8); al=cv2.morphologyEx(al,cv2.MORPH_OPEN,np.ones((9,9),np.uint8))
    n,l,st,_=cv2.connectedComponentsWithStats(al); keep=1+np.argmax(st[1:,cv2.CC_STAT_AREA]); al=np.where(l==keep,255,0).astype(np.uint8); al=cv2.GaussianBlur(al,(0,0),1.6)
    o=np.dstack([a[:,:,::-1],al]); Image.fromarray(o).save(O+f'cut-{k}.png')
W=Image.new('RGB',(1400,400),(12,52,52))
for i,k in enumerate(srcs):
    c=Image.open(O+f'cut-{k}.png').resize((200,200)); W.paste(c,(i*200,0),c)
    W2=c.copy(); bg=Image.new('RGB',(200,200),(247,244,236)); bg.paste(W2,(0,0),W2); W.paste(bg,(i*200,200))
W.save(O+'cuts.png')
