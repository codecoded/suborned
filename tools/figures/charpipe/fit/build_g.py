"""Stage 2G (brief 069 fit test): the garment path. The generated person becomes separate pieces fitted to the MPFB base by construction:
  - suit: the MPFB proxy (subdivided) wrapped onto the source where it is tight, left on the body under garments; the source's colour baked on;
  - garments (coat, skirt, cape): cut from the source by the classifier, solidified, voxel-remeshed, decimated to budget, re-baked;
  - head pieces: hat (rigid on Head, sized by the wearer's head) and the person's loose hair (rigid on Head);
then every piece except the hat is un-posed to the MPFB rest and bound to the basemesh with MPFB's own MakeClothes matching (an .mhclo per piece), so
it follows every slider; weights come from the base by MPFB's interpolation. Each body variant (fit, and fit.json extremes) is rebuilt from the
.mhclo files, gets the cape chain, the attachment points and the props, and is posed to the standard rest.
  out/scratch/forma/char-pipeline/blender.sh -P tools/figures/charpipe/fit/build_g.py -- <input id> [--only-cut]
Writes fit-test/g/<id>/: pieces/*.mhclo|obj|png, <id>_G_<variant>.blend|fbx, <id>_G.json."""
import os, sys, json, math, time, importlib.util
import numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
L = importlib.util.module_from_spec(s := importlib.util.spec_from_file_location('fitlib', os.path.join(HERE, 'lib.py'))); s.loader.exec_module(L)
import bpy, bmesh
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree

G = L.D['garments']
ARGS = sys.argv[sys.argv.index('--') + 1:]; FID = ARGS[0]; ONLY_CUT = '--only-cut' in ARGS
OUT = L.ensure(os.path.join(L.SCR, 'g', FID)); PIECES = L.ensure(os.path.join(OUT, 'pieces'))
TM = L.Timer(); STATS = {'id': FID, 'approach': 'G', 'pieces': {}, 'variants': {}}


def apply_mods(o):
    with bpy.context.temp_override(object=o, active_object=o, selected_objects=[o]):
        for m in list(o.modifiers): bpy.ops.object.modifier_apply(modifier=m.name)


def bake(target, source, img_name, px):
    """the source's base colour onto target's UVs by closest point (lib.texture_transfer); saved as a png, set as the piece's matte material."""
    img = L.texture_transfer(target, source, L.base_color_image(source), px); img.name = img_name
    path = os.path.join(PIECES, img_name + '.png'); img.filepath_raw = path; img.file_format = 'PNG'; img.save()
    m = L.simple_material(img_name + '_matte', img); target.data.materials.clear(); target.data.materials.append(m)
    return path


def cut_piece(src, lab, kind):
    """the source faces whose vertices are all `kind`, as a new object (UVs kept for the bake), solidified, voxel-remeshed, smoothed, decimated, UV'd."""
    T = np.array([list(p.vertices) for p in src.data.polygons], dtype=object)
    keep = np.array([all(lab[i] == kind for i in p.vertices) for p in src.data.polygons])
    c = src.copy(); c.data = src.data.copy(); bpy.context.scene.collection.objects.link(c); c.name = f'{FID}_{kind}'
    bm = bmesh.new(); bm.from_mesh(c.data); bm.faces.ensure_lookup_table()
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if not keep[f.index]], context='FACES'); bm.to_mesh(c.data); bm.free()
    for m in list(c.modifiers): c.modifiers.remove(m)
    for g in list(c.vertex_groups): c.vertex_groups.remove(g)
    raw = len(c.data.polygons)
    if not G['kinds'][kind].get('retopo', True):
        tris = L.decimate_to(c, G['kinds'][kind]['tris']); return c, raw, tris
    sol = c.modifiers.new('sol', 'SOLIDIFY'); sol.thickness = G['solidifyM']; sol.offset = -1.0; sol.use_even_offset = True
    rm = c.modifiers.new('rm', 'REMESH'); rm.mode = 'VOXEL'; rm.voxel_size = G['voxelM']
    sm = c.modifiers.new('sm', 'CORRECTIVE_SMOOTH') if False else c.modifiers.new('sm', 'SMOOTH'); sm.iterations = 4; sm.factor = 0.5
    apply_mods(c)
    # keep the largest pieces only (drop voxel crumbs under 2 % of the piece)
    bm = bmesh.new(); bm.from_mesh(c.data); bm.verts.ensure_lookup_table()
    seen = set(); comps = []
    for v in bm.verts:
        if v.index in seen: continue
        stack = [v]; comp = []
        while stack:
            x = stack.pop()
            if x.index in seen: continue
            seen.add(x.index); comp.append(x); stack.extend(e.other_vert(x) for e in x.link_edges)
        comps.append(comp)
    big = max(len(cc) for cc in comps)
    bmesh.ops.delete(bm, geom=[v for cc in comps if len(cc) < 0.02 * big for v in cc], context='VERTS'); bm.to_mesh(c.data); bm.free()
    tris = L.decimate_to(c, G['kinds'][kind]['tris'])
    bpy.ops.object.select_all(action='DESELECT'); L.bp.ctx(c, 'EDIT'); bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=math.radians(60), island_margin=0.01); L.bp.ctx(c, 'OBJECT')
    return c, raw, tris


