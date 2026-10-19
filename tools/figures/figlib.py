"""Shared Blender helpers for the stand-in people (brief 020). Run inside Blender: blender -b -P script.py -- args."""
import bpy, math, os, sys, mathutils
from mathutils import Vector, Matrix, Quaternion

def args():
    return sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []

def clean():
    bpy.ops.wm.read_factory_settings(use_empty=True)

def load(path):
    """Import a glb/gltf/fbx; return the new objects. Drops the importer's bone-shape icospheres."""
    before = set(bpy.data.objects)
    if path.lower().endswith(('.glb', '.gltf')): bpy.ops.import_scene.gltf(filepath=path)
    else: bpy.ops.import_scene.fbx(filepath=path)
    new = [o for o in bpy.data.objects if o not in before]
    for o in list(new):
        if o.type == 'MESH' and o.name.startswith('Icosphere'):
            bpy.data.objects.remove(o, do_unlink=True); new.remove(o)
    return new

def rigs(objs):
    arm = [o for o in objs if o.type == 'ARMATURE']
    meshes = [o for o in objs if o.type == 'MESH']
    return (arm[0] if arm else None), meshes

def tris(o):
    return sum(len(p.vertices) - 2 for p in o.data.polygons)

def world_bounds(meshes):
    dg = bpy.context.evaluated_depsgraph_get()
    mn = Vector((1e9,) * 3); mx = Vector((-1e9,) * 3)
    for o in meshes:
        e = o.evaluated_get(dg); me = e.to_mesh()
        for v in me.vertices:
            w = o.matrix_world @ v.co
            for i in range(3): mn[i] = min(mn[i], w[i]); mx[i] = max(mx[i], w[i])
        e.to_mesh_clear()
    return mn, mx

def setup_render(w=1000, h=1000, ortho=2.4, bg=(0.78, 0.78, 0.76)):
    sc = bpy.context.scene
    sc.render.engine = 'BLENDER_EEVEE'
    sc.render.resolution_x = w; sc.render.resolution_y = h
    sc.render.film_transparent = False
    sc.view_settings.view_transform = 'Standard'
    try: sc.eevee.taa_render_samples = 16
    except Exception: pass
    wd = bpy.data.worlds.new('w'); wd.use_nodes = True
    bgn = wd.node_tree.nodes['Background']; bgn.inputs['Color'].default_value = (*bg, 1); bgn.inputs['Strength'].default_value = 1.0
    sc.world = wd
    sun = bpy.data.objects.new('sun', bpy.data.lights.new('sun', 'SUN')); bpy.context.scene.collection.objects.link(sun)
    sun.data.energy = 2.5; sun.rotation_euler = (math.radians(50), 0, math.radians(-25))
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); bpy.context.scene.collection.objects.link(cam)
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = ortho; cam.data.clip_end = 100
    sc.camera = cam
    return cam

def aim(cam, target, pitch_deg, yaw_deg, dist=20):
    """Camera looking at target from above: pitch = angle below horizontal, yaw = turn about Z (0 = looking along +Y... i.e. from -Y)."""
    p = math.radians(pitch_deg); y = math.radians(yaw_deg)
    # camera sits at -Y rotated by yaw, raised by pitch, looks at the target
    d = Vector((math.sin(y) * math.cos(p), -math.cos(y) * math.cos(p), math.sin(p)))
    cam.location = target + d * dist
    cam.rotation_euler = (-d).to_track_quat('-Z', 'Y').to_euler()


# ---------------------------------------------------------------- rigs: names, weights, units

# Meshy-style rig (scout, tech, guard, waitress, C1): the spine is named top-down and a knee helper doubles each knee.
MESHY_TO_MIXAMO = {'Spine02': 'Spine', 'Spine01': 'Spine1', 'Spine': 'Spine2', 'neck': 'Neck', 'head_end': 'HeadTop_End'}
MERGE_INTO = {'LeftKneeHelper': 'LeftLeg', 'RightKneeHelper': 'RightLeg', 'headfront': 'Head'}   # duplicate-position helpers: weights go to the real joint

def vg_rename(meshes, old, new):
    for m in meshes:
        g = m.vertex_groups.get(old)
        if g: g.name = new

