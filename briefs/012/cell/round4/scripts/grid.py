import sys
from PIL import Image, ImageDraw
out=sys.argv[1]; cols=int(sys.argv[2]); cw=int(sys.argv[3]); ch=int(sys.argv[4]); fs=sys.argv[5:]
rows=(len(fs)+cols-1)//cols
M=Image.new('RGB',(cols*cw,rows*ch),'white'); d=ImageDraw.Draw(M)
for k,f in enumerate(fs):
    im=Image.open(f).convert('RGB'); im.thumbnail((cw,ch)); x=(k%cols)*cw+(cw-im.width)//2; y=(k//cols)*ch
    M.paste(im,(x,y)); d.text(((k%cols)*cw+4,y+2),f.split('/')[-1].split('.')[0][-14:],fill=(255,0,0))
M.save(out,quality=85)