def wrap_suit(px, src, lab_src, donor_names):
    """the proxy subdivided and wrapped: each vertex moves along its normal (both ways, up to wrapMaxM) to the source where the source is tight
    (its label is ''), else it keeps the body + suitPadM; hands keep the body. The offsets are smoothed over the mesh."""
    S = G['suit']; s = px.copy(); s.data = px.data.copy(); bpy.context.scene.collection.objects.link(s); s.name = f'{FID}_suit'
    s.parent = None; s.matrix_world = Matrix.Identity(4)
    for m in list(s.modifiers):
        if m.type != 'ARMATURE': s.modifiers.remove(m)
    apply_mods(s)                       # bake the fitted pose (the proxy rides the posed rig)
    sd = s.modifiers.new('sd', 'SUBSURF'); sd.levels = S['suitSubdiv']; sd.render_levels = S['suitSubdiv']; sd.subdivision_type = 'CATMULL_CLARK'
    sd.uv_smooth = 'PRESERVE_BOUNDARIES'
    apply_mods(s)
    V = np.array([list(v.co) for v in s.data.vertices]); Tt = L.tris_of(s); N = L.vnormals(V, Tt)
    names = [g.name for g in s.vertex_groups]; W = L.weights_of(s, names) if names else None
    hand = np.zeros(len(V), bool)
    if W is not None:
        for nm in S['noWrap']:
            fam = [i for i, n in enumerate(names) if n.startswith(nm)]
            if fam: hand |= W[:, fam].sum(1) > 0.5
    Vs = np.array([list(v.co) for v in src.data.vertices]); Ts = L.tris_of(src)
    bvh = BVHTree.FromPolygons([Vector(v) for v in Vs], [list(t) for t in Ts], all_triangles=True)
    off = np.full(len(V), S['suitPadM']); hit = np.zeros(len(V), bool)
    for i in range(len(V)):
        if hand[i]: continue
        p = Vector(V[i]); n = Vector(N[i]); best = None
        for d in (n, -n):
            loc, nrm, fi, dist = bvh.ray_cast(p, d, S['wrapMaxM'])
            if loc is not None and (best is None or dist < best[1]): best = (loc, dist, fi, d)
        if best is None: continue
        f = Ts[best[2]]
        if any(lab_src[j] != '' for j in f): continue
        off[i] = (best[0] - p).dot(n); hit[i] = True
    # smooth the offsets (the wrapped ones pull, the others relax)
    E = np.concatenate([Tt[:, [0, 1]], Tt[:, [1, 2]], Tt[:, [2, 0]]]); E = np.concatenate([E, E[:, ::-1]])
    deg = np.bincount(E[:, 0], minlength=len(V)).astype(float); deg[deg == 0] = 1
    o2 = off.copy()
    for _ in range(S['smooth']):
        avg = np.bincount(E[:, 0], o2[E[:, 1]], len(V)) / deg; o2 = np.where(hit, 0.5 * off + 0.5 * avg, avg)
    o2[hand] = 0.0
    L.set_co(s, V + N * o2[:, None])
    return s, {'wrapped': int(hit.sum()), 'verts': int(len(V)), 'pctWrapped': round(100 * float(hit.mean()), 1)}