def vg_merge(meshes, src, dst):
    """Add src's weights to dst and drop src."""
    for m in meshes:
        gs = m.vertex_groups.get(src)
        if not gs: continue
        gd = m.vertex_groups.get(dst) or m.vertex_groups.new(name=dst)
        for v in m.data.vertices:
            for ge in v.groups:
                if ge.group == gs.index:
                    w = ge.weight
                    try: w += gd.weight(v.index)
                    except RuntimeError: pass
                    gd.add([v.index], min(w, 1.0), 'REPLACE')
        m.vertex_groups.remove(gs)

def mixamo_names(arm, meshes):
    """Rename a Meshy-style rig to Mixamo names (Unity Humanoid maps it unaided) and fold the helper bones into their joints."""
    names = {b.name for b in arm.data.bones}
    if 'Spine02' not in names: return
    for src, dst in MERGE_INTO.items():
        if src in names: vg_merge(meshes, src, dst)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode='EDIT')
    eb = arm.data.edit_bones
    for src in MERGE_INTO:
        if src in eb:
            for c in list(eb[src].children): c.parent = eb[src].parent
            eb.remove(eb[src])
    bpy.ops.object.mode_set(mode='OBJECT')
    # two steps: 'Spine' -> 'Spine2' must not collide with 'Spine02' -> 'Spine'
    tmp = {s: '__' + d for s, d in MESHY_TO_MIXAMO.items()}
    for s, t in tmp.items():
        if s in arm.data.bones: arm.data.bones[s].name = t; vg_rename(meshes, s, t)
    for s, d in MESHY_TO_MIXAMO.items():
        t = '__' + d
        if t in arm.data.bones: arm.data.bones[t].name = d; vg_rename(meshes, t, d)

def limit_weights(meshes, limit=4):
    """At most `limit` influences per vertex, renormalised (Unity skins four)."""
    for m in meshes:
        for v in m.data.vertices:
            gs = sorted(((g.group, g.weight) for g in v.groups), key=lambda x: -x[1])
            if len(gs) <= limit and abs(sum(w for _, w in gs) - 1) < 1e-4: continue
            keep = gs[:limit]; tot = sum(w for _, w in keep) or 1.0
            for gi, _ in gs[limit:]: m.vertex_groups[gi].remove([v.index])
            for gi, w in keep: m.vertex_groups[gi].add([v.index], w / tot, 'REPLACE')

def rest_pose(arm):
    arm.data.pose_position = 'REST'
    if arm.animation_data: arm.animation_data.action = None
    bpy.context.view_layer.update()

def bake_to_meters(arm, meshes, scale=1.0, shift=Vector((0, 0, 0))):
    """Put the rig and its skinned meshes in metres at object scale 1: the armature's own scale (glTF rigs are x0.01) and
    the mesh parents are baked into the data, then everything is scaled and shifted (feet on z = 0)."""
    A = arm.matrix_world.copy()
    T = Matrix.Translation(shift) @ Matrix.Scale(scale, 4)
    for m in meshes:
        m.data.transform(T @ m.matrix_world)
        m.parent = None; m.matrix_world = Matrix.Identity(4)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode='EDIT')
    M = T @ A
    for eb in arm.data.edit_bones:
        h, t = M @ eb.head, M @ eb.tail
        eb.head, eb.tail = h, t
    bpy.ops.object.mode_set(mode='OBJECT')
    arm.matrix_world = Matrix.Identity(4); arm.scale = (1, 1, 1)
    for m in meshes:
        m.parent = arm
        m.matrix_parent_inverse = Matrix.Identity(4)
        for md in m.modifiers:
            if md.type == 'ARMATURE': md.object = arm
    bpy.context.view_layer.update()

def height_of(arm, meshes):
    rest_pose(arm)
    mn, mx = world_bounds(meshes)
    return mn, mx


