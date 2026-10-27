"""Brief 069: two generated meshes of one person side by side in close-ups, same light, same camera relative to each (the Fixer at 20k and 30k).
  blender.sh -P tools/figures/charpipe/fit/compare_meshes.py -- <out dir> <labelA=glb> <labelB=glb>
Each glb is normalised as the fit does (lib.normalise: feet at 0, front -Y, a nominal 1.75 m) and drawn flat-shaded from its geometry and smooth
from its own normals, so facets show. Views (fractions of the height, the person's own left = +X): face, the left hand, the left shoulder cut-out,
the split at her left hip, the back. Writes <out>/cmp_<view>_<label>.png and a wireframe pass of each (cmp_<view>_<label>_wire.png)."""
import bpy, os, sys, math, importlib.util
HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE); import look
L = importlib.util.module_from_spec(s := importlib.util.spec_from_file_location('fitlib', os.path.join(HERE, 'lib.py'))); s.loader.exec_module(L)
from mathutils import Vector

A = sys.argv[sys.argv.index('--') + 1:]; OUT = L.ensure(A[0]); items = [x.split('=', 1) for x in A[1:]]
VIEWS = {   # name: (target as fractions of height (x, y, z), camera offset in metres, fov)
    'face': ((0.0, 0.0, 0.93), (-0.12, -0.55, 0.02), 16),
    'hand': ((0.31, 0.0, 0.535), (0.08, -0.5, 0.04), 20),
    'cutout': ((0.13, 0.0, 0.80), (0.20, -0.55, 0.12), 20),
    'split': ((0.05, 0.0, 0.40), (0.05, -0.95, 0.05), 24),
    'back': ((0.0, 0.0, 0.70), (0.0, 0.95, 0.05), 26),
}
for lab, path in items:
    bpy.ops.wm.read_factory_settings(use_empty=True); sc = bpy.context.scene
    o = L.import_glb(path); n = L.normalise(o); img = L.base_color_image(o); L.flat_material(o, img)
    H = max(v.co.z for v in o.data.vertices)
    look.studio(sc, 700, 700); cam = look.camera(sc); cam.data.sensor_fit = 'VERTICAL'
    for wire in (False, True):
        if wire:
            md = o.modifiers.new('w', 'WIREFRAME'); md.thickness = 0.0012; md.use_replace = False
            wm = L.simple_material('wire', None, (0.05, 0.05, 0.05, 1), 1.0); o.data.materials.append(wm); md.material_offset = len(o.data.materials) - 1
        for name, (t, off, fov) in VIEWS.items():
            tgt = Vector((t[0] * H, t[1] * H, t[2] * H)); cam.data.angle_y = math.radians(fov)
            look.look_at(cam, tgt + Vector(off), tgt)
            sc.render.filepath = os.path.join(OUT, f'cmp_{name}_{lab}{"_wire" if wire else ""}.png'); bpy.ops.render.render(write_still=True)
    open(os.path.join(OUT, f'cmp_{lab}.txt'), 'w').write(f'{lab}: {path}: {len(L.tris_of(o))} triangles\n')
