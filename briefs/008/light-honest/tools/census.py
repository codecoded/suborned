import sys, json, numpy as np
sys.path.insert(0,'/Users/sky/Projects/suborned/out/glint-honest')
import deltae as D
from PIL import Image
SIG={'cones':'#e03a2f','guard':'#f0a020','rivals':'#8a4fd0'}
res={}
for tag in ('before','after'):
    for v in ('play','009-kerb','009-cafe','aerA'):
        A=np.asarray(Image.open(f'/Users/sky/Projects/suborned/out/glint-honest/frames/{tag}-{v}-nopeople.png').convert('RGB')).reshape(-1,3)[::3].astype(float)
        row={}
        for k,h in SIG.items():
            de=D.de2000(A,np.broadcast_to(D.hex2rgb(h),A.shape))
            row[k]={'pct_lt20':round(float((de<20).mean()*100),3),'min':round(float(de.min()),1),'pct_lt10':round(float((de<10).mean()*100),3)}
        res[f'{tag}/{v}']=row
json.dump(res,open('/Users/sky/Projects/suborned/out/glint-honest/census.json','w'),indent=1)
for k,v in res.items(): print(k, {s:(x['pct_lt20'],x['min']) for s,x in v.items()})
