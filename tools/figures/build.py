"""Stand-in people for Unity (brief 020). Blender: blender -b -P tools/figures/build.py -- <figures.json> <out People dir> [id ...]

One FBX per figure: a Mixamo-named skeleton (Unity Humanoid maps it unaided), the LOD meshes as `<id>_LOD0..n` (Unity makes the LOD group
from the names), 1.75 m standing height, feet at the origin. Textures extracted from the source glb (albedo and normal copied byte for byte,
the glTF ORM map repacked into URP's metallic map: R metallic, G occlusion, A smoothness), a URP Lit .mat per texture set, and .meta files
with deterministic GUIDs. Clips are the Mixamo library files, copied (tools/figures/clips.py), not baked here. Writes manifest.json."""
import sys, os, json, hashlib, shutil
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from figlib import *
import unitymeta as um
import numpy as np

STORE = os.path.expanduser(os.environ.get('SUBORNED_STORE', '~/Projects/the-suborned-store')) + '/source-art'
sha = lambda b: hashlib.sha256(b).hexdigest()
TARGET_H = 1.75


def store(p):
    return p if os.path.isabs(p) else os.path.normpath(os.path.join(STORE, p))


def provenance(keys):
    """The PROVENANCE.json entries of the generation steps a figure derives from, verbatim: Citizens3D/PROVENANCE.json (3D) and
    concepts/Citizens/PROVENANCE.json (model sheets); a key with no entry is kept as null."""
    rows = []
    for p in (os.path.join(STORE, 'Citizens3D/PROVENANCE.json'), os.path.join(STORE, '../concepts/Citizens/PROVENANCE.json')):
        if os.path.exists(p): rows += json.load(open(p))
    out = []
    for k in keys:
        hit = [r for r in rows if k in (r['file'] if isinstance(r['file'], list) else [r['file']])]
        out.append({'file': k, 'entry': hit[0] if hit else None})
    return out


def upstream_image(sock):
    seen, stack = set(), [sock]
    while stack:
        s = stack.pop()
        for l in s.links:
            n = l.from_node
            if n.type == 'TEX_IMAGE' and n.image: return n.image
            if n.name in seen: continue
            seen.add(n.name)
            stack += [i for i in n.inputs if i.is_linked]
    return None


def material_maps(mat):
    """The images a glTF material uses, by role: albedo, normal, orm, emissive; plus emission strength and alpha mode."""
    nt = mat.node_tree
    pb = next(n for n in nt.nodes if n.type == 'BSDF_PRINCIPLED')
    out = {'albedo': upstream_image(pb.inputs['Base Color']), 'normal': upstream_image(pb.inputs['Normal']),
           'orm': upstream_image(pb.inputs['Metallic']) or upstream_image(pb.inputs['Roughness']),
           'emissive': upstream_image(pb.inputs['Emission Color'])}
    out['emission_strength'] = float(pb.inputs['Emission Strength'].default_value) if 'Emission Strength' in pb.inputs else 1.0
    out['blend'] = getattr(mat, 'blend_method', 'OPAQUE') in ('BLEND', 'HASHED') or pb.inputs['Alpha'].is_linked
    return out


def image_bytes(img):
    if img.packed_file: return bytes(img.packed_file.data)
    return open(bpy.path.abspath(img.filepath), 'rb').read()


def ext_of(img):
    """The container the bytes really are (a glTF image's file_format is not reliable)."""
    b = image_bytes(img)[:4]
    return 'png' if b[:4] == b'\x89PNG' else ('jpg' if b[:2] == b'\xff\xd8' else 'png')


def save_scaled(img, path, size, fmt):
    c = img.copy(); c.scale(size, size)
    c.file_format = fmt; c.filepath_raw = path
    c.save()
    bpy.data.images.remove(c)


def opaque(img):
    px = np.empty(img.size[0] * img.size[1] * 4, dtype=np.float32); img.pixels.foreach_get(px)
    return float(px[3::4].min()) > 0.99


