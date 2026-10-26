"""Person specs -> LOD'd FBX figures on one skeleton, one shared material (brief 069, proof, round 2). Blender side; runs at build time only.
  BLENDER_USER_RESOURCES=<scratch>/blender-user BLENDER_USER_EXTENSIONS=<scratch>/blender-user/extensions MPFBDATA=<mpfb data dir> \\
  blender -b --factory-startup -P tools/figures/charpipe/build.py -- <specs.json> <out CharPipe dir> [id ...] [shared] [--master]

Per person:
 1. MPFB makes the body from the person's macros and fits its Mixamo-named rig (mixamo_unity) and two CC0 MakeHuman proxies: male1591 / female1605 (LOD0, separate
    fingers) and proxy741 (LOD1). The rest pose becomes a T-pose (MPFB's own t-pose for this rig, then arms, hands and fingers straightened along +-X, palms down),
    the bind pose Mixamo's clips were made on, so Unity's avatar needs no T-pose correction (round 1's A-pose made Unity turn the arm rest 46 degrees: aim_pistol
    crossed the wrists and opened the fingers).
 2. The rig is cut to 52 Humanoid bones (22 body + 3 x 5 x 2 fingers); Root, breasts, buttocks, jaw, eyes and lids fold into their parents' weights.
 3. The wardrobe (wardrobe.json) runs on each LOD: every border a paint rule draws is first cut into the mesh as a plane (a z plane on the torso and head, a plane
    across the bone at t on a limb, a vertical plane for a panel's width), so colour edges run straight instead of zigzagging across faces. Paint parts then
    take faces and push them out; the extra geometry (hem tubes, collars, hair curtains, caps, the cape collar, aprons, bags) is built on the pushed-out surface,
    so a cap clears hair; its weights come by Data Transfer from the body (hems blend toward the hips and thighs, more at the front than the back).
 4. LOD1 is held to <= 1,500 triangles: if the dressed proxy741 is over, its body is collapsed a step (before dressing) and dressed again.
 5. Each face's colour role picks the person's colour; UVs point at its cell of the shared 256 px palette, so every figure uses the one material cp_palette.
 6. Two FBX per person: <id>_full.fbx (the 52-bone skeleton, for heroes and close-ups) and <id>.fbx (the crowd export: the same mesh with the fingers posed relaxed
    and baked, their weights folded into the hand, 22 bones: Redline measured the finger bones as round 1's +8 % per person, all on the Animator side).
 7. A row per person (rows/<id>.json) with both FBXs, triangles, and its own clip speeds (stride.json: speed = k(clip) x leg height), so the game moves each
    person at their own stride and planted feet don't slide.
The FBX header time and object ids are pinned (as tools/decokit/export_kit.py does), so the same specs give the same bytes."""
import bpy, bmesh, addon_utils, json, os, sys, math, hashlib, datetime, importlib.util
from mathutils import Vector, Matrix

HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.dirname(os.path.dirname(os.path.dirname(HERE)))
_sp = importlib.util.spec_from_file_location('unitymeta', os.path.join(ROOT, 'tools/figures/unitymeta.py')); um = importlib.util.module_from_spec(_sp); _sp.loader.exec_module(um)   # read only
MPFB = os.environ['MPFBDATA']
MPFB_CODE = os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(MPFB)))), 'user_default', 'mpfb')
PROXY0 = {'m': 'male1591', 'f': 'female1605'}; PROXY1 = 'proxy741'
LOD1_MAX_TRIS = 1500
PREFIX = 'mixamorig:'
CORE = ['Hips', 'Spine', 'Spine1', 'Spine2', 'Neck', 'Head'] + [s + b for s in ('Left', 'Right') for b in ('Shoulder', 'Arm', 'ForeArm', 'Hand', 'UpLeg', 'Leg', 'Foot', 'ToeBase')]
FINGERS = [s + 'Hand' + f + str(i) for s in ('Left', 'Right') for f in ('Thumb', 'Index', 'Middle', 'Ring', 'Pinky') for i in (1, 2, 3)]
KEEP = CORE + FINGERS                       # 52
SEG = {'Hips': 'torso', 'Spine': 'torso', 'Spine1': 'torso', 'Spine2': 'torso', 'Neck': 'neck', 'Head': 'head'}
for s in ('Left', 'Right'):
    SEG.update({s + 'Shoulder': 'torso', s + 'Arm': 'arm', s + 'ForeArm': 'forearm', s + 'Hand': 'hand', s + 'UpLeg': 'thigh', s + 'Leg': 'shin', s + 'Foot': 'foot', s + 'ToeBase': 'foot'})
for f in FINGERS: SEG[f] = 'hand'
LIMB_BONE = {'arm': 'Arm', 'forearm': 'ForeArm', 'thigh': 'UpLeg', 'shin': 'Leg', 'hand': 'Hand', 'foot': 'Foot'}
ROLES = ['S', 'E', 'HR', 'BT', 'M', 'B', 'T', 'TL', 'H', 'L', 'LT', 'G', 'D', 'AP', 'BG']
CELLS = 16


def args():
    a = sys.argv[sys.argv.index('--') + 1:]
    return a[0], a[1], [x for x in a[2:] if not x.startswith('--')], '--master' in a


# ------------------------------------------------------------------ deterministic FBX (as tools/decokit/export_kit.py)
def pin_fbx():
    from io_scene_fbx import export_fbx_bin as eb, fbx_utils
    fbx_utils._key_to_uuid = lambda uuids, key: int.from_bytes(hashlib.sha256(repr(key).encode()).digest()[:8], 'big') >> 1 or 1
    orig = getattr(eb, '_cp_orig_header', None) or eb.fbx_header_elements; eb._cp_orig_header = orig
    eb.fbx_header_elements = lambda root, scene_data, time=None: orig(root, scene_data, datetime.datetime(2026, 1, 1))


def export_fbx(path, objs, active):
    pin_fbx()
    bpy.ops.object.select_all(action='DESELECT')
    for x in objs: x.select_set(True)
    bpy.context.view_layer.objects.active = active
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={'ARMATURE', 'MESH'}, use_mesh_modifiers=False,
                             add_leaf_bones=False, bake_anim=False, axis_forward='-Z', axis_up='Y', apply_unit_scale=True,
                             apply_scale_options='FBX_SCALE_ALL', bake_space_transform=False, primary_bone_axis='Y', secondary_bone_axis='X',
                             armature_nodetype='NULL', mesh_smooth_type='FACE', use_tspace=True, path_mode='STRIP', embed_textures=False)


# ------------------------------------------------------------------ palette
def palette():
    pal = json.load(open(os.path.join(HERE, 'palettes.json')))
    names = list(pal['colours'].keys())
    assert len(names) <= CELLS * CELLS
    return pal, {n: i for i, n in enumerate(names)}


def cell_uv(i):
    c, r = i % CELLS, i // CELLS
    return ((c + 0.5) / CELLS, 1.0 - (r + 0.5) / CELLS)


def write_png(path, w, h, rows):
    """Plain RGBA PNG, no time chunk: the same pixels give the same bytes."""
    import zlib, struct
    raw = b''.join(b'\x00' + bytes(r) for r in rows)
    def chunk(t, d): return struct.pack('>I', len(d)) + t + d + struct.pack('>I', zlib.crc32(t + d) & 0xffffffff)
    data = b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', w, h, 8, 6, 0, 0, 0)) + chunk(b'IDAT', zlib.compress(raw, 9)) + chunk(b'IEND', b'')
    os.makedirs(os.path.dirname(path), exist_ok=True); open(path, 'wb').write(data)


def write_shared(out):
    pal, index = palette(); px = 256 // CELLS
    alb = [[0] * (256 * 4) for _ in range(256)]; met = [[0] * (256 * 4) for _ in range(256)]
    for n, i in index.items():
        c = pal['colours'][n]; h = c['hex'].lstrip('#'); rgb = [int(h[k:k + 2], 16) for k in (0, 2, 4)]
        m = int(round(c.get('metal', 0.0) * 255)); sm = int(round(c.get('smooth', 0.15) * 255))
        cx, cy = i % CELLS, i // CELLS
        for y in range(cy * px, cy * px + px):
            for x in range(cx * px, cx * px + px):
                alb[y][x * 4:x * 4 + 4] = rgb + [255]; met[y][x * 4:x * 4 + 4] = [m, 255, 0, sm]
    write_png(os.path.join(out, 'Shared/Textures/cp_palette_albedo.png'), 256, 256, alb)
    write_png(os.path.join(out, 'Shared/Textures/cp_palette_metal.png'), 256, 256, met)
    for name, kind in (('cp_palette_albedo', 'albedo'), ('cp_palette_metal', 'data')):
        rel = f'CharPipe/Shared/Textures/{name}.png'
        t = um.tex_meta(rel, kind, 256).replace('enableMipMap: 1', 'enableMipMap: 0').replace('filterMode: 2', 'filterMode: 0').replace('textureCompression: 1', 'textureCompression: 0').replace('aniso: 4', 'aniso: 1')
        open(os.path.join(out, f'Shared/Textures/{name}.png.meta'), 'w').write(t)
    os.makedirs(os.path.join(out, 'Shared/Materials'), exist_ok=True)
    mrel = 'CharPipe/Shared/Materials/cp_palette.mat'
    open(os.path.join(out, 'Shared/Materials/cp_palette.mat'), 'w').write(um.mat_text('cp_palette', base='CharPipe/Shared/Textures/cp_palette_albedo.png', metal='CharPipe/Shared/Textures/cp_palette_metal.png', smoothness=1.0, metallic=1.0))
    open(os.path.join(out, 'Shared/Materials/cp_palette.mat.meta'), 'w').write(um.mat_meta(mrel))
    return mrel


