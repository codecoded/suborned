# Blender: print tris, bones, actions, size of each glb/fbx given after `--`. Read-only.
import bpy, sys, json, os
files = sys.argv[sys.argv.index('--') + 1:]
for f in files:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    try:
        if f.endswith('.glb') or f.endswith('.gltf'): bpy.ops.import_scene.gltf(filepath=f)
        else: bpy.ops.import_scene.fbx(filepath=f)
    except Exception as e:
        print('INSPECT', json.dumps({'file': f, 'error': str(e)[:100]})); continue
    meshes = [o for o in bpy.data.objects if o.type == 'MESH']
    arms = [o for o in bpy.data.objects if o.type == 'ARMATURE']
    tris = 0; mn = [1e9]*3; mx = [-1e9]*3
    for o in meshes:
        tris += sum(len(p.vertices) - 2 for p in o.data.polygons)
        for c in o.bound_box:
            w = o.matrix_world @ __import__('mathutils').Vector(c)
            for i in range(3): mn[i] = min(mn[i], w[i]); mx[i] = max(mx[i], w[i])
    acts = [(a.name, round(a.frame_range[1] - a.frame_range[0] + 1)) for a in bpy.data.actions]
    print('INSPECT', json.dumps({'file': os.path.basename(f), 'tris': tris, 'meshes': len(meshes), 'skinned': sum(1 for o in meshes if o.find_armature()), 'bones': [len(a.data.bones) for a in arms],
        'size': [round(mx[i]-mn[i], 3) for i in range(3)], 'min': [round(v,3) for v in mn], 'actions': acts[:8], 'fps': bpy.context.scene.render.fps,
        'mats': len(bpy.data.materials), 'imgs': [(i.name, i.size[0], i.size[1]) for i in bpy.data.images][:6]}))
