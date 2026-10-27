"""Stage 2A (brief 069 fit test): keep the generated mesh. The fitted base is the skeleton and weight donor; the source mesh and its texture are kept,
reduced to the LOD budgets, weighted by robust transfer + inpainting + the loose rules, and posed to the standard rest (reference T-pose, neutral legs).
  out/scratch/forma/char-pipeline/blender.sh -P tools/figures/charpipe/fit/build_a.py -- <input id> [...]
  ... -- <input id> [...] --hybrid    the hybrid (brief 069 end-to-end): A, plus the hat cut out as its own rigid piece on Head when the cut is clean
                                        (e2e.json hat; else left welded), the hair-under-hat rule, and the props on the attachment points.
Writes <out>/a/<id>_A.blend (Armature, the LODs tagged fit_role=body, the hidden donor proxy tagged donor), <id>_A.fbx and <id>_A.json (stats);
the hybrid writes <id>_H.*.  FIT_SET=e2e runs it on the end-to-end people (lib.SET)."""
import os, sys, json, math, time, importlib.util
import numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
L = importlib.util.module_from_spec(s := importlib.util.spec_from_file_location('fitlib', os.path.join(HERE, 'lib.py'))); s.loader.exec_module(L)
import bpy
from mathutils import Vector, Matrix

OUT = L.ensure(os.path.join(L.SCR, 'a'))


def write_avatar_meta(fid, path):
    """the _avatarT's Unity meta (Create From This Model, auto-mapping off): Unity's own T-pose for this file (tools/figures/avatar.py, MakePoseValid)
    and its human map, with the finger limits widened for finger-boned figures (fit.json transfer.fingers.humanLimits). Latch's import takes the
    figure's human rows from this avatar, so the limits reach the figure."""
    sys.path.append(os.path.dirname(os.path.dirname(HERE))); import avatar as av, unitymeta as um
    row = {r['id']: r for r in L.D['inputs']}[fid]; F = L.D['transfer'].get('fingers', {})
    nodes = av.read_fbx(path, os.path.splitext(os.path.basename(path))[0] + '(Clone)'); b = av.map_bones(nodes); av.make_pose_valid(b)
    human = [(b[i].name, av.HUMAN[i]) for i in sorted(b)]
    tab = json.load(open(os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(HERE))), '..', F['humanLimits']))) if F.get('humanLimits') else None
    lim = av.finger_limits(tab, human) if tab and L.budget(row)['skeleton'] in F.get('skeletons', []) else None
    h, sk = av.human_description(human, nodes, lim)
    rel = f'Figures/{fid}/{os.path.basename(path)}'
    open(path + '.meta', 'w').write(um.model_meta(rel, {}, avatar={'human': h, 'skeleton': sk, 'source': None}).replace('  importAnimation: 0', '  importAnimation: 0', 1))
    return {k: v for k, v in (lim or {}).items()}