def tuck(pieces, suit):
    S = G['seam']; Vs = np.array([list(v.co) for v in suit.data.vertices]); Ts = L.tris_of(suit)
    bvh = BVHTree.FromPolygons([Vector(v) for v in Vs], [list(t) for t in Ts], all_triangles=True)
    for kind, o in pieces.items():
        if not G['kinds'][kind].get('retopo', True): continue
        V = np.array([list(v.co) for v in o.data.vertices]); top = V[:, 2].max(); n = 0
        for i in np.nonzero(V[:, 2] > top - S['bandM'])[0]:
            loc, nrm, fi, d = bvh.find_nearest(Vector(V[i]))
            if loc is None: continue
            f = min(1.0, (V[i, 2] - (top - S['bandM'])) / S['bandM'] * 1.5)
            V[i] = V[i] * (1 - f) + np.array(loc - nrm * S['insetM']) * f; n += 1
        L.set_co(o, V); STATS['pieces'][kind]['tucked'] = n


def main_cut():
    fit = json.load(open(os.path.join(L.SCR, 'fit', f'{FID}.fit.json'))); row = {r['id']: r for r in L.D['inputs']}[FID]
    bpy.ops.wm.open_mainfile(filepath=L.src_blend(FID))
    for o in list(bpy.data.objects):
        if o.name.endswith('_fitproxy'): bpy.data.objects.remove(o, do_unlink=True)
    src = bpy.data.objects[f'{FID}_src']; img = L.base_color_image(src); L.clean_normals(src); L.flat_material(src, img)
    L.addon_utils.enable('bl_ext.user_default.mpfb', default_set=True, persistent=False)
    arm, b, px, donor = L.fitted_human(fit); TM.lap('body')
    lm = L.landmarks(arm, donor['V'])
    Vs = np.array([list(v.co) for v in src.data.vertices]); Ts = L.tris_of(src)
    F, Bc, Q, dist, Nd = L.closest_on(donor['V'], donor['T'], Vs)
    nb = np.array(donor['names'])[np.argmax(np.einsum('vk,vkb->vb', Bc, donor['W'][donor['T'][F]]), 1)]
    lab = L.classify(Vs, Ts, dist, nb, lm, row['wears'] + ['hair']); TM.lap('classify')
    np.save(os.path.join(OUT, 'labels.npy'), lab.astype(str))
    pieces = {}
    for kind in ('coat', 'skirt', 'cape', 'hat', 'hair'):
        if not (lab == kind).any(): continue
        t0 = time.time(); c, raw, tris = cut_piece(src, lab, kind)
        if len(c.data.polygons) == 0: bpy.data.objects.remove(c, do_unlink=True); continue
        if G['kinds'][kind].get('retopo', True): bake(c, src, f'{FID}_{kind}', G['bakePx'])
        else:
            im = L.base_color_image(src); path = os.path.join(PIECES, f'{FID}_{kind}.png'); im.save(filepath=path)
            c.data.materials.clear(); c.data.materials.append(L.simple_material(f'{FID}_{kind}_matte', im))
        c['fit_kind'] = kind
        pieces[kind] = c; STATS['pieces'][kind] = {'srcFaces': raw, 'tris': tris, 'seconds': round(time.time() - t0, 2), 'srcVerts': int((lab == kind).sum())}
        L.log(FID, 'piece', kind, raw, '->', tris, round(time.time() - t0, 1), 's')
    t0 = time.time()
    suit, wst = wrap_suit(px, src, lab, donor['names'])
    tuck(pieces, suit)
    budget = L.D['lods']['tris'][0] - sum(STATS['pieces'][k]['tris'] for k in pieces)
    st = L.decimate_to(suit, budget)
    bake(suit, src, f'{FID}_suit', G['suitBakePx']); suit['fit_kind'] = 'suit'
    STATS['pieces']['suit'] = dict(wst, tris=st, seconds=round(time.time() - t0, 2)); TM.lap('pieces')
    src.hide_render = True
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, f'{FID}_cut.blend'), compress=True)
    json.dump({'fit': FID, 'labels': {k: int((lab == k).sum()) for k in set(lab.tolist()) if k}, 'pieces': STATS['pieces'], 'seconds': TM.t},
              open(os.path.join(OUT, 'cut.json'), 'w'), indent=1)
    L.log('CUT', FID, STATS['pieces'], TM.t)
    return {'fit': fit, 'arm': arm, 'b': b, 'px': px, 'donor': donor, 'pieces': pieces, 'suit': suit, 'lm': lm, 'src': src}



