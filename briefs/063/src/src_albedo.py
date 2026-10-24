import bpy,sys
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=sys.argv[sys.argv.index('--')+1])
for im in bpy.data.images: print('IMG',im.name,im.size[:])
im=max(bpy.data.images,key=lambda i:i.size[0]*i.size[1])
im.filepath_raw=sys.argv[sys.argv.index('--')+2]; im.file_format='PNG'; im.save()