def write_tex(rel_dir, base, img, size, kind, out_root, tint=None, lift=0.0, repair=None):
    """Copy or rescale `img` to People/<rel_dir>/<base>.<ext> (+.meta); big opaque PNG albedo and normal maps become JPEG q90. Returns (path relative to People, size)."""
    w = img.size[0]; sz = min(size or w, w)
    srcext = ext_of(img)
    heavy = sz >= 1024 and kind in ('albedo', 'normal') and (srcext == 'png' or len(image_bytes(img)) > 1_200_000) and opaque(img)   # re-encode as JPEG q90
    raw = sz == w and tint is None and not heavy and not lift and not repair
    ext = srcext if raw else ('jpg' if (kind in ('albedo', 'normal') and (heavy or srcext == 'jpg' or opaque(img))) else 'png')
    rel = f'{rel_dir}/{base}.{ext}'; dst = os.path.join(out_root, rel)
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    if raw: open(dst, 'wb').write(image_bytes(img))
    else:
        c = img.copy()
        if sz != w: c.scale(sz, sz)
        for r in (repair or []): repair_albedo(c, r)
        for t in ([] if tint is None else tint if isinstance(tint, list) else [tint]): apply_tint(c, t)
        if lift: lift_blacks(c, lift)
        c.file_format = 'JPEG' if ext == 'jpg' else 'PNG'; c.filepath_raw = dst
        c.save(quality=90) if ext == 'jpg' else c.save()
        bpy.data.images.remove(c)
    open(dst + '.meta', 'w').write(um.tex_meta(rel, kind, sz))
    return rel, sz


def _hsv(px):
    r, g, b = px[:, 0], px[:, 1], px[:, 2]
    mx, mn = np.maximum(np.maximum(r, g), b), np.minimum(np.minimum(r, g), b); d = mx - mn
    hh = np.zeros_like(mx); m = d > 1e-6
    rm = m & (mx == r); gm = m & (mx == g) & ~rm; bm = m & ~rm & ~gm
    hh[rm] = ((g - b)[rm] / d[rm]) % 6; hh[gm] = (b - r)[gm] / d[gm] + 2; hh[bm] = (r - g)[bm] / d[bm] + 4
    return hh * 60.0, np.where(mx > 1e-6, d / np.maximum(mx, 1e-6), 0), mx


def _box(a, r):
    """Mean of a over a (2r+1)^2 window (edges clamp), by cumulative sums."""
    p = np.pad(a, [(r + 1, r), (r + 1, r)] + [(0, 0)] * (a.ndim - 2), mode='edge'); c = p.cumsum(0).cumsum(1)
    n = 2 * r + 1
    return (c[n:, n:] - c[:-n, n:] - c[n:, :-n] + c[:-n, :-n]) / (n * n)


