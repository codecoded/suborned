"""Sign-off plan (brief 069): every picture the sign-off sheet shows, worked out from the pipeline's data files only (roles.json, wardrobe.json,
palettes.json), plus the combination counts. Pure python.
  python3 -I tools/figures/charpipe/proof/signoff_plan.py <out jobs.json>
The bodies sweep each slider across the range the generator actually draws from (the union over the roles in roles.json, per sex where it is split);
each part is shown on both bodies at the reference and at the slider ends, over the smallest outfit it layers on (from its slot); colourways come from
each role's colour sets; the play-zoom row is real people picked by pick.py's rules."""
import importlib.util, itertools, json, os, sys

HERE = os.path.dirname(os.path.abspath(__file__)); CP = os.path.dirname(HERE)
ROLES = json.load(open(os.path.join(CP, 'roles.json')))['roles']
WARD = json.load(open(os.path.join(CP, 'wardrobe.json')))
PAL = json.load(open(os.path.join(CP, 'palettes.json')))
_sp = importlib.util.spec_from_file_location('pick', os.path.join(CP, 'pick.py')); pick = importlib.util.module_from_spec(_sp); _sp.loader.exec_module(pick)

NEUTRAL = {'S': 'skin_3', 'E': 'eye_dark', 'HR': 'hair_dark', 'BT': 'boot_white', 'M': 'accent_turquoise', 'B': 'paper_cream', 'T': 'chrome', 'TL': 'chrome',
           'H': 'ivory_uniform', 'L': 'paper_cream', 'LT': 'skin_3', 'G': 'ink_black', 'D': 'ink_black', 'AP': 'ivory_uniform', 'BG': 'chrome'}
BARE = {'S': 'skin_3', 'E': 'eye_dark', 'HR': 'hair_dark', **{k: 'skin_3' for k in ('BT', 'M', 'B', 'T', 'TL', 'H', 'L', 'LT', 'G', 'D', 'AP', 'BG')}}
SLIDERS = ['height', 'weight', 'muscle', 'age', 'proportions']


def ranges():
    """the union of every role's range per slider (per sex for a split one): what the generator can produce."""
    out = {}
    for sx in ('m', 'f'):
        r = {}
        for s in SLIDERS:
            lo, hi = 9, -9
            for role in ROLES.values():
                v = role['body'][s]
                if s == 'age': v = [min(a[0] for a in v.values()), max(a[1] for a in v.values())]
                elif isinstance(v, dict): v = v[sx]
                lo, hi = min(lo, v[0]), max(hi, v[1])
            r[s] = [round(lo, 3), round(hi, 3)]
        out[sx] = r
    return out


def ref_macros(sx, R):
    return {'gender': 1.0 if sx == 'm' else 0.0, **{s: round((R[sx][s][0] + R[sx][s][1]) / 2, 3) for s in SLIDERS}}


def context(part):
    """the smallest outfit a part sits on, from its slot: so a coat is seen over a top and legs, a hat over hair."""
    slot = WARD['parts'][part]['slot']; base = ['body', 'eyes']
    if slot == 'top': return base + ['boots'], [part] + ([] if part == 'dress_shift' else ['legs_trousers'])
    if slot == 'legs': return base + ['boots'], [part]
    if slot == 'hair': return base, [part]
    if slot == 'hat': return base, ['hair_crop', part]
    if slot == 'hood': return base + ['boots'], ['top_jacket', 'legs_trousers', 'hair_crop', part]
    return base + ['boots'], ['top_jacket' if slot not in ('apron',) else 'top_tunic', 'legs_trousers', part]


def combos():
    """distinct outfits per role and sex before colour: the product of the slot options the rules allow (weight > 0), after the rules that remove
    parts (a dress empties the legs slot; a part's `replaces` drops the one under it)."""
    out = {}
    for rn, role in ROLES.items():
        out[rn] = {}
        for sx in ('m', 'f'):
            opts = []
            for slot, o in role['slots'].items():
                ok = [k for k, w in o.items() if (w.get(sx, 0) if isinstance(w, dict) else w) > 0]
                opts.append([(slot, k) for k in ok])
            seen = set()
            for combo in itertools.product(*opts):
                d = dict(combo)
                if d.get('top') == 'dress_shift': d['legs'] = 'none'
                parts = [v for s, v in d.items() if s != 'gloves' and v not in ('none',)]
                gone = {x for v in parts for x in WARD['parts'].get(v, {}).get('replaces', [])}
                key = tuple(sorted((s, v) for s, v in d.items() if v not in gone))
                seen.add(key)
            out[rn][sx] = len(seen)
    return out