# ------------------------------------------------------------------ body
def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    addon_utils.enable('bl_ext.user_default.mpfb', default_set=True, persistent=False)


def ctx(o, mode=None):
    bpy.context.view_layer.objects.active = o
    for x in bpy.context.view_layer.objects: x.select_set(x is o)
    if mode and o.mode != mode: bpy.ops.object.mode_set(mode=mode)


def aim(pb, want):
    """Turn a pose bone (armature space) so it points along `want`, about its head, keeping its roll as far as a shortest turn does."""
    head = pb.head.copy(); cur = (pb.tail - pb.head).normalized()
    q = cur.rotation_difference(want.normalized())
    pb.matrix = Matrix.Translation(head) @ q.to_matrix().to_4x4() @ Matrix.Translation(-head) @ pb.matrix
    bpy.context.view_layer.update()


def apply_rest(arm, meshes):
    """Bake the current pose into the meshes and make it the rest pose."""
    for o in meshes:
        ctx(o)
        for md in list(o.modifiers):
            if md.type == 'ARMATURE': bpy.ops.object.modifier_apply(modifier=md.name)
    ctx(arm, 'POSE'); bpy.ops.pose.armature_apply(selected=False); bpy.ops.object.mode_set(mode='OBJECT')
    for o in meshes:
        md = o.modifiers.new('Armature', 'ARMATURE'); md.object = arm


def tpose(arm, meshes, p, ref):
    """MPFB's t-pose for mixamo_unity, then every chain bone aimed along the reference rig's rest direction (Clips/reference_tpose.json, from the clips' own
    Mixamo rig): the figures rest exactly as the clips' rig does, so a retarget adds nothing. Feet and toes keep the body's own direction."""
    from bl_ext.user_default.mpfb.services.rigservice import RigService
    pose = json.load(open(os.path.join(MPFB_CODE, 'data/poses/mixamo_unity_fk/t-pose.json')))
    ctx(arm, 'POSE'); RigService.set_pose_from_dict(arm, pose); bpy.context.view_layer.update()
    P = arm.pose.bones
    def depth(b): return 0 if b.parent is None else 1 + depth(b.parent)
    for pb in sorted(P, key=lambda b: (depth(b), b.name)):
        n = pb.name[len(p):] if pb.name.startswith(p) else pb.name
        if n in ref: aim(pb, Vector(ref[n]))
    bpy.ops.object.mode_set(mode='OBJECT')
    apply_rest(arm, meshes)


def make_human(macros, sex, ref):
    from bl_ext.user_default.mpfb.services.humanservice import HumanService
    from bl_ext.user_default.mpfb.services.targetservice import TargetService
    m = TargetService.get_default_macro_info_dict(); m.update(macros)
    b = HumanService.create_human(scale=0.1, macro_detail_dict=m)
    arm = HumanService.add_builtin_rig(b, 'mixamo_unity')
    p0 = HumanService.add_mhclo_asset(f'{MPFB}/proxymeshes/{PROXY0[sex]}/{PROXY0[sex]}.proxy', b, asset_type='Proxymeshes', subdiv_levels=0)
    p1 = HumanService.add_mhclo_asset(f'{MPFB}/proxymeshes/{PROXY1}/{PROXY1}.proxy', b, asset_type='Proxymeshes', subdiv_levels=0)
    eyes = [(arm.matrix_world @ arm.data.bones[PREFIX + n].head_local).copy() for n in ('LeftEye', 'RightEye')]
    bpy.data.objects.remove(b, do_unlink=True)
    for o in (p0, p1): bake_world(o, arm)
    tpose(arm, [p0, p1], PREFIX, ref)
    return arm, [p0, p1], eyes


def prune(arm, meshes, keep, prefix=PREFIX):
    """Cut the rig to `keep`; a cut bone's weights go to its nearest kept ancestor; names lose the prefix."""
    bones = {b.name: b for b in arm.data.bones}
    bare = lambda n: n[len(prefix):] if n.startswith(prefix) else n
    def heir(n):
        b = bones[n]
        while b is not None and bare(b.name) not in keep: b = b.parent
        return b.name if b is not None else prefix + 'Hips'
    cut = sorted(n for n in bones if bare(n) not in keep)
    for o in meshes:
        for n in cut:
            if n not in o.vertex_groups: continue
            h = heir(n); hg = o.vertex_groups.get(h) or o.vertex_groups.new(name=h); gi = o.vertex_groups[n].index
            for v in o.data.vertices:
                for g in v.groups:
                    if g.group == gi and g.weight > 0: hg.add([v.index], g.weight, 'ADD')
            o.vertex_groups.remove(o.vertex_groups[n])
        for g in o.vertex_groups:
            if g.name.startswith(prefix): g.name = g.name[len(prefix):]
    ctx(arm, 'EDIT')
    eb = arm.data.edit_bones
    for n in cut: eb.remove(eb[n])
    bpy.ops.object.mode_set(mode='OBJECT')
    for b in arm.data.bones: b.name = bare(b.name)
    arm.name = 'Armature'; arm.data.name = 'Armature'
    for o in meshes:
        for md in o.modifiers:
            if md.type == 'ARMATURE': md.object = arm


def landmarks(arm, eyes, mesh):
    B = {b.name: (arm.matrix_world @ b.head_local, arm.matrix_world @ b.tail_local) for b in arm.data.bones}
    zs = [(mesh.matrix_world @ v.co).z for v in mesh.data.vertices]
    hip = (B['LeftUpLeg'][0].z + B['RightUpLeg'][0].z) / 2
    L = {'feet': 0.0, 'ankle': (B['LeftFoot'][0].z + B['RightFoot'][0].z) / 2, 'knee': (B['LeftLeg'][0].z + B['RightLeg'][0].z) / 2, 'hip': hip, 'crotch': hip - 0.07,
         'waist': B['Spine1'][0].z, 'chest': B['Spine2'][0].z, 'neckbase': B['Neck'][0].z, 'chin': B['Head'][0].z, 'eye': (eyes[0].z + eyes[1].z) / 2, 'crown': max(zs)}
    return L, B, max(zs) / 1.75


def zexpr(e, L, hs):
    if '..' in e:
        a, rest = e.split('..'); b, f = rest.split('@'); return L[a] + (L[b] - L[a]) * float(f)
    for op in ('+', '-'):
        if op in e[1:]:
            k, d = e.split(op, 1); return L[k] + (1 if op == '+' else -1) * float(d) * hs
    return L[e]


# ------------------------------------------------------------------ mesh facts
class Facts:
    def __init__(self, o, B):
        me = o.data; self.o = o
        self.co = [o.matrix_world @ v.co for v in me.vertices]
        self.nrm = [(o.matrix_world.to_3x3() @ v.normal).normalized() for v in me.vertices]
        gname = {g.index: g.name for g in o.vertex_groups}
        self.w = [{gname[g.group]: g.weight for g in v.groups if g.weight > 0 and gname[g.group] in SEG} for v in me.vertices]
        self.faces = [list(p.vertices) for p in me.polygons]
        self.cent = [sum((self.co[i] for i in f), Vector()) / len(f) for f in self.faces]
        self.fbone = []
        for f in self.faces:
            acc = {}
            for i in f:
                for k, x in self.w[i].items(): acc[k] = acc.get(k, 0.0) + x
            self.fbone.append(max(sorted(acc), key=lambda k: acc[k]) if acc else 'Hips')
        self.vseg = [SEG[max(sorted(w), key=lambda k: w[k])] if w else 'torso' for w in self.w]
        self.B = B

    def t(self, fi):
        h, tl = self.B[self.fbone[fi]]; d = tl - h
        return (self.cent[fi] - h).dot(d) / max(d.length_squared, 1e-9)

    def side(self, fi):
        h, tl = self.B[self.fbone[fi]]; ax = (tl - h).normalized(); p = self.cent[fi] - h; p = p - ax * p.dot(ax)
        if p.length < 1e-6: return 0.0, 0.0
        p.normalize(); return p.y, p.x


