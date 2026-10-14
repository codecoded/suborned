import sys, json, numpy as np
sys.path.insert(0,'/Users/sky/Projects/suborned/out/glint-honest')
import measure as M, deltae as D
from PIL import Image
def lum(m): 
    l=D.lin(np.asarray(m)); return 0.2126*l[0]+0.7152*l[1]+0.0722*l[2]
def summ(name, suffix='-nopeople'):
    r={}
    for v in ('play','009-kerb','009-cafe'):
        for n,m in M.stats(f'/Users/sky/Projects/suborned/out/glint-honest/frames/{name}-{v}{suffix}.png', v).items(): r[(v,n)]=m
    sun=[D.de2000(r[k],np.array(M.tgt(k[1]))) for k in r if k[1].startswith(('sun paving','sun stone'))]
    shp=D.de2000(r[('009-kerb','shade on paving')],np.array(M.T['shade']))
    shr=D.de2000(r[('009-kerb','shade on road')],np.array(M.T['shade on road']))
    ratio=lum(r[('009-kerb','shade on paving')])/lum(r[('009-kerb','sun paving')])
    A=np.asarray(Image.open(f'/Users/sky/Projects/suborned/out/glint-honest/frames/{name}-play{suffix}.png').convert('RGB'))
    clip=(A[...,0]>=246).mean()*100; clipall=(A.min(-1)>=250).mean()*100
    sp=r[('play','sun paving')]; kp=r[('009-kerb','shade on paving')]
    return dict(sun=np.mean(sun), shade=shp, road=shr, ratio=ratio, clipR=clip, sunpave=tuple(sp.astype(int)), shadepave=tuple(kp.astype(int)))
if __name__=='__main__':
    print(f"{'cand':18s} sunΔE shadeΔE roadΔE ratio R>=246 sun paving       shade paving   (painting ratio 0.48)")
    for n in sys.argv[1:]:
        s=summ(n); print(f"{n:18s} {s['sun']:5.1f} {s['shade']:6.1f} {s['road']:6.1f} {s['ratio']:5.2f} {s['clipR']:5.1f}% {s['sunpave']} {s['shadepave']}")