def cut_webs(obj):
    """Remove the faces an image-to-3D mesh grows between the two feet (boots fused at the sole): a vertex below the crotch that leans on both a left
    and a right leg is a bridge, and its faces go; so do faces joining a left and a right leg there. Returns how many faces went."""
    import bmesh
    names = {g.index: g.name for g in obj.vertex_groups}
    def side(n): return 'L' if n.startswith('Left') else ('R' if n.startswith('Right') else '')
    def base(n): return n[4:] if n.startswith('Left') else (n[5:] if n.startswith('Right') else n)
    LEG = ('UpLeg', 'Leg', 'Foot', 'ToeBase'); ARM = ('ForeArm', 'Hand'); CORE = ('Hips', 'Spine', 'Spine1', 'Spine2')
    info = {}
    for v in obj.data.vertices:
        w = {}
        for g in v.groups: w[names[g.group]] = g.weight
        legs = {'L': sum(x for n, x in w.items() if side(n) == 'L' and base(n) in LEG), 'R': sum(x for n, x in w.items() if side(n) == 'R' and base(n) in LEG)}
        low = v.co.z < 0.62   # below the crotch: above it the two legs really are one surface
        bridge = low and legs['L'] >= 0.25 and legs['R'] >= 0.25
        dom = max(w.items(), key=lambda kv: kv[1]) if w else ('', 0.0)
        k = None
        if dom[1] >= 0.45:
            n = dom[0]; b = base(n); sd = side(n)
            if b in LEG: k = ('leg', sd if low else '')
            elif b in ARM: k = ('arm', sd)
            elif b in CORE: k = ('core', '')
        info[v.index] = (bridge, k)
    bm = bmesh.new(); bm.from_mesh(obj.data); bm.verts.ensure_lookup_table()
    kill = []
    for f in bm.faces:
        ks = [info[v.index] for v in f.verts]
        if any(b for b, _ in ks): kill.append(f); continue
        cl = {k for _, k in ks if k}
        legs = {k[1] for k in cl if k[0] == 'leg' and k[1]}; anyleg = any(k[0] == 'leg' for k in cl); arms = {k[1] for k in cl if k[0] == 'arm'}; core = any(k[0] == 'core' for k in cl)
        if len(legs) > 1: kill.append(f)   # (forearm-to-hip and arm-to-arm rules cracked jackets and capes where a hand hangs by the hem: weights are cleaned instead, clean_weights)
    bmesh.ops.delete(bm, geom=kill, context='FACES_ONLY')
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context='VERTS')
    bm.to_mesh(obj.data); bm.free(); obj.data.update()
    return len(kill)


def weld(meshes, dist=1e-5):
    """The glTF importer leaves a vertex per UV seam or normal split, so a mesh is a set of islands that touch only by position. Skinning keeps those
    twins together, but decimating does not (each island's border moves alone and pixel-sized cracks open). Weld the twins; UVs stay (they live on the loops)."""
    import bmesh
    n = 0
    for m in meshes:
        bm = bmesh.new(); bm.from_mesh(m.data); a = len(bm.verts)
        bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=dist)
        n += a - len(bm.verts)
        bm.to_mesh(m.data); bm.free(); m.data.update()
    return n