def region_faces(F, clause, L, hs):
    out = []
    for fi in range(len(F.faces)):
        if clause.get('all'): out.append(fi); continue
        seg = SEG[F.fbone[fi]]
        if 'seg' in clause and seg != clause['seg']: continue
        if 't' in clause:
            t = F.t(fi)
            if not (clause['t'][0] <= t < clause['t'][1]): continue
        if 'z' in clause:
            z = F.cent[fi].z
            if not (zexpr(clause['z'][0], L, hs) <= z < zexpr(clause['z'][1], L, hs)): continue
        if 'maxX' in clause:
            if abs(F.cent[fi].x - F.B['Spine2'][0].x) > clause['maxX'] * hs: continue
        if 'side' in clause:
            c = F.cent[fi]; deg = clause['deg']
            if seg in ('torso', 'head', 'neck'):
                ref = F.B['Spine2'][0] if seg == 'torso' else F.B['Head'][0]
                d = Vector((c.x - ref.x, c.y - ref.y)); d = d.normalized() if d.length > 1e-6 else Vector((0, -1))
                ang = math.degrees(math.acos(max(-1, min(1, -d.y))))     # 0 = straight front
                if clause['side'] == 'front' and ang > deg: continue
                if clause['side'] == 'back' and ang < 180 - deg: continue
            else:
                fy, fx = F.side(fi); sgn = 1.0 if c.x > 0 else -1.0
                ang = math.degrees(math.acos(max(-1, min(1, fx * sgn))))
                if clause['side'] == 'lateral' and ang > deg: continue
                if clause['side'] == 'medial' and ang < 180 - deg: continue
                if clause['side'] in ('front', 'back'):
                    a2 = math.degrees(math.acos(max(-1, min(1, -fy if clause['side'] == 'front' else fy))))
                    if a2 > deg: continue
        out.append(fi)
    return out


# ------------------------------------------------------------------ straight colour borders: cut every border into the mesh first
def cut_planes(items, person, L, B, hs, lod):
    """(plane point, plane normal, bones whose faces it may cut, (zlo, zhi, side) or None) for every border a paint rule draws; a panel's vertical
    planes cut only inside the panel's own height and side."""
    planes = []
    def add(co, no, bones, lim=None): planes.append((tuple(round(x, 6) for x in co), tuple(round(x, 6) for x in no), tuple(sorted(bones)), lim))
    torso_b = ['Hips', 'Spine', 'Spine1', 'Spine2', 'LeftShoulder', 'RightShoulder']
    for spec, _ in items:
        for cl in regions_of(spec):
            if not wanted(cl, person) or cl.get('all') or (lod and cl.get('lod0Only')): continue
            seg = cl.get('seg')
            if 'z' in cl and seg in ('torso', 'neck', 'head'):
                bones = torso_b if seg == 'torso' else ['Neck'] if seg == 'neck' else ['Head']
                for e in cl['z']:
                    if e in ('feet', 'crown', 'crown+1'): continue
                    add((0, 0, zexpr(e, L, hs)), (0, 0, 1), bones)
            if 't' in cl and seg in LIMB_BONE:
                for t in cl['t']:
                    if t <= 0 or t >= 1: continue
                    for s in ('Left', 'Right'):
                        h, tl = B[s + LIMB_BONE[seg]]; add(h + (tl - h) * t, (tl - h).normalized(), [s + LIMB_BONE[seg]])
            if 'maxX' in cl and seg == 'torso':
                cx = B['Spine2'][0].x
                zl = zexpr(cl['z'][0], L, hs) if 'z' in cl and cl['z'][0] != 'feet' else -9.0
                zh = zexpr(cl['z'][1], L, hs) if 'z' in cl and not cl['z'][1].startswith('crown') else 9.0
                sd = -1 if cl.get('side') == 'front' and cl.get('deg', 180) <= 90 else 1 if cl.get('side') == 'back' and cl.get('deg', 180) <= 90 else 0
                for sx in (-1, 1): add((cx + sx * cl['maxX'] * hs, 0, 0), (1, 0, 0), torso_b, (round(zl, 6), round(zh, 6), sd))
    return sorted(set(planes), key=lambda p: (p[0], p[1], p[2], p[3] or ()))


def cut(o, planes):
    """Bisect the faces whose dominant bone is in the plane's set (customdata, weights included, interpolate on the new vertices)."""
    if not planes: return
    me = o.data; bm = bmesh.new(); bm.from_mesh(me)
    dl = bm.verts.layers.deform.active
    gname = {g.index: g.name for g in o.vertex_groups}
    def fbone(f):
        acc = {}
        for v in f.verts:
            for gi, w in v[dl].items():
                n = gname.get(gi)
                if n in SEG: acc[n] = acc.get(n, 0.0) + w
        return max(sorted(acc), key=lambda k: acc[k]) if acc else 'Hips'
    cy = None
    for co, no, bones, lim in planes:
        bm.faces.ensure_lookup_table()
        faces = [f for f in bm.faces if fbone(f) in bones]
        if lim:
            zl, zh, sd = lim
            if cy is None: cy = sum((v.co.y for v in bm.verts), 0.0) / max(len(bm.verts), 1)
            faces = [f for f in faces if zl - 0.03 <= f.calc_center_median().z <= zh + 0.03 and (sd == 0 or (f.calc_center_median().y - cy) * sd > -0.01)]
        if not faces: continue
        geom = list(set([e for f in faces for e in f.edges] + faces + [v for f in faces for v in f.verts]))
        geom.sort(key=lambda g: (type(g).__name__, g.index))
        bmesh.ops.bisect_plane(bm, geom=geom, plane_co=Vector(co), plane_no=Vector(no), dist=0.003)
        bm.verts.index_update(); bm.edges.index_update(); bm.faces.index_update()
    bm.to_mesh(me); bm.free(); me.update()


# ------------------------------------------------------------------ geometry helpers
def hull2(pts):
    pts = sorted(set((round(p[0], 6), round(p[1], 6)) for p in pts))
    if len(pts) < 3: return pts
    def cross(o, a, b): return (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0])
    lo, hi = [], []
    for p in pts:
        while len(lo) >= 2 and cross(lo[-2], lo[-1], p) <= 0: lo.pop()
        lo.append(p)
    for p in reversed(pts):
        while len(hi) >= 2 and cross(hi[-2], hi[-1], p) <= 0: hi.pop()
        hi.append(p)
    return lo[:-1] + hi[:-1]


def ray_hull(c, d, hull):
    best = None
    for i in range(len(hull)):
        a = Vector(hull[i]); b = Vector(hull[(i + 1) % len(hull)]); e = b - a
        den = d.x * (-e.y) - d.y * (-e.x)
        if abs(den) < 1e-12: continue
        q = a - c
        t = (q.x * (-e.y) - q.y * (-e.x)) / den; s = (d.x * q.y - d.y * q.x) / den
        if t >= 0 and -1e-9 <= s <= 1 + 1e-9: best = t if best is None else max(best, t)
    return best or 0.0


def slab(F, z, dz, segs):
    return [F.co[i] for i in range(len(F.co)) if abs(F.co[i].z - z) < dz and F.vseg[i] in segs]


def band(F, z0, z1, segs):
    lo, hi = min(z0, z1), max(z0, z1)
    return [F.co[i] for i in range(len(F.co)) if lo - 0.005 <= F.co[i].z <= hi + 0.005 and F.vseg[i] in segs]


def ang_dir(a): return Vector((math.sin(a), -math.cos(a)))   # a = 0: the front (-Y); + toward the figure's left (+X)


class Extra:
    def __init__(self): self.v = []; self.f = []; self.role = []; self.mode = []

    def add_v(self, p, mode): self.v.append(Vector(p)); self.mode.append(mode); return len(self.v) - 1

    def quad(self, a, b, c, d, role): self.f.append((a, b, c, d)); self.role.append(role)

    def tri(self, a, b, c, role): self.f.append((a, b, c)); self.role.append(role)


def ring_grid(X, rings, closed, role_of_strip, mode_of_ring, outward_ref):
    idx = [[X.add_v(p, mode_of_ring(k)) for p in ring] for k, ring in enumerate(rings)]
    n = len(rings[0]); span = n if closed else n - 1
    for k in range(len(rings) - 1):
        for j in range(span):
            a, b, c, d = idx[k][j], idx[k][(j + 1) % n], idx[k + 1][(j + 1) % n], idx[k + 1][j]
            pa, pb, pd = X.v[a], X.v[b], X.v[d]
            nrm = (pb - pa).cross(pd - pa); mid = (pa + X.v[c]) / 2
            if nrm.dot(mid - outward_ref(mid)) < 0: X.quad(a, b, c, d, role_of_strip(k))
            else: X.quad(a, d, c, b, role_of_strip(k))
    return idx


