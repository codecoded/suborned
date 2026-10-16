# Glyph, brief 014: patch the old HUD out of the 013 play shots so the "after" mock-ups sit on the same moments. Scratch; same-camera shots fill each other, the rest is inpainted.
import json, cv2, numpy as np, glob, os
R='/Users/sky/Projects/suborned'
rects=json.load(open(R+'/briefs/014/glyph/before.rects.json'))
extra={ # old world tags the new HUD replaces (x,y,w,h)
 '02':[(748,428,64,130),(928,488,50,50),(1100,452,50,26),(485,585,30,30)],
 '03':[(846,476,52,26),(870,550,70,34)],
 '04':[(846,476,52,26),(870,550,70,34)],
 '05':[(888,636,86,138)],
 '06':[(876,684,70,56)],
 '07':[(975,262,60,70),(972,378,66,26)],
 '08':[(756,688,66,34)],
 '09':[(876,676,70,64),(896,636,56,28)],
 '10':[(878,610,106,150),(922,732,58,28)],
 '11':[(878,610,106,150),(922,732,58,28),(752,654,410,26)],
 '01':[(712,700,80,40)],
}
files=sorted(glob.glob(R+'/briefs/013/play/*.png'))
ims={os.path.basename(f)[:2]:cv2.imread(f) for f in files}
H,W=next(iter(ims.values())).shape[:2]
def mask_of(k):
    m=np.zeros((H,W),np.uint8)
    name=[n for n in rects if n.startswith(k)][0]
    for r in rects[name]['rects']: cv2.rectangle(m,(int(r['x'])-5,int(r['y'])-5),(int(r['x']+r['w'])+5,int(r['y']+r['h'])+5),255,-1)
    for (x,y,w,h) in extra.get(k,[]): cv2.rectangle(m,(x,y),(x+w,y+h),255,-1)
    # keep the roster column and status pill area: the after HUD redraws them, but their old pixels are patched too
    return m
masks={k:mask_of(k) for k in ims}
groups=[['06','09','10','11'],['03','04']]
out={}
for k in ims:
    img=ims[k].copy(); m=masks[k].copy()
    g=[g for g in groups if k in g]
    if g:
        others=[o for o in g[0] if o!=k]
        stack=np.stack([ims[o] for o in others]).astype(np.float32); valid=np.stack([(masks[o]==0) for o in others])
        cnt=valid.sum(0)
        acc=np.where(valid[...,None],stack,0).sum(0)/np.maximum(cnt,1)[...,None]
        fill=(m>0)&(cnt>0)
        img[fill]=acc[fill].astype(np.uint8); m[fill]=0
    # inpaint what is left: coarse (large holes) with NS then smooth seam
    img=cv2.inpaint(img,m,7,cv2.INPAINT_TELEA)
    out[k]=img; cv2.imwrite(f'bg/{k}.png',img)
print('ok',list(out))
