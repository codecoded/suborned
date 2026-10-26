"""Run a true-size proof (brief 067, Forma): a row of figures at the play camera over a real stretch of the game's road, in the sun or in shade.
  python3 -I tools/figures/proof_run.py --out <png> [--people <People dir>] [--figs id[=heading][@dx/dy][:albedo.jpg][*],...] [--yaw 30] [--light sun|shade|both]
                                         [--scale 3] [--spacing 1.0] [--frame 4] [--label text] [--h 170] [--samples 48] [--ambient k] [--gain k] [--veil k] [--expo k]
Writes <png> (true size, 1 px = 1 px of a 1080p frame) and <png minus .png>@3x.png (nearest-neighbour). The people dir is the build (out/people-models/People) or the
installed Unity copy; the figures' own idle pose; headings in Unity degrees (0 = north, +90 = east; default faces the camera). It needs Blender (BLENDER or the app's)
and the road patches in tools/figures/proof/ (road_sun.png, road_shade.png: 160 x 80 and 120 x 60 crops of the real 0.2.122 frame briefs/041/build/0311/proof-1080/overhead-pickup.png at (860, 640) and (1140, 510); the shade one gives the sun-to-shade ratio only)."""
import argparse, json, os, subprocess, sys, tempfile
import numpy as np
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.dirname(os.path.dirname(HERE))
import importlib.util   # tools/figures has an inspect.py: never put this folder on sys.path
_sp = importlib.util.spec_from_file_location('proof_post', os.path.join(HERE, 'proof_post.py')); pp = importlib.util.module_from_spec(_sp); _sp.loader.exec_module(pp)

BLENDER = os.environ.get('BLENDER', '/Applications/Blender.app/Contents/MacOS/Blender')
# Fitted against the real frames (briefs/041/build/0311/proof-1080): the sun-to-shade ratio of the road (ambient x 3.1 over light.json's trilight), and the old rivals in the 3:20 frame
# (jacket median (82, 62, 80), dark median (67, 58, 64)): figure light x 0.7 and a 0.03 veil. Not Unity's own pixels; a relative read: whether a figure reads at 53 px beside its neighbours, not what colour the game will show.
AMBIENT, GAIN, VEIL = 3.1, 0.7, 0.03
PATCH = {'sun': (os.path.join(HERE, 'proof/road_sun.png'), (165, 178, 183)), 'shade': (os.path.join(HERE, 'proof/road_shade.png'), (119, 134, 144))}   # the shade patch is the real palm shade of the same road: it gives the sun-to-shade ratio below, not a ground
# the real road in the palm's shade over the same road in the sun, in scene light (the game's grade taken back): what shade does to the ground in the 0.2.122 frame
SHADE_RATIO = pp.ungrade(np.array(PATCH['shade'][1], dtype=float).reshape(1, 3))[0] / pp.ungrade(np.array(PATCH['sun'][1], dtype=float).reshape(1, 3))[0]


def tile(path, w, h):
    t = Image.open(path).convert('RGB'); out = Image.new('RGB', (w, h))
    for y in range(0, h, t.height):
        for x in range(0, w, t.width): out.paste(t, (x, y))
    return np.asarray(out)


