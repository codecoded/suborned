import sys; sys.path.insert(0,'/Users/sky/Projects/suborned/out/scratch/forma/063')
from cameo_comp import *
from PIL import Image
P='/Users/sky/Projects/suborned/paid/063-portraits/concepts/'
BOX={'vale_A':(120,0,860,740),'vale_B':(50,-10,800,740),'vale_C':(190,0,870,680),
 'inez_A':(120,31,860,771),'inez_B':(70,-30,750,650),'inez_C':(185,0,775,590),
 'driver_A':(25,0,915,890),'driver_B':(120,20,840,740),'driver_C':(110,0,910,800)}
if __name__=='__main__':
    ks=sys.argv[1:] or list(BOX)
    for k in ks:
        im=Image.open(P+k+'.png').convert('RGBA'); cv=Image.new('RGBA',(1536,1024+400),(246,238,214,255)); cv.paste(im,(0,200)); b=BOX[k]; c=cv.crop((b[0],b[1]+200,b[2],b[3]+200)).resize((1024,1024),Image.LANCZOS); c.save(D+f'r_{k}.png')
        cameo(D+f'r_{k}.png',D+f'pc_{k}')
    W=Image.new('RGB',(512*3,512*3),(30,60,58))
    for i,k in enumerate(BOX):
        im=Image.open(D+f'pc_{k}.png'); W.paste(im,((i%3)*512,(i//3)*512),im)
    W.save(D+'pc_sheet.png')