def double(X, rings, closed, role, mode, centre, inset):
    """the same rings again a little inside, faces turned in: a wall seen from inside (collars, hair, cape collars)."""
    inner = [[Vector((centre.x + (p.x - centre.x) * (1 - inset / max((Vector((p.x - centre.x, p.y - centre.y))).length, 1e-4)),
                      centre.y + (p.y - centre.y) * (1 - inset / max((Vector((p.x - centre.x, p.y - centre.y))).length, 1e-4)), p.z)) for p in ring] for ring in rings]
    ring_grid(X, inner, closed, lambda k: role, mode, lambda p: p + (p - Vector((centre.x, centre.y, p.z))))
    return inner


def head_hull(F, z, dz, hs):
    hd = slab(F, z, dz, ('head', 'neck'))
    c = sum(hd, Vector()) / max(len(hd), 1)
    return Vector((c.x, c.y)), hull2([(p.x, p.y) for p in hd]), c


def tube(X, F, L, hs, spec, lod, left_x):
    zt = zexpr(spec['top'], L, hs); zb = zexpr(spec['bottom'], L, hs); bnd = spec.get('band', 0.0) * hs
    R = spec['rings'][lod]; N = spec['segs'][lod]
    top = slab(F, zt, 0.025 * hs, ('torso', 'thigh'))
    c = sum(top, Vector()) / len(top); c2 = Vector((c.x, c.y))
    htop = hull2([(p.x, p.y) for p in top])
    zs = [zt + (zb + bnd - zt) * k / R for k in range(R + 1)] + ([zb] if bnd > 0 else [])
    a0 = math.radians(spec.get('halfDeg', 180)); closed = spec.get('halfDeg', 180) >= 180
    angs = [2 * math.pi * j / N for j in range(N)] if closed else [-a0 + 2 * a0 * j / N for j in range(N + 1)]
    rings = []
    for k, z in enumerate(zs):
        s = (zt - z) / max(zt - zb, 1e-6)
        za = zs[k - 1] if k > 0 else z; zb_ = zs[k + 1] if k + 1 < len(zs) else z - 0.03 * hs
        sl = [F.co[i] for i in range(len(F.co)) if zb_ - 0.01 <= F.co[i].z <= za + 0.01 and F.vseg[i] in ('torso', 'thigh', 'shin')]
        sl += [p for p in X.v if zb_ - 0.01 <= p.z <= za + 0.01 and (Vector((p.x, p.y)) - c2).length < 0.5]
        hz = hull2([(p.x, p.y) for p in sl]) if len(sl) > 3 else None
        ring = []
        for a in angs:
            d = ang_dir(a); r0 = ray_hull(c2, d, htop)
            r = r0 - 0.004 if k == 0 else max(r0 * (1 + spec['flare'] * s), (ray_hull(c2, d, hz) + 0.018 * hs) if hz else 0.0) / math.cos(math.pi / N)
            if bnd > 0 and k >= len(zs) - 2: r += 0.008 * hs
            ring.append(Vector((c.x + d.x * r, c.y + d.y * r, z)))
        rings.append(ring)
    nstrip = len(rings) - 1
    role = lambda k: spec['bandRole'] if (bnd > 0 and k == nstrip - 1) else spec['role']
    lf, lb = spec.get('legsFront', 0.8), spec.get('legsBack', 0.35)
    mode = lambda k: ('legs', min(1.0, (zt - zs[k]) / max(zt - zb, 1e-6)), lf, lb, c.x, c.y, left_x)
    ring_grid(X, rings, closed, role, mode, lambda p: Vector((c.x, c.y, p.z)))
    if not closed: double(X, rings, False, spec['role'], mode, c, 0.004)
    return zt, zb


def collar(X, F, L, hs, spec, lod):
    z0 = L['neckbase'] + 0.004 * hs; hgt = spec['height'] * hs; N = (18, 10)[lod]
    sl = slab(F, z0 + 0.02 * hs, 0.02 * hs, ('neck',)) or slab(F, z0 + 0.02 * hs, 0.03 * hs, ('neck', 'head')); c = sum(sl, Vector()) / len(sl); c2 = Vector((c.x, c.y))
    h = hull2([(p.x, p.y) for p in sl])
    outer = []
    for k, z in enumerate((z0, z0 + hgt)):
        ro = []
        for j in range(N):
            d = ang_dir(2 * math.pi * j / N); r = (ray_hull(c2, d, h) + 0.006 * hs) * (1 + spec['flare'] * k)
            ro.append(Vector((c.x + d.x * r, c.y + d.y * r, z)))
        outer.append(ro)
    mode = lambda k: ('dt',)
    ring_grid(X, outer, True, lambda k: spec['role'], mode, lambda p: Vector((c.x, c.y, p.z)))
    inner = double(X, outer, True, spec['role'], mode, c, 0.005)
    ring_grid(X, [outer[1], inner[1]], True, lambda k: spec['bandRole'], mode, lambda p: Vector((p.x, p.y, p.z - 1.0)))


def curtain(X, F, L, hs, spec, lod):
    zt = zexpr(spec['top'], L, hs); zb = zexpr(spec['bottom'], L, hs); R = spec['rings'][lod]; N = spec['segs'][lod]
    c2, h, c = head_hull(F, zt, 0.02 * hs, hs)
    a0 = math.radians(spec['openFrontDeg']); angs = [a0 + (2 * math.pi - 2 * a0) * j / N for j in range(N + 1)]
    outer = []
    for k in range(R + 1):
        z = zt + (zb - zt) * k / R; s = k / R
        ro = []
        for a in angs:
            d = ang_dir(a); r = (ray_hull(c2, d, h) + spec['pad'] * hs) * (1 + spec['flare'] * s * 0.25)
            if k == 0: r -= 0.004
            ro.append(Vector((c.x + d.x * r, c.y + d.y * r, z)))
        outer.append(ro)
    m = lambda k: ('rigid', 'Head')
    ring_grid(X, outer, False, lambda k: spec['role'], m, lambda p: Vector((c.x, c.y, p.z)))
    inner = double(X, outer, False, spec['role'], m, c, 0.006)
    ring_grid(X, [outer[-1], inner[-1]], False, lambda k: spec['role'], m, lambda p: Vector((p.x, p.y, p.z + 1.0)))


def fitted_rings(F, c2, zs, base_r, pad, hs):
    """per ring, per angle: the larger of the shape asked for and the head (and hair) there plus pad: nothing pokes through."""
    out = []
    for z, shape in zs:
        sl = [F.co[i] for i in range(len(F.co)) if F.vseg[i] in ('head', 'neck') and F.co[i].z >= z - 0.004]
        h = hull2([(p.x, p.y) for p in sl]) if len(sl) > 3 else None
        out.append([max(base_r[j] * shape, (ray_hull(c2, ang_dir(2 * math.pi * j / len(base_r)), h) + pad) if h else 0.0) for j in range(len(base_r))])
    return out


def head_top(F):
    return max(F.co[i].z for i in range(len(F.co)) if F.vseg[i] in ('head',))


def dome(X, F, L, hs, spec, lod):
    """a rounded cap (with a visor) or a hair puff: rings from the base up, each clearing the head and hair above it."""
    zb = zexpr(spec['base'], L, hs); N = spec['segs'][lod]; R = spec['rings'][lod]; bandh = spec.get('band', 0.0) * hs; pad = spec['pad'] * hs
    c2, h, c = head_hull(F, zb, 0.02 * hs, hs)
    rad = [ray_hull(c2, ang_dir(2 * math.pi * j / N), h) + pad for j in range(N)]
    top = head_top(F) + spec['lift'] * hs; zb2 = zb + bandh
    zs = [(zb, 1.0)] + ([(zb2, 1.0)] if bandh else []) + [(zb2 + (top - zb2) * math.sin((math.pi / 2) * k / R), math.cos((math.pi / 2) * k / R)) for k in range(1, R)]
    radii = fitted_rings(F, c2, zs, rad, pad, hs)
    rings = [[Vector((c2.x + ang_dir(2 * math.pi * j / N).x * radii[k][j], c2.y + ang_dir(2 * math.pi * j / N).y * radii[k][j], zs[k][0])) for j in range(N)] for k in range(len(zs))]
    m = lambda k: ('rigid', 'Head')
    idx = ring_grid(X, rings, True, lambda k: spec.get('bandRole', spec['role']) if (bandh and k == 0) else spec['role'], m, lambda p: Vector((c2.x, c2.y, zb)))
    apex = X.add_v((c2.x, c2.y, top), m(0)); last = idx[-1]
    for j in range(N):
        a, b = last[j], last[(j + 1) % N]
        nrm = (X.v[b] - X.v[a]).cross(X.v[apex] - X.v[a])
        X.tri(a, b, apex, spec['role']) if nrm.z > 0 else X.tri(b, a, apex, spec['role'])
    if spec.get('visor'): visor(X, c2, zb, rad, N, spec, hs, m, inset=pad)