def straighten_feet(arm, meshes, lod0):
    """Image-to-3D figures stand with the toes turned out (the A-pose sheets splay the feet 5 to 20 degrees). A Humanoid avatar keeps the rest yaw,
    so every walk comes out duck-footed and the planted foot twists. Turn each foot, its toe bones and the vertices that follow them about the
    vertical axis through the ankle until the foot points straight ahead (forward = square to the line across the hips)."""
    import numpy as np
    names = {g.index: g.name for g in lod0.vertex_groups}
    B = arm.data.bones
    P = lambda n: B[n].head_local.copy()
    lr = P('LeftUpLeg') - P('RightUpLeg'); lr.z = 0; lr.normalize()
    fwd = Vector((-lr.y, lr.x, 0))
    d = P('LeftToeBase') - P('LeftFoot'); d.z = 0
    if d.dot(fwd) < 0: fwd = -fwd
    turns = {}
    for s in ('Left', 'Right'):
        pts = []
        for v in lod0.data.vertices:
            w = sum(g.weight for g in v.groups if names[g.group] in (s + 'Foot', s + 'ToeBase'))
            if w > 0.6 and v.co.z < 0.14: pts.append((v.co.x, v.co.y))
        if len(pts) < 20: continue
        a = np.array(pts); a -= a.mean(0); ax = np.linalg.svd(a, full_matrices=False)[2][0]
        axv = Vector((ax[0], ax[1], 0))
        if axv.dot(fwd) < 0: axv = -axv
        phi = math.atan2(axv.x * fwd.y - axv.y * fwd.x, axv.dot(fwd))   # signed angle about Z that takes the foot's axis onto forward
        turns[s] = (phi, P(s + 'Foot'))
    # the bones
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode='EDIT')
    eb = arm.data.edit_bones
    for s, (phi, piv) in turns.items():
        R = Matrix.Rotation(phi, 3, 'Z')
        for n in (s + 'Foot', s + 'ToeBase', s + 'Toe_End'):
            if n not in eb: continue
            b = eb[n]
            if n != s + 'Foot': b.head = piv + R @ (b.head - piv)
            b.tail = piv + R @ (b.tail - piv)
        # the toe joint sits a little to the side of the ankle in these rigs: put the ball of the foot straight ahead of it, so the bone points forward too
        side = lr if s == 'Left' else -lr
        for n in (s + 'ToeBase', s + 'Toe_End'):
            if n in eb:
                b = eb[n]; b.head = b.head - side * ((b.head - piv).dot(side)); b.tail = b.tail - side * ((b.tail - piv).dot(side))
        if s + 'Foot' in eb and s + 'ToeBase' in eb: eb[s + 'Foot'].tail = eb[s + 'ToeBase'].head
    bpy.ops.object.mode_set(mode='OBJECT')
    # the vertices, by weight
    for m in meshes:
        gn = {g.index: g.name for g in m.vertex_groups}
        for v in m.data.vertices:
            for s, (phi, piv) in turns.items():
                w = sum(g.weight for g in v.groups if gn[g.group] in (s + 'Foot', s + 'ToeBase', s + 'Toe_End'))
                if w <= 0.0: continue
                R = Matrix.Rotation(phi * min(w, 1.0), 3, 'Z')
                v.co = piv + R @ (v.co - piv)
    bpy.context.view_layer.update()
    return {s: round(math.degrees(t[0]), 1) for s, t in turns.items()}


def clean_weights(obj, arm):
    """Nearest-surface weights from a donor give a hip vertex the forearm's weight when a hand hangs beside it. Drop any influence whose bone is too far from
    the vertex in rest (forearm and hand 0.14 m, arm 0.2, foot 0.18, shin 0.22, thigh 0.28), renormalise, and if nothing is left take the nearest bone."""
    bones = {}
    for b in arm.data.bones:   # a leaf bone (hand, toes, head end) has no real tail: stand one in along the parent's direction, 12 cm out
        t = b.tail_local.copy()
        if not b.children and b.parent:
            d = b.head_local - b.parent.head_local
            if d.length > 1e-6: t = b.head_local + d.normalized() * 0.12
        bones[b.name] = (b.head_local.copy(), t)
    def radius(n):
        b = n[4:] if n.startswith('Left') else (n[5:] if n.startswith('Right') else n)
        return {'ForeArm': 0.15, 'Hand': 0.17, 'Arm': 0.20, 'Foot': 0.18, 'ToeBase': 0.18, 'Leg': 0.22, 'UpLeg': 0.28}.get(b, 0.9)
    def seg(p, a, b):
        ab = b - a; l2 = ab.length_squared
        t = max(0.0, min(1.0, (p - a).dot(ab) / l2)) if l2 > 1e-9 else 0.0
        return (p - (a + ab * t)).length
    gn = {g.index: g.name for g in obj.vertex_groups}; changed = 0
    for v in obj.data.vertices:
        ws = {gn[g.group]: g.weight for g in v.groups}
        if not ws: continue
        kept = {n: w for n, w in ws.items() if seg(v.co, *bones[n]) <= radius(n)}
        if len(kept) == len(ws): continue
        if not kept:
            n = min(bones, key=lambda k: seg(v.co, *bones[k])); kept = {n: 1.0}
        tot = sum(kept.values())
        for n in ws:
            if n not in kept: obj.vertex_groups[n].remove([v.index])
        for n, w in kept.items():
            (obj.vertex_groups.get(n) or obj.vertex_groups.new(name=n)).add([v.index], w / tot, 'REPLACE')
        changed += 1
    return changed


