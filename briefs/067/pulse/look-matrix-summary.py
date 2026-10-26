# The ranking behind briefs/067/pulse.md section 7.3 (Pulse, brief 067 I): reads look-matrix.txt (look-matrix.py over the play yaws 30 and 210, sun and shade, two headings, for Forma's build, then for
# the install) and prints, for each figure, its nearest approach to the guard and to the police over the views: Forma's sum (upper + lower block dE), and the sum with the head block (where a cap or
# a hood shows). A screening measure, not a threshold: it sees colour, never silhouette (cap against hood, trousers against bare legs).   python3 -I briefs/067/pulse/look-matrix-summary.py
import collections, os, re

here = os.path.dirname(os.path.abspath(__file__))
txt = open(os.path.join(here, 'look-matrix.txt')).read()


def parse(section):
    runs, cur, ref = [], None, None
    for line in section.splitlines():
        m = re.match(r'# (sun|shade), yaw (\d+), heading (\w+);', line)
        if m: cur = {'k': f'{m[1]} yaw {m[2]} heading {m[3]}', 'vs': {}}; runs.append(cur); continue
        m = re.match(r'vs (\w+)\s', line)
        if m and cur is not None: ref = m[1]; cur['vs'][ref] = {}; continue
        m = re.match(r'\s+(\w+)\s+upper\s+([\d.]+)\s+lower\s+([\d.]+)\s+head\s+([\d.]+)\s+sum\s+([\d.]+)', line)
        if m and cur is not None and ref: cur['vs'][ref][m[1]] = tuple(float(m[i]) for i in (2, 3, 4))
    return runs


def rank(runs, ref, head):
    best = {}
    for r in runs:
        for fid, (u, l, h) in r['vs'].get(ref, {}).items():
            t = u + l + (h if head else 0)
            if fid not in best or t < best[fid][0]: best[fid] = (t, r['k'], u, l, h)
    return sorted(best.items(), key=lambda kv: kv[1][0])


for part in txt.split('## ')[1:]:
    title, runs = part.splitlines()[0], parse(part)
    print(f'== {title} ({len(runs)} views)')
    for ref in ('guard', 'security'):
        for head in (False, True):
            rows = rank(runs, ref, head)
            if not rows: continue
            print(f'  nearest to {ref}, {"upper + lower + head" if head else "upper + lower (Forma\'s sum)"}, smallest over the views:')
            for fid, (t, k, u, l, h) in rows: print(f'    {fid:11s} {t:5.1f}   {k:26s} upper {u:5.1f} lower {l:5.1f} head {h:5.1f}')
    print()
