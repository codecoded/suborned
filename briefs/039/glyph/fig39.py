# Glyph, brief 039 (2026-10-26): a thin driver for Forma's true-size proof renderer (tools/figures/proof.py, read only), so the simplified board can stand real library clips
# on the real figures at the play camera, instead of warping flat cut-outs. Nothing in tools/ or unity/ is changed; every output goes under out/scratch/glyph/.
import json, os, subprocess, sys, tempfile, importlib.util
import numpy as np
R = '/Users/sky/Projects/suborned/'
BLENDER = os.environ.get('BLENDER', '/Applications/Blender.app/Contents/MacOS/Blender')
PEOPLE = R + 'out/people-models/People'
LIBD = '/Users/sky/Projects/the-suborned-store/source-art/Anim/mixamo_library/'
_sp = importlib.util.spec_from_file_location('proof_post', R + 'tools/figures/proof_post.py'); pp = importlib.util.module_from_spec(_sp); _sp.loader.exec_module(pp)

def lib(name):
    """a library clip by its stem (the file's hash suffix is looked up), as a path relative to People/Clips (proof.py adds .fbx)"""
    hits = sorted(f for f in os.listdir(LIBD) if f.startswith(name + '__') and f.endswith('.fbx'))
    if not hits: raise SystemExit('no library clip ' + name)
    return os.path.relpath(LIBD + hits[0][:-4], PEOPLE + '/Clips')

# the lighting the proof runner fitted against the real 0.2.122 frames (tools/figures/proof_run.py): ambient x 3.1, figure gain 0.7, veil 0.03
AMBIENT, GAIN, VEIL = 3.1, 0.7, 0.03
def ambient_dict(k=AMBIENT):
    sky = np.array(pp.srgb_to_lin([0.871 * 0.69, 0.863 * 0.69, 0.878 * 0.69]))
    return {'sky': (sky * k).tolist(), 'equator': (np.array(pp.srgb_to_lin([0.98 * 0.71, 0.918 * 0.71, 0.863 * 0.71])) * k).tolist(), 'ground': (np.array(pp.srgb_to_lin([0.886 * 0.41, 0.773 * 0.41, 0.643 * 0.41])) * k).tolist()}

def render(figs, w, h, look=(0.0, 0.0, 0.62), ppm=1080 / 28.0, yaw=30.0, samples=32, road=(165, 178, 183), bg=None, workdir=None, sun=True):
    """figs: [dict(id, heading (Unity degrees, 0 = north, +90 = east), x, y (metres: right on the screen, up it over the ground, from the middle), clip, frame, albedo?)].
    Returns (composed uint8 H x W x 3 over `bg` or a flat road tone, the three passes' prefix). The picture is at the game's pixel size (1 px = 1 px of a 1080p frame) when ppm is 38.571."""
    wd = workdir or tempfile.mkdtemp(prefix='g39_'); amb = ambient_dict()
    sky = np.array(pp.srgb_to_lin([0.871 * 0.69, 0.863 * 0.69, 0.878 * 0.69]))
    g = pp.ground_albedo(road, sky_lin=(sky * AMBIENT).tolist()).tolist()
    spec = {'out': os.path.join(wd, 'p'), 'people': PEOPLE, 'yaw': yaw, 'ppm': ppm, 'w': w, 'h': h, 'sun': sun, 'ground': g, 'ambient': amb, 'spacing': 0.0, 'pose': 'idle', 'samples': samples,
            'look': list(look), 'veil': VEIL, 'figures': figs}
    json.dump(spec, open(os.path.join(wd, 'spec.json'), 'w'))
    r = subprocess.run([BLENDER, '-b', '-P', R + 'briefs/039/glyph/proof39.py', '--', os.path.join(wd, 'spec.json')], capture_output=True, text=True)
    if r.returncode != 0 or 'Traceback' in r.stdout + r.stderr:
        raise SystemExit('blender failed:\n' + '\n'.join(l for l in (r.stdout + r.stderr).splitlines() if 'rror' in l or 'Trace' in l or 'File' in l)[:3000])
    base = bg if bg is not None else np.full((h, w, 3), road, np.uint8)
    return pp.compose(os.path.join(wd, 'p'), base, 1.0, GAIN, None), os.path.join(wd, 'p')
