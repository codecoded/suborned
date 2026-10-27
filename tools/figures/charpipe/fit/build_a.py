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
    W, pit_n = L.armpit_merge(Vt, W, donor['names']); rule_counts['armpitMerged'] = pit_n; tm.lap('rules')
    L.set_weights(src, W, donor['names'], L.D['transfer']['maxInfluences'])
    src.parent = arm; md = src.modifiers.new('Armature', 'ARMATURE'); md.object = arm
    KINDS = ['', 'hair', 'coat', 'skirt', 'cape', 'hat']
    at = src.data.attributes.new('fit_lab', 'INT', 'POINT'); at.data.foreach_set('value', np.array([KINDS.index(x) for x in lab], np.int32))
    # the fitted pose becomes the rest, then the standard rest is posed and applied
    bpy.data.objects.remove(b, do_unlink=True)
    for m in list(src.modifiers):
        if m.type == 'ARMATURE': src.modifiers.remove(m)       # the source is already in the fitted pose
    L.apply_rest(arm, [px])                                     # the donor proxy keeps its fitted pose; the rig's rest becomes the fitted pose
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
    legs, avatar = humanoid_zero(fid, arm, hybrid)
    lab_now = np.empty(len(src.data.vertices), np.int32); src.data.attributes['fit_lab'].data.foreach_get('value', lab_now)
    np.save(os.path.join(OUT, f'{fid}_{TAGN(hybrid)}_labels.npy'), np.array(KINDS, dtype=object)[lab_now].astype(str))
    # LOD1, LOD2 from LOD0 (Blender's decimate carries the weights)
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
                c = o.copy(); c.data = o.data.copy(); bpy.context.scene.collection.objects.link(c); c.name = f'{o.name}_LOD{k}'
                for m in list(c.modifiers):
                    if m.type == 'ARMATURE': c.modifiers.remove(m)
                L.decimate_to(c, max(60, int(len(L.tris_of(o)) * t / L.D['lods']['tris'][0]))); m = c.modifiers.new('Armature', 'ARMATURE'); m.object = arm
                L.tag(c, 'garment', k); c['fit_kind'] = o['fit_kind']; lods.append(c)
    props = [o for o in extra if o.get('fit_role') == 'prop']
    lods += [o for o in extra if o.get('fit_role') != 'prop']
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
             'attach': sorted(att), 'rest': L.rest_mode(), 'avatarT': avatar, 'materials': 1, 'texture': list(img.size) if img else None, 'transfer': st, 'crumbsDropped': crumbs, 'sourceParts': parts, 'looseRules': rule_counts,
             'labels': {k: int((lab == k).sum()) for k in sorted(set(lab.tolist())) if k}, 'restLegs': legs, 'seconds': tm.t, 'heightM': round(height, 3)}
    if L.SET in ('e2e', 'fixer'): json.dump(stats.pop('seconds'), open(L.work(f'{fid}_{TAGN(hybrid)}.seconds.json'), 'w'), indent=1)
    json.dump(stats, open(os.path.join(OUT, f'{fid}_{TAGN(hybrid)}.json'), 'w'), indent=1, sort_keys=True)
    L.log('DONE A', fid, tris, st, rule_counts)


def TAGN(h): return 'H' if h else 'A'


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
        a2.name = 'Armature_avatarT'; path = os.path.join(OUT, f'{fid}_{TAGN(hybrid)}_avatarT.fbx')
        a2.name = 'Armature'; arm.name = 'Armature_figure'          # the avatar file's armature carries the figure's own name, as Unity maps by path
        L.export_fbx(path, [a2], a2); a2.name = 'Armature_avatarT'; arm.name = 'Armature'
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
            hat = L.separate_tris_mask(src, hf); hat.name = f'{fid}_hat'
            for g in list(hat.vertex_groups): hat.vertex_groups.remove(g)
            L.set_weights(hat, np.ones((len(hat.data.vertices), 1)), ['Head'], 1)
            L.tag(hat, 'garment'); hat['fit_kind'] = 'hat'; out.append(hat)
            hd = arm.matrix_world @ arm.data.bones['Head'].head_local; tl = arm.matrix_world @ arm.data.bones['Head'].tail_local
            lm = L.landmarks(arm, L.eval_co(src)); info['hairUnderHatMoved'] = L.hair_under_hat(hat, [src], (np.array(hd) + np.array(tl)) / 2, lm['brow'])
        else: info['welded'] = 'the cut is not clean: the hat stays in the A mesh, rigid on Head (the loose rule)'
    if L.SET in ('e2e', 'fixer') and L.E2E.get('props'):
        pr = L.props(arm, att, src); out += pr; info['props'] = [o.name for o in pr]
    return out, info


if __name__ == '__main__':
    a = sys.argv[sys.argv.index('--') + 1:]
    for fid in [x for x in a if not x.startswith('--')]: build(fid, '--hybrid' in a)
