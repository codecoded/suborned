"""Held props for the stand-in people (brief 022). Blender: blender -b -P tools/figures/props.py -- <props.json> <out People dir>

Each prop is built from primitives here (nothing imported), 1 unit = 1 m, the origin where it rests on the hand, and exported like a figure (Y up, +Z the way
the fingers point; FBX unit scale 1). Writes People/Props/<id>/<id>.fbx, its URP Lit materials, deterministic .meta files and .rows.props.json for assemble.py."""
import sys, os, json, math, hashlib
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from figlib import *
import unitymeta as um
import bmesh

PAL = {   # name: (linear-ish sRGB colour, metallic, smoothness): cream enamel, chrome, a brass line, coffee
    'cream': ((0.93, 0.88, 0.76), 0.0, 0.45),
    'chrome': ((0.82, 0.84, 0.86), 1.0, 0.85),
    'brass': ((0.80, 0.62, 0.28), 0.9, 0.7),
    'coffee': ((0.16, 0.09, 0.05), 0.0, 0.6),
}
SEG = 28


def mat(name):
    m = bpy.data.materials.new(f'tray_{name}'); m.use_nodes = True
    b = m.node_tree.nodes['Principled BSDF']; c = PAL[name]
    b.inputs['Base Color'].default_value = (*c[0], 1); b.inputs['Metallic'].default_value = c[1]; b.inputs['Roughness'].default_value = 1 - c[2]
    m.diffuse_color = (*c[0], 1)
    return m


def lathe(name, profile, material, seg=SEG, offset=(0, 0, 0), closed=False, scale=(1, 1, 1)):
    """Surface of revolution about Z from [(r, z), ...] (open profile unless closed)."""
    bm = bmesh.new(); rings = []
    for r, z in profile:
        rings.append([bm.verts.new((r * math.cos(2 * math.pi * i / seg), r * math.sin(2 * math.pi * i / seg), z)) for i in range(seg)])
    n = len(rings) - (0 if closed else 1)
    for k in range(n):
        a, b = rings[k], rings[(k + 1) % len(rings)]
        for i in range(seg):
            j = (i + 1) % seg
            try: bm.faces.new((a[i], a[j], b[j], b[i]))
            except ValueError: pass
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-6)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    o = bpy.data.objects.new(name, me); bpy.context.collection.objects.link(o)
    o.location = offset; o.scale = scale
    me.materials.append(material)
    return o


def torus(name, major, minor, material, z, offset=(0, 0), seg=SEG, ring=10, scale=(1, 1, 1), rot=(0, 0, 0)):
    prof = [(major + minor * math.cos(2 * math.pi * k / ring), minor * math.sin(2 * math.pi * k / ring)) for k in range(ring)]
    o = lathe(name, prof, material, seg=seg, offset=(offset[0], offset[1], z), closed=True, scale=scale)
    o.rotation_euler = rot
    return o


def finish(objs, name):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    bpy.ops.object.join(); o = bpy.context.active_object; o.name = name; o.data.name = name
    me = o.data
    for p in me.polygons: p.use_smooth = True
    try: bpy.ops.object.shade_smooth_by_angle(angle=math.radians(35))
    except Exception: pass
    return o


