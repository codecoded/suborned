import sys, json, numpy as np
sys.path.insert(0, '/Users/sky/Projects/suborned/out/glint-honest')
from PIL import Image
import deltae as D
reg = json.load(open('/Users/sky/Projects/suborned/out/glint-honest/regions.json'))
# painting targets: medians of plain tiles in the 26 Sep paintings (see paint-targets.txt)
T = {'sun paving': (240,220,201), 'sun stone': (248,227,200), 'shade': (170,161,153), 'sun seafoam': (147,168,150), 'shade on road': (97,97,106)}
def tgt(name):
    if name.startswith('sun paving'): return T['sun paving']
    if name.startswith('sun stone') or name.startswith('sun roof'): return T['sun stone']
    if name.startswith('sun seafoam'): return T['sun seafoam']
    if name.startswith('shade on road'): return T['shade on road']
    if name.startswith('shade'): return T['shade']
SIG = {'cones': '#e03a2f', 'guard': '#f0a020', 'rivals': '#8a4fd0'}
def stats(png, view):
    A = np.asarray(Image.open(png).convert('RGB')).astype(float); out = {}
    for n,(x0,y0,x1,y1) in reg[view].items():
        out[n] = np.median(A[y0:y1, x0:x1].reshape(-1,3), 0)
    return out
def main(prefix, suffix='-nopeople', views=('play','009-kerb','009-cafe'), quiet=False):
    tot=[]; sigmin=999; lines=[]
    for v in views:
        s = stats(f'/Users/sky/Projects/suborned/out/glint-honest/frames/{prefix}-{v}{suffix}.png', v)
        for n,m in s.items():
            t = np.array(tgt(n),float); de = float(D.de2000(m,t)); tot.append(de)
            sd = min(float(D.de2000(m, D.hex2rgb(h))) for h in SIG.values()); sigmin=min(sigmin,sd)
            lines.append(f"{v:9s} {n:18s} ({m[0]:3.0f},{m[1]:3.0f},{m[2]:3.0f}) R-B {m[0]-m[2]:3.0f} | paint ({t[0]:.0f},{t[1]:.0f},{t[2]:.0f}) dE {de:5.1f} | min sig dE {sd:5.1f}")
    if not quiet: print('\n'.join(lines))
    return float(np.mean(tot)), sigmin, lines
if __name__ == '__main__':
    pre = sys.argv[1]; m, sm, _ = main(pre); print(f"mean dE {m:.1f}  min signal dE {sm:.1f}")