def repair_albedo(img, op):
    """Fix a generator's baked-in defect in an sRGB albedo (Glint, briefs/020/figures-render.md items 3 and 4; sizes are for a 1024 map and scale with it).
    spots: dark specks inside a colour field (hue window `hue`, `minSat`, `minVal`): a pixel below `dark` value whose neighbourhood (radius `r`) is at least
    `field` that colour is painted over with the field's local mean (normalised convolution, mask grown by `grow`).
    recolour: pixels inside a hue / saturation / value window are set to `to` (sRGB) keeping their light and dark by `keep` (0 = flat, 1 = as it was)."""
    w, h = img.size; k = w / 1024.0
    px = np.empty(w * h * 4, dtype=np.float32); img.pixels.foreach_get(px); px = px.reshape(h, w, 4)
    hd, sa, va = _hsv(px.reshape(-1, 4)); hd, sa, va = hd.reshape(h, w), sa.reshape(h, w), va.reshape(h, w)
    if op['op'] == 'spots':
        lo, hi = op['hue']; field = (hd >= lo) & (hd <= hi) & (sa >= op.get('minSat', 0.3)) & (va >= op.get('minVal', 0.4))
        dark = va < op.get('dark', 0.3)
        r = max(2, int(op.get('r', 14) * k)); frac = _box(field.astype(np.float32), r)
        spot = dark & (frac > op.get('field', 0.35))
        g = max(1, int(op.get('grow', 2) * k)); spot = _box(spot.astype(np.float32), g) > 0.0
        wgt = field.astype(np.float32); rr = max(2, int(op.get('fill', 12) * k))
        num = _box(px[..., :3] * wgt[..., None], rr); den = _box(wgt, rr)[..., None]
        fill = num / np.maximum(den, 1e-4); ok = spot & (den[..., 0] > 0.02)
        px[ok, :3] = fill[ok]
        print('REPAIR spots', int(ok.sum()), 'px')
    elif op['op'] == 'flatten':
        # matte a baked sheen: neutral light pixels (saturation within `sat`, value within `val`) are pulled toward the level `to` by `k` (0 = flat, 1 = as it was);
        # the generator painted satin highlights into a pale jacket and a light cloth reads as foil in the sun. Dark seams (below `val`) are left alone.
        s0, s1 = op['sat']; v0, v1 = op['val']
        sel = (sa >= s0) & (sa <= s1) & (va >= v0) & (va <= v1)
        sel = _box(sel.astype(np.float32), 1) > 0.5
        lvl = op['to']; k_ = op.get('k', 0.4)
        v_ = va[sel]; newv = lvl + (v_ - lvl) * k_
        px[sel, :3] = np.clip(px[sel, :3] * (newv / np.maximum(v_, 1e-3))[:, None], 0, 1)
        print('REPAIR flatten', int(sel.sum()), 'px')
    elif op['op'] == 'recolour':
        h0, h1 = op['hue']; s0, s1 = op['sat']; v0, v1 = op['val']
        sel = (hd >= h0) & (hd <= h1) & (sa >= s0) & (sa <= s1) & (va >= v0) & (va <= v1)
        sel = _box(sel.astype(np.float32), 1) > 0.5
        if op.get('boxes'):   # only inside these atlas rectangles ([x0, y0, x1, y1] as shares of the map, origin top left): a hue window alone also takes pale skin elsewhere
            inside = np.zeros((h, w), dtype=bool)
            for x0, y0, x1, y1 in op['boxes']:
                inside[int((1 - y1) * h):int((1 - y0) * h), int(x0 * w):int(x1 * w)] = True   # image rows run bottom to top
            sel &= inside
        to = np.array(op['to'], dtype=np.float32); keep = op.get('keep', 0.3)
        lum = va[..., None] / max(float(va[sel].mean()), 1e-3) if sel.any() else 1.0
        newc = np.clip(to * (1 - keep + keep * lum), 0, 1) if sel.any() else px[..., :3]
        px[sel, :3] = newc[sel]
        print('REPAIR recolour', int(sel.sum()), 'px')
    img.pixels.foreach_set(px.reshape(-1))


def lift_blacks(img, floor):
    """Lift near-black albedo toward `floor` (0..1, sRGB) so a black outfit keeps its shape in shade: out = in + max(0, floor - in) * 0.85."""
    w, h = img.size; px = np.empty(w * h * 4, dtype=np.float32); img.pixels.foreach_get(px); px = px.reshape(-1, 4)
    px[:, :3] = px[:, :3] + np.maximum(0.0, floor - px[:, :3]) * 0.85
    img.pixels.foreach_set(px.reshape(-1))


