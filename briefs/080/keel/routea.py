# Route A's margin against the new clock (brief 080 pacing). Scratch: writes variants, plays them through the worktree's engine, prints the table.
import json,os,shutil,subprocess,re,sys
W='/Users/sky/Projects/suborned/out/scratch/keel/wt080'
P='/Users/sky/Projects/suborned/out/scratch/keel/p080'
V=P+'/routea'; os.makedirs(V,exist_ok=True)
base=json.load(open(W+'/contracts/missions/handover/routes/route-a.json')); base.pop('expect',None)
S=W+'/sim/cli/bin/Release/net10.0/suborned-sim.dll'
def write(name,orders):
    d=dict(base); d['orders']=orders; d['until']=5100; json.dump(d,open(f'{V}/{name}.json','w')); return f'{V}/{name}.json'
# 1. the Enter moved later, everything from the loop on with it (the Help before it too)
sweep=[]
for dt in range(0,141,10):
    o=[]
    for x in base['orders']:
        y=dict(x)
        if x['tick']>=850: y['tick']=x['tick']+dt*10
        o.append(y)
    sweep.append((dt,write(f'enter+{dt:03d}',o)))
# 2. the slow bot's pace (briefs/078: Listen 1:40, turned 4:58, call 5:02, entry 5:44.8 on the old talk), new talk: Listen 30, Help 35 twice: Listen 1:40, Help 3:06 and 3:41, call 4:20, entry 5:02.8
tick={('scout','watch'):600,('scout','listen'):1000,('tech','loop'):1970}
slow=[]; turns=[1860,2210]; moved=False
for x in base['orders']:
    y=dict(x); t=x['tick']
    if (x['op'],x['do']) in tick: y['tick']=tick[(x['op'],x['do'])]
    elif x['do']=='turn': y['tick']=turns.pop(0)
    elif t==1050: y['tick']=2300
    elif t==1710: y['tick']=2600
    elif t>=1729: y['tick']=t-1729+3028
    slow.append(y)
slowf=write('slow-bot',slow)
# 3. the 45 s fallback, as a data copy: Vale out 5:05, close 7:45, the city 15 s later than 4:50's, the rivals where they are
A=P+'/alt45'; shutil.rmtree(A,ignore_errors=True); os.makedirs(A+'/sim'); shutil.copytree(W+'/contracts',A+'/contracts'); shutil.copytree(W+'/sim/data',A+'/sim/data')
for f in os.listdir(W):
    if f in ('package.json','CLAUDE.md','AGENTS.md'): shutil.copy(W+'/'+f,A+'/'+f)
s=json.load(open(A+'/sim/data/handover.site.json'))
for k,v in [('chime',285),('driverLast',295),('valeExits',305),('valeAtCar',325),('over',465)]:
    s['schedule'][k]=v
json.dump(s,open(A+'/sim/data/handover.site.json','w'))
p=json.load(open(A+'/sim/data/handover.people.json'))
for x in p['people']:
    for st in x['stops']:
        st[1]+=15; st[2]+=15
json.dump(p,open(A+'/sim/data/handover.people.json','w'))
def spread(cwd,files,n,extra=[]):
    r=subprocess.run(['dotnet',S,'spread',*files,'--seeds',str(n),*extra],cwd=cwd,capture_output=True,text=True)
    if r.returncode: print(r.stderr[:600]); sys.exit(1)
    return r.stdout
def wins(out):
    res={}; cur=None
    for l in out.splitlines():
        m=re.match(r'## ([^:]+):',l)
        if m: cur=m.group(1)
        m=re.match(r'\s+wins (\d+)/(\d+) \(\d+%\) \[([^\]]*)\]',l)
        if m: res[cur]=(int(m.group(1)),int(m.group(2)),m.group(3))
    return res
fs=[f for _,f in sweep]
a=wins(spread(W,fs,60,['--retry-go'])); b=wins(spread(A,fs,60,['--retry-go']))
print('Enter at | Vale 4:50 wins/60 [fails] | Vale 5:05 wins/60 [fails]')
for dt,f in sweep:
    k=os.path.basename(f)[:-5]; E=172.9+dt
    print(f'{int(E)//60}:{int(E)%60:02d} (+{dt}) | {a[k][0]} [{a[k][2]}] | {b[k][0]} [{b[k][2]}]')
for n in (60,200):
    a2=wins(spread(W,[slowf],n,['--retry-go'])); b2=wins(spread(A,[slowf],n,['--retry-go']))
    print(f'slow bot entry 5:02.8, {n} seeds: Vale 4:50 {a2["slow-bot"]} | Vale 5:05 {b2["slow-bot"]}')
print(spread(W,[slowf],60,['--retry-go']).splitlines()[0]); print(spread(A,[slowf],60,['--retry-go']).splitlines()[0])