def smooth_skin(obj, z0, z1, iters=12, keep=0.5):
    """Loose cloth (the cape of C1) skinned by nearest surface follows the arm on one side of a fold and the spine on the other, so it tears open when the arm
    swings. Between heights z0 and z1 every vertex's weights are blended with its neighbours' `iters` times (own share `keep`), so the change from arm to spine is
    spread over many edges and the cloth stretches instead of splitting. Weights stay normalised and at most four per vertex (limit_weights runs after)."""
    import bmesh
    gn = {g.index: g.name for g in obj.vertex_groups}
    W = {v.index: {gn[g.group]: g.weight for g in v.groups} for v in obj.data.vertices}
    bm = bmesh.new(); bm.from_mesh(obj.data); bm.verts.ensure_lookup_table()
    sel = {v.index for v in bm.verts if z0 <= v.co.z <= z1}
    for _ in range(iters):
        new = {}
        for i in sel:
            v = bm.verts[i]; nb = [e.other_vert(v).index for e in v.link_edges]
            acc = {n: w * keep for n, w in W[i].items()}
            if nb:
                f = (1 - keep) / len(nb)
                for j in nb:
                    for n, w in W[j].items(): acc[n] = acc.get(n, 0.0) + w * f
            new[i] = acc
        W.update(new)
    bm.free()
    for i in sel:
        acc = sorted(W[i].items(), key=lambda kv: -kv[1])[:6]; tot = sum(w for _, w in acc)
        for g in list(obj.data.vertices[i].groups): obj.vertex_groups[gn[g.group]].remove([i])
        for n, w in acc:
            (obj.vertex_groups.get(n) or obj.vertex_groups.new(name=n)).add([i], w / tot, 'REPLACE')
    return len(sel)


def rigid_cloth(obj, img, op):
    """Loose cloth that must not tear (the cape of C1): every vertex whose texel is in a colour window and whose height is in a range is skinned to one bone
    (or a blend, op['bones'] = {name: weight}), so the whole piece moves as one and cannot split between an arm and the spine. smooth_skin spread the change over
    many edges and the cape still opened a dark slit when the arm swung; one transform cannot. Returns how many vertices were set.
    op: hue [lo, hi] degrees, minSat, minVal, z [lo, hi] metres, bones {name: weight}, maxX (|x| limit in metres, keeps the hands out)."""
    import numpy as np
    w, h = img.size
    px = np.empty(w * h * 4, dtype=np.float32); img.pixels.foreach_get(px); px = px.reshape(h, w, 4)   # rows run bottom to top, like UV v
    r, g, b = px[..., 0], px[..., 1], px[..., 2]
    mx, mn = np.maximum(np.maximum(r, g), b), np.minimum(np.minimum(r, g), b); d = mx - mn
    hh = np.zeros_like(mx); m = d > 1e-6
    rm = m & (mx == r); gm = m & (mx == g) & ~rm; bm_ = m & ~rm & ~gm
    hh[rm] = ((g - b)[rm] / d[rm]) % 6; hh[gm] = (b - r)[gm] / d[gm] + 2; hh[bm_] = (r - g)[bm_] / d[bm_] + 4
    hd = hh * 60.0; sa = np.where(mx > 1e-6, d / np.maximum(mx, 1e-6), 0)
    lo, hi = op['hue']; ok = (hd >= lo) & (hd <= hi) & (sa >= op.get('minSat', 0.3)) & (mx >= op.get('minVal', 0.3))
    uv = obj.data.uv_layers.active.data; vuv = {}
    for l in obj.data.loops: vuv.setdefault(l.vertex_index, uv[l.index].uv.copy())
    z0, z1 = op['z']; maxx = op.get('maxX', 9.0); names = op['bones']; n = 0
    gname = {g.index: g.name for g in obj.vertex_groups}
    def limb(v):   # a hand or forearm: skin in the same hue window, never cape
        return sum(g.weight for g in v.groups if gname[g.group].endswith(('Hand', 'ForeArm'))) >= op.get('limb', 0.6)
    inside = lambda v: z0 <= v.co.z <= z1 and abs(v.co.x) <= maxx and not limb(v)
    pick = set()
    for v in obj.data.vertices:
        if not inside(v) or v.index not in vuv: continue
        u, vv = vuv[v.index]; x = min(w - 1, max(0, int((u % 1.0) * w))); y = min(h - 1, max(0, int((vv % 1.0) * h)))
        if ok[y, x]: pick.add(v.index)
    nb = {}   # grow the pick over the mesh edges: a cape's hem and fold edges sample dark or inpainted texels and would stay on the arm
    for e in obj.data.edges:
        a_, b_ = e.vertices; nb.setdefault(a_, []).append(b_); nb.setdefault(b_, []).append(a_)
    for _ in range(op.get('grow', 0)):
        pick |= {j for i in pick for j in nb.get(i, ()) if inside(obj.data.vertices[j])}
    for v in obj.data.vertices:
        if v.index not in pick: continue
        for gr in list(v.groups): obj.vertex_groups[gr.group].remove([v.index])
        for bn, bw in names.items():
            (obj.vertex_groups.get(bn) or obj.vertex_groups.new(name=bn)).add([v.index], bw, 'REPLACE')
        n += 1
    return n


