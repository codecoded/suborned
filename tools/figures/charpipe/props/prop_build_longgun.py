"""Brief 069 props: a delivered long gun (LOD GLBs) brought to the weapon spec (props.json spec) and written for Unity.
  out/scratch/forma/char-pipeline/blender.sh -P tools/figures/charpipe/props/prop_build_longgun.py -- <prop>
By rule (props.json props.<prop>):
  axes     the longest extent is the barrel; the muzzle is the end with the smaller height (the stock's butt is tall); turned so the barrel is
           Blender -Y (Unity +Z) and up Blender +Z (Unity +Y), as the pistol (prop_build.py)
  length   scaled uniformly to spec.classes.longgun.lengthM (Sky, 2026-10-27: 0.9 m)
  profile  the gun's top and bottom along its length, by rays down and up through its centre plane (low-poly meshes have too few vertices to slice)
  grip     the pistol grip: the run of the bottom profile in gripSearch (fractions of the length from the butt) that drops more than dropM below
           the receiver's underside there; the grip region fitted to the class's grip block (depth, width) about its centre line, as the
           pistol's; the pivot (socket grip) at the grip's section belowFrac down from the underside
  support  on the handguard's underside, supportFromGripM ahead of the grip along the barrel (the chosen aim clip's own hand spacing), centred
  shoulder the butt plate's centre; muzzle the muzzle's centre
  LODs     as delivered, each its own mesh <prop>_LOD<k> and its own material; maps from the delivered PNGs (prop_textures.py, system python)
Writes out/scratch/forma/char-pipeline/props/<prop>/a/: <prop>.fbx (+ .meta), <prop>.json (sockets, sizes, LOD triangles). Pinned FBX."""
import bpy, os, sys, json, math, hashlib, importlib.util
import numpy as np
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree
HERE = os.path.dirname(os.path.abspath(__file__)); CP = os.path.dirname(HERE); FIG = os.path.dirname(CP); ROOT = os.path.dirname(os.path.dirname(FIG))
sys.path.append(FIG); import unitymeta as um, avatar as av
L = importlib.util.module_from_spec(s := importlib.util.spec_from_file_location('fitlib', os.path.join(CP, 'fit', 'lib.py'))); s.loader.exec_module(L)
D = json.load(open(os.path.join(HERE, 'props.json'))); PID = sys.argv[sys.argv.index('--') + 1]; P = D['props'][PID]
CL = D['spec']['classes'][P['class']]; OUT = L.ensure(os.path.join(ROOT, 'out/scratch/forma/char-pipeline/props', PID, 'a')); REL = f'Props/{PID}'


def load(path):
    before = set(bpy.data.objects); bpy.ops.import_scene.gltf(filepath=path)
    ms = [o for o in bpy.data.objects if o not in before and o.type == 'MESH']
    for o in ms: o.data.transform(o.matrix_world); o.matrix_world = Matrix.Identity(4); o.parent = None
    if len(ms) > 1:
        bpy.ops.object.select_all(action='DESELECT')
        for o in ms: o.select_set(True)
        bpy.context.view_layer.objects.active = ms[0]; bpy.ops.object.join()
    return ms[0]


def cut(V, T, z, ya, yb):
    """the mesh's cross-section points at height z between ya and yb along the barrel: where its edges cross the plane (a low-poly mesh has
    too few vertices to take a band of them)"""
    E = np.unique(np.sort(np.concatenate([T[:, [0, 1]], T[:, [1, 2]], T[:, [2, 0]]]), 1), axis=0); a, b = V[E[:, 0]], V[E[:, 1]]
    m = (a[:, 2] - z) * (b[:, 2] - z) < 0; a, b = a[m], b[m]; t = ((z - a[:, 2]) / (b[:, 2] - a[:, 2]))[:, None]; P_ = a + (b - a) * t
    return P_[(P_[:, 1] > ya) & (P_[:, 1] < yb)]