def build(fid, hybrid=False):
    tm = L.Timer(); fit = json.load(open(os.path.join(L.SCR, 'fit', f'{fid}.fit.json'))); row = {r['id']: r for r in L.D['inputs']}[fid]
    bpy.ops.wm.open_mainfile(filepath=L.src_blend(fid))
    for o in list(bpy.data.objects):
        if o.name.endswith('_fitproxy'): bpy.data.objects.remove(o, do_unlink=True)
    src = bpy.data.objects[f'{fid}_src']; img = L.base_color_image(src)
    if not (L.SET in ('e2e', 'fixer') and L.E2E.get('lod0', {}).get('mode') == 'source'): L.clean_normals(src)
    L.addon_utils.enable('bl_ext.user_default.mpfb', default_set=True, persistent=False)
    arm, b, px, donor = L.fitted_human(fit); tm.lap('body')
    lm = L.landmarks(arm, donor['V']); height = lm['crown']
    # LOD0: the source decimated to budget, one matte material with the source's base colour
    L.flat_material(src, img)
    if not (L.SET in ('e2e', 'fixer') and L.E2E.get('lod0', {}).get('mode') == 'source'): L.decimate_to(src, L.D['lods']['tris'][0])
    src.name = f'{fid}_A_LOD0'; tm.lap('lod0')
    crumbs, parts = L.drop_crumbs(src, L.D['transfer']['crumbPct'])
    Vt = np.array([list(v.co) for v in src.data.vertices]); Tt = L.tris_of(src)
    W, st, dist, nb = L.transfer(donor, Vt, Tt, height); tm.lap('transfer')
    lab = L.classify(Vt, Tt, dist, nb, lm, row['wears'])
    W, rule_counts = L.loose_rules(W, donor['names'], Vt, lab, lm, arm)
    W, pit_n = L.armpit_merge(Vt, W, donor['names']); rule_counts['armpitMerged'] = pit_n
    W, bm = L.band_merge(Vt, W, lm); rule_counts['bandMerge'] = bm
    if L.SET in ('e2e', 'fixer'):
        W, rule_counts['hands'] = L.hand_rule(Vt, Tt, W, donor['names'], arm)
        W, rule_counts['layerMerge'] = L.layer_merge(Vt, W)
    tm.lap('rules')
    L.set_weights(src, W, donor['names'], L.D['transfer']['maxInfluences'])
    src.parent = arm; md = src.modifiers.new('Armature', 'ARMATURE'); md.object = arm
    KINDS = ['', 'hair', 'coat', 'skirt', 'cape', 'hat']
    at = src.data.attributes.new('fit_lab', 'INT', 'POINT'); at.data.foreach_set('value', np.array([KINDS.index(x) for x in lab], np.int32))
    # the fitted pose becomes the rest, then the standard rest is posed and applied
    bpy.data.objects.remove(b, do_unlink=True)
    for m in list(src.modifiers):
        if m.type == 'ARMATURE': src.modifiers.remove(m)       # the source is already in the fitted pose
    L.apply_rest(arm, [px])                                     # the donor proxy keeps its fitted pose; the rig's rest becomes the fitted pose
    if L.SET in ('e2e', 'fixer') and L.budget(row)['skeleton'] in L.D['transfer'].get('fingers', {}).get('skeletons', []):
        rule_counts['fingers'] = L.finger_fit(src, arm)         # the finger bones in the generated fingers, weighted from them (52-bone figures)
    m = src.modifiers.new('Armature', 'ARMATURE'); m.object = arm
    if L.rest_mode() == 'tpose': L.pose_blender(arm, L.standard_aims(arm)); L.apply_rest(arm, [src, px])
    tm.lap('rest')
    # back to the fitted macros' own MPFB size (the fit's global scale and offset taken out), like G
    pr = fit['params']; sc_, t_ = pr['s'], np.array([pr['tx'], pr['ty'], pr['tz']])
    for o in (src, px): L.set_co(o, (np.array([list(v.co) for v in o.data.vertices]) - t_) / sc_)
    L.bp.ctx(arm, 'EDIT')
    for e in arm.data.edit_bones: e.head = Vector((np.array(e.head) - t_) / sc_); e.tail = Vector((np.array(e.tail) - t_) / sc_)
    L.bp.ctx(arm, 'OBJECT')
    body_co = L.eval_co(px); att = L.add_attach(arm, body_co)
    extra = []; hyb = {}
    if hybrid: extra, hyb = hybrid_parts(fid, src, arm, px, att)
    rigs = L.garment_rigs(arm, src, px, bool(rule_counts.get('bandMerge', {}).get('applied'))) if L.SET in ('e2e', 'fixer') and os.environ.get('FIT_RIGS', 'on') != 'off' else []   # FIT_RIGS=off: the same figure without garment chains
    if L.SET in ('e2e', 'fixer') and L.budget(row)['skeleton'] == 22:
        # the crowd skeleton (fit.json budgets): the finger bones go (their weights are already in Hand: the hand rule), every other bone stays,
        # the attachment bones included (build.py's crowd_skeleton prunes to the 22 core bones and would drop them)
        import re
        keep = [b.name for b in arm.data.bones if not re.match(r'(Left|Right)Hand(Thumb|Index|Middle|Ring|Pinky)[0-9]$', b.name)]
        L.bp.prune(arm, [src, px] + [o for o in extra], keep, prefix='')
    legs, avatar = humanoid_zero(fid, arm, hybrid)
    lab_now = np.empty(len(src.data.vertices), np.int32); src.data.attributes['fit_lab'].data.foreach_get('value', lab_now)
    np.save(os.path.join(OUT, f'{fid}_{TAGN(hybrid)}_labels.npy'), np.array(KINDS, dtype=object)[lab_now].astype(str))
    if L.SET in ('e2e', 'fixer'):
        lods, tris, props, patches = budget_lods(fid, row, src, arm, extra, hybrid)
        src = lods[0]
        np.save(os.path.join(OUT, f'{fid}_{TAGN(hybrid)}_labels.npy'), lod_labels(src))
    else:
        lods, tris, props, patches = old_lods(fid, src, arm, extra)
    src['fit_labels'] = os.path.join(OUT, f'{fid}_{TAGN(hybrid)}_labels.npy'); arm['fit_weight'] = fit['macros']['weight']
    L.tag(px, 'donor'); px.hide_render = True; px.name = f'{fid}_A_donor'
    arm.name = 'Armature'; tm.lap('lods')
    bpy.ops.wm.save_as_mainfile(filepath=L.work(f'{fid}_{TAGN(hybrid)}.blend') if L.SET in ('e2e', 'fixer') else os.path.join(OUT, f'{fid}_{TAGN(hybrid)}.blend'), compress=True)
    if L.SET in ('e2e', 'fixer') and img is not None:       # the material points at the texture delivered beside the FBX (e2e_textures.py), by its file name
        ext = {'JPEG': 'jpg', 'PNG': 'png', 'WEBP': 'webp'}.get(img.file_format, 'png')       # the glb's own image format (e2e_textures.py copies it as is)
        img.name = f'{fid}_{TAGN(hybrid)}_albedo.{ext}'; img.filepath_raw = os.path.join(OUT, img.name)
    L.export_fbx(os.path.join(OUT, f'{fid}_{TAGN(hybrid)}.fbx'), [arm] + lods, arm)          # the figure: body, LODs and its own pieces; never the props
    if props: L.export_fbx(os.path.join(OUT, f'{fid}_{TAGN(hybrid)}_props.fbx'), [arm] + props, arm)
    tm.lap('export')
    stats = {'id': fid, 'approach': TAGN(hybrid), 'hybrid': hyb, 'sex': fit['sex'], 'macros': fit['macros'], 'lodTris': tris, 'srcTris': fit['srcTris'], 'bones': len([b for b in arm.data.bones if b.use_deform]),
             'attach': sorted(att), 'garmentRigs': rigs, 'bodyHeightM': round(float(L.eval_co(px)[:, 2].max() - L.eval_co(px)[:, 2].min()), 4),
             'targetHeightM': fit.get('targetHeightM'), 'heightClass': fit.get('heightClass'), 'colourPatches': patches, 'kind': row.get('kind'), 'texPx': L.budget(row)['texPx'] if L.SET in ('e2e', 'fixer') else None, 'rest': L.rest_mode(), 'avatarT': avatar, 'materials': 1, 'texture': list(img.size) if img else None, 'transfer': st, 'crumbsDropped': crumbs, 'sourceParts': parts, 'looseRules': rule_counts,
             'labels': {k: int((lab == k).sum()) for k in sorted(set(lab.tolist())) if k}, 'restLegs': legs, 'seconds': tm.t, 'heightM': round(height, 3)}
    if L.SET in ('e2e', 'fixer'): json.dump(stats.pop('seconds'), open(L.work(f'{fid}_{TAGN(hybrid)}.seconds.json'), 'w'), indent=1)
    json.dump(stats, open(os.path.join(OUT, f'{fid}_{TAGN(hybrid)}.json'), 'w'), indent=1, sort_keys=True)
    L.log('DONE A', fid, tris, st, rule_counts)


