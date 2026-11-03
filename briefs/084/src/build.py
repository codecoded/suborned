# 084: builds briefs/084/cues.html from the seed 108 dump, the reel's camera path and the stitched backdrop. Inputs live in out/scratch/glyph/084 (made by mosaic*.py).
import json,base64,csv,os
R='/Users/sky/Projects/suborned/'; O=R+'out/scratch/glyph/084/'; K=R+'unity/Assets/Suborned/UI/Resources/Kit/'; H=R+'briefs/084/src/'
d=json.load(open(O+'seed-A.dump.json')); M=json.load(open(O+'mosaic.json')); offs=json.load(open(O+'offs.json'))
rows=list(csv.DictReader(open(R+'briefs/083/fight/oldtown-seed108.frames.csv'))); T={int(r['frame']):int(r['tick']) for r in rows}
def tick(f):
    while f not in T and f>0: f-=1
    return T.get(f,2655)
t0,t1=2655,3054
def clip(e):
    sp=[s for s in e['spans'] if s[0]<=t1 and s[0]+((len(s)-3)//2)*e['step']>=t0]
    if not sp: return None
    # keep whole span decoding simple: positions per step, from the last sample before t0
    out=[]
    for s in sp:
        if s[0]>=t0: out.append(s); continue
        k=(t0-s[0])//e['step']; x=s[1]+sum(s[3:3+2*k:2]); y=s[2]+sum(s[4:4+2*k:2])
        out.append([s[0]+k*e['step'],x,y]+s[3+2*k:])
    o={k:e[k] for k in('id','label','side','cls','step','hpMax')}; o['spans']=out; o['st']=e['st']; o['hp']=[v for p in zip(*[iter(e['hp'])]*2) for v in p]; return o
ents=[c for c in (clip(e) for e in d['entities']) if c]
log=[l for l in d['log'] if l[0]>=t0-400]
img={}
def f(p,mime='image/png'): return f'data:{mime};base64,'+base64.b64encode(open(p,'rb').read()).decode()
for n in 'eye_shut eye_open eye_unk sawyou fire radio s_moving lock pointer'.split(): img[n]=f(K+f'icons/singles/{n}@2x.png')
for n in 'npc_patrol npc_watcher npc_passer signal_guard signal_rival signal_police'.split(): img[n]=f(K+f'badges/{n}@2x.png')
for n in 'plate_dark disc_dark_24 disc_light_24'.split(): img[n]=f(K+f'markers/{n}@2x.png')
for c in 'scout fixer tech enforcer'.split(): img['cameo_'+c]=f(K+f'portraits/cameo_{c}_normal@2x.png')
img['crosshair']=f(K+'overlays/crosshair@2x.png') if os.path.exists(K+'overlays/crosshair@2x.png') else img['lock']
img['mosaic']=f(O+'mosaic.jpg','image/jpeg')
cam=[[tick(o[0]),o[1],o[2]] for o in offs if o[0]<1195]
data={'t0':t0,'t1':t1,'reg':{'bx':M['bx'],'by':M['by'],'minx':M['minx'],'miny':M['miny']},'ents':ents,'shots':d['shots'],'log':log,'windows':[w for w in d['windows']],'cars':d['cars'],'cameras':d['cameras'],'cam':cam,'img':img}
# who has a cell agent inside their sight, from the dump's own fans: [who, agent, from, to]
def track(e):
    out={}
    for s in e['spans']:
        x,y,t=s[1],s[2],s[0]; out[t]=(x,y)
        for i in range(3,len(s),2):
            nx,ny=x+s[i],y+s[i+1]
            for k in range(1,e['step']+1): out[t+k]=(x+(nx-x)*k/e['step'],y+(ny-y)*k/e['step'])
            x,y=nx,ny; t+=e['step']
    return out
def inside(p,P):
    x,y=p; c=False
    for i in range(len(P)):
        a,b=P[i],P[(i+1)%len(P)]
        if (a[1]>y)!=(b[1]>y) and x<(b[0]-a[0])*(y-a[1])/(b[1]-a[1])+a[0]: c=not c
    return c
EN={e['id']:e for e in d['entities']}; CELLT={c:track(EN[c]) for c in('scout','fixer','tech','enforcer')}; seen=[]
for fn in d['fans']:
    who=fn['id'].split(':')[-1]; ks=fn['keys']
    for i,k in enumerate(ks):
        hi=ks[i+1][0] if i+1<len(ks) else t1+1; on,_,poly=k[1].partition(';')
        if on=='0' or not poly: continue
        v=[int(q) for q in poly.split(',')]; P=[(v[n]*10,v[n+1]*10) for n in range(0,len(v)-1,2)]
        for t in range(max(k[0],t0),min(hi,t1+1)):
            for c,tr in CELLT.items():
                q=tr.get(t)
                if q is None or not inside(q,P): continue
                last=next((s for s in reversed(seen) if s[0]==who and s[1]==c),None)
                if last and t-last[3]<=5: last[3]=t
                else: seen.append([who,c,t,t])
data['seen']=seen
def clipfan(f):
    ks=[k for k in f['keys'] if k[0]<=t1]; lead=[k for k in ks if k[0]<t0][-1:]
    return {'id':f['id'],'kind':f['kind'],'keys':lead+[k for k in ks if k[0]>=t0]}
data['fans']=[clipfan(f) for f in d['fans'] if f['keys'] and f['kind'] in('camera','guard','patrol')]
html=open(H+'cues.tpl.html').read().replace('%%FONT%%',f(K+'fonts/Jost-Bold.ttf','font/ttf')).replace('%%DATA%%',json.dumps(data,separators=(',',':')).replace('</','<\\/')).replace('%%JS%%',open(H+'cues.js').read())
open(R+'briefs/084/cues.html','w').write(html); print(len(html)//1024,'KB',len(ents),'ents')
