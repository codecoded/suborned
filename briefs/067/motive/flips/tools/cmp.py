import json,sys
F='/Users/sky/Projects/suborned/out/scratch/motive/067f/logs/'
def load(f):
    out={}
    for l in open(F+f):
        l=l.strip()
        if not l.startswith('{'): continue
        d=json.loads(l); out[d['id']]=d
    return out
a=load(sys.argv[1]); b=load(sys.argv[2])
def clk(t): return f"{t//600}:{(t%600)/10:04.1f}"
def s(x):
    if 'error' in x: return 'ERROR '+x['error'][:30]
    return f"{x['outcome']} {x.get('fail') or ''} {clk(x['tick'])} {x['trail']}T A{x['alert']}"
print(f"{'route':34} {sys.argv[3]:40} {sys.argv[4]:40}")
for k in sorted(a):
    x=a[k]; y=b.get(k,{'error':'missing'})
    sx=s(x); sy=s(y)
    mark='' if (x.get('outcome'),x.get('fail'))==(y.get('outcome'),y.get('fail')) else '  <== outcome'
    if sx!=sy or (len(sys.argv)>5): print(f"{k:34} {sx:40} {sy:40}{mark}")