def TAGN(h): return 'H' if h else 'A'


def old_lods(fid, src, arm, extra):
    """the fit test's LODs: LOD0 as built, LOD1 and LOD2 decimated from it; pieces keep their own LOD copies."""
    lods = [src]; tris = [len(L.tris_of(src))]
    for k, t in enumerate(L.D['lods']['tris'][1:], 1):
        c = src.copy(); c.data = src.data.copy(); bpy.context.scene.collection.objects.link(c); c.name = f'{fid}_A_LOD{k}'
        for m in list(c.modifiers):
            if m.type == 'ARMATURE': c.modifiers.remove(m)
        tris.append(L.decimate_to(c, t)); m = c.modifiers.new('Armature', 'ARMATURE'); m.object = arm; lods.append(c)
    for k, o in enumerate(lods): L.tag(o, 'body', k)
    for o in [x for x in extra]:
        if o.get('fit_role') == 'garment':
            for k, t in enumerate(L.D['lods']['tris'][1:], 1):
                c = o.copy(); c.data = o.data.copy(); bpy.context.scene.collection.objects.link(c); c.name = o.name.replace('_LOD0', '') + f'_LOD{k}'
                for m in list(c.modifiers):
                    if m.type == 'ARMATURE': c.modifiers.remove(m)
                L.decimate_to(c, max(60, int(len(L.tris_of(o)) * t / L.D['lods']['tris'][0]))); m = c.modifiers.new('Armature', 'ARMATURE'); m.object = arm
                L.tag(c, 'garment', k); c['fit_kind'] = o['fit_kind']; lods.append(c)
    props = [o for o in extra if o.get('fit_role') == 'prop']
    lods += [o for o in extra if o.get('fit_role') != 'prop']
    return lods, tris, props, []


