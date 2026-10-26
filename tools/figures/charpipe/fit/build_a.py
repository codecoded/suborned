"""Stage 2A (brief 069 fit test): keep the generated mesh. The fitted base is the skeleton and weight donor; the source mesh and its texture are kept,
reduced to the LOD budgets, weighted by robust transfer + inpainting + the loose rules, and posed to the standard rest (reference T-pose, neutral legs).
  out/scratch/forma/char-pipeline/blender.sh -P tools/figures/charpipe/fit/build_a.py -- <input id> [...]
Writes fit-test/a/<id>_A.blend (Armature, the LODs tagged fit_role=body, the hidden donor proxy tagged donor), <id>_A.fbx and <id>_A.json (stats)."""
import os, sys, json, math, time, importlib.util
import numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
L = importlib.util.module_from_spec(s := importlib.util.spec_from_file_location('fitlib', os.path.join(HERE, 'lib.py'))); s.loader.exec_module(L)
import bpy
from mathutils import Vector, Matrix

OUT = L.ensure(os.path.join(L.SCR, 'a'))


def build(fid):
    tm = L.Timer(); fit = json.load(open(os.path.join(L.SCR, 'fit', f'{fid}.fit.json'))); row = {r['id']: r for r in L.D['inputs']}[fid]
    bpy.ops.wm.open_mainfile(filepath=os.path.join(L.SCR, 'fit', f'{fid}_src.blend'))
    for o in list(bpy.data.objects):
        if o.name.endswith('_fitproxy'): bpy.data.objects.remove(o, do_unlink=True)
    src = bpy.data.objects[f'{fid}_src']; img = L.base_color_image(src)
    L.addon_utils.enable('bl_ext.user_default.mpfb', default_set=True, persistent=False)
    arm, b, px, donor = L.fitted_human(fit); tm.lap('body')
    lm = L.landmarks(arm, donor['V']); height = lm['crown']
    # LOD0: the source decimated to budget, one matte material with the source's base colour
    L.flat_material(src, img); n0 = L.decimate_to(src, L.D['lods']['tris'][0]); src.name = f'{fid}_A_LOD0'; tm.lap('lod0')
    crumbs, parts = L.drop_crumbs(src, L.D['transfer']['crumbPct'])
    Vt = np.array([list(v.co) for v in src.data.vertices]); Tt = L.tris_of(src)
    W, st, dist, nb = L.transfer(donor, Vt, Tt, height); tm.lap('transfer')
    lab = L.classify(Vt, Tt, dist, nb, lm, row['wears'])
    W, rule_counts = L.loose_rules(W, donor['names'], Vt, lab, lm, arm); tm.lap('rules')
    L.set_weights(src, W, donor['names'], L.D['transfer']['maxInfluences'])
    src.parent = arm; md = src.modifiers.new('Armature', 'ARMATURE'); md.object = arm
    np.save(os.path.join(OUT, f'{fid}_A_labels.npy'), lab.astype(str))
    # the fitted pose becomes the rest, then the standard rest is posed and applied
    bpy.data.objects.remove(b, do_unlink=True)
    for m in list(src.modifiers):
        if m.type == 'ARMATURE': src.modifiers.remove(m)       # the source is already in the fitted pose
    L.apply_rest(arm, [px])                                     # the donor proxy keeps its fitted pose; the rig's rest becomes the fitted pose
    m = src.modifiers.new('Armature', 'ARMATURE'); m.object = arm
    L.pose_blender(arm, L.standard_aims(arm)); L.apply_rest(arm, [src, px]); tm.lap('rest')
    # back to the fitted macros' own MPFB size (the fit's global scale and offset taken out), like G
    pr = fit['params']; sc_, t_ = pr['s'], np.array([pr['tx'], pr['ty'], pr['tz']])
    for o in (src, px): L.set_co(o, (np.array([list(v.co) for v in o.data.vertices]) - t_) / sc_)
    L.bp.ctx(arm, 'EDIT')
    for e in arm.data.edit_bones: e.head = Vector((np.array(e.head) - t_) / sc_); e.tail = Vector((np.array(e.tail) - t_) / sc_)
    L.bp.ctx(arm, 'OBJECT')
    legs = L.rest_leg_report(arm)
    body_co = L.eval_co(px); att = L.add_attach(arm, body_co)
    # LOD1, LOD2 from LOD0 (Blender's decimate carries the weights)
    lods = [src]; tris = [len(L.tris_of(src))]
    for k, t in enumerate(L.D['lods']['tris'][1:], 1):
        c = src.copy(); c.data = src.data.copy(); bpy.context.scene.collection.objects.link(c); c.name = f'{fid}_A_LOD{k}'
        for m in list(c.modifiers):
            if m.type == 'ARMATURE': c.modifiers.remove(m)
        tris.append(L.decimate_to(c, t)); m = c.modifiers.new('Armature', 'ARMATURE'); m.object = arm; lods.append(c)
    for k, o in enumerate(lods): L.tag(o, 'body', k)
    src['fit_labels'] = os.path.join(OUT, f'{fid}_A_labels.npy'); arm['fit_weight'] = fit['macros']['weight']
    L.tag(px, 'donor'); px.hide_render = True; px.name = f'{fid}_A_donor'
    arm.name = 'Armature'; tm.lap('lods')
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, f'{fid}_A.blend'), compress=True)
    L.bp.export_fbx(os.path.join(OUT, f'{fid}_A.fbx'), [arm] + lods, arm); tm.lap('export')
    stats = {'id': fid, 'approach': 'A', 'sex': fit['sex'], 'macros': fit['macros'], 'lodTris': tris, 'srcTris': fit['srcTris'], 'bones': len([b for b in arm.data.bones if b.use_deform]),
             'attach': sorted(att), 'materials': 1, 'texture': list(img.size) if img else None, 'transfer': st, 'crumbsDropped': crumbs, 'sourceParts': parts, 'looseRules': rule_counts,
             'labels': {k: int((lab == k).sum()) for k in set(lab.tolist()) if k}, 'restLegs': legs, 'seconds': tm.t, 'heightM': round(height, 3)}
    json.dump(stats, open(os.path.join(OUT, f'{fid}_A.json'), 'w'), indent=1)
    L.log('DONE A', fid, tris, st, rule_counts, tm.t)


for fid in sys.argv[sys.argv.index('--') + 1:]: build(fid)