# ------------------------------------------------------------------ bind: un-pose to the MPFB rest, MakeClothes matching, write .mhclo
def deform_mats(arm):
    """per bone: the 4x4 world deform (posed world @ rest world^-1)."""
    Mw = arm.matrix_world; return {b.name: np.array(Mw @ arm.pose.bones[b.name].matrix @ (Mw @ b.matrix_local).inverted()) for b in arm.data.bones}


def unpose(V, W, names, Dm):
    A = np.zeros((len(V), 3, 3)); c = np.zeros((len(V), 3))
    for j, n in enumerate(names):
        w = W[:, j]
        if not (w > 0).any(): continue
        M = Dm[n]; A += w[:, None, None] * M[:3, :3]; c += w[:, None] * M[:3, 3]
    return np.linalg.solve(A, (V - c)[..., None])[..., 0]


def head_measure(V, z_brow, cx=0.0):
    """width and depth of the head at the brow, and the crown, from a body's vertices (above the neck)."""
    band = V[np.abs(V[:, 2] - z_brow) < 0.012]; band = band[np.abs(band[:, 0] - cx) < 0.14]
    return {'w': float(np.ptp(band[:, 0])), 'd': float(np.ptp(band[:, 1])), 'cx': float(band[:, 0].mean()), 'cy': float((band[:, 1].max() + band[:, 1].min()) / 2),
            'crown': float(V[:, 2].max())}


