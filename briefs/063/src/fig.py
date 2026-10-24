# full-body turnaround-ish render of a figure for the board: usage Blender -b --python fig.py -- name out [albedo]
import bpy, sys, mathutils, glob, os
a=sys.argv[sys.argv.index('--')+1:]; name,out=a[0],a[1]; alb=a[2] if len(a)>2 and a[2]!='-' else None
F='/Users/sky/Projects/suborned/unity/Assets/Suborned/People/Figures/%s/'%name
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=F+name+'.fbx')
objs=[o for o in bpy.context.scene.objects if o.type=='MESH']
lods=sorted([o for o in objs if '_LOD' in o.name],key=lambda o:o.name)
for o in lods[1:]: bpy.data.objects.remove(o,do_unlink=True)
objs=lods[:1] or objs
c=sorted(glob.glob(F+'Textures/%s_body_*_albedo.jpg'%name),key=lambda p:-int(os.path.basename(p).split('_')[-2]))
tex=bpy.data.images.load(alb or c[0])
for o in objs:
    m=bpy.data.materials.new('m'); m.use_nodes=True; nt=m.node_tree
    for n in list(nt.nodes): nt.nodes.remove(n)
    t=nt.nodes.new('ShaderNodeTexImage'); t.image=tex; b=nt.nodes.new('ShaderNodeBsdfPrincipled'); b.inputs['Roughness'].default_value=0.75
    ot=nt.nodes.new('ShaderNodeOutputMaterial'); nt.links.new(t.outputs['Color'],b.inputs['Base Color']); nt.links.new(b.outputs['BSDF'],ot.inputs['Surface'])
    o.data.materials.clear(); o.data.materials.append(m)
tgt=mathutils.Vector((0,0,0.9)); cam=bpy.data.objects.new('c',bpy.data.cameras.new('c')); bpy.context.scene.collection.objects.link(cam)
cam.data.lens=85; cam.data.sensor_width=36; cam.location=(0,-6.2,0.9); cam.rotation_euler=(tgt-cam.location).to_track_quat('-Z','Y').to_euler(); bpy.context.scene.camera=cam
def L(loc,e,col=(1,.96,.9)):
    l=bpy.data.lights.new('l','AREA'); l.energy=e; l.size=3; l.color=col; o=bpy.data.objects.new('l',l); bpy.context.scene.collection.objects.link(o); o.location=loc; o.rotation_euler=(tgt-o.location).to_track_quat('-Z','Y').to_euler()
L((-3,-4,3),700); L((3,-3,1.5),250,(.88,.95,1)); L((0,4,3),300)
w=bpy.data.worlds.new('w'); w.use_nodes=True; w.node_tree.nodes['Background'].inputs[0].default_value=(.55,.55,.55,1); w.node_tree.nodes['Background'].inputs[1].default_value=.55; bpy.context.scene.world=w
sc=bpy.context.scene; sc.render.engine='BLENDER_EEVEE'; sc.render.resolution_x=600; sc.render.resolution_y=1000; sc.view_settings.view_transform='Standard'
sc.render.film_transparent=True; sc.render.image_settings.color_mode='RGBA'; sc.render.filepath=out; bpy.ops.render.render(write_still=True)
