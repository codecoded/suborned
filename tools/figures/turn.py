"""Close views of a built figure: front, side, back and two head close-ups, at a chosen LOD, with that LOD's own albedo (brief 067, Forma). Blender, one figure a run:
  blender -b -P tools/figures/turn.py -- <People/Figures dir> <figure id> <lod> <out dir>
Writes <out>/<id>_{front,side,back,head,head3q}.png (420 x 760, a neutral grey ground, two soft lights; not the game's light: tools/figures/proof.py is). EEVEE, a second a view."""
import os, sys, math, glob, bpy, mathutils

root, name, lod, out = sys.argv[sys.argv.index('--') + 1:][:4]; lod = int(lod); os.makedirs(out, exist_ok=True)
F = f'{root}/{name}/'
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=F + name + '.fbx')
meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
keep = [o for o in meshes if o.name.endswith('_LOD%d' % lod)]
for o in meshes:
    if o not in keep: bpy.data.objects.remove(o, do_unlink=True)
size = sorted(int(os.path.basename(p).split('_')[-2]) for p in glob.glob(F + f'Textures/{name}_body_*_albedo.jpg'))[::-1][lod]
img = bpy.data.images.load(F + f'Textures/{name}_body_{size}_albedo.jpg')
for o in keep:
    m = bpy.data.materials.new('m'); m.use_nodes = True; nt = m.node_tree
    for n in list(nt.nodes): nt.nodes.remove(n)
    t = nt.nodes.new('ShaderNodeTexImage'); t.image = img; b = nt.nodes.new('ShaderNodeBsdfPrincipled'); b.inputs['Roughness'].default_value = 0.85
    ot = nt.nodes.new('ShaderNodeOutputMaterial'); nt.links.new(t.outputs['Color'], b.inputs['Base Color']); nt.links.new(b.outputs['BSDF'], ot.inputs['Surface'])
    o.data.materials.clear(); o.data.materials.append(m)
sc = bpy.context.scene
w = bpy.data.worlds.new('w'); w.use_nodes = True; w.node_tree.nodes['Background'].inputs[0].default_value = (.62, .62, .62, 1); w.node_tree.nodes['Background'].inputs[1].default_value = .9; sc.world = w
cam = bpy.data.objects.new('c', bpy.data.cameras.new('c')); sc.collection.objects.link(cam); sc.camera = cam
sc.render.engine = 'BLENDER_EEVEE'; sc.view_settings.view_transform = 'Standard'; sc.render.resolution_x = 420; sc.render.resolution_y = 760


def light(loc, e, tgt, col=(1, .96, .9)):
    l = bpy.data.lights.new('l', 'AREA'); l.energy = e; l.size = 3; l.color = col; o = bpy.data.objects.new('l', l); sc.collection.objects.link(o); o.location = loc
    o.rotation_euler = (tgt - o.location).to_track_quat('-Z', 'Y').to_euler()


def shot(tgt, dist, lens, ang, path):
    cam.data.lens = lens; cam.data.sensor_width = 36
    ca, sa = math.cos(math.radians(ang)), math.sin(math.radians(ang))
    cam.location = (tgt.x + dist * sa, tgt.y - dist * ca, tgt.z); cam.rotation_euler = (tgt - cam.location).to_track_quat('-Z', 'Y').to_euler()
    for o in [o for o in sc.objects if o.type == 'LIGHT']: bpy.data.objects.remove(o, do_unlink=True)
    light(cam.location + mathutils.Vector((-2.5 * ca, -2.5 * sa, 2.0)), 900, tgt); light(cam.location + mathutils.Vector((2.5 * ca, 2.0, 1.0)), 250, tgt, (.88, .95, 1))
    sc.render.filepath = path; bpy.ops.render.render(write_still=True)


body = mathutils.Vector((0, 0, 0.9)); head = mathutils.Vector((0, 0, 1.62))
shot(body, 6.5, 85, 0, f'{out}/{name}_front.png'); shot(body, 6.5, 85, 90, f'{out}/{name}_side.png'); shot(body, 6.5, 85, 180, f'{out}/{name}_back.png')
shot(head, 2.4, 85, 0, f'{out}/{name}_head.png'); shot(head, 2.4, 85, 60, f'{out}/{name}_head3q.png')
print('TURN', name, 'LOD', lod, 'albedo', size)