def main_bind(state):
    from bl_ext.user_default.mpfb.services.clothesservice import ClothesService
    fit, arm, donor, pieces, suit, lm = state['fit'], state['arm'], state['donor'], state['pieces'], state['suit'], state['lm']
    p = fit['params']; s_, t_ = p['s'], np.array([p['tx'], p['ty'], p['tz']])
    Dm = deform_mats(arm); names = donor['names']; height = lm['crown']
    rest = {}
    for kind, o in list(pieces.items()) + [('suit', suit)]:
        V = np.array([list(v.co) for v in o.data.vertices]); T = L.tris_of(o)
        if kind in ('hat', 'hair'):
            W = np.zeros((len(V), len(names))); W[:, names.index('Head')] = 1
        else:
            W, st, _, _ = L.transfer(donor, V, T, height)
        Vr = (unpose(V, W, names, Dm) - t_) / s_
        L.set_co(o, Vr); rest[kind] = o
    TM.lap('unpose')
    # the fit body at scale 1 in MPFB's rest: the binding target
    for x in (arm, state['b'], state['px']): bpy.data.objects.remove(x, do_unlink=True)
    arm1, b1, _ = L.human(fit['macros'], fit['sex'], proxy=False)
    for m in b1.modifiers:
        if m.type == 'MASK': m.show_viewport = False
    ref = ClothesService.get_reference_scale(b1)
    bco = L.eval_co(b1, armature=False); bodym = np.array([any(g.group == b1.vertex_groups['body'].index and g.weight > 0.5 for g in v.groups) for v in b1.data.vertices])
    lm1 = L.landmarks(arm1, bco[bodym]); hm1 = head_measure(bco[bodym], lm1['brow'])
    bind = {}
    for kind, o in rest.items():
        if kind in ('hat', 'hair'):
            continue
        t0 = time.time(); o.name = f'{FID}_{kind}'
        for g in list(o.vertex_groups): o.vertex_groups.remove(g)
        o.vertex_groups.new(name=G['bindGroup'][kind]).add(list(range(len(o.data.vertices))), 1.0, 'REPLACE')
        mh = ClothesService.create_mhclo_from_clothes_matching(b1, o, properties_dict={'name': f'{FID}_{kind}', 'uuid': f'fit-{FID}-{kind}', 'author': 'brief 069 fit test', 'license': 'CC0'})
        mh.clothes = o; path = os.path.join(PIECES, f'{FID}_{kind}.mhclo'); mh.write_mhclo(path, also_export_obj=True, reference_scale=ref)
        bind[kind] = {'mhclo': path, 'seconds': round(time.time() - t0, 2), 'png': os.path.join(PIECES, f'{FID}_{kind}.png')}
        L.log(FID, 'bound', kind, bind[kind]['seconds'], 's')
    # rigid head pieces: kept as obj in the fit body's unit rest space with the head they were made on
    for kind in ('hat', 'hair'):
        if kind not in rest: continue
        o = rest[kind]; path = os.path.join(PIECES, f'{FID}_{kind}_rigid.npz')
        Tq = np.array([list(f.vertices) for f in o.data.polygons], dtype=object)
        me = o.data; uv = np.empty(len(me.loops) * 2); me.uv_layers.active.data.foreach_get('uv', uv)
        np.savez(path, V=np.array([list(v.co) for v in me.vertices]), loops=np.array([l.vertex_index for l in me.loops]), starts=np.array([f.loop_start for f in me.polygons]),
                 totals=np.array([f.loop_total for f in me.polygons]), uv=uv.reshape(-1, 2), head=json.dumps(hm1), brow=lm1['brow'])
        bind[kind] = {'rigid': path, 'png': os.path.join(PIECES, f'{FID}_{kind}.png')}
    TM.lap('bind')
    json.dump(bind, open(os.path.join(OUT, 'bind.json'), 'w'), indent=1)
    return bind


# ------------------------------------------------------------------ variants
def rigid_obj(spec, name, img):
    z = np.load(spec)
    V = z['V']; loops = z['loops']; st = z['starts']; tt = z['totals']
    F = [list(loops[a:a + n]) for a, n in zip(st, tt)]
    o = L.mesh_obj(name, V, F); uvl = o.data.uv_layers.new(); uvl.data.foreach_set('uv', z['uv'].ravel())
    o.data.materials.append(L.simple_material(name + '_matte', img)); return o, json.loads(str(z['head'])), float(z['brow'])


def hat_rule(hat, head_old, head_new):
    """the hat rides Head, scaled by the wearer's head (width, depth at the brow; height as their mean) about the crown, seated on the new crown."""
    kx = head_new['w'] / head_old['w']; ky = head_new['d'] / head_old['d']; kz = (kx + ky) / 2
    C0 = np.array([head_old['cx'], head_old['cy'], head_old['crown']]); C1 = np.array([head_new['cx'], head_new['cy'], head_new['crown']])
    V = np.array([list(v.co) for v in hat.data.vertices]); L.set_co(hat, C1 + (V - C0) * np.array([kx, ky, kz]))
    return {'kx': round(kx, 3), 'ky': round(ky, 3), 'kz': round(kz, 3)}


def hair_under_hat(hat, objs, centre, brow): return L.hair_under_hat(hat, objs, centre, brow)