def apply_tint(img, t):
    """Recolour part of an sRGB albedo: pixels whose hue is within `width` degrees of `from` (and saturated and bright enough) get their hue
    moved by `shift` degrees, saturation times `sat`, value times `val`. Skin and hair sit outside the window, so a garment changes alone."""
    w, h = img.size; px = np.empty(w * h * 4, dtype=np.float32); img.pixels.foreach_get(px); px = px.reshape(-1, 4)
    r, g, b = px[:, 0].copy(), px[:, 1].copy(), px[:, 2].copy()
    mx, mn = np.maximum(np.maximum(r, g), b), np.minimum(np.minimum(r, g), b); d = mx - mn
    hh = np.zeros_like(mx); m = d > 1e-6
    rm = m & (mx == r); gm = m & (mx == g) & ~rm; bm = m & ~rm & ~gm
    hh[rm] = ((g - b)[rm] / d[rm]) % 6; hh[gm] = (b - r)[gm] / d[gm] + 2; hh[bm] = (r - g)[bm] / d[bm] + 4
    hdeg = hh * 60.0; s_ = np.where(mx > 1e-6, d / np.maximum(mx, 1e-6), 0); v_ = mx
    dh = np.abs((hdeg - t.get('from', 0) + 180) % 360 - 180)
    sel = (dh <= t.get('width', 30)) & (s_ >= t.get('minSat', 0.2)) & (s_ <= t.get('maxSat', 1.0)) & (v_ >= t.get('minVal', 0.0)) & (v_ <= t.get('maxVal', 1.0))
    hdeg = np.where(sel, (hdeg + t.get('shift', 0)) % 360, hdeg)
    s_ = np.where(sel, np.clip(s_ * t.get('sat', 1.0), 0, 1), s_); v_ = np.where(sel, np.clip(v_ * t.get('val', 1.0), 0, 1), v_)
    H = hdeg / 60.0; i = np.floor(H).astype(int) % 6; f = H - np.floor(H)
    p, q, tt = v_ * (1 - s_), v_ * (1 - f * s_), v_ * (1 - (1 - f) * s_)
    cs = [(v_, tt, p), (q, v_, p), (p, v_, tt), (p, q, v_), (tt, p, v_), (v_, p, q)]
    for k in range(6):
        sk = i == k
        px[sk, 0], px[sk, 1], px[sk, 2] = cs[k][0][sk], cs[k][1][sk], cs[k][2][sk]
    img.pixels.foreach_set(px.reshape(-1))


def write_metal(rel_dir, base, orm, size, out_root, trim_only=False):
    """glTF ORM (R occlusion, G roughness, B metallic) -> URP metallic map: R metallic, G occlusion, B 0, A smoothness. The generators' maps read as wet latex
    and chrome in the game's light (cloth at 0.4 metallic and smoothness 0.5 to 0.7: Glint, briefs/020/figures-render.md), so they are pulled to the world's matte:
    t = smoothstep(0.3, 0.7, metal); metallic = 0.6 t; smoothness = lerp(min(0.45 s, 0.28), min(s, 0.55), t). Cloth, skin and hair end at 0 / 0.15..0.28, trim
    at 0.6 / 0.45..0.55. The map is at most 1024 px (the 2048 materials use the same 1024 map)."""
    w = orm.size[0]; sz = min(size or w, w, 1024)
    px = np.empty(w * w * 4, dtype=np.float32); orm.pixels.foreach_get(px); px = px.reshape(-1, 4)
    occ, rough, metal = px[:, 0], px[:, 1], px[:, 2]
    t = np.clip((metal - 0.3) / 0.4, 0, 1); t = t * t * (3 - 2 * t)
    s = 1 - rough
    smooth = np.minimum(0.45 * s, 0.28) * (1 - t) + np.minimum(s, 0.55) * t
    if trim_only:
        # Glint recheck (guard, security, cafe_w5): the generator marked whole white panels as metal (R mean 0.24, p95 0.60), so the map made foil.
        # Only thin metal survives: what an opening (erode then dilate by 7 px) removes, i.e. strips narrower than 15 px at 1024 (shoulder edges,
        # belt, eyelets, boot caps). Panels end at R 0, A <= 0.16 (cloth); trim at R 0.6, A 0.45.
        n = int(round(len(t) ** 0.5)); m = (t > 0.5).reshape(n, n)
        def sweep(a, r, f):
            o = a.copy()
            for k in range(1, r + 1):
                for ax in (0, 1):
                    for sh in (k, -k): o = f(o, np.roll(a, sh, ax))
            return o
        opened = sweep(sweep(m, 7, np.logical_and), 7, np.logical_or)
        trim = (m & ~opened).reshape(-1)
        trim = sweep(trim.reshape(n, n), 1, np.logical_or).reshape(-1) & (t > 0.2)   # one pixel of margin, never on cloth
        t = trim.astype(np.float32)
        smooth = np.where(trim, 0.45, np.minimum(0.3 * s, 0.16))   # Glint's cloth target: 0.15 (0.12 to 0.20)
    out = np.zeros_like(px); out[:, 0] = 0.6 * t; out[:, 1] = occ; out[:, 2] = 0; out[:, 3] = smooth
    im = bpy.data.images.new(base, w, w, alpha=True, float_buffer=False, is_data=True)
    im.pixels.foreach_set(out.reshape(-1))
    if sz != w: im.scale(sz, sz)
    rel = f'{rel_dir}/{base}.png'; dst = os.path.join(out_root, rel)
    im.file_format = 'PNG'; im.filepath_raw = dst; im.alpha_mode = 'STRAIGHT'
    sc = bpy.context.scene; sc.render.image_settings.color_mode = 'RGBA'
    im.save()
    bpy.data.images.remove(im)
    open(dst + '.meta', 'w').write(um.tex_meta(rel, 'data', sz))
    return rel, sz