def build_tray():
    cream, chrome, brass, coffee = mat('cream'), mat('chrome'), mat('brass'), mat('coffee')
    parts = []
    # the tray: a shallow dish, flat floor 8 mm thick, a lip that stands 18 mm; origin at the middle of its underside
    parts.append(lathe('dish', [(0.0, 0.0), (0.140, 0.0), (0.152, 0.006), (0.157, 0.016), (0.150, 0.0185), (0.146, 0.0145), (0.136, 0.0085), (0.0, 0.0085)], cream))
    parts.append(torus('rim', 0.1545, 0.0045, chrome, 0.0175))                                  # the chrome rim
    parts.append(torus('line', 0.112, 0.0013, brass, 0.0087, ring=6))                             # the brass inlay: one thin line
    # cup and saucer, a hair larger than life so the cup reads at play zoom; set a little toward the far side so the handle faces the guest
    cx, cy, zs = 0.0, -0.018, 0.0085
    parts.append(lathe('saucer', [(0.0, 0.0), (0.046, 0.0), (0.072, 0.008), (0.074, 0.010), (0.070, 0.0105), (0.050, 0.0045), (0.0, 0.0045)], cream, offset=(cx, cy, zs)))
    parts.append(lathe('cup', [(0.0, 0.0045), (0.024, 0.0045), (0.040, 0.030), (0.046, 0.058), (0.043, 0.058), (0.038, 0.030), (0.0, 0.012)], cream, offset=(cx, cy, zs + 0.004)))
    parts.append(lathe('coffee', [(0.0, 0.0), (0.0405, 0.0)], coffee, offset=(cx, cy, zs + 0.004 + 0.049), seg=SEG, closed=False))
    parts[-1].data.materials.clear(); parts[-1].data.materials.append(coffee)
    parts.append(torus('handle', 0.019, 0.0048, cream, 0.0, offset=(cx + 0.049, cy, zs + 0.004 + 0.032), seg=20, ring=8, scale=(0.9, 0.9, 1.15), rot=(math.radians(90), 0, 0)))
    # spoon: a chrome bar and bowl on the saucer's side
    parts.append(lathe('spoonbowl', [(0.0, 0.0), (0.009, 0.0), (0.010, 0.0035), (0.0, 0.0035)], chrome, offset=(cx - 0.060, cy + 0.020, zs + 0.0105), scale=(1.0, 1.5, 1.0), seg=16))
    return parts


def main():
    a = args(); spec = json.load(open(a[0])); out = a[1]; rows = []
    for p in spec['props']:
        clean(); parts = build_tray() if p['builder'] == 'tray' else []
        obj = finish(parts, p['id'])
        rel = f"Props/{p['id']}/{p['id']}.fbx"; fbx = os.path.join(out, rel); os.makedirs(os.path.dirname(fbx), exist_ok=True)
        bpy.ops.object.select_all(action='DESELECT'); obj.select_set(True); bpy.context.view_layer.objects.active = obj
        bpy.ops.export_scene.fbx(filepath=fbx, use_selection=True, object_types={'MESH'}, use_mesh_modifiers=False, add_leaf_bones=False, bake_anim=False,
                                 axis_forward='-Z', axis_up='Y', apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True,
                                 mesh_smooth_type='OFF', path_mode='STRIP', embed_textures=False)
        mats = {}
        for name, (col, metallic, smooth) in PAL.items():
            key = f"tray_{name}"; mrel = f"Props/{p['id']}/Materials/{key}.mat"; os.makedirs(os.path.join(out, os.path.dirname(mrel)), exist_ok=True)
            text = um.mat_text(key, smoothness=smooth, tint=col).replace('    - _Metallic: 0\n', f'    - _Metallic: {metallic}\n')
            open(os.path.join(out, mrel), 'w').write(text); open(os.path.join(out, mrel + '.meta'), 'w').write(um.mat_meta(mrel)); mats[key] = mrel
        open(fbx + '.meta', 'w').write(um.prop_meta(rel, mats))
        me = obj.data
        rows.append({'id': p['id'], 'label': p['label'], 'fbx': rel, 'sha256': hashlib.sha256(open(fbx, 'rb').read()).hexdigest(), 'tris': sum(len(q.vertices) - 2 for q in me.polygons),
                     'materials': [{'name': k, 'file': v} for k, v in mats.items()], 'bone': p['bone'], 'clips': p['clips'], 'size': p['size'], 'attach': p.get('attach')})
        print('PROP', p['id'], rows[-1]['tris'], 'tris')
    json.dump(rows, open(os.path.join(out, '.props.rows.json'), 'w'), indent=1)


main()