def cape_chain(arm, cape, n):
    """three bones down the centre back from the cape's top to its hem, parented to Spine2; the cape weighted along them, blended to Spine2 at the collar."""
    V = np.array([list(v.co) for v in cape.data.vertices]); back = V[(np.abs(V[:, 0]) < 0.06) & (V[:, 1] > np.median(V[:, 1]))]
    if len(back) < 4: back = V
    top = back[np.argmax(back[:, 2])]; bot = back[np.argmin(back[:, 2])]
    L.bp.ctx(arm, 'EDIT'); eb = arm.data.edit_bones; prev = eb['Spine2']; heads = []
    for i in range(n):
        a = top + (bot - top) * i / n; b = top + (bot - top) * (i + 1) / n
        e = eb.new(f'Cape_{i + 1}'); e.head = Vector(a); e.tail = Vector(b); e.parent = prev; e.use_connect = i > 0; e.use_deform = True; prev = e; heads.append(a)
    L.bp.ctx(arm, 'OBJECT')
    u = np.clip((top[2] - V[:, 2]) / max(top[2] - bot[2], 1e-3), 0, 1)
    names = ['Spine2'] + [f'Cape_{i + 1}' for i in range(n)]; W = np.zeros((len(V), len(names)))
    W[:, 0] = np.clip(1 - u / 0.2, 0, 1)
    for i in range(n):
        c = (i + 0.5) / n; W[:, i + 1] = np.clip(1 - np.abs(u - c) * n, 0, 1) * (1 - W[:, 0])
    W[:, 1] += np.where(W.sum(1) == 0, 1, 0)
    L.set_weights(cape, W, names, 4)


def props(arm, att, suit): return L.props(arm, att, suit)