def lod_labels(o):
    KINDS = ['', 'hair', 'coat', 'skirt', 'cape', 'hat']
    if 'fit_lab' not in o.data.attributes: return np.array([''] * len(o.data.vertices))
    a = np.empty(len(o.data.vertices), np.int32); o.data.attributes['fit_lab'].data.foreach_get('value', a)
    return np.array(KINDS, dtype=object)[np.clip(a, 0, len(KINDS) - 1)].astype(str)


def free_cell(o, grid):
    """the last grid cell (scanning from the texture's bottom-right) that no UV triangle of o touches: room for a merged prop's colour."""
    me = o.data; uv = np.empty(len(me.loops) * 2); me.uv_layers.active.data.foreach_get('uv', uv); uv = uv.reshape(-1, 2)
    me.calc_loop_triangles(); li = np.empty(len(me.loop_triangles) * 3, np.int64); me.loop_triangles.foreach_get('loops', li); tri = uv[li].reshape(-1, 3, 2)
    used = np.zeros((grid, grid), bool)
    lo = np.clip(np.floor(tri.min(1) * grid).astype(int), 0, grid - 1); hi = np.clip(np.floor(tri.max(1) * grid).astype(int), 0, grid - 1)
    for (a, b), (c, d) in zip(lo, hi): used[a:c + 1, b:d + 1] = True              # [u cell, v cell]: conservative (bounding boxes)
    for u in range(grid - 1, -1, -1):
        for v in range(grid):
            if not used[u, v] and (u == 0 or not used[u - 1, v]) and (u == grid - 1 or not used[u + 1, v]): return u, v
    return None