def visor(X, c2, z, rad, N, spec, hs, m, inset=0.0):
    span = sorted([j for j in range(N) if math.cos(2 * math.pi * j / N) > math.cos(math.radians(spec.get('visorDeg', 70)))], key=lambda j: ((2 * math.pi * j / N + math.pi) % (2 * math.pi)))
    drop = math.tan(math.radians(spec['visorDropDeg'])) * spec['visor'] * hs
    inn = [Vector((c2.x + ang_dir(2 * math.pi * j / N).x * (rad[j] - inset * 0.7), c2.y + ang_dir(2 * math.pi * j / N).y * (rad[j] - inset * 0.7), z + 0.002)) for j in span]
    out = [Vector((p.x + ang_dir(2 * math.pi * j / N).x * spec['visor'] * hs * math.cos(2 * math.pi * j / N) ** 0.5, p.y + ang_dir(2 * math.pi * j / N).y * spec['visor'] * hs * math.cos(2 * math.pi * j / N) ** 0.5, p.z - drop)) for p, j in zip(inn, span)]
    role = spec.get('visorRole', spec['role'])
    ring_grid(X, [inn, out], False, lambda k: role, m, lambda p: Vector((p.x, p.y, p.z - 1.0)))
    ring_grid(X, [inn, out], False, lambda k: role, m, lambda p: Vector((p.x, p.y, p.z + 1.0)))


def peaked(X, F, L, hs, spec, lod):
    """a peaked (service) cap: a band round the head, a crown wider than the band, a dark visor: the guard's shape."""
    z0 = zexpr(spec['base'], L, hs); N = spec['segs'][lod]; pad = spec['pad'] * hs
    c2, h, c = head_hull(F, z0, 0.02 * hs, hs)
    base = [ray_hull(c2, ang_dir(2 * math.pi * j / N), h) + pad for j in range(N)]
    top = max(z0 + spec['bandH'] * hs, head_top(F) - 0.012 * hs)
    z1 = top; z2 = top + spec['crownLift'] * hs
    rb = fitted_rings(F, c2, [(z0, 1.0), (z1, 1.0)], base, pad, hs)
    crown_r = [max(rb[1][j], rb[0][j]) * spec['crownFlare'] for j in range(N)]
    P = lambda r, z: [Vector((c2.x + ang_dir(2 * math.pi * j / N).x * r[j], c2.y + ang_dir(2 * math.pi * j / N).y * r[j], z)) for j in range(N)]
    zb = z0 + spec.get('band', 0.018) * hs
    rings = [P(rb[0], z0), P([rb[0][j] + (rb[1][j] - rb[0][j]) * (zb - z0) / max(z1 - z0, 1e-4) for j in range(N)], zb), P(rb[1], z1), P(crown_r, z1 + (z2 - z1) * 0.6), P([x * 0.92 for x in crown_r], z2)]
    m = lambda k: ('rigid', 'Head')
    roles = [spec['bandRole'], spec['role'], spec['role'], spec['role']]
    idx = ring_grid(X, rings, True, lambda k: roles[k], m, lambda p: Vector((c2.x, c2.y, z0 - 0.1)))
    apex = X.add_v((c2.x, c2.y, z2 + 0.004), m(0)); last = idx[-1]
    for j in range(N):
        a, b = last[j], last[(j + 1) % N]
        nrm = (X.v[b] - X.v[a]).cross(X.v[apex] - X.v[a])
        X.tri(a, b, apex, spec['role']) if nrm.z > 0 else X.tri(b, a, apex, spec['role'])
    visor(X, c2, z0, rb[0], N, spec, hs, m, inset=pad)


def pillbox(X, F, L, hs, spec, lod):
    """a small flat-topped cylinder on the crown (cafe): the base ring follows the head where it sits, so it never floats or sinks."""
    N = spec['segs'][lod]; ht = head_top(F); z0 = ht - spec['sink'] * hs; z1 = ht + spec['height'] * hs
    c2, h, c = head_hull(F, z0 - 0.01 * hs, 0.012 * hs, hs)
    r = [ray_hull(c2, ang_dir(2 * math.pi * j / N), h) * spec['scale'] for j in range(N)]
    c2 = c2 + Vector((0, spec.get('back', 0.0) * hs))
    P = lambda z: [Vector((c2.x + ang_dir(2 * math.pi * j / N).x * r[j], c2.y + ang_dir(2 * math.pi * j / N).y * r[j], z)) for j in range(N)]
    m = lambda k: ('rigid', 'Head')
    roles = [spec['bandRole'], spec['role']]
    idx = ring_grid(X, [P(z0), P(z0 + spec['band'] * hs), P(z1)], True, lambda k: roles[min(k, 1)], m, lambda p: Vector((c2.x, c2.y, z0 - 0.1)))
    apex = X.add_v((c2.x, c2.y, z1), m(0)); last = idx[-1]
    for j in range(N):
        a, b = last[j], last[(j + 1) % N]
        nrm = (X.v[b] - X.v[a]).cross(X.v[apex] - X.v[a])
        X.tri(a, b, apex, spec['role']) if nrm.z > 0 else X.tri(b, a, apex, spec['role'])


def capelet(X, F, L, hs, spec, lod):
    """a cape collar: a short ring hung from the neck over the shoulders, wider than the body line from above (the rival's cut). Weighted to the chest and
    shoulders only, never the arms, so it stays a collar when the arms move under it."""
    zt = zexpr(spec['top'], L, hs); zb = zexpr(spec['bottom'], L, hs); R = spec['rings'][lod]; N = spec['segs'][lod]
    top = slab(F, zt, 0.02 * hs, ('neck', 'torso')); c = sum(top, Vector()) / len(top); c2 = Vector((c.x, c.y)); ht = hull2([(p.x, p.y) for p in top])
    body = [F.co[i] for i in range(len(F.co)) if zb - 0.01 <= F.co[i].z <= zt and F.vseg[i] in ('torso', 'neck')]
    hb = hull2([(p.x, p.y) for p in body])
    rings = []
    bandh = spec.get('band', 0.012) * hs
    for k in range(R + 2):
        s = min(k, R) / R; z = zt + (zb - zt) * s if k <= R else zb - bandh
        if k == R + 1: s = 1.0 + bandh / max(zt - zb, 1e-4)
        rings.append([Vector((c.x + ang_dir(2 * math.pi * j / N).x * rr, c.y + ang_dir(2 * math.pi * j / N).y * rr, z)) for j in range(N)
                      for rr in [max(ray_hull(c2, ang_dir(2 * math.pi * j / N), ht) * (1 + spec['flare'] * s ** 0.7), (ray_hull(c2, ang_dir(2 * math.pi * j / N), hb) + spec['pad'] * hs) * min(1.0, 0.3 + s)) / math.cos(math.pi / N)]])
    lx = F.B['LeftShoulder'][0].x
    m = lambda k: ('cape', min(k, R) / R, c.x, lx)
    roles = lambda k: spec['bandRole'] if k == R else spec['role']
    ring_grid(X, rings, True, roles, m, lambda p: Vector((c.x, c.y, p.z + 0.2)))
    double(X, rings, True, spec['role'], m, c, 0.005)


def hood(X, F, L, hs, spec, lod):
    """a hood: over the head (and hair) and down the back of the neck to the shoulders, open at the face; above the chin it rides the head, below it it
    blends to the chest so the neck can turn. The rival's head shape, and from behind it reads as a back."""
    N = spec['segs'][lod]; pad = spec['pad'] * hs; a0 = math.radians(spec['openFrontDeg'])
    angs = [a0 + (2 * math.pi - 2 * a0) * j / N for j in range(N + 1)]
    c2, h, c = head_hull(F, L['eye'], 0.02 * hs, hs)
    ht = head_top(F); top = ht + pad
    zs = [ht - 0.018 * hs, ht - 0.045 * hs, L['eye'] + 0.05 * hs, L['eye'], L['chin'], L['neckbase'] + 0.01 * hs, zexpr(spec['bottom'], L, hs)]
    rings = []; prev = None
    for k, z in enumerate(zs):
        sl = [F.co[i] for i in range(len(F.co)) if F.vseg[i] in ('head', 'neck', 'torso') and (F.co[i].z >= z - 0.004 if k < 2 else abs(F.co[i].z - z) < 0.03 * hs) and (F.vseg[i] != 'torso' or k >= 5)]
        hz = hull2([(p.x, p.y) for p in sl]) if len(sl) > 3 else h
        r = [ray_hull(c2, ang_dir(a), hz) + pad * (1.0 + 0.6 * k / len(zs)) for a in angs]
        if prev: r = [max(x, y * (0.92 if k < 5 else 1.0)) if k >= 2 else x for x, y in zip(r, prev)]
        prev = r; rings.append([Vector((c2.x + ang_dir(a).x * rr, c2.y + ang_dir(a).y * rr, z)) for a, rr in zip(angs, r)])
    zc, zn = L['chin'], zs[-1]
    m = lambda k: ('hood', zs[k], zc, zn)
    ring_grid(X, rings, False, lambda k: spec['role'], m, lambda p: Vector((c2.x, c2.y, p.z)))
    double(X, rings, False, spec['role'], m, Vector((c2.x, c2.y, 0)), 0.005)
    apex = X.add_v((c2.x, c2.y + 0.01 * hs, top), ('hood', top, zc, zn)); first = [X.add_v(p, m(0)) for p in rings[0]]
    for j in range(N):
        a, b = first[j], first[j + 1]; nrm = (X.v[b] - X.v[a]).cross(X.v[apex] - X.v[a])
        X.tri(a, b, apex, spec['role']) if nrm.z > 0 else X.tri(b, a, apex, spec['role'])


