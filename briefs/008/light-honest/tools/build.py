import sys, json, base64, io, shutil, os, numpy as np
sys.path.insert(0, '/Users/sky/Projects/suborned/out/glint-honest')
from PIL import Image
import deltae as D
ROOT='/Users/sky/Projects/suborned/out/glint-honest'; OUT='/Users/sky/Projects/suborned/briefs/008/light-honest'
reg=json.load(open(ROOT+'/regions.json'))
SAMPLES={
 'sun paving':[(235,214,192),(243,223,204),(245,226,207),(237,217,199)],
 'sun stone':[(246,225,196),(251,228,197),(247,228,204)],
 'shade':[(160,148,137),(162,155,145),(196,184,172),(163,155,158)],
 'sun seafoam':[(147,169,152),(153,186,180)],
 'shade on road':[(97,97,106),(93,96,104),(97,99,110)]}
SRC={'sun paving':'gameplay-plan 235,214,192 / mission-block 243,223,204 / records-office 245,226,207 / inez 237,217,199',
 'sun stone':'gameplay-plan, mission-block, inez (lit cream wall)','shade':'gameplay-plan stone 160,148,137 and paving 162,155,145 / records-office stone 196,184,172 and paving 163,155,158',
 'sun seafoam':'gameplay-plan paving inlay / inez curved band','shade on road':'gameplay-plan, mission-block asphalt'}
MEAN={k:tuple(np.mean(v,0)) for k,v in SAMPLES.items()}
def kind(n):
    if n.startswith('sun paving'): return 'sun paving'
    if n.startswith(('sun stone','sun roof')): return 'sun stone'
    if n.startswith('sun seafoam'): return 'sun seafoam'
    if n.startswith('shade on road'): return 'shade on road'
    return 'shade'
def med(png,box):
    A=np.asarray(Image.open(png).convert('RGB')).astype(float); x0,y0,x1,y1=box; return np.median(A[y0:y1,x0:x1].reshape(-1,3),0)
SIG={'cones':'#e03a2f','guard':'#f0a020','rivals':'#8a4fd0'}
rows=[]
for v,names in reg.items():
    for n,box in names.items():
        if v=='009-cafe' and n.startswith('sun roof'): continue
        b=med(f'{ROOT}/frames/before-{v}-nopeople.png',box); a=med(f'{ROOT}/frames/after-{v}-nopeople.png',box)
        k=kind(n); t=np.array(MEAN[k]); per=[float(D.de2000(a,np.array(s,float))) for s in SAMPLES[k]]
        sb=min(float(D.de2000(b,D.hex2rgb(h))) for h in SIG.values()); sa=min(float(D.de2000(a,D.hex2rgb(h))) for h in SIG.values())
        rows.append(dict(view=v,name=n,kind=k,before=b.round().astype(int).tolist(),after=a.round().astype(int).tolist(),target=t.round().astype(int).tolist(),
            de_b=float(D.de2000(b,t)),de_a=float(D.de2000(a,t)),per_min=min(per),per_max=max(per),sig_b=sb,sig_a=sa))
json.dump(rows,open(OUT+'/surfaces.json','w'),indent=1)
mb=np.mean([r['de_b'] for r in rows]); ma=np.mean([r['de_a'] for r in rows])
sun=[r for r in rows if r['kind'] in('sun paving','sun stone')]
print('mean dE all', round(mb,1), round(ma,1), '| sun only', round(np.mean([r['de_b'] for r in sun]),1), round(np.mean([r['de_a'] for r in sun]),1))
for r in rows: print(f"{r['view']:9s} {r['name']:22s} before {r['before']} after {r['after']} target {r['target']} dE {r['de_b']:.1f} -> {r['de_a']:.1f} (per painting {r['per_min']:.1f}-{r['per_max']:.1f}) sig {r['sig_b']:.1f} -> {r['sig_a']:.1f}")
