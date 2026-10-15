import sys
from PIL import Image
from pathlib import Path
out=sys.argv[1]; h=int(sys.argv[2]); fs=sys.argv[3:]
ims=[]
for f in fs:
    im=Image.open(f).convert('RGB'); ims.append(im.resize((int(im.width*h/im.height),h)))
W=sum(i.width for i in ims); M=Image.new('RGB',(W,h),'white'); x=0
for i in ims: M.paste(i,(x,0)); x+=i.width
M.save(out,quality=85)