def verts(o): a = np.empty(len(o.data.vertices) * 3); o.data.vertices.foreach_get('co', a); return a.reshape(-1, 3)


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    src = [os.path.join(ROOT, x) for x in P['source']['lods']]; objs = [load(p) for p in src]
    V0 = verts(objs[0]); ext = V0.max(0) - V0.min(0); bar = int(np.argmax(ext[:2]))
    lo, hi = V0[:, bar].min(), V0[:, bar].max(); band = 0.08 * (hi - lo)
    h_lo = np.ptp(V0[V0[:, bar] < lo + band][:, 2]); h_hi = np.ptp(V0[V0[:, bar] > hi - band][:, 2])
    msign = 1.0 if h_hi < h_lo else -1.0                                          # the muzzle: the thinner end
    b = np.zeros(3); b[bar] = msign; u = np.array([0, 0, 1.0]); x = np.cross(b, u); R = np.array([x, b, u])   # barrel -> +Y for now (as prop_build)
    c0 = (V0.max(0) + V0.min(0)) / 2; s = CL['lengthM'] / (hi - lo)
    for o in objs:
        V = (verts(o) - c0) @ R.T * s; o.data.vertices.foreach_set('co', V.ravel()); o.data.update()
    V = verts(objs[0]); T = L.tris_of(objs[0]); bvh = BVHTree.FromPolygons([Vector(v) for v in V], [list(t) for t in T], all_triangles=True)
    y0, y1 = V[:, 1].min(), V[:, 1].max(); n = 200; ys = np.linspace(y0, y1, n); top = np.full(n, np.nan); bot = np.full(n, np.nan)
    for i, y in enumerate(ys):
        h = bvh.ray_cast(Vector((0, y, 1.0)), Vector((0, 0, -1)), 2.0); top[i] = h[0].z if h[0] else np.nan
        h = bvh.ray_cast(Vector((0, y, -1.0)), Vector((0, 0, 1)), 2.0); bot[i] = h[0].z if h[0] else np.nan
    uu = (ys - y0) / (y1 - y0); Gs = P['gripSearch']
    win = (uu >= Gs['from']) & (uu <= Gs['to']) & ~np.isnan(bot)
    under = float(np.nanmedian(bot[(uu >= Gs['underFrom']) & (uu <= Gs['underTo'])]))      # the receiver's underside
    run = win & (bot < under - Gs['dropM'])
    if not run.any(): raise SystemExit('no pistol grip found')
    idx = np.nonzero(run)[0]; ga, gb = ys[idx.min()], ys[idx.max()]; gbot = float(np.nanmin(bot[run]))
    # the grip fitted to the class's block: depth (along the barrel) and width, about its centre line, the region below the underside
    B = CL['grip']; Fb = P['gripFit']; grip_rep = None
    for o in objs:
        V = verts(o); sel = (V[:, 2] < under) & (V[:, 1] > ga - Fb['marginM']) & (V[:, 1] < gb + Fb['marginM'])
        if o is objs[0]:
            zm = under - 0.5 * (under - gbot); To = L.tris_of(o); sm = cut(V, To, zm, ga - Fb['marginM'], gb + Fb['marginM'])
            dep, wid = float(np.ptp(sm[:, 1])), float(np.ptp(sm[:, 0])); sd, sw = B['depthM'] / dep, B['widthM'] / wid; sh = max(1.0, B['heightM'] / (under - gbot)) if Fb.get('height') else 1.0   # a grip shorter than the block is lengthened downward
            lows = np.linspace(gbot + 0.1 * (under - gbot), under - 0.2 * (under - gbot), 8); cen = []
            for zz in lows:
                sc_ = cut(V, To, zz, ga - Fb['marginM'], gb + Fb['marginM'])
                if len(sc_) >= 3: cen.append((zz, (sc_[:, 1].min() + sc_[:, 1].max()) / 2, (sc_[:, 0].min() + sc_[:, 0].max()) / 2))
            cen = np.array(cen); ky = np.polyfit(cen[:, 0], cen[:, 1], 1); kx = np.polyfit(cen[:, 0], cen[:, 2], 1)
            grip_rep = {'before': {'depthM': round(dep, 4), 'widthM': round(wid, 4)}, 'heightM': round(under - gbot, 4)}
        for i in np.nonzero(sel)[0]:
            z = V[i, 2]; cy, cx = np.polyval(ky, z), np.polyval(kx, z); w = min(1.0, (under - z) / Fb['blendM'])
            V[i, 1] = cy + (V[i, 1] - cy) * (1 + (sd - 1) * w); V[i, 0] = cx + (V[i, 0] - cx) * (1 + (sw - 1) * w)
            V[i, 2] = under - (under - z) * (1 + (sh - 1) * w)
        o.data.vertices.foreach_set('co', V.ravel()); o.data.update()
    V = verts(objs[0]); sel = (V[:, 2] < under) & (V[:, 1] > ga - Fb['marginM']) & (V[:, 1] < gb + Fb['marginM'])
    zm = under - 0.5 * (under - gbot); T0 = L.tris_of(objs[0]); sm = cut(V, T0, zm, ga - Fb['marginM'], gb + Fb['marginM'])
    gbot = under - (under - gbot) * sh; grip_rep['after'] = {'heightM': round(float(under - gbot), 4), 'depthM': round(float(np.ptp(sm[:, 1])), 4), 'widthM': round(float(np.ptp(sm[:, 0])), 4)}
    zg = under - Gs['belowFrac'] * (under - gbot); sg = cut(V, T0, zg, ga - Fb['marginM'], gb + Fb['marginM'])
    origin = np.array([(sg[:, 0].min() + sg[:, 0].max()) / 2, (sg[:, 1].min() + sg[:, 1].max()) / 2, zg])
    # sockets (in the +Y-barrel frame first)
    ys_ = origin[1] + P['support']['fromGripM']; k = int(np.argmin(np.abs(ys - ys_)))
    support = np.array([0.0, ys_, bot[k] - P['support']['belowM']])
    endb = V[V[:, 1] < y0 + 0.01]; shoulder = np.array([0.0, y0, (endb[:, 2].min() + endb[:, 2].max()) / 2])
    endm = V[V[:, 1] > y1 - 0.01]; muzzle = np.array([0.0, y1, (endm[:, 2].min() + endm[:, 2].max()) / 2])
    FLIP = np.diag([-1.0, -1.0, 1.0])
    def unity(p):                                                                  # (+Y-barrel frame) -> flip -> Unity (x mirrored, Blender Z up, -Y forward)
        q = FLIP @ (p - origin); return [round(float(-q[0]), 4), round(float(q[2]), 4), round(float(-q[1]), 4)]
    for o in objs:
        V = (verts(o) - origin) @ FLIP.T; o.data.vertices.foreach_set('co', V.ravel()); o.data.update()
    Vf = verts(objs[0]); dims = Vf.max(0) - Vf.min(0)
    # export: the LODs as <prop>_LOD<k>, each its own material (prop_textures.py writes the maps and .mat files)
    for k, o in enumerate(objs):
        o.name = f'{PID}_LOD{k}'; o.data.name = o.name; m = o.data.materials[0]; m.name = f'{PID}_LOD{k}'
    L.bp.pin_fbx(); fbx = os.path.join(OUT, f'{PID}.fbx')
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.export_scene.fbx(filepath=fbx, use_selection=True, object_types={'MESH'}, use_mesh_modifiers=False, add_leaf_bones=False, bake_anim=False,
                             axis_forward='-Z', axis_up='Y', apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True,
                             mesh_smooth_type='FACE', use_tspace=False, path_mode='STRIP', embed_textures=False)
    open(fbx + '.meta', 'w').write(um.prop_meta(f'{REL}/{PID}.fbx', {f'{PID}_LOD{k}': f'{REL}/{PID}_LOD{k}.mat' for k in range(len(objs))}))
    # the written FBX read back in Unity's frame: the bulk forward of the grip, the top above it
    top_, _ = av._fbx_parser().parse(fbx); geos = [e for e in next(e for e in top_.elems if e.id == b'Objects').elems if e.id == b'Geometry']
    Vu = np.concatenate([np.array(next(e for e in g.elems if e.id == b'Vertices').props[0]).reshape(-1, 3) for g in geos]) * np.array([-1.0, 1.0, 1.0])
    gs = {p.props[0]: p.props[4:] for p in next(e for e in next(e for e in top_.elems if e.id == b'GlobalSettings').elems if e.id == b'Properties70').elems}
    Vu = Vu * gs[b'UnitScaleFactor'][0] * 0.01; cu = (Vu.max(0) + Vu.min(0)) / 2
    if not (cu[2] > 0.05 and abs(np.ptp(Vu[:, 2]) - CL['lengthM']) < 0.01): raise SystemExit(f'LONGGUN AXES FAIL in Unity frame: centre {cu}')
    rot_support = av.look_rotation(av.norm(np.array(P['support']['longAxis'], float)), np.array(P['support']['thumb'], float))
    rep = {'id': PID, 'class': P['class'], 'sources': [{'file': os.path.relpath(p, ROOT), 'sha256': hashlib.sha256(open(p, 'rb').read()).hexdigest()} for p in src],
           'scale': round(float(s), 5), 'lengthM': CL['lengthM'], 'sizeM': {'length': round(float(dims[1]), 4), 'height': round(float(dims[2]), 4), 'width': round(float(dims[0]), 4)},
           'lodTris': [len(L.tris_of(o)) for o in objs], 'gripFit': grip_rep,
           'sockets': {'grip': {'position': [0, 0, 0], 'rotation': [0, 0, 0, 1]},
                       'support': {'position': unity(support), 'rotation': [round(float(x), 6) for x in rot_support]},
                       'shoulder': {'position': unity(shoulder), 'rotation': [0, 0, 0, 1]},
                       'muzzle': {'position': unity(muzzle), 'rotation': [0, 0, 0, 1]}},
           'unityFrame': {'boundsCentreM': [round(float(x), 4) for x in cu]}, 'fbx': f'{PID}.fbx', 'fbxSha256': hashlib.sha256(open(fbx, 'rb').read()).hexdigest()}
    json.dump(rep, open(os.path.join(OUT, f'{PID}.json'), 'w'), indent=1, sort_keys=True)
    print('LONGGUN', json.dumps(rep))


main()
