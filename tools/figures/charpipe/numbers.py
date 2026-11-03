"""Per-figure cost, old against new (brief 069): for each FBX, per LOD mesh: triangles, Blender vertices, vertices after UV/normal splits (what the GPU skins:
one per unique (position, normal, UV) corner), material slots; bones; and the textures each material binds. Writes JSON.
  blender -b -P tools/figures/charpipe/numbers.py -- <out.json> <People dir> <id,id,...> <CharPipe dir> <id,id,...>"""
import bpy, sys, os, json
a = sys.argv[sys.argv.index('--') + 1:]
out, people, old_ids, cp, new_ids = a[0], a[1], a[2].split(','), a[3], a[4].split(',')
rows = []
def measure(path, fid, kind):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=path)
    arm = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
    lods = []
    for o in sorted((o for o in bpy.data.objects if o.type == 'MESH'), key=lambda o: o.name):
        me = o.data; me.calc_loop_triangles() if hasattr(me, 'calc_loop_triangles') else None
        uv = me.uv_layers.active.data if me.uv_layers.active else None
        corners = set()
        for p in me.polygons:
            for li in p.loop_indices:
                l = me.loops[li]; n = tuple(round(x, 3) for x in me.corner_normals[li].vector) if hasattr(me, 'corner_normals') else ()
                corners.add((l.vertex_index, n, tuple(round(x, 5) for x in uv[li].uv) if uv else ()))
        lods.append({'name': o.name, 'tris': sum(len(p.vertices) - 2 for p in me.polygons), 'verts': len(me.vertices), 'gpuVerts': len(corners), 'materials': len(me.materials),
                     'groups': len(o.vertex_groups)})
    return {'id': fid, 'kind': kind, 'bones': len(arm.data.bones), 'lods': lods, 'bytes': os.path.getsize(path)}
for i in old_ids: rows.append(measure(f'{people}/Figures/{i}/{i}.fbx', i, 'today'))
for i in new_ids: rows.append(measure(f'{cp}/Figures/{i}/{i}.fbx', i, 'new'))
json.dump(rows, open(out, 'w'), indent=1); print('NUMBERS', len(rows))