def bag(X, F, L, hs, spec, lod, bvh):
    """a strap from the left shoulder across the body to a flat case at the right hip: a front and a back at a glance."""
    lsh = F.B['LeftShoulder'][1]; rhip = F.B['RightUpLeg'][0]; sx = 1.0 if F.B['LeftUpLeg'][0].x > 0 else -1.0
    a = Vector((lsh.x - sx * 0.02 * hs, lsh.y, lsh.z + 0.03 * hs)); b = Vector((rhip.x - sx * 0.07 * hs, rhip.y, rhip.z + 0.06 * hs))
    n = (10, 6)[lod]; w = spec['strap'] * hs / 2; off = spec['offset'] * hs
    for face_dir in ((-1, 1) if not spec.get('noStrap') else ()):       # front and back of the body
        pts = []
        for i in range(n + 1):
            p = a.lerp(b, i / n)
            ray_o = Vector((p.x, p.y + face_dir * -0.5, p.z)); hit, nrm, _, _ = bvh.ray_cast(ray_o, Vector((0, face_dir, 0)), 1.0)
            if hit is None: hit, nrm = p, Vector((0, -face_dir, 0))
            pts.append((hit + nrm.normalized() * off, nrm.normalized()))
        along = (b - a).normalized()
        left = [p + (along.cross(nv)).normalized() * w for p, nv in pts]; right = [p - (along.cross(nv)).normalized() * w for p, nv in pts]
        ring_grid(X, [left, right], False, lambda k: spec['strapRole'], lambda k: ('dt',), lambda q: q + Vector((0, face_dir * 0.3, 0)))
    # the case: a box at the right hip's outer side
    bw, bh, bd = spec['case']; bw *= hs; bh *= hs; bd *= hs
    cx = rhip.x - sx * (0.075 + bd / 2 + 0.03) * hs; cz = rhip.z - 0.02 * hs; cy = rhip.y
    corners = [Vector((cx + dx * bd / 2, cy + dy * bw / 2, cz + dz * bh / 2)) for dx in (-1, 1) for dy in (-1, 1) for dz in (-1, 1)]
    m = ('rigid', 'Hips'); ids = [X.add_v(p, m) for p in corners]
    for fq in ((0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1), (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3)):
        q = [ids[i] for i in fq]; cen = sum((X.v[i] for i in q), Vector()) / 4; nrm = (X.v[q[1]] - X.v[q[0]]).cross(X.v[q[3]] - X.v[q[0]])
        mid = Vector((cx, cy, cz))
        X.quad(*q, spec['role']) if nrm.dot(cen - mid) > 0 else X.quad(q[0], q[3], q[2], q[1], spec['role'])


# ------------------------------------------------------------------ one LOD
def regions_of(spec):
    return spec.get('regions', []) + spec.get('paint', {}).get('regions', [])


def wanted(cl, person):
    return not any(n in person['parts'] for n in cl.get('skipIf', [])) and all(n in person['parts'] for n in cl.get('onlyIf', []))


def items_for(person, ward):
    items = [(ward['base'][n], n) for n in person['base']] + [(ward['parts'][n], n) for n in person['parts']]
    if person.get('buzz'): items.append(({'layer': 70, 'kind': 'paint', 'offsetMm': 3, 'regions': ward['parts']['hair_crop']['regions']}, 'buzz'))
    items.sort(key=lambda x: (x[0]['layer'], x[1]))
    return items


def dress(o, arm, B, L, hs, eyes, person, ward, lod, colours, index):
    items = items_for(person, ward)
    cut(o, cut_planes(items, person, L, B, hs, lod))
    F = Facts(o, B); nf = len(F.faces)
    role = ['S'] * nf; off = [0.0] * nf; layer = [0] * nf
    left_x = B['LeftUpLeg'][0].x
    collar_spec = None
    # pass 1: paint (and what a tube will hang over takes the tube's colour)
    for spec, name in items:
        if regions_of(spec):
            for cl in regions_of(spec):
                if not wanted(cl, person) or (lod and cl.get('lod0Only')): continue
                for fi in region_faces(F, cl, L, hs):
                    if spec['layer'] >= layer[fi] or cl.get('all'):
                        role[fi] = cl['role']; off[fi] = spec.get('offsetMm', 6) / 1000.0 * hs; layer[fi] = spec['layer']
        if spec['kind'] == 'paint':
            if 'shoulders' in spec:
                sh = spec['shoulders']; z0, z1 = zexpr(sh['z'][0], L, hs), zexpr(sh['z'][1], L, hs)
                for fi in range(nf):
                    c = F.cent[fi]
                    if layer[fi] == spec['layer'] and SEG[F.fbone[fi]] in ('torso', 'arm') and z0 <= c.z < z1 and abs(c.x) > sh['minX'] * hs: off[fi] = sh['offsetMm'] / 1000.0 * hs
            for tb in spec.get('tubes', []):
                if any(n in person['parts'] for n in tb.get('skipIf', [])) or tb.get('halfDeg', 180) < 180: continue
                zt, zb = zexpr(tb['top'], L, hs), zexpr(tb['bottom'], L, hs)
                for fi in range(nf):
                    sg = SEG[F.fbone[fi]]; z = F.cent[fi].z
                    if (sg == 'torso' and zb - 0.04 * hs <= z < zt) or (sg == 'thigh' and zb <= z < zt):     # whatever hangs under a tube takes its colour, whatever its layer (a jacket's hem band under a skirt showed as a white square: cp_commuter_02)
                        role[fi] = tb['role']; layer[fi] = spec['layer']
            if 'collar' in spec: collar_spec = spec['collar']
        elif spec['kind'] == 'eyes':
            for e in eyes:
                cand = sorted((fi for fi in range(nf) if SEG[F.fbone[fi]] == 'head' and F.cent[fi].y < e.y + 0.004),
                              key=lambda fi: ((F.cent[fi].x - e.x) ** 2 + (F.cent[fi].z - e.z) ** 2, fi))
                for fi in cand[:(2 if lod == 0 else 1)]: role[fi] = spec['role']; layer[fi] = spec['layer']
                brow = Vector((e.x, e.y, e.z + 0.03 * hs))
                cb = sorted((fi for fi in range(nf) if SEG[F.fbone[fi]] == 'head' and F.cent[fi].y < e.y + 0.004 and role[fi] == 'S'),
                            key=lambda fi: ((F.cent[fi].x - brow.x) ** 2 + (F.cent[fi].z - brow.z) ** 2, fi))
                for fi in cb[:1]: role[fi] = spec['browRole']; layer[fi] = spec['layer']
    # pass 2: push the painted faces out (a vertex moves by the least offset of the faces round it, so a garment meets the skin flush)
    vo = [None] * len(F.co)
    for fi, f in enumerate(F.faces):
        for i in f: vo[i] = off[fi] if vo[i] is None else min(vo[i], off[fi])
    me = o.data
    for i, v in enumerate(me.vertices):
        if vo[i]: v.co = v.co + F.nrm[i] * vo[i]
    me.update()
    # pass 3: the extra geometry, on the pushed-out surface (so a cap clears the hair and a hem the trousers)
    F = Facts(o, B)
    from mathutils.bvhtree import BVHTree
    bvh = BVHTree.FromPolygons([tuple(p) for p in F.co], F.faces)
    X = Extra()
    for spec, name in items:
        k = spec['kind']
        if k == 'paint':
            for tb in spec.get('tubes', []):
                if any(n in person['parts'] for n in tb.get('skipIf', [])): continue
                tube(X, F, L, hs, tb, lod, left_x)
            if 'curtain' in spec: curtain(X, F, L, hs, spec['curtain'], lod)
            if 'capelet' in spec: capelet(X, F, L, hs, spec['capelet'], lod)
            if 'hood' in spec: hood(X, F, L, hs, spec['hood'], lod)
            if 'bag' in spec: bag(X, F, L, hs, spec['bag'], lod, bvh)
        elif k == 'dome': dome(X, F, L, hs, spec, lod)
        elif k == 'peaked': peaked(X, F, L, hs, spec, lod)
        elif k == 'pillbox': pillbox(X, F, L, hs, spec, lod)
    if collar_spec: collar(X, F, L, hs, collar_spec, lod)
    # weights of the extra geometry: Data Transfer from the body (nearest face interpolated), then the rules by kind
    ext_roles = list(X.role); eo = None
    if X.f:
        em = bpy.data.meshes.new(o.name + '_x'); em.from_pydata([tuple(p) for p in X.v], [], X.f); em.update()
        eo = bpy.data.objects.new(o.name + '_x', em); bpy.context.scene.collection.objects.link(eo)
        for g in o.vertex_groups: eo.vertex_groups.new(name=g.name)
        md = eo.modifiers.new('dt', 'DATA_TRANSFER'); md.object = o; md.use_vert_data = True; md.data_types_verts = {'VGROUP_WEIGHTS'}
        md.vert_mapping = 'POLYINTERP_NEAREST'; md.layers_vgroup_select_src = 'ALL'; md.layers_vgroup_select_dst = 'NAME'
        with bpy.context.temp_override(object=eo, active_object=eo, selected_objects=[eo]): bpy.ops.object.modifier_apply(modifier='dt')
        for vi, mode in enumerate(X.mode):
            if mode[0] == 'dt': continue
            cur = {eo.vertex_groups[g.group].name: g.weight for g in eo.data.vertices[vi].groups}
            p = X.v[vi]
            if mode[0] == 'rigid': new = {mode[1]: 1.0}
            elif mode[0] == 'hood':
                _, z, zc, zn = mode; t = max(0.0, min(1.0, (z - zn) / max(zc - zn, 1e-4)))
                new = {'Head': t, 'Neck': (1 - t) * 0.4, 'Spine2': (1 - t) * 0.6}
            elif mode[0] == 'cape':
                _, s, cx, lx = mode; sgn = 1.0 if lx > cx else -1.0
                l = max(0.0, min(1.0, 0.5 + sgn * (p.x - cx) / 0.16)); a = 0.55 * min(1.0, s + 0.3)
                new = {'Spine2': 1 - a, 'LeftShoulder': a * l, 'RightShoulder': a * (1 - l)}
            else:
                _, s, lf, lb, cx, cy, lx = mode; sgn = 1.0 if lx > cx else -1.0
                d = Vector((p.x - cx, p.y - cy)); d = d.normalized() if d.length > 1e-6 else Vector((0, -1))
                front = (1 - d.y) / 2; legs = lb + (lf - lb) * front
                l = max(0.0, min(1.0, 0.5 + sgn * (p.x - cx) / 0.18))
                tgt = {'Hips': 1 - legs, 'LeftUpLeg': legs * l, 'RightUpLeg': legs * (1 - l)}
                tot = sum(cur.values()) or 1.0; cur = {k: x / tot for k, x in cur.items()}
                s = s ** 0.8; new = {k: (1 - s) * cur.get(k, 0.0) + s * tgt.get(k, 0.0) for k in sorted(set(cur) | set(tgt))}
            for k in list(cur): eo.vertex_groups[k].remove([vi])
            for k, x in new.items():
                if x > 1e-4: eo.vertex_groups[k].add([vi], x, 'REPLACE')
    for uvl in list(me.uv_layers): me.uv_layers.remove(uvl)
    def uv_and_mat(mesh, roles):
        uv = mesh.uv_layers.new(name='UVMap'); ch = mesh.uv_layers.new(name='Channel')
        for p in mesh.polygons:
            col = colours[roles[p.index]]; u, v = cell_uv(index[col])
            for li in p.loop_indices:
                uv.data[li].uv = (u, v); ch.data[li].uv = ((ROLES.index(roles[p.index]) + 0.5) / CELLS, 0.5)
            p.use_smooth = True
    uv_and_mat(me, role)
    if eo:
        uv_and_mat(eo.data, ext_roles)
        with bpy.context.temp_override(object=o, active_object=o, selected_objects=[o, eo], selected_editable_objects=[o, eo]): bpy.ops.object.join()
    mat = bpy.data.materials.get('cp_palette') or bpy.data.materials.new('cp_palette')
    me.materials.clear(); me.materials.append(mat)
    for p in me.polygons: p.material_index = 0
    limit4(o)
    return {r: role.count(r) + ext_roles.count(r) for r in ROLES if role.count(r) + ext_roles.count(r)}