def budget_lods(fid, row, full, arm, extra, hybrid):
    """fit.json budgets for an end-to-end figure: each LOD reduced from the full generated mesh (lib.reduce_lod: face and hands protected, the full
    mesh's normals carried over); garment pieces the same; worn props and pieces merged into the body at each LOD for the crowd, so one renderer per
    LOD; a merged prop takes its colour from a patch painted in a free corner of the figure's texture (e2e_textures.py paints it)."""
    B = L.budget(row); full.name = f'{fid}_full'
    pieces = [o for o in extra if o.get('fit_role') == 'garment']
    worn = [o for o in extra if o.get('fit_role') == 'prop' and L.D['attach']['props'].get(o.name.replace('prop_', '').split('.')[0], {}).get('worn')]
    loose = [o for o in extra if o.get('fit_role') == 'prop' and o not in worn]
    patches = []
    if B['mergeWorn'] and worn:
        grid = 128; mat = full.data.materials[0]
        for o in worn:
            cell = free_cell(full, grid)
            col = list(o.data.materials[0].node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value)[:3] if o.data.materials else [0.5, 0.5, 0.5]
            if cell is None: continue
            u, v = cell; uvv = ((u + 0.5) / grid, (v + 0.5) / grid)
            uvl = o.data.uv_layers[0] if len(o.data.uv_layers) else o.data.uv_layers.new(name='UVMap')
            uvl.data.foreach_set('uv', np.tile(uvv, len(o.data.loops)))
            o.data.materials.clear(); o.data.materials.append(mat)
            patches.append({'prop': o.name, 'grid': grid, 'cell': [int(u), int(v)], 'rgbLinear': [round(float(c), 4) for c in col]})
    n0 = len(L.tris_of(full)); lods = []; tris = []; out_props = list(loose)
    for k, t in enumerate(B['lodTris']):
        share = (t if t is not None else n0) / n0
        parts = []
        for o in pieces:
            keep_full = t is None or (k == 0 and L.D['reduction'].get('piecesFullAtLod0'))
            pc, _ = L.reduce_lod(o, None if keep_full else max(60, int(len(L.tris_of(o)) * share)), o.name.replace('_LOD0', '') + f'_LOD{k}', arm, k)
            pc['fit_kind'] = o.get('fit_kind', 'garment'); parts.append(pc)
        extra_tris = sum(len(L.tris_of(x)) for x in parts) + (sum(len(L.tris_of(x)) for x in worn) if B['mergeWorn'] else 0)
        if t is None:
            body = full.copy(); body.data = full.data.copy(); bpy.context.scene.collection.objects.link(body); body.name = f'{fid}_A_LOD{k}'
            for m in list(body.modifiers): body.modifiers.remove(m)
            md = body.modifiers.new('Armature', 'ARMATURE'); md.object = arm; body.parent = arm; nb = n0
        else:   # the LOD's budget is the renderer's: merged pieces and worn props first, the body takes the rest
            body, nb = L.reduce_lod(full, max(200, t - extra_tris) if B['mergeWorn'] else t, f'{fid}_A_LOD{k}', arm, k)
        wl = []
        for o in worn:
            c = o.copy(); c.data = o.data.copy(); bpy.context.scene.collection.objects.link(c); c.name = f'{o.name}_LOD{k}'; wl.append(c)
        if B['mergeWorn']:
            join = [x for x in parts + wl]
            if join:
                bpy.ops.object.select_all(action='DESELECT')
                for x in join + [body]: x.select_set(True)
                bpy.context.view_layer.objects.active = body; bpy.ops.object.join()
            L.tag(body, 'body', k); lods.append(body)
        else:
            L.tag(body, 'body', k); lods.append(body)
            for pc in parts: L.tag(pc, 'garment', k); lods.append(pc)
            if k == 0: out_props += wl
            else:
                for c in wl: bpy.data.objects.remove(c, do_unlink=True)
        tris.append(len(L.tris_of(body)))
    # the full mesh stays only as the normal source: removed, with the pieces and worn props now inside the LODs
    for o in pieces + worn: bpy.data.objects.remove(o, do_unlink=True)
    lab = lod_labels(full); bpy.data.objects.remove(full, do_unlink=True)
    return lods, tris, out_props, patches


def humanoid_zero(fid, arm, hybrid):
    """the Humanoid zero: the reference T-pose with Latch's neutral legs, on a copy of the armature. With the source rest the mesh is never re-posed:
    the zero goes out as an armature-only FBX for Unity's avatar (Copy From Other Avatar) and onto the armature (fit_zero, bone -> armature-space
    4x4) for the Blender retarget. Returns the zero's measured legs and the avatar file."""
    a2 = arm.copy(); a2.data = arm.data.copy(); bpy.context.scene.collection.objects.link(a2)
    L.pose_blender(a2, L.standard_aims(a2)); L.bp.ctx(a2, 'POSE'); bpy.ops.pose.armature_apply(selected=False); L.bp.ctx(a2, 'OBJECT')
    legs = L.rest_leg_report(a2)
    arm['fit_zero'] = json.dumps({b.name: [list(r) for r in b.matrix_local] for b in a2.data.bones}, sort_keys=True)
    path = None
    if L.rest_mode() == 'source':
        path = os.path.join(OUT, f'{fid}_{TAGN(hybrid)}_avatarT.fbx')
        # the same node layout as the figure file (Latch's take 1): the figure's armature steps aside first, so this one is named 'Armature' (not
        # 'Armature.001'), and a one-triangle marker mesh sits beside it as a second root, so Unity keeps root / Armature / Hips instead of folding a
        # single root node into the prefab root
        arm.name = 'Armature_figure'; a2.name = 'Armature'; a2.data.name = 'Armature'
        mk = L.mesh_obj(f'{fid}_avatar_marker', [(0, 0, 0), (0.001, 0, 0), (0, 0.001, 0)], [(0, 1, 2)])
        L.export_fbx(path, [a2, mk], a2); bpy.data.objects.remove(mk, do_unlink=True); a2.name = 'Armature_avatarT'; arm.name = 'Armature'
        if L.SET in ('e2e', 'fixer'): write_avatar_meta(fid, path)
    bpy.data.objects.remove(a2, do_unlink=True)
    return legs, os.path.basename(path) if path else None