def run(figs, people, yaw=30.0, light='sun', spacing=1.0, frame=4, ambient=None, expo=1.0, w=None, h=170, samples=48, workdir=None, rest=False, veil=None, gain=None, bg=None, look=(0.0, 0.0, 0.62)):
    """One row: returns the picture as an H x W x 3 uint8 array. figs: [(id, heading|None, albedo|None, rest, (x, y)|None)]: a figure with an offset (metres right on the screen, metres up it
    over the ground) stands there, the others in a row `spacing` apart; `bg` is a real picture to lay them over (H x W x 3) in place of the road patch; `look` is the world point at the middle of the picture."""
    ambient = AMBIENT if ambient is None else ambient; veil = VEIL if veil is None else veil; gain = GAIN if gain is None else gain
    n = len(figs); w = w or int(n * spacing * pp_ppm() + 90)
    wd = workdir or tempfile.mkdtemp(prefix='proof_')
    lit = light == 'sun'
    road_px = PATCH['sun'][1]
    sky = np.array(pp.srgb_to_lin([0.871 * 0.69, 0.863 * 0.69, 0.878 * 0.69]))
    amb = {'sky': (sky * ambient).tolist(), 'equator': (np.array(pp.srgb_to_lin([0.98 * 0.71, 0.918 * 0.71, 0.863 * 0.71])) * ambient).tolist(), 'ground': (np.array(pp.srgb_to_lin([0.886 * 0.41, 0.773 * 0.41, 0.643 * 0.41])) * ambient).tolist()}
    g = pp.ground_albedo(road_px, expo=expo, sky_lin=(sky * ambient).tolist()).tolist()
    spec = {'out': os.path.join(wd, 'p'), 'people': people, 'yaw': yaw, 'w': w, 'h': h, 'sun': lit, 'ground': g, 'ambient': amb, 'spacing': spacing,
            'pose': 'rest' if rest else 'idle', 'samples': samples, 'look': list(look), 'veil': veil, 'figGain': gain,
            'figures': [dict({'id': i, 'heading': (hd if hd is not None else yaw + 180.0 - 20.0), 'albedo': alb, 'frame': frame, 'rest': rs}, **({'x': ps[0], 'y': ps[1]} if ps else {})) for i, hd, alb, rs, ps in figs]}
    if all(f[4] for f in figs): spec['spacing'] = 0.0
    json.dump(spec, open(os.path.join(wd, 'spec.json'), 'w'))
    r = subprocess.run([BLENDER, '-b', '-P', os.path.join(HERE, 'proof.py'), '--', os.path.join(wd, 'spec.json')], capture_output=True, text=True)
    if r.returncode != 0 or 'Traceback' in r.stdout + r.stderr:
        raise SystemExit('blender failed:\n' + '\n'.join(l for l in (r.stdout + r.stderr).splitlines() if 'rror' in l or 'Trace' in l or 'File' in l)[:3000])
    bgp = PATCH['sun'][0]     # the sunlit patch for both: shade is the sun taken out of it by the render's own ratio (proof_post.compose), so no marking of the road comes with it
    return pp.compose(os.path.join(wd, 'p'), bg if bg is not None else tile(bgp, w, h), expo, gain, None if lit else SHADE_RATIO)


def pp_ppm(): return 1080 / 28.0


def parse_figs(s):
    """id[=heading][@dx/dy][:albedo.jpg][*]: a trailing * keeps the figure in its rest A-pose (the cell's own rigs do not take the library's idle through this retarget); @dx/dy puts it that many
    metres to the right on the screen and up it over the ground, from the picture's middle."""
    out = []
    for item in s.split(','):
        rest = item.endswith('*'); item = item.rstrip('*')
        alb = None
        if ':' in item: item, alb = item.split(':', 1)
        ps = None
        if '@' in item: item, q = item.split('@', 1); ps = tuple(float(v) for v in q.split('/'))
        hd = None
        if '=' in item: item, hd = item.split('=', 1); hd = float(hd)
        out.append((item, hd, alb, rest, ps))
    return out


def label(img, text, h=16):
    im = Image.fromarray(img); out = Image.new('RGB', (im.width, im.height + h), (247, 244, 236)); out.paste(im, (0, h))
    ImageDraw.Draw(out).text((4, 2), text, fill=(23, 48, 47)); return out


if __name__ == '__main__':
    ap = argparse.ArgumentParser()
    ap.add_argument('--out', required=True); ap.add_argument('--people', default=os.path.join(ROOT, 'unity/Assets/Suborned/People'))
    ap.add_argument('--figs', required=True); ap.add_argument('--yaw', type=float, default=30.0); ap.add_argument('--light', default='sun')
    ap.add_argument('--scale', type=int, default=3); ap.add_argument('--spacing', type=float, default=1.0); ap.add_argument('--frame', type=int, default=4)
    ap.add_argument('--ambient', type=float, default=AMBIENT); ap.add_argument('--expo', type=float, default=1.0); ap.add_argument('--label', default='')
    ap.add_argument('--h', type=int, default=170); ap.add_argument('--samples', type=int, default=48); ap.add_argument('--veil', type=float, default=VEIL); ap.add_argument('--gain', type=float, default=GAIN)
    a = ap.parse_args()
    figs = parse_figs(a.figs); rows = []
    for lt in (['sun', 'shade'] if a.light == 'both' else [a.light]):
        img = run(figs, a.people, a.yaw, lt, a.spacing, a.frame, a.ambient, a.expo, h=a.h, samples=a.samples, veil=a.veil, gain=a.gain)
        rows.append(label(img, f'{a.label} {lt}, yaw {a.yaw:g}, 1:1'.strip()))
    W = max(r.width for r in rows); board = Image.new('RGB', (W, sum(r.height for r in rows)), (247, 244, 236)); y = 0
    for r in rows: board.paste(r, (0, y)); y += r.height
    board.save(a.out)
    big = board.resize((board.width * a.scale, board.height * a.scale), Image.NEAREST); big.save(a.out[:-4] + f'@{a.scale}x.png')
    print('wrote', a.out, board.size)