def limit4(o, keep=KEEP):
    names = {g.index: g.name for g in o.vertex_groups}
    for v in o.data.vertices:
        ws = sorted(((g.weight, names[g.group]) for g in v.groups if g.weight > 0 and names[g.group] in keep), key=lambda x: (-x[0], x[1]))
        top = ws[:4]; tot = sum(w for w, _ in top) or 1.0
        for g in list(v.groups): o.vertex_groups[names[g.group]].remove([v.index])
        for w, n in top: o.vertex_groups[n].add([v.index], w / tot, 'REPLACE')
        if not top: o.vertex_groups['Hips'].add([v.index], 1.0, 'REPLACE')
    for g in list(o.vertex_groups):
        if g.name not in keep: o.vertex_groups.remove(g)


def tris(o): return sum(len(p.vertices) - 2 for p in o.data.polygons)


def bake_world(o, arm):
    o.data.transform(o.matrix_world); o.parent = None; o.matrix_world = Matrix.Identity(4)
    o.parent = arm; o.matrix_parent_inverse = Matrix.Identity(4)
    for md in list(o.modifiers):
        if md.type != 'ARMATURE': o.modifiers.remove(md)


def collapse(o, ratio):
    md = o.modifiers.new('cp_decimate', 'DECIMATE'); md.ratio = ratio; md.use_collapse_triangulate = False
    with bpy.context.temp_override(object=o, active_object=o):
        bpy.ops.object.modifier_move_to_index(modifier=md.name, index=0)
        bpy.ops.object.modifier_apply(modifier=md.name)


def dress_lod1(o, arm, B, L, hs, eyes, person, ward, colours, index):
    """LOD1 within LOD1_MAX_TRIS: dress proxy741 as it is; if over, collapse its body a step first (the garments' own geometry is never thinned) and dress again."""
    src = o.data.copy(); src.name = o.data.name + '_src'
    for r in (1.0, 0.94, 0.88, 0.82, 0.76, 0.7, 0.64, 0.58, 0.52, 0.47, 0.42, 0.38):
        if r < 1.0:
            old = o.data; o.data = src.copy(); bpy.data.meshes.remove(old)
            collapse(o, r)
        hit = dress(o, arm, B, L, hs, eyes, person, ward, 1, colours, index)
        if tris(o) <= LOD1_MAX_TRIS: bpy.data.meshes.remove(src); return hit, r
    bpy.data.meshes.remove(src); return hit, r


# ------------------------------------------------------------------ the crowd export: fingers relaxed, baked, folded into the hand
RELAX = {'1': 12, '2': 22, '3': 16}; THUMB = {'1': 4, '2': 10, '3': 10}


def crowd_skeleton(arm, meshes):
    ctx(arm, 'POSE'); P = arm.pose.bones
    for side, sx in (('Left', 1.0), ('Right', -1.0)):
        palm = Vector((0, 0, -1))              # palms down in the T-pose rest
        for f in ('Index', 'Middle', 'Ring', 'Pinky', 'Thumb'):
            for i in ('1', '2', '3'):
                pb = P[side + 'Hand' + f + i]; d = (pb.tail - pb.head).normalized()
                ang = math.radians((THUMB if f == 'Thumb' else RELAX)[i])
                axis = d.cross(palm)
                if axis.length < 1e-6: continue
                q = Matrix.Rotation(ang, 4, axis.normalized())
                pb.matrix = Matrix.Translation(pb.head) @ q @ Matrix.Translation(-pb.head) @ pb.matrix
                bpy.context.view_layer.update()
    bpy.ops.object.mode_set(mode='OBJECT')
    apply_rest(arm, meshes)
    prune(arm, meshes, CORE, prefix='')
    for o in meshes: limit4(o, CORE)


# ------------------------------------------------------------------ one person
def leg_length(arm):
    """thigh + shin in the rest pose, both legs averaged: measured as the clip stage measures a clip's rig (pipeline/clips.py leg_length)."""
    W = arm.matrix_world; B = arm.data.bones; t = 0.0
    for s in ('Left', 'Right'):
        up, kn, an = W @ B[s + 'UpLeg'].head_local, W @ B[s + 'Leg'].head_local, W @ B[s + 'Foot'].head_local
        t += (kn - up).length + (an - kn).length
    return t / 2


def speeds(out, leg):
    """each clip's speed for this body: the clip's stride (measured on its own rig) x this leg / the clip rig's leg. Nothing tuned."""
    p = os.path.join(out, 'Clips/clips.rows.json')
    if not os.path.exists(p): raise SystemExit('FAIL: build the clips stage first (Clips/clips.rows.json missing)')
    return {c['name']: round(c['strideMs'] * leg / c['sourceLegLengthM'], 3) for c in json.load(open(p))['clips']}