def main():
    R = ranges(); jobs = []
    # 1 · bodies: each slider at its low, middle and high end, the others at the middle; then mixed people from the rules
    for sx in ('m', 'f'):
        ref = ref_macros(sx, R)
        for s in SLIDERS:
            for tag, v in (('low', R[sx][s][0]), ('mid', ref[s]), ('high', R[sx][s][1])):
                jobs.append({'key': f'body_{sx}_{s}_{tag}', 'group': 'bodies', 'sex': sx, 'macros': {**ref, s: v}, 'base': ['body', 'eyes'], 'parts': [],
                             'colours': BARE, 'views': ['front'], 'label': f'{"man" if sx == "m" else "woman"} · {s} {tag} ({v})'})
    for i in range(8):
        role = ['commuter', 'guard', 'rival', 'cafe'][i % 4]; p = pick.person(f'signoff-body-{i}', role, ROLES, PAL['sets'])
        jobs.append({'key': f'mix_{i}', 'group': 'mixed', 'sex': p['sex'], 'macros': p['macros'], 'base': ['body', 'eyes'], 'parts': [], 'colours': BARE,
                     'views': ['front', 'q3'], 'label': f'mixed ({role} rules): ' + ', '.join(f'{k} {v}' for k, v in p['macros'].items() if k != 'gender')})
    # 2 · wardrobe: every part, both bodies, the reference and the slider ends
    for part, spec in WARD['parts'].items():
        base, parts = context(part)
        for sx in ('m', 'f'):
            ref = ref_macros(sx, R)
            for tag, m in (('ref', ref), ('height low', {**ref, 'height': R[sx]['height'][0]}), ('height high', {**ref, 'height': R[sx]['height'][1]}),
                           ('weight low', {**ref, 'weight': R[sx]['weight'][0]}), ('weight high', {**ref, 'weight': R[sx]['weight'][1]}), ('muscle high', {**ref, 'muscle': R[sx]['muscle'][1]})):
                jobs.append({'key': f'part_{part}_{sx}_{tag.replace(" ", "_")}', 'group': 'part:' + part, 'sex': sx, 'macros': m, 'base': base, 'parts': parts,
                             'colours': NEUTRAL, 'views': ['front', 'q3'] if tag == 'ref' else ['front'], 'label': f'{part} · {"man" if sx == "m" else "woman"} · {tag}'})
    # buzz (a paint rule, not a part) and gloves (a base layer)
    for sx in ('m', 'f'):
        ref = ref_macros(sx, R)
        jobs.append({'key': f'part_buzz_{sx}_ref', 'group': 'part:buzz', 'sex': sx, 'macros': ref, 'base': ['body', 'eyes'], 'parts': [], 'buzz': True, 'colours': NEUTRAL, 'views': ['front', 'q3'], 'label': f'buzz (hair as paint) · {sx}'})
        jobs.append({'key': f'part_gloves_{sx}_ref', 'group': 'part:gloves', 'sex': sx, 'macros': ref, 'base': ['body', 'eyes', 'boots', 'gloves'], 'parts': ['top_jacket', 'legs_trousers'], 'colours': NEUTRAL, 'views': ['front'], 'label': f'gloves and boots (base layers) · {sx}'})
    # 4 · colour: per role, one person from its rules in each main colour its set allows
    for rn in ('commuter', 'guard', 'rival', 'cafe'):
        p = pick.person(f'signoff-colour-{rn}', rn, ROLES, PAL['sets'])
        src = ROLES[rn]['colours']['M']
        mains = list(PAL['sets'][src]) if isinstance(src, str) else list(src.keys()) if isinstance(src, dict) else list(src)
        mains = list(dict.fromkeys(mains))
        for c in mains:
            col = dict(p['colours']); col['M'] = c
            jobs.append({'key': f'colour_{rn}_{c}', 'group': 'colour:' + rn, 'sex': p['sex'], 'macros': p['macros'], 'base': p['base'], 'parts': p['parts'], 'buzz': p.get('buzz'),
                         'colours': col, 'views': ['q3'], 'label': f'{rn} · main {c}'})
    # play zoom: real people from the rules, two per role
    for rn in ('commuter', 'commuter', 'guard', 'guard', 'rival', 'rival', 'cafe', 'cafe'):
        i = sum(1 for j in jobs if j['group'] == 'play' and j['role'] == rn)
        p = pick.person(f'signoff-play-{rn}-{i}', rn, ROLES, PAL['sets'])
        jobs.append({'key': f'play_{rn}_{i}', 'group': 'play', 'role': rn, 'sex': p['sex'], 'macros': p['macros'], 'base': p['base'], 'parts': p['parts'], 'buzz': p.get('buzz'),
                     'colours': p['colours'], 'views': ['play'], 'label': f'{rn} {i + 1}'})
    out = {'schema': 'suborned.charpipe.signoff/1', 'ranges': R, 'combos': combos(), 'jobs': jobs}
    json.dump(out, open(sys.argv[1], 'w'), indent=1, sort_keys=True); print('JOBS', len(jobs), 'renders', sum(len(j['views']) for j in jobs))


if __name__ == '__main__':
    main()