def hybrid_parts(fid, src, arm, px, att):
    """the hybrid's G pieces on an A figure (already in the standard rest): the hat cut out as a rigid Head piece when clean, the hair-under-hat
    rule on the body, the props on the attachment points."""
    H = L.E2E['hat'] if hasattr(L, 'E2E') else {'minMainPart': 0.8, 'rimM': 0.01}
    info = {}; out = []
    lab = np.empty(len(src.data.vertices), np.int32); src.data.attributes['fit_lab'].data.foreach_get('value', lab)
    hat_v = lab == 5
    if hat_v.any():
        F = [list(p.vertices) for p in src.data.polygons]; hf = np.array([all(hat_v[i] for i in f) for f in F])
        V = np.array([list(v.co) for v in src.data.vertices])
        # shells of the whole mesh, and the hat's share of each
        import bmesh
        bm = bmesh.new(); bm.from_mesh(src.data); bm.faces.ensure_lookup_table(); shell = np.full(len(bm.faces), -1); k = 0
        for f in bm.faces:
            if shell[f.index] >= 0: continue
            st = [f]
            while st:
                x = st.pop()
                if shell[x.index] >= 0: continue
                shell[x.index] = k; st.extend(o for e in x.edges for o in e.link_faces if shell[o.index] < 0)
            k += 1
        share = np.bincount(shell, hf.astype(float), k) / np.maximum(np.bincount(shell, minlength=k), 1)
        own = share[shell] >= H['shellShare']                      # faces in the hat's own shells
        cutf = hf & ~own                                           # hat faces left in shared shells: a real cut
        rim = set()
        for e in bm.edges:
            lf = list(e.link_faces)
            if any(cutf[f.index] for f in lf) and any(not hf[f.index] for f in lf): rim.update(v.index for v in e.verts if not hat_v[v.index])
        bm.free()
        main = float((hf & own).sum()) / max(1, int(hf.sum())); parts = [int(x) for x in np.nonzero(share >= H['shellShare'])[0]]
        hv = V[np.unique(np.concatenate([F[i] for i in np.nonzero(own | hf)[0]]))]; hull = L.bp.hull2([(p[0], p[1]) for p in hv]) if len(hv) > 2 else []
        def inside(p):
            if len(hull) < 3: return False
            c = np.mean(np.array(hull), 0); q = np.array(p[:2]); d = q - c; r = np.linalg.norm(d)
            if r < 1e-9: return True
            t = L.bp.ray_hull(L.Vector(c), L.Vector(d / r), hull); return r <= t + H['rimM']
        rim_ok = [inside(V[i]) and V[i, 2] < hv[:, 2].max() for i in rim]
        clean = main >= H['minOwnShells'] and (all(rim_ok) if rim_ok else True)
        hf = own | hf
        info = {'hatFaces': int(hf.sum()), 'hatInOwnShells': round(float(main), 3), 'hatShells': len(parts), 'rimVerts': len(rim), 'rimUnderHat': int(sum(rim_ok)), 'clean': bool(clean)}
        if clean:
            hat = L.separate_tris_mask(src, hf); hat.name = f'{fid}_hat_LOD0'          # every piece's LOD0 is named _LOD0 (Latch's take 1: an unsuffixed cap drew at every distance)
            for g in list(hat.vertex_groups): hat.vertex_groups.remove(g)
            L.set_weights(hat, np.ones((len(hat.data.vertices), 1)), ['Head'], 1)
            L.tag(hat, 'garment'); hat['fit_kind'] = 'hat'; out.append(hat)
            hd = arm.matrix_world @ arm.data.bones['Head'].head_local; tl = arm.matrix_world @ arm.data.bones['Head'].tail_local
            lm = L.landmarks(arm, L.eval_co(src))
            info['hairUnderHatMoved'] = L.hair_under_hat(hat, [src], (np.array(hd) + np.array(tl)) / 2, lm['brow']) if H.get('hairRule', True) else 0
        else: info['welded'] = 'the cut is not clean: the hat stays in the A mesh, rigid on Head (the loose rule)'
    if L.SET in ('e2e', 'fixer') and L.E2E.get('props'):
        pr = L.props(arm, att, src, skip=L.role_skips({r['id']: r for r in L.D['inputs']}[fid])); out += pr; info['props'] = [o.name for o in pr]
    return out, info


if __name__ == '__main__':
    a = sys.argv[sys.argv.index('--') + 1:]
    for fid in [x for x in a if not x.startswith('--')]: build(fid, '--hybrid' in a)
