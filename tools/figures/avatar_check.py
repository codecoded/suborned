"""Check tools/figures/avatar.py against avatars Unity itself built (brief 069): Latch's proof wrote Unity's own avatar, with his neutral legs, into
the metas of copies of the game's walk.fbx and four of its figures (out/scratch/latch/char-pipeline/unity/Assets/Proof/FootTest). Each copy is
byte-identical to the game's file, so this module must give the same skeleton from the same bytes. Prints the worst position (mm) and rotation
(degrees) difference per file, legs and the rest apart; exits 1 over the tolerance.
  blender -b --factory-startup -P tools/figures/avatar_check.py -- [<proof FootTest dir>]"""
import sys, os, re, json, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
import avatar as av

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
A = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
D = A[0] if A else os.path.join(ROOT, 'out/scratch/latch/char-pipeline/unity/Assets/Proof/FootTest')
CASES = [('walk_neutralLegs.fbx', 40.0), ('figs/c6_neutralLegs.fbx', None), ('figs/guard_neutralLegs.fbx', None),
         ('figs/cp_commuter_01_neutralLegs.fbx', None), ('figs/cp_commuter_02_neutralLegs.fbx', None)]
TOL_MM, TOL_DEG = 0.5, 0.1


def truth(meta):
    s = open(meta).read(); num = r'(-?[0-9.e+-]+)'
    rows = re.findall(r'\n    - name: (.*)\n      parentName: (.*)\n      position: \{x: ' + num + ', y: ' + num + ', z: ' + num + r'\}\n      rotation: \{x: ' + num + ', y: ' + num + ', z: ' + num + ', w: ' + num + r'\}', s)
    hum = re.findall(r'boneName: (.*)\n      humanName: (.*)', s)
    return {r[0]: (r[1], np.array([float(x) for x in r[2:5]]), np.array([float(x) for x in r[5:9]])) for r in rows}, hum


bad = 0; report = []
for rel, pitch in CASES:
    f = os.path.join(D, rel); stem = os.path.splitext(os.path.basename(f))[0]
    t, th = truth(f + '.meta')
    human, nodes, p = av.neutral_avatar(f, f'{stem}(Clone)', pitch)
    mine = {n.name: n for n in nodes}
    worst = {'legs': [0.0, 0.0, ''], 'rest': [0.0, 0.0, '']}
    for name, (par, pos, rot) in t.items():
        n = mine.get(name)
        if n is None: worst['rest'][2] += f' missing:{name}'; bad += 1; continue
        k = 'legs' if any(name.split(':')[-1] == s + l for s in ('Left', 'Right') for l in av.LEGS) else 'rest'
        dp = float(np.linalg.norm(n.lpos - pos)) * 1000; dq = math.degrees(2 * math.acos(min(1.0, abs(float(np.dot(n.lrot, rot / np.linalg.norm(rot)))))))
        if dp > worst[k][0]: worst[k][0] = dp
        if dq > worst[k][1]: worst[k][1] = dq; worst[k][2] = name
    extra = sorted(set(mine) - set(t))
    hm = sorted(human) == sorted(th)
    ok = hm and not extra and all(w[0] <= TOL_MM and w[1] <= TOL_DEG for w in worst.values())
    bad += 0 if ok else 1
    report.append({'file': rel, 'pitchDeg': round(p, 3), 'humanMapSame': hm, 'humanBones': len(human), 'skeletonRows': len(nodes), 'extraRows': extra,
                   'legs': {'mm': round(worst['legs'][0], 4), 'deg': round(worst['legs'][1], 4), 'worstBone': worst['legs'][2]},
                   'rest': {'mm': round(worst['rest'][0], 4), 'deg': round(worst['rest'][1], 4), 'worstBone': worst['rest'][2]}, 'ok': ok})
print('AVATARCHECK ' + json.dumps({'tolerance': {'mm': TOL_MM, 'deg': TOL_DEG}, 'cases': report}, indent=1))
sys.exit(1 if bad else 0)
