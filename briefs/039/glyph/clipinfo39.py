# Glyph, brief 039: print the frame range of each clip FBX (Blender side). blender -b -P clipinfo39.py -- <out.json> <path>...
import bpy, sys, json, os
args = sys.argv[sys.argv.index('--') + 1:]; out = args[0]; res = {}
for p in args[1:]:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    try:
        bpy.ops.import_scene.fbx(filepath=p)
        acts = [a for a in bpy.data.actions]
        best = max(acts, key=lambda a: a.frame_range[1] - a.frame_range[0]) if acts else None
        res[os.path.basename(p)] = [best.frame_range[0], best.frame_range[1]] if best else None
    except Exception as e:
        res[os.path.basename(p)] = str(e)[:80]
json.dump(res, open(out, 'w'), indent=1); print('wrote', out)
