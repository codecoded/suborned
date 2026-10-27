"""Brief 069 props: clean a generated held prop into the props.json convention and write it for Unity.
  out/scratch/forma/char-pipeline/blender.sh -P tools/figures/charpipe/props/prop_build.py -- <prop> [<glb>]
Reads the Tripo GLB (default paid/<job>/meshes/<prop>_t1.glb), then by rule (props.json):
  axes   the longest horizontal extent is the barrel; the muzzle is the end away from the grip (the grip is the deepest part, in the rear);
         turned so the barrel is Blender +Y (Unity +Z) and up is Blender +Z (Unity +Y)
  size   scaled so the barrel-axis length is size.lengthM (true size)
  origin the grip point (grip rule): the slide's underside is the lowest point of the front muzzleProbeFrac of the length; the grip is what lies
         below it in the rear rearFrac; the origin is the centre of the grip's section belowSlideFrac of the grip's height below the slide
  clean  seams welded (1e-6 m), loose specks under 0.5 % of the faces dropped, triangulated; the generated normals kept
  maps   albedo, normal and the URP metallic map (R metallic, A smoothness = 1 - roughness, from the glTF ORM) at texPx, Lanczos from the 4K source
Writes out/scratch/forma/char-pipeline/props/<prop>/a/: <prop>.fbx (+ .meta, the static-prop importer), textures (+ .meta), <prop>.mat (+ .meta),
<prop>.json (triangles, size, origin, grip, sha256). FBX pinned (fixed time and ids, no tangents): the same input gives the same bytes."""
import bpy, bmesh, os, sys, json, math, hashlib, importlib.util
import numpy as np
from mathutils import Vector, Matrix
HERE = os.path.dirname(os.path.abspath(__file__)); CP = os.path.dirname(HERE); FIG = os.path.dirname(CP); ROOT = os.path.dirname(os.path.dirname(FIG))
sys.path.append(FIG); import unitymeta as um
L = importlib.util.module_from_spec(s := importlib.util.spec_from_file_location('fitlib', os.path.join(CP, 'fit', 'lib.py'))); s.loader.exec_module(L)
D = json.load(open(os.path.join(HERE, 'props.json')))
A = sys.argv[sys.argv.index('--') + 1:]; PID = A[0]; P = D['props'][PID]
GLB = A[1] if len(A) > 1 else os.path.join(ROOT, 'paid', P['mesh']['job'], 'meshes', f'{PID}_t1.glb')
OUT = L.ensure(os.path.join(ROOT, 'out/scratch/forma/char-pipeline/props', PID, 'a'))
REL = f'Props/{PID}'   # the GUID namespace: unitymeta.guid('Props/<prop>/...'), as the game's props


