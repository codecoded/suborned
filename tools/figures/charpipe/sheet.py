"""Play-zoom contact sheets for brief 069: the generated people beside today's crowd, true size (1 px = 1 px of a 1080p frame at play zoom 14, 38 degrees, yaw 30)
in the game's light, through tools/figures/proof_run.py (read only: its camera, sun, trilight, road patch and grade), plus a 3x nearest-neighbour copy.
  python3 -I tools/figures/charpipe/sheet.py <CharPipe dir> <out dir> [--people unity/Assets/Suborned/People] [--frame 4] [--clip idle]
Writes crowd.png (new 50 in rows, then today's crowd), groups.png (a guard group and a rival group standing together), and their @3x copies.
The new figures are shown through a view folder (<out>/view) of symlinks in the People layout, so proof.py binds the shared palette as it binds any figure's albedo."""
import argparse, json, os, sys, importlib.util
import numpy as np
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.dirname(os.path.dirname(os.path.dirname(HERE)))
_sp = importlib.util.spec_from_file_location('proof_run', os.path.join(ROOT, 'tools/figures/proof_run.py')); pr = importlib.util.module_from_spec(_sp); _sp.loader.exec_module(pr)
pr.HERE = HERE   # run Blender on charpipe/proof.py (the copy with the upright-torso retarget); the road patches were resolved at import from tools/figures/proof/
TODAY = ['c6', 'd6', 'el1', 'sh2', 'workers_d5', 'd4', 'd4b', 'd5c', 'cafe_w2', 'cafe_w3', 'cafe_w5', 'guard', 'security', 'rival', 'rival_b', 'rival_c', 'rival_d']


def view(cp, out, people):
    v = os.path.join(out, 'view'); os.makedirs(os.path.join(v, 'Figures'), exist_ok=True)
    clips = os.path.join(v, 'Clips')
    if not os.path.islink(clips): os.symlink(os.path.join(people, 'Clips'), clips)
    for fid in sorted(os.listdir(os.path.join(cp, 'Figures'))):
        d = os.path.join(v, 'Figures', fid, 'Textures'); os.makedirs(d, exist_ok=True)
        for src, dst in ((os.path.join(cp, 'Figures', fid, fid + '.fbx'), os.path.join(v, 'Figures', fid, fid + '.fbx')),
                         (os.path.join(cp, 'Shared/Textures/cp_palette_albedo.png'), os.path.join(d, 'cp_palette_albedo.png'))):
            if not os.path.lexists(dst): os.symlink(src, dst)
    return v


def row(ids, people, frame, label, spacing=0.95, heading=None, offsets=None):
    figs = [(i, heading, None, False, offsets[k] if offsets else None) for k, i in enumerate(ids)]
    img = pr.run(figs, people, 30.0, 'sun', spacing, frame, h=150, samples=48, w=int(max(len(ids), 13) * spacing * pr.pp_ppm() + 60))
    return pr.label(img, label)


def board(rows, path, scale=3):
    W = max(r.width for r in rows); b = Image.new('RGB', (W, sum(r.height for r in rows)), (247, 244, 236)); y = 0
    for r in rows: b.paste(r, (0, y)); y += r.height
    b.save(path); b.resize((b.width * scale, b.height * scale), Image.NEAREST).save(path[:-4] + f'@{scale}x.png'); print('wrote', path, b.size)


def main():
    ap = argparse.ArgumentParser(); ap.add_argument('cp'); ap.add_argument('out')
    ap.add_argument('--people', default=os.path.join(ROOT, 'unity/Assets/Suborned/People')); ap.add_argument('--frame', type=int, default=4)
    ap.add_argument('--only', default='crowd,groups')
    a = ap.parse_args(); os.makedirs(a.out, exist_ok=True)
    v = view(a.cp, a.out, a.people)
    m = json.load(open(os.path.join(a.cp, 'manifest.json')))
    ids = [f['id'] for f in m['figures']]
    if 'crowd' in a.only:
        rows = []
        for k in range(0, len(ids), 13):
            rows.append(row(ids[k:k + 13], v, a.frame, f'new: {ids[k]} .. {ids[min(k + 12, len(ids) - 1)]} (LOD1, idle frame {a.frame}, play zoom 1:1, sun)'))
        rows.append(row(TODAY[:9], a.people, a.frame, 'today: the crowd figures (passers, office, cafe staff)'))
        rows.append(row(TODAY[9:], a.people, a.frame, 'today: cafe_w5, guard, security, the four rivals'))
        board(rows, os.path.join(a.out, 'crowd.png'))
    if 'groups' in a.only:
        g = [i for i in ids if '_guard_' in i]; r = [i for i in ids if '_rival_' in i]
        # a loose knot each, facing about the same way, as they'd stand at a post or round a car
        offs = [(-1.6, 0.0), (-0.8, 0.5), (0.0, 0.0), (0.8, 0.45), (1.6, 0.0), (-1.2, -0.7), (0.4, -0.75), (1.4, -0.6)]
        rows = [row(g, v, a.frame, 'new: the guard group (one amber uniform, eight people)', heading=210.0 - 25, offsets=offs),
                row(r, v, a.frame, 'new: the rival group (the Velvet Set: violet, each their own)', heading=210.0 - 25, offsets=offs),
                row(['guard', 'security', 'guard', 'security'], a.people, a.frame, 'today: guard and security (two models)', heading=210.0 - 25, offsets=offs[:4]),
                row(['rival', 'rival_b', 'rival_c', 'rival_d'], a.people, a.frame, 'today: the four rival colourways', heading=210.0 - 25, offsets=offs[:4])]
        board(rows, os.path.join(a.out, 'groups.png'))


if __name__ == '__main__':
    main()