# ---------------------------------------------------------------- one figure

def import_rig(src):
    new = load(store(src))
    arm, meshes = rigs(new)
    for o in list(new):
        if o.type not in ('ARMATURE', 'MESH'):
            bpy.data.objects.remove(o, do_unlink=True)
    meshes = [m for m in meshes if m.name in bpy.data.objects]
    weld(meshes)
    return arm, meshes


def join(meshes, name):
    if len(meshes) > 1:
        bpy.ops.object.select_all(action='DESELECT')
        for m in meshes: m.select_set(True)
        bpy.context.view_layer.objects.active = meshes[0]
        bpy.ops.object.join()
    m = bpy.context.view_layer.objects.active if len(meshes) > 1 else meshes[0]
    m.name = name; m.data.name = name
    return m


def decimate(src, name, target):
    """A new mesh object `name` from `src` at about `target` triangles; weights follow the surviving vertices, UV borders are kept."""
    dup = src.copy(); dup.data = src.data.copy(); bpy.context.scene.collection.objects.link(dup)
    arm_mod = [(md.object) for md in dup.modifiers if md.type == 'ARMATURE']
    for md in list(dup.modifiers): dup.modifiers.remove(md)
    d = dup.modifiers.new('dec', 'DECIMATE'); d.decimate_type = 'COLLAPSE'; d.ratio = min(1.0, target / max(tris(src), 1)); d.delimit = {'UV'}; d.use_collapse_triangulate = True
    bpy.ops.object.select_all(action='DESELECT'); dup.select_set(True); bpy.context.view_layer.objects.active = dup
    bpy.ops.object.modifier_apply(modifier='dec')
    for o in arm_mod:
        a = dup.modifiers.new('Armature', 'ARMATURE'); a.object = o
    dup.name = name; dup.data.name = name
    return dup


def transfer_weights(target, donor, names):
    """Skin `target` (a static mesh in metres, same place as the donor) with the donor's weights, nearest surface, then smooth a little."""
    for n in names:
        if n not in target.vertex_groups: target.vertex_groups.new(name=n)
    m = target.modifiers.new('xfer', 'DATA_TRANSFER'); m.object = donor
    m.use_vert_data = True; m.data_types_verts = {'VGROUP_WEIGHTS'}; m.vert_mapping = 'POLYINTERP_NEAREST'
    m.layers_vgroup_select_src = 'ALL'; m.layers_vgroup_select_dst = 'NAME'
    bpy.ops.object.select_all(action='DESELECT'); target.select_set(True); bpy.context.view_layer.objects.active = target
    bpy.ops.object.modifier_apply(modifier='xfer')
    try:
        bpy.ops.object.vertex_group_smooth(group_select_mode='ALL', factor=0.5, repeat=2)
    except Exception as e:
        print('smooth skipped', e)
    bpy.ops.object.vertex_group_normalize_all(group_select_mode='ALL', lock_active=False)