def variant(vname, macros, bind, fit):
    t0 = time.time()
    L.reset() if False else bpy.ops.wm.read_factory_settings(use_empty=True)
    L.addon_utils.enable('bl_ext.user_default.mpfb', default_set=True, persistent=False)
    from bl_ext.user_default.mpfb.services.humanservice import HumanService
    arm, b, _ = L.human(macros, fit['sex'], proxy=False)
    for m in b.modifiers:
        if m.type == 'MASK': m.show_viewport = False
    bco = L.eval_co(b, armature=False); bodym = np.array([any(g.group == b.vertex_groups['body'].index and g.weight > 0.5 for g in v.groups) for v in b.data.vertices])
    lmv = L.landmarks(arm, bco[bodym]); hmv = head_measure(bco[bodym], lmv['brow'])
    objs = {}
    for kind, spec in bind.items():
        img = bpy.data.images.load(spec['png'])
        if 'mhclo' in spec:
            o = HumanService.add_mhclo_asset(spec['mhclo'], b, asset_type='Clothes', subdiv_levels=0, material_type='NONE', import_subrig=False, import_weights=False)
            for md in list(o.modifiers):
                if md.type not in ('ARMATURE',): o.modifiers.remove(md)
            o.data.materials.clear(); o.data.materials.append(L.simple_material(f'{FID}_{kind}_matte', img))
            L.bake_world(o, arm) if hasattr(L, 'bake_world') else L.bp.bake_world(o, arm)
            if not any(md.type == 'ARMATURE' for md in o.modifiers): md = o.modifiers.new('Armature', 'ARMATURE'); md.object = arm
        else:
            o, head_old, brow_old = rigid_obj(spec['rigid'], f'{FID}_{kind}', img); objs[kind + '_head'] = head_old
            o.parent = arm; md = o.modifiers.new('Armature', 'ARMATURE'); md.object = arm
            L.set_weights(o, np.ones((len(o.data.vertices), 1)), ['Head'], 1)
        o.name = f'{FID}_G_{vname}_{kind}'; objs[kind] = o
    info = {'macros': macros}
    for kind in ('hat', 'hair'):
        if kind in objs: info[kind + 'Scale'] = hat_rule(objs[kind], objs[kind + '_head'], hmv)
    if 'hat' in objs:
        Hd = arm.matrix_world @ arm.data.bones['Head'].head_local; Tl = arm.matrix_world @ arm.data.bones['Head'].tail_local
        centre = (np.array(Hd) + np.array(Tl)) / 2
        info['hairUnderHatMoved'] = hair_under_hat(objs['hat'], [objs[k] for k in ('suit', 'hair') if k in objs], centre, lmv['brow'])
    bpy.data.objects.remove(b, do_unlink=True)
    if 'cape' in objs: cape_chain(arm, objs['cape'], G['cape']['bones'])
    meshes = [objs[k] for k in objs if not k.endswith('_head')]
    L.pose_blender(arm, L.standard_aims(arm)); L.apply_rest(arm, meshes)
    suit = objs['suit']; sco = L.eval_co(suit); att = L.add_attach(arm, sco)
    pr = props(arm, att, suit)
    info['restLegs'] = L.rest_leg_report(arm); info['heightM'] = round(float(sco[:, 2].max()), 3)
    for k, o in objs.items():
        if k.endswith('_head'): continue
        L.tag(o, 'suit' if k == 'suit' else 'hair' if k == 'hair' else 'garment'); o['fit_kind'] = k
    lods = list(meshes) + pr
    tri0 = sum(len(L.tris_of(o)) for o in meshes)
    for lod, tot in enumerate(L.D['lods']['tris'][1:], 1):
        for o in meshes:
            c = o.copy(); c.data = o.data.copy(); bpy.context.scene.collection.objects.link(c); c.name = f'{o.name}_LOD{lod}'
            for md in list(c.modifiers):
                if md.type == 'ARMATURE': c.modifiers.remove(md)
            L.decimate_to(c, max(60, int(len(L.tris_of(o)) * tot / L.D['lods']['tris'][0])))
            md = c.modifiers.new('Armature', 'ARMATURE'); md.object = arm; L.tag(c, o['fit_role'], lod); c['fit_kind'] = o['fit_kind']; lods.append(c)
    arm.name = 'Armature'; arm['fit_weight'] = macros.get('weight', 0.5)
    path = os.path.join(OUT, f'{FID}_G_{vname}')
    bpy.ops.wm.save_as_mainfile(filepath=path + '.blend', compress=True)
    L.export_fbx(path + '.fbx', [arm] + lods, arm)
    info.update({'tris': tri0, 'trisProps': sum(len(L.tris_of(o)) for o in pr), 'pieces': {k: len(L.tris_of(o)) for k, o in objs.items() if not k.endswith('_head')},
                 'materials': len(set(m.name for o in meshes + pr for m in o.data.materials)), 'bones': len([b for b in arm.data.bones if b.use_deform]),
                 'attach': sorted(att), 'seconds': round(time.time() - t0, 2)})
    STATS['variants'][vname] = info
    L.log(FID, 'variant', vname, info['heightM'], info['tris'], info.get('hatScale'), info.get('hairUnderHatMoved'), info['seconds'])


def run():
    fit = json.load(open(os.path.join(L.SCR, 'fit', f'{FID}.fit.json'))); row = {r['id']: r for r in L.D['inputs']}[FID]
    if '--variants-only' in ARGS:
        bind = json.load(open(os.path.join(OUT, 'bind.json'))); STATS.update({k: v for k, v in json.load(open(os.path.join(OUT, f'{FID}_G.json'))).items() if k in ('pieces',)})
    else:
        state = main_cut()
        if ONLY_CUT: return
        bind = main_bind(state)
    vs = {'fit': dict(fit['macros'])}
    for k, v in L.D['extremes']['bodies'].items(): m = dict(fit['macros']); m.update(v); vs[k] = m
    only = [a.split('=', 1)[1].split(',') for a in ARGS if a.startswith('--variants=')]
    for vname, mac in vs.items():
        if only and vname not in only[0]: continue
        variant(vname, mac, bind, fit)
    STATS['seconds'] = TM.t; STATS['sex'] = fit['sex']
    json.dump(STATS, open(os.path.join(OUT, f'{FID}_G.json'), 'w'), indent=1)
    L.log('DONE G', FID, TM.t)


if __name__ == '__main__':
    run()