def reference(out):
    p = os.path.join(out, 'Clips/reference_tpose.json')
    if not os.path.exists(p): raise SystemExit('FAIL: build the clips stage first (Clips/reference_tpose.json missing)')
    return json.load(open(p))['directions']


def build(person, ward, out, index):
    reset()
    arm, (p0, p1), eyes = make_human(person['macros'], person['sex'], reference(out))
    prune(arm, [p0, p1], KEEP)
    L, B, hs = landmarks(arm, eyes, p0)
    fid = person['id'].replace('-', '_')
    cols = person['colours']
    if os.environ.get('CP_DEBUG'):
        dbg = ['skin_2', 'eye_dark', 'hair_black', 'boot_white', 'accent_turquoise', 'mustard', 'vermilion', 'enamel_cobalt', 'chrome', 'accent_mint', 'violet_signal_cloth', 'ink_black', 'navy', 'boot_white', 'coral_transit']
        cols = {r: dbg[i] for i, r in enumerate(ROLES)}
    p0.name = f'{fid}_LOD0'; p0.data.name = p0.name; p1.name = f'{fid}_LOD1'; p1.data.name = p1.name
    hit0 = dress(p0, arm, B, L, hs, eyes, person, ward, 0, cols, index)
    hit1, ratio1 = dress_lod1(p1, arm, B, L, hs, eyes, person, ward, cols, index)
    p1.name = f'{fid}_LOD1'; p1.data.name = p1.name
    full_rel = f'Figures/{fid}/{fid}_full.fbx'; crowd_rel = f'Figures/{fid}/{fid}.fbx'
    mats = {'cp_palette': 'CharPipe/Shared/Materials/cp_palette.mat'}
    export_fbx(os.path.join(out, full_rel), (arm, p0, p1), arm)
    open(os.path.join(out, full_rel) + '.meta', 'w').write(um.model_meta('CharPipe/' + full_rel, mats))
    full_bones = len(arm.data.bones)
    lods = [{'name': o.name, 'tris': tris(o), 'verts': len(o.data.vertices)} for o in (p0, p1)]
    crowd_skeleton(arm, [p0, p1])
    export_fbx(os.path.join(out, crowd_rel), (arm, p0, p1), arm)
    open(os.path.join(out, crowd_rel) + '.meta', 'w').write(um.model_meta('CharPipe/' + crowd_rel, mats))
    zs = [v.co.z for v in p0.data.vertices]
    leg = round(leg_length(arm), 4)
    sha = lambda r: hashlib.sha256(open(os.path.join(out, r), 'rb').read()).hexdigest()
    row = {'id': fid, 'person': person['id'], 'role': person['role'], 'label': f"{person['role']} ({person['sex']}, {person['ageBand']}): " + ', '.join(person['parts']),
           'standIn': True, 'generated': 'tools/figures/charpipe (brief 069 proof, round 2)',
           'fbx': 'CharPipe/' + crowd_rel, 'sha256': sha(crowd_rel), 'rig': {'type': 'humanoid', 'bones': len(arm.data.bones), 'names': 'mixamo', 'fingers': 'modelled, posed relaxed, weighted to the hand'},
           'fbxFull': 'CharPipe/' + full_rel, 'sha256Full': sha(full_rel), 'rigFull': {'type': 'humanoid', 'bones': full_bones, 'names': 'mixamo', 'fingers': True},
           'restPose': 'T (arms, hands and fingers along +-X, palms down)',
           'heightM': round(max(zs) - min(zs), 3), 'legLengthM': leg, 'speedsMs': speeds(out, leg),
           'speedRule': 'in-place clips: move at speedsMs[clip] = clip strideMs x legLengthM / clip sourceLegLengthM (Clips/clips.rows.json)',
           'lods': lods, 'lod1BodyCollapse': ratio1, 'lodScreenHeights': [0.08, 0.012],
           'materials': [{'name': 'cp_palette', 'file': 'CharPipe/Shared/Materials/cp_palette.mat', 'shared': True}],
           'macros': person['macros'], 'parts': person['parts'], 'base': person['base'], 'colours': person['colours'], 'faceRoles': [hit0, hit1]}
    os.makedirs(os.path.join(out, 'rows'), exist_ok=True)
    with open(os.path.join(out, 'rows', fid + '.json'), 'w') as f: json.dump(row, f, indent=1, sort_keys=True)
    print('DONE', fid, row['heightM'], [(l['name'], l['tris']) for l in lods], full_bones, len(arm.data.bones), 'lod1 body x', ratio1)
    return row


# ------------------------------------------------------------------ masters: the base bodies and their slider shape keys
SLIDERS = [('height_low', {'height': 0.0}), ('height_high', {'height': 1.0}), ('weight_low', {'weight': 0.0}), ('weight_high', {'weight': 1.0}),
           ('muscle_low', {'muscle': 0.0}), ('muscle_high', {'muscle': 1.0}), ('age_old', {'age': 0.95})]


def master(out, ward, index):
    for sex in ('m', 'f'):
        ref = {'gender': 1.0 if sex == 'm' else 0.0, 'age': 0.5, 'muscle': 0.5, 'weight': 0.5, 'height': 0.5, 'proportions': 0.5}
        outfit = {'id': f'master-{sex}', 'role': 'commuter', 'sex': sex, 'ageBand': 'young', 'base': ['body', 'boots', 'eyes'], 'buzz': False,
                  'parts': ['top_jacket', 'legs_skirt' if sex == 'f' else 'legs_trousers', 'coat_car', 'hair_bob' if sex == 'f' else 'hair_crop'],
                  'colours': {'S': 'skin_3', 'E': 'eye_dark', 'HR': 'hair_dark', 'M': 'accent_turquoise', 'B': 'paper_cream', 'T': 'chrome', 'TL': 'brass_line', 'BT': 'boot_white', 'H': 'ivory_uniform', 'L': 'paper_cream', 'LT': 'skin_3', 'G': 'ink_black', 'D': 'ink_black', 'AP': 'ivory_uniform', 'BG': 'chrome'}}
        for what, parts in (('base', []), ('outfit', outfit['parts'])):
            shapes = []
            for name, d in [('ref', {})] + SLIDERS:
                reset(); m = dict(ref); m.update(d)
                arm, (p0, p1), eyes = make_human(m, sex, reference(out)); prune(arm, [p0, p1], KEEP)
                bpy.data.objects.remove(p1, do_unlink=True)
                L, B, hs = landmarks(arm, eyes, p0)
                pp = dict(outfit); pp['parts'] = parts; pp['base'] = outfit['base'] if parts else ['body', 'eyes']
                dress(p0, arm, B, L, hs, eyes, pp, ward, 0, outfit['colours'], index)
                shapes.append((name, [tuple(v.co) for v in p0.data.vertices]))
                if name == 'ref':
                    os.makedirs(os.path.join(out, 'Base'), exist_ok=True)
                    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(out, 'Base', f'_{what}_{sex}_ref.blend'), compress=False)
            bpy.ops.wm.open_mainfile(filepath=os.path.join(out, 'Base', f'_{what}_{sex}_ref.blend'))
            o = next(x for x in bpy.data.objects if x.type == 'MESH'); arm = next(x for x in bpy.data.objects if x.type == 'ARMATURE')
            o.name = f'{what}_{sex}'; o.shape_key_add(name='Basis')
            for name, co in shapes[1:]:
                if len(co) != len(o.data.vertices): print('SKIP-SHAPE', what, sex, name, len(co), len(o.data.vertices)); continue
                k = o.shape_key_add(name=name)
                for i, c in enumerate(co): k.data[i].co = c
            if what == 'base': export_fbx(os.path.join(out, 'Base', f'base_{sex}.fbx'), (o, arm), arm)
            os.makedirs(os.path.join(out, 'Master'), exist_ok=True)
            bpy.ops.wm.save_as_mainfile(filepath=os.path.join(out, 'Master', f'{what}_{sex}.blend'), compress=False)
            os.remove(os.path.join(out, 'Base', f'_{what}_{sex}_ref.blend'))
            print('MASTER', what, sex, len(o.data.vertices), 'keys', len(o.data.shape_keys.key_blocks) - 1)


def main():
    specs_path, out, only, want_master = args()
    ward = json.load(open(os.path.join(HERE, 'wardrobe.json')))
    _, index = palette()
    reset(); pin_fbx()
    if want_master:
        master(out, ward, index); return
    people = json.load(open(specs_path))['people']
    if only: people = [p for p in people if p['id'] in only]
    for p in people: build(p, ward, out, index)
    if not only or 'shared' in only: write_shared(out)


if __name__ == '__main__':
    main()
