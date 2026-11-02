"""Brief 090 (Chime): picks the Larthi options and the Milena auditions by the rule: heard word for word, pitch in its band, then the take whose closest neighbour (Inez, and the other new voices) is furthest. Larthi options are judged against Inez and every Milena audition take; Milena auditions against Inez, the old Larthi and the picked Larthi options. Writes out/scratch/chime/r3_pick.json."""
import json, numpy as np, glob
from pathlib import Path
R = Path('/Users/sky/Projects/suborned/out/scratch/chime'); c = json.loads(Path('/Users/sky/Projects/suborned/briefs/090/chime/cast2.json').read_text())
e = json.loads((R / 'emb_r3.json').read_text()); refs = {Path(w).stem.rsplit('_', 1)[0]: np.array(v) for w, v in e['ref_all'].items()}
asr = {Path(x['wav']).name: x['word_error'] for x in json.loads((R / 'asr_r3.json').read_text())}; me = {json.loads(l)['file']: json.loads(l) for l in open(R / 'r3_meas.jsonl')}
V = {Path(w).name: np.array(v) for w, v in e['r3'].items()}; bands = {**c['milena_auditions'], **c['larthi_options']}; pick = {}; out = {}
for k in sorted(c['larthi_options']) + sorted(c['milena_auditions']):
    lo, hi = bands[k]['band']; best = None
    for f in sorted(x for x in V if x.startswith(k + '_')):
        others = [('inez', refs['inez'])] + ([(x, V[x]) for x in V if x.startswith('a')] if k.startswith('l') else [('larthi_old', refs['larthi'])] + [(p, V[pick[p]]) for p in pick if p.startswith('l')])
        sims = {n: float(V[f] @ v) for n, v in others}; worst = max(sims.values()); ok = asr[f] == 0 and lo <= me[f]['f0_median'] <= hi
        if ok and (best is None or worst < best[1]): best = (f, worst, me[f]['f0_median'], round(sims['inez'], 3), max(sims, key=sims.get))
    if best: pick[k] = best[0]; out[k] = {'take': best[0], 'pitch_hz': round(best[2]), 'cos_inez': best[3], 'closest_other': best[4], 'closest_cos': round(best[1], 3), 'label': bands[k]['label']}
    else: out[k] = {'take': None, 'why': 'no take heard word for word in its band'}
(R / 'r3_pick.json').write_text(json.dumps(pick)); print(json.dumps(out, indent=0))