def label_slots(meshes):
    """[(mesh, slot, label, maps)] for a LOD's source meshes: 'visor' for an emissive visor sheet, then 'body', then 'part<n>'."""
    out, n = [], 0
    for m in meshes:
        for si, s in enumerate(m.material_slots):
            mp = material_maps(s.material)
            names = ' '.join((mp[k].name.lower() if mp[k] else '') for k in ('albedo', 'emissive'))
            if 'visor' in names: label = 'visor'
            else: label = 'body' if n == 0 else f'part{n}'; n += 1
            out.append((m, si, label, mp))
    return out


def build_figure(spec, out_root, manifest_rows):
    fid = spec['id']; clean()
    fig_dir = f'Figures/{fid}'; tdir = f'{fig_dir}/Textures'
    lods = spec['lods']
    tint = spec.get('tint')
    mat_rows, ext_mats, label_maps = {}, {}, {}

    def material(label, tsize):
        key = f'{fid}_{label}_{tsize}'
        if key not in mat_rows:
            mp = label_maps[label]; tx = {}
            far = tsize <= 512   # the play-zoom LOD: albedo only (a 53 px figure sparkles with a normal map) and a matte surface
            if mp['albedo']: tx['albedo'] = write_tex(tdir, key + '_albedo', mp['albedo'], tsize, 'albedo', out_root, tint=tint if label != 'visor' else None, lift=spec.get('lift', 0.0) if label != 'visor' else 0.0, repair=spec.get('repair') if label == 'body' else None)
            if mp['normal'] and not far and not spec.get('noNormal'): tx['normal'] = write_tex(tdir, key + '_normal', mp['normal'], tsize, 'normal', out_root)
            if mp['orm'] and not spec.get('noMetal') and not far and label != 'visor': tx['metal'] = write_metal(tdir, key + '_metal', mp['orm'], min(tsize, 1024), out_root, spec.get('trimOnly', False))
            if mp['emissive']: tx['emissive'] = write_tex(tdir, key + '_emissive', mp['emissive'], tsize, 'emissive', out_root)
            mrel = f'{fig_dir}/Materials/{key}.mat'
            os.makedirs(os.path.join(out_root, fig_dir, 'Materials'), exist_ok=True)
            open(os.path.join(out_root, mrel), 'w').write(um.mat_text(key, base=tx.get('albedo', (None,))[0], normal=tx.get('normal', (None,))[0],
                                                                 metal=tx.get('metal', (None,))[0], emissive=tx.get('emissive', (None,))[0], emission=mp['emission_strength'], smoothness=0.75 if label == 'visor' else (spec['matSmoothness'] if 'matSmoothness' in spec and label == 'body' else (0.15 if (far or spec.get('noMetal')) else 1.0))))
            open(os.path.join(out_root, mrel + '.meta'), 'w').write(um.mat_meta(mrel))
            mat_rows[key] = {'name': key, 'file': mrel, 'size': tsize, 'label': label, 'textures': {k: v[0] for k, v in tx.items()},
                             'emissionStrength': mp['emission_strength'] if mp['emissive'] else 0, 'alphaInSource': bool(mp['blend'])}
            ext_mats[key] = mrel
        return bpy.data.materials.get(key) or bpy.data.materials.new(key)

    kind = spec.get('kind', 'rigged'); H = spec.get('height', TARGET_H)
    by_lod = {}
    if kind == 'rigged':
        # LOD0's rig sets the units; every other LOD is the same rig, so it takes the same transform
        arm0, m0 = import_rig(lods[0]['src']); arm0.name = 'Armature'; rest_pose(arm0)
        mn, mx = world_bounds(m0)
        scale = H / (mx.z - mn.z)
        shift = Vector((0, 0, -mn.z * scale))
        srcs = [(0, arm0, m0)]
        for i in range(1, len(lods)):
            if 'src' in lods[i]:
                a, m = import_rig(lods[i]['src']); rest_pose(a); srcs.append((i, a, m))
        # materials first (keyed by label and size), while the source nodes are still there
        for i, a, m in srcs:
            ls = label_slots(m)
            for _, _, label, mp in ls: label_maps.setdefault(label, mp)
            for mm, si, label, mp in ls: mm.data.materials[si] = material(label, lods[i]['tex'])
        for i, a, m in srcs:
            mixamo_names(a, m)
            if a is arm0: bake_to_meters(a, m, scale, shift)
            else:
                for mm in m:
                    mm.data.transform(Matrix.Translation(shift) @ Matrix.Scale(scale, 4) @ mm.matrix_world)
                    mm.parent = None; mm.matrix_world = Matrix.Identity(4)
                    mm.parent = arm0; mm.matrix_parent_inverse = Matrix.Identity(4)
                    for md in mm.modifiers:
                        if md.type == 'ARMATURE': md.object = arm0
                bpy.data.objects.remove(a, do_unlink=True)
            by_lod[i] = join([o for o in m if o.name in bpy.data.objects], f'{fid}_LOD{i}')
            if lods[i].get('decimateTo'):
                full = by_lod[i]; full.name = '__full'
                by_lod[i] = decimate(full, f'{fid}_LOD{i}', lods[i]['decimateTo']); bpy.data.objects.remove(full, do_unlink=True)
    else:
        # a static mesh skinned from a donor rig (Meshy-style, already in Mixamo names after mixamo_names): fit the donor to the figure, copy its weights
        arm0, dm = import_rig(spec['donor']); arm0.name = 'Armature'; rest_pose(arm0)
        dmn, dmx = world_bounds(dm)
        dscale = H / (dmx.z - dmn.z)
        mixamo_names(arm0, dm)
        bake_to_meters(arm0, dm, dscale, Vector((0, 0, -dmn.z * dscale)))
        donor = join(dm, 'donor')
        names = [b.name for b in arm0.data.bones]
        dmn, dmx = world_bounds([donor]); dc = (dmn + dmx) / 2
        T = None; srcs = []
        for i, l in enumerate(lods):
            if 'src' not in l: continue
            new = load(store(l['src'])); meshes = [o for o in new if o.type == 'MESH']
            for o in new:
                if o.type != 'MESH': bpy.data.objects.remove(o, do_unlink=True)
            weld(meshes)
            if spec.get('rotZ'):
                Rz = Matrix.Rotation(math.radians(spec['rotZ']), 4, 'Z')
                for mm in meshes:
                    mm.data.transform(Rz @ mm.matrix_world); mm.parent = None; mm.matrix_world = Matrix.Identity(4)
            if T is None:
                tmn, tmx = world_bounds(meshes); ts = H / (tmx.z - tmn.z); tc = (tmn + tmx) / 2
                T = Matrix.Translation(Vector((dc.x - tc.x * ts, dc.y - tc.y * ts, -tmn.z * ts))) @ Matrix.Scale(ts, 4)
            ls = label_slots(meshes)
            for _, _, label, mp in ls: label_maps.setdefault(label, mp)
            for mm, si, label, mp in ls: mm.data.materials[si] = material(label, l['tex'])
            for mm in meshes:
                mm.data.transform(T @ mm.matrix_world); mm.parent = None; mm.matrix_world = Matrix.Identity(4)
            o = join(meshes, f'{fid}_LOD{i}')
            if l.get('decimateTo'):
                full = o; full.name = '__full'; o = decimate(full, f'{fid}_LOD{i}', l['decimateTo']); bpy.data.objects.remove(full, do_unlink=True)
            transfer_weights(o, donor, names)
            print('CLEAN', fid, i, clean_weights(o, arm0), 'WEBS', cut_webs(o))
            if spec.get('smoothSkin'): print('SMOOTH', fid, i, smooth_skin(o, *spec['smoothSkin']))
            if spec.get('rigidCloth'): print('RIGID', fid, i, rigid_cloth(o, label_maps['body']['albedo'], spec['rigidCloth']))
            md = o.modifiers.new('Armature', 'ARMATURE'); md.object = arm0
            o.parent = arm0; o.matrix_parent_inverse = Matrix.Identity(4)
            by_lod[i] = o
        bpy.data.objects.remove(donor, do_unlink=True)
    for i, l in enumerate(lods):
        if 'src' not in l:
            d = decimate(by_lod[l['from']], f'{fid}_LOD{i}', l['tris']); by_lod[i] = d
            for si, slot in enumerate(d.material_slots):
                label = mat_rows[slot.material.name]['label']; d.data.materials[si] = material(label, l['tex'])
    objs = [by_lod[i] for i in range(len(lods))]
    print('TORSO', fid, torso_guard(objs, arm0), 'WEBS', cut_side_webs(objs, arm0) if spec.get('torsoWebs') else 'kept')
    limit_weights(objs, 4)
    print('FEET', fid, straighten_feet(arm0, objs, objs[0]))
    # 2. export
    bpy.ops.object.select_all(action='DESELECT')
    arm0.select_set(True)
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = arm0
    fbx_rel = f'{fig_dir}/{fid}.fbx'; fbx = os.path.join(out_root, fbx_rel)
    os.makedirs(os.path.dirname(fbx), exist_ok=True)
    bpy.ops.export_scene.fbx(filepath=fbx, use_selection=True, object_types={'ARMATURE', 'MESH'}, use_mesh_modifiers=False,
                             add_leaf_bones=False, bake_anim=False, axis_forward='-Z', axis_up='Y', apply_unit_scale=True,
                             apply_scale_options='FBX_SCALE_ALL', bake_space_transform=False, primary_bone_axis='Y', secondary_bone_axis='X',
                             armature_nodetype='NULL', mesh_smooth_type='FACE', use_tspace=True, path_mode='STRIP', embed_textures=False)
    open(fbx + '.meta', 'w').write(um.model_meta(fbx_rel, ext_mats))
    mn2, mx2 = world_bounds([objs[0]])
    row = {'id': fid, 'role': spec['role'], 'label': spec.get('label', fid), 'standIn': True, 'fbx': fbx_rel, 'sha256': sha(open(fbx, 'rb').read()),
           'rig': {'type': 'humanoid', 'bones': len(arm0.data.bones), 'names': 'mixamo'}, 'heightM': round(mx2.z - mn2.z, 3),
           'lods': [{'name': o.name, 'tris': tris(o), 'tex': lods[i]['tex'], 'source': lods[i].get('src') or f"decimated from LOD{lods[i]['from']}"} for i, o in enumerate(objs)],
           'materials': list(mat_rows.values()), 'source': spec.get('source'), 'provenance': provenance(spec.get('provenance', [])), 'notes': spec.get('notes', '')}
    manifest_rows.append(row)
    return row


def main():
    a = args(); spec_path, out_root = a[0], a[1]; only = a[2:]
    spec = json.load(open(spec_path)); rows = []
    for f in spec['figures']:
        if only and f['id'] not in only: continue
        print('BUILD', f['id'])
        r = build_figure(f, out_root, rows)
        print('DONE', r['id'], r['heightM'], [(l['name'], l['tris']) for l in r['lods']])
    json.dump(rows, open(os.path.join(out_root, f'.rows.{"_".join(only) or "all"}.json'), 'w'), indent=1)


main()
