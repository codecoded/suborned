"""Close views of generated figures (brief 069): front, three-quarter, side and back of each, in a row, optionally posed from a clip frame.
  blender -b -P tools/figures/charpipe/preview.py -- <CharPipe dir> <out.png> [--lod 0] [--clip <People/Clips/x.fbx> --frame N] [--px 360] id [id ...]
EEVEE, a warm sun and a soft sky, the shared palette texture on the shared material: a look at the shapes, not the game's light (tools/figures/proof_run.py is that)."""
import bpy, sys, os, math, importlib.util
from mathutils import Vector, Matrix

a = sys.argv[sys.argv.index('--') + 1:]
def opt(n, d):
    if n in a: i = a.index(n); v = a[i + 1]; del a[i:i + 2]; return v
    return d
LOD = int(opt('--lod', '0')); CLIP = opt('--clip', None); FRAME = int(opt('--frame', '4')); PX = int(opt('--px', '360')); VIEWS = opt('--views', '0,45,90,180')
ROOT, OUT, ids = a[0], a[1], a[2:]
views = [float(v) for v in VIEWS.split(',')]
HERE = os.path.dirname(os.path.abspath(__file__))

bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene
sc.render.engine = 'BLENDER_EEVEE' if 'BLENDER_EEVEE' in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items.keys() else 'BLENDER_EEVEE_NEXT'
sc.render.resolution_x = PX * len(views) * len(ids) if False else PX; sc.render.resolution_y = int(PX * 1.6)
sc.view_settings.view_transform = 'Standard'
w = bpy.data.worlds.new('w'); w.use_nodes = True; w.node_tree.nodes['Background'].inputs['Color'].default_value = (0.93, 0.9, 0.85, 1); w.node_tree.nodes['Background'].inputs['Strength'].default_value = 0.9; sc.world = w
sl = bpy.data.lights.new('sun', 'SUN'); sl.energy = 3.2; sl.color = (1.0, 0.9, 0.82); so = bpy.data.objects.new('sun', sl); sc.collection.objects.link(so); so.rotation_euler = (math.radians(50), 0, math.radians(-35))

PAL = os.path.join(ROOT, 'Shared/Textures/cp_palette_albedo.png')   # a figure from elsewhere (today's People) is drawn plain grey
mat = bpy.data.materials.new('pv'); mat.use_nodes = True; nt = mat.node_tree; bs = nt.nodes['Principled BSDF']; bs.inputs['Roughness'].default_value = 0.8
if os.path.exists(PAL):
    tx = nt.nodes.new('ShaderNodeTexImage'); tx.image = bpy.data.images.load(PAL); tx.interpolation = 'Closest'; nt.links.new(tx.outputs['Color'], bs.inputs['Base Color'])

cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
cam.data.type = 'ORTHO'; cam.data.ortho_scale = 2.05

_rp = None
if CLIP:
    import importlib
    # proof.py's one-frame retarget, loaded as text (proof.py runs a scene at import, so only the function is taken)
    src = open(os.path.join(HERE, 'proof.py')).read()   # charpipe/proof.py: the upright-torso retarget
    start = src.index('def retarget_pose'); end = src.index('\nfigs = []')
    ns = {'bpy': bpy, 'Matrix': Matrix, 'Vector': Vector, 'sc': sc, 'bare': lambda n: n.split(':')[-1]}
    exec(src[start:end], ns); _rp = ns['retarget_pose']

tiles = []
for fid in ids:
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=os.path.join(ROOT, 'Figures', fid, fid + '.fbx'))
    new = [o for o in bpy.data.objects if o not in before]
    arm = next(o for o in new if o.type == 'ARMATURE')
    for o in new:
        if o.type == 'MESH':
            if '_LOD' in o.name and not o.name.startswith(f'{fid}_LOD{LOD}'): o.hide_render = True
            o.data.materials.clear(); o.data.materials.append(mat)
    if _rp: _rp(arm, CLIP, FRAME)
    for k, yaw in enumerate(views):
        r = math.radians(yaw); d = Vector((math.sin(r), -math.cos(r), 0))
        cam.location = Vector((0, 0, 0.88)) + d * 6; cam.rotation_euler = (math.radians(90), 0, r)
        path = f'{OUT}_{fid}_{k}.png'; sc.render.filepath = path; bpy.ops.render.render(write_still=True); tiles.append(path)
    for o in new: bpy.data.objects.remove(o, do_unlink=True)
print('TILES', len(tiles))