def torso_guard(objs, arm, margin=0.05):
    """Keep the arms' weights off the waist and hips (brief 022, the carry hip tear). Nearest-surface weights from the donor give a vertex on the side of the
    waist or hip (a jacket hem, a jumpsuit's belt line) the forearm's or upper arm's influence when an arm hangs a hand's width away. A walk never shows it
    (the arm swings a little); a person draped over a shoulder hangs the arms far from the hips, so those vertices are dragged a metre from their neighbours
    and a sliver of cloth tears open at the fold (edges stretched 30 to 60 times). Band: below the elbow's own height (+ margin), above the crotch, closer
    to the middle line than the elbow joint by `margin`: no arm, forearm, hand or finger weight there; what is left is renormalised (nothing left: the hips,
    or the spine above the waist). Returns how many vertices were changed (all objs)."""
    B = arm.data.bones
    el = B['LeftForeArm'].head_local; xmax = abs(el.x) - margin; z1 = el.z + margin; z0 = B['LeftUpLeg'].head_local.z - 0.05
    zw = B['Spine'].head_local.z
    is_arm = lambda n: ('Arm' in n) or n.startswith(('LeftHand', 'RightHand'))
    n = 0
    for o in objs:
        gn = {g.index: g.name for g in o.vertex_groups}
        for v in o.data.vertices:
            if not (z0 <= v.co.z <= z1 and abs(v.co.x) <= xmax): continue
            ws = {gn[g.group]: g.weight for g in v.groups}
            drop = [k for k in ws if is_arm(k)]
            if not drop: continue
            keep = {k: w for k, w in ws.items() if k not in drop}
            if sum(keep.values()) < 1e-3: keep = {('Hips' if v.co.z < zw else 'Spine'): 1.0}
            tot = sum(keep.values())
            for k in drop: o.vertex_groups[k].remove([v.index])
            for k, w in keep.items(): (o.vertex_groups.get(k) or o.vertex_groups.new(name=k)).add([v.index], w / tot, 'REPLACE')
            n += 1
    return n


def cut_side_webs(objs, arm, margin=0.05):
    """A sleeve that hangs flat against the jacket is joined to it by a strip of faces down the side, where an image-to-3D mesh fuses the two. Walking never shows
    it; a body hung over a shoulder swings the arm away and the strip tears into a sliver. Below the elbow's height (+ margin) and above the crotch, a face with a
    vertex that follows the arm (arm, forearm, hand weight over half) and one that follows the trunk (hips and spine over half) is a web: deleted. Returns the faces cut."""
    import bmesh
    B = arm.data.bones
    z1 = B['LeftForeArm'].head_local.z + margin; z0 = B['LeftUpLeg'].head_local.z - 0.05
    is_arm = lambda n: ('Arm' in n) or n.startswith(('LeftHand', 'RightHand'))
    is_core = lambda n: n in ('Hips', 'Spine', 'Spine1', 'Spine2')
    cut = 0
    for o in objs:
        gn = {g.index: g.name for g in o.vertex_groups}
        kind = {}
        for v in o.data.vertices:
            a = sum(g.weight for g in v.groups if is_arm(gn[g.group])); c = sum(g.weight for g in v.groups if is_core(gn[g.group]))
            kind[v.index] = ('A' if a > 0.5 else ('C' if c > 0.5 else '')) if z0 <= v.co.z <= z1 else ''
        bm = bmesh.new(); bm.from_mesh(o.data); bm.verts.ensure_lookup_table()
        kill = [f for f in bm.faces if {kind[v.index] for v in f.verts} >= {'A', 'C'}]
        bmesh.ops.delete(bm, geom=kill, context='FACES_ONLY')
        bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context='VERTS')
        bm.to_mesh(o.data); bm.free(); o.data.update(); cut += len(kill)
    return cut