def verts(o): a = np.empty(len(o.data.vertices) * 3); o.data.vertices.foreach_get('co', a); return a.reshape(-1, 3)


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=GLB)
    ms = [o for o in bpy.data.objects if o.type == 'MESH']
    for o in ms: o.data.transform(o.matrix_world); o.matrix_world = Matrix.Identity(4)
    if len(ms) > 1:
        bpy.ops.object.select_all(action='DESELECT')
        for o in ms: o.select_set(True)
        bpy.context.view_layer.objects.active = ms[0]; bpy.ops.object.join()
    o = ms[0]; o.name = PID; o.data.name = PID
    raw_tris = len(L.tris_of(o))
    # clean: weld seams, drop loose specks, triangulate (normals as generated)
    bm = bmesh.new(); bm.from_mesh(o.data)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-6)
    bm.faces.ensure_lookup_table(); seen = set(); islands = []
    for f in bm.faces:
        if f.index in seen: continue
        st = [f]; isl = []; seen.add(f.index)
        while st:
            g = st.pop(); isl.append(g)
            for e in g.edges:
                for h in e.link_faces:
                    if h.index not in seen: seen.add(h.index); st.append(h)
        islands.append(isl)
    small = [f for isl in islands if len(isl) < 0.005 * len(bm.faces) for f in isl]
    bmesh.ops.delete(bm, geom=small, context='FACES')
    bmesh.ops.triangulate(bm, faces=bm.faces[:])
    bm.to_mesh(o.data); bm.free()
    # axes by rule
    V = verts(o); lo, hi = V.min(0), V.max(0); ext = hi - lo
    up = 2                                                       # glTF Y-up imports as Blender +Z
    bar = 0 if ext[0] >= ext[1] else 1; side = 1 - bar
    zc = lo[up] + 0.3 * ext[up]; low = V[V[:, up] < zc]          # the deepest 30 %: the grip
    mid = (lo[bar] + hi[bar]) / 2; muzzle_sign = -1.0 if low[:, bar].mean() > mid else 1.0   # the muzzle is the end away from the grip
    # rotation taking (barrel, side, up) to Blender (+Y, -X or +X, +Z): build from the barrel direction with up kept
    b = np.zeros(3); b[bar] = muzzle_sign; u = np.array([0, 0, 1.0]); x = np.cross(b, u)      # x = barrel x up = the prop's right in Blender (-X maps to Unity +X)
    R = np.array([x, b, u])                                      # rows: new X, Y, Z axes in old coordinates
    V = (V - (lo + hi) / 2) @ R.T
    s = P['size']['lengthM'] / (V[:, 1].max() - V[:, 1].min()); V *= s
    # grip point
    G = P['grip']; L0, L1 = V[:, 1].min(), V[:, 1].max(); length = L1 - L0
    slide_bottom = V[V[:, 1] > L1 - G['muzzleProbeFrac'] * length][:, 2].min()
    grip = V[(V[:, 1] < L0 + G['rearFrac'] * length) & (V[:, 2] < slide_bottom)]
    gh = slide_bottom - grip[:, 2].min(); zg = slide_bottom - G['belowSlideFrac'] * gh
    sec = grip[np.abs(grip[:, 2] - zg) < 0.004]
    origin = np.array([(sec[:, 0].min() + sec[:, 0].max()) / 2, (sec[:, 1].min() + sec[:, 1].max()) / 2, zg])
    V -= origin
    o.data.vertices.foreach_set('co', V.ravel()); o.data.update()
    # the generated custom normals turn with the mesh: rotate them by R (scale and shift don't change normals)
    if o.data.has_custom_normals:
        o.data.calc_normals_split() if hasattr(o.data, 'calc_normals_split') else None
        n = np.array([list(l.normal) for l in o.data.loops]) @ R.T
        o.data.normals_split_custom_set([tuple(v) for v in n])
    # maps at texPx
    px = P['texPx']; mat = o.data.materials[0]; nodes = {n.label or n.name: n for n in mat.node_tree.nodes}
    imgs = {}
    for n in mat.node_tree.nodes:
        if n.type == 'TEX_IMAGE' and n.image:
            k = 'albedo' if n.image.name.startswith('Color') else 'normal' if n.image.name.startswith('Normal') else 'orm' if n.image.name.startswith('ORM') else None
            if k: imgs[k] = n.image
    def arr(img): return np.array(img.pixels[:], dtype=np.float32).reshape(img.size[1], img.size[0], 4)
    def save(a, name, kind, alpha=False):
        """a (bottom-up rows, as Blender holds pixels) -> a texPx PNG through Blender's own scaler (no PIL in Blender's python)"""
        h, w = a.shape[:2]; im = bpy.data.images.new(name, w, h, alpha=True, float_buffer=False)
        im.colorspace_settings.name = 'sRGB' if kind == 'albedo' else 'Non-Color'
        b = np.ones((h, w, 4), np.float32); b[..., :a.shape[2]] = a; im.pixels.foreach_set(b.ravel()); im.scale(px, px)
        p = os.path.join(OUT, name); im.filepath_raw = p; im.file_format = 'PNG'
        bpy.context.scene.render.image_settings.color_mode = 'RGBA' if alpha else 'RGB'; im.save()
        open(p + '.meta', 'w').write(um.tex_meta(f'{REL}/{name}', kind, px)); return name, hashlib.sha256(open(p, 'rb').read()).hexdigest()
    tex = {}
    for k in ('albedo', 'normal'):
        if k in imgs:
            imgs[k].colorspace_settings.name = 'sRGB' if k == 'albedo' else 'Non-Color'
            tex[k] = save(arr(imgs[k])[..., :3], f'{PID}_{k}.png', k)
    if 'orm' in imgs:
        imgs['orm'].colorspace_settings.name = 'Non-Color'; a = arr(imgs['orm'])
        m = np.zeros_like(a); m[..., 0] = a[..., 2]; m[..., 1] = a[..., 0]; m[..., 3] = 1 - a[..., 1]   # R metallic, G occlusion, A smoothness
        tex['metal'] = save(m, f'{PID}_metal.png', 'data', alpha=True)
        orm_stats = {'metallicMean': round(float(a[..., 2].mean()), 3), 'roughnessMean': round(float(a[..., 1].mean()), 3)}
    else: orm_stats = None
    mrel = f'{REL}/{PID}.mat'
    open(os.path.join(OUT, f'{PID}.mat'), 'w').write(um.mat_text(PID, base=f"{REL}/{tex['albedo'][0]}", normal=f"{REL}/{tex['normal'][0]}" if 'normal' in tex else None,
                                                                 metal=f"{REL}/{tex['metal'][0]}" if 'metal' in tex else None, smoothness=1.0, metallic=1.0))
    open(os.path.join(OUT, f'{PID}.mat.meta'), 'w').write(um.mat_meta(mrel))
    mat.name = PID
    # export (static mesh, axes baked so Unity's root is identity), pinned
    L.bp.pin_fbx()
    from io_scene_fbx import export_fbx_bin as eb
    fbx = os.path.join(OUT, f'{PID}.fbx')
    bpy.ops.object.select_all(action='DESELECT'); o.select_set(True); bpy.context.view_layer.objects.active = o
    bpy.ops.export_scene.fbx(filepath=fbx, use_selection=True, object_types={'MESH'}, use_mesh_modifiers=False, add_leaf_bones=False, bake_anim=False,
                             axis_forward='-Z', axis_up='Y', apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True,
                             mesh_smooth_type='FACE', use_tspace=False, path_mode='STRIP', embed_textures=False)
    open(fbx + '.meta', 'w').write(um.prop_meta(f'{REL}/{PID}.fbx', {PID: mrel}))
    V = verts(o); dims = (V.max(0) - V.min(0))
    rep = {'id': PID, 'source': os.path.relpath(GLB, ROOT), 'sourceSha256': hashlib.sha256(open(GLB, 'rb').read()).hexdigest(),
           'rawTris': raw_tris, 'tris': len(L.tris_of(o)), 'sizeM': {'length': round(float(dims[1]), 4), 'height': round(float(dims[2]), 4), 'width': round(float(dims[0]), 4)},
           'axes': {'barrelAxisInSource': 'xy'[bar], 'muzzleSign': muzzle_sign, 'unity': '+Z barrel, +Y slide, origin the grip point'},
           'grip': {'slideBottomM': round(float(slide_bottom - origin[2]), 4), 'gripHeightM': round(float(gh), 4), 'sectionVerts': int(len(sec))},
           'texPx': px, 'textures': {k: {'file': v[0], 'sha256': v[1]} for k, v in tex.items()}, 'orm': orm_stats,
           'fbx': f'{PID}.fbx', 'fbxSha256': hashlib.sha256(open(fbx, 'rb').read()).hexdigest()}
    json.dump(rep, open(os.path.join(OUT, f'{PID}.json'), 'w'), indent=1, sort_keys=True)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(L.ensure(os.path.join(OUT, '..', 'work')), f'{PID}.blend'))
    print('PROP', json.dumps(rep))


main()
