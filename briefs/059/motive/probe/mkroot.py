# Motive 059 probe roots: the getaway car moved to the service-drive stand (brief 059 §1.4), with a choice of loud police entry.
# Writes out/scratch/motive/059/root-<v>/ (a copy of the engine's inputs with only handover.site.json changed). Never touches the repo's inputs.
import json, os, shutil, sys, math
REPO = os.getcwd(); OUT = 'out/scratch/motive/059'
site0 = json.load(open('sim/data/handover.site.json'))
W = site0['grid']; X0, Y0, C, NX, NY = W['x0'], W['y0'], W['cell'], W['nx'], W['ny']
def free(i, j): return 0 <= i < NX and 0 <= j < NY and W['rows'][i][j] == '.'
def snap(p):
    ci, cj = int((p[0]-X0)//C), int((p[1]-Y0)//C); best = None
    for i in range(ci-8, ci+9):
        for j in range(cj-8, cj+9):
            if free(i, j):
                q = (int(X0+(i+.5)*C), int(Y0+(j+.5)*C)); d = (q[0]-p[0])**2 + (q[1]-p[1])**2
                if best is None or d < best[0]: best = (d, q)
    return list(best[1])
YARD = [6935, -1500]
STANDS = [snap(p) for p in [(5900, -1500), (6935, -350), (6935, -2650), (7500, -2650)]]
ENTRIES = {'north': None, 'apron-south': [8600, -6400], 'official': [7625, -3000], 'apron-bay': [8600, -3100]}
for v, ent in ENTRIES.items():
    root = f'{OUT}/root-{v}'
    if os.path.exists(root): shutil.rmtree(root)
    os.makedirs(f'{root}/contracts/rules'); os.makedirs(f'{root}/contracts/people')
    shutil.copy('out/scratch/motive/059/head/rulebook.json', f'{root}/contracts/rules/rulebook.json')
    shutil.copy('contracts/people/people.rules.json', f'{root}/contracts/people/people.rules.json')
    shutil.copytree('sim/data', f'{root}/sim/data')
    s = json.loads(json.dumps(site0))
    s['places']['getaway-yard'] = {'at': YARD, 'src': 'Motive 059 probe: brief 059 §1.4 service-drive stand'}
    for k, p in enumerate(STANDS): s['places'][f'yard-police-{k+1}'] = {'at': p, 'src': 'Motive 059 probe'}
    s['getaway']['car'] = 'getaway-yard'
    s['getaway']['exit'] = {'polyline': [[8350, -1500], [8350, 1500]], 'w': 12, 'src': 'Motive 059 probe: Apron Road (site +y, block east) from the drive mouth, 30 m'}
    s['fight']['police'] = [f'yard-police-{k+1}' for k in range(4)]
    if ent: s['places']['yard-entry'] = {'at': snap(ent), 'src': 'Motive 059 probe'}; s['fight']['entry'] = 'yard-entry'
    json.dump(s, open(f'{root}/sim/data/handover.site.json', 'w'), indent=1)
    print(v, 'entry', s['places'][s['fight']['entry']]['at'], 'stands', STANDS, [round(math.dist(p, YARD)/100, 1) for p in STANDS])
